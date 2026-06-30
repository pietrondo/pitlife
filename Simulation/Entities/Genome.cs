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
    public float MemorySpan;
    public float Aggression;
    public float Sociability;
    public float Intelligence;
    public float PlantRecognition;
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

    public void ApplyGeneticDrift(Random rng)
    {
        var cfg = PitLife.Core.GenomeConfig.Data;
        var drift = (float)(rng.NextDouble() - 0.5) * cfg.TraitDriftAmplitude;
        Speed = MathHelper.Clamp(Speed + drift, cfg.TraitMin, cfg.TraitMax);
        Size = MathHelper.Clamp(Size + drift, cfg.TraitMin, cfg.TraitMax);
        Metabolism = MathHelper.Clamp(Metabolism + drift, cfg.TraitMin, cfg.TraitMax);
        var adaptDrift = (float)(rng.NextDouble() - 0.5) * cfg.AdaptationDriftAmplitude;
        DesertAdaptation = MathHelper.Clamp(DesertAdaptation + adaptDrift, cfg.AdaptationMin, cfg.AdaptationMax);
        ColdAdaptation = MathHelper.Clamp(ColdAdaptation + adaptDrift, cfg.AdaptationMin, cfg.AdaptationMax);
    }
}
