using System;
using Moq;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class CataclysmSystemTests
{
    [Fact]
    public void Reset_ClearsActiveState()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.NextDouble()).Returns(0.5);

        sys.TriggerAt(eco, mockRng.Object, "Earthquake", Vector2.Zero);
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

        var mockRng = new Mock<Random>();
        sys.TriggerAt(eco, mockRng.Object, "Earthquake", Vector2.Zero);
        Assert.True(sys.IsActive);
        float initialTimer = sys.Timer;

        sys.Update(eco, 10f, mockRng.Object);
        Assert.True(sys.IsActive);
        Assert.True(sys.Timer < initialTimer);

        sys.Update(eco, sys.Timer + 1f, mockRng.Object);
        Assert.False(sys.IsActive);
        Assert.Equal("", sys.ActiveEvent);
    }

    [Fact]
    public void TriggerAt_Asteroid_ModifiesTerrainAndValues()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var centerPos = new Vector2(8 * eco.World.TileSize, 8 * eco.World.TileSize);
        var mockRng = new Mock<Random>();

        sys.TriggerAt(eco, mockRng.Object, "Asteroid", centerPos);

        var tile = eco.World.GetTile(8, 8);
        Assert.Equal(BiomeType.Volcano, tile.Biome);
        Assert.Equal(0f, tile.GrassAmount);
        Assert.Equal(0.1f, tile.SoilNutrients);
        Assert.Equal("Asteroid", sys.ActiveEvent);
        Assert.True(sys.IsActive);
    }

    [Fact]
    public void TriggerAt_Flood_UpdatesGrassMultiplier()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var mockRng = new Mock<Random>();
        sys.TriggerAt(eco, mockRng.Object, "Flood", new Vector2(8 * eco.World.TileSize, 8 * eco.World.TileSize));

        Assert.Equal(2.5f, sys.GrassMultiplier);
        var tile = eco.World.GetTile(8, 8);
        Assert.Equal(BiomeType.ShallowWater, tile.Biome);
    }

    [Fact]
    public void TriggerAt_InvalidCataclysm_DoesNotThrowButIgnores()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);
        var mockRng = new Mock<Random>();

        sys.TriggerAt(eco, mockRng.Object, "NonExistentCataclysm", Vector2.Zero);

        Assert.Equal("Asteroid", sys.ActiveEvent);
        Assert.True(sys.IsActive);
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

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.NextDouble()).Returns(0.0);
        mockRng.Setup(r => r.Next(2, 4)).Returns(3);

        sys.UpdateVolcanoes(eco, 1000f, mockRng.Object);

        Assert.True(adjTile.GrassAmount < 100f);
    }

    [Fact]
    public void TriggerManual_PicksRandomEvent()
    {
        var sys = new CataclysmSystem();
        var eco = new Ecosystem(16, 16, 42);
        eco.Initialize(0, 0, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(2); // Supervolcano

        sys.TriggerManual(eco, mockRng.Object);

        Assert.True(sys.IsActive);
        Assert.Equal("Supervolcano", sys.ActiveEvent);
        Assert.True(sys.ImpactRadius > 0);
    }
}
