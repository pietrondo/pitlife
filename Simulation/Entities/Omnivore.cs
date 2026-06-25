using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Omnivore : Creature
{
    public float AttackDamage => 12f * Genome.Size;

    public Omnivore(Vector2 position, Genome genome, string species = "Omnivore")
        : base(position, genome, CreatureType.Omnivore) { Species = species; }
    public override bool IsAquatic => Species == "Jellyfish";

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        string evolvedSpecies = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, Species, rng);
        return new Omnivore(position, genome, evolvedSpecies);
    }
}
