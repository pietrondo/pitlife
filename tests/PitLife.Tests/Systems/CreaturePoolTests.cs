using System;
using Microsoft.Xna.Framework;
using Moq;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

// Dummy class to test non-overridable Species property
public class DummyCreature : Creature
{
    public DummyCreature(Vector2 position, Genome genome, string species)
        : base(position, genome, CreatureType.Herbivore)
    {
        Species = species;
    }

    protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
    {
        return new DummyCreature(position, genome, Species);
    }
}

public class CreaturePoolTests
{
    [Fact]
    public void Return_WithNullCreature_DoesNotThrow()
    {
        var pool = new CreaturePool();
        var ex = Record.Exception(() => pool.Return(null!));
        Assert.Null(ex);
    }

    [Fact]
    public void Return_WithUnknownSpecies_DoesNotThrow()
    {
        var pool = new CreaturePool();
        var creature = new DummyCreature(Vector2.Zero, new Genome(), "UnknownSpecies");
        var ex = Record.Exception(() => pool.Return(creature));
        Assert.Null(ex);
    }

    [Fact]
    public void Rent_WhenPoolEmpty_CreatesNewInstance()
    {
        var pool = new CreaturePool();

        // Use a species known to the game
        var rented = pool.Rent("Rabbit", Vector2.Zero, new Genome());

        Assert.NotNull(rented);
        Assert.Equal("Rabbit", rented.Species);
    }

    [Fact]
    public void Rent_WhenPoolHasItems_ReturnsReusedInstance()
    {
        var pool = new CreaturePool();

        // Initial creation
        var initialCreature = pool.Rent("Rabbit", Vector2.Zero, new Genome());
        Assert.NotNull(initialCreature);

        // Return to pool
        pool.Return(initialCreature);

        // Rent again
        var reusedCreature = pool.Rent("Rabbit", Vector2.Zero, new Genome());

        Assert.NotNull(reusedCreature);
        Assert.Equal(initialCreature, reusedCreature); // Should be the exact same instance
    }

    [Fact]
    public void Rent_WithUnknownSpecies_ReturnsNull()
    {
        var pool = new CreaturePool();

        var result = pool.Rent("DefinitelyNotARealSpecies", Vector2.Zero, new Genome());

        Assert.Null(result);
    }
}
