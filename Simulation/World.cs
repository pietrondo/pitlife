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
        float[,] elevation = GenerateNoise(Width, Height, 8, rng);
        float[,] moisture = GenerateNoise(Width, Height, 6, new Random(rng.Next()));

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                float e = elevation[x, y];
                float m = moisture[x, y];

                BiomeType biome;
                if (e < 0.25f)
                    biome = BiomeType.Water;
                else if (e < 0.35f)
                    biome = BiomeType.Desert;
                else if (m < 0.3f)
                    biome = BiomeType.Grassland;
                else if (m < 0.6f)
                    biome = BiomeType.Forest;
                else
                    biome = BiomeType.Mountain;

                Tiles[x, y] = new Tile(biome);
            }
        }
    }

    private static float[,] GenerateNoise(int width, int height, int octaves, Random rng)
    {
        float[,] noise = new float[width, height];
        float[,] values = new float[width, height];
        for (int i = 0; i < width * height; i++)
            values[i % width, i / width] = (float)rng.NextDouble();

        float amplitude = 1f;
        float totalAmplitude = 0f;

        for (int o = 0; o < octaves; o++)
        {
            int step = 1 << o;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int sx = (x / step) * step;
                    int sy = (y / step) * step;
                    int ex = Math.Min(sx + step, width - 1);
                    int ey = Math.Min(sy + step, height - 1);
                    float fx = (x - sx) / (float)(ex - sx + 1);
                    float fy = (y - sy) / (float)(ey - sy + 1);
                    float v = Lerp(
                        Lerp(values[sx, sy], values[ex, sy], fx),
                        Lerp(values[sx, ey], values[ex, ey], fx),
                        fy
                    );
                    noise[x, y] += v * amplitude;
                }
            }
            totalAmplitude += amplitude;
            amplitude *= 0.5f;
        }

        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                noise[x, y] /= totalAmplitude;

        return noise;
    }

    private static float Lerp(float a, float b, float t) => a + (b - a) * t;

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
