using PitLife.Simulation;

namespace PitLife.Tests;

public class RiverSystemTests
{
    private World CreateTestWorld(int width, int height)
    {
        var world = new World(width, height, 42);

        Array.Clear(world.RiverMask, 0, world.RiverMask.Length);
        for (var i = 0; i < world.ContinentMask.Length; i++)
        {
            world.ContinentMask[i] = 1.0f; // All land by default
            world.ElevationField[i] = 0.5f; // Flat by default
        }
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                world.Tiles[x, y] = new Tile(BiomeType.Grassland);
            }
        }
        return world;
    }

    [Fact]
    public void CarveRivers_CapsElevationAtRiverLocations()
    {
        var world = CreateTestWorld(64, 64);

        // Create a slope down to the bottom right
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                world.ElevationField[y * 64 + x] = 1.0f - ((x + y) * 0.007f);
                // bottom right corner is ocean to prevent pruning
                if (x >= 60 && y >= 60)
                {
                    world.ContinentMask[y * 64 + x] = 0.0f;
                }
            }
        }

        var riverSystem = new RiverSystem(world);
        riverSystem.CarveRivers(42);

        var foundRiver = false;
        for (var i = 0; i < world.RiverMask.Length; i++)
        {
            if (world.RiverMask[i])
            {
                foundRiver = true;
                Assert.True(world.ElevationField[i] <= 0.18f,
                    $"Elevation at river cell {i} should be capped at 0.18, but was {world.ElevationField[i]}");
            }
        }
        Assert.True(foundRiver, "Expected at least one river cell to be created");
    }

    [Fact]
    public void CarveRivers_ModifiesRiverMaskAndTiles()
    {
        var world = CreateTestWorld(64, 64);

        // Create a slope down to the right, slightly angled to avoid exact straight lines that might distribute flow evenly
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                world.ElevationField[y * 64 + x] = 1.0f - ((x + y * 0.1f) * 0.015f);
                // Right edge is ocean
                if (x >= 60)
                {
                    world.ContinentMask[y * 64 + x] = 0.0f;
                }
            }
        }

        var riverSystem = new RiverSystem(world);
        riverSystem.CarveRivers(42);

        var riverCount = 0;
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                var idx = y * 64 + x;
                if (world.RiverMask[idx])
                {
                    riverCount++;
                    Assert.Equal(BiomeType.ShallowWater, world.Tiles[x, y].Biome);
                }
            }
        }
        Assert.True(riverCount > 0, "Expected at least one river cell to be created");
    }

    [Fact]
    public void CarveRivers_PrunesDisconnectedRivers()
    {
        var world = CreateTestWorld(64, 64);

        // Create a bowl shape, all land
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                world.ContinentMask[y * 64 + x] = 1.0f; // All land
                // Distance from center (31.5, 31.5)
                var dist = (float)Math.Sqrt(Math.Pow(x - 31.5, 2) + Math.Pow(y - 31.5, 2));
                world.ElevationField[y * 64 + x] = dist * 0.02f; // Lowest at center
            }
        }

        var riverSystem = new RiverSystem(world);
        riverSystem.CarveRivers(42);

        // Since there is no ocean connection, all rivers should be pruned
        for (var i = 0; i < world.RiverMask.Length; i++)
        {
            Assert.False(world.RiverMask[i], $"Expected river mask to be pruned at {i}");
        }
    }

    [Fact]
    public void CarveRivers_KeepsConnectedRivers()
    {
        var world = CreateTestWorld(64, 64);

        // Create a bowl shape, but center is ocean
        for (var y = 0; y < 64; y++)
        {
            for (var x = 0; x < 64; x++)
            {
                // Distance from center (31.5, 31.5)
                var dist = (float)Math.Sqrt(Math.Pow(x - 31.5, 2) + Math.Pow(y - 31.5, 2));
                world.ElevationField[y * 64 + x] = dist * 0.02f; // Lowest at center

                // Center is ocean
                if (dist < 8.0f)
                {
                    world.ContinentMask[y * 64 + x] = 0.0f;
                }
                else
                {
                    world.ContinentMask[y * 64 + x] = 1.0f;
                }
            }
        }

        var riverSystem = new RiverSystem(world);
        riverSystem.CarveRivers(42);

        // Since there is an ocean connection in the center (lowest point), rivers should be kept
        var foundRiver = false;
        for (var i = 0; i < world.RiverMask.Length; i++)
        {
            if (world.RiverMask[i])
            {
                foundRiver = true;
                break;
            }
        }
        Assert.True(foundRiver, "Expected rivers to be kept because they connect to the ocean");
    }
}
