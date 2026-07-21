using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class ConfigLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static T Load<T>(string configPath) where T : new()
    {
        try
        {
            var path = Path.Combine("Content", "config", configPath);
            if (!File.Exists(path)) return new T();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options) ?? new T();
        }
        catch (Exception ex)
        {
            Logger.Warn($"Failed to load config '{configPath}': {ex.Message}. Using defaults.");
            return new T();
        }
    }

    public static T Load<T>(string configPath, T fallback)
    {
        try
        {
            var path = Path.Combine("Content", "config", configPath);
            if (!File.Exists(path)) return fallback;
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json, Options) ?? fallback;
        }
        catch { return fallback; }
    }
}