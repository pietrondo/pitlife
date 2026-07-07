using PitLife.Simulation;

namespace PitLife.Tests.Simulation.Generation;

public class TerrainRefinerTests
{
    [Fact]
    public void CopyEdgesForWrap_CopiesLeftToRightAndTopToBottom()
    {
        // Arrange
        var world = new World(10, 10, 42); // 10x10 world

        // Setup initial state: make left and top edges unique
        for (int y = 0; y < world.Height; y++)
        {
            world.Tiles[0, y] = new Tile(BiomeType.Grassland);
            world.Tiles[world.Width - 1, y] = new Tile(BiomeType.Desert);

            world.ElevationField[y * world.Width + 0] = y + 1.0f; // Left edge elevation
            world.ContinentMask[y * world.Width + 0] = y + 2.0f;
            world.RiverMask[y * world.Width + 0] = true;

            world.ElevationField[y * world.Width + (world.Width - 1)] = 0.0f; // Right edge elevation
            world.ContinentMask[y * world.Width + (world.Width - 1)] = 0.0f;
            world.RiverMask[y * world.Width + (world.Width - 1)] = false;
        }

        for (int x = 0; x < world.Width; x++)
        {
            world.Tiles[x, 0] = new Tile(BiomeType.Forest);
            world.Tiles[x, world.Height - 1] = new Tile(BiomeType.Snow);

            world.ElevationField[0 * world.Width + x] = x + 10.0f; // Top edge elevation
            world.ContinentMask[0 * world.Width + x] = x + 20.0f;
            world.RiverMask[0 * world.Width + x] = true;

            world.ElevationField[(world.Height - 1) * world.Width + x] = 0.0f; // Bottom edge elevation
            world.ContinentMask[(world.Height - 1) * world.Width + x] = 0.0f;
            world.RiverMask[(world.Height - 1) * world.Width + x] = false;
        }

        // Note: setting bottom/right corners twice, they will eventually take the top/left corner values.

        var refiner = new TerrainRefiner(world);

        // Act
        refiner.CopyEdgesForWrap();

        // Assert
        // Check horizontal wrap (left edge copied to right edge)
        for (int y = 0; y < world.Height; y++)
        {
            Assert.Equal(world.Tiles[0, y].Biome, world.Tiles[world.Width - 1, y].Biome);
            Assert.Equal(world.ElevationField[y * world.Width + 0], world.ElevationField[y * world.Width + (world.Width - 1)]);
            Assert.Equal(world.ContinentMask[y * world.Width + 0], world.ContinentMask[y * world.Width + (world.Width - 1)]);
            Assert.Equal(world.RiverMask[y * world.Width + 0], world.RiverMask[y * world.Width + (world.Width - 1)]);
        }

        // Check vertical wrap (top edge copied to bottom edge)
        for (int x = 0; x < world.Width; x++)
        {
            Assert.Equal(world.Tiles[x, 0].Biome, world.Tiles[x, world.Height - 1].Biome);
            Assert.Equal(world.ElevationField[0 * world.Width + x], world.ElevationField[(world.Height - 1) * world.Width + x]);
            Assert.Equal(world.ContinentMask[0 * world.Width + x], world.ContinentMask[(world.Height - 1) * world.Width + x]);
            Assert.Equal(world.RiverMask[0 * world.Width + x], world.RiverMask[(world.Height - 1) * world.Width + x]);
        }
    }
}
