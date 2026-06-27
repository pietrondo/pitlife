using System;
using System.Numerics;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public readonly record struct GeneticAllele(float Value, float Dominance)
{
    public GeneticAllele Mutate(float rate, float minimum, float maximum, float step, Random random)
    {
        var value = Value;
        var dominance = Dominance;
        if (random.NextDouble() < rate)
            value = MathHelper.Clamp(value + (float)(random.NextDouble() - 0.5) * step, minimum, maximum);
        if (random.NextDouble() < rate * 0.25f)
            dominance = MathHelper.Clamp(dominance + (float)(random.NextDouble() - 0.5) * 0.2f, 0.05f, 1f);
        return new GeneticAllele(value, dominance);
    }
}

public readonly record struct DiploidLocus(GeneticAllele AlleleA, GeneticAllele AlleleB)
{
    public float ExpressedValue
    {
        get
        {
            var totalDominance = AlleleA.Dominance + AlleleB.Dominance;
            return totalDominance <= 0.0001f
                ? (AlleleA.Value + AlleleB.Value) * 0.5f
                : (AlleleA.Value * AlleleA.Dominance + AlleleB.Value * AlleleB.Dominance) /
                  totalDominance;
        }
    }

    public static DiploidLocus Homozygous(float value) => new(
        new GeneticAllele(value, 0.5f),
        new GeneticAllele(value, 0.5f));

    public static DiploidLocus Random(float minimum, float maximum, Random random)
    {
        return new DiploidLocus(
            new GeneticAllele(RandomRange(minimum, maximum, random), RandomDominance(random)),
            new GeneticAllele(RandomRange(minimum, maximum, random), RandomDominance(random)));
    }

    public static DiploidLocus Inherit(
        DiploidLocus firstParent,
        DiploidLocus secondParent,
        float mutationRate,
        float minimum,
        float maximum,
        float mutationStep,
        Random random)
    {
        GeneticAllele first = (random.Next(2) == 0 ? firstParent.AlleleA : firstParent.AlleleB)
            .Mutate(mutationRate, minimum, maximum, mutationStep, random);
        GeneticAllele second = (random.Next(2) == 0 ? secondParent.AlleleA : secondParent.AlleleB)
            .Mutate(mutationRate, minimum, maximum, mutationStep, random);
        return new DiploidLocus(first, second);
    }

    private static float RandomRange(float minimum, float maximum, Random random) =>
        minimum + (float)random.NextDouble() * (maximum - minimum);

    private static float RandomDominance(Random random) => 0.2f + (float)random.NextDouble() * 0.8f;
}

public struct GeneticProfile
{
    public bool IsInitialized;
    public DiploidLocus Speed;
    public DiploidLocus Size;
    public DiploidLocus Metabolism;
    public DiploidLocus VisionRange;
    public DiploidLocus MutationRate;
    public DiploidLocus DesertAdaptation;
    public DiploidLocus ColdAdaptation;
    public DiploidLocus ForestAdaptation;
    public DiploidLocus WaterAdaptation;
    public DiploidLocus ColorR;
    public DiploidLocus ColorG;
    public DiploidLocus ColorB;
    public ulong MarkerHaplotypeA;
    public ulong MarkerHaplotypeB;

    public float Heterozygosity => IsInitialized
        ? BitOperations.PopCount(MarkerHaplotypeA ^ MarkerHaplotypeB) / 64f
        : 0f;

    public static GeneticProfile Random(Random random)
    {
        return new GeneticProfile
        {
            IsInitialized = true,
            Speed = DiploidLocus.Random(0.5f, 2f, random),
            Size = DiploidLocus.Random(0.5f, 2f, random),
            Metabolism = DiploidLocus.Random(0.5f, 2f, random),
            VisionRange = DiploidLocus.Random(1f, 10f, random),
            MutationRate = DiploidLocus.Homozygous(0.05f),
            DesertAdaptation = DiploidLocus.Random(0f, 1f, random),
            ColdAdaptation = DiploidLocus.Random(0f, 1f, random),
            ForestAdaptation = DiploidLocus.Random(0f, 1f, random),
            WaterAdaptation = DiploidLocus.Random(0f, 1f, random),
            ColorR = DiploidLocus.Random(40f, 255f, random),
            ColorG = DiploidLocus.Random(40f, 255f, random),
            ColorB = DiploidLocus.Random(40f, 255f, random),
            MarkerHaplotypeA = NextUInt64(random),
            MarkerHaplotypeB = NextUInt64(random)
        };
    }

    public static GeneticProfile FromPhenotype(Genome genome)
    {
        var markerA = HashPhenotype(genome);
        var markerB = BitOperations.RotateLeft(markerA ^ 0x9E3779B97F4A7C15UL, 29);
        return new GeneticProfile
        {
            IsInitialized = true,
            Speed = DiploidLocus.Homozygous(genome.Speed),
            Size = DiploidLocus.Homozygous(genome.Size),
            Metabolism = DiploidLocus.Homozygous(genome.Metabolism),
            VisionRange = DiploidLocus.Homozygous(genome.VisionRange),
            MutationRate = DiploidLocus.Homozygous(genome.MutationRate),
            DesertAdaptation = DiploidLocus.Homozygous(genome.DesertAdaptation),
            ColdAdaptation = DiploidLocus.Homozygous(genome.ColdAdaptation),
            ForestAdaptation = DiploidLocus.Homozygous(genome.ForestAdaptation),
            WaterAdaptation = DiploidLocus.Homozygous(genome.WaterAdaptation),
            ColorR = DiploidLocus.Homozygous(genome.Color.R),
            ColorG = DiploidLocus.Homozygous(genome.Color.G),
            ColorB = DiploidLocus.Homozygous(genome.Color.B),
            MarkerHaplotypeA = markerA,
            MarkerHaplotypeB = markerB
        };
    }

    public static GeneticProfile Recombine(
        GeneticProfile first,
        GeneticProfile second,
        Random random)
    {
        var inheritedMutationRate = MathHelper.Clamp(
            (first.MutationRate.ExpressedValue + second.MutationRate.ExpressedValue) * 0.5f,
            0.01f,
            0.2f);
        var child = new GeneticProfile
        {
            IsInitialized = true,
            MutationRate = DiploidLocus.Inherit(first.MutationRate, second.MutationRate,
                inheritedMutationRate, 0.01f, 0.2f, 0.03f, random),
            Speed = DiploidLocus.Inherit(first.Speed, second.Speed,
                inheritedMutationRate, 0.5f, 2f, 0.3f, random),
            Size = DiploidLocus.Inherit(first.Size, second.Size,
                inheritedMutationRate, 0.5f, 2f, 0.3f, random),
            Metabolism = DiploidLocus.Inherit(first.Metabolism, second.Metabolism,
                inheritedMutationRate, 0.5f, 2f, 0.3f, random),
            VisionRange = DiploidLocus.Inherit(first.VisionRange, second.VisionRange,
                inheritedMutationRate, 1f, 10f, 1f, random),
            DesertAdaptation = DiploidLocus.Inherit(first.DesertAdaptation, second.DesertAdaptation,
                inheritedMutationRate, 0f, 1f, 0.2f, random),
            ColdAdaptation = DiploidLocus.Inherit(first.ColdAdaptation, second.ColdAdaptation,
                inheritedMutationRate, 0f, 1f, 0.2f, random),
            ForestAdaptation = DiploidLocus.Inherit(first.ForestAdaptation, second.ForestAdaptation,
                inheritedMutationRate, 0f, 1f, 0.2f, random),
            WaterAdaptation = DiploidLocus.Inherit(first.WaterAdaptation, second.WaterAdaptation,
                inheritedMutationRate, 0f, 1f, 0.2f, random),
            ColorR = DiploidLocus.Inherit(first.ColorR, second.ColorR,
                inheritedMutationRate, 40f, 255f, 30f, random),
            ColorG = DiploidLocus.Inherit(first.ColorG, second.ColorG,
                inheritedMutationRate, 40f, 255f, 30f, random),
            ColorB = DiploidLocus.Inherit(first.ColorB, second.ColorB,
                inheritedMutationRate, 40f, 255f, 30f, random),
            MarkerHaplotypeA = MutateMarkers(CreateGamete(first, random), inheritedMutationRate, random),
            MarkerHaplotypeB = MutateMarkers(CreateGamete(second, random), inheritedMutationRate, random)
        };
        return child;
    }

    public void ApplyPhenotype(ref Genome genome)
    {
        genome.Speed = Speed.ExpressedValue;
        genome.Size = Size.ExpressedValue;
        genome.Metabolism = Metabolism.ExpressedValue;
        genome.VisionRange = VisionRange.ExpressedValue;
        genome.MutationRate = MathHelper.Clamp(MutationRate.ExpressedValue, 0.01f, 0.2f);
        genome.DesertAdaptation = MathHelper.Clamp(DesertAdaptation.ExpressedValue, 0f, 1f);
        genome.ColdAdaptation = MathHelper.Clamp(ColdAdaptation.ExpressedValue, 0f, 1f);
        genome.ForestAdaptation = MathHelper.Clamp(ForestAdaptation.ExpressedValue, 0f, 1f);
        genome.WaterAdaptation = MathHelper.Clamp(WaterAdaptation.ExpressedValue, 0f, 1f);
        genome.Color = new Color(
            (byte)Math.Clamp((int)MathF.Round(ColorR.ExpressedValue), 40, 255),
            (byte)Math.Clamp((int)MathF.Round(ColorG.ExpressedValue), 40, 255),
            (byte)Math.Clamp((int)MathF.Round(ColorB.ExpressedValue), 40, 255));
        genome.Genetics = this;
    }

    private static ulong CreateGamete(GeneticProfile profile, Random random)
    {
        var crossover = random.Next(1, 64);
        var lowerMask = (1UL << crossover) - 1UL;
        return random.Next(2) == 0
            ? (profile.MarkerHaplotypeA & lowerMask) | (profile.MarkerHaplotypeB & ~lowerMask)
            : (profile.MarkerHaplotypeB & lowerMask) | (profile.MarkerHaplotypeA & ~lowerMask);
    }

    private static ulong MutateMarkers(ulong markers, float mutationRate, Random random)
    {
        if (random.NextDouble() < mutationRate)
            markers ^= 1UL << random.Next(64);
        return markers;
    }

    private static ulong NextUInt64(Random random)
    {
        Span<byte> bytes = stackalloc byte[8];
        random.NextBytes(bytes);
        return BitConverter.ToUInt64(bytes);
    }

    private static ulong HashPhenotype(Genome genome)
    {
        var hash = 1469598103934665603UL;
        Add(ref hash, genome.Speed);
        Add(ref hash, genome.Size);
        Add(ref hash, genome.Metabolism);
        Add(ref hash, genome.VisionRange);
        Add(ref hash, genome.MutationRate);
        Add(ref hash, genome.DesertAdaptation);
        Add(ref hash, genome.ColdAdaptation);
        Add(ref hash, genome.ForestAdaptation);
        Add(ref hash, genome.WaterAdaptation);
        hash = (hash ^ genome.Color.PackedValue) * 1099511628211UL;
        return hash;
    }

    private static void Add(ref ulong hash, float value)
    {
        hash = (hash ^ BitConverter.SingleToUInt32Bits(value)) * 1099511628211UL;
    }
}
