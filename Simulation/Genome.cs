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

    public static Genome Random(Random rng)
    {
        return new Genome
        {
            Speed = (float)(0.5 + rng.NextDouble() * 1.5),
            Size = (float)(0.5 + rng.NextDouble() * 1.5),
            Metabolism = (float)(0.5 + rng.NextDouble() * 1.5),
            VisionRange = (float)(1.0 + rng.NextDouble() * 9.0),
            Color = new Color(rng.Next(40, 256), rng.Next(40, 256), rng.Next(40, 256)),
            MutationRate = 0.05f
        };
    }

    public static Genome Reproduce(Genome parent1, Genome parent2, Random rng)
    {
        var child = rng.Next(2) == 0 ? parent1 : parent2;
        child.Speed = Mutate(child.Speed, 0.5f, 2.0f, rng);
        child.Size = Mutate(child.Size, 0.5f, 2.0f, rng);
        child.Metabolism = Mutate(child.Metabolism, 0.5f, 2.0f, rng);
        child.VisionRange = Mutate(child.VisionRange, 1.0f, 10.0f, rng);
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

    private static float Mutate(float value, float min, float max, Random rng)
    {
        if (rng.NextDouble() >= 0.05) return value;
        value += (float)((rng.NextDouble() - 0.5) * 0.3);
        return MathHelper.Clamp(value, min, max);
    }
}
