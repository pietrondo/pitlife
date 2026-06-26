using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

/// <summary>
/// Represents the WeatherSystem.
/// </summary>
public sealed class WeatherSystem
{
    private struct Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Life;
    }

    private Particle[] _particles = [];
    private int _count;
    private readonly Random _rng = new();
    private const int MaxRain = 200;
    private const int MaxSnow = 150;

    /// <summary>
    /// Executes the Update.
    /// </summary>
    /// <param name="climate">The climate parameter.</param>
    /// <param name="camera">The camera parameter.</param>
    /// <param name="dt">The dt parameter.</param>
    /// <param name="worldW">The worldW parameter.</param>
    /// <param name="worldH">The worldH parameter.</param>
    public void Update(ClimateSystem climate, Camera camera, float dt, int worldW, int worldH)
    {
        bool isSnow = climate.CurrentSeason == Season.Winter || climate.TemperatureModifier < -0.05f;
        bool isRain = !isSnow && (climate.CurrentSeason == Season.Autumn || climate.CurrentSeason == Season.Spring);

        int target = 0;
        if (isSnow) target = MaxSnow;
        else if (isRain) target = MaxRain;

        if (_particles.Length < target)
            _particles = new Particle[target];

        float windDir = climate.WindDirection;
        float windSpeed = climate.WindSpeed;
        windSpeed += MathF.Sin(climate.SeasonProgress * MathF.PI * 2f) * 0.5f;

        for (int i = _count - 1; i >= 0; i--)
        {
            ref var p = ref _particles[i];
            p.Life -= dt;
            if (p.Life <= 0)
            {
                _particles[i] = _particles[_count - 1];
                _count--;
                continue;
            }

            float sway = isSnow ? MathF.Sin(p.Life * 3f + p.Position.X * 0.01f) * 20f : 0f;
            float fallSpeed = isSnow ? 40f + _rng.NextSingle() * 60f : 250f + _rng.NextSingle() * 200f;
            p.Position.X += (MathF.Cos(windDir) * windSpeed * 30f + sway) * dt;
            p.Position.Y += fallSpeed * dt;

            if (p.Position.Y > worldH || p.Position.X < -100 || p.Position.X > worldW + 100)
                p.Life = 0;
        }

        float viewLeft = camera.Position.X - camera.ViewportWidth / (2f * camera.Zoom);
        float viewRight = camera.Position.X + camera.ViewportWidth / (2f * camera.Zoom);
        float viewTop = camera.Position.Y - camera.ViewportHeight / (2f * camera.Zoom);
        float viewHeight = camera.ViewportHeight / camera.Zoom;

        int spawnPerFrame = isSnow ? 4 : (isRain ? 8 : 0);
        for (int i = 0; i < spawnPerFrame && _count < target; i++)
        {
            float x = viewLeft + _rng.NextSingle() * (viewRight - viewLeft);
            float y = viewTop - _rng.NextSingle() * viewHeight * 0.5f;
            float life = isSnow ? 3f + _rng.NextSingle() * 5f : 0.5f + _rng.NextSingle() * 0.8f;
            _particles[_count++] = new Particle
            {
                Position = new Vector2(x, y),
                Velocity = new Vector2(0, isSnow ? 40f : 300f),
                Life = life
            };
        }
    }

    /// <summary>
    /// Executes the Draw.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="pixel">The pixel parameter.</param>
    /// <param name="isSnow">The isSnow parameter.</param>
    public void Draw(SpriteBatch sb, Texture2D pixel, bool isSnow)
    {
        for (int i = 0; i < _count; i++)
        {
            ref var p = ref _particles[i];
            if (p.Life <= 0) continue;
            Color color = isSnow
                ? new Color(255, 255, 255, (int)(180 * Math.Min(p.Life, 1f)))
                : new Color(140, 180, 255, (int)(120 * Math.Min(p.Life * 2f, 1f)));
            int px = (int)p.Position.X;
            int py = (int)p.Position.Y;
            if (isSnow)
            {
                sb.Draw(pixel, new Rectangle(px - 1, py - 1, 2, 2), color);
            }
            else
            {
                int len = (int)(4 + p.Velocity.Y * 0.01f);
                sb.Draw(pixel, new Rectangle(px, py, 1, len), color);
            }
        }
    }
}
