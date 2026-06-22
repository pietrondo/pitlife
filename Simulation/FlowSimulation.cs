using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.Simulation;

public sealed class FlowSimulation : IDisposable
{
    private readonly World _world;
    private float[,] _water;
    private float[,] _lava;
    private float[,] _humidity;
    private RenderTarget2D? _overlay;
    private bool _dirty = true;
    private float _timeAccum;

    public FlowSimulation(World world)
    {
        _world = world;
        _water = new float[world.Width, world.Height];
        _lava = new float[world.Width, world.Height];
        _humidity = new float[world.Width, world.Height];

        for (int y = 0; y < world.Height; y++)
            for (int x = 0; x < world.Width; x++)
            {
                var b = world.Tiles[x, y].Biome;
                if (b == BiomeType.DeepOcean) _water[x, y] = 1f;
                else if (b == BiomeType.ShallowWater || b == BiomeType.CoralReef) _water[x, y] = 0.7f;
                else if (world.RiverMask[y * world.Width + x]) _water[x, y] = 0.5f;
                if (b == BiomeType.Volcano) _lava[x, y] = 0.4f;
                _humidity[x, y] = _water[x, y];
            }
    }

    public void Invalidate() => _dirty = true;

    public void Update(float dt, Random rng)
    {
        _timeAccum += dt;
        if (_timeAccum < 0.5f) return;
        _timeAccum = 0f;

        int w = _world.Width, h = _world.Height;
        var nw = new float[w, h];
        var nl = new float[w, h];
        var nh = new float[w, h];

        for (int y = 1; y < h - 1; y++)
            for (int x = 1; x < w - 1; x++)
            {
                float myElev = _world.ElevationField[y * w + x];
                float w0 = _water[x, y];
                float l0 = _lava[x, y];
                nw[x, y] = w0;
                nl[x, y] = l0;

                // Flow to lower neighbors
                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        int nx = x + dx, ny = y + dy;
                        float ne = _world.ElevationField[ny * w + nx];
                        float diff = myElev - ne;
                        if (diff <= 0) continue;
                        float rate = diff * 0.15f;
                        float wf = Math.Min(w0 * rate, w0 * 0.2f);
                        nw[x, y] -= wf;
                        nw[nx, ny] += wf;
                        float lf = Math.Min(l0 * rate * 0.3f, l0 * 0.1f);
                        nl[x, y] -= lf;
                        nl[nx, ny] += lf;
                    }

                // Evaporation from land water
                float temp = TileTemperature(x, y, _world.Tiles[x, y].Biome);
                float evap = w0 * 0.02f * (1f + temp / 40f);
                nw[x, y] = Math.Max(0, nw[x, y] - evap);

                // Volcano generates lava
                if (_world.Tiles[x, y].Biome == BiomeType.Volcano)
                    nl[x, y] = Math.Min(1f, nl[x, y] + 0.1f);

                // River source regenerates
                if (_world.RiverMask[y * w + x])
                    nw[x, y] = Math.Min(1f, nw[x, y] + 0.05f);

                // Humidity: influenced by water and temperature
                float localHum = 0f;
                for (int dy = -2; dy <= 2; dy++)
                    for (int dx = -2; dx <= 2; dx++)
                    {
                        int sx = Math.Clamp(x + dx, 0, w - 1);
                        int sy = Math.Clamp(y + dy, 0, h - 1);
                        localHum += _water[sx, sy];
                    }
                localHum /= 25f;
                nh[x, y] = MathHelper.Clamp(localHum * (1f + temp / 50f), 0f, 1f);
            }

        _water = nw;
        _lava = nl;
        _humidity = nh;
        _dirty = true;
    }

    private static float TileTemperature(int x, int y, BiomeType biome)
    {
        float latFactor = Math.Abs(y / 100f - 0.5f) * 2f;
        return biome switch
        {
            BiomeType.Desert => 38f - latFactor * 15f,
            BiomeType.Savanna => 32f - latFactor * 10f,
            BiomeType.Grassland => 24f - latFactor * 8f,
            BiomeType.Forest => 20f - latFactor * 6f,
            BiomeType.DenseForest => 18f - latFactor * 5f,
            BiomeType.Swamp => 22f - latFactor * 8f,
            BiomeType.Tundra => 5f - latFactor * 5f,
            BiomeType.Snow => -10f - latFactor * 10f,
            BiomeType.Mountain => 10f - latFactor * 15f,
            BiomeType.Beach => 25f - latFactor * 5f,
            BiomeType.ShallowWater => 22f - latFactor * 8f,
            BiomeType.DeepOcean => 15f - latFactor * 10f,
            BiomeType.CoralReef => 26f - latFactor * 5f,
            BiomeType.Cave => 12f,
            BiomeType.Volcano => 45f + latFactor * 5f,
            _ => 20f
        };
    }

    public float GetWater(int tx, int ty) => tx >= 0 && tx < _world.Width && ty >= 0 && ty < _world.Height ? _water[tx, ty] : 0;
    public float GetHumidity(int tx, int ty) => tx >= 0 && tx < _world.Width && ty >= 0 && ty < _world.Height ? _humidity[tx, ty] : 0;

    public void DrawOverlay(SpriteBatch sb, int tileSize, int renderScale)
    {
        int tw = _world.Width * renderScale;
        int th = _world.Height * renderScale;

        if (_overlay == null || _overlay.Width != tw || _overlay.Height != th)
        {
            _overlay?.Dispose();
            _overlay = new RenderTarget2D(sb.GraphicsDevice, tw, th, false, SurfaceFormat.Color, DepthFormat.None);
            _dirty = true;
        }

        if (_dirty)
        {
            Color[] data = new Color[tw * th];
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int tx = x / renderScale;
                    int ty = y / renderScale;
                    if (tx >= _world.Width || ty >= _world.Height) continue;

                    float w = Sample(_water, tx, ty, x % renderScale, y % renderScale, renderScale);
                    float l = Sample(_lava, tx, ty, x % renderScale, y % renderScale, renderScale);

                    byte r = (byte)(l * 255);
                    byte g = 0;
                    byte b = (byte)(w * 200);
                    byte a = (byte)((w * 120 + l * 200));

                    data[y * tw + x] = new Color(r, g, b, a);
                }

            _overlay.SetData(data);
            sb.GraphicsDevice.SetRenderTarget(null);
            _dirty = false;
        }

        sb.Draw(_overlay, new Rectangle(0, 0, _world.PixelWidth, _world.PixelHeight), Color.White);
    }

    private static float Sample(float[,] field, int tx, int ty, int sx, int sy, int scale)
    {
        float fx = sx / (float)scale;
        float fy = sy / (float)scale;
        int w = field.GetLength(0), h = field.GetLength(1);
        float v00 = field[tx, ty];
        float v10 = tx + 1 < w ? field[tx + 1, ty] : v00;
        float v01 = ty + 1 < h ? field[tx, ty + 1] : v00;
        float v11 = (tx + 1 < w && ty + 1 < h) ? field[tx + 1, ty + 1] : v00;
        return MathHelper.Clamp(
            v00 * (1 - fx) * (1 - fy) + v10 * fx * (1 - fy) + v01 * (1 - fx) * fy + v11 * fx * fy, 0, 1);
    }

    public void Dispose() => _overlay?.Dispose();
}
