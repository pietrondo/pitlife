using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class FlowSimulation : IDisposable
{
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
        _overlayScale = Math.Max(1, FlowConfig.Data.Visuals.OverlayScaleNumerator / FlowConfig.Data.Visuals.OverlayScaleDenominator);
        int w = world.Width, h = world.Height;
        _water = new float[w, h];
        _lava = new float[w, h];
        _flowDir = new Vector2[w, h];

        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                var b = world.Tiles[x, y].Biome;
                if (b == BiomeType.DeepOcean) _water[x, y] = FlowConfig.Data.InitialLevels.DeepOceanWater;
                else if (b == BiomeType.ShallowWater || b == BiomeType.CoralReef) _water[x, y] = FlowConfig.Data.InitialLevels.ShallowWaterWater;
                else if (world.RiverMask[y * w + x]) _water[x, y] = FlowConfig.Data.InitialLevels.RiverWater;
                if (b == BiomeType.Volcano) _lava[x, y] = FlowConfig.Data.InitialLevels.VolcanoLava;
            }
    }

    public void Invalidate() => _dirty = true;

    public void Tick(Ecosystem eco, GameTime gameTime) => Update((float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed, eco.Random);

    public void Reset() { Invalidate(); _timeAccum = 0; }

    public void Update(float dt, Random rng)
    {
        _timeAccum += dt;
        if (_timeAccum < FlowConfig.Data.FlowRates.TickAccumulatorThreshold) return;
        _timeAccum = 0f;

        int w = _world.Width, h = _world.Height;

        var nw = System.Buffers.ArrayPool<float>.Shared.Rent(w * h);
        var nl = System.Buffers.ArrayPool<float>.Shared.Rent(w * h);

        try
        {
            Array.Clear(nw, 0, w * h);
            Array.Clear(nl, 0, w * h);

            for (int y = 2; y < h - 2; y += 2)
                for (int x = 2; x < w - 2; x += 2)
                {
                    float myElev = _world.ElevationField[y * w + x];
                    float w0 = _water[x, y], l0 = _lava[x, y];
                    nw[y * w + x] = w0; nl[y * w + x] = l0;
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
                            float rate = diff * FlowConfig.Data.FlowRates.WaterFlowRateBase;
                            float wf = Math.Min(w0 * rate, w0 * FlowConfig.Data.FlowRates.WaterFlowRateMax);
                            nw[y * w + x] -= wf; nw[(y + dy) * w + (x + dx)] += wf;
                            float lf = Math.Min(l0 * rate * FlowConfig.Data.FlowRates.LavaFlowRateMultiplier, l0 * FlowConfig.Data.FlowRates.LavaFlowRateMax);
                            nl[y * w + x] -= lf; nl[(y + dy) * w + (x + dx)] += lf;
                        }

                    _flowDir[x, y] = bestDir;

                    float temp = TileTemperature(x, y, _world.Tiles[x, y].Biome);
                    nw[y * w + x] = Math.Max(0, nw[y * w + x] - w0 * FlowConfig.Data.FlowRates.EvaporationBaseRate * (1f + temp / FlowConfig.Data.FlowRates.EvaporationTempDivisor));

                    if (_world.Tiles[x, y].Biome == BiomeType.Volcano)
                        nl[y * w + x] = Math.Min(1f, nl[y * w + x] + FlowConfig.Data.FlowRates.VolcanoLavaRegen);
                    if (_world.RiverMask[y * w + x])
                        nw[y * w + x] = Math.Min(1f, nw[y * w + x] + FlowConfig.Data.FlowRates.RiverWaterRegen);
                }

            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    _water[x, y] = nw[y * w + x];
                    _lava[x, y] = nl[y * w + x];
                }
        }
        finally
        {
            System.Buffers.ArrayPool<float>.Shared.Return(nw);
            System.Buffers.ArrayPool<float>.Shared.Return(nl);
        }

        _dirty = true;
    }

    private static float TileTemperature(int x, int y, BiomeType biome)
    {
        float latFactor = Math.Abs(y / 100f - 0.5f) * 2f;
        return biome switch
        {
            BiomeType.Desert => FlowConfig.Data.Temperature.DesertTemp - latFactor * FlowConfig.Data.Temperature.DesertLatMod,
            BiomeType.Savanna => FlowConfig.Data.Temperature.SavannaTemp,
            BiomeType.Volcano => FlowConfig.Data.Temperature.VolcanoTemp,
            BiomeType.Snow => FlowConfig.Data.Temperature.SnowTemp - latFactor * FlowConfig.Data.Temperature.SnowLatMod,
            BiomeType.Tundra => FlowConfig.Data.Temperature.TundraTemp,
            BiomeType.DeepOcean => FlowConfig.Data.Temperature.DeepOceanTemp,
            _ => FlowConfig.Data.Temperature.DefaultTemp - latFactor * FlowConfig.Data.Temperature.DefaultLatMod
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

                    byte r = (byte)(l * FlowConfig.Data.Visuals.LavaRedMultiplier);
                    byte g = 0;
                    byte b = (byte)(w * FlowConfig.Data.Visuals.WaterBlueMultiplier);
                    byte a = (byte)((w * FlowConfig.Data.Visuals.WaterAlphaMultiplier + l * FlowConfig.Data.Visuals.LavaAlphaMultiplier));

                    if (w > 0.1f && _world.RiverMask[ty * _world.Width + tx])
                    {
                        float phase = (x + y + _animTime * FlowConfig.Data.Visuals.RiverAnimPhaseMultiplier) % (_overlayScale * FlowConfig.Data.Visuals.RiverAnimScaleMultiplier);
                        if (phase < _overlayScale)
                        { a = (byte)Math.Min(255, a + FlowConfig.Data.Visuals.RiverAlphaBoost); b = (byte)Math.Min(255, b + FlowConfig.Data.Visuals.RiverBlueBoost); }
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
