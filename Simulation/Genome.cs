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

    public static Genome Random(Random rng)
    {
        return new Genome
        {
            Speed = (float)(0.5 + rng.NextDouble() * 1.5),
            Size = (float)(0.5 + rng.NextDouble() * 1.5),
            Metabolism = (float)(0.5 + rng.NextDouble() * 1.5),
            VisionRange = (float)(1.0 + rng.NextDouble() * 9.0),
            Color = new Color(rng.Next(40, 256), rng.Next(40, 256), rng.Next(40, 256)),
            MutationRate = 0.05f,
            DesertAdaptation = (float)rng.NextDouble(),
            ColdAdaptation = (float)rng.NextDouble(),
            ForestAdaptation = (float)rng.NextDouble(),
            WaterAdaptation = (float)rng.NextDouble()
        };
    }

    public static Genome Reproduce(Genome parent1, Genome parent2, Random rng)
    {
        var child = new Genome
        {
            Speed = Choose(parent1.Speed, parent2.Speed, rng),
            Size = Choose(parent1.Size, parent2.Size, rng),
            Metabolism = Choose(parent1.Metabolism, parent2.Metabolism, rng),
            VisionRange = Choose(parent1.VisionRange, parent2.VisionRange, rng),
            Color = rng.Next(2) == 0 ? parent1.Color : parent2.Color,
            MutationRate = Choose(parent1.MutationRate, parent2.MutationRate, rng),
            DesertAdaptation = Choose(parent1.DesertAdaptation, parent2.DesertAdaptation, rng),
            ColdAdaptation = Choose(parent1.ColdAdaptation, parent2.ColdAdaptation, rng),
            ForestAdaptation = Choose(parent1.ForestAdaptation, parent2.ForestAdaptation, rng),
            WaterAdaptation = Choose(parent1.WaterAdaptation, parent2.WaterAdaptation, rng)
        };
        child.Speed = Mutate(child.Speed, 0.5f, 2.0f, child.MutationRate, rng);
        child.Size = Mutate(child.Size, 0.5f, 2.0f, child.MutationRate, rng);
        child.Metabolism = Mutate(child.Metabolism, 0.5f, 2.0f, child.MutationRate, rng);
        child.VisionRange = Mutate(child.VisionRange, 1.0f, 10.0f, child.MutationRate, rng);
        child.MutationRate = Mutate(child.MutationRate, 0.01f, 0.2f, child.MutationRate, rng);
        child.DesertAdaptation = Mutate(child.DesertAdaptation, 0.0f, 1.0f, child.MutationRate, rng);
        child.ColdAdaptation = Mutate(child.ColdAdaptation, 0.0f, 1.0f, child.MutationRate, rng);
        child.ForestAdaptation = Mutate(child.ForestAdaptation, 0.0f, 1.0f, child.MutationRate, rng);
        child.WaterAdaptation = Mutate(child.WaterAdaptation, 0.0f, 1.0f, child.MutationRate, rng);

        if (rng.NextDouble() < child.MutationRate)
        {
            int comp = rng.Next(3);
            int delta = rng.Next(-30, 31);
            var c = child.Color;
            child.Color = comp switch
            {
                0 => new Color(MathHelper.Clamp(c.R + delta, 40, 255), c.G, c.B),
                1 => new Color(c.R, MathHelper.Clamp(c.G + delta, 40, 255), c.B),
                _ => new Color(c.R, c.G, MathHelper.Clamp(c.B + delta, 40, 255)),
            };
        }
        return child;
    }

    private static float Choose(float parent1, float parent2, Random rng) =>
        rng.Next(2) == 0 ? parent1 : parent2;

    private static float Mutate(float value, float min, float max, float rate, Random rng)
    {
        if (rng.NextDouble() >= rate) return value;
        value += (float)((rng.NextDouble() - 0.5) * 0.3);
        return MathHelper.Clamp(value, min, max);
    }
}
