using System;
using System.Numerics;

namespace PitLife.Simulation;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; } = 32;
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;
    public Tile[,] Tiles { get; }
    internal float[] ContinentMask { get; }
    internal float[] ElevationField { get; }
    internal bool[] RiverMask { get; }
    private float _baseHeight;

    public World(int width, int height, int seed)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        ContinentMask = new float[width * height];
        ElevationField = new float[width * height];
        RiverMask = new bool[width * height];
        Generate(seed);
    }

    private void Generate(int seed)
    {
        var rng = new Random(seed);

        GenerateContinentMask(seed, new Random(rng.Next()));

        float[,] warp = GenerateFBM(Width, Height, 2, 2f, 1f, new Random(rng.Next()));
        float[,] elevation = GenerateFBMWarped(Width, Height, 6, 1.4f, 0.55f, warp, 3f, new Random(rng.Next()));
        float[,] moisture = GenerateFBMWarped(Width, Height, 5, 1.0f, 0.50f, warp, 2f, new Random(rng.Next()));
        float[,] temperature = GenerateFBM(Width, Height, 3, 0.6f, 0.60f, new Random(rng.Next()));

        for (int y = 0; y < Height; y++)
        {
            float latFactor = Math.Abs(y / (float)Height - 0.5f) * 2f;
            for (int x = 0; x < Width; x++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];
                float t = temperature[x, y] * (1f - latFactor * 0.45f);

                float distX = x / (float)Width - 0.5f;
                float distY = y / (float)Height - 0.5f;
                e -= (distX * distX + distY * distY) * 0.55f;

                if (e < 0.22f && m > 0.3f) e = 0.18f;

                float elev = ContinentMask[y * Width + x] > 0.5f ? ContinentMask[y * Width + x] * _baseHeight + warp[x, y] * ContinentMask[y * Width + x] * 0.25f : 0f;
                ElevationField[y * Width + x] = elev;

                BiomeType biome = AssignBiome(e, m, t);
                Tiles[x, y] = new Tile(biome);
            }
        }

        CarveRivers(rng.Next());
        SmoothTerrain();
    }

    private void GenerateContinentMask(int seed, Random rng)
    {
        int variant = ((seed % 3) + 3) % 3;
        int cellCount;
        float cellRadius;
        switch (variant)
        {
            case 0:
                cellCount = 1;
                cellRadius = 0.45f;
                break;
            case 1:
                cellCount = 4;
                cellRadius = 0.28f;
                break;
            default:
                cellCount = 6;
                cellRadius = 0.18f;
                break;
        }

        _baseHeight = 0.4f + (float)rng.NextDouble() * 0.6f;

        Vector2[] centers = new Vector2[cellCount];
        if (cellCount == 1)
        {
            centers[0] = new Vector2(0.5f, 0.5f);
        }
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
        float[,] warpU = GenerateFBM(Width, Height, 2, 16f, 0.5f, new Random(rng.Next()));
        float[,] warpV = GenerateFBM(Width, Height, 2, 16f, 0.5f, new Random(rng.Next()));

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float nx = x / (float)Width;
                float ny = y / (float)Height;

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
                ContinentMask[y * Width + x] = mask;
            }
        }
    }

    private static BiomeType AssignBiome(float e, float m, float t)
    {
        if (e < 0.13f) return BiomeType.DeepOcean;
        if (e < 0.22f) return BiomeType.ShallowWater;
        if (e < 0.27f) return BiomeType.Beach;

        if (e > 0.72f)
        {
            if (e > 0.87f || t < 0.15f) return BiomeType.Snow;
            return BiomeType.Mountain;
        }

        if (e > 0.58f)
        {
            if (t < 0.25f || m < 0.25f) return BiomeType.Tundra;
            return BiomeType.Mountain;
        }

        if (e < 0.32f && m > 0.55f)
            return BiomeType.Swamp;

        if (t < 0.18f)
            return m < 0.45f ? BiomeType.Tundra : BiomeType.Grassland;

        if (t < 0.32f)
            return m < 0.30f ? BiomeType.Tundra : BiomeType.Grassland;

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

        int W = Width, H = Height;
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        float[] dd = { 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f };

        int[] flowDir = new int[W * H];
        for (int y = 0; y < H; y++)
        {
            for (int x = 0; x < W; x++)
            {
                int idx = y * W + x;
                float e = ElevationField[idx];
                int bestDir = -1;
                float bestDrop = 0f;
                for (int d = 0; d < 8; d++)
                {
                    int nx = x + dx[d];
                    int ny = y + dy[d];
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    float ne = ElevationField[ny * W + nx];
                    float drop = (e - ne) / dd[d];
                    if (drop > bestDrop)
                    {
                        bestDrop = drop;
                        bestDir = d;
                    }
                }
                flowDir[idx] = bestDir;
            }
        }

        int[] cells = new int[W * H];
        for (int i = 0; i < W * H; i++) cells[i] = i;
        Array.Sort(cells, (a, b) => ElevationField[b].CompareTo(ElevationField[a]));

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
            int nidx = ny * W + nx;
            flowAccum[nidx] += flowAccum[idx];
        }

        for (int i = 0; i < W * H; i++)
        {
            if (flowAccum[i] > riverThreshold)
            {
                RiverMask[i] = true;
                if (ElevationField[i] > 0.18f) ElevationField[i] = 0.18f;
            }
        }
    }

    private void SmoothTerrain()
    {
        var tmp = new BiomeType[Width, Height];
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                tmp[x, y] = Tiles[x, y].Biome;

        for (int y = 1; y < Height - 1; y++)
            for (int x = 1; x < Width - 1; x++)
            {
                var b = Tiles[x, y].Biome;
                if (b == BiomeType.DeepOcean || b == BiomeType.ShallowWater) continue;

                int matches = 0;
                if (Tiles[x - 1, y].Biome == b) matches++;
                if (Tiles[x + 1, y].Biome == b) matches++;
                if (Tiles[x, y - 1].Biome == b) matches++;
                if (Tiles[x, y + 1].Biome == b) matches++;

                tmp[x, y] = matches >= 2 ? b : MostCommonNeighbor(x, y);
            }

        for (int y = 1; y < Height - 1; y++)
            for (int x = 1; x < Width - 1; x++)
                Tiles[x, y].Biome = tmp[x, y];
    }

    private BiomeType MostCommonNeighbor(int x, int y)
    {
        Span<BiomeType> nb = [Tiles[x - 1, y].Biome, Tiles[x + 1, y].Biome,
                              Tiles[x, y - 1].Biome, Tiles[x, y + 1].Biome];
        int bestCount = 0;
        BiomeType best = Tiles[x, y].Biome;
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
            {
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
        }

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                result[x, y] /= maxVal;

        return result;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;
    private static int Mod(int x, int m) => ((x % m) + m) % m;

    public Tile GetTile(int x, int y)
    {
        x = Math.Clamp(x, 0, Width - 1);
        y = Math.Clamp(y, 0, Height - 1);
        return Tiles[x, y];
    }

    public Tile GetTileAtPosition(float worldX, float worldY)
    {
        int tx = (int)(worldX / TileSize);
        int ty = (int)(worldY / TileSize);
        return GetTile(tx, ty);
    }
}
