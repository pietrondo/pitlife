using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class EnvironmentConfig
{
    public static EnvironmentConfigData Data { get; private set; } = new();

    static EnvironmentConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "environment.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<EnvironmentConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load environment.json: {ex.Message}");
        }
    }
}

public class EnvironmentConfigData
{
    public int Version { get; set; } = 1;
    public float AltitudeThreshold { get; set; } = 0.6f;
    public float AltitudeFactor { get; set; } = 3f;
    public float LotkaPressureWeight { get; set; } = 0.5f;
    public float O2FactorWeight { get; set; } = 0.3f;
    public float TickDivisor { get; set; } = 60f;
    public float TemperaturePenaltyThreshold { get; set; } = 15f;
    public float TemperaturePenaltyFactor { get; set; } = 0.02f;
    public float InvalidBiomePenalty { get; set; } = 4f;
    public float InvalidTemperaturePenalty { get; set; } = 2f;
    public float AquaticSpeedMultiplier { get; set; } = 1f;
    public float AquaticEnergyMultiplier { get; set; } = 1f;
    public float StrandedSpeedMultiplier { get; set; } = 0.3f;
    public float StrandedEnergyMultiplier { get; set; } = 2.5f;
}
