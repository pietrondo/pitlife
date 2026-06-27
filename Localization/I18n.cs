using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using PitLife.Simulation;

namespace PitLife.Localization;

public static class I18n
{
    private static readonly Dictionary<string, string> English;
    private static readonly Dictionary<string, string> Italian;
    private static IReadOnlyDictionary<string, string> _current;

    public static string CurrentLanguage { get; private set; } = "it";
    public static IReadOnlyCollection<string> SupportedLanguages { get; } = ["it", "en"];

    static I18n()
    {
        Dictionary<string, string> en = new(StringComparer.Ordinal);
        Dictionary<string, string> itOverrides = new(StringComparer.Ordinal);
        var loaded = false;

        try
        {
            var path = Path.Combine("Content", "config", "i18n.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("en", out JsonElement enEl))
                {
                    foreach (JsonProperty prop in enEl.EnumerateObject())
                        en[prop.Name] = prop.Value.GetString() ?? prop.Name;
                }
                if (doc.RootElement.TryGetProperty("it", out JsonElement itEl))
                {
                    foreach (JsonProperty prop in itEl.EnumerateObject())
                        itOverrides[prop.Name] = prop.Value.GetString() ?? prop.Name;
                }
                loaded = true;
            }
        }
        catch { }

        if (!loaded)
        {
            en["error"] = "i18n.json not found";
            itOverrides["error"] = "i18n.json non trovato";
        }

        English = en;
        Italian = new Dictionary<string, string>(English, StringComparer.Ordinal);
        foreach (var kv in itOverrides)
            Italian[kv.Key] = kv.Value;

        _current = Italian;
    }

    public static void SetLanguage(string language)
    {
        var normalized = language.Trim().ToLowerInvariant();
        if (normalized.StartsWith("it"))
        {
            CurrentLanguage = "it";
            _current = Italian;
            return;
        }

        if (normalized.StartsWith("en"))
        {
            CurrentLanguage = "en";
            _current = English;
            return;
        }

        throw new ArgumentException($"Unsupported language: {language}", nameof(language));
    }

    public static string T(string key) =>
        _current.TryGetValue(key, out var value) ? value :
        Italian.TryGetValue(key, out value) ? value : key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.GetCultureInfo(CurrentLanguage), T(key), args);

    public static string Species(string species)
    {
        var key = $"species.{species}";
        var value = T(key);
        return value == key ? species : value;
    }

    public static string CreatureTypeName(CreatureType type) => T($"creatureType.{type}");

    public static IReadOnlyCollection<string> Keys(string language) =>
        language.StartsWith("it", StringComparison.OrdinalIgnoreCase) ? Italian.Keys : English.Keys;

    public static void RegisterCustomSpecies(string species, string englishName, string italianName)
    {
        var key = $"species.{species}";
        if (English.ContainsKey(key) || Italian.ContainsKey(key))
            throw new InvalidOperationException($"Localization already registered for species '{species}'.");
        English[key] = englishName;
        Italian[key] = italianName;
    }

    internal static void UnregisterCustomSpecies(string species)
    {
        var key = $"species.{species}";
        English.Remove(key);
        Italian.Remove(key);
    }
}
