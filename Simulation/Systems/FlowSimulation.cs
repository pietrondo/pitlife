using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.Simulation;

public sealed class FlowSimulation : ISimulationSystem, IDisposable
{

    public SimulationPhase Phase => SimulationPhase.Update;

    private readonly World _world;
    private float[,] _water, _lava;
    private Vector2[,] _flowDir;
    private RenderTarget2D? _overlay;
    private Color[]? _pixels;
    private bool _dirty = true;
    private float _timeAccum, _animTime;
    private int _overlayScale;

    public FlowSimulation(World world)
    {
        _world = world;
        _overlayScale = Math.Max(1, 16 / 4);
        int w = world.Width, h = world.Height;
        _water = new float[w, h];
        _lava = new float[w, h];
        _flowDir = new Vector2[w, h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var b = world.Tiles[x, y].Biome;
                if (b == BiomeType.DeepOcean) _water[x, y] = 1f;
                else if (b == BiomeType.ShallowWater || b == BiomeType.CoralReef) _water[x, y] = 0.7f;
                else if (world.RiverMask[y * w + x]) _water[x, y] = 0.5f;
                if (b == BiomeType.Volcano) _lava[x, y] = 0.4f;
            }
    }

    public void Invalidate() => _dirty = true;

    public void Tick(Ecosystem eco, GameTime gameTime) => Update((float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed, eco.Random);

    public void Initialize(World world) { }
    public void Reset() { Invalidate(); _timeAccum = 0; }

    public void Update(float dt, Random rng)
    {
        _timeAccum += dt;
        if (_timeAccum < 1f) return;
        _timeAccum = 0f;

        int w = _world.Width, h = _world.Height;
        var nw = new float[w, h];
        var nl = new float[w, h];

        for (int y = 2; y < h - 2; y += 2)
            for (int x = 2; x < w - 2; x += 2)
            {
                float myElev = _world.ElevationField[y * w + x];
                float w0 = _water[x, y], l0 = _lava[x, y];
                nw[x, y] = w0; nl[x, y] = l0;
                Vector2 bestDir = Vector2.Zero;
                float bestDiff = 0;

                for (int dy = -1; dy <= 1; dy++)
                    for (int dx = -1; dx <= 1; dx++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        float ne = _world.ElevationField[(y + dy) * w + (x + dx)];
                        float diff = myElev - ne;
                        if (diff <= 0) continue;
                        if (diff > bestDiff) { bestDiff = diff; bestDir = new Vector2(dx, dy); }
                        float rate = diff * 0.1f;
                        float wf = Math.Min(w0 * rate, w0 * 0.25f);
                        nw[x, y] -= wf; nw[x + dx, y + dy] += wf;
                        float lf = Math.Min(l0 * rate * 0.3f, l0 * 0.1f);
                        nl[x, y] -= lf; nl[x + dx, y + dy] += lf;
                    }

                _flowDir[x, y] = bestDir;

                float temp = TileTemperature(x, y, _world.Tiles[x, y].Biome);
                nw[x, y] = Math.Max(0, nw[x, y] - w0 * 0.01f * (1f + temp / 40f));

                if (_world.Tiles[x, y].Biome == BiomeType.Volcano)
                    nl[x, y] = Math.Min(1f, nl[x, y] + 0.1f);
                if (_world.RiverMask[y * w + x])
                    nw[x, y] = Math.Min(1f, nw[x, y] + 0.05f);
            }

        _water = nw; _lava = nl;
        _dirty = true;
    }

    private static float TileTemperature(int x, int y, BiomeType biome)
    {
        float latFactor = Math.Abs(y / 100f - 0.5f) * 2f;
        return biome switch
        {
            BiomeType.Desert => 38f - latFactor * 15f,
            BiomeType.Savanna => 32f,
            BiomeType.Volcano => 45f,
            BiomeType.Snow => -10f - latFactor * 10f,
            BiomeType.Tundra => 5f,
            BiomeType.DeepOcean => 15f,
            _ => 20f - latFactor * 5f
        };
    }

    public void DrawOverlay(SpriteBatch sb, int tileSize)
    {
        _animTime += 0.016f;
        int tw = _world.Width * _overlayScale;
        int th = _world.Height * _overlayScale;

        if (_overlay == null || _overlay.Width != tw || _overlay.Height != th)
        {
            _overlay?.Dispose();
            _overlay = new RenderTarget2D(sb.GraphicsDevice, tw, th, false, SurfaceFormat.Color, DepthFormat.None);
            _pixels = new Color[tw * th];
            _dirty = true;
        }

        if (_dirty && _pixels != null)
        {
            for (int y = 0; y < th; y++)
                for (int x = 0; x < tw; x++)
                {
                    int tx = x / _overlayScale, ty = y / _overlayScale;
                    if (tx >= _world.Width || ty >= _world.Height) continue;

                    float w = _water[tx, ty];
                    float l = _lava[tx, ty];

                    byte r = (byte)(l * 200);
                    byte g = 0;
                    byte b = (byte)(w * 160);
                    byte a = (byte)((w * 100 + l * 180));

                    if (w > 0.1f && _world.RiverMask[ty * _world.Width + tx])
                    {
                        float phase = (x + y + _animTime * 40f) % (_overlayScale * 2f);
                        if (phase < _overlayScale)
                        { a = (byte)Math.Min(255, a + 70); b = (byte)Math.Min(255, b + 40); }
                    }

                    _pixels[y * tw + x] = new Color(r, g, b, a);
                }

            _overlay.SetData(_pixels);
            sb.GraphicsDevice.SetRenderTarget(null);
            _dirty = false;
        }

        if (_overlay != null)
            sb.Draw(_overlay, new Rectangle(0, 0, _world.PixelWidth, _world.PixelHeight), Color.White);
    }

    public void Dispose() { _overlay?.Dispose(); }
}
