using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class DiseaseConfig
{
    public static IReadOnlyList<DiseaseDefEntry> Diseases { get; } = LoadDiseases();
    public static OutbreakDefaults Outbreak { get; } = LoadOutbreak();

    private static IReadOnlyList<DiseaseDefEntry> LoadDiseases()
    {
        try
        {
            string path = Path.Combine("Content", "config", "diseases.json");
            if (!File.Exists(path)) return FallbackDiseases;

            string json = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<DiseaseConfigDoc>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            return doc?.Diseases ?? FallbackDiseases;
        }
        catch { return FallbackDiseases; }
    }

    private static OutbreakDefaults LoadOutbreak()
    {
        try
        {
            string path = Path.Combine("Content", "config", "diseases.json");
            if (!File.Exists(path)) return new OutbreakDefaults();

            string json = File.ReadAllText(path);
            var doc = JsonSerializer.Deserialize<DiseaseConfigDoc>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                AllowTrailingCommas = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });
            return doc?.Outbreak ?? new OutbreakDefaults();
        }
        catch { return new OutbreakDefaults(); }
    }

    private static readonly IReadOnlyList<DiseaseDefEntry> FallbackDiseases = new[]
    {
        new DiseaseDefEntry { Name = "Fever", TransmissionRate = 0.15f, Lethality = 0.1f, RecoveryTime = 30f, EnergyDrain = 2f },
        new DiseaseDefEntry { Name = "Plague", TransmissionRate = 0.3f, Lethality = 0.3f, RecoveryTime = 45f, EnergyDrain = 4f },
        new DiseaseDefEntry { Name = "Parasite", TransmissionRate = 0.1f, Lethality = 0.05f, RecoveryTime = 60f, EnergyDrain = 1f }
    };

    private sealed record DiseaseConfigDoc(int Version, List<DiseaseDefEntry> Diseases, OutbreakDefaults? Outbreak);

    public sealed record DiseaseDefEntry
    {
        public string Name { get; init; } = "";
        public float TransmissionRate { get; init; }
        public float Lethality { get; init; }
        public float RecoveryTime { get; init; }
        public float EnergyDrain { get; init; }
    }

    public sealed record OutbreakDefaults
    {
        public int MinCreatures { get; init; } = 10;
        public float InitialTimerSeconds { get; init; } = 60f;
    }
}
