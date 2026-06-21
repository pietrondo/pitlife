using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public abstract class Creature
{
    public Vector2 Position { get; set; }
    public Genome Genome { get; protected set; }
    public float Energy { get; set; }
    public float Age { get; protected set; }
    public bool IsAlive { get; protected set; } = true;
    public CreatureType CreatureType { get; protected set; }
    public string Species { get; set; } = "";
    public Gender Gender { get; set; } = Gender.None;
    public float MaturityAge => SpeciesRegistry.Get(Species)?.MaturityAge ?? 30f;
    public LifeStage LifeStage => Age >= MaturityAge ? LifeStage.Adult : LifeStage.Infant;
    public bool IsAdult => LifeStage == LifeStage.Adult;
    public bool IsBaby => LifeStage == LifeStage.Infant;
    public float MaxEnergy => 50f * Genome.Size;
    public float EnergyConsumption => Genome.Metabolism * 0.5f * Genome.Size;
    public float CurrentSpeedMultiplier { get; protected set; } = 1f;
    public float CurrentEnergyMultiplier { get; protected set; } = 1f;
    public float Speed => Genome.Speed * 30f * CurrentSpeedMultiplier;
    public float VisionPixels => Genome.VisionRange * 32f;
    public float ReproductionThreshold => MaxEnergy * 0.7f;
    public virtual bool IsAquatic => false;

    public Vector2 Facing { get; set; } = new(0, 1);
    public Vector2? Waypoint { get; set; }
    public ICreatureBehavior Behavior { get; set; } = new BaseBehavior();
    private const float WaypointReachedDistance = 8f;

    protected Creature(Vector2 position, Genome genome, CreatureType type)
    {
        Position = ClampToWorld(position);
        Genome = genome;
        Energy = MaxEnergy * 0.5f;
        CreatureType = type;
    }

    public virtual void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0 || dt > 1f) return;
        Age += dt;

        UpdateEnvironmentalMultipliers(world);

        ConsumeEnergy(dt);
        if (Energy <= 0 || Age > 300f)
        {
            Die();
            return;
        }
        Position = ClampToWorld(Position, world);

        Behavior.Update(this, world, ecosystem, gameTime);

        if (IsAlive)
            TryReproduce(ecosystem, dt);
    }

    private void TryReproduce(Ecosystem ecosystem, float dt)
    {
        if (!IsAdult) return;
        if (Energy < ReproductionThreshold) return;
        if (CreatureType == CreatureType.Plant) return;

        var mate = ecosystem.FindNearestMate(this);
        if (mate != null && DistanceTo(mate) < VisionPixels * 0.5f)
        {
            var child = ReproduceWith(mate, ecosystem.Random);
            if (child != null) ecosystem.AddCreature(child);
        }
    }

    internal void GrowFor(float seconds) => Age += seconds;

    protected virtual void ConsumeEnergy(float dt)
    {
        Energy -= EnergyConsumption * CurrentEnergyMultiplier * dt;
    }

    private void UpdateEnvironmentalMultipliers(World world)
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
            bool inWater = tile.Biome is BiomeType.DeepOcean or BiomeType.ShallowWater;
            if (inWater)
            {
                CurrentSpeedMultiplier = 1.0f;
                CurrentEnergyMultiplier = 1.0f;
            }
            else
            {
                CurrentSpeedMultiplier = 0.2f;
                CurrentEnergyMultiplier = 3.0f;
            }
            return;
        }

        // Terrestrial creature
        switch (tile.Biome)
        {
            case BiomeType.Desert:
            case BiomeType.Savanna:
            case BiomeType.Beach:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.DesertAdaptation) * 1.5f;
                CurrentSpeedMultiplier = 0.5f + Genome.DesertAdaptation * 0.5f;
                break;

            case BiomeType.Tundra:
            case BiomeType.Snow:
            case BiomeType.Mountain:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.ColdAdaptation) * 2.0f;
                CurrentSpeedMultiplier = 0.4f + Genome.ColdAdaptation * 0.6f;
                break;

            case BiomeType.Forest:
            case BiomeType.DenseForest:
            case BiomeType.Swamp:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.ForestAdaptation) * 0.5f;
                CurrentSpeedMultiplier = 0.5f + Genome.ForestAdaptation * 0.5f;
                break;

            case BiomeType.DeepOcean:
            case BiomeType.ShallowWater:
                CurrentEnergyMultiplier = 1.0f + (1.0f - Genome.WaterAdaptation) * 1.0f;
                CurrentSpeedMultiplier = 0.3f + Genome.WaterAdaptation * 0.7f;
                break;

            default: // Grassland or others with no penalty
                CurrentSpeedMultiplier = 1.0f;
                CurrentEnergyMultiplier = 1.0f;
                break;
        }
    }

    public virtual void Die()
    {
        IsAlive = false;
        Logger.Event("DEATH", $"{Species} died at age {Age:F1}s, energy {Energy:F1}");
    }

    public void Wander(World world, float dt, Random random, float radius)
    {
        if (Waypoint == null || Vector2.Distance(Position, Waypoint.Value) < WaypointReachedDistance)
            Waypoint = PickWaypoint(world, random, radius);

        if (Waypoint.HasValue)
            MoveToward(Waypoint.Value, dt, world);
    }

    protected Vector2 PickWaypoint(World world, Random random, float radius)
    {
        float rx = (float)(random.NextDouble() - 0.5) * radius * 2;
        float ry = (float)(random.NextDouble() - 0.5) * radius * 2;
        var target = Position + new Vector2(rx, ry);
        target.X = Math.Clamp(target.X, 1, world.PixelWidth - 1);
        target.Y = Math.Clamp(target.Y, 1, world.PixelHeight - 1);
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
        if (!IsAlive || partner == null || !partner.IsAlive) return null;
        if (!IsAdult || !partner.IsAdult) return null;
        if (Gender == partner.Gender) return null;
        if (Energy < ReproductionThreshold || partner.Energy < partner.ReproductionThreshold)
            return null;
        Energy -= MaxEnergy * 0.3f;
        partner.Energy -= partner.MaxEnergy * 0.3f;
        Genome childGenome = Genome.Reproduce(Genome, partner.Genome, rng);
        Vector2 offset = new((float)(rng.NextDouble() - 0.5) * 30, (float)(rng.NextDouble() - 0.5) * 30);
        var child = CreateChild(ClampToWorld(Position + offset), childGenome, rng);
        if (child != null)
        {
            if (child.CreatureType != CreatureType.Plant)
            {
                child.Gender = rng.Next(2) == 0 ? Gender.Male : Gender.Female;
            }
            Logger.Event("BIRTH", $"{Species} + {partner.Species} -> baby at ({child.Position.X:F0},{child.Position.Y:F0})");
        }
        return child;
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
        float maxX = world?.PixelWidth - 1 ?? float.MaxValue;
        float maxY = world?.PixelHeight - 1 ?? float.MaxValue;
        return new(Math.Clamp(pos.X, 1, maxX), Math.Clamp(pos.Y, 1, maxY));
    }

    protected abstract Creature CreateChild(Vector2 position, Genome genome, Random rng);
}
