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
    public float MaxEnergy => 50f * Genome.Size;
    public float EnergyConsumption => Genome.Metabolism * 0.5f * Genome.Size;
    public float Speed => Genome.Speed * 30f;
    public float VisionPixels => Genome.VisionRange * 32f;
    public float ReproductionThreshold => MaxEnergy * 0.7f;

    public Vector2 Facing { get; set; } = new(0, 1);

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
        Position = ClampToWorld(Position);
    }

    protected virtual void ConsumeEnergy(float dt)
    {
        Energy -= EnergyConsumption * dt;
    }

    public virtual void Die()
    {
        IsAlive = false;
    }

    public void MoveToward(Vector2 target, float dt)
    {
        Vector2 dir = target - Position;
        float dist = dir.Length();
        if (dist < 1f) return;
        if (dist > 0.001f) dir /= dist;
        float moveAmount = Speed * dt;
        Position += dir * Math.Min(moveAmount, dist);
        Facing = dir;
    }

    public bool MoveAwayFrom(Vector2 threat, float dt)
    {
        Vector2 dir = Position - threat;
        float dist = dir.Length();
        if (dist < 1f) return false;
        dir /= dist;
        Position += dir * Speed * dt;
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
        if (Energy < ReproductionThreshold || partner.Energy < ReproductionThreshold)
            return null;
        Energy -= MaxEnergy * 0.3f;
        partner.Energy -= partner.MaxEnergy * 0.3f;
        Genome childGenome = Genome.Reproduce(Genome, partner.Genome, rng);
        Vector2 offset = new((float)(rng.NextDouble() - 0.5) * 30, (float)(rng.NextDouble() - 0.5) * 30);
        return CreateChild(ClampToWorld(Position + offset), childGenome, rng);
    }

    public bool IsInRange(Creature other, float range)
    {
        if (other == null) return false;
        return Vector2.DistanceSquared(Position, other.Position) <= range * range;
    }

    protected static Vector2 ClampToWorld(Vector2 pos)
    {
        const float max = 200 * 32f;
        return new(Math.Clamp(pos.X, 1, max - 1), Math.Clamp(pos.Y, 1, max - 1));
    }

    protected abstract Creature CreateChild(Vector2 position, Genome genome, Random rng);
}
