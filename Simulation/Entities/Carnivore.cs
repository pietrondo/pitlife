using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Carnivore : Creature
{
    public float AttackDamage => 20f * Genome.Size;

    public Carnivore(Vector2 position, Genome genome, string species = "Carnivore")
        : base(position, genome, CreatureType.Carnivore) { Species = species; }
    public override bool IsAquatic => Species is "Shark" or "Piranha";

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        var evolvedSpecies = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, Species, rng);
        return new Carnivore(position, genome, evolvedSpecies);
    }
}
