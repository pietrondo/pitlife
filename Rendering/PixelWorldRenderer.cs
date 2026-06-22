using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

public sealed class PixelWorldRenderer : IDisposable
{
    private readonly World _world;
    private readonly FastNoiseLite _noise;
    private RenderTarget2D? _worldTexture;
    private bool _needsRedraw = true;
    private int _renderScale = 4; // 4x4 sub-pixels per tile for smoother look

    // Biome colors matching the minimap (clean base colors)
    private static readonly Color[] BiomeBaseColors =
    {
        new(28, 60, 110),    // DeepOcean
        new(58, 118, 168),   // ShallowWater
        new(220, 200, 140),  // Beach
        new(220, 190, 110),  // Desert
        new(200, 190, 110),  // Savanna
        new(120, 180, 80),   // Grassland
        new(60, 130, 60),    // Forest
        new(35, 95, 45),     // DenseForest
        new(80, 100, 70),    // Swamp
        new(180, 185, 170),  // Tundra
        new(120, 110, 100),  // Mountain
        new(235, 240, 245),  // Snow
        new(0, 180, 160),     // CoralReef
    };

    // Slightly darker variation for subtle texture
    private static readonly Color[] BiomeDetailColors =
    {
        new(20, 50, 90),     // DeepOcean dark
        new(48, 100, 148),   // ShallowWater dark
        new(200, 180, 120),  // Beach dark
        new(200, 170, 90),   // Desert dark
        new(180, 170, 90),   // Savanna dark
        new(100, 160, 60),   // Grassland dark
        new(45, 110, 45),    // Forest dark
        new(25, 80, 30),     // DenseForest dark
        new(65, 85, 55),     // Swamp dark
        new(160, 165, 150),  // Tundra dark
        new(105, 95, 85),    // Mountain dark
        new(220, 225, 230),  // Snow dark
        new(0, 160, 140),     // CoralReef dark
    };

    // Slightly lighter variation for subtle highlights
    private static readonly Color[] BiomeHighlightColors =
    {
        new(38, 75, 135),    // DeepOcean light
        new(68, 135, 190),   // ShallowWater light
        new(235, 215, 155),  // Beach light
        new(235, 205, 125),  // Desert light
        new(215, 205, 125),  // Savanna light
        new(135, 200, 95),   // Grassland light
        new(70, 150, 70),    // Forest light
        new(45, 110, 60),    // DenseForest light
        new(95, 115, 85),    // Swamp light
        new(195, 200, 185),  // Tundra light
        new(140, 125, 115),  // Mountain light
        new(250, 255, 255),  // Snow light
        new(0, 210, 190),     // CoralReef light
    };

    public PixelWorldRenderer(World world, int seed)
    {
        _world = world;
        _noise = new FastNoiseLite(seed);
        _noise.SetNoiseType(FastNoiseLite.NoiseType.OpenSimplex2);
        _noise.SetFrequency(0.05f);
    }

    public void LoadContent(GraphicsDevice gd)
    {
        int texWidth = _world.Width * _renderScale;
        int texHeight = _world.Height * _renderScale;
        _worldTexture = new RenderTarget2D(gd, texWidth, texHeight, false, SurfaceFormat.Color, DepthFormat.None);
        _needsRedraw = true;
    }

    public void Invalidate() => _needsRedraw = true;

    public void Draw(SpriteBatch sb, Camera camera)
    {
        if (_worldTexture == null) return;

        // Redraw world texture if needed (tile changed, etc)
        if (_needsRedraw)
        {
            RedrawWorldTexture(sb.GraphicsDevice);
            _needsRedraw = false;
        }

        // Draw the cached world texture over the entire world bounds.
        // The camera transform matrix will automatically handle position, zoom, and viewport clipping.
        sb.Draw(_worldTexture,
            new Rectangle(0, 0, _world.PixelWidth, _world.PixelHeight),
            Color.White);
    }

    private void RedrawWorldTexture(GraphicsDevice gd)
    {
        if (_worldTexture == null) return;

        // Get color data for the texture
        int width = _worldTexture.Width;
        int height = _worldTexture.Height;
        Color[] data = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // Map pixel to tile coordinates
                int tileX = x / _renderScale;
                int tileY = y / _renderScale;

                // Get biome at this tile
                BiomeType biome = _world.GetTile(tileX, tileY).Biome;
                int biomeIdx = (int)biome;                // Use clean solid base color to match the minimap and ensure readability
                data[y * width + x] = BiomeBaseColors[biomeIdx];
            }
        }

        _worldTexture.SetData(data);
    }

    public void Dispose()
    {
        _worldTexture?.Dispose();
        _worldTexture = null;
    }

    // Static accessors for testing
    public static Color[] GetBiomeBaseColors() => BiomeBaseColors;
    public static Color[] GetBiomeDetailColors() => BiomeDetailColors;
    public static Color[] GetBiomeHighlightColors() => BiomeHighlightColors;
    public static int RenderScale => 1;
}

// Simplex noise implementation (FastNoiseLite-style)
public sealed class FastNoiseLite
{
    private readonly int _seed;
    private float _frequency = 0.01f;
    private NoiseType _type = NoiseType.OpenSimplex2;

    public enum NoiseType { OpenSimplex2, Perlin, Value }

    public FastNoiseLite(int seed) => _seed = seed;

    public void SetFrequency(float freq) => _frequency = freq;
    public void SetNoiseType(NoiseType type) => _type = type;

    public float GetNoise(float x, float y)
    {
        return _type switch
        {
            NoiseType.OpenSimplex2 => OpenSimplex2(x * _frequency, y * _frequency),
            NoiseType.Perlin => Perlin(x * _frequency, y * _frequency),
            _ => Value(x * _frequency, y * _frequency)
        };
    }

    // Simplified OpenSimplex2 implementation
    private float OpenSimplex2(float x, float y)
    {
        // Skew and unskew factors (pre-calculated constants)
        const float F2 = 0.366025403f; // 0.5 * (Sqrt(3) - 1)
        const float G2 = 0.211324865f; // (3 - Sqrt(3)) / 6

        float s = (x + y) * F2;
        int i = (int)MathF.Floor(x + s);
        int j = (int)MathF.Floor(y + s);
        float t = (i + j) * G2;
        float X0 = i - t;
        float Y0 = j - t;
        float x0 = x - X0;
        float y0 = y - Y0;

        int i1, j1;
        if (x0 > y0) { i1 = 1; j1 = 0; }
        else { i1 = 0; j1 = 1; }

        float x1 = x0 - i1 + G2;
        float y1 = y0 - j1 + G2;
        float x2 = x0 - 1 + 2 * G2;
        float y2 = y0 - 1 + 2 * G2;

        int ii = i & 255;
        int jj = j & 255;

        float n0 = 0, n1 = 0, n2 = 0;

        float t0 = 0.5f - x0 * x0 - y0 * y0;
        if (t0 >= 0)
        {
            t0 *= t0;
            n0 = t0 * t0 * Grad(Perm(ii + Perm(jj)), x0, y0);
        }

        float t1 = 0.5f - x1 * x1 - y1 * y1;
        if (t1 >= 0)
        {
            t1 *= t1;
            n1 = t1 * t1 * Grad(Perm(ii + i1 + Perm(jj + j1)), x1, y1);
        }

        float t2 = 0.5f - x2 * x2 - y2 * y2;
        if (t2 >= 0)
        {
            t2 *= t2;
            n2 = t2 * t2 * Grad(Perm(ii + 1 + Perm(jj + 1)), x2, y2);
        }

        return 70f * (n0 + n1 + n2);
    }

    private float Perlin(float x, float y)
    {
        int X = (int)MathF.Floor(x) & 255;
        int Y = (int)MathF.Floor(y) & 255;
        x -= MathF.Floor(x);
        y -= MathF.Floor(y);
        float u = Fade(x);
        float v = Fade(y);

        int A = Perm(X) + Y;
        int AA = Perm(A);
        int AB = Perm(A + 1);
        int B = Perm(X + 1) + Y;
        int BA = Perm(B);
        int BB = Perm(B + 1);

        return Lerp(v, Lerp(u, Grad(Perm(AA), x, y), Grad(Perm(BA), x - 1, y)),
                         Lerp(u, Grad(Perm(AB), x, y - 1), Grad(Perm(BB), x - 1, y - 1)));
    }

    private float Value(float x, float y)
    {
        int X = (int)MathF.Floor(x);
        int Y = (int)MathF.Floor(y);
        float fx = x - X;
        float fy = y - Y;

        float v00 = Val(X, Y);
        float v10 = Val(X + 1, Y);
        float v01 = Val(X, Y + 1);
        float v11 = Val(X + 1, Y + 1);

        float v0 = Lerp(fx, v00, v10);
        float v1 = Lerp(fx, v01, v11);
        return Lerp(fy, v0, v1);
    }

    private float Val(int x, int y) => ((Perm(x + Perm(y)) / 255f) * 2f - 1f);

    private int Perm(int i) => PermTable[(i + _seed) & 255];

    private static float Fade(float t) => t * t * t * (t * (t * 6 - 15) + 10);
    private static float Lerp(float t, float a, float b) => a + t * (b - a);
    private static float Grad(int hash, float x, float y)
    {
        int h = hash & 15;
        float u = h < 8 ? x : y;
        float v = h < 4 ? y : h == 12 || h == 14 ? x : 0;
        return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v);
    }

    private static readonly byte[] PermTable =
    [
        151,160,137,91,90,15,131,13,201,95,96,53,194,233,7,225,
        140,36,103,30,69,142,8,99,37,240,21,10,23,190,6,148,
        247,120,234,75,0,26,197,62,94,252,219,203,117,35,11,32,
        57,177,33,88,237,149,56,87,174,20,125,136,171,168,68,175,
        74,165,71,134,139,48,27,166,77,146,158,231,83,111,229,122,
        60,211,133,230,220,105,92,41,55,46,245,40,244,102,143,54,
        65,25,63,161,1,216,80,73,209,76,132,187,208,89,18,169,
        200,196,135,130,116,188,159,86,164,100,109,198,173,186,3,64,
        52,217,226,250,124,123,5,202,38,147,118,126,255,82,85,212,
        207,206,59,227,47,16,58,17,182,189,28,42,223,183,170,213,
        119,248,152,2,44,154,163,70,221,153,101,155,167,43,172,9,
        129,22,39,253,19,98,108,110,79,113,224,232,178,185,112,104,
        218,246,97,228,251,34,242,193,238,210,144,12,191,179,162,241,
        81,51,145,235,249,14,239,107,49,192,214,31,181,199,106,157,
        184,84,204,176,115,121,50,45,127,4,150,254,138,236,205,93,
        222,114,67,29,24,72,243,141,128,195,78,66,215,61,156,180
    ];
}
