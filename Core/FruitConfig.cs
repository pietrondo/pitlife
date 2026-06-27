using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class FruitConfig
{
    public static FruitConfigData Data { get; private set; } = new();

    static FruitConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "fruit.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<FruitConfigData>(json, new JsonSerializerOptions
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
            Logger.Error($"Failed to load fruit.json: {ex.Message}");
        }
    }
}

public class FruitConfigData
{
    public int Version { get; set; } = 1;
    public int MaxFruits { get; set; } = 500;
    public float SpawnTimerBase { get; set; } = 3f;
    public float SpawnTimerVariance { get; set; } = 5f;
    public int SpawnAttempts { get; set; } = 10;
    public int FindPlantMaxTries { get; set; } = 50;
    public float SpawnOffsetMax { get; set; } = 40f;
    public float EnergyValueBase { get; set; } = 5f;
    public float EnergyValueVariance { get; set; } = 10f;
    public float LifetimeBase { get; set; } = 20f;
    public float LifetimeVariance { get; set; } = 30f;
    public float PoisonousToxicityBase { get; set; } = 0.6f;
    public float PoisonousToxicityVariance { get; set; } = 0.3f;
}
