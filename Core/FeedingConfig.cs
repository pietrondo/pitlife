using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class FeedingConfig
{
    public static FeedingConfigData Instance { get; private set; } = new();

    public static void Load()
    {
        try
        {
            string path = Path.Combine("Content", "config", "feeding.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<FeedingConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Instance = parsed;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load feeding.json: {ex.Message}");
        }
    }
}

public class FeedingConfigData
{
    public int Version { get; set; } = 1;
    public float HungerThresholdHerbivore { get; set; } = 0.6f;
    public float HungerThresholdCarnivore { get; set; } = 0.8f;
    public float AttackEnergyGain { get; set; } = 1.5f;
    public float ToxicityReduction { get; set; } = 0.5f;
    public float PreyEscapeThreshold { get; set; } = 0.3f;
    public float ScavengeRange { get; set; } = 10f; // Fix tests
    public float HerbivorePlantEnergy { get; set; } = 8f;
    public float CarnivoreAttackCost { get; set; } = 3f;
    public float OmnivoreAttackCost { get; set; } = 4f;
    public float PlantDigestionRate { get; set; } = 2f; // Fix tests
    public float MaxFruitEatRange { get; set; } = 12f; // Fix tests
}