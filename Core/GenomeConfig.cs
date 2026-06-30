using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class GenomeConfig
{
    public static GenomeConfigData Data { get; private set; } = new();

    static GenomeConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "genome.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<GenomeConfigData>(json, new JsonSerializerOptions
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
            Logger.Error($"Failed to load genome.json: {ex.Message}");
        }
    }
}

public class GenomeConfigData
{
    public int Version { get; set; } = 1;
    public float TraitDriftAmplitude { get; set; } = 0.02f;
    public float TraitMin { get; set; } = 0.5f;
    public float TraitMax { get; set; } = 2f;
    public float AdaptationDriftAmplitude { get; set; } = 0.01f;
    public float AdaptationMin { get; set; } = 0f;
    public float AdaptationMax { get; set; } = 1f;
}
