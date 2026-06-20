using System;
using Microsoft.Xna.Framework;

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
    public Gender Gender { get; set; } = Gender.Male;
    public bool IsAdult => Age >= 30f;
    public bool IsBaby => !IsAdult;
    public float MaxEnergy => 50f * Genome.Size;
    public float EnergyConsumption => Genome.Metabolism * 0.5f * Genome.Size;
    public float Speed => Genome.Speed * 30f;
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
        Energy -= EnergyConsumption * dt;
    }

    public virtual void Die()
    {
        IsAlive = false;
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
        if (Energy < ReproductionThreshold || partner.Energy < ReproductionThreshold)
            return null;
        Energy -= MaxEnergy * 0.3f;
        partner.Energy -= partner.MaxEnergy * 0.3f;
        Genome childGenome = Genome.Reproduce(Genome, partner.Genome, rng);
        Vector2 offset = new((float)(rng.NextDouble() - 0.5) * 30, (float)(rng.NextDouble() - 0.5) * 30);
        return CreateChild(ClampToWorld(Position + offset), childGenome, rng);
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
