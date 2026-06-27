using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SpawnTests
{
    [Fact]
    public void SpawnByName_Plant_InLandBiome_Succeeds()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);

        Vector2 landPos = FindLandPosition(eco);
        Assert.True(landPos != Vector2.Zero);
        Assert.True(eco.SpawnByName("Clover", landPos));
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        Assert.Contains(eco.Creatures, c => c.Species == "Clover");
    }

    [Fact]
    public void SpawnByName_Shark_InDeepOcean_Succeeds()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);

        Vector2 deepPos = FindBiomePosition(eco, BiomeType.DeepOcean);
        Assert.True(deepPos != Vector2.Zero);
        Assert.True(eco.SpawnByName("Shark", deepPos));
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        Assert.Contains(eco.Creatures, c => c.Species == "Shark");
    }

    [Fact]
    public void SpawnByName_Plant_InOcean_Fails()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);

        Vector2 oceanPos = FindBiomePosition(eco, BiomeType.DeepOcean);
        Assert.True(oceanPos != Vector2.Zero);
        Assert.False(eco.SpawnByName("Clover", oceanPos));
    }

    [Fact]
    public void SpawnByName_UnknownSpecies_ReturnsFalse()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);

        Vector2 pos = FindLandPosition(eco);
        Assert.True(pos != Vector2.Zero);
        Assert.False(eco.SpawnByName("Unicorn", pos));
    }

    [Fact]
    public void SpawnByName_RespectsMaxCreatures()
    {
        var eco = new Ecosystem(64, 48, 42) { MaxCreatures = 3 };
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);
        Vector2 pos = FindLandPosition(eco);
        Assert.True(pos != Vector2.Zero);
        Assert.True(eco.SpawnByName("Clover", pos));
        Assert.True(eco.SpawnByName("Clover", pos));
        Assert.True(eco.SpawnByName("Clover", pos));
        Assert.False(eco.SpawnByName("Clover", pos));
    }

    private static Vector2 FindLandPosition(Ecosystem eco)
    {
        for (var x = 0; x < eco.World.Width; x++)
            for (var y = 0; y < eco.World.Height; y++)
            {
                var tile = eco.World.GetTile(x, y);
                if (tile.IsPassable && tile.Biome != BiomeType.DeepOcean && tile.Biome != BiomeType.ShallowWater)
                    return new Vector2((x + 0.5f) * eco.World.TileSize, (y + 0.5f) * eco.World.TileSize);
            }
        return Vector2.Zero;
    }

    private static Vector2 FindBiomePosition(Ecosystem eco, BiomeType target)
    {
        for (var x = 0; x < eco.World.Width; x++)
            for (var y = 0; y < eco.World.Height; y++)
            {
                if (eco.World.GetTile(x, y).Biome == target)
                    return new Vector2((x + 0.5f) * eco.World.TileSize, (y + 0.5f) * eco.World.TileSize);
            }
        return Vector2.Zero;
    }
}
