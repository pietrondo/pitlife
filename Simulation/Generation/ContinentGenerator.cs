using System;
using PitLife.Rendering;

namespace PitLife.Simulation;

internal sealed class ContinentGenerator
{
    private readonly World _world;
    private readonly Random _rng;

    public ContinentGenerator(World world, Random rng)
    {
        _world = world;
        _rng = rng;
    }

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

        var centers = new (float X, float Y)[continentCount];
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
        float sizeFactor = islandScale switch { 0.10f => 1.15f, 0.15f => 1.0f, 0.20f => 0.85f, _ => 1.0f };
        continentRadius *= sizeFactor;

        var baseNoise = new FastNoiseLite(_rng.Next());
        baseNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        baseNoise.SetFrequency(0.03f / islandScale);

        var coastNoise = new FastNoiseLite(_rng.Next());
        coastNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        coastNoise.SetFrequency(0.10f / islandScale);

        var detailNoise = new FastNoiseLite(_rng.Next());
        detailNoise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        detailNoise.SetFrequency(0.30f / islandScale);

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float minDist = float.MaxValue;
                for (int c = 0; c < continentCount; c++)
                {
                    float dx = x - centers[c].X;
                    float dy = y - centers[c].Y;
                    float d = MathF.Sqrt(dx * dx + dy * dy);
                    if (d < minDist) minDist = d;
                }

                float distNorm = minDist / continentRadius;
                float mask = 1f - Math.Clamp(distNorm, 0f, 1f);

                float cst = (coastNoise.GetNoise(x, y) + 1f) * 0.5f;
                float edgeDist = MathF.Abs(mask - seaLevel) / 0.15f;
                if (edgeDist < 1f)
                    mask += (cst - 0.5f) * 0.25f * (1f - edgeDist);
                mask = Math.Clamp(mask, 0f, 1f);

                float landMask = mask > seaLevel ? 1f : mask / seaLevel;
                float isLand = mask > seaLevel ? 1f : 0f;

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
                    < 0.20f => BiomeType.Beach,
                    < 0.35f => BiomeType.Grassland,
                    < 0.55f => BiomeType.Forest,
                    < 0.72f => BiomeType.DenseForest,
                    < 0.88f => BiomeType.Mountain,
                    _ => BiomeType.Snow
                };
                _world.Tiles[x, y] = new Tile(biome);
            }
        }
    }
}
