using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class CataclysmSystemTests
{
    [Fact]
    public void Reset_ClearsActiveState()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        sys.TriggerAt(eco, eco.Random, "Earthquake", Vector2.Zero);
        Assert.True(sys.IsActive);

        sys.Reset();

        Assert.False(sys.IsActive);
        Assert.Equal("", sys.ActiveEvent);
        Assert.Equal(1f, sys.GrassMultiplier);
    }

    [Fact]
    public void Update_WhenActive_DecrementsTimerAndDeactivates()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        sys.TriggerAt(eco, eco.Random, "Earthquake", Vector2.Zero);
        Assert.True(sys.IsActive);
        float initialTimer = sys.Timer;

        sys.Update(eco, 10f, eco.Random);
        Assert.True(sys.IsActive);
        Assert.True(sys.Timer < initialTimer);

        sys.Update(eco, sys.Timer + 1f, eco.Random);
        Assert.False(sys.IsActive);
        Assert.Equal("", sys.ActiveEvent);
    }

    [Fact]
    public void Update_EarthquakeAppliesScreenShake()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        sys.TriggerAt(eco, eco.Random, "Earthquake", Vector2.Zero);
        sys.Update(eco, 1f, eco.Random);

        Assert.NotEqual(Vector2.Zero, sys.ScreenShake);
    }

    [Fact]
    public void TriggerAt_Asteroid_ModifiesTerrain()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var centerPos = new Vector2(8 * eco.World.TileSize, 8 * eco.World.TileSize);
        sys.TriggerAt(eco, eco.Random, "Asteroid", centerPos);

        var tile = eco.World.GetTile(8, 8);
        Assert.Equal(BiomeType.Volcano, tile.Biome);
        Assert.Equal(0f, tile.GrassAmount);
        Assert.Equal(0.1f, tile.SoilNutrients);
    }

    [Fact]
    public void TriggerAt_Flood_UpdatesGrassMultiplier()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        sys.TriggerAt(eco, eco.Random, "Flood", new Vector2(8 * eco.World.TileSize, 8 * eco.World.TileSize));

        Assert.Equal(2.5f, sys.GrassMultiplier);
        var tile = eco.World.GetTile(8, 8);
        Assert.Equal(BiomeType.ShallowWater, tile.Biome);
    }

    [Fact]
    public void UpdateVolcanoes_DamagesGrassNearVolcano()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var tile = eco.World.GetTile(8, 8);
        tile.Biome = BiomeType.Volcano;

        var adjTile = eco.World.GetTile(8, 9);
        adjTile.GrassAmount = 100f;

        var mockRng = new Random(1);
        sys.UpdateVolcanoes(eco, 1000f, mockRng);

        Assert.True(adjTile.GrassAmount < 100f);
    }

    [Fact]
    public void TriggerManual_MassExtinction_ModifiesRadiusAndState()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        // Random 42 gave 2 ("Supervolcano")
        sys.TriggerManual(eco, new Random(42));

        Assert.True(sys.IsActive);
        Assert.Equal("Supervolcano", sys.ActiveEvent);
        Assert.True(sys.GrassMultiplier < 1f); // Supervolcano is 0.05f or Volcanic Winter 0.01f
        Assert.True(sys.ImpactRadius > 0);
    }

    [Fact]
    public void ChainReaction_EarthquakeNearWater_TriggersTsunami()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        // Place DeepOcean at 8,8
        eco.World.GetTile(8, 8).Biome = BiomeType.DeepOcean;

        sys.TriggerAt(eco, new Random(1), "Earthquake", new Vector2(8 * eco.World.TileSize, 8 * eco.World.TileSize));

        // Ensure it chained
        Assert.Equal("Tsunami", sys.ActiveEvent);
        Assert.Equal(2.5f, sys.GrassMultiplier);
    }
}
