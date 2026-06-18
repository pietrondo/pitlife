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

        float[,] elevation = GenerateFBM(Width, Height, 5, 1.2f, 0.55f, new Random(rng.Next()));
        float[,] moisture = GenerateFBM(Width, Height, 5, 1.0f, 0.50f, new Random(rng.Next()));
        float[,] temperature = GenerateFBM(Width, Height, 3, 0.6f, 0.60f, new Random(rng.Next()));

        for (int y = 0; y < Height; y++)
        {
            float latFactor = Math.Abs(y / (float)Height - 0.5f) * 2f;
            for (int x = 0; x < Width; x++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];
                float t = temperature[x, y] * (1f - latFactor * 0.4f);

                float distX = x / (float)Width - 0.5f;
                float distY = y / (float)Height - 0.5f;
                float edge = distX * distX + distY * distY;
                e -= edge * 0.5f;

                BiomeType biome = AssignBiome(e, m, t);
                Tiles[x, y] = new Tile(biome);
            }
        }
    }

    private static BiomeType AssignBiome(float e, float m, float t)
    {
        if (e < 0.15f) return BiomeType.DeepOcean;
        if (e < 0.25f) return BiomeType.ShallowWater;
        if (e < 0.30f) return BiomeType.Beach;

        if (e > 0.75f)
        {
            if (e > 0.88f || t < 0.15f) return BiomeType.Snow;
            return BiomeType.Mountain;
        }

        if (e > 0.60f)
        {
            if (t < 0.25f) return BiomeType.Tundra;
            if (m < 0.30f) return BiomeType.Tundra;
            return BiomeType.Mountain;
        }

        if (e < 0.35f && m > 0.60f)
            return BiomeType.Swamp;

        if (t < 0.20f)
            return m < 0.40f ? BiomeType.Tundra : BiomeType.Grassland;

        if (t < 0.35f)
            return m < 0.30f ? BiomeType.Tundra : BiomeType.Grassland;

        if (m < 0.15f) return BiomeType.Desert;
        if (m < 0.30f) return BiomeType.Savanna;
        if (m < 0.55f) return BiomeType.Grassland;
        if (m < 0.75f) return BiomeType.Forest;
        return BiomeType.DenseForest;
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
            int step = gs / (1 << o);
            float amp = (float)Math.Pow(persistence, o);
            maxVal += amp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float gx = x / (float)w * scale * (1 << o) * gs / 8f;
                    float gy = y / (float)h * scale * (1 << o) * gs / 8f;

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

                    float v = Lerp(
                        Lerp(grid[gx0, gy0], grid[gx1, gy0], sx),
                        Lerp(grid[gx0, gy1], grid[gx1, gy1], sx),
                        sy
                    );
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
