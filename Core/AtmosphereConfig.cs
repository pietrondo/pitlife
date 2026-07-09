using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class AtmosphereConfig
{
    public static AtmosphereConfigData Data { get; private set; } = new();

    static AtmosphereConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "atmosphere.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<AtmosphereConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load atmosphere.json: {ex.Message}");
        }
    }
}

public class AtmosphereConfigData
{
    public int Version { get; set; } = 1;
    public float InitialOxygen { get; set; } = 50f;
    public float InitialCO2 { get; set; } = 30f;
    public float MaxLevel { get; set; } = 100f;
    public float O2PerPlant { get; set; } = 0.003f;
    public float O2PerAnimal { get; set; } = 0.002f;
    public float Co2PerAnimal { get; set; } = 0.001f;
    public float Co2DecayRate { get; set; } = 0.01f;
    public float OxygenModifierBase { get; set; } = 50f;
    public float Co2ModifierBase { get; set; } = 30f;
    public float OxygenModifierMin { get; set; } = 0.3f;
    public float OxygenModifierMax { get; set; } = 2.0f;
    public float Co2ModifierMin { get; set; } = 0.5f;
    public float Co2ModifierMax { get; set; } = 2.0f;
}
