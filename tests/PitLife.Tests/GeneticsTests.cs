using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class GeneticsTests
{
    [Fact]
    public void GeneticAllele_Mutate_ZeroRateReturnsSameValue()
    {
        var random = new Random(1);
        var allele = new GeneticAllele(1f, 0.5f);
        var mutated = allele.Mutate(0f, 0f, 2f, 0.1f, random);

        Assert.Equal(1f, mutated.Value);
        Assert.Equal(0.5f, mutated.Dominance);
    }

    [Fact]
    public void GeneticAllele_Mutate_ChangesValueWithinBounds_RespectsStep()
    {
        var random = new Random(1);
        var allele = new GeneticAllele(1f, 0.5f);

        bool valueChanged = false;
        bool dominanceChanged = false;

        for (int i = 0; i < 100; i++)
        {
            var mutated = allele.Mutate(1f, 0.8f, 1.2f, 0.1f, random);
            Assert.InRange(mutated.Value, 0.8f, 1.2f);
            Assert.InRange(mutated.Dominance, 0.05f, 1f);

            if (Math.Abs(mutated.Value - 1f) > 0.0001f) valueChanged = true;
            if (Math.Abs(mutated.Dominance - 0.5f) > 0.0001f) dominanceChanged = true;

            allele = mutated;
        }

        Assert.True(valueChanged);
        Assert.True(dominanceChanged);
    }

    [Fact]
    public void DiploidLocus_ExpressedValue_DominantRecessive()
    {
        // One dominant, one recessive
        var locus = new DiploidLocus(
            new GeneticAllele(10f, 0.9f),
            new GeneticAllele(5f, 0.1f)
        );
        // Expression = (10 * 0.9 + 5 * 0.1) / (0.9 + 0.1) = (9 + 0.5) / 1 = 9.5
        Assert.Equal(9.5f, locus.ExpressedValue, 5);

        // When total dominance <= 0.0001f, uses average
        var locusNoDom = new DiploidLocus(
            new GeneticAllele(10f, 0f),
            new GeneticAllele(5f, 0f)
        );
        Assert.Equal(7.5f, locusNoDom.ExpressedValue, 5);
    }

    [Fact]
    public void DiploidLocus_Homozygous_BothAllelesSame()
    {
        var locus = DiploidLocus.Homozygous(3.14f);
        Assert.Equal(3.14f, locus.AlleleA.Value);
        Assert.Equal(3.14f, locus.AlleleB.Value);
        Assert.Equal(0.5f, locus.AlleleA.Dominance);
        Assert.Equal(0.5f, locus.AlleleB.Dominance);
        Assert.Equal(3.14f, locus.ExpressedValue, 5);
    }

    [Fact]
    public void DiploidLocus_Random_ValuesWithinRange()
    {
        var random = new Random(2);
        var locus = DiploidLocus.Random(2f, 5f, random);

        Assert.InRange(locus.AlleleA.Value, 2f, 5f);
        Assert.InRange(locus.AlleleB.Value, 2f, 5f);
        Assert.InRange(locus.AlleleA.Dominance, 0.2f, 1f);
        Assert.InRange(locus.AlleleB.Dominance, 0.2f, 1f);
    }

    [Fact]
    public void DiploidLocus_Inherit_SinglePointCrossoverAndMutation()
    {
        var random = new Random(3);
        var parent1 = new DiploidLocus(new GeneticAllele(1f, 0.5f), new GeneticAllele(2f, 0.5f));
        var parent2 = new DiploidLocus(new GeneticAllele(3f, 0.5f), new GeneticAllele(4f, 0.5f));

        var child = DiploidLocus.Inherit(parent1, parent2, 0f, 0f, 5f, 0.1f, random);

        // Without mutation, it should take exactly one allele from each parent
        bool aFromParent1 = child.AlleleA.Value == 1f || child.AlleleA.Value == 2f;
        bool bFromParent2 = child.AlleleB.Value == 3f || child.AlleleB.Value == 4f;

        Assert.True(aFromParent1);
        Assert.True(bFromParent2);

        // With high mutation, values should change
        var mutatedChild = DiploidLocus.Inherit(parent1, parent2, 1f, 0f, 5f, 0.5f, random);

        bool aChanged = mutatedChild.AlleleA.Value != 1f && mutatedChild.AlleleA.Value != 2f;
        bool bChanged = mutatedChild.AlleleB.Value != 3f && mutatedChild.AlleleB.Value != 4f;

        Assert.True(aChanged || mutatedChild.AlleleA.Dominance != 0.5f);
        Assert.True(bChanged || mutatedChild.AlleleB.Dominance != 0.5f);
    }

    [Fact]
    public void GeneticProfile_Random_InitializedWithinRanges()
    {
        var random = new Random(4);
        var profile = GeneticProfile.Random(random);

        Assert.True(profile.IsInitialized);
        Assert.InRange(profile.Speed.ExpressedValue, 0.5f, 2f);
        Assert.InRange(profile.Size.ExpressedValue, 0.5f, 2f);
        Assert.InRange(profile.Metabolism.ExpressedValue, 0.5f, 2f);
        Assert.InRange(profile.VisionRange.ExpressedValue, 1f, 10f);
        Assert.Equal(0.05f, profile.MutationRate.ExpressedValue);
        Assert.InRange(profile.DesertAdaptation.ExpressedValue, 0f, 1f);
        Assert.InRange(profile.ColorR.ExpressedValue, 40f, 255f);

        Assert.NotEqual(0ul, profile.MarkerHaplotypeA);
        Assert.NotEqual(0ul, profile.MarkerHaplotypeB);
    }

    [Fact]
    public void GeneticProfile_FromPhenotype_MapsGenomeCorrectly()
    {
        var genome = new Genome
        {
            Speed = 1.2f,
            Size = 0.8f,
            Metabolism = 1.1f,
            VisionRange = 8f,
            MutationRate = 0.1f,
            DesertAdaptation = 0.5f,
            ColdAdaptation = 0.2f,
            ForestAdaptation = 0.3f,
            WaterAdaptation = 0.4f,
            Color = new Color(100, 150, 200)
        };

        var profile = GeneticProfile.FromPhenotype(genome);

        Assert.True(profile.IsInitialized);
        Assert.Equal(1.2f, profile.Speed.ExpressedValue);
        Assert.Equal(0.8f, profile.Size.ExpressedValue);
        Assert.Equal(0.1f, profile.MutationRate.ExpressedValue);
        Assert.Equal(100f, profile.ColorR.ExpressedValue);
        Assert.Equal(150f, profile.ColorG.ExpressedValue);
        Assert.Equal(200f, profile.ColorB.ExpressedValue);

        // Haplotypes should be initialized based on HashPhenotype
        Assert.NotEqual(0ul, profile.MarkerHaplotypeA);
        Assert.NotEqual(0ul, profile.MarkerHaplotypeB);
        Assert.NotEqual(profile.MarkerHaplotypeA, profile.MarkerHaplotypeB);
    }

    [Fact]
    public void GeneticProfile_Recombine_IdenticalParentsProduceSimilarOffspring()
    {
        var random = new Random(5);
        var parent = GeneticProfile.Random(random);

        // When parents are identical and mutation rate is 0
        parent.MutationRate = DiploidLocus.Homozygous(0f);

        var child = GeneticProfile.Recombine(parent, parent, random);

        Assert.Equal(parent.Speed.ExpressedValue, child.Speed.ExpressedValue, 5);
        Assert.Equal(parent.Size.ExpressedValue, child.Size.ExpressedValue, 5);
        Assert.Equal(parent.ColorR.ExpressedValue, child.ColorR.ExpressedValue, 5);
    }

    [Fact]
    public void GeneticProfile_Recombine_CrossingOverProducesValidProfile()
    {
        var random = new Random(6);
        var p1 = GeneticProfile.Random(random);
        var p2 = GeneticProfile.Random(random);

        var child = GeneticProfile.Recombine(p1, p2, random);

        Assert.True(child.IsInitialized);
        Assert.InRange(child.Speed.ExpressedValue, 0.5f, 2f);
        Assert.InRange(child.ColorR.ExpressedValue, 40f, 255f);
    }

    [Fact]
    public void GeneticProfile_ApplyPhenotype_WritesProfileBackToGenome()
    {
        var random = new Random(7);
        var profile = GeneticProfile.Random(random);
        // Force an out-of-bounds mutation rate in profile to test clamping in ApplyPhenotype
        profile.MutationRate = DiploidLocus.Homozygous(0.5f);
        profile.ColorR = DiploidLocus.Homozygous(300f); // out of bounds color
        profile.ColorB = DiploidLocus.Homozygous(10f); // out of bounds color

        var genome = new Genome();
        profile.ApplyPhenotype(ref genome);

        Assert.Equal(profile.Speed.ExpressedValue, genome.Speed);
        Assert.Equal(profile.VisionRange.ExpressedValue, genome.VisionRange);

        // Check bounds clamping
        Assert.Equal(0.2f, genome.MutationRate); // clamped to max 0.2f
        Assert.Equal(255, genome.Color.R); // clamped to max 255
        Assert.Equal(40, genome.Color.B); // clamped to min 40

        // Assert genome now has the profile assigned
        Assert.True(genome.Genetics.IsInitialized);
    }

    [Fact]
    public void GeneticProfile_Recombine_ExtremeMutationRates()
    {
        var random = new Random(8);
        var p1 = GeneticProfile.Random(random);
        p1.MutationRate = DiploidLocus.Homozygous(10f); // High

        var p2 = GeneticProfile.Random(random);
        p2.MutationRate = DiploidLocus.Homozygous(-5f); // Low

        var child = GeneticProfile.Recombine(p1, p2, random);

        // The inherited mutation rate logic averages them: (10 + -5)/2 = 2.5
        // But Recombine applies MathHelper.Clamp(..., 0.01f, 0.2f)

        // We can't directly check the inheritedMutationRate local variable, but we can verify
        // the child's mutation locus was bounded appropriately.
        // DiploidLocus.Inherit passes 0.01f and 0.2f as bounds for MutationRate locus
        // In Inherit for MutationRate, DiploidLocus passes 0.01f and 0.2f as bounds,
        // however the initial value before Mutate might be outside (e.g. inherited directly from 10 or -5).
        // But since ApplyPhenotype does MathHelper.Clamp(MutationRate.ExpressedValue, 0.01f, 0.2f),
        // we test ApplyPhenotype handles clamping, but here we just ensure Recombine executes without throwing.
        var genome = new Genome();
        child.ApplyPhenotype(ref genome);
        Assert.InRange(genome.MutationRate, 0.01f, 0.2f);
    }
}
