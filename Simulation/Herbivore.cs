using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Herbivore : Creature
{
    public Herbivore(Vector2 position, Genome genome, string species = "Herbivore")
        : base(position, genome, CreatureType.Herbivore) { Species = species; Behavior = new BaseBehavior(); }
    public override bool IsAquatic => Species is "Fish" or "Salmon";

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        string evolvedSpecies = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, Species, rng);
        return new Herbivore(position, genome, evolvedSpecies);
    }
}
