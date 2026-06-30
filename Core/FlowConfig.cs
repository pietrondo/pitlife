using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PitLife.Core;

public static class FlowConfig
{
    public static FlowConfigData Data { get; private set; } = new();

    static FlowConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "flow.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<FlowConfigData>(json, new JsonSerializerOptions
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
            Logger.Error($"Failed to load flow.json: {ex.Message}");
        }
    }
}

public class FlowConfigData
{
    public int Version { get; set; } = 1;
    public InitialLevelsConfig InitialLevels { get; set; } = new();
    public FlowRatesConfig FlowRates { get; set; } = new();
    public FlowTemperatureConfig Temperature { get; set; } = new();
    public VisualsConfig Visuals { get; set; } = new();
}

public class InitialLevelsConfig
{
    public float DeepOceanWater { get; set; } = 1f;
    public float ShallowWaterWater { get; set; } = 0.7f;
    public float RiverWater { get; set; } = 0.5f;
    public float VolcanoLava { get; set; } = 0.4f;
}

public class FlowRatesConfig
{
    public float TickAccumulatorThreshold { get; set; } = 1f;
    public float WaterFlowRateBase { get; set; } = 0.1f;
    public float WaterFlowRateMax { get; set; } = 0.25f;
    public float LavaFlowRateMultiplier { get; set; } = 0.3f;
    public float LavaFlowRateMax { get; set; } = 0.1f;
    public float EvaporationBaseRate { get; set; } = 0.01f;
    public float EvaporationTempDivisor { get; set; } = 40f;
    public float VolcanoLavaRegen { get; set; } = 0.1f;
    public float RiverWaterRegen { get; set; } = 0.05f;
}

public class FlowTemperatureConfig
{
    public float DesertTemp { get; set; } = 38f;
    public float DesertLatMod { get; set; } = 15f;
    public float SavannaTemp { get; set; } = 32f;
    public float VolcanoTemp { get; set; } = 45f;
    public float SnowTemp { get; set; } = -10f;
    public float SnowLatMod { get; set; } = 10f;
    public float TundraTemp { get; set; } = 5f;
    public float DeepOceanTemp { get; set; } = 15f;
    public float DefaultTemp { get; set; } = 20f;
    public float DefaultLatMod { get; set; } = 5f;
}

public class VisualsConfig
{
    public int OverlayScaleNumerator { get; set; } = 16;
    public int OverlayScaleDenominator { get; set; } = 4;
    public byte LavaRedMultiplier { get; set; } = 200;
    public byte WaterBlueMultiplier { get; set; } = 160;
    public byte WaterAlphaMultiplier { get; set; } = 100;
    public byte LavaAlphaMultiplier { get; set; } = 180;
    public float RiverAnimPhaseMultiplier { get; set; } = 40f;
    public float RiverAnimScaleMultiplier { get; set; } = 2f;
    public byte RiverAlphaBoost { get; set; } = 70;
    public byte RiverBlueBoost { get; set; } = 40;
}
