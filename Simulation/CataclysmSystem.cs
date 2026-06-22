using System;
using Microsoft.Xna.Framework;
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

    private float _cooldownTimer;

    public void Update(Ecosystem ecosystem, float dt, Random rng)
    {
        if (IsActive)
        {
            Timer -= dt;
            if (Timer <= 0)
            {
                IsActive = false;
                GrassMultiplier = 1f;
                ActiveEvent = "";
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
        ApplyTerrainChange(ecosystem);
        Logger.Event("CATACLYSM", $"MASS EXTINCTION: {ActiveEvent} at T={ecosystem.TotalTime:F1}s, duration={Timer:F1}s");
    }

    private void ApplyTerrainChange(Ecosystem ecosystem)
    {
        int radius = ActiveEvent switch
        {
            "Asteroid Impact" => 8,
            "Supervolcano" => 5,
            _ => 0
        };
        if (radius <= 0) return;
        int w = ecosystem.World.Width, h = ecosystem.World.Height;
        int cx = ecosystem.Random.Next(Math.Min(radius, w - radius), Math.Max(radius + 1, w - radius));
        int cy = ecosystem.Random.Next(Math.Min(radius, h - radius), Math.Max(radius + 1, h - radius));
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var tile = ecosystem.World.GetTile(cx + dx, cy + dy);
                tile.GrassAmount = 0f;
                tile.SoilNutrients = 0.1f;
            }
        Logger.Event("TERRAIN", $"{ActiveEvent} crater at ({cx},{cy}) radius={radius}");
    }

    public void TriggerManual(Ecosystem ecosystem, Random rng)
    {
        TriggerMassExtinction(ecosystem, rng);
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
            "Asteroid" => new Color(255, 100, 30, 200),
            "Supervolcano" => new Color(255, 50, 10, 200),
            "Earthquake" => new Color(180, 140, 100, 150),
            "IceAge" => new Color(100, 200, 255, 150),
            "Drought" => new Color(255, 180, 40, 150),
            "Flood" => new Color(40, 140, 255, 150),
            _ => Color.Transparent
        };
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var tile = ecosystem.World.GetTile(tx + dx, ty + dy);
                if (tile.Biome != BiomeType.DeepOcean && tile.Biome != BiomeType.ShallowWater)
                { tile.GrassAmount = type == "Flood" ? tile.MaxGrass : 0f; tile.SoilNutrients = type == "Flood" ? 2f : 0.1f; }
            }
        Logger.Event("CATACLYSM", $"Player {type} at ({tx},{ty}) r={radius}");
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
}
