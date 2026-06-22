using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Plant : Creature
{
    public float GrowthRate { get; }

    public Plant(Vector2 position, Genome genome, string species = "Clover")
        : base(position, genome, CreatureType.Plant)
    {
        Species = species;
        GrowthRate = 2f + genome.Metabolism * 2f;
        Behavior = new PlantBehavior();
    }

    protected override void ConsumeEnergy(float dt) { }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new Plant(position, genome, Species);
    }
}
