using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class WorldGenOptionsTests
{
    [Fact]
    public void Pangea_IsTheDefaultPreset()
    {
        var opts = WorldGenOptions.Pangea();
        Assert.Equal(WorldGenPreset.Pangea, opts.Preset);
        Assert.Equal(1, opts.ContinentCount);
        Assert.Equal(96, opts.MapWidth);
        Assert.Equal(72, opts.MapHeight);
    }

    [Fact]
    public void Pangea_ProducesLargeCentralLandmass()
    {
        var world = new World(WorldGenOptions.Pangea() with { MapWidth = 96, MapHeight = 72 }, 42);
        int landCells = 0;
        for (int i = 0; i < world.Width * world.Height; i++)
            if (world.ContinentMask[i] > 0.5f) landCells++;
        double landRatio = landCells / (double)(world.Width * world.Height);
        Assert.InRange(landRatio, 0.50, 0.70);
    }

    [Fact]
    public void Archipelago_ProducesManySmallIslands()
    {
        var world = new World(WorldGenOptions.Archipelago() with { MapWidth = 96, MapHeight = 72 }, 42);
        int landCells = 0;
        for (int i = 0; i < world.Width * world.Height; i++)
            if (world.ContinentMask[i] > 0.5f) landCells++;
        double landRatio = landCells / (double)(world.Width * world.Height);
        Assert.InRange(landRatio, 0.45, 0.65);
    }

    [Fact]
    public void WetWorld_HasMoreOceanThanDryWorld()
    {
        var wet = new World(WorldGenOptions.WetWorld() with { MapWidth = 96, MapHeight = 72 }, 42);
        var dry = new World(WorldGenOptions.DryWorld() with { MapWidth = 96, MapHeight = 72 }, 42);
        int WetOcean() { int n = 0; for (int i = 0; i < wet.Width * wet.Height; i++) if (wet.ContinentMask[i] <= 0.5f) n++; return n; }
        int DryOcean() { int n = 0; for (int i = 0; i < dry.Width * dry.Height; i++) if (dry.ContinentMask[i] <= 0.5f) n++; return n; }
        Assert.True(WetOcean() > DryOcean(), "WetWorld should have more ocean cells than DryWorld");
    }

    [Fact]
    public void MapSize_ChangesWorldDimensions()
    {
        var small = new World(WorldGenOptions.Pangea() with { MapWidth = 64, MapHeight = 48 }, 42);
        var large = new World(WorldGenOptions.Pangea() with { MapWidth = 128, MapHeight = 96 }, 42);
        Assert.Equal(64, small.Width);
        Assert.Equal(48, small.Height);
        Assert.Equal(128, large.Width);
        Assert.Equal(96, large.Height);
    }

    [Fact]
    public void ContinentCount_ConstrainsToValidRange()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldGenOptions.Pangea() with { ContinentCount = 0 });
        Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldGenOptions.Pangea() with { ContinentCount = 7 });
        var opts = WorldGenOptions.Pangea() with { ContinentCount = 4 };
        Assert.Equal(4, opts.ContinentCount);
    }

    [Fact]
    public void SeaLevel_ConstrainsToValidRange()
    {
        Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldGenOptions.Pangea() with { SeaLevel = -0.1f });
        Assert.Throws<System.ArgumentOutOfRangeException>(() => WorldGenOptions.Pangea() with { SeaLevel = 1.1f });
        var opts = WorldGenOptions.Pangea() with { SeaLevel = 0.6f };
        Assert.Equal(0.6f, opts.SeaLevel);
    }

    [Fact]
    public void IslandSize_AffectsContinentShape()
    {
        var small = new World(WorldGenOptions.Pangea() with { IslandSize = IslandSize.Small, MapWidth = 96, MapHeight = 72 }, 42);
        var large = new World(WorldGenOptions.Pangea() with { IslandSize = IslandSize.Large, MapWidth = 96, MapHeight = 72 }, 42);
        int CountContiguous(float[] mask, int w, int h) { return mask.Count(c => c > 0.5f); }
        Assert.NotEqual(CountContiguous(small.ContinentMask, small.Width, small.Height),
                        CountContiguous(large.ContinentMask, large.Width, large.Height));
    }
}
