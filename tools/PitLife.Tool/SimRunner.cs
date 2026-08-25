using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tool;

internal static class SimRunner
{
    private sealed class Options
    {
        public int Ticks = 1000;
        public int Seed = 42;
        public int Width = 64;
        public int Height = 48;
        public int Herbivores = 30;
        public int Carnivores = 10;
        public int Omnivores = 8;
        public int Plants = 80;
        public int Interval = 100;
    }

    public static int Run(string[] args)
    {
        var o = Parse(args);

        var eco = new Ecosystem(o.Width, o.Height, o.Seed) { MaxCreatures = 1000 };
        eco.Initialize(o.Herbivores, o.Carnivores, o.Omnivores, o.Plants);

        Console.WriteLine($"Simulazione: {o.Width}x{o.Height}, seed={o.Seed}, tick={o.Ticks}");
        Console.WriteLine($"Iniziali: H={o.Herbivores} C={o.Carnivores} O={o.Omnivores} P={o.Plants}\n");
        Console.WriteLine("tick   plants  herb   carn   omni   total");

        var dt = TimeSpan.FromSeconds(0.1f);
        for (int t = 0; t <= o.Ticks; t++)
        {
            if (t % o.Interval == 0)
            {
                eco.UpdateStats();
                Console.WriteLine(
                    $"{t,5}  {eco.PlantCount,6}  {eco.HerbivoreCount,5}  {eco.CarnivoreCount,4}  " +
                    $"{eco.OmnivoreCount,4}  {eco.Creatures.Count,6}");
            }
            if (t < o.Ticks)
                eco.Tick(new GameTime(dt, dt));
        }
        return 0;
    }

    private static Options Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string? Value() => i + 1 < args.Length ? args[++i] : null;
            switch (args[i])
            {
                case "--ticks": o.Ticks = int.Parse(Value()!); break;
                case "--seed": o.Seed = int.Parse(Value()!); break;
                case "--width": o.Width = int.Parse(Value()!); break;
                case "--height": o.Height = int.Parse(Value()!); break;
                case "--herbivores": o.Herbivores = int.Parse(Value()!); break;
                case "--carnivores": o.Carnivores = int.Parse(Value()!); break;
                case "--omnivores": o.Omnivores = int.Parse(Value()!); break;
                case "--plants": o.Plants = int.Parse(Value()!); break;
                case "--interval": o.Interval = int.Parse(Value()!); break;
                default:
                    throw new ArgumentException($"Opzione sconosciuta: {args[i]}");
            }
        }
        return o;
    }
}
