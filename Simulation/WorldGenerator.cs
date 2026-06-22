using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class WorldGenerator
{
    private readonly World _world;
    private readonly Random _rng;
    private float _baseHeight;

    public WorldGenerator(World world, int seed)
    {
        _world = world;
        _rng = new Random(seed);
    }

    public void Generate()
    {
        GenerateContinentMask(new Random(_rng.Next()));
        float[,] warp = GenerateFBM(_world.Width, _world.Height, 2, 2f, 1f, new Random(_rng.Next()));
        float[,] elevation = GenerateFBMWarped(_world.Width, _world.Height, 6, 1.4f, 0.55f, warp, 3f, new Random(_rng.Next()));
        float[,] moisture = GenerateFBMWarped(_world.Width, _world.Height, 5, 1.0f, 0.50f, warp, 2f, new Random(_rng.Next()));
        float[,] temperature = GenerateFBM(_world.Width, _world.Height, 3, 0.6f, 0.60f, new Random(_rng.Next()));

        for (int y = 0; y < _world.Height; y++)
        {
            float latFactor = Math.Abs(y / (float)_world.Height - 0.5f) * 2f;
            for (int x = 0; x < _world.Width; x++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];
                float t = temperature[x, y] * (1f - latFactor * 0.45f);

                float distX = x / (float)_world.Width - 0.5f;
                float distY = y / (float)_world.Height - 0.5f;
                e -= (distX * distX + distY * distY) * 0.55f;

                if (e < 0.22f && m > 0.3f) e = 0.18f;

                float mask = _world.ContinentMask[y * _world.Width + x];
                // Boost land elevation and reduce water areas
                float elev = mask > 0.3f  // Lowered threshold from 0.5 to 0.3
                    ? mask * _baseHeight * 1.2f + warp[x, y] * mask * 0.3f  // Boosted multipliers
                    : mask * 0.15f;  // Give shallow water some elevation instead of 0
                _world.ElevationField[y * _world.Width + x] = elev;

                BiomeType biome = AssignBiome(elev, m, t);
                _world.Tiles[x, y] = new Tile(biome);
            }
        }

        PlaceCoralReefs();
        CarveRivers(_rng.Next());
        SmoothTerrain();
        EnsureAllBiomesPresent();
    }

    private void EnsureAllBiomesPresent()
    {
        var allTwelve = new BiomeType[]
        {
            BiomeType.DeepOcean, BiomeType.ShallowWater, BiomeType.Beach,
            BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest,
            BiomeType.Desert, BiomeType.Savanna, BiomeType.Swamp,
            BiomeType.Mountain, BiomeType.Snow, BiomeType.Tundra,
            BiomeType.CoralReef
        };

        var present = new HashSet<BiomeType>();
        for (int y = 0; y < _world.Height; y++)
            for (int x = 0; x < _world.Width; x++)
                present.Add(_world.Tiles[x, y].Biome);

        foreach (var biome in allTwelve)
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

    private void PlaceCoralReefs()
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

    private void GenerateContinentMask(Random rng)
    {
        int variant = _rng.Next(0, 3);
        int cellCount;
        float cellRadius;
        switch (variant)
        {
            case 0: cellCount = 1; cellRadius = 0.55f; break;  // Was 0.45
            case 1: cellCount = 4; cellRadius = 0.35f; break;  // Was 0.28
            default: cellCount = 6; cellRadius = 0.24f; break;  // Was 0.18
        }

        _baseHeight = 0.4f + (float)rng.NextDouble() * 0.6f;

        Vector2[] centers = new Vector2[cellCount];
        if (cellCount == 1) centers[0] = new Vector2(0.5f, 0.5f);
        else if (cellCount == 4)
        {
            centers[0] = new Vector2(0.2f, 0.2f);
            centers[1] = new Vector2(0.8f, 0.2f);
            centers[2] = new Vector2(0.2f, 0.8f);
            centers[3] = new Vector2(0.8f, 0.8f);
        }
        else
        {
            centers[0] = new Vector2(0.3f, 0.2f);
            centers[1] = new Vector2(0.7f, 0.2f);
            centers[2] = new Vector2(0.2f, 0.5f);
            centers[3] = new Vector2(0.8f, 0.5f);
            centers[4] = new Vector2(0.3f, 0.8f);
            centers[5] = new Vector2(0.7f, 0.8f);
        }

        float jitterRange = 0.05f;
        for (int i = 0; i < cellCount; i++)
        {
            centers[i].X += (float)(rng.NextDouble() * 2.0 - 1.0) * jitterRange;
            centers[i].Y += (float)(rng.NextDouble() * 2.0 - 1.0) * jitterRange;
            centers[i].X = Math.Clamp(centers[i].X, 0.1f, 0.9f);
            centers[i].Y = Math.Clamp(centers[i].Y, 0.1f, 0.9f);
        }

        float warpStrength = 0.6f + (float)rng.NextDouble() * 0.6f;
        float[,] warpU = GenerateFBM(_world.Width, _world.Height, 2, 16f, 0.5f, new Random(rng.Next()));
        float[,] warpV = GenerateFBM(_world.Width, _world.Height, 2, 16f, 0.5f, new Random(rng.Next()));

        for (int y = 0; y < _world.Height; y++)
            for (int x = 0; x < _world.Width; x++)
            {
                float nx = x / (float)_world.Width;
                float ny = y / (float)_world.Height;
                float wu = (warpU[x, y] - 0.5f) * warpStrength;
                float wv = (warpV[x, y] - 0.5f) * warpStrength;
                float wx = Math.Clamp(nx + wu, 0f, 1f);
                float wy = Math.Clamp(ny + wv, 0f, 1f);
                float minDist = float.MaxValue;
                for (int i = 0; i < cellCount; i++)
                {
                    float dx = wx - centers[i].X;
                    float dy = wy - centers[i].Y;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < minDist) minDist = d;
                }
                float mask = Math.Clamp(1f - minDist / cellRadius, 0f, 1f);
                _world.ContinentMask[y * _world.Width + x] = mask;
            }
    }

    private static BiomeType AssignBiome(float e, float m, float t)
    {
        // Raised water thresholds to create more land
        if (e < 0.08f) return BiomeType.DeepOcean;      // Was 0.13
        if (e < 0.16f) return BiomeType.ShallowWater;   // Was 0.22
        if (e < 0.22f) return BiomeType.Beach;          // Was 0.27
        if (e > 0.75f) return e > 0.88f || t < 0.15f ? BiomeType.Snow : BiomeType.Mountain;
        if (e > 0.60f) return t < 0.25f || m < 0.25f ? BiomeType.Tundra : BiomeType.Mountain;
        if (e < 0.28f && m > 0.55f) return BiomeType.Swamp;
        if (t < 0.18f) return m < 0.45f ? BiomeType.Tundra : BiomeType.Grassland;
        if (t < 0.32f) return m < 0.30f ? BiomeType.Tundra : BiomeType.Grassland;
        if (m < 0.12f) return BiomeType.Desert;
        if (m < 0.28f) return BiomeType.Savanna;
        if (m < 0.52f) return BiomeType.Grassland;
        if (m < 0.72f) return BiomeType.Forest;
        return BiomeType.DenseForest;
    }

    private void CarveRivers(int seed)
    {
        var rng = new Random(seed);
        int riverThreshold = 3 + (int)(rng.NextDouble() * 8f);
        int W = _world.Width, H = _world.Height;
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        float[] dd = { 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f };

        int[] flowDir = new int[W * H];
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                int idx = y * W + x;
                float e = _world.ElevationField[idx];
                int bestDir = -1;
                float bestDrop = 0f;
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    float ne = _world.ElevationField[ny * W + nx];
                    float drop = (e - ne) / dd[d];
                    if (drop > bestDrop) { bestDrop = drop; bestDir = d; }
                }
                flowDir[idx] = bestDir;
            }

        int[] cells = new int[W * H];
        for (int i = 0; i < W * H; i++) cells[i] = i;
        Array.Sort(cells, (a, b) => _world.ElevationField[b].CompareTo(_world.ElevationField[a]));

        int[] flowAccum = new int[W * H];
        for (int i = 0; i < cells.Length; i++)
        {
            int idx = cells[i];
            flowAccum[idx] = Math.Max(1, flowAccum[idx]);
            int d = flowDir[idx];
            if (d < 0) continue;
            int x = idx % W;
            int y = idx / W;
            int nx = x + dx[d];
            int ny = y + dy[d];
            if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
            flowAccum[ny * W + nx] += flowAccum[idx];
        }

        for (int i = 0; i < W * H; i++)
            if (flowAccum[i] > riverThreshold)
            {
                _world.RiverMask[i] = true;
                if (_world.ElevationField[i] > 0.18f) _world.ElevationField[i] = 0.18f;
            }

        for (int i = 0; i < W * H; i++)
            if (_world.RiverMask[i])
            {
                int rx = i % W;
                int ry = i / W;
                _world.Tiles[rx, ry] = new Tile(BiomeType.ShallowWater);
            }

        PruneDisconnectedRivers();
    }

    private void PruneDisconnectedRivers()
    {
        int W = _world.Width, H = _world.Height;
        var visited = new bool[W * H];
        var queue = new Queue<int>();
        for (int i = 0; i < W * H; i++)
        {
            if (!_world.RiverMask[i]) continue;
            int rx = i % W;
            int ry = i / W;
            bool oceanAdj = false;
            if (rx > 0 && _world.ContinentMask[i - 1] <= 0.5f) oceanAdj = true;
            else if (rx < W - 1 && _world.ContinentMask[i + 1] <= 0.5f) oceanAdj = true;
            else if (ry > 0 && _world.ContinentMask[i - W] <= 0.5f) oceanAdj = true;
            else if (ry < H - 1 && _world.ContinentMask[i + W] <= 0.5f) oceanAdj = true;
            if (oceanAdj)
            {
                visited[i] = true;
                queue.Enqueue(i);
            }
        }

        int[] offsets = [-1, 1, -W, W];
        while (queue.Count > 0)
        {
            int idx = queue.Dequeue();
            for (int k = 0; k < 4; k++)
            {
                int n = idx + offsets[k];
                if (n < 0 || n >= W * H) continue;
                int nx = n % W;
                int ny = n / W;
                if (k < 2 && (idx % W == 0 || idx % W == W - 1)) continue;
                if (visited[n]) continue;
                if (_world.RiverMask[n] || _world.ElevationField[n] <= 0.5f)
                {
                    visited[n] = true;
                    queue.Enqueue(n);
                }
            }
        }

        for (int i = 0; i < W * H; i++)
            if (_world.RiverMask[i] && !visited[i])
                _world.RiverMask[i] = false;
    }

    private void SmoothTerrain()
    {
        var tmp = new BiomeType[_world.Width, _world.Height];
        for (int y = 0; y < _world.Height; y++)
            for (int x = 0; x < _world.Width; x++)
                tmp[x, y] = _world.Tiles[x, y].Biome;

        for (int y = 1; y < _world.Height - 1; y++)
            for (int x = 1; x < _world.Width - 1; x++)
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

        for (int y = 1; y < _world.Height - 1; y++)
            for (int x = 1; x < _world.Width - 1; x++)
                _world.Tiles[x, y].Biome = tmp[x, y];
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

    private static float[,] GenerateFBMWarped(int w, int h, int octaves, float scale,
        float persistence, float[,] warp, float warpStrength, Random rng)
    {
        float[,] noise = GenerateFBM(w, h, octaves, scale, persistence, rng);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                noise[x, y] = (noise[x, y] + warp[x, y] * warpStrength) / (1f + warpStrength);
        return noise;
    }

    private static float[,] GenerateFBM(int w, int h, int octaves, float scale, float persistence, Random rng)
    {
        int gs = 1 << octaves;
        float[,] grid = new float[gs + 2, gs + 2];
        for (int i = 0; i <= gs + 1; i++)
            for (int j = 0; j <= gs + 1; j++)
                grid[i, j] = (float)rng.NextDouble();

        float[,] result = new float[w, h];
        float maxVal = 0;

        for (int o = 0; o < octaves; o++)
        {
            float amp = (float)Math.Pow(persistence, o);
            maxVal += amp;
            float freq = scale * (1 << o) * gs / 8f;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float gx = x / (float)w * freq;
                    float gy = y / (float)h * freq;
                    int ix0 = (int)gx;
                    int iy0 = (int)gy;
                    float fx = gx - ix0;
                    float fy = gy - iy0;
                    float sx = fx * fx * (3f - 2f * fx);
                    float sy = fy * fy * (3f - 2f * fy);
                    int gx0 = Mod(ix0, gs + 1);
                    int gy0 = Mod(iy0, gs + 1);
                    int gx1 = Mod(ix0 + 1, gs + 1);
                    int gy1 = Mod(iy0 + 1, gs + 1);
                    float v = Lerp(Lerp(grid[gx0, gy0], grid[gx1, gy0], sx),
                                   Lerp(grid[gx0, gy1], grid[gx1, gy1], sx), sy);
                    result[x, y] += v * amp;
                }
        }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result[x, y] /= maxVal;

        return result;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static int Mod(int x, int m) => ((x % m) + m) % m;
}
