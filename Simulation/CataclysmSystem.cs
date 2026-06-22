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
