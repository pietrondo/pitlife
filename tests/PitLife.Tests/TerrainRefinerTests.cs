using System;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class TerrainRefinerTests
{
    private World CreateTestWorld(int width, int height, BiomeType defaultBiome = BiomeType.Grassland)
    {
        var world = new World(width, height, 42);
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                world.Tiles[x, y] = new Tile(defaultBiome);
            }
        }
        return world;
    }

    [Fact]
    public void SmoothTerrain_IgnoresEdges()
    {
        var world = CreateTestWorld(5, 5, BiomeType.Grassland);

        // Setup edge tiles to something else
        for (int x = 0; x < 5; x++)
        {
            world.Tiles[x, 0].Biome = BiomeType.Desert;
            world.Tiles[x, 4].Biome = BiomeType.Desert;
        }
        for (int y = 0; y < 5; y++)
        {
            world.Tiles[0, y].Biome = BiomeType.Desert;
            world.Tiles[4, y].Biome = BiomeType.Desert;
        }

        var refiner = new TerrainRefiner(world);
        refiner.SmoothTerrain();

        // Check that edges were not modified
        for (int x = 0; x < 5; x++)
        {
            Assert.Equal(BiomeType.Desert, world.Tiles[x, 0].Biome);
            Assert.Equal(BiomeType.Desert, world.Tiles[x, 4].Biome);
        }
        for (int y = 0; y < 5; y++)
        {
            Assert.Equal(BiomeType.Desert, world.Tiles[0, y].Biome);
            Assert.Equal(BiomeType.Desert, world.Tiles[4, y].Biome);
        }
    }

    [Fact]
    public void SmoothTerrain_IgnoresWaterBiomes()
    {
        var world = CreateTestWorld(5, 5, BiomeType.Grassland);
        // Place DeepOcean surrounded by Grassland
        world.Tiles[2, 2].Biome = BiomeType.DeepOcean;
        // Place ShallowWater surrounded by Grassland
        world.Tiles[1, 1].Biome = BiomeType.ShallowWater;

        var refiner = new TerrainRefiner(world);
        refiner.SmoothTerrain();

        // Water biomes should be ignored and not changed to Grassland
        Assert.Equal(BiomeType.DeepOcean, world.Tiles[2, 2].Biome);
        Assert.Equal(BiomeType.ShallowWater, world.Tiles[1, 1].Biome);
    }

    [Fact]
    public void SmoothTerrain_PreservesTile_WhenTwoOrMoreNeighborsMatch()
    {
        var world = CreateTestWorld(5, 5, BiomeType.Grassland);
        world.Tiles[2, 2].Biome = BiomeType.Forest; // Target
        world.Tiles[2, 1].Biome = BiomeType.Forest; // Neighbor 1
        world.Tiles[2, 3].Biome = BiomeType.Forest; // Neighbor 2

        var refiner = new TerrainRefiner(world);
        refiner.SmoothTerrain();

        // 2,2 has 2 Forest neighbors, so it should retain Forest
        Assert.Equal(BiomeType.Forest, world.Tiles[2, 2].Biome);
    }

    [Fact]
    public void SmoothTerrain_ChangesTile_ToMostCommonNeighbor()
    {
        var world = CreateTestWorld(5, 5, BiomeType.Grassland);
        world.Tiles[2, 2].Biome = BiomeType.Forest; // Target
        // Neighbors: 3 Desert, 1 Grassland
        world.Tiles[2, 1].Biome = BiomeType.Desert;
        world.Tiles[2, 3].Biome = BiomeType.Desert;
        world.Tiles[1, 2].Biome = BiomeType.Desert;
        world.Tiles[3, 2].Biome = BiomeType.Grassland;

        var refiner = new TerrainRefiner(world);
        refiner.SmoothTerrain();

        // 2,2 has 0 Forest neighbors, so it should be smoothed to the most common neighbor (Desert)
        Assert.Equal(BiomeType.Desert, world.Tiles[2, 2].Biome);
    }
}
