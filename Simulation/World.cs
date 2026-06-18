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

        float[,] elevation = GenerateFBMNoise(Width, Height, 4, 1f, 0.5f, new Random(rng.Next()));
        float[,] moisture = GenerateFBMNoise(Width, Height, 4, 0.8f, 0.5f, new Random(rng.Next()));

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];

                float distFromCenter = Math.Max(
                    Math.Abs(x / (float)Width - 0.5f) * 2f,
                    Math.Abs(y / (float)Height - 0.5f) * 2f
                );
                e -= distFromCenter * distFromCenter * 0.3f;

                BiomeType biome = AssignBiome(e, m);
                Tiles[x, y] = new Tile(biome);
            }
        }
    }

    private static BiomeType AssignBiome(float elevation, float moisture)
    {
        if (elevation < 0.25f)
            return BiomeType.Water;
        if (elevation < 0.32f)
            return BiomeType.Desert;

        if (elevation > 0.65f)
            return BiomeType.Mountain;

        if (moisture < 0.25f)
            return BiomeType.Desert;
        if (moisture < 0.50f)
            return BiomeType.Grassland;
        return BiomeType.Forest;
    }

    private static float[,] GenerateFBMNoise(int w, int h, int octaves, float scale, float persistence, Random rng)
    {
        float[,] grid = new float[2 + (1 << (octaves - 1)), 2 + (1 << (octaves - 1))];
        for (int i = 0; i < grid.GetLength(0); i++)
            for (int j = 0; j < grid.GetLength(1); j++)
                grid[i, j] = (float)rng.NextDouble();

        float[,] result = new float[w, h];
        float maxVal = 0;

        for (int o = 0; o < octaves; o++)
        {
            int step = 1 << (octaves - 1 - o);
            float amp = (float)Math.Pow(persistence, o);
            maxVal += amp;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float gx = x / (float)w * scale * (1 << o);
                    float gy = y / (float)h * scale * (1 << o);

                    int ix0 = (int)gx;
                    int iy0 = (int)gy;
                    int ix1 = ix0 + 1;
                    int iy1 = iy0 + 1;

                    float fx = gx - ix0;
                    float fy = gy - iy0;
                    float sx = SmoothStep(fx);
                    float sy = SmoothStep(fy);

                    ix0 = Mod(ix0, grid.GetLength(0));
                    ix1 = Mod(ix1, grid.GetLength(0));
                    iy0 = Mod(iy0, grid.GetLength(1));
                    iy1 = Mod(iy1, grid.GetLength(1));

                    float v = Lerp(
                        Lerp(grid[ix0, iy0], grid[ix1, iy0], sx),
                        Lerp(grid[ix0, iy1], grid[ix1, iy1], sx),
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

    private static float SmoothStep(float t) => t * t * (3f - 2f * t);
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
