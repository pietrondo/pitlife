using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Carnivore : Creature
{
    public float AttackDamage => 20f * Genome.Size;

    public Carnivore(Vector2 position, Genome genome, string species = "Carnivore")
        : base(position, genome, CreatureType.Carnivore) { Species = species; }
    public override bool IsAquatic => Species is "Shark" or "Piranha";

    public override void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        base.Update(world, ecosystem, gameTime);
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Creature? prey = ecosystem.FindNearestPrey(this);
        if (prey != null && DistanceTo(prey) < VisionPixels)
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
                MoveToward(prey.Position, dt, world);
            }
        }
        else
        {
            Wander(world, dt);
        }
    }

    private void Wander(World world, float dt)
    {
        float rx = (float)(new Random().NextDouble() - 0.5) * 80f;
        float ry = (float)(new Random().NextDouble() - 0.5) * 80f;
        var target = Position + new Vector2(rx, ry);
        target.X = Math.Clamp(target.X, 0, world.PixelWidth - 1);
        target.Y = Math.Clamp(target.Y, 0, world.PixelHeight - 1);
        MoveToward(target, dt, world);
    }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new Carnivore(position, genome, Species);
    }
}
