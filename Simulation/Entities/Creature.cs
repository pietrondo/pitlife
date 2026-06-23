using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Rendering;

namespace PitLife.Simulation;

public abstract class Creature
{
    public Vector2 Position { get; set; }
    public Genome Genome { get; protected set; }
    public float Energy { get; set; }
    public float Age { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    public DeathCause DeathCause { get; private set; }
    public CreatureType CreatureType { get; protected set; }
    public DietType Diet { get; protected set; }
    public bool IsInfected { get; set; }
    public float DiseaseTimer { get; set; }
    public string DiseaseName { get; set; } = "";
    public float Immunity { get; set; }
    public bool IsPoisonous { get; set; }
    public float Toxicity { get; set; }
    public float NutritionalValue => Genome.Size * 5f * (1f - Toxicity * 0.7f);
    public bool IsSleeping { get; private set; }
    public float LastReproductionTime { get; set; } = -60f;
    public int LitterSize => Math.Max(1, (int)(Genome.Size * 1.5f));
    public float ReproductionCooldown => 30f + (1f - Genome.Metabolism) * 30f;
    public float Defense => Genome.Size * 5f + Genome.Metabolism * 3f;
    public float AttackPower => Genome.Speed * 4f + Genome.Size * 2f;
    public string Subspecies { get; set; } = "";

    // Memory
    public List<Vector2> RememberedFood { get; } = new();
    public List<Vector2> RememberedDanger { get; } = new();
    private const int MaxMemories = 5;
    private const float MemoryDecayChance = 0.001f;

    public void RememberFood(Vector2 pos)
    {
        if (RememberedFood.Count >= MaxMemories) RememberedFood.RemoveAt(0);
        RememberedFood.Add(pos);
    }

    public void RememberDanger(Vector2 pos)
    {
        if (RememberedDanger.Count >= MaxMemories) RememberedDanger.RemoveAt(0);
        RememberedDanger.Add(pos);
    }

    public void DecayMemories(Random rng)
    {
        float decayRate = 0.002f * (1f - Genome.MemorySpan * 0.8f);
        if (rng.NextDouble() < decayRate && RememberedFood.Count > 0)
            RememberedFood.RemoveAt(rng.Next(RememberedFood.Count));
        if (rng.NextDouble() < decayRate && RememberedDanger.Count > 0)
            RememberedDanger.RemoveAt(rng.Next(RememberedDanger.Count));
    }
    private ActivityPattern? _activity;
    public ActivityPattern Activity
    {
        get
        {
            _activity ??= GetDefaultActivity(Species);
            return _activity.Value;
        }
        set => _activity = value;
    }

    public bool IsActive(DayPhase phase) => Activity switch
    {
        ActivityPattern.Diurnal => phase is DayPhase.Day or DayPhase.Dawn or DayPhase.Dusk,
        ActivityPattern.Nocturnal => phase is DayPhase.Night or DayPhase.Dusk or DayPhase.Dawn,
        ActivityPattern.Crepuscular => phase is DayPhase.Dawn or DayPhase.Dusk,
        _ => true
    };

    public float TemperaturePreference => Genome.ColdAdaptation > 0.5f ? 10f : Genome.DesertAdaptation > 0.5f ? 35f : 22f;

    private static ActivityPattern GetDefaultActivity(string species) => species switch
    {
        "Owl" or "Bat" or "Badger" or "Fox" or "Raccoon" or "Wolf" => ActivityPattern.Nocturnal,
        "Deer" or "Rabbit" or "Boar" => ActivityPattern.Crepuscular,
        _ => ActivityPattern.Diurnal
    };
    public string Species { get; set; } = "";
    public Gender Gender { get; set; } = Gender.None;
    public LineageRecord Lineage { get; private set; } = LineageRecord.Founder();
    public float InbreedingCoefficient { get; private set; }
    public float GeneticFitness => MathHelper.Clamp(1f - InbreedingCoefficient * 0.5f, 0.5f, 1f);
    public float MaturityAge => SpeciesRegistry.Get(Species)?.MaturityAge ?? 30f;
    public LifeStage LifeStage => Age >= MaturityAge ? LifeStage.Adult : LifeStage.Infant;
    public bool IsAdult => LifeStage == LifeStage.Adult;
    public bool IsBaby => LifeStage == LifeStage.Infant;
    public float MaxEnergy => 50f * Genome.Size * GeneticFitness;
    public float EnergyConsumption => Genome.Metabolism * 0.5f * Genome.Size;
    public float CurrentSpeedMultiplier { get; protected set; } = 1f;
    public float CurrentEnergyMultiplier { get; protected set; } = 1f;
    public float Speed => Genome.Speed * 30f * CurrentSpeedMultiplier * GeneticFitness * (IsBaby ? 0.5f : 1f);
    public float VisionPixels => Genome.VisionRange * 32f * (IsBaby ? 0.5f : 1f);
    public float ReproductionThreshold => MaxEnergy * 0.7f;
    public virtual bool IsAquatic => false;

    public Vector2 Facing { get; set; } = new(0, 1);
    public Vector2? Waypoint { get; set; }
    public ICreatureBehavior Behavior { get; set; } = new BaseBehavior();
    public Creature? Parent { get; set; }
    public Vector2 HomePosition { get; set; }
    public float Thirst { get; set; }
    private const float MaxThirst = 100f;
    private const float WaypointReachedDistance = 14f;

    protected Creature(Vector2 position, Genome genome, CreatureType type)
    {
        Position = ClampToWorld(position);
        HomePosition = Position;
        Genome = genome;
        Energy = MaxEnergy * 0.5f;
        CreatureType = type;
        Diet = type switch
        {
            CreatureType.Plant => DietType.Herbivore,
            CreatureType.Herbivore => DietType.Herbivore,
            CreatureType.Carnivore => DietType.Carnivore,
            CreatureType.Omnivore => DietType.Omnivore,
            _ => DietType.Omnivore
        };
    }

    public virtual void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0 || dt > 1f) return;
        Age += dt;

        UpdateEnvironmentalMultipliers(world, ecosystem);
        Position = ClampToWorld(Position, world);

        bool active = IsActive(ecosystem.CurrentDayPhase) || CreatureType == CreatureType.Plant;
        IsSleeping = !active && CreatureType != CreatureType.Plant;

        if (active)
        {
            Behavior.Update(this, world, ecosystem, gameTime);
            if (!IsAlive) return;
        }
        else
        {
            dt *= 0.3f;
        }

        ApplyClimateAndPopulationPressure(ecosystem);
        ConsumeEnergy(dt);

        float thirstRate = 2f + CurrentEnergyMultiplier * 4f;
        if (CreatureType == CreatureType.Plant) thirstRate = 0f;
        Thirst = Math.Min(MaxThirst, Thirst + thirstRate * dt);
        if (Thirst >= MaxThirst * 0.9f)
            Energy -= EnergyConsumption * 3f * dt;

        if (Energy <= 0 || Age > 300f)
        {
            Die(Energy <= 0 ? DeathCause.Starvation : DeathCause.OldAge);
            return;
        }

        TryReproduce(ecosystem, dt);
    }

    private void TryReproduce(Ecosystem ecosystem, float dt)
    {
        if (!IsAdult) return;
        if (Energy < ReproductionThreshold) return;
        if (CreatureType == CreatureType.Plant) return;

        float timeSinceLastReproduction = ecosystem.TotalTime - LastReproductionTime;
        if (timeSinceLastReproduction < ReproductionCooldown) return;

        int sameSpeciesCount = 0;
        ecosystem.Metrics.SpeciesPopulations.TryGetValue(Species, out sameSpeciesCount);
        int totalAnimals = ecosystem.HerbivoreCount + ecosystem.CarnivoreCount + ecosystem.OmnivoreCount;
        if (totalAnimals > 10 && sameSpeciesCount > totalAnimals / 3 && ecosystem.Random.NextDouble() > 0.3f)
            return;

        var mate = ecosystem.FindNearestMate(this);
        if (mate != null && DistanceTo(mate) < VisionPixels * 0.5f)
        {
            if (Gender == Gender.Male)
            {
                var rivals = ecosystem.FindNeighbors(this, VisionPixels * 0.3f,
                    c => c != this && c.Species == Species && c.Gender == Gender.Male && c.IsAdult);
                foreach (var rival in rivals)
                {
                    if (rival.AttackPower * rival.Genome.Aggression > AttackPower * Genome.Aggression)
                        return;
                }
            }

            if (timeSinceLastReproduction < ReproductionCooldown ||
                ecosystem.TotalTime - mate.LastReproductionTime < mate.ReproductionCooldown)
                return;

            int litter = Math.Min(LitterSize, mate.LitterSize);
            for (int i = 0; i < litter && IsAlive && mate.IsAlive; i++)
            {
                var child = ReproduceWith(mate, ecosystem.Random);
                if (child != null)
                {
                    ecosystem.AddCreature(child);
                    ecosystem.Metrics.RecordBirth();
                }
            }
            LastReproductionTime = ecosystem.TotalTime;
            mate.LastReproductionTime = ecosystem.TotalTime;
        }
    }

    internal void GrowFor(float seconds) => Age += seconds;

    protected virtual void ConsumeEnergy(float dt)
    {
        Energy -= EnergyConsumption * CurrentEnergyMultiplier * dt;
    }

    internal void ApplyClimateAndPopulationPressure(Ecosystem ecosystem)
    {
        if (CreatureType == CreatureType.Plant) return;
        float seasonalFactor = ecosystem.Climate.EnergyModifier;
        float pressureFactor = ecosystem.PopulationPressure;
        float o2Factor = 2f - ecosystem.Atmosphere.OxygenModifier;
        float altitude = ecosystem.World.GetElevation(Position.X, Position.Y);
        float altitudeFactor = altitude > 0.6f ? (altitude - 0.6f) * 3f : 0f;
        Energy -= EnergyConsumption * (seasonalFactor - 1f + (pressureFactor - 1f) * 0.5f + o2Factor * 0.3f + altitudeFactor) * (1f / 60f);
    }

    private void UpdateEnvironmentalMultipliers(World world, Ecosystem ecosystem)
    {
        var tile = world.GetTileAtPosition(Position.X, Position.Y);
        if (tile == null)
        {
            CurrentSpeedMultiplier = 1f;
            CurrentEnergyMultiplier = 1f;
            return;
        }

        if (IsAquatic)
        {
            bool inWater = tile.Biome is BiomeType.DeepOcean or BiomeType.ShallowWater or BiomeType.CoralReef;
            if (inWater)
            {
                CurrentSpeedMultiplier = 1.0f;
                CurrentEnergyMultiplier = 1.0f;
            }
            else
            {
                CurrentSpeedMultiplier = 0.3f;
                CurrentEnergyMultiplier = 2.5f;
            }
            return;
        }

        // Terrestrial creature
        switch (tile.Biome)
        {
            case BiomeType.Desert:
            case BiomeType.Savanna:
            case BiomeType.Beach:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.DesertAdaptation) * 1.0f;
                CurrentSpeedMultiplier = 0.6f + Genome.DesertAdaptation * 0.4f;
                break;

            case BiomeType.Tundra:
            case BiomeType.Snow:
            case BiomeType.Mountain:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.ColdAdaptation) * 1.5f;
                CurrentSpeedMultiplier = 0.5f + Genome.ColdAdaptation * 0.5f;
                break;

            case BiomeType.Forest:
            case BiomeType.DenseForest:
            case BiomeType.Swamp:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.ForestAdaptation) * 0.3f;
                CurrentSpeedMultiplier = 0.6f + Genome.ForestAdaptation * 0.4f;
                break;

            case BiomeType.DeepOcean:
            case BiomeType.ShallowWater:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.WaterAdaptation) * 0.8f;
                CurrentSpeedMultiplier = 0.4f + Genome.WaterAdaptation * 0.6f;
                break;

            default: // Grassland or others with no penalty
                CurrentSpeedMultiplier = 1.0f;
                CurrentEnergyMultiplier = 1.0f;
                break;
        }

        int tileY = (int)(Position.Y / ecosystem.World.TileSize);
        float tileTemp = ecosystem.Climate.GetTileTemperature(tile, tileY, ecosystem.World.Height);
        float tempDiff = Math.Abs(tileTemp - TemperaturePreference);
        if (tempDiff > 15f && CreatureType != CreatureType.Plant)
            CurrentEnergyMultiplier += tempDiff * 0.02f;
    }

    public virtual void Die(DeathCause cause = DeathCause.Unknown)
    {
        IsAlive = false;
        DeathCause = cause;
        Logger.Event("DEATH", $"{Species} died at age {Age:F1}s, energy {Energy:F1}, cause={cause}");
        Logger.Debug($"DEATH_DETAIL: {Species} age={Age:F1} energy={Energy:F1} pos=({Position.X:F0},{Position.Y:F0}) cause={cause} thirst={Thirst:F0} infected={IsInfected}");
    }

    public void Wander(World world, float dt, Random random, float radius)
    {
        DecayMemories(random);

        if (Waypoint == null || Vector2.Distance(Position, Waypoint.Value) < WaypointReachedDistance)
        {
            float distFromHome = Vector2.Distance(Position, HomePosition);
            if (distFromHome > radius * 3f)
            {
                Waypoint = HomePosition;
            }
            else if (RememberedFood.Count > 0 && random.NextDouble() < Genome.Intelligence * 0.5f)
            {
                Waypoint = RememberedFood[random.Next(RememberedFood.Count)];
            }
            else if (RememberedDanger.Count > 0 && random.NextDouble() < 0.15f)
            {
                var danger = RememberedDanger[random.Next(RememberedDanger.Count)];
                Waypoint = new Vector2(Position.X + (Position.X - danger.X), Position.Y + (Position.Y - danger.Y));
            }
            else
            {
                Waypoint = PickWaypoint(world, random, radius);
            }
        }

        if (CreatureType != CreatureType.Plant)
        {
            var homeTile = world.GetTileAtPosition(HomePosition.X, HomePosition.Y);
            if (homeTile.GrassAmount < 0.05f && random.NextDouble() < 0.0005f)
                HomePosition = Position;
        }

        // Genetic drift: small populations get random gene fluctuations
        if (random.NextDouble() < 0.0001f)
        {
            Genome.ApplyGeneticDrift(random);
        }

        if (Waypoint.HasValue)
            MoveToward(Waypoint.Value, dt, world);
    }

    protected Vector2 PickWaypoint(World world, Random random, float radius)
    {
        float minDist = radius * 0.35f;
        Vector2 target;
        int attempts = 0;
        do
        {
            float rx = (float)(random.NextDouble() - 0.5) * radius * 2;
            float ry = (float)(random.NextDouble() - 0.5) * radius * 2;
            target = Position + new Vector2(rx, ry);
            target.X = Math.Clamp(target.X, 1, world.PixelWidth - 1);
            target.Y = Math.Clamp(target.Y, 1, world.PixelHeight - 1);
            attempts++;
        } while (Vector2.Distance(Position, target) < minDist && attempts < 10);
        return target;
    }

    public void MoveToward(Vector2 target, float dt, World? world = null)
    {
        Vector2 dir = target - Position;
        float dist = dir.Length();
        if (dist < 1f) return;
        if (dist > 0.001f) dir /= dist;
        float moveAmount = Speed * dt;
        Vector2 newPos = ClampToWorld(Position + dir * Math.Min(moveAmount, dist), world);
        if (world != null && !world.GetTileAtPosition(newPos.X, newPos.Y).IsPassableFor(IsAquatic))
            return;
        Position = newPos;
        Facing = dir;
    }

    public bool MoveAwayFrom(Vector2 threat, float dt, World? world = null)
    {
        Vector2 dir = Position - threat;
        float dist = dir.Length();
        if (dist < 1f) return false;
        dir /= dist;
        Vector2 newPos = ClampToWorld(Position + dir * Speed * dt, world);
        if (world != null && !world.GetTileAtPosition(newPos.X, newPos.Y).IsPassableFor(IsAquatic))
            return false;
        Position = newPos;
        Facing = dir;
        return true;
    }

    public float DistanceTo(Creature other)
    {
        if (other == null) return float.MaxValue;
        return Vector2.Distance(Position, other.Position);
    }

    public Creature? ReproduceWith(Creature partner, Random rng)
    {
        if (!CanMateWith(partner)) return null;
        if (Energy < ReproductionThreshold || partner.Energy < partner.ReproductionThreshold)
            return null;
        Energy -= MaxEnergy * 0.3f;
        partner.Energy -= partner.MaxEnergy * 0.3f;
        Genome childGenome = Genome.Reproduce(Genome, partner.Genome, rng);
        Vector2 offset = new((float)(rng.NextDouble() - 0.5) * 30, (float)(rng.NextDouble() - 0.5) * 30);
        var child = CreateChild(ClampToWorld(Position + offset), childGenome, rng);
        if (child != null)
        {
            child.Lineage = LineageRecord.CreateChild(Lineage, partner.Lineage);
            child.InbreedingCoefficient = LineageRecord.CalculateOffspringInbreeding(Lineage, partner.Lineage);
            child.Energy = child.MaxEnergy * 0.5f;
            child.Parent = Gender == Gender.Female ? this : partner;
            if (child.CreatureType != CreatureType.Plant)
            {
                child.Gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
            }
            Logger.Event("BIRTH", $"{Species} + {partner.Species} -> baby at ({child.Position.X:F0},{child.Position.Y:F0})");
        }
        return child;
    }

    public bool CanMateWith(Creature? partner)
    {
        if (!IsAlive || partner == null || !partner.IsAlive) return false;
        if (!IsAdult || !partner.IsAdult) return false;
        if (!string.Equals(Species, partner.Species, StringComparison.Ordinal)) return false;
        if (Gender is not (Gender.Male or Gender.Female)) return false;
        if (partner.Gender is not (Gender.Male or Gender.Female)) return false;
        return Gender != partner.Gender;
    }

    public bool CanMateWithSubspecies(Creature? partner)
    {
        if (!CanMateWith(partner) || partner == null) return false;
        if (string.IsNullOrEmpty(Subspecies) || string.IsNullOrEmpty(partner.Subspecies))
            return true;
        return true;
    }

    public float RelatednessTo(Creature? other) =>
        other is null ? 0f : LineageRecord.CalculateRelatedness(Lineage, other.Lineage);

    internal void AssignIndividualId(ulong individualId)
    {
        if (individualId == 0)
            throw new ArgumentOutOfRangeException(nameof(individualId));
        Lineage = Lineage.WithIndividualId(individualId);
    }

    internal void RestoreGeneticHistory(LineageRecord lineage, float inbreedingCoefficient)
    {
        Lineage = lineage;
        InbreedingCoefficient = MathHelper.Clamp(inbreedingCoefficient, 0f, 1f);
    }

    internal void InheritAsexualLineage(Creature parent)
    {
        Lineage = LineageRecord.CreateAsexualChild(parent.Lineage);
        InbreedingCoefficient = parent.InbreedingCoefficient;
    }

    public Creature? FindNearestSameSpecies(Ecosystem ecosystem)
    {
        return ecosystem.FindNearestSameSpecies(this);
    }

    public bool IsInRange(Creature other, float range)
    {
        if (other == null) return false;
        return Vector2.DistanceSquared(Position, other.Position) <= range * range;
    }

    protected static Vector2 ClampToWorld(Vector2 pos, World? world = null)
    {
        if (world == null) return pos;
        return new Vector2(
            ((pos.X % world.PixelWidth) + world.PixelWidth) % world.PixelWidth,
            ((pos.Y % world.PixelHeight) + world.PixelHeight) % world.PixelHeight);
    }

    protected abstract Creature CreateChild(Vector2 position, Genome genome, Random rng);
}
