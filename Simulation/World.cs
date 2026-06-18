using System;

namespace PitLife.Simulation;

public class World
{
    public int Width { get; }
    public int Height { get; }
    public int TileSize { get; } = 32;
    public int PixelWidth => Width * TileSize;
    public int PixelHeight => Height * TileSize;
    public Tile[,] Tiles { get; }

    public World(int width, int height, int seed)
    {
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
        Generate(seed);
    }

    private void Generate(int seed)
    {
        var rng = new Random(seed);

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

                BiomeType biome = AssignBiome(e, m, t);
                Tiles[x, y] = new Tile(biome);
            }
        }

        CarveRivers(rng.Next());
        SmoothTerrain();
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
        int rivers = 3 + rng.Next(4);

        for (int r = 0; r < rivers; r++)
        {
            int x = rng.Next(Width);
            int y = 0;
            int dir = rng.Next(2) == 0 ? 1 : -1;

            for (int i = 0; i < Height * 2 && y >= 0 && y < Height; i++)
            {
                if (x >= 0 && x < Width && y >= 0 && y < Height)
                {
                    var tile = Tiles[x, y];
                    if (tile.Biome != BiomeType.DeepOcean && tile.Biome != BiomeType.ShallowWater)
                    {
                        tile.Biome = BiomeType.ShallowWater;
                        tile.Vegetation = 0;
                    }
                }
                x += rng.Next(3) - 1;
                y += dir;
                x = Math.Clamp(x, 0, Width - 1);
            }
        }
    }

    private void SmoothTerrain()
    {
        var tmp = new BiomeType[Width, Height];
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
