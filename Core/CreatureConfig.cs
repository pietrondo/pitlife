using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class CreatureConfig
{
    public static CreatureConfigData Data { get; private set; } = new();

    static CreatureConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "creatures.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<CreatureConfigData>(json, new JsonSerializerOptions
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
            Logger.Error($"Failed to load creatures.json: {ex.Message}");
        }
    }
}

public class CreatureConfigData
{
    public int Version { get; set; } = 1;
    public float ReproduceEnergyCostRatio { get; set; } = 0.3f;
    public float ChildOffsetRadius { get; set; } = 30f;
    public float ChildInitialEnergyRatio { get; set; } = 0.5f;
    public float GeneticDriftChance { get; set; } = 0.0001f;
    public float ScarcityGrassThreshold { get; set; } = 0.2f;
    public float ScarcityPenaltyFactor { get; set; } = 5f;
    public float ColdPrefTemp { get; set; } = 10f;
    public float HotPrefTemp { get; set; } = 35f;
    public float NeutralPrefTemp { get; set; } = 22f;
    public float InitialReproductionOffset { get; set; } = -60f;

    // Plant specific
    public float ChildEnergyRatio { get; set; } = 0.5f;
    public float ReproductionEnergyCostRatio { get; set; } = 0.2f;
}
