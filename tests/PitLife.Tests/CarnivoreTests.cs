using Microsoft.Xna.Framework;
using PitLife.Simulation;
using System;
using System.Reflection;
using Xunit;

namespace PitLife.Tests;

public class CarnivoreTests
{
    [Fact]
    public void AttackDamage_DependsOnGenomeSize()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);
        genome.Size = 2.0f;

        var carnivore = new Carnivore(new Vector2(100, 100), genome);

        Assert.Equal(40.0f, carnivore.AttackDamage, 0.01f);
    }

    [Fact]
    public void IsAquatic_TrueForSharkAndPiranha()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);

        var shark = new Carnivore(new Vector2(100, 100), genome, "Shark");
        var piranha = new Carnivore(new Vector2(100, 100), genome, "Piranha");
        var bear = new Carnivore(new Vector2(100, 100), genome, "Lion");

        Assert.True(shark.IsAquatic);
        Assert.True(piranha.IsAquatic);
        Assert.False(bear.IsAquatic);
    }

    [Fact]
    public void CreateChild_ReturnsCarnivoreType()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);
        var parent = new Carnivore(new Vector2(100, 100), genome, "TestCarnivore");

        var methodInfo = typeof(Carnivore).GetMethod("CreateChild", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        var child = (Creature)methodInfo.Invoke(parent, new object[] { new Vector2(110, 110), genome, rng })!;

        Assert.NotNull(child);
        Assert.IsType<Carnivore>(child);
        Assert.Equal(CreatureType.Carnivore, child.CreatureType);
        Assert.NotNull(child.Species);
    }
}
