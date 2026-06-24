using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PitLife.Simulation;

internal sealed class TerrainRefiner
{
    private readonly World _world;

    public TerrainRefiner(World world)
    {
        _world = world;
    }

    public void SmoothTerrain()
    {
        var tmp = new BiomeType[_world.Width, _world.Height];
        int w = _world.Width, h = _world.Height;

        Parallel.For(0, h, y =>
        {
            for (int x = 0; x < w; x++)
                tmp[x, y] = _world.Tiles[x, y].Biome;
        });

        Parallel.For(1, h - 1, y =>
        {
            for (int x = 1; x < w - 1; x++)
            {
                var b = _world.Tiles[x, y].Biome;
                if (b == BiomeType.DeepOcean || b == BiomeType.ShallowWater) continue;
                int matches = 0;
                if (_world.Tiles[x - 1, y].Biome == b) matches++;
                if (_world.Tiles[x + 1, y].Biome == b) matches++;
                if (_world.Tiles[x, y - 1].Biome == b) matches++;
                if (_world.Tiles[x, y + 1].Biome == b) matches++;
                tmp[x, y] = matches >= 2 ? b : MostCommonNeighbor(x, y);
            }
        });

        Parallel.For(1, h - 1, y =>
        {
            for (int x = 1; x < w - 1; x++)
                _world.Tiles[x, y].Biome = tmp[x, y];
        });
    }

    private BiomeType MostCommonNeighbor(int x, int y)
    {
        Span<BiomeType> nb = [_world.Tiles[x - 1, y].Biome, _world.Tiles[x + 1, y].Biome,
                             _world.Tiles[x, y - 1].Biome, _world.Tiles[x, y + 1].Biome];
        int bestCount = 0;
        BiomeType best = _world.Tiles[x, y].Biome;
        foreach (var b in nb)
        {
            int c = 0;
            foreach (var n in nb) if (n == b) c++;
            if (c > bestCount) { bestCount = c; best = b; }
        }
        return best;
    }

    public void CopyEdgesForWrap()
    {
        int w = _world.Width, h = _world.Height;
        Parallel.For(0, h, y =>
        {
            int iLeft = y * w + 0;
            int iRight = y * w + (w - 1);
            _world.Tiles[w - 1, y] = _world.Tiles[0, y];
            _world.ElevationField[iRight] = _world.ElevationField[iLeft];
            _world.ContinentMask[iRight] = _world.ContinentMask[iLeft];
            _world.RiverMask[iRight] = _world.RiverMask[iLeft];
        });
        Parallel.For(0, w, x =>
        {
            int iTop = 0 * w + x;
            int iBot = (h - 1) * w + x;
            _world.Tiles[x, h - 1] = _world.Tiles[x, 0];
            _world.ElevationField[iBot] = _world.ElevationField[iTop];
            _world.ContinentMask[iBot] = _world.ContinentMask[iTop];
            _world.RiverMask[iBot] = _world.RiverMask[iTop];
        });
    }

    public void EnsureAllBiomesPresent()
    {
        var allFifteen = new BiomeType[]
        {
            BiomeType.DeepOcean, BiomeType.ShallowWater, BiomeType.Beach,
            BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest,
            BiomeType.Desert, BiomeType.Savanna, BiomeType.Swamp,
            BiomeType.Mountain, BiomeType.Snow, BiomeType.Tundra,
            BiomeType.CoralReef, BiomeType.Cave, BiomeType.Volcano
        };

        var present = new HashSet<BiomeType>();
        for (int y = 0; y < _world.Height; y++)
            for (int x = 0; x < _world.Width; x++)
                present.Add(_world.Tiles[x, y].Biome);

        if (!present.Contains(BiomeType.Grassland))
        {
            bool placed = false;
            for (int y = 0; y < _world.Height && !placed; y++)
                for (int x = 0; x < _world.Width && !placed; x++)
                {
                    int i = y * _world.Width + x;
                    if (_world.RiverMask[i]) continue;
                    var b = _world.Tiles[x, y].Biome;
                    if (b == BiomeType.DeepOcean || b == BiomeType.ShallowWater) continue;
                    _world.Tiles[x, y] = new Tile(BiomeType.Grassland);
                    present.Add(BiomeType.Grassland);
                    placed = true;
                }
            if (!placed)
                for (int y = 0; y < _world.Height && !placed; y++)
                    for (int x = 0; x < _world.Width && !placed; x++)
                    {
                        if (_world.RiverMask[y * _world.Width + x]) continue;
                        _world.Tiles[x, y] = new Tile(BiomeType.Grassland);
                        present.Add(BiomeType.Grassland);
                        placed = true;
                    }
        }

        foreach (var biome in allFifteen)
        {
            if (present.Contains(biome)) continue;
            for (int y = 0; y < _world.Height && !present.Contains(biome); y++)
            {
                for (int x = 0; x < _world.Width && !present.Contains(biome); x++)
                {
                    int i = y * _world.Width + x;
                    if (_world.RiverMask[i]) continue;
                    if (_world.Tiles[x, y].Biome != BiomeType.Grassland) continue;
                    _world.Tiles[x, y] = new Tile(biome);
                    present.Add(biome);
                }
            }
        }
    }
}
