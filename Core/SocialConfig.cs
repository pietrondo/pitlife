using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class SocialConfig
{
    public static SocialConfigData Data { get; private set; } = new();

    static SocialConfig()
    {
        try
        {
            string path = Path.Combine("Content", "config", "social.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<SocialConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch { /* use defaults */ }
    }
}

public class SocialConfigData
{
    public int Version { get; set; } = 1;
    public SocialFlockingConfig Flocking { get; set; } = new();
    public CombatConfig Combat { get; set; } = new();
}

public class SocialFlockingConfig
{
    public float SeparationWeight { get; set; } = 1.5f;
    public float AlignmentWeight { get; set; } = 2.0f;
    public float CohesionWeight { get; set; } = 0.5f;
    public float SeparationDistance { get; set; } = 30f;
    public float HerdRadius { get; set; } = 0.2f;
}

public class CombatConfig
{
    public float CombatDamage { get; set; } = 10f;
    public float DefenseDivisor { get; set; } = 20f;
    public float AggressionFactor { get; set; } = 25f;
}
