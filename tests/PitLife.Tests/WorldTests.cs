using System.Collections.Generic;
using System.Linq;
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
        // With deterministic continent algorithm, masks should differ (positions not count)
        Assert.False(w0.ContinentMask.SequenceEqual(w1.ContinentMask),
            "Different seeds should produce different continent masks");
    }

    [Fact]
    public void Constructor_ElevationField_OceanCellsHaveLowElevation()
    {
        var world = new World(64, 48, 42);
        var hasOcean = false;
        var hasLand = false;
        for (int i = 0; i < world.ContinentMask.Length; i++)
        {
            if (world.ContinentMask[i] <= 0.3f)
            {
                hasOcean = true;
                Assert.True(world.ElevationField[i] >= 0f && world.ElevationField[i] < 0.5f, 
                    $"Cella oceanica {i} ha elev={world.ElevationField[i]}, atteso in [0, 0.5)");
            }
            else
            {
                hasLand = true;
                Assert.True(world.ElevationField[i] > 0f, $"Cella terrestre {i} (mask={world.ContinentMask[i]}) ha elev={world.ElevationField[i]}, atteso > 0");
            }
        }
        Assert.True(hasOcean, "Attesa almeno una cella oceanica con seed=42 64x48");
        Assert.True(hasLand, "Attesa almeno una cella terrestre con seed=42 64x48 (Pangea variant)");
    }

    [Fact]
    public void Constructor_ElevationField_IsDeterministic()
    {
        var first = new World(64, 48, 42);
        var second = new World(64, 48, 42);
        Assert.Equal(first.ElevationField, second.ElevationField);
    }

    [Fact]
    public void Constructor_RiverMask_HasAtLeastOneRiver()
    {
        var world = new World(64, 48, 42);
        var hasRiver = false;
        for (int i = 0; i < world.RiverMask.Length; i++)
        {
            if (world.RiverMask[i])
            {
                hasRiver = true;
                break;
            }
        }
        Assert.True(hasRiver, "Attesa almeno una cella fluviale con seed=42 64x48");
    }

    [Fact]
    public void Constructor_RiverMask_IsDeterministic()
    {
        var first = new World(64, 48, 42);
        var second = new World(64, 48, 42);
        Assert.Equal(first.RiverMask, second.RiverMask);
    }

    [Fact]
    public void Constructor_RiverMask_AllCellsConnectedToOceanViaValleys()
    {
        var world = new World(96, 72, 42);
        int W = world.Width, H = world.Height;
        var visited = new bool[W * H];
        var queue = new Queue<int>();

        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int i = y * W + x;
                if (!world.RiverMask[i]) continue;
                bool hasOceanNeighbor = false;
                if (x > 0     && world.ContinentMask[i - 1] <= 0.5f) hasOceanNeighbor = true;
                if (x < W - 1 && world.ContinentMask[i + 1] <= 0.5f) hasOceanNeighbor = true;
                if (y > 0     && world.ContinentMask[i - W] <= 0.5f) hasOceanNeighbor = true;
                if (y < H - 1 && world.ContinentMask[i + W] <= 0.5f) hasOceanNeighbor = true;
                if (hasOceanNeighbor)
                {
                    visited[i] = true;
                    queue.Enqueue(i);
                }
            }
        }

        while (queue.Count > 0)
        {
            int c = queue.Dequeue();
            int cx = c % W, cy = c / W;
            if (cx > 0)         { int n = c - 1; if (!visited[n] && (world.RiverMask[n] || world.ElevationField[n] <= 0.5f)) { visited[n] = true; queue.Enqueue(n); } }
            if (cx < W - 1)     { int n = c + 1; if (!visited[n] && (world.RiverMask[n] || world.ElevationField[n] <= 0.5f)) { visited[n] = true; queue.Enqueue(n); } }
            if (cy > 0)         { int n = c - W; if (!visited[n] && (world.RiverMask[n] || world.ElevationField[n] <= 0.5f)) { visited[n] = true; queue.Enqueue(n); } }
            if (cy < H - 1)     { int n = c + W; if (!visited[n] && (world.RiverMask[n] || world.ElevationField[n] <= 0.5f)) { visited[n] = true; queue.Enqueue(n); } }
        }

        var disconnected = new List<int>();
        for (int i = 0; i < world.RiverMask.Length; i++)
            if (world.RiverMask[i] && !visited[i])
                disconnected.Add(i);

        Assert.True(disconnected.Count == 0,
            $"Attese 0 celle fluviali scollegate dall'oceano via valli in 96x72 seed=42, trovate {disconnected.Count}: indici [{string.Join(",", disconnected.Take(10))}...]");
    }

    [Fact]
    public void Constructor_Biomes_AllTwelveTypesPresent()
    {
        var world = new World(96, 72, 42);
        var present = new HashSet<BiomeType>();
        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
                present.Add(world.Tiles[x, y].Biome);

        var allTwelve = new[]
        {
            BiomeType.DeepOcean, BiomeType.ShallowWater, BiomeType.Beach,
            BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest,
            BiomeType.Desert, BiomeType.Savanna, BiomeType.Swamp,
            BiomeType.Mountain, BiomeType.Snow, BiomeType.Tundra
        };
        var missing = new List<BiomeType>();
        foreach (var b in allTwelve)
            if (!present.Contains(b))
                missing.Add(b);
        Assert.True(missing.Count == 0,
            $"Attesi tutti i 12 biomi in world 96x72 seed=42, mancanti: {string.Join(", ", missing)}");
    }



    [Fact]
    public void Constructor_Biomes_RiverCells_AreShallowWater()
    {
        var world = new World(96, 72, 42);
        var violations = new List<string>();
        for (int i = 0; i < world.RiverMask.Length; i++)
        {
            if (!world.RiverMask[i]) continue;
            int x = i % world.Width;
            int y = i / world.Width;
            if (world.Tiles[x, y].Biome != BiomeType.ShallowWater)
                violations.Add($"{i}({world.Tiles[x, y].Biome})");
        }
        Assert.True(violations.Count == 0,
            $"Attese 0 celle fluviali con bioma != ShallowWater in 96x72 seed=42, trovate {violations.Count}: indici [{string.Join(",", violations.Take(10))}...]");
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
