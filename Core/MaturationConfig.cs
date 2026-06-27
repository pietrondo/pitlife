using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class MaturationConfig
{
    public static MaturationConfigData Data { get; private set; } = new();

    static MaturationConfig()
    {
        try
        {
            string path = Path.Combine("Content", "config", "maturation.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<MaturationConfigData>(json, new JsonSerializerOptions
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
            Logger.Error($"Failed to load maturation.json: {ex.Message}");
        }
    }
}

public class MaturationConfigData
{
    public int Version { get; set; } = 1;
    public float DefaultAge { get; set; } = 30f;
    public Dictionary<string, float> Ages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
