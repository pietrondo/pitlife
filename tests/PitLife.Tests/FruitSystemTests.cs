using System;
using System.Linq;
using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.Tests;

public class FruitSystemTests
{
    [Fact]
    public void Fruit_Initialization_SetsProperties()
    {
        var fruit = new Fruit(new Vector2(10, 20), 15f, 5f, "Poppy", true, 0.5f);

        Assert.Equal(new Vector2(10, 20), fruit.Position);
        Assert.Equal(15f, fruit.EnergyValue);
        Assert.Equal(5f, fruit.Lifetime);
        Assert.Equal(5f, fruit.MaxLifetime);
        Assert.Equal("Poppy", fruit.PlantSpecies);
        Assert.True(fruit.Poisonous);
        Assert.Equal(0.5f, fruit.Toxicity);
        Assert.True(fruit.IsAlive);
    }

    [Fact]
    public void Fruit_GetColor_TransparentWhenDead()
    {
        var fruit = new Fruit(Vector2.Zero, 10f, 5f, "Poppy");
        fruit.Lifetime = 0f;

        Assert.Equal(Color.Transparent, fruit.GetColor());
    }

    [Fact]
    public void Fruit_GetColor_LerpsBasedOnAge_NonPoisonous()
    {
        var fruit = new Fruit(Vector2.Zero, 10f, 10f, "Poppy", false);

        Assert.Equal(Color.Red, fruit.GetColor()); // Full lifetime

        fruit.Lifetime = 0f;
        Assert.Equal(Color.Transparent, fruit.GetColor());
    }

    [Fact]
    public void Fruit_GetColor_LerpsBasedOnAge_Poisonous()
    {
        var fruit = new Fruit(Vector2.Zero, 10f, 10f, "Belladonna", true);

        Assert.Equal(Color.DarkViolet, fruit.GetColor()); // Full lifetime

        fruit.Lifetime = 0f;
        Assert.Equal(Color.Transparent, fruit.GetColor());
    }


    [Fact]
    public void FruitSystem_Initialize_ResetsStateAndPreservesReference()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        // Should start with 0 fruits
        Assert.Empty(system.Fruits);

        // Access private field to verify allocation
        var fieldInfo = typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fruitsArray = (Fruit[])fieldInfo!.GetValue(system)!;

        Assert.Equal(FruitConfig.Data.MaxFruits, fruitsArray.Length);

        // Store reference to test preservation
        var originalArray = fruitsArray;

        // Re-initialize should not recreate the array if size is the same
        system.Initialize(eco.World);
        fruitsArray = (Fruit[])fieldInfo.GetValue(system)!;
        Assert.Same(originalArray, fruitsArray);
    }

    [Fact]
    public void FruitSystem_Reset_ClearsFruitCount()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        // We need to simulate adding a fruit to test Reset properly,
        // we can do this via reflection since there's no public AddFruit method
        var countField = typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        countField!.SetValue(system, 5);

        Assert.Equal(5, system.Fruits.Count);

        system.Reset();

        Assert.Empty(system.Fruits);
    }


    [Fact]
    public void FruitSystem_Update_SpawnsFruitsFromSeedPlants()
    {
        var eco = new Ecosystem(32, 24, 7);
        eco.Initialize(0, 0, 0, 0); // start empty

        // Add a seed producing plant
        var plantPos = new Vector2(100, 100);
        var plant = new Plant(plantPos, new Genome(), "Poppy");
        eco.Creatures.Add(plant);

        var system = new FruitSystem();
        system.Initialize(eco.World);

        // Force spawn timer to expire
        var timerField = typeof(FruitSystem).GetField("_spawnTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        timerField!.SetValue(system, 0f);

        system.Update(eco, 1.0f); // this triggers spawning

        Assert.NotEmpty(system.Fruits);
        var spawnedFruit = system.Fruits[0];
        Assert.Equal("Poppy", spawnedFruit.PlantSpecies);
        // Distance should be within SpawnOffsetMax
        Assert.True(Vector2.Distance(plantPos, spawnedFruit.Position) <= FruitConfig.Data.SpawnOffsetMax * 1.5f); // *1.5 for diagonal
    }

    [Fact]
    public void FruitSystem_Update_DecaysAndRemovesDeadFruits()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        // Manually inject a fruit
        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(Vector2.Zero, 10f, 2f, "Poppy");
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 1);

        // Update with delta time < lifetime, fruit should decay but survive
        system.Update(eco, 1f);
        Assert.Single(system.Fruits);
        Assert.Equal(1f, system.Fruits[0].Lifetime);

        // Update again with delta time >= remaining lifetime, fruit decays to 0 but is removed on the *next* update pass
        system.Update(eco, 1f);
        Assert.False(system.Fruits[0].IsAlive);

        // Final update pass cleans up the dead fruit
        system.Update(eco, 1f);
        Assert.Empty(system.Fruits);
    }

    [Fact]
    public void FruitSystem_Update_SpawnsPoisonousFruitFromCarnivorousPlants()
    {
        var eco = new Ecosystem(32, 24, 7);
        eco.Initialize(0, 0, 0, 0);

        var plantPos = new Vector2(100, 100);
        var plant = new Plant(plantPos, new Genome(), "Belladonna");
        eco.Creatures.Add(plant);

        var system = new FruitSystem();
        system.Initialize(eco.World);

        var timerField = typeof(FruitSystem).GetField("_spawnTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        timerField!.SetValue(system, 0f);

        system.Update(eco, 1.0f);

        Assert.NotEmpty(system.Fruits);
        var spawnedFruit = system.Fruits[0];
        Assert.Equal("Belladonna", spawnedFruit.PlantSpecies);
        Assert.True(spawnedFruit.Poisonous);
        Assert.True(spawnedFruit.Toxicity > 0f);
    }

    [Fact]
    public void FruitSystem_Update_RespectsMaxFruits()
    {
        var eco = new Ecosystem(32, 24, 7);
        eco.Initialize(0, 0, 0, 0);
        var plant = new Plant(Vector2.Zero, new Genome(), "Poppy");
        eco.Creatures.Add(plant);

        var system = new FruitSystem();
        system.Initialize(eco.World);

        // Fill array to MaxFruits
        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        for(int i = 0; i < FruitConfig.Data.MaxFruits; i++) {
            fruitsArray[i] = new Fruit(Vector2.Zero, 10f, 10f, "Poppy");
        }
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, FruitConfig.Data.MaxFruits);

        var timerField = typeof(FruitSystem).GetField("_spawnTimer", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        timerField!.SetValue(system, 0f);

        system.Update(eco, 1.0f);

        // Should not exceed MaxFruits
        Assert.Equal(FruitConfig.Data.MaxFruits, system.Fruits.Count);
    }

    [Fact]
    public void FruitSystem_TryEatFruit_ReturnsFruitAndRemovesIt_WhenInRange()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        // Inject 2 fruits
        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 5f, 10f, "Poppy");
        fruitsArray[1] = new Fruit(new Vector2(50, 50), 5f, 10f, "Poppy");
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 2);

        // Try eat at pos (12, 10) with range 5
        var eatenFruit = system.TryEatFruit(new Vector2(12, 10), 5f);

        Assert.NotNull(eatenFruit);
        Assert.Equal(new Vector2(10, 10), eatenFruit.Value.Position);

        // Ensure it was removed
        Assert.Single(system.Fruits);
        Assert.Equal(new Vector2(50, 50), system.Fruits[0].Position);
    }

    [Fact]
    public void FruitSystem_TryEatFruit_ReturnsNull_WhenOutOfRange()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 5f, 10f, "Poppy");
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 1);

        var eatenFruit = system.TryEatFruit(new Vector2(20, 20), 5f);

        Assert.Null(eatenFruit);
        Assert.Single(system.Fruits); // Still there
    }

    [Fact]
    public void FruitSystem_TryEatFruit_ReturnsNull_WhenFruitIsDead()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 5f, 0f, "Poppy"); // dead fruit (lifetime=0)
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 1);

        var eatenFruit = system.TryEatFruit(new Vector2(10, 10), 5f);

        Assert.Null(eatenFruit);
        Assert.Single(system.Fruits); // Dead but still in array until next update
    }

    [Fact]
    public void FruitSystem_Update_HandlesAllFruitsDecayingSimultaneously()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 10f, 1f, "Poppy");
        fruitsArray[1] = new Fruit(new Vector2(20, 20), 10f, 1f, "Poppy");
        fruitsArray[2] = new Fruit(new Vector2(30, 30), 10f, 1f, "Poppy");
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 3);

        system.Update(eco, 1f); // Decays to 0
        system.Update(eco, 1f); // Removed

        Assert.Empty(system.Fruits);
    }

    [Fact]
    public void FruitSystem_TryEatFruit_SelectsFirstMatchingWhenMultipleFruitsAtSamePosition()
    {
        var system = new FruitSystem();
        var eco = new Ecosystem(32, 24, 7);
        system.Initialize(eco.World);

        var fruitsArray = (Fruit[])typeof(FruitSystem).GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(system)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 5f, 10f, "Poppy1");
        fruitsArray[1] = new Fruit(new Vector2(10, 10), 10f, 10f, "Poppy2");
        typeof(FruitSystem).GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.SetValue(system, 2);

        var eatenFruit = system.TryEatFruit(new Vector2(10, 10), 5f);

        Assert.NotNull(eatenFruit);
        Assert.Equal("Poppy1", eatenFruit.Value.PlantSpecies); // Should pick the first one matching
        Assert.Single(system.Fruits);
        Assert.Equal("Poppy2", system.Fruits[0].PlantSpecies); // Second one remains
    }

    [Fact]
    public void FruitSystem_Initialize_HandlesZeroMaxFruits()
    {
        // Setup an explicit configuration modification via Reflection since Config is data-driven
        var oldConfig = FruitConfig.Data;
        try
        {
            var configProp = typeof(FruitConfig).GetProperty("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            var zeroConfig = new FruitConfigData { MaxFruits = 0 };
            configProp!.SetValue(null, zeroConfig);

            var system = new FruitSystem();
            var eco = new Ecosystem(32, 24, 7);
            system.Initialize(eco.World);

            Assert.Empty(system.Fruits);

            // Should not crash on Update
            system.Update(eco, 1f);
            Assert.Empty(system.Fruits);
        }
        finally
        {
            // Restore config
            var configProp = typeof(FruitConfig).GetProperty("Data", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
            configProp!.SetValue(null, oldConfig);
        }
    }
}
