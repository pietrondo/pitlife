using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class CreaturePoolTests
{
    [Fact]
    public void Rent_WhenEmpty_CreatesNewInstance()
    {
        // Arrange
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(42));
        var pos = new Vector2(10, 10);

        // Act
        var creature = pool.Rent("Deer", pos, genome);

        // Assert
        Assert.NotNull(creature);
        Assert.Equal("Deer", creature!.Species);
        Assert.Equal(pos, creature.Position);
        Assert.Equal(genome, creature.Genome);
    }

    [Fact]
    public void Rent_WhenPoolHasInstance_PopsAndResets()
    {
        // Arrange
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(42));
        var genome2 = Genome.Random(new Random(43));
        var pos1 = new Vector2(10, 10);
        var pos2 = new Vector2(20, 20);

        var creature1 = pool.Rent("Deer", pos1, genome);
        Assert.NotNull(creature1);
        creature1!.Energy = 50f;

        // Return to pool
        pool.Return(creature1);

        // Act
        var creature2 = pool.Rent("Deer", pos2, genome2);

        // Assert
        Assert.NotNull(creature2);
        Assert.Same(creature1, creature2);
        Assert.Equal(pos2, creature2!.Position);
        Assert.Equal(genome2, creature2.Genome);
        Assert.Equal(0, creature2.Age); // Verify ResetForReuse was called
    }

    [Fact]
    public void Rent_WithInvalidSpecies_ReturnsNull()
    {
        // Arrange
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(42));

        // Act
        var creature = pool.Rent("NonExistentSpecies", Vector2.Zero, genome);

        // Assert
        Assert.Null(creature);
    }

    [Fact]
    public void Return_NullOrInvalid_DoesNotThrow()
    {
        // Arrange
        var pool = new CreaturePool();

        // Act & Assert
        // Returning null should not throw
        pool.Return(null!);

        // Returning creature with invalid species should not throw
        var genome = Genome.Random(new Random(42));
        var invalidCreature = new Herbivore(Vector2.Zero, genome, "InvalidSpeciesForReturn");
        pool.Return(invalidCreature);

        // If we made it here, no exceptions were thrown
        Assert.True(true);
    }
}
