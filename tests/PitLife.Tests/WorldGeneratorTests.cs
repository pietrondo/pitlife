using System.Linq;
using PitLife.Simulation;

namespace PitLife.Tests;

public class WorldGeneratorTests
{
    [Fact]
    public void Generator_ProducesWorld_WithAllBiomes()
    {
        var world = new World(96, 72, 42);
        var biomes = Enumerable.Range(0, world.Width * world.Height)
            .Select(i => world.Tiles[i % world.Width, i / world.Width].Biome)
            .Distinct().ToList();
        Assert.Equal(12, biomes.Count);
    }

    [Fact]
    public void Generator_Deterministic_SameSeedSameBiomes()
    {
        var w1 = new World(64, 48, 42);
        var w2 = new World(64, 48, 42);
        for (int y = 0; y < w1.Height; y++)
            for (int x = 0; x < w1.Width; x++)
                Assert.Equal(w1.Tiles[x, y].Biome, w2.Tiles[x, y].Biome);
    }

    [Fact]
    public void Generator_DifferentSeeds_DifferentMaps()
    {
        var w1 = new World(64, 48, 1);
        var w2 = new World(64, 48, 999);
        int diffs = 0;
        for (int y = 0; y < w1.Height; y++)
            for (int x = 0; x < w1.Width; x++)
                if (w1.Tiles[x, y].Biome != w2.Tiles[x, y].Biome) diffs++;
        Assert.True(diffs > 100, $"Different seeds should give mostly different maps, got {diffs} diffs");
    }

    [Fact]
    public void Generator_ProducesRivers()
    {
        var world = new World(128, 96, 42);
        int riverCells = 0;
        for (int i = 0; i < world.Width * world.Height; i++)
            if (world.RiverMask[i]) riverCells++;
        Assert.True(riverCells > 0, "Should produce at least some river cells");
    }
}
