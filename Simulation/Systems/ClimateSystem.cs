using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class ClimateSystem : ISimulationSystem
{
    public SimulationPhase Phase => SimulationPhase.EarlyUpdate;

    public const float PlanetRadiusKm = 6371f;
    public const float DefaultOrbitalAU = 1f;
    public const float DefaultEccentricity = 0.12f;
    public const float OrbitalPeriod = 480f;
    public const float YearLength = OrbitalPeriod;
    public const float SeasonLength = OrbitalPeriod / 4f;
    public const float BaseSurfaceTempK = 288f;

    public Season CurrentSeason { get; private set; } = Season.Summer;
    public float SeasonProgress { get; private set; }
    public float TemperatureModifier { get; private set; }
    public float GrassRegenModifier { get; private set; } = 1f;
    public float EnergyModifier { get; private set; } = 1f;
    public bool IsExtremeEvent { get; private set; }
    public string ExtremeEventName { get; private set; } = "";
    public float WindSpeed { get; private set; } = 0.5f;
    public float WindDirection { get; private set; }

    public float OrbitalAngle { get; private set; }
    public float SunDistanceAU { get; private set; } = 1f;
    public float OrbitalSpeedKmS { get; private set; } = 29.8f;
    public float OrbitalAU { get; private set; } = DefaultOrbitalAU;
    public float Eccentricity { get; private set; } = DefaultEccentricity;

    private float _extremeEventTimer;
    private float _extremeEventDuration;

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco.TotalTime, eco.Random);

    public void Initialize(World world) { }
    public void Reset()
    {
        _extremeEventTimer = 0;
        IsExtremeEvent = false;
        ExtremeEventName = "";
        OrbitalAngle = 0;
        SunDistanceAU = 1f;
        OrbitalSpeedKmS = 29.8f;
        TemperatureModifier = 0;
        GrassRegenModifier = 1f;
        EnergyModifier = 1f;
    }

    public void Configure(float planetRadiusKm, float orbitalAU, float eccentricity)
    {
        OrbitalAU = orbitalAU;
        Eccentricity = eccentricity;
        SunDistanceAU = orbitalAU;
        OrbitalSpeedKmS = 29.8f * MathF.Sqrt(1f / orbitalAU);
    }

    public void Update(float totalTime, Random rng)
    {
        OrbitalAngle = totalTime / OrbitalPeriod * MathF.PI * 2 % (MathF.PI * 2);
        float cosTheta = MathF.Cos(OrbitalAngle);
        float semiLatus = OrbitalAU * (1f - Eccentricity * Eccentricity);
        SunDistanceAU = semiLatus / (1f + Eccentricity * cosTheta);

        float orbitalCircumference = 2f * MathF.PI * OrbitalAU * 149.6e6f;
        OrbitalSpeedKmS = orbitalCircumference / (OrbitalPeriod * 149.6e6f) * OrbitalAU;

        TemperatureModifier = cosTheta * 0.15f;

        SeasonProgress = (OrbitalAngle % (MathF.PI / 2f)) / (MathF.PI / 2f);

        Season newSeason = OrbitalAngle switch
        {
            < MathF.PI / 2f => Season.Summer,
            < MathF.PI => Season.Autumn,
            < MathF.PI * 3f / 2f => Season.Winter,
            _ => Season.Spring
        };

        if (newSeason != CurrentSeason)
        {
            CurrentSeason = newSeason;
            Logger.Event("SEASON", $"Season changed to {CurrentSeason} at T={totalTime:F1}s (dist={SunDistanceAU:F3} AU)");
        }

        (GrassRegenModifier, EnergyModifier) = CurrentSeason switch
        {
            Season.Spring => (1.3f, 1.0f),
            Season.Summer => (0.9f, 1.0f),
            Season.Autumn => (0.7f, 1.0f),
            Season.Winter => (0.3f, 1.2f),
            _ => (1f, 1f)
        };

        float orbitalBoost = TemperatureModifier * 0.3f;
        GrassRegenModifier += orbitalBoost;
        EnergyModifier -= orbitalBoost * 0.5f;

        WindDirection = (WindDirection + (float)rng.NextDouble() * 0.1f) % (MathF.PI * 2);
        WindSpeed = 0.3f + (float)rng.NextDouble() * 0.6f + Math.Abs(TemperatureModifier) * 2f;

        if (_extremeEventTimer > 0)
        {
            _extremeEventTimer -= 1f / 60f;
            if (_extremeEventTimer <= 0)
            {
                IsExtremeEvent = false;
                ExtremeEventName = "";
                Logger.Event("CLIMATE", $"Extreme event ended at T={totalTime:F1}s");
            }
        }
        else if (rng.NextDouble() < 0.0002f)
        {
            IsExtremeEvent = true;
            _extremeEventDuration = 10f + (float)rng.NextDouble() * 20f;
            _extremeEventTimer = _extremeEventDuration;

            if (CurrentSeason == Season.Summer && rng.NextDouble() < 0.5f)
            {
                ExtremeEventName = "Heatwave";
                TemperatureModifier += 0.2f;
                GrassRegenModifier *= 0.3f;
            }
            else if (CurrentSeason == Season.Winter && rng.NextDouble() < 0.5f)
            {
                ExtremeEventName = "ColdSnap";
                TemperatureModifier -= 0.15f;
                EnergyModifier += 0.2f;
            }
            else
            {
                ExtremeEventName = "Storm";
                GrassRegenModifier *= 0.5f;
            }

            Logger.Event("CLIMATE", $"Extreme event '{ExtremeEventName}' started at T={totalTime:F1}s, duration={_extremeEventDuration:F1}s");
        }
    }

    public float GetLatitudeModifier(float tileY, int worldHeight)
    {
        float normalizedY = tileY / Math.Max(1, worldHeight - 1);
        float latitude = (normalizedY - 0.5f) * MathF.PI;
        return MathF.Cos(latitude);
    }

    public Season GetLocalSeason(float tileY, int worldHeight)
    {
        float normalizedY = tileY / Math.Max(1, worldHeight - 1);
        bool isSouth = normalizedY > 0.5f;
        if (!isSouth) return CurrentSeason;
        return CurrentSeason switch
        {
            Season.Summer => Season.Winter,
            Season.Winter => Season.Summer,
            Season.Spring => Season.Autumn,
            Season.Autumn => Season.Spring,
            _ => CurrentSeason
        };
    }

    public float GetTileTemperature(Tile tile, float tileY, int worldHeight)
    {
        float latMod = GetLatitudeModifier(tileY, worldHeight);
        float orbitalEffect = TemperatureModifier * 20f;
        float latEffect = (latMod - 0.6f) * 25f;
        return tile.Temperature + orbitalEffect + latEffect;
    }
}

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}
