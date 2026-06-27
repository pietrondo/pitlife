using PitLife.Simulation;

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
        var landCells = 0;
        for (var i = 0; i < world.Width * world.Height; i++)
            if (world.ContinentMask[i] > 0.5f) landCells++;
        var landRatio = landCells / (double)(world.Width * world.Height);
        Assert.InRange(landRatio, 0.50, 0.70);
    }

    [Fact]
    public void Archipelago_ProducesManySmallIslands()
    {
        var world = new World(WorldGenOptions.Archipelago() with { MapWidth = 96, MapHeight = 72 }, 42);
        var landCells = 0;
        for (var i = 0; i < world.Width * world.Height; i++)
            if (world.ContinentMask[i] > 0.5f) landCells++;
        var landRatio = landCells / (double)(world.Width * world.Height);
        Assert.InRange(landRatio, 0.15, 0.40);
    }

    [Fact]
    public void WetWorld_HasMoreOceanThanDryWorld()
    {
        var wet = new World(WorldGenOptions.WetWorld() with { MapWidth = 96, MapHeight = 72 }, 42);
        var dry = new World(WorldGenOptions.DryWorld() with { MapWidth = 96, MapHeight = 72 }, 42);
        int WetOcean() { var n = 0; for (var i = 0; i < wet.Width * wet.Height; i++) if (wet.ContinentMask[i] <= 0.5f) n++; return n; }
        int DryOcean() { var n = 0; for (var i = 0; i < dry.Width * dry.Height; i++) if (dry.ContinentMask[i] <= 0.5f) n++; return n; }
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

    [Fact]
    public void MoreContinents_ProducesDifferentMap()
    {
        var one = new World(WorldGenOptions.Pangea() with { ContinentCount = 1, MapWidth = 96, MapHeight = 72 }, 42);
        var four = new World(WorldGenOptions.Pangea() with { ContinentCount = 4, MapWidth = 96, MapHeight = 72 }, 42);
        var six = new World(WorldGenOptions.Pangea() with { ContinentCount = 6, MapWidth = 96, MapHeight = 72 }, 42);

        int[] Hash(float[] mask) => mask.Select(c => c > 0.5f ? 1 : 0).ToArray();
        var h1 = Hash(one.ContinentMask);
        var h4 = Hash(four.ContinentMask);
        var h6 = Hash(six.ContinentMask);

        Assert.False(h1.SequenceEqual(h4), "1 and 4 continents should produce different maps");
        Assert.False(h1.SequenceEqual(h6), "1 and 6 continents should produce different maps");
        Assert.False(h4.SequenceEqual(h6), "4 and 6 continents should produce different maps");
    }

    [Fact]
    public void Ecosystem_Constructor_PassesOptionsToWorld()
    {
        var opts = WorldGenOptions.Pangea() with { MapWidth = 64, MapHeight = 48, ContinentCount = 3, SeaLevel = 0.5f };
        var eco = new Ecosystem(opts, 42);
        Assert.Equal(64, eco.World.Width);
        Assert.Equal(48, eco.World.Height);
        Assert.Equal(42, eco.Seed);
    }

    [Fact]
    public void DifferentSeaLevels_ChangeLandRatio()
    {
        var shallow = new World(WorldGenOptions.Pangea() with { SeaLevel = 0.25f, MapWidth = 96, MapHeight = 72 }, 99);
        var deep = new World(WorldGenOptions.Pangea() with { SeaLevel = 0.75f, MapWidth = 96, MapHeight = 72 }, 99);
        int LandCells(float[] mask) => mask.Count(c => c > 0.5f);
        var shallowLand = LandCells(shallow.ContinentMask);
        var deepLand = LandCells(deep.ContinentMask);
        Assert.True(shallowLand > deepLand,
            $"Shallow sea (0.25) should have more land ({shallowLand}) than deep sea 0.75 ({deepLand})");
    }
}
