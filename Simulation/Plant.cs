using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Plant : Creature
{
    public float GrowthRate { get; private set; }

    public Plant(Vector2 position, Genome genome, string species = "Plant")
        : base(position, genome, CreatureType.Plant)
    {
        Species = species;
        GrowthRate = 2f + genome.Metabolism * 2f;
    }

    public override void Update(World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        Age += dt;

        var tile = world.GetTileAtPosition(Position.X, Position.Y);
        float sunlight = tile.Vegetation * 0.5f + 0.5f;
        float energyGain = GrowthRate * sunlight * dt;
        Energy = Math.Min(Energy + energyGain, MaxEnergy);

        if (Energy >= ReproductionThreshold)
        {
            ecosystem.TrySpreadPlant(this);
        }

        if (Age > 120f) Die();
    }

    protected override void ConsumeEnergy(float dt) { }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new Plant(position, genome, Species);
    }
}
