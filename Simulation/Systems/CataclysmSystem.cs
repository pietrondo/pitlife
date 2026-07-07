using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class CataclysmSystem
{
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

    private float _cooldownTimer = CataclysmConfig.Data.Chances.RandomCooldownMin;

    public void Tick(Ecosystem eco, GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed;
        Update(eco, dt, eco.Random);
        UpdateVolcanoes(eco, dt, eco.Random);
    }


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
                var intensity = Math.Clamp(Timer / 10f, 0f, 1f);
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

        _cooldownTimer = CataclysmConfig.Data.Chances.RandomCooldownMin + (float)rng.NextDouble() * CataclysmConfig.Data.Chances.RandomCooldownSpread;

        if (rng.NextDouble() < CataclysmConfig.Data.Chances.RandomTriggerChance)
        {
            TriggerRandom(ecosystem, rng);
        }
        else if (rng.NextDouble() < CataclysmConfig.Data.Chances.MassExtinctionChance && ecosystem.TotalTime > CataclysmConfig.Data.Chances.MassExtinctionMinTime)
        {
            TriggerMassExtinction(ecosystem, rng);
        }
    }

    private void TriggerMassExtinction(Ecosystem ecosystem, Random rng)
    {
        var extinctions = CataclysmConfig.Data.MassExtinctions;
        if (extinctions.Count == 0) return;
        var ev = extinctions[rng.Next(extinctions.Count)];

        ActiveEvent = ev.Name;
        GrassMultiplier = ev.GrassMultiplier;
        Timer = ev.Duration;
        IsActive = true;

        var radius = ApplyTerrainChange(ecosystem, ev.Radius);
        if (radius > 0)
        {
            ImpactRadius = radius * ecosystem.World.TileSize;
            ImpactColor = ev.Color.ToColor();
            AnimTimer = 0;
            AnimDuration = 2f;
        }
        Logger.Event("CATACLYSM", $"MASS EXTINCTION: {ActiveEvent} at T={ecosystem.TotalTime:F1}s, duration={Timer:F1}s");
        TryChainReaction(ecosystem, rng, ActiveEvent, ImpactPosition);
    }

    private int ApplyTerrainChange(Ecosystem ecosystem, int defaultRadius = 0)
    {
        var radius = defaultRadius;
        if (radius <= 0) return 0;
        int w = ecosystem.World.Width, h = ecosystem.World.Height;
        var cx = ecosystem.Random.Next(Math.Min(radius, w - radius), Math.Max(radius + 1, w - radius));
        var cy = ecosystem.Random.Next(Math.Min(radius, h - radius), Math.Max(radius + 1, h - radius));
        ImpactPosition = new Vector2(cx * ecosystem.World.TileSize, cy * ecosystem.World.TileSize);
        for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
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
        var chains = CataclysmConfig.Data.ChainReactions;

        // Earthquake on water → Tsunami
        if (type == "Earthquake")
        {
            var tx = (int)(position.X / ecosystem.World.TileSize);
            var ty = (int)(position.Y / ecosystem.World.TileSize);
            var nearWater = false;
            for (var dy = -chains.EarthquakeTsunamiRadius; dy <= chains.EarthquakeTsunamiRadius && !nearWater; dy++)
                for (var dx = -chains.EarthquakeTsunamiRadius; dx <= chains.EarthquakeTsunamiRadius && !nearWater; dx++)
                {
                    var t = ecosystem.World.GetTile(tx + dx, ty + dy);
                    if (t.Biome == BiomeType.DeepOcean || t.Biome == BiomeType.ShallowWater)
                        nearWater = true;
                }
            if (nearWater && rng.NextDouble() < chains.EarthquakeTsunamiChance)
            {
                Logger.Event("CATACLYSM", $"Chain: Earthquake → Tsunami at ({tx},{ty})");
                ImpactColor = new Color(30, 100, 200, 180);
                ActiveEvent = "Tsunami";
                GrassMultiplier = chains.TsunamiGrassMultiplier;
                IsActive = true;
                Timer = chains.TsunamiDuration;
            }
        }

        // Supervolcano → volcanic winter
        if (type == "Supervolcano" && rng.NextDouble() < chains.SupervolcanoWinterChance)
        {
            Logger.Event("CATACLYSM", $"Chain: Supervolcano → Volcanic Winter");
            GrassMultiplier *= chains.SupervolcanoWinterGrassMultiplier;
            Timer = Math.Max(Timer, chains.WinterMinDuration);
        }
    }

    public void TriggerAt(Ecosystem ecosystem, Random rng, string type, Microsoft.Xna.Framework.Vector2 position)
    {
        var ev = CataclysmConfig.Data.PlayerEvents.Find(e => e.Name == type);
        if (ev == null) return;

        ActiveEvent = ev.Name;
        IsActive = true;
        Timer = ev.Duration;
        GrassMultiplier = ev.GrassMultiplier;

        var tx = (int)(position.X / ecosystem.World.TileSize);
        var ty = (int)(position.Y / ecosystem.World.TileSize);
        var radius = ev.Radius;

        ImpactPosition = position;
        ImpactRadius = radius * ecosystem.World.TileSize;
        ImpactColor = ev.Color.ToColor();

        Enum.TryParse<BiomeType>(ev.InnerBiome, out var innerBiome);
        Enum.TryParse<BiomeType>(ev.OuterBiome, out var outerBiome);

        var r04 = radius * 0.4f;
        var r04Sq = r04 * r04;
        var r08 = radius * 0.8f;
        var r08Sq = r08 * r08;

        for (var dy = -radius; dy <= radius; dy++)
            for (var dx = -radius; dx <= radius; dx++)
            {
                int wx = tx + dx, wy = ty + dy;
                var tile = ecosystem.World.GetTile(wx, wy);
                if (tile.Biome == BiomeType.DeepOcean || tile.Biome == BiomeType.ShallowWater) continue;

                BiomeType previousBiome = tile.Biome;

                // Visible terrain changes (set Biome first - it resets grass)
                var distSq = dx * dx + dy * dy;
                if (distSq < r04Sq && ev.InnerBiome != "None")
                {
                    tile.Biome = innerBiome;
                }
                else if (distSq < r08Sq && ev.OuterBiome != "None")
                {
                    tile.Biome = outerBiome;
                }

                var biomeChanged = tile.Biome != previousBiome;
                if (biomeChanged)
                {
                    tile.OriginalBiome = previousBiome;
                    tile.CataclysmDamage = 1f;
                }

                // Then override grass/soil
                tile.GrassAmount = ev.GrassAmount;
                tile.SoilNutrients = ev.SoilNutrients;
            }
        Logger.Event("CATACLYSM", $"Player {type} at ({tx},{ty}) r={radius}");
        TryChainReaction(ecosystem, rng, type, position);
    }

    public void UpdateVolcanoes(Ecosystem ecosystem, float dt, Random rng)
    {
        for (var y = 0; y < ecosystem.World.Height; y++)
            for (var x = 0; x < ecosystem.World.Width; x++)
            {
                if (ecosystem.World.Tiles[x, y].Biome != BiomeType.Volcano) continue;
                if (rng.NextDouble() < 0.0005f * dt)
                {
                    var r = rng.Next(2, 4);
                    for (var dy = -r; dy <= r; dy++)
                        for (var dx = -r; dx <= r; dx++)
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
        var events = CataclysmConfig.Data.Chances.RandomEvents;
        if (events == null || events.Count == 0) return;
        var ev = events[rng.Next(events.Count)];

        ActiveEvent = ev.Name;
        GrassMultiplier = ev.GrassMultiplier;
        Timer = ev.BaseDuration + (float)rng.NextDouble() * ev.DurationSpread;
        IsActive = true;
        Logger.Event("CATACLYSM", $"{ActiveEvent} started at T={ecosystem.TotalTime:F1}s, duration={Timer:F1}s");
    }


    public void Draw(SpriteBatch sb, Texture2D pixel, Rectangle visibleArea)
    {
        if (!IsActive || ImpactRadius <= 0) return;

        var bounds = new Rectangle((int)(ImpactPosition.X - ImpactRadius), (int)(ImpactPosition.Y - ImpactRadius), (int)(ImpactRadius * 2), (int)(ImpactRadius * 2));
        if (!visibleArea.Intersects(bounds)) return;

        var progress = AnimTimer / AnimDuration; // 0..1
        Vector2 pos = ImpactPosition;
        var maxR = ImpactRadius * 0.8f;

        switch (ActiveEvent)
        {
            case "Asteroid":
            case "Asteroid Impact":
                DrawAsteroidEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Supervolcano":
                DrawSupervolcanoEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Firestorm":
                DrawFirestormEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "IceAge":
            case "Ice Age":
                DrawIceAgeEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Earthquake":
                DrawEarthquakeEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Drought":
                DrawDroughtEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Flood":
                DrawFloodEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
            case "Bloom":
                DrawBloomEvent(sb, pixel, pos, maxR, progress, visibleArea);
                break;
        }

        // Generic impact ring for all types
        var genR = maxR * 0.5f * (1f + progress);
        var genA = (byte)((1f - progress) * 60);
        if (genA > 0)
            DrawRing(sb, pixel, pos, genR, new Color(ImpactColor.R, ImpactColor.G, ImpactColor.B, genA), 2, visibleArea);
    }

    private void DrawAsteroidEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Falling meteor (first 0.3s)
        if (progress < 0.3f)
        {
            var t = progress / 0.3f;
            var meteorY = -100 + (pos.Y + 100) * (t * t); // accelerate down
            var mColor = new Color(255, 200, 50);
            DrawFireball(sb, pixel, new Vector2(pos.X, meteorY), 6 + (1 - t) * 12, mColor, 255, visibleArea);

            // Smoke trail
            for (var i = 1; i <= 4; i++)
            {
                var trailY = meteorY + i * 20;
                var alpha = (byte)((1f - i * 0.25f) * 180);
                var trailColor = new Color(100, 80, 40, (int)alpha);
                DrawFireball(sb, pixel, new Vector2(pos.X, trailY), 4 + i, trailColor, alpha, visibleArea);
            }
        }

        // Explosion after impact
        var explodeT = Math.Max(0, (progress - 0.3f) / 0.7f);
        var ringR = maxR * explodeT;
        var explodeColor = new Color(255, 150, 30);
        // Expanding shock rings
        for (var r = 0; r < 3; r++)
        {
            var rT = (explodeT + r * 0.3f) % 1.0f;
            var rr = maxR * rT;
            var alpha = (byte)((1f - rT) * 128);
            DrawRing(sb, pixel, pos, rr, new Color(255, 180, 40, (int)alpha), 3, visibleArea);
        }
        // Fire particles at impact
        DrawFireCluster(sb, pixel, pos, maxR * 0.6f, explodeT, progress, visibleArea);
    }

    private void DrawSupervolcanoEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Rising magma column from ground
        var intensity = Math.Min(1f, progress * 2f) * Math.Max(0f, 1f - progress * 1.5f);
        var colH = maxR * 2f * intensity;
        // Magma column (vertical stream going up)
        for (var stripe = 0; stripe < 3; stripe++)
        {
            var sx = pos.X + (stripe - 1) * maxR * 0.2f;
            var sy = pos.Y - colH * (0.5f + stripe * 0.2f);
            DrawFireball(sb, pixel, new Vector2(sx, sy), 3 + stripe * 2, new Color(255, 60, 10), (byte)(intensity * 180), visibleArea);
        }
        // Rising lava blobs
        for (var i = 0; i < 6; i++)
        {
            var angle = (float)Math.Sin(progress * 5f + i * 1.2f) * 0.5f;
            var rise = (progress + i * 0.1f) % 1f;
            var bx = pos.X + angle * maxR * 0.4f;
            var by = pos.Y - rise * colH;
            DrawFireball(sb, pixel, new Vector2(bx, by), 2 + i % 3, new Color(255, 140, 20), (byte)(intensity * 200), visibleArea);
        }
        // Lava pool at base
        DrawRing(sb, pixel, pos, maxR * progress * 0.7f, new Color(255, 40, 10, (int)(intensity * 150)), 5, visibleArea);
        DrawFireCluster(sb, pixel, pos, maxR * 0.4f * progress, progress, progress, visibleArea);
    }

    private void DrawFirestormEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        DrawFireCluster(sb, pixel, pos, maxR, progress, progress, visibleArea);
        for (var i = 0; i < 5; i++)
        {
            var angle = i * 1.256f + progress * 3f;
            var fx = pos.X + MathF.Cos(angle) * maxR * (0.5f + progress * 0.5f);
            var fy = pos.Y + MathF.Sin(angle) * maxR * (0.5f + progress * 0.5f);
            DrawFireball(sb, pixel, new Vector2(fx, fy), 3 + progress * 4, new Color(255, 160, 30), 180, visibleArea);
        }
    }

    private void DrawIceAgeEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Frost spread
        var frostR = maxR * progress;
        var frostColors = new[] { new Color(180, 220, 255, (int)60), new Color(150, 200, 240, (int)40), new Color(200, 230, 255, (int)30) };
        for (var r = 0; r < 3; r++)
        {
            var cr = frostR * (1f - r * 0.3f);
            DrawRing(sb, pixel, pos, cr, frostColors[r], (int)(2 + r * 2), visibleArea);
        }
        // Snowflakes
        for (var i = 0; i < 8; i++)
        {
            var sx = pos.X + MathF.Cos(i * 0.8f + progress * 5f) * frostR * 0.8f;
            var sy = pos.Y + MathF.Sin(i * 0.8f + progress * 2f) * frostR * 0.8f;
            DrawFireball(sb, pixel, new Vector2(sx, sy), 2, new Color(255, 255, 255, (int)150), 150, visibleArea);
        }
    }

    private void DrawEarthquakeEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Crack lines radiating from center
        var crackR = maxR * progress;
        for (var i = 0; i < 6; i++)
        {
            var angle = i * 1.047f + progress * 0.5f;
            var endX = pos.X + MathF.Cos(angle) * crackR;
            var endY = pos.Y + MathF.Sin(angle) * crackR;
            DrawLine(sb, pixel, pos, new Vector2(endX, endY), new Color(100, 80, 60, (int)120), 2, visibleArea);
        }
    }

    private void DrawDroughtEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Heat shimmer rings
        for (var r = 0; r < 4; r++)
        {
            var rr = maxR * (1f - (progress + r * 0.2f) % 1f);
            DrawRing(sb, pixel, pos, rr, new Color(255, 200, 80, (int)(progress * 80)), 2, visibleArea);
        }
    }

    private void DrawFloodEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Water waves
        for (var r = 0; r < 4; r++)
        {
            var rr = maxR * (progress + r * 0.25f) % maxR;
            DrawRing(sb, pixel, pos, rr, new Color(60, 160, 240, (int)((1f - rr / maxR) * 120)), 3, visibleArea);
        }
    }

    private void DrawBloomEvent(SpriteBatch sb, Texture2D pixel, Vector2 pos, float maxR, float progress, Rectangle visibleArea)
    {
        // Flower/grass particles
        for (var i = 0; i < 10; i++)
        {
            var angle = i * 0.628f + progress * 2f;
            var fx = pos.X + MathF.Cos(angle) * maxR * progress;
            var fy = pos.Y + MathF.Sin(angle) * maxR * progress;
            DrawFireball(sb, pixel, new Vector2(fx, fy), 3, new Color(100, 220, 80, (int)120), 120, visibleArea);
        }
    }

    private static void DrawFireball(SpriteBatch sb, Texture2D p, Vector2 pos, float r, Color c, byte alpha, Rectangle visibleArea)
    {
        var color = new Color(c.R, c.G, c.B, alpha);
        var rInt = (int)r;
        var minX = Math.Max(-rInt, visibleArea.X - (int)pos.X);
        var maxX = Math.Min(rInt, visibleArea.Right - (int)pos.X);
        var minY = Math.Max(-rInt, visibleArea.Y - (int)pos.Y);
        var maxY = Math.Min(rInt, visibleArea.Bottom - (int)pos.Y);

        for (var dy = minY; dy <= maxY; dy++)
            for (var dx = minX; dx <= maxX; dx++)
            {
                if (dx * dx + dy * dy <= r * r)
                    sb.Draw(p, new Rectangle((int)pos.X + dx, (int)pos.Y + dy, 1, 1), color);
            }
    }

    private static void DrawRing(SpriteBatch sb, Texture2D p, Vector2 pos, float r, Color c, int thickness, Rectangle visibleArea)
    {
        var rInt = (int)r + thickness;
        var minX = Math.Max(-rInt, visibleArea.X - (int)pos.X);
        var maxX = Math.Min(rInt, visibleArea.Right - (int)pos.X);
        var minY = Math.Max(-rInt, visibleArea.Y - (int)pos.Y);
        var maxY = Math.Min(rInt, visibleArea.Bottom - (int)pos.Y);

        var rSq = r * r;
        var rInner = r - thickness;
        var rInnerSq = rInner * rInner;

        for (var dy = minY; dy <= maxY; dy++)
            for (var dx = minX; dx <= maxX; dx++)
            {
                var distSq = dx * dx + dy * dy;
                var withinInner = rInner < 0 || distSq >= rInnerSq;
                if (withinInner && distSq <= rSq)
                    sb.Draw(p, new Rectangle((int)pos.X + dx, (int)pos.Y + dy, 1, 1), c);
            }
    }

    private void DrawFireCluster(SpriteBatch sb, Texture2D p, Vector2 pos, float radius, float t, float progress, Rectangle visibleArea)
    {
        var seed = (int)(t * 100);
        var rng = new Random(seed);
        for (var i = 0; i < 20; i++)
        {
            var angle = (float)rng.NextDouble() * 6.28f;
            var dist = (float)rng.NextDouble() * radius * (0.7f + 0.3f * MathF.Sin(progress * 3f + i));
            var fx = pos.X + MathF.Cos(angle) * dist;
            var fy = pos.Y + MathF.Sin(angle) * dist;
            var size = 1 + rng.Next(3);
            var alpha = (byte)(160 + rng.Next(95));
            var fc = new Color(255, 100 + rng.Next(155), 10 + rng.Next(30));
            DrawFireball(sb, p, new Vector2(fx, fy), size, fc, alpha, visibleArea);
        }
    }

    private static void DrawLine(SpriteBatch sb, Texture2D p, Vector2 from, Vector2 to, Color c, int thickness, Rectangle visibleArea)
    {
        var b = new Rectangle(
            (int)Math.Min(from.X, to.X) - thickness,
            (int)Math.Min(from.Y, to.Y) - thickness,
            (int)Math.Abs(from.X - to.X) + thickness * 2,
            (int)Math.Abs(from.Y - to.Y) + thickness * 2);
        if (!visibleArea.Intersects(b)) return;

        var dx = to.X - from.X;
        var dy = to.Y - from.Y;
        var len = MathF.Sqrt(dx * dx + dy * dy);
        if (len < 1) return;
        float nx = dx / len, ny = dy / len;
        for (var i = 0; i < (int)len; i++)
        {
            var px = (int)(from.X + nx * i);
            var py = (int)(from.Y + ny * i);
            if (visibleArea.Contains(px, py))
                sb.Draw(p, new Rectangle(px, py, 1, thickness), c);
        }
    }
}
