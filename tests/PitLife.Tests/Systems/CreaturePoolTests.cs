using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class CreaturePoolTests
{
    [Fact]
    public void Rent_UnknownSpecies_ReturnsNull()
    {
        var pool = new CreaturePool();
        var creature = pool.Rent("UnknownSpecies", Vector2.Zero, Genome.Random(new Random(42)));
        Assert.Null(creature);
    }

    [Fact]
    public void Rent_NewCreature_IsCreatedCorrectly()
    {
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(42));
        var pos = new Vector2(100, 100);

        var creature = pool.Rent("Rabbit", pos, genome);

        Assert.NotNull(creature);
        Assert.Equal("Rabbit", creature.Species);
        Assert.Equal(pos, creature.Position);
        Assert.Equal(genome, creature.Genome);
        Assert.True(creature.IsAlive);
        Assert.IsType<Herbivore>(creature);
    }

    [Fact]
    public void Return_And_Rent_ReusesCreature()
    {
        var pool = new CreaturePool();
        var rng = new Random(42);

        var firstGenome = Genome.Random(rng);
        var firstPos = new Vector2(50, 50);
        var creature1 = pool.Rent("Rabbit", firstPos, firstGenome);

        Assert.NotNull(creature1);

        // Mutate the creature to simulate game tick
        creature1.Die(DeathCause.Starvation);

        // Return to pool
        pool.Return(creature1);

        // Rent again
        var secondGenome = Genome.Random(rng);
        var secondPos = new Vector2(150, 150);
        var creature2 = pool.Rent("Rabbit", secondPos, secondGenome);

        Assert.NotNull(creature2);

        // Same reference because it was pooled
        Assert.Same(creature1, creature2);

        // But properties are reset
        Assert.Equal(secondPos, creature2.Position);
        Assert.Equal(secondGenome, creature2.Genome);
        Assert.Equal(0, creature2.Age);
        Assert.True(creature2.IsAlive);
        Assert.Equal(DeathCause.Unknown, creature2.DeathCause);
    }

    [Fact]
    public void Return_NullCreature_IsIgnored()
    {
        var pool = new CreaturePool();
        var exception = Record.Exception(() => pool.Return(null!));
        Assert.Null(exception);
    }

    [Fact]
    public void Pools_AreSegregatedBySpecies()
    {
        var pool = new CreaturePool();
        var rng = new Random(42);

        var rabbit = pool.Rent("Rabbit", Vector2.Zero, Genome.Random(rng));
        var wolf = pool.Rent("Wolf", Vector2.Zero, Genome.Random(rng));

        pool.Return(rabbit!);
        pool.Return(wolf!);

        var newWolf = pool.Rent("Wolf", Vector2.Zero, Genome.Random(rng));

        Assert.Same(wolf, newWolf);
        Assert.NotSame(rabbit, newWolf);
    }
}
