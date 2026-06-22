using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public struct Genome
{
    public float Speed;
    public float Size;
    public float Metabolism;
    public float VisionRange;
    public Color Color;
    public float MutationRate;
    public float DesertAdaptation;
    public float ColdAdaptation;
    public float ForestAdaptation;
    public float WaterAdaptation;
    public GeneticProfile Genetics;

    public float Heterozygosity => EnsureGeneticProfile().Heterozygosity;

    public static Genome Random(Random rng)
    {
        var genome = new Genome();
        GeneticProfile profile = GeneticProfile.Random(rng);
        profile.ApplyPhenotype(ref genome);
        return genome;
    }

    public static Genome Reproduce(Genome parent1, Genome parent2, Random rng)
    {
        GeneticProfile profile = GeneticProfile.Recombine(
            parent1.EnsureGeneticProfile(),
            parent2.EnsureGeneticProfile(),
            rng);
        var child = new Genome();
        profile.ApplyPhenotype(ref child);
        return child;
    }

    public GeneticProfile EnsureGeneticProfile() =>
        Genetics.IsInitialized ? Genetics : GeneticProfile.FromPhenotype(this);
}
