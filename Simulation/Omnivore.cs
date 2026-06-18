using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Omnivore : Creature
{
    public float AttackDamage => 12f * Genome.Size;

    public Omnivore(Vector2 position, Genome genome, string species = "Omnivore")
        : base(position, genome, CreatureType.Omnivore) { Species = species; }

    public override void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        base.Update(world, ecosystem, gameTime);
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Creature? threat = ecosystem.FindNearestPredator(this);
        if (threat != null && DistanceTo(threat) < VisionPixels * 0.6f)
        {
            MoveAwayFrom(threat.Position, dt);
            return;
        }

        bool isHungry = Energy < MaxEnergy * 0.4f;
        if (isHungry || Energy < ReproductionThreshold * 0.8f)
        {
            Creature? prey = ecosystem.FindNearestPrey(this);
            if (prey != null && DistanceTo(prey) < VisionPixels && Random.Shared.NextDouble() < 0.4)
            {
                if (DistanceTo(prey) < 10f)
                {
                    float damage = AttackDamage * dt;
                    prey.Energy -= damage;
                    Energy = Math.Min(Energy + damage * 1.5f, MaxEnergy);
                    if (prey.Energy <= 0) prey.Die();
                }
                else
                {
                    MoveToward(prey.Position, dt);
                }
                return;
            }
        }

        Plant? food = ecosystem.FindNearestPlantFor(this);
        if (food != null)
        {
            if (DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 8f * dt);
                food.Energy -= eaten;
                Energy = Math.Min(Energy + eaten * 1.5f, MaxEnergy);
                if (food.Energy <= 0) food.Die();
            }
            else
            {
                MoveToward(food.Position, dt);
            }
        }
        else
        {
            float rx = (float)(Random.Shared.NextDouble() - 0.5) * 70f;
            float ry = (float)(Random.Shared.NextDouble() - 0.5) * 70f;
            var target = Position + new Vector2(rx, ry);
            target.X = Math.Clamp(target.X, 0, world.PixelWidth - 1);
            target.Y = Math.Clamp(target.Y, 0, world.PixelHeight - 1);
            MoveToward(target, dt);
        }
    }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new Omnivore(position, genome, Species);
    }
}
