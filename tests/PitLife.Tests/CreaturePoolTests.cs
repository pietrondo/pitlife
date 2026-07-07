using System;
using System.Reflection;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class CreaturePoolTests
{
    private class DummyCreature : Creature
    {
        public DummyCreature(Vector2 position, Genome genome, string species)
            : base(position, genome, CreatureType.Herbivore)
        {
            Species = species;
        }

        public override bool IsAquatic => false;

        protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
        {
            return new DummyCreature(position, genome, Species);
        }
    }

    [Fact]
    public void Return_ValidCreature_IncrementsPoolCount()
    {
        // Arrange
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(1));
        var position = Vector2.Zero;
        var testSpecies = "TestDummy";

        SpeciesRegistry.Register(new SpeciesDefinition(
            species: testSpecies,
            creatureType: typeof(DummyCreature),
            kind: CreatureType.Herbivore,
            isAquatic: false,
            socialBehavior: SocialBehavior.None,
            validBiomes: new[] { BiomeType.Grassland }
        ));

        var creature = new DummyCreature(position, genome, testSpecies);

        // Act
        pool.Return(creature);

        // Assert
        var poolsField = typeof(CreaturePool).GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
        var poolsDict = (Dictionary<string, Stack<Creature>>)poolsField!.GetValue(pool)!;
        var key = $"{typeof(DummyCreature).Name}:{testSpecies}";

        Assert.True(poolsDict.ContainsKey(key));
        Assert.Single(poolsDict[key]);

        // Cleanup
        typeof(SpeciesRegistry).GetMethod("Unregister", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!.Invoke(null, new object[] { testSpecies });
    }

    [Fact]
    public void Return_NullCreature_DoesNothing()
    {
        // Arrange
        var pool = new CreaturePool();

        // Act
        pool.Return(null!);

        // Assert
        var poolsField = typeof(CreaturePool).GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
        var poolsDict = (Dictionary<string, Stack<Creature>>)poolsField!.GetValue(pool)!;

        Assert.Empty(poolsDict);
    }

    [Fact]
    public void Return_UnregisteredSpecies_DoesNothing()
    {
        // Arrange
        var pool = new CreaturePool();
        var genome = Genome.Random(new Random(1));
        var position = Vector2.Zero;
        var unregisteredSpecies = "UnregisteredDummy";
        var creature = new DummyCreature(position, genome, unregisteredSpecies);

        // Act
        pool.Return(creature);

        // Assert
        var poolsField = typeof(CreaturePool).GetField("_pools", BindingFlags.NonPublic | BindingFlags.Instance);
        var poolsDict = (Dictionary<string, Stack<Creature>>)poolsField!.GetValue(pool)!;

        Assert.Empty(poolsDict);
    }
}
