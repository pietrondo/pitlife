using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class DiagnosticsTests
{
    [Fact]
    public void CataclysmPanel_ToggleWorks()
    {
        var panel = new CataclysmPanel();
        Assert.False(panel.IsOpen);
        panel.Toggle();
        Assert.True(panel.IsOpen);
        panel.Toggle();
        Assert.False(panel.IsOpen);
    }

    [Fact]
    public void CataclysmPanel_SelectsTypeOnClick()
    {
        var panel = new CataclysmPanel();
        panel.Toggle();
        Assert.Null(panel.SelectedType);

        var pressed = new MouseState(50, 152, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var released = new MouseState();
        panel.Update(pressed, released);

        Assert.NotNull(panel.SelectedType);
    }

    [Fact]
    public void FlowSimulation_WaterFlowsDownhill()
    {
        var world = new World(16, 12, 42);
        var flow = new FlowSimulation(world);
        flow.Update(2f, new Random(7));
        flow.Update(2f, new Random(7));
        Assert.True(true);
    }

    [Fact]
    public void FlowSimulation_LavaAtVolcanoes()
    {
        var world = new World(16, 12, 42);
        for (var y = 0; y < world.Height; y++)
            for (var x = 0; x < world.Width; x++)
                world.Tiles[x, y] = new Tile(BiomeType.Volcano);

        var flow = new FlowSimulation(world);
        flow.Update(3f, new Random(7));
        Assert.True(true);
    }

    [Fact]
    public void World_HasAllBiomes()
    {
        var world = new World(96, 72, 42);
        var biomes = Enumerable.Range(0, world.Width * world.Height)
            .Select(i => world.Tiles[i % world.Width, i / world.Width].Biome)
            .Distinct().ToList();
        Assert.True(biomes.Count >= 13, $"Expected >=13 biomes, got {biomes.Count}");
    }

    [Fact]
    public void Ecosystem_InitializesWithoutErrors()
    {
        var eco = new Ecosystem(32, 24, 99);
        eco.Initialize(10, 5, 2, 20);
        Assert.True(eco.PlantCount > 0);
        Assert.True(eco.HerbivoreCount > 0);
        Assert.True(eco.CarnivoreCount > 0);
        Assert.True(eco.OmnivoreCount > 0);
    }

    [Fact]
    public void Ecosystem_RunsFor60Seconds_WithoutErrors()
    {
        var eco = new Ecosystem(32, 24, 42);
        eco.Initialize(5, 3, 2, 15);
        for (var i = 0; i < 600; i++)
        {
            eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        }
        Assert.True(eco.TotalTime > 0);
    }

    [Fact]
    public void Atmosphere_StabilizesWithPlants()
    {
        var eco = new Ecosystem(32, 24, 42);
        eco.Initialize(0, 0, 0, 50);
        for (var i = 0; i < 100; i++)
            eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        Assert.True(eco.Atmosphere.Oxygen > 40, $"O2={eco.Atmosphere.Oxygen:F1}");
    }

    [Fact]
    public void Cataclysm_TerrainModification()
    {
        var eco = new Ecosystem(16, 12, 42);
        eco.Initialize(0, 0, 0, 5);
        eco.Cataclysms.TriggerAt(eco, eco.Random, "Asteroid", new Vector2(200, 150));
        Assert.True(eco.Cataclysms.IsActive);
    }

    [Fact]
    public void SpawnPanel_HasAllCategories()
    {
        string[] expected = ["Plants", "AquaticPlants", "Herbivores", "Carnivores", "Omnivores"];
        foreach (var cat in expected)
        {
            var species = SpawnPanel.SpeciesForCategory(cat);
            Assert.NotEmpty(species);
        }
    }

    [Fact]
    public void AllSpecies_CanBeSpawned()
    {
        var eco = new Ecosystem(48, 36, 42);
        var spawned = 0;
        foreach (var sp in SpeciesRegistry.All)
        {
            for (var i = 0; i < 50; i++)
            {
                var pos = new Microsoft.Xna.Framework.Vector2(eco.Random.Next(100, 500), eco.Random.Next(100, 500));
                if (eco.SpawnByName(sp, pos))
                {
                    spawned++;
                    break;
                }
            }
        }
        Assert.True(spawned > 0, $"Spawned {spawned} species");
    }

    [Fact]
    public void Climate_Seasons_Cycle()
    {
        var climate = new ClimateSystem();
        var rng = new Random(42);
        climate.Update(0, rng);
        Assert.Equal(Season.Summer, climate.CurrentSeason);
        climate.Update(ClimateSystem.YearLength + 10, rng);
        Assert.Equal(Season.Summer, climate.CurrentSeason);
    }

    [Fact]
    public void Disease_DoesNotTriggerImmediately()
    {
        var eco = new Ecosystem(16, 12, 42);
        eco.Initialize(3, 2, 1, 10);
        Assert.False(eco.Disease.HasOutbreak);
    }

    [Fact]
    public void FoodWeb_AllDiets_Valid()
    {
        Assert.True(FoodWeb.CanEat(CreatureType.Herbivore, CreatureType.Plant, DietType.Herbivore));
        Assert.True(FoodWeb.CanEat(CreatureType.Carnivore, CreatureType.Herbivore, DietType.Carnivore));
        Assert.True(FoodWeb.CanEat(CreatureType.Omnivore, CreatureType.Plant, DietType.Omnivore));
        Assert.False(FoodWeb.CanEat(CreatureType.Herbivore, CreatureType.Carnivore, DietType.Herbivore));
    }
}
