using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Rendering;

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

    public void Generate() => Generate(WorldGenOptions.Pangea());

    public void Generate(WorldGenOptions options)
    {
        float seaLevel = options.SeaLevel;
        float islandScale = options.IslandSize switch
        {
            IslandSize.Small => 0.20f,
            IslandSize.Medium => 0.15f,
            IslandSize.Large => 0.10f,
            _ => 0.15f
        };
        int continentCount = options.ContinentCount;
        int w = _world.Width, h = _world.Height;

        // ── 1. Place continent centers ──────────────────────────────
        var centers = new (float X, float Y)[continentCount];
        float maxDim = MathF.Max(w, h);
        for (int c = 0; c < continentCount; c++)
        {
            float angle = (c + _rng.NextSingle() * 0.3f) * MathF.PI * 2f / continentCount;
            float r = 0.25f + _rng.NextSingle() * 0.15f;
            centers[c] = (
                (0.5f + MathF.Cos(angle) * r) * w,
                (0.5f + MathF.Sin(angle) * r) * h
            );
        }
        float continentRadius = MathF.Min(w, h) * 0.45f / (continentCount * 0.35f + 0.3f);
        // IslandSize.Large = bigger continents, Small = smaller
        float sizeFactor = islandScale switch { 0.10f => 1.15f, 0.15f => 1.0f, 0.20f => 0.85f, _ => 1.0f };
        continentRadius *= sizeFactor;

        // ── 2. Multi-scale noise for natural coastlines ─────────────
        var baseNoise = new FastNoiseLite(_rng.Next());
        baseNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        baseNoise.SetFrequency(0.03f / islandScale);

        var coastNoise = new FastNoiseLite(_rng.Next());
        coastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        coastNoise.SetFrequency(0.10f / islandScale);

        var detailNoise = new FastNoiseLite(_rng.Next());
        detailNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        detailNoise.SetFrequency(0.30f / islandScale);

        // ── 3. Generate terrain ─────────────────────────────────────
        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                // Distance to nearest continent center
                float minDist = float.MaxValue;
                for (int c = 0; c < continentCount; c++)
                {
                    float dx = x - centers[c].X;
                    float dy = y - centers[c].Y;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < minDist) minDist = d;
                }

                // Continent mask: 1 = center, 0 = far away
                float distNorm = minDist / continentRadius;
                float mask = 1f - Math.Clamp(distNorm, 0f, 1f);

                // Coastline noise warps the mask for natural shapes (only near edges)
                float cst = (coastNoise.GetNoise(x, y) + 1f) * 0.5f;
                float edgeDist = MathF.Abs(mask - seaLevel) / 0.15f;
                if (edgeDist < 1f)
                    mask += (cst - 0.5f) * 0.25f * (1f - edgeDist);
                mask = Math.Clamp(mask, 0f, 1f);

                // Smooth land/water transition
                float landMask = mask > seaLevel ? 1f : mask / seaLevel;
                float isLand = mask > seaLevel ? 1f : 0f;

                // Base elevation from continent shape + noise
                float baseElev = (baseNoise.GetNoise(x, y) + 1f) * 0.5f;
                float detail = (detailNoise.GetNoise(x, y) + 1f) * 0.5f;
                float elev = isLand * (baseElev * 0.6f + detail * 0.4f + mask * 0.3f);
                elev += (1f - isLand) * mask * 0.12f;
                elev = Math.Clamp(elev, 0f, 1f);

                int idx = y * w + x;
                _world.ElevationField[idx] = elev;
                _world.ContinentMask[idx] = landMask;

                BiomeType biome = elev switch
                {
                    < 0.12f => isLand > 0f ? BiomeType.ShallowWater : BiomeType.DeepOcean,
                    < 0.22f => BiomeType.Beach,
                    < 0.35f => detail < 0.3f ? BiomeType.Desert : BiomeType.Grassland,
                    < 0.50f => cst > 0.55f ? BiomeType.Swamp : BiomeType.Grassland,
                    < 0.65f => detail < 0.35f ? BiomeType.Savanna : BiomeType.Forest,
                    < 0.78f => BiomeType.DenseForest,
                    < 0.90f => BiomeType.Mountain,
                    _ => BiomeType.Snow
                };
                _world.Tiles[x, y] = new Tile(biome);
            }
        }

        PlaceCoralReefs();
        PlaceCaves();
        PlaceFaultLines();
        PlaceVolcanoes();
        CarveRivers(_rng.Next());
        SmoothTerrain();
        CopyEdgesForWrap();
        EnsureAllBiomesPresent();
    }

    private void CopyEdgesForWrap()
    {
        int w = _world.Width, h = _world.Height;
        for (int y = 0; y < h; y++)
        {
            int iLeft = y * w + 0;
            int iRight = y * w + (w - 1);
            _world.Tiles[w - 1, y] = _world.Tiles[0, y];
            _world.ElevationField[iRight] = _world.ElevationField[iLeft];
            _world.ContinentMask[iRight] = _world.ContinentMask[iLeft];
            _world.RiverMask[iRight] = _world.RiverMask[iLeft];
        }
        for (int x = 0; x < w; x++)
        {
            int iTop = 0 * w + x;
            int iBot = (h - 1) * w + x;
            _world.Tiles[x, h - 1] = _world.Tiles[x, 0];
            _world.ElevationField[iBot] = _world.ElevationField[iTop];
            _world.ContinentMask[iBot] = _world.ContinentMask[iTop];
            _world.RiverMask[iBot] = _world.RiverMask[iTop];
        }
    }

    private void BlendWorldEdges()
    {
        int w = _world.Width, h = _world.Height;
        int blend = Math.Max(w, h) / 4;
        for (int y = 0; y < h; y++)
            for (int x = 0; x < blend; x++)
            {
                int il = y * w + x, ir = y * w + (w - 1 - x);
                float avgElev = (_world.ElevationField[il] + _world.ElevationField[ir]) * 0.5f;
                float avgCont = (_world.ContinentMask[il] + _world.ContinentMask[ir]) * 0.5f;
                for (int bx = 0; bx <= x; bx++)
                {
                    int i = y * w + bx;
                    _world.ElevationField[i] = avgElev;
                    _world.ContinentMask[i] = avgCont;
                    int j = y * w + (w - 1 - bx);
                    _world.ElevationField[j] = avgElev;
                    _world.ContinentMask[j] = avgCont;
                }
            }
        for (int x = 0; x < w; x++)
            for (int y = 0; y < blend; y++)
            {
                int it = y * w + x, ib = (h - 1 - y) * w + x;
                float avgElev = (_world.ElevationField[it] + _world.ElevationField[ib]) * 0.5f;
                float avgCont = (_world.ContinentMask[it] + _world.ContinentMask[ib]) * 0.5f;
                for (int by = 0; by <= y; by++)
                {
                    int i = by * w + x;
                    _world.ElevationField[i] = avgElev;
                    _world.ContinentMask[i] = avgCont;
                    int j = (h - 1 - by) * w + x;
                    _world.ElevationField[j] = avgElev;
                    _world.ContinentMask[j] = avgCont;
                }
            }
    }

    private void EnsureAllBiomesPresent()
    {
        var allTwelve = new BiomeType[]
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

        // Ensure Grassland exists first (it's used as replacement for other missing biomes)
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
            // Force-place if still missing
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

    private void PlaceCaves()
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

    private bool[,] _faultMask = null!;

    private void PlaceFaultLines()
    {
        _faultMask = new bool[_world.Width, _world.Height];
        int numFaults = 2 + _rng.Next(3);
        for (int f = 0; f < numFaults; f++)
        {
            float angle = (float)(_rng.NextDouble() * Math.PI);
            float offset = _rng.Next(_world.Width + _world.Height);
            float dx = (float)Math.Cos(angle);
            float dy = (float)Math.Sin(angle);
            for (int y = 0; y < _world.Height; y++)
            {
                for (int x = 0; x < _world.Width; x++)
                {
                    float dist = Math.Abs(x * dx + y * dy - offset) / (float)Math.Sqrt(dx * dx + dy * dy);
                    if (dist < 2f + _rng.NextDouble() * 3f)
                        _faultMask[x, y] = true;
                }
            }
        }
    }

    private void PlaceVolcanoes()
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
