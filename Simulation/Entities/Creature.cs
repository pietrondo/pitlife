using System;
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
    public bool Hibernating;
    public bool IsSleeping { get; protected set; }
    public string Species { get; set; } = "";
    public string Subspecies { get; set; } = "";
    public LineageRecord Lineage { get; protected set; } = LineageRecord.Founder();
    public float InbreedingCoefficient { get; protected set; }
    public ICreatureBehavior Behavior { get; set; } = new BaseBehavior();
    public Creature? Parent { get; set; }
    public Gender Gender { get; set; } = Gender.None;

    public Motor Motor;
    public MemoryStore Memory;
    public ReproductionData Reproduction;
    public EnvironmentResponse EnvironmentState;

    public float NutritionalValue => Genome.Size * BalanceConfig.Data.Creature.NutritionalValueSizeMultiplier * (1f - Toxicity * BalanceConfig.Data.Creature.NutritionalValueToxicityRatio);
    public float MaxEnergy => BalanceConfig.Data.Creature.MaxEnergyBaseMultiplier * Genome.Size * GeneticFitness;
    public float EnergyConsumption => Genome.Metabolism * BalanceConfig.Data.Creature.EnergyConsumptionBaseMultiplier * Genome.Size;
    public float Defense => Genome.Size * BalanceConfig.Data.Creature.DefenseSizeMultiplier + Genome.Metabolism * BalanceConfig.Data.Creature.DefenseMetabolismMultiplier;
    public float AttackPower => Motor.GetSpeed(Genome, GeneticFitness, IsBaby) * BalanceConfig.Data.Creature.AttackSpeedMultiplier + Genome.Size * BalanceConfig.Data.Creature.AttackSizeMultiplier;
    public float TemperaturePreference => Genome.ColdAdaptation > 0.5f ? Core.CreatureConfig.Data.ColdPrefTemp : Genome.DesertAdaptation > 0.5f ? Core.CreatureConfig.Data.HotPrefTemp : Core.CreatureConfig.Data.NeutralPrefTemp;
    public float MaturityAge => SpeciesRegistry.Get(Species)?.MaturityAge ?? 30f;
    public LifeStage LifeStage => Age >= MaturityAge ? LifeStage.Adult : LifeStage.Infant;
    public bool IsAdult => LifeStage == LifeStage.Adult;
    public bool IsBaby => LifeStage == LifeStage.Infant;
    public float GeneticFitness => MathHelper.Clamp(1f - InbreedingCoefficient * BalanceConfig.Data.Inbreeding.CoefficientImpact, BalanceConfig.Data.Inbreeding.MinFitness, 1f);
    public virtual bool IsAquatic => false;

    // Delegate properties
    public Vector2 Facing { get => Motor.Facing; set => Motor.Facing = value; }
    public Vector2 HomePosition { get => Motor.HomePosition; set => Motor.HomePosition = value; }
    public Vector2? Waypoint { get => Motor.Waypoint; set => Motor.Waypoint = value; }
    public float Speed => Motor.GetSpeed(Genome, GeneticFitness, IsBaby);
    public float VisionPixels => Motor.GetVisionPixels(Genome, IsBaby);
    public float CurrentSpeedMultiplier => Motor.CurrentSpeedMultiplier;
    public float CurrentEnergyMultiplier => Motor.CurrentEnergyMultiplier;
    public float Thirst { get => EnvironmentState.Thirst; set => EnvironmentState.Thirst = value; }
    public float LastReproductionTime { get => Reproduction.LastReproductionTime; set => Reproduction.LastReproductionTime = value; }
    public int LitterSize => Reproduction.GetLitterSize(Genome);
    public float ReproductionCooldown => Reproduction.GetReproductionCooldown(Genome);
    public float ReproductionThreshold => Reproduction.GetReproductionThreshold(MaxEnergy);

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

    private static ActivityPattern GetDefaultActivity(string species) => species switch
    {
        "Owl" or "Bat" or "Badger" or "Fox" or "Raccoon" or "Wolf" => ActivityPattern.Nocturnal,
        "Deer" or "Rabbit" or "Boar" => ActivityPattern.Crepuscular,
        _ => ActivityPattern.Diurnal
    };

    protected Creature(Vector2 position, Genome genome, CreatureType type)
    {
        Position = Motor.ClampToWorld(position);
        Genome = genome;
        CreatureType = type;
        Diet = type switch
        {
            CreatureType.Plant => DietType.Herbivore,
            CreatureType.Herbivore => DietType.Herbivore,
            CreatureType.Carnivore => DietType.Carnivore,
            CreatureType.Omnivore => DietType.Omnivore,
            _ => DietType.Omnivore
        };
        Motor = new Motor();
        Motor.Reset(Position);
        if (type != CreatureType.Plant)
        {
            Memory = new MemoryStore();
        }
        Reproduction = new ReproductionData();
        Reproduction.Reset();
        EnvironmentState = new EnvironmentResponse();
        EnvironmentState.Reset();
        Energy = MaxEnergy * BalanceConfig.Data.Creature.InitialEnergyRatio;
    }

    internal void ResetForReuse(Vector2 position, Genome genome)
    {
        Position = Motor.ClampToWorld(position);
        Genome = genome;
        Age = 0;
        IsAlive = true;
        DeathCause = DeathCause.Unknown;
        IsInfected = false;
        DiseaseTimer = 0;
        DiseaseName = "";
        IsPoisonous = false;
        Toxicity = 0;
        Hibernating = false;
        IsSleeping = false;

        Motor.Reset(Position);
        if (CreatureType != CreatureType.Plant)
            Memory.Reset();
        Reproduction.Reset();
        EnvironmentState.Reset();

        Energy = MaxEnergy * BalanceConfig.Data.Creature.InitialEnergyRatio;
    }

    public virtual void Update(World world, Ecosystem ecosystem, float dt)
    {
        if (!IsAlive) return;

        if (dt <= 0 || dt > 1f) return;
        Age += dt;

        EnvironmentState.UpdateHibernation(ref Hibernating, Species, CreatureType, Position, ecosystem);
        EnvironmentState.UpdateEnvironmentalMultipliers(ref Motor.CurrentSpeedMultiplier, ref Motor.CurrentEnergyMultiplier, CreatureType, Species, Genome, Position, TemperaturePreference, IsAquatic, world, ecosystem);

        Position = Motor.ClampToWorld(Position, world);

        bool active = IsActive(ecosystem.CurrentDayPhase) || CreatureType == CreatureType.Plant;
        IsSleeping = !active && CreatureType != CreatureType.Plant;

        if (Hibernating)
        {
            dt *= BalanceConfig.Data.Hibernation.TimeMultiplier;
            active = false;
        }

        if (active)
        {
            Behavior.Update(this, world, ecosystem, dt);
            if (!IsAlive) return;
        }
        else
        {
            dt *= BalanceConfig.Data.Sleep.TimeMultiplier;
        }

        float tempEnergy = Energy; EnvironmentState.ApplyClimateAndPopulationPressure(ref tempEnergy, EnergyConsumption, CreatureType, Position, ecosystem); Energy = tempEnergy;
        float scarcity = CreatureType == CreatureType.Plant ? 1f : GrassScarcityPenalty(world);
        ConsumeEnergy(dt * scarcity);

        float thirstRate = BalanceConfig.Data.Thirst.BaseRate + Motor.CurrentEnergyMultiplier * BalanceConfig.Data.Thirst.EnergyMultiplierRate;
        if (CreatureType == CreatureType.Plant) thirstRate = 0f;
        Thirst = Math.Min(BalanceConfig.Data.Thirst.MaxThirst, Thirst + thirstRate * dt);
        if (Thirst >= BalanceConfig.Data.Thirst.MaxThirst * BalanceConfig.Data.Thirst.PenaltyThresholdRatio)
            Energy -= EnergyConsumption * BalanceConfig.Data.Thirst.EnergyDrainMultiplier * dt;

        if (Energy <= 0 || Age > BalanceConfig.Data.Creature.MaxAge)
        {
            Die(Energy <= 0 ? DeathCause.Starvation : DeathCause.OldAge);
            return;
        }

        TryReproduce(ecosystem, dt);
    }

    public void ApplyWindDrift(float windDir, float windSpeed, float dt, World world)
    {
        Vector2 pos = Position;
        EnvironmentState.ApplyWindDrift(ref pos, CreatureType, IsAquatic, windDir, windSpeed, dt, world);
        Position = pos;
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

        float scarcity = GrassScarcityPenalty(ecosystem.World);
        if (scarcity > 1.5f && ecosystem.Random.NextDouble() > 0.3f)
            return;

        // Lotka-Volterra: adjust reproduction probability based on trophic balance
        float trophicBirthBonus = CreatureType switch
        {
            CreatureType.Herbivore => ecosystem.Trophic.HerbivoreBirthBonus,
            CreatureType.Carnivore => ecosystem.Trophic.CarnivoreBirthBonus,
            _ => 1f
        };
        if (trophicBirthBonus < 1f && ecosystem.Random.NextDouble() > trophicBirthBonus)
            return;

        var mate = ecosystem.FindNearestMate(this);
        if (mate == null || DistanceTo(mate) >= VisionPixels * 0.5f) return;

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

    internal void GrowFor(float seconds) => Age += seconds;

    protected virtual void ConsumeEnergy(float dt)
    {
        float cost = EnergyConsumption * Motor.CurrentEnergyMultiplier;
        Energy -= cost * dt;
    }

    public float GrassScarcityPenalty(World world)
    {
        var tile = world.GetTileAtPosition(Position.X, Position.Y);
        float grassRatio = tile.GrassAmount / Math.Max(tile.MaxGrass, 0.001f);
        return grassRatio < Core.CreatureConfig.Data.ScarcityGrassThreshold ? 1f + (Core.CreatureConfig.Data.ScarcityGrassThreshold - grassRatio) * Core.CreatureConfig.Data.ScarcityPenaltyFactor : 1f;
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
        if (CreatureType != CreatureType.Plant)
            Memory.DecayMemories(random, Genome);

        if (random.NextDouble() < Core.CreatureConfig.Data.GeneticDriftChance)
        {
            Genome.ApplyGeneticDrift(random);
        }

        Vector2 pos = Position;
        Motor.Wander(ref pos, Genome, Speed, dt, random, radius, Memory, CreatureType == CreatureType.Plant, world, IsAquatic);
        Position = pos;
    }

    public void MoveToward(Vector2 target, float dt, World? world = null)
    {
        Vector2 pos = Position;
        Motor.MoveToward(ref pos, target, Speed, dt, IsAquatic, world);
        Position = pos;
    }

    public bool MoveAwayFrom(Vector2 threat, float dt, World? world = null)
    {
        Vector2 pos = Position;
        bool moved = Motor.MoveAwayFrom(ref pos, threat, Speed, dt, IsAquatic, world);
        Position = pos;
        return moved;
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
        Energy -= MaxEnergy * Core.CreatureConfig.Data.ReproduceEnergyCostRatio;
        partner.Energy -= partner.MaxEnergy * Core.CreatureConfig.Data.ReproduceEnergyCostRatio;
        Genome childGenome = Genome.Reproduce(Genome, partner.Genome, rng);
        Vector2 offset = new((float)(rng.NextDouble() - 0.5) * Core.CreatureConfig.Data.ChildOffsetRadius, (float)(rng.NextDouble() - 0.5) * Core.CreatureConfig.Data.ChildOffsetRadius);
        var child = CreateChild(Motor.ClampToWorld(Position + offset), childGenome, rng);
        if (child != null)
        {
            child.Lineage = LineageRecord.CreateChild(Lineage, partner.Lineage);
            child.InbreedingCoefficient = LineageRecord.CalculateOffspringInbreeding(Lineage, partner.Lineage);
            child.Energy = child.MaxEnergy * Core.CreatureConfig.Data.ChildInitialEnergyRatio;
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

    public void RememberFood(Vector2 pos) => Memory.RememberFood(pos);
    public void RememberDanger(Vector2 pos) => Memory.RememberDanger(pos);

    protected abstract Creature CreateChild(Vector2 position, Genome genome, Random rng);
}
