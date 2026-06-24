using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;
using System;
using System.Reflection;

namespace PitLife.Tests;

public class OmnivoreTests
{
    [Fact]
    public void AttackDamage_DependsOnGenomeSize()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);
        genome.Size = 2.0f;

        var omnivore = new Omnivore(new Vector2(100, 100), genome);

        Assert.Equal(24.0f, omnivore.AttackDamage, 0.01f);
    }

    [Fact]
    public void IsAquatic_TrueOnlyForJellyfish()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);

        var jellyfish = new Omnivore(new Vector2(100, 100), genome, "Jellyfish");
        var bear = new Omnivore(new Vector2(100, 100), genome, "Bear");

        Assert.True(jellyfish.IsAquatic);
        Assert.False(bear.IsAquatic);
    }

    [Fact]
    public void CreateChild_ReturnsOmnivoreType()
    {
        var rng = new Random(42);
        var genome = Genome.Random(rng);
        var parent = new Omnivore(new Vector2(100, 100), genome, "TestOmni");

        var methodInfo = typeof(Omnivore).GetMethod("CreateChild", BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(methodInfo);

        var child = (Creature)methodInfo.Invoke(parent, new object[] { new Vector2(110, 110), genome, rng })!;

        Assert.NotNull(child);
        Assert.IsType<Omnivore>(child);
        Assert.Equal(CreatureType.Omnivore, child.CreatureType);
        Assert.NotNull(child.Species);
    }
}
