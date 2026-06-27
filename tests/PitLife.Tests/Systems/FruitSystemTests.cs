using System;
using System.Linq;
using Microsoft.Xna.Framework;
using Moq;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class FruitSystemTests
{
    [Fact]
    public void Reset_ClearsFruits()
    {
        var eco = new Ecosystem(16, 12, 42);
        var fruitSys = new FruitSystem();

        var plant = new Plant(new Vector2(100, 100), Genome.Random(eco.Random), "BerryBush");
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        fruitSys.Update(eco, 10f);

        Assert.True(fruitSys.Fruits.Count > 0, "Should have spawned fruits");

        fruitSys.Reset();

        Assert.Empty(fruitSys.Fruits);
    }

    [Fact]
    public void Update_SpawnsFruits_WhenSeedsPlantExists()
    {
        var eco = new Ecosystem(16, 12, 42);
        var fruitSys = new FruitSystem();

        var plant = new Plant(new Vector2(100, 100), Genome.Random(eco.Random), "BerryBush");
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        fruitSys.Update(eco, 10f); // 10s should trigger spawn timer

        Assert.NotEmpty(fruitSys.Fruits);
        var fruit = fruitSys.Fruits[0];
        Assert.Equal("BerryBush", fruit.PlantSpecies);
        Assert.True(fruit.IsAlive);
    }

    [Fact]
    public void Update_DecaysFruits_AndRemovesDead()
    {
        var eco = new Ecosystem(16, 12, 42);
        var fruitSys = new FruitSystem();

        var plant = new Plant(new Vector2(100, 100), Genome.Random(eco.Random), "BerryBush");
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        fruitSys.Update(eco, 10f); // Spawn fruits

        var initialCount = fruitSys.Fruits.Count;
        Assert.True(initialCount > 0);

        // Save first fruit
        var f1 = fruitSys.Fruits[0];
        Assert.True(f1.IsAlive);
        var initialLife = f1.Lifetime;

        // Remove the plant to prevent new ones from spawning during the decay phase
        eco.Creatures.Clear();

        fruitSys.Update(eco, 5f); // Decay

        // Check that lifetime has decreased
        var f1Updated = fruitSys.Fruits.FirstOrDefault(f => f.Position == f1.Position);
        Assert.True(f1Updated.Lifetime < initialLife);

                for(int i = 0; i < 20; i++)
        {
            fruitSys.Update(eco, 5f);
        }

        Assert.Empty(fruitSys.Fruits); // All should be dead and removed
    }

    [Fact]
    public void TryEatFruit_ReturnsFruitAndRemovesIt_WhenInRange()
    {
        var eco = new Ecosystem(16, 12, 42);
        var fruitSys = new FruitSystem();

        var plant = new Plant(new Vector2(100, 100), Genome.Random(eco.Random), "BerryBush");
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        fruitSys.Update(eco, 10f); // Spawn fruits

        Assert.NotEmpty(fruitSys.Fruits);
        var firstFruitPos = fruitSys.Fruits[0].Position;
        var countBefore = fruitSys.Fruits.Count;

        var eatenFruit = fruitSys.TryEatFruit(firstFruitPos, 5f);

        Assert.NotNull(eatenFruit);
        Assert.Equal(countBefore - 1, fruitSys.Fruits.Count);
    }

    [Fact]
    public void TryEatFruit_ReturnsNull_WhenNotInRange()
    {
        var eco = new Ecosystem(16, 12, 42);
        var fruitSys = new FruitSystem();

        var plant = new Plant(new Vector2(100, 100), Genome.Random(eco.Random), "BerryBush");
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        fruitSys.Update(eco, 10f); // Spawn fruits

        Assert.NotEmpty(fruitSys.Fruits);
        var firstFruitPos = fruitSys.Fruits[0].Position;
        var countBefore = fruitSys.Fruits.Count;

        var farPos = firstFruitPos + new Vector2(100, 100); // Definitely out of range
        var eatenFruit = fruitSys.TryEatFruit(farPos, 5f);

        Assert.Null(eatenFruit);
        Assert.Equal(countBefore, fruitSys.Fruits.Count);
    }
}
