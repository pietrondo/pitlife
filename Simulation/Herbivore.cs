using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Herbivore : Creature
{
    public Herbivore(Vector2 position, Genome genome, string species = "Herbivore")
        : base(position, genome, CreatureType.Herbivore) { Species = species; }
    public override bool IsAquatic => Species is "Fish" or "Salmon";

    public override void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        base.Update(world, ecosystem, gameTime);
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Creature? threat = ecosystem.FindNearestPredator(this);
        if (threat != null && DistanceTo(threat) < VisionPixels * 0.8f)
        {
            MoveAwayFrom(threat.Position, dt, world);
            return;
        }

        Plant? food = ecosystem.FindNearestPlant(this);
        if (food != null)
        {
            if (DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 10f * dt);
                food.Energy -= eaten;
                Energy = Math.Min(Energy + eaten * 2f, MaxEnergy);
                if (food.Energy <= 0) food.Die();
            }
            else
            {
                MoveToward(food.Position, dt, world);
            }
        }
        else
        {
            Wander(world, dt);
        }
    }

    private void Wander(World world, float dt)
    {
        float rx = (float)(new Random().NextDouble() - 0.5) * 60f;
        float ry = (float)(new Random().NextDouble() - 0.5) * 60f;
        var target = Position + new Vector2(rx, ry);
        target.X = Math.Clamp(target.X, 0, world.PixelWidth - 1);
        target.Y = Math.Clamp(target.Y, 0, world.PixelHeight - 1);
        MoveToward(target, dt, world);
    }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new Herbivore(position, genome, Species);
    }
}
