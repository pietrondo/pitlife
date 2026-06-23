using System;

namespace PitLife.Simulation;

internal sealed class FeaturePlacer
{
    private readonly World _world;
    private readonly Random _rng;

    public FeaturePlacer(World world, Random rng)
    {
        _world = world;
        _rng = rng;
    }

    public void PlaceCoralReefs()
    {
        for (int y = 0; y < _world.Height; y++)
        {
            float latFactor = Math.Abs(y / (float)_world.Height - 0.5f) * 2f;
            if (latFactor > 0.7f) continue;
            for (int x = 0; x < _world.Width; x++)
            {
                if (_world.Tiles[x, y].Biome != BiomeType.ShallowWater) continue;
                if (_rng.NextDouble() < 0.25f)
                    _world.Tiles[x, y] = new Tile(BiomeType.CoralReef);
            }
        }
    }

    public void PlaceCaves()
    {
        for (int y = 1; y < _world.Height - 1; y++)
        {
            for (int x = 1; x < _world.Width - 1; x++)
            {
                if (_world.Tiles[x, y].Biome != BiomeType.Mountain) continue;
                int mountainNeighbors = 0;
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                        if (_world.Tiles[x + dx, y + dy].Biome == BiomeType.Mountain)
                            mountainNeighbors++;
                if (mountainNeighbors >= 7 && _rng.NextDouble() < 0.15f)
                    _world.Tiles[x, y] = new Tile(BiomeType.Cave);
            }
        }
    }

    public void PlaceVolcanoes()
    {
        int placed = 0;
        int maxVolcanoes = Math.Max(1, _world.Width * _world.Height / 3000);
        for (int attempt = 0; attempt < 100 && placed < maxVolcanoes; attempt++)
        {
            int x = _rng.Next(1, _world.Width - 1);
            int y = _rng.Next(1, _world.Height - 1);
            if (_world.Tiles[x, y].Biome == BiomeType.Mountain && _rng.NextDouble() < 0.2f)
            {
                _world.Tiles[x, y] = new Tile(BiomeType.Volcano);
                placed++;
            }
        }
    }
}
