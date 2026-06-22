using System;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class CataclysmSystem
{
    public bool IsActive { get; private set; }
    public string ActiveEvent { get; private set; } = "";
    public float Timer { get; private set; }
    public float GrassMultiplier { get; private set; } = 1f;

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
        int cx = ecosystem.Random.Next(radius, ecosystem.World.Width - radius);
        int cy = ecosystem.Random.Next(radius, ecosystem.World.Height - radius);
        for (int dy = -radius; dy <= radius; dy++)
            for (int dx = -radius; dx <= radius; dx++)
            {
                var tile = ecosystem.World.GetTile(cx + dx, cy + dy);
                tile.GrassAmount = 0f;
                tile.SoilNutrients = 0.1f;
            }
        Logger.Event("TERRAIN", $"{ActiveEvent} crater at ({cx},{cy}) radius={radius}");
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
