using System;
using System.Numerics;
using Microsoft.Xna.Framework;
using Xunit;
using PitLife.Simulation;

namespace PitLife.Tests.Entities;

public class GeneticsTests
{
    [Fact]
    public void GeneticAllele_Mutate_WithZeroRate_ShouldNotChange()
    {
        var random = new Random(123);
        var allele = new GeneticAllele(1.0f, 0.5f);
        var mutated = allele.Mutate(0.0f, 0.0f, 2.0f, 0.1f, random);

        Assert.Equal(1.0f, mutated.Value);
        Assert.Equal(0.5f, mutated.Dominance);
    }

    [Fact]
    public void GeneticAllele_Mutate_WithHighRate_ShouldClampValues()
    {
        var random = new Random(123);
        var allele = new GeneticAllele(1.0f, 0.5f);
        var mutated = allele.Mutate(1.0f, 1.0f, 1.0f, 0.1f, random);

        Assert.Equal(1.0f, mutated.Value);
        // Dominance bounds are 0.05f to 1f, since step is 0.2f,
        // random dominance diff will be within +/- 0.1f.
        Assert.InRange(mutated.Dominance, 0.05f, 1f);
    }

    [Fact]
    public void DiploidLocus_Homozygous_ShouldHaveEqualAlleles()
    {
        var locus = DiploidLocus.Homozygous(1.5f);
        Assert.Equal(1.5f, locus.AlleleA.Value);
        Assert.Equal(1.5f, locus.AlleleB.Value);
        Assert.Equal(0.5f, locus.AlleleA.Dominance);
        Assert.Equal(0.5f, locus.AlleleB.Dominance);
        Assert.Equal(1.5f, locus.ExpressedValue);
    }

    [Fact]
    public void DiploidLocus_ExpressedValue_WithZeroTotalDominance_ShouldAverage()
    {
        var locus = new DiploidLocus(
            new GeneticAllele(1.0f, 0.0f),
            new GeneticAllele(2.0f, 0.0f)
        );
        Assert.Equal(1.5f, locus.ExpressedValue);
    }

    [Fact]
    public void DiploidLocus_ExpressedValue_WithDominantRecessive_ShouldWeightValues()
    {
        var locus = new DiploidLocus(
            new GeneticAllele(1.0f, 0.8f),
            new GeneticAllele(2.0f, 0.2f)
        );
        // (1.0 * 0.8 + 2.0 * 0.2) / (0.8 + 0.2) = (0.8 + 0.4) / 1.0 = 1.2
        Assert.Equal(1.2f, locus.ExpressedValue, 5);
    }

    [Fact]
    public void DiploidLocus_Random_GeneratesValuesWithinRange()
    {
        var random = new Random(123);
        var locus = DiploidLocus.Random(0.5f, 2.0f, random);

        Assert.InRange(locus.AlleleA.Value, 0.5f, 2.0f);
        Assert.InRange(locus.AlleleB.Value, 0.5f, 2.0f);
        Assert.InRange(locus.AlleleA.Dominance, 0.2f, 1.0f);
        Assert.InRange(locus.AlleleB.Dominance, 0.2f, 1.0f);
    }

    [Fact]
    public void DiploidLocus_Inherit_CreatesChildFromParents()
    {
        var random = new Random(123);
        var p1 = DiploidLocus.Homozygous(1.0f);
        var p2 = DiploidLocus.Homozygous(2.0f);

        var child = DiploidLocus.Inherit(p1, p2, 0.0f, 0.0f, 3.0f, 0.1f, random);

        // With zero mutation rate, alleles should exactly match one allele from each parent.
        Assert.Equal(1.0f, child.AlleleA.Value);
        Assert.Equal(2.0f, child.AlleleB.Value);
    }

    [Fact]
    public void GeneticProfile_Random_InitializesFully()
    {
        var random = new Random(123);
        var profile = GeneticProfile.Random(random);

        Assert.True(profile.IsInitialized);
        Assert.NotEqual(0UL, profile.MarkerHaplotypeA);
        Assert.NotEqual(0UL, profile.MarkerHaplotypeB);
        Assert.Equal(0.05f, profile.MutationRate.ExpressedValue);
        Assert.InRange(profile.Heterozygosity, 0f, 1f);
    }

    [Fact]
    public void GeneticProfile_Heterozygosity_ReturnsZeroWhenUninitialized()
    {
        var profile = new GeneticProfile(); // default struct
        Assert.False(profile.IsInitialized);
        Assert.Equal(0f, profile.Heterozygosity);
    }

    [Fact]
    public void GeneticProfile_FromPhenotype_HashesConsistently()
    {
        var genome = new Genome
        {
            Speed = 1.0f,
            Size = 1.5f,
            Color = new Color(100, 150, 200)
        };

        var profile = GeneticProfile.FromPhenotype(genome);
        Assert.True(profile.IsInitialized);
        Assert.Equal(1.0f, profile.Speed.ExpressedValue);
        Assert.Equal(1.5f, profile.Size.ExpressedValue);
        Assert.Equal(100f, profile.ColorR.ExpressedValue);

        // The haplotypes should be derived from the FNV-1a hash
        Assert.NotEqual(0UL, profile.MarkerHaplotypeA);
        Assert.NotEqual(0UL, profile.MarkerHaplotypeB);

        // Generating from the same phenotype should yield the exact same profile
        var profile2 = GeneticProfile.FromPhenotype(genome);
        Assert.Equal(profile.MarkerHaplotypeA, profile2.MarkerHaplotypeA);
    }

    [Fact]
    public void GeneticProfile_ApplyPhenotype_AppliesPropertiesProperly()
    {
        var random = new Random(123);
        var profile = GeneticProfile.Random(random);

        var genome = new Genome();
        profile.ApplyPhenotype(ref genome);

        Assert.Equal(profile.Speed.ExpressedValue, genome.Speed);
        Assert.Equal(profile.Size.ExpressedValue, genome.Size);
        Assert.Equal(Math.Clamp((int)MathF.Round(profile.ColorR.ExpressedValue), 40, 255), genome.Color.R);
        Assert.Equal(Math.Clamp((int)MathF.Round(profile.ColorG.ExpressedValue), 40, 255), genome.Color.G);
        Assert.Equal(Math.Clamp((int)MathF.Round(profile.ColorB.ExpressedValue), 40, 255), genome.Color.B);
        Assert.True(genome.Genetics.IsInitialized);
    }

    [Fact]
    public void GeneticProfile_Recombine_GeneratesValidOffspring()
    {
        var random = new Random(123);
        var p1 = GeneticProfile.Random(random);
        var p2 = GeneticProfile.Random(random);

        var child = GeneticProfile.Recombine(p1, p2, random);

        Assert.True(child.IsInitialized);
        Assert.InRange(child.MutationRate.ExpressedValue, 0.01f, 0.2f);

        // Test gamete recombination and marker crossover
        Assert.NotEqual(0UL, child.MarkerHaplotypeA);
        Assert.NotEqual(0UL, child.MarkerHaplotypeB);
    }

    [Fact]
    public void Genome_EnsureGeneticProfile_CreatesFromPhenotypeIfUninitialized()
    {
        var genome = new Genome { Speed = 2.0f, Size = 1.0f };
        Assert.False(genome.Genetics.IsInitialized);

        var profile = genome.EnsureGeneticProfile();
        Assert.True(profile.IsInitialized);
        Assert.Equal(2.0f, profile.Speed.ExpressedValue);
    }

    [Fact]
    public void Genome_EnsureGeneticProfile_ReturnsExistingIfInitialized()
    {
        var random = new Random(123);
        var genome = Genome.Random(random);
        var origProfile = genome.Genetics;

        var profile = genome.EnsureGeneticProfile();
        Assert.Equal(origProfile.MarkerHaplotypeA, profile.MarkerHaplotypeA);
    }

    [Fact]
    public void Genome_Reproduce_CombinesParentsGenetics()
    {
        var random = new Random(123);
        var p1 = Genome.Random(random);
        var p2 = Genome.Random(random);

        var child = Genome.Reproduce(p1, p2, random);

        Assert.True(child.Genetics.IsInitialized);
        Assert.NotEqual(p1.Genetics.MarkerHaplotypeA, child.Genetics.MarkerHaplotypeA);
        Assert.NotEqual(p2.Genetics.MarkerHaplotypeA, child.Genetics.MarkerHaplotypeA);
    }

    [Fact]
    public void Genetics_ApplyPhenotype_ClampsMutationRateAndAdaptations()
    {
        // Even if genetic profile is created with out-of-bounds mutation rate
        var profile = GeneticProfile.FromPhenotype(new Genome { MutationRate = 1.0f, DesertAdaptation = 2.0f, ColdAdaptation = -1.0f });
        var genome = new Genome();
        profile.ApplyPhenotype(ref genome);

        Assert.InRange(genome.MutationRate, 0.01f, 0.2f);
        Assert.InRange(genome.DesertAdaptation, 0f, 1f);
        Assert.InRange(genome.ColdAdaptation, 0f, 1f);
    }
}
