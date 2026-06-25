using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

public sealed class PixelWorldRenderer : IDisposable
{
    private readonly World _world;
    private Texture2D? _worldTexture;
    private bool _needsRedraw = true;
    private int _renderScale = 8;

    public PixelWorldRenderer(World world)
    {
        _world = world;
    }

    public void LoadContent(GraphicsDevice gd)
    {
        int texWidth = _world.Width * _renderScale;
        int texHeight = _world.Height * _renderScale;
        _worldTexture?.Dispose();
        _worldTexture = new Texture2D(gd, texWidth, texHeight);
        _needsRedraw = true;
    }

    public void Invalidate() => _needsRedraw = true;

    public void Draw(SpriteBatch sb, Camera camera)
    {
        if (_worldTexture == null) return;

        if (_needsRedraw)
        {
            RedrawWorldTexture(sb.GraphicsDevice);
            _needsRedraw = false;
        }

        int pw = _world.PixelWidth, ph = _world.PixelHeight;
        var visible = camera.VisibleArea;

        int startX = (int)MathF.Floor((float)visible.Left / pw);
        int endX = (int)MathF.Floor((float)visible.Right / pw);
        int startY = (int)MathF.Floor((float)visible.Top / ph);
        int endY = (int)MathF.Floor((float)visible.Bottom / ph);

        for (int dy = startY; dy <= endY; dy++)
        {
            for (int dx = startX; dx <= endX; dx++)
            {
                sb.Draw(_worldTexture,
                    new Rectangle(dx * pw, dy * ph, pw + 1, ph + 1),
                    Color.White);
            }
        }
    }

    private void RedrawWorldTexture(GraphicsDevice gd)
    {
        if (_worldTexture == null) return;

        int width = _worldTexture.Width;
        int height = _worldTexture.Height;
        Color[] data = new Color[width * height];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float worldX = x * _world.TileSize / (float)_renderScale;
                float worldY = y * _world.TileSize / (float)_renderScale;
                int tileX = Math.Clamp((int)(worldX / _world.TileSize), 0, _world.Width - 1);
                int tileY = Math.Clamp((int)(worldY / _world.TileSize), 0, _world.Height - 1);
                int idx = tileY * _world.Width + tileX;

                // Position within tile (0..1)
                float lx = (worldX / _world.TileSize) - tileX;
                float ly = (worldY / _world.TileSize) - tileY;

                var biome = _world.Tiles[tileX, tileY].Biome;
                Color pixelColor = GetBiomeRenderColor(biome);

                // Ordered dither at biome boundaries for smooth transitions
                // Check 4 neighbors; at boundaries blend using position-based threshold
                Color BlendEdge(int nx, int ny, float proximity)
                {
                    var nb = _world.Tiles[nx, ny].Biome;
                    if (nb == biome) return pixelColor;
                    float t = 1f - proximity; // 0 at boundary, 1 deep inside
                    int hash = (x * 3 + y * 7) & 7; // 0..7
                    return hash < t * 8f ? pixelColor : GetBiomeRenderColor(nb);
                }

                if (lx < 0.25f && tileX > 0)               pixelColor = BlendEdge(tileX - 1, tileY, lx / 0.25f);
                else if (lx > 0.75f && tileX < _world.Width - 1) pixelColor = BlendEdge(tileX + 1, tileY, (1f - lx) / 0.25f);
                if (ly < 0.25f && tileY > 0)               pixelColor = BlendEdge(tileX, tileY - 1, ly / 0.25f);
                else if (ly > 0.75f && tileY < _world.Height - 1) pixelColor = BlendEdge(tileX, tileY + 1, (1f - ly) / 0.25f);

                if (_world.RiverMask[idx])
                    pixelColor = Color.Lerp(pixelColor, new Color(40, 100, 200), 0.3f);

                data[y * width + x] = pixelColor;
            }
        }

        // Draw per-tile decorations (trees, rocks, flowers)
        DrawDecorations(data, width, height);

        _worldTexture.SetData(data);
    }

    private void DrawDecorations(Color[] data, int texWidth, int texHeight)
    {
        for (int ty = 0; ty < _world.Height; ty++)
        {
            for (int tx = 0; tx < _world.Width; tx++)
            {
                float vegetation = _world.Tiles[tx, ty].Vegetation;
                int seed = _world.Width * 7919 + _world.Height;
                var deco = TileDecorations.GetDecoration(_world.Tiles[tx, ty].Biome, tx, ty, seed, vegetation);
                if (deco == TileDecorations.DecorationType.None) continue;

                var color = TileDecorations.GetColor(deco, tx, ty, seed);

                // Draw at center of tile in world texture space
                int cx = tx * _renderScale + _renderScale / 2;
                int cy = ty * _renderScale + _renderScale / 2;

                switch (deco)
                {
                    case TileDecorations.DecorationType.Tree:
                        // 2x3 tree trunk + 3x2 canopy
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy - 1, color);
                        DrawPixel(data, texWidth, texHeight, cx - 1, cy - 2, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy - 2, color);
                        DrawPixel(data, texWidth, texHeight, cx + 1, cy - 2, color);
                        break;
                    case TileDecorations.DecorationType.Rock:
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx + 1, cy, color);
                        break;
                    case TileDecorations.DecorationType.Flower:
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        break;
                    case TileDecorations.DecorationType.Bush:
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx + 1, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy + 1, color);
                        break;
                    case TileDecorations.DecorationType.Mushroom:
                        DrawPixel(data, texWidth, texHeight, cx, cy - 1, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy, new Color(245, 222, 179));
                        break;
                    case TileDecorations.DecorationType.Cactus:
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy - 1, color);
                        DrawPixel(data, texWidth, texHeight, cx, cy - 2, color);
                        break;
                    case TileDecorations.DecorationType.Coral:
                        DrawPixel(data, texWidth, texHeight, cx, cy, color);
                        DrawPixel(data, texWidth, texHeight, cx + 1, cy - 1, color);
                        break;
                }
            }
        }
    }

    private static void DrawPixel(Color[] data, int texWidth, int texHeight, int px, int py, Color color)
    {
        if (px >= 0 && px < texWidth && py >= 0 && py < texHeight)
            data[py * texWidth + px] = color;
    }

    private static Color GetBiomeRenderColor(BiomeType biome) => biome switch
    {
        BiomeType.DeepOcean => new(28, 60, 110),
        BiomeType.ShallowWater => new(58, 118, 168),
        BiomeType.Beach => new(220, 200, 140),
        BiomeType.Desert => new(220, 190, 110),
        BiomeType.Savanna => new(200, 190, 110),
        BiomeType.Grassland => new(120, 180, 80),
        BiomeType.Forest => new(60, 130, 60),
        BiomeType.DenseForest => new(35, 95, 45),
        BiomeType.Swamp => new(80, 100, 70),
        BiomeType.Tundra => new(180, 185, 170),
        BiomeType.Mountain => new(120, 110, 100),
        BiomeType.Snow => new(235, 240, 245),
        BiomeType.CoralReef => new(0, 180, 160),
        BiomeType.Cave => new(70, 60, 50),
        BiomeType.Volcano => new(170, 50, 20),
        _ => Color.Magenta
    };

    public void Dispose()
    {
        _worldTexture?.Dispose();
        _worldTexture = null;
    }

    // Static accessors for testing
    public static Color[] GetBiomeBaseColors() => new[] { GetBiomeRenderColor((BiomeType)0), GetBiomeRenderColor((BiomeType)1), GetBiomeRenderColor((BiomeType)2), GetBiomeRenderColor((BiomeType)3), GetBiomeRenderColor((BiomeType)4), GetBiomeRenderColor((BiomeType)5), GetBiomeRenderColor((BiomeType)6), GetBiomeRenderColor((BiomeType)7), GetBiomeRenderColor((BiomeType)8), GetBiomeRenderColor((BiomeType)9), GetBiomeRenderColor((BiomeType)10), GetBiomeRenderColor((BiomeType)11), GetBiomeRenderColor((BiomeType)12), GetBiomeRenderColor((BiomeType)13), GetBiomeRenderColor((BiomeType)14) };
    public static Color[] GetBiomeDetailColors() => GetBiomeBaseColors();
    public static Color[] GetBiomeHighlightColors() => GetBiomeBaseColors();
    public static int RenderScale => 8;
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
