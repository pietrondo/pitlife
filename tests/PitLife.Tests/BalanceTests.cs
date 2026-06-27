using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class BalanceTests
{
    private static GameTime Tick(float seconds) =>
        new(TimeSpan.FromSeconds(seconds), TimeSpan.FromSeconds(seconds));

    [Fact]
    public void Simulation_Survives_60Seconds_WithoutTotalExtinction()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 30, c: 10, o: 8, p: 80);

        for (var i = 0; i < 600; i++)
            eco.Tick(Tick(0.1f));

        Assert.True(eco.Creatures.Count > 0, "Ecosystem collapsed to zero creatures");
        Assert.True(eco.PlantCount > 0, "All plants died");
    }

    [Fact]
    public void Simulation_DoesNotExplode_BeyondMaxCreatures()
    {
        var eco = new Ecosystem(64, 48, 42) { MaxCreatures = 500 };
        eco.Initialize(h: 30, c: 10, o: 8, p: 80);

        for (var i = 0; i < 600; i++)
            eco.Tick(Tick(0.1f));

        Assert.True(eco.Creatures.Count <= 500, $"Population exploded to {eco.Creatures.Count}");
    }

    [Fact]
    public void SolitarySpecies_CanReproduce()
    {
        var eco = new Ecosystem(64, 48, 7);
        var m = new Carnivore(new Vector2(100, 100), Genome.Random(new Random(1)), "Tiger")
        { Gender = Gender.Male, Energy = 1000f };
        var f = new Carnivore(new Vector2(110, 100), Genome.Random(new Random(2)), "Tiger")
        { Gender = Gender.Female, Energy = 1000f };
        m.GrowFor(60f);
        f.GrowFor(60f);
        eco.AddCreature(m);
        eco.AddCreature(f);

        var child = m.ReproduceWith(f, new Random(1));
        Assert.NotNull(child);
        Assert.True(child.IsBaby);
    }

    [Fact]
    public void SolitarySpecies_Reproduce_InSimulation()
    {
        var eco = new Ecosystem(64, 48, 7);
        var m = new Carnivore(new Vector2(100, 100), Genome.Random(new Random(1)), "Tiger")
        { Gender = Gender.Male, Energy = 1000f };
        var f = new Carnivore(new Vector2(110, 100), Genome.Random(new Random(2)), "Tiger")
        { Gender = Gender.Female, Energy = 1000f };
        m.GrowFor(60f);
        f.GrowFor(60f);
        eco.AddCreature(m);
        eco.AddCreature(f);
        eco.Tick(Tick(0.1f));

        var initialCount = eco.Creatures.Count;

        for (var i = 0; i < 100; i++)
            eco.Tick(Tick(0.1f));

        Assert.True(eco.Creatures.Count > initialCount,
            $"Solitary species did not reproduce in simulation: {initialCount} → {eco.Creatures.Count}");
    }

    [Fact]
    public void Simulation_MaintainsPredatorPreyBalance()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 30, c: 10, o: 8, p: 80);

        for (var i = 0; i < 600; i++)
            eco.Tick(Tick(0.1f));

        var totalAnimals = eco.HerbivoreCount + eco.CarnivoreCount + eco.OmnivoreCount;
        Assert.True(totalAnimals > 0, "All animals died");
        Assert.True(eco.PlantCount > 0, "Plants extinct — herbivores will starve");
    }

    [Fact]
    public void Simulation_PopulationStable_OverTime()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 30, c: 10, o: 8, p: 80);

        for (var i = 0; i < 300; i++)
            eco.Tick(Tick(0.1f));

        var popAt30s = eco.Creatures.Count;

        for (var i = 0; i < 300; i++)
            eco.Tick(Tick(0.1f));

        var popAt60s = eco.Creatures.Count;

        var ratio = popAt60s / (double)Math.Max(1, popAt30s);
        Assert.True(ratio is > 0.1 and < 10.0,
            $"Population unstable: {popAt30s} at 30s → {popAt60s} at 60s (ratio {ratio:F2})");
    }
}
