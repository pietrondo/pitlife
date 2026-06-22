using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class CreatureSpawnerTests
{
    [Fact]
    public void Spawner_CanSpawn_ValidatesBiome()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);
        var spawner = new CreatureSpawner(eco);

        var landPos = FindLandPosition(eco);
        Assert.True(spawner.CanSpawn("Deer", landPos));
        Assert.False(spawner.CanSpawn("Clover", FindBiomePosition(eco, BiomeType.DeepOcean)));
    }

    [Fact]
    public void Spawner_CreatesCorrectSubclass()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);
        var spawner = new CreatureSpawner(eco);

        var landPos = FindLandPosition(eco);
        Assert.True(spawner.SpawnAt<Herbivore>("Deer", landPos));

        eco.Tick(new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1)));
        var deer = Assert.Single(eco.Creatures, c => c.Species == "Deer");
        Assert.IsType<Herbivore>(deer);
    }

    [Fact]
    public void Spawner_RespectsMaxCreatures()
    {
        var eco = new Ecosystem(64, 48, 42) { MaxCreatures = 2 };
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);
        var spawner = new CreatureSpawner(eco);

        var landPos = FindLandPosition(eco);
        Assert.True(spawner.SpawnByName("Clover", landPos));
        Assert.True(spawner.SpawnByName("Clover", landPos));
        Assert.False(spawner.SpawnByName("Clover", landPos));
    }

    private static Vector2 FindLandPosition(Ecosystem eco)
    {
        for (int x = 0; x < eco.World.Width; x++)
        for (int y = 0; y < eco.World.Height; y++)
        {
            var tile = eco.World.GetTile(x, y);
            if (tile.IsPassable && tile.Biome != BiomeType.DeepOcean && tile.Biome != BiomeType.ShallowWater)
                return new Vector2((x + 0.5f) * eco.World.TileSize, (y + 0.5f) * eco.World.TileSize);
        }
        return Vector2.Zero;
    }

    private static Vector2 FindBiomePosition(Ecosystem eco, BiomeType target)
    {
        for (int x = 0; x < eco.World.Width; x++)
        for (int y = 0; y < eco.World.Height; y++)
            if (eco.World.GetTile(x, y).Biome == target)
                return new Vector2((x + 0.5f) * eco.World.TileSize, (y + 0.5f) * eco.World.TileSize);
        return Vector2.Zero;
    }
}
