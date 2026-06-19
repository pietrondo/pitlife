using PitLife.Simulation;

namespace PitLife.Tests;

public class TileTests
{
    [Theory]
    [InlineData(BiomeType.DeepOcean, true, false)]
    [InlineData(BiomeType.ShallowWater, true, false)]
    [InlineData(BiomeType.Grassland, false, true)]
    [InlineData(BiomeType.Snow, false, false)]
    public void IsPassableFor_RestrictsCreaturesToCompatibleBiomes(
        BiomeType biome,
        bool aquaticPassable,
        bool terrestrialPassable)
    {
        var tile = new Tile(biome);

        Assert.Equal(aquaticPassable, tile.IsPassableFor(true));
        Assert.Equal(terrestrialPassable, tile.IsPassableFor(false));
    }

    [Fact]
    public void Biome_WhenChanged_RecalculatesVegetation()
    {
        var tile = new Tile(BiomeType.Desert);

        tile.Biome = BiomeType.DenseForest;

        Assert.Equal(1f, tile.Vegetation);
    }
}
