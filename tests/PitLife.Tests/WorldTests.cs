using System.Collections.Generic;
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

    [Fact]
    public void World_InternalAccessors_AreAccessibleFromTests()
    {
        var world = new World(64, 48, 42);

        float[] continent = world.ContinentMask;
        float[] elevation = world.ElevationField;
        bool[] rivers = world.RiverMask;

        Assert.Equal(64 * 48, continent.Length);
        Assert.Equal(64 * 48, elevation.Length);
        Assert.Equal(64 * 48, rivers.Length);
    }

    [Fact]
    public void Constructor_ContinentMask_HasBothLandAndOcean()
    {
        int[] seeds = { 0, 1, 2, 42, 1337 };
        foreach (int seed in seeds)
        {
            var world = new World(64, 48, seed);
            int landCells = 0;
            for (int i = 0; i < world.ContinentMask.Length; i++)
                if (world.ContinentMask[i] > 0.5f)
                    landCells++;
            int total = world.ContinentMask.Length;
            Assert.True(landCells > 0, $"seed={seed}: no land cells (all ocean)");
            Assert.True(landCells < total, $"seed={seed}: no ocean cells (all land)");
        }
    }

    [Fact]
    public void Constructor_DifferentSeeds_ProduceDifferentTopologies()
    {
        var w0 = new World(64, 48, 0);
        var w1 = new World(64, 48, 1);
        var w2 = new World(64, 48, 2);
        int c0 = CountContinents(w0.ContinentMask, 64, 48);
        int c1 = CountContinents(w1.ContinentMask, 64, 48);
        int c2 = CountContinents(w2.ContinentMask, 64, 48);
        Assert.True(c0 != c1 || c1 != c2,
            $"Expected different continent counts for different seeds, got c0={c0}, c1={c1}, c2={c2}");
    }

    private static int CountContinents(float[] mask, int width, int height)
    {
        var visited = new bool[width * height];
        int count = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int i = y * width + x;
                if (visited[i] || mask[i] <= 0.5f) continue;
                count++;
                var queue = new Queue<int>();
                queue.Enqueue(i);
                visited[i] = true;
                while (queue.Count > 0)
                {
                    int c = queue.Dequeue();
                    int cx = c % width;
                    int cy = c / width;
                    if (cx > 0)            { int n = c - 1;     if (!visited[n] && mask[n] > 0.5f) { visited[n] = true; queue.Enqueue(n); } }
                    if (cx < width - 1)    { int n = c + 1;     if (!visited[n] && mask[n] > 0.5f) { visited[n] = true; queue.Enqueue(n); } }
                    if (cy > 0)            { int n = c - width; if (!visited[n] && mask[n] > 0.5f) { visited[n] = true; queue.Enqueue(n); } }
                    if (cy < height - 1)   { int n = c + width; if (!visited[n] && mask[n] > 0.5f) { visited[n] = true; queue.Enqueue(n); } }
                }
            }
        }
        return count;
    }
}
