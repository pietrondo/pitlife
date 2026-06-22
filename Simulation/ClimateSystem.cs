using System;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class ClimateSystem
{
    public const float YearLength = 480f;
    public const float SeasonLength = YearLength / 4f;
    public Season CurrentSeason { get; private set; } = Season.Spring;
    public float SeasonProgress { get; private set; }
    public float TemperatureModifier { get; private set; }
    public float GrassRegenModifier { get; private set; } = 1f;
    public float EnergyModifier { get; private set; } = 1f;
    public bool IsExtremeEvent { get; private set; }
    public string ExtremeEventName { get; private set; } = "";

    private float _extremeEventTimer;
    private float _extremeEventDuration;

    public void Update(float totalTime, Random rng)
    {
        float yearTime = totalTime % YearLength;
        SeasonProgress = (yearTime % SeasonLength) / SeasonLength;

        Season newSeason = yearTime switch
        {
            < SeasonLength => Season.Spring,
            < SeasonLength * 2 => Season.Summer,
            < SeasonLength * 3 => Season.Autumn,
            _ => Season.Winter
        };

        if (newSeason != CurrentSeason)
        {
            CurrentSeason = newSeason;
            Logger.Event("SEASON", $"Season changed to {CurrentSeason} at T={totalTime:F1}s");
        }

        (TemperatureModifier, GrassRegenModifier, EnergyModifier) = CurrentSeason switch
        {
            Season.Spring => (0.0f, 1.3f, 1.0f),
            Season.Summer => (0.15f, 0.9f, 1.0f),
            Season.Autumn => (0.0f, 0.7f, 1.0f),
            Season.Winter => (-0.15f, 0.3f, 1.2f),
            _ => (0f, 1f, 1f)
        };

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
}

public enum Season
{
    Spring,
    Summer,
    Autumn,
    Winter
}
