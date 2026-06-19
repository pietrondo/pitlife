using PitLife.Simulation;

namespace PitLife.Tests;

public class WorldTests
{
    [Fact]
    public void Constructor_WithSameSeed_GeneratesSameBiomes()
    {
        var first = new World(64, 48, 42);
        var second = new World(64, 48, 42);

        for (int y = 0; y < first.Height; y++)
            for (int x = 0; x < first.Width; x++)
                Assert.Equal(first.Tiles[x, y].Biome, second.Tiles[x, y].Biome);
    }

    [Fact]
    public void Constructor_PreservesShallowWaterRiverPathsAfterSmoothing()
    {
        var world = new World(96, 72, 42);
        int shallowWaterTiles = 0;

        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
                if (world.Tiles[x, y].Biome == BiomeType.ShallowWater)
                    shallowWaterTiles++;

        Assert.True(shallowWaterTiles > world.Height,
            $"Expected river paths longer than one edge per river, found {shallowWaterTiles} shallow-water tiles.");
    }
}
