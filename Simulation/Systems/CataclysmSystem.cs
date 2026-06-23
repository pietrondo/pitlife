using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class CataclysmSystem : ISimulationSystem
{
    public SimulationPhase Phase => SimulationPhase.Update;
    public bool IsActive { get; private set; }
    public string ActiveEvent { get; private set; } = "";
    public float Timer { get; private set; }
    public float GrassMultiplier { get; private set; } = 1f;
    public Vector2 ImpactPosition { get; private set; }
    public float ImpactRadius { get; private set; }
    public Color ImpactColor { get; private set; } = Color.Transparent;
    public Vector2 ScreenShake { get; private set; }
    public float AnimTimer { get; private set; }
    public float AnimDuration { get; private set; } = 1.5f;

    private float _cooldownTimer = 120f;

    public void Tick(Ecosystem eco, GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed;
        Update(eco, dt, eco.Random);
        UpdateVolcanoes(eco, dt, eco.Random);
    }

    public void Initialize(World world) { }
    public void Reset() { IsActive = false; ActiveEvent = ""; GrassMultiplier = 1f; _cooldownTimer = 0; }

    public void Update(Ecosystem ecosystem, float dt, Random rng)
    {
        if (IsActive)
        {
            Timer -= dt;
            AnimTimer += dt;
            if (AnimTimer > AnimDuration) AnimTimer = AnimDuration;

            // Screen shake for earthquakes
            if (ActiveEvent == "Earthquake")
            {
                float intensity = Math.Clamp(Timer / 10f, 0f, 1f);
                ScreenShake = new Vector2(
                    (float)(rng.NextDouble() - 0.5) * 8f * intensity,
                    (float)(rng.NextDouble() - 0.5) * 8f * intensity);
            }
            else
            {
                ScreenShake = Vector2.Zero;
            }

            if (Timer <= 0)
            {
                IsActive = false;
                GrassMultiplier = 1f;
                ActiveEvent = "";
                ImpactPosition = Vector2.Zero;
                ImpactRadius = 0;
                ImpactColor = Color.Transparent;
                ScreenShake = Vector2.Zero;
                AnimTimer = 0;
                AnimDuration = 1.5f;
                Logger.Event("CATACLYSM", $"Cataclysm ended at T={ecosystem.TotalTime:F1}s");
            }
            return;
        }

        _cooldownTimer -= dt;
        if (_cooldownTimer > 0) return;

        _cooldownTimer = 180f + (float)rng.NextDouble() * 420f;

        if (rng.NextDouble() < 0.3f)
        {
            TriggerRandom(ecosystem, rng);
        }
        else if (rng.NextDouble() < 0.05f && ecosystem.TotalTime > 120f)
        {
            TriggerMassExtinction(ecosystem, rng);
        }
    }

    private void TriggerMassExtinction(Ecosystem ecosystem, Random rng)
    {
        int type = rng.Next(3);
        switch (type)
        {
            case 0:
                ActiveEvent = "Asteroid Impact";
                GrassMultiplier = 0f;
                Timer = 60f;
                break;
            case 1:
                ActiveEvent = "Ice Age";
                GrassMultiplier = 0.1f;
                Timer = 120f;
                break;
            case 2:
                ActiveEvent = "Supervolcano";
                GrassMultiplier = 0.05f;
                Timer = 90f;
                break;
        }
        IsActive = true;
        int radius = ApplyTerrainChange(ecosystem);
        if (radius > 0)
        {
            ImpactRadius = radius * ecosystem.World.TileSize;
            ImpactColor = ActiveEvent switch
            {
                "Asteroid Impact" => new Color(255, 100, 30, (int)200),
                "Supervolcano" => new Color(255, 50, 10, (int)200),
                "Ice Age" => new Color(100, 200, 255, (int)150),
                _ => Color.Transparent
            };
            AnimTimer = 0;
            AnimDuration = 2f;
        }
        Logger.Event("CATACLYSM", $"MASS EXTINCTION: {ActiveEvent} at T={ecosystem.TotalTime:F1}s, duration={Timer:F1}s");
        TryChainReaction(ecosystem, rng, ActiveEvent, ImpactPosition);
    }

    private int ApplyTerrainChange(Ecosystem ecosystem)
    {
        int radius = ActiveEvent switch
        {
            "Asteroid Impact" => 8,
            "Supervolcano" => 5,
            _ => 0
        };
        if (radius <= 0) return 0;
        int w = ecosystem.World.Width, h = ecosystem.World.Height;
        int cx = ecosystem.Random.Next(Math.Min(radius, w - radius), Math.Max(radius + 1, w - radius));
        int cy = ecosystem.Random.Next(Math.Min(radius, h - radius), Math.Max(radius + 1, h - radius));
        ImpactPosition = new Vector2(cx * ecosystem.World.TileSize, cy * ecosystem.World.TileSize);
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var tile = ecosystem.World.GetTile(cx + dx, cy + dy);
                tile.GrassAmount = 0f;
                tile.SoilNutrients = 0.1f;
            }
        Logger.Event("TERRAIN", $"{ActiveEvent} crater at ({cx},{cy}) radius={radius}");
        return radius;
    }

    public void TriggerManual(Ecosystem ecosystem, Random rng)
    {
        TriggerMassExtinction(ecosystem, rng);
    }

    private void TryChainReaction(Ecosystem ecosystem, Random rng, string type, Vector2 position)
    {
        // Earthquake on water → Tsunami
        if (type == "Earthquake")
        {
            int tx = (int)(position.X / ecosystem.World.TileSize);
            int ty = (int)(position.Y / ecosystem.World.TileSize);
            bool nearWater = false;
            for (int dy = -3; dy <= 3 && !nearWater; dy++)
                for (int dx = -3; dx <= 3 && !nearWater; dx++)
                {
                    var t = ecosystem.World.GetTile(tx + dx, ty + dy);
                    if (t.Biome == BiomeType.DeepOcean || t.Biome == BiomeType.ShallowWater)
                        nearWater = true;
                }
            if (nearWater && rng.NextDouble() < 0.4f)
            {
                Logger.Event("CATACLYSM", $"Chain: Earthquake → Tsunami at ({tx},{ty})");
                ImpactColor = new Color(30, 100, 200, 180);
                ActiveEvent = "Tsunami";
                GrassMultiplier = 2.5f;
                IsActive = true;
                Timer = 25f;
            }
        }

        // Supervolcano → volcanic winter
        if (type == "Supervolcano" && rng.NextDouble() < 0.5f)
        {
            Logger.Event("CATACLYSM", $"Chain: Supervolcano → Volcanic Winter");
            GrassMultiplier *= 0.2f;
            Timer = Math.Max(Timer, 60f);
        }
    }

    public void TriggerAt(Ecosystem ecosystem, Random rng, string type, Microsoft.Xna.Framework.Vector2 position)
    {
        ActiveEvent = type;
        IsActive = true;
        Timer = 40f;
        GrassMultiplier = type switch { "Drought" => 0.1f, "Flood" => 2.5f, _ => 0.2f };
        int tx = (int)(position.X / ecosystem.World.TileSize);
        int ty = (int)(position.Y / ecosystem.World.TileSize);
        int radius = type switch { "Asteroid" => 6, "Supervolcano" => 5, "Earthquake" => 8, _ => 3 };
        ImpactPosition = position;
        ImpactRadius = radius * ecosystem.World.TileSize;
        ImpactColor = type switch
        {
            "Asteroid" => new Color(255, 100, 30, (int)200),
            "Supervolcano" => new Color(255, 50, 10, (int)200),
            "Earthquake" => new Color(180, 140, 100, (int)150),
            "IceAge" => new Color(100, 200, 255, (int)150),
            "Drought" => new Color(255, 180, 40, (int)150),
            "Flood" => new Color(40, 140, 255, (int)150),
            _ => Color.Transparent
        };
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                int wx = tx + dx, wy = ty + dy;
                var tile = ecosystem.World.GetTile(wx, wy);
                if (tile.Biome == BiomeType.DeepOcean || tile.Biome == BiomeType.ShallowWater) continue;

                BiomeType previousBiome = tile.Biome;

                // Visible terrain changes (set Biome first - it resets grass)
                float dist = MathF.Sqrt(dx * dx + dy * dy);
                if (dist < radius * 0.4f)
                {
                    tile.Biome = type switch
                    {
                        "Asteroid" or "Supervolcano" => BiomeType.Volcano,
                        "Earthquake" => BiomeType.Cave,
                        "IceAge" => BiomeType.Snow,
                        "Flood" => BiomeType.ShallowWater,
                        _ => BiomeType.Desert
                    };
                }
                else if (dist < radius * 0.8f)
                {
                    if (type is "Asteroid" or "Supervolcano" or "Drought")
                        tile.Biome = BiomeType.Desert;
                    else if (type == "Earthquake")
                        tile.Biome = BiomeType.Mountain;
                    else if (type == "IceAge")
                        tile.Biome = BiomeType.Tundra;
                }

                bool biomeChanged = tile.Biome != previousBiome;
                if (biomeChanged)
                {
                    tile.OriginalBiome = previousBiome;
                    tile.CataclysmDamage = 1f;
                }

                // Then override grass/soil
                tile.GrassAmount = type == "Flood" ? tile.MaxGrass : 0f;
                tile.SoilNutrients = type == "Flood" ? 2f : 0.1f;
            }
        Logger.Event("CATACLYSM", $"Player {type} at ({tx},{ty}) r={radius}");
        TryChainReaction(ecosystem, rng, type, position);
    }

    public void UpdateVolcanoes(Ecosystem ecosystem, float dt, Random rng)
    {
        for (int y = 0; y < ecosystem.World.Height; y++)
            for (int x = 0; x < ecosystem.World.Width; x++)
            {
                if (ecosystem.World.Tiles[x, y].Biome != BiomeType.Volcano) continue;
                if (rng.NextDouble() < 0.0005f * dt)
                {
                    int r = rng.Next(2, 4);
                    for (int dy = -r; dy <= r; dy++)
                        for (int dx = -r; dx <= r; dx++)
                        {
                            var t = ecosystem.World.GetTile(x + dx, y + dy);
                            if (t.Biome != BiomeType.DeepOcean && t.Biome != BiomeType.ShallowWater)
                            { t.GrassAmount = 0f; t.SoilNutrients = Math.Min(2f, t.SoilNutrients + 0.4f); }
                        }
                }
            }
    }

    private void TriggerRandom(Ecosystem ecosystem, Random rng)
    {
        int type = rng.Next(4);
        switch (type)
        {
            case 0:
                ActiveEvent = "Drought";
                GrassMultiplier = 0.1f;
                Timer = 30f + (float)rng.NextDouble() * 30f;
                break;
            case 1:
                ActiveEvent = "Flood";
                GrassMultiplier = 2.5f;
                Timer = 15f + (float)rng.NextDouble() * 15f;
                break;
            case 2:
                ActiveEvent = "Firestorm";
                GrassMultiplier = 0f;
                Timer = 10f + (float)rng.NextDouble() * 10f;
                break;
            case 3:
                ActiveEvent = "Bloom";
                GrassMultiplier = 3f;
                Timer = 20f + (float)rng.NextDouble() * 20f;
                break;
        }
        IsActive = true;
        Logger.Event("CATACLYSM", $"{ActiveEvent} started at T={ecosystem.TotalTime:F1}s, duration={Timer:F1}s");
    }

    public void Draw(SpriteBatch sb, Texture2D pixel)
    {
        if (!IsActive || ImpactRadius <= 0) return;

        float progress = AnimTimer / AnimDuration; // 0..1
        Vector2 pos = ImpactPosition;
        float maxR = ImpactRadius * 0.8f;

        if (ActiveEvent is "Asteroid" or "Asteroid Impact")
        {
            // Falling meteor (first 0.3s)
            if (progress < 0.3f)
            {
                float t = progress / 0.3f;
                float meteorY = -100 + (pos.Y + 100) * (t * t); // accelerate down
                var mColor = new Color(255, 200, 50);
                DrawFireball(sb, pixel, new Vector2(pos.X, meteorY), 6 + (1-t)*12, mColor, 255);

                // Smoke trail
                for (int i = 1; i <= 4; i++)
                {
                    float trailY = meteorY + i * 20;
                    byte alpha = (byte)((1f - i * 0.25f) * 180);
                    var trailColor = new Color(100, 80, 40, (int)alpha);
                    DrawFireball(sb, pixel, new Vector2(pos.X, trailY), 4 + i, trailColor, alpha);
                }
            }

            // Explosion after impact
            float explodeT = Math.Max(0, (progress - 0.3f) / 0.7f);
            float ringR = maxR * explodeT;
            var explodeColor = new Color(255, 150, 30);
            // Expanding shock rings
            for (int r = 0; r < 3; r++)
            {
                float rT = (explodeT + r * 0.3f) % 1.0f;
                float rr = maxR * rT;
                byte alpha = (byte)((1f - rT) * 128);
                DrawRing(sb, pixel, pos, rr, new Color(255, 180, 40, (int)alpha), 3);
            }
            // Fire particles at impact
            DrawFireCluster(sb, pixel, pos, maxR * 0.6f, explodeT, progress);
        }
        else if (ActiveEvent == "Supervolcano")
        {
            // Rising magma column from ground
            float intensity = Math.Min(1f, progress * 2f) * Math.Max(0f, 1f - progress * 1.5f);
            float colH = maxR * 2f * intensity;
            // Magma column (vertical stream going up)
            for (int stripe = 0; stripe < 3; stripe++)
            {
                float sx = pos.X + (stripe - 1) * maxR * 0.2f;
                float sy = pos.Y - colH * (0.5f + stripe * 0.2f);
                DrawFireball(sb, pixel, new Vector2(sx, sy), 3 + stripe * 2, new Color(255, 60, 10), (byte)(intensity * 180));
            }
            // Rising lava blobs
            for (int i = 0; i < 6; i++)
            {
                float angle = (float)Math.Sin(progress * 5f + i * 1.2f) * 0.5f;
                float rise = (progress + i * 0.1f) % 1f;
                float bx = pos.X + angle * maxR * 0.4f;
                float by = pos.Y - rise * colH;
                DrawFireball(sb, pixel, new Vector2(bx, by), 2 + i % 3, new Color(255, 140, 20), (byte)(intensity * 200));
            }
            // Lava pool at base
            DrawRing(sb, pixel, pos, maxR * progress * 0.7f, new Color(255, 40, 10, (int)(intensity * 150)), 5);
            DrawFireCluster(sb, pixel, pos, maxR * 0.4f * progress, progress, progress);
        }
        else if (ActiveEvent == "Firestorm")
        {
            DrawFireCluster(sb, pixel, pos, maxR, progress, progress);
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 1.256f + progress * 3f;
                float fx = pos.X + MathF.Cos(angle) * maxR * (0.5f + progress * 0.5f);
                float fy = pos.Y + MathF.Sin(angle) * maxR * (0.5f + progress * 0.5f);
                DrawFireball(sb, pixel, new Vector2(fx, fy), 3 + progress * 4, new Color(255, 160, 30), 180);
            }
        }
        else if (ActiveEvent is "IceAge" or "Ice Age")
        {
            // Frost spread
            float frostR = maxR * progress;
            var frostColors = new[] { new Color(180, 220, 255, (int)60), new Color(150, 200, 240, (int)40), new Color(200, 230, 255, (int)30) };
            for (int r = 0; r < 3; r++)
            {
                float cr = frostR * (1f - r * 0.3f);
                DrawRing(sb, pixel, pos, cr, frostColors[r], (int)(2 + r * 2));
            }
            // Snowflakes
            for (int i = 0; i < 8; i++)
            {
                float sx = pos.X + MathF.Cos(i * 0.8f + progress * 5f) * frostR * 0.8f;
                float sy = pos.Y + MathF.Sin(i * 0.8f + progress * 2f) * frostR * 0.8f;
                DrawFireball(sb, pixel, new Vector2(sx, sy), 2, new Color(255, 255, 255, (int)150), 150);
            }
        }
        else if (ActiveEvent == "Earthquake")
        {
            // Crack lines radiating from center
            float crackR = maxR * progress;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 1.047f + progress * 0.5f;
                float endX = pos.X + MathF.Cos(angle) * crackR;
                float endY = pos.Y + MathF.Sin(angle) * crackR;
                DrawLine(sb, pixel, pos, new Vector2(endX, endY), new Color(100, 80, 60, (int)120), 2);
            }
        }
        else if (ActiveEvent == "Drought")
        {
            // Heat shimmer rings
            for (int r = 0; r < 4; r++)
            {
                float rr = maxR * (1f - (progress + r * 0.2f) % 1f);
                DrawRing(sb, pixel, pos, rr, new Color(255, 200, 80, (int)(progress * 80)), 2);
            }
        }
        else if (ActiveEvent == "Flood")
        {
            // Water waves
            for (int r = 0; r < 4; r++)
            {
                float rr = maxR * (progress + r * 0.25f) % maxR;
                DrawRing(sb, pixel, pos, rr, new Color(60, 160, 240, (int)((1f - rr / maxR) * 120)), 3);
            }
        }
        else if (ActiveEvent == "Bloom")
        {
            // Flower/grass particles
            for (int i = 0; i < 10; i++)
            {
                float angle = i * 0.628f + progress * 2f;
                float fx = pos.X + MathF.Cos(angle) * maxR * progress;
                float fy = pos.Y + MathF.Sin(angle) * maxR * progress;
                DrawFireball(sb, pixel, new Vector2(fx, fy), 3, new Color(100, 220, 80, (int)120), 120);
            }
        }

        // Generic impact ring for all types
        float genR = maxR * 0.5f * (1f + progress);
        byte genA = (byte)((1f - progress) * 60);
        if (genA > 0)
            DrawRing(sb, pixel, pos, genR, new Color(ImpactColor.R, ImpactColor.G, ImpactColor.B, genA), 2);
    }

    private static void DrawFireball(SpriteBatch sb, Texture2D p, Vector2 pos, float r, Color c, byte alpha)
    {
        var color = new Color(c.R, c.G, c.B, alpha);
        for (int dy = -(int)r; dy <= (int)r; dy++)
            for (int dx = -(int)r; dx <= (int)r; dx++)
            {
                if (dx * dx + dy * dy <= r * r)
                    sb.Draw(p, new Rectangle((int)pos.X + dx, (int)pos.Y + dy, 1, 1), color);
            }
    }

    private static void DrawRing(SpriteBatch sb, Texture2D p, Vector2 pos, float r, Color c, int thickness)
    {
        for (int dy = -(int)r - thickness; dy <= (int)r + thickness; dy++)
            for (int dx = -(int)r - thickness; dx <= (int)r + thickness; dx++)
            {
                float d = MathF.Sqrt(dx * dx + dy * dy);
                if (d >= r - thickness && d <= r)
                    sb.Draw(p, new Rectangle((int)pos.X + dx, (int)pos.Y + dy, 1, 1), c);
            }
    }

    private void DrawFireCluster(SpriteBatch sb, Texture2D p, Vector2 pos, float radius, float t, float progress)
    {
        int seed = (int)(t * 100);
        var rng = new Random(seed);
        for (int i = 0; i < 20; i++)
        {
            float angle = (float)rng.NextDouble() * 6.28f;
            float dist = (float)rng.NextDouble() * radius * (0.7f + 0.3f * MathF.Sin(progress * 3f + i));
            float fx = pos.X + MathF.Cos(angle) * dist;
            float fy = pos.Y + MathF.Sin(angle) * dist;
            int size = 1 + rng.Next(3);
            byte alpha = (byte)(160 + rng.Next(95));
            var fc = new Color(255, 100 + rng.Next(155), 10 + rng.Next(30));
            DrawFireball(sb, p, new Vector2(fx, fy), size, fc, alpha);
        }
    }

    private static void DrawLine(SpriteBatch sb, Texture2D p, Vector2 from, Vector2 to, Color c, int thickness)
    {
        float dx = to.X - from.X;
        float dy = to.Y - from.Y;
        float len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1) return;
        float nx = dx / len, ny = dy / len;
        for (int i = 0; i < (int)len; i++)
        {
            int px = (int)(from.X + nx * i);
            int py = (int)(from.Y + ny * i);
            sb.Draw(p, new Rectangle(px, py, 1, thickness), c);
        }
    }
}
