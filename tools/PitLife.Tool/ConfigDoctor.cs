using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PitLife.Tool;

internal static class ConfigDoctor
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    // (tipo nel namespace PitLife.Core, file JSON)
    private static readonly (string Type, string Json)[] Configs =
    [
        ("AtmosphereConfig", "atmosphere.json"),
        ("BalanceConfig", "balance.json"),
        ("CataclysmConfig", "cataclysms.json"),
        ("ClimateConfig", "climate.json"),
        ("CreatureConfig", "creatures.json"),
        ("DiseaseConfig", "diseases.json"),
        ("EnvironmentConfig", "environment.json"),
        ("EvolutionConfig", "evolution.json"),
        ("FeedingConfig", "feeding.json"),
        ("FlowConfig", "flow.json"),
        ("FruitConfig", "fruit.json"),
        ("GeneticsConfig", "genetics.json"),
        ("GenomeConfig", "genome.json"),
        ("MaturationConfig", "maturation.json"),
        ("SimulationConfig", "simulation.json"),
        ("SocialConfig", "social.json"),
    ];

    public static int Run()
    {
        Console.WriteLine("== PitLife config doctor ==\n");
        var errors = ValidateJsonFiles();
        var drift = CheckDefaultDrift();
        var dup = CheckSpeciesDuplication();

        Console.WriteLine($"\nJSON non validi: {errors}");
        Console.WriteLine($"Campi con drift default-vs-JSON: {drift}");
        Console.WriteLine($"Specie duplicate C#/JSON: {dup}");
        return errors > 0 ? 1 : 0;
    }

    private static int ValidateJsonFiles()
    {
        Console.WriteLine("--- Validità JSON ---");
        int errors = 0;
        foreach (var file in Directory.EnumerateFiles(ToolRoot.ConfigDir, "*.json").OrderBy(f => f))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                Console.WriteLine($"  OK   {Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                errors++;
                Console.WriteLine($"  FAIL {Path.GetFileName(file)}: {ex.Message}");
            }
        }
        return errors;
    }

    private static int CheckDefaultDrift()
    {
        Console.WriteLine("\n--- Drift default C# vs JSON ---");
        int drift = 0;
        int inspected = 0;
        var assembly = typeof(PitLife.Core.BalanceConfig).Assembly;

        foreach (var (typeName, json) in Configs)
        {
            try
            {
                var type = assembly.GetType($"PitLife.Core.{typeName}");
                if (type is null) { Console.WriteLine($"  SKIP {typeName} (tipo non trovato)"); continue; }

                var dataProp = type.GetProperty("Data", BindingFlags.Public | BindingFlags.Static);
                if (dataProp is null) continue;

                var dataType = dataProp.PropertyType;
                // I record posizionali (es. CataclysmConfigDoc) non hanno ctor parameterless:
                // il loro default è nel campo Fallback, non ispezionabile in modo generico.
                if (dataType.GetConstructor(Type.EmptyTypes) is null)
                    continue;

                var defaults = Activator.CreateInstance(dataType);
                var path = Path.Combine(ToolRoot.ConfigDir, json);
                if (!File.Exists(path)) { Console.WriteLine($"  MISSING {json}"); continue; }

                var jsonInstance = JsonSerializer.Deserialize(File.ReadAllText(path), dataType, JsonOptions);

                inspected++;
                foreach (var prop in dataType.GetProperties().Where(IsPrimitive))
                {
                    var a = prop.GetValue(defaults);
                    var b = prop.GetValue(jsonInstance);
                    if (!Equals(a, b))
                    {
                        drift++;
                        Console.WriteLine($"  DRIFT {typeName}.{prop.Name}: default={Format(a)} json={Format(b)}");
                    }
                }
            }
            catch (Exception ex)
            {
                drift++;
                Console.WriteLine($"  ERROR {typeName}: {ex.Message}");
            }
        }
        Console.WriteLine($"  config ispezionati: {inspected}/{Configs.Length}, drift rilevati: {drift}");
        return drift;
    }

    private static int CheckSpeciesDuplication()
    {
        Console.WriteLine("\n--- Duplicazione specie (C# vs JSON) ---");
        var jsonSpecies = new HashSet<string>(SpeciesFromJson(), StringComparer.OrdinalIgnoreCase);
        var builtin = new HashSet<string>(SpeciesFromBuiltinSource(), StringComparer.OrdinalIgnoreCase);

        var overlap = jsonSpecies.Intersect(builtin).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();

        Console.WriteLine($"  specie in species.json: {jsonSpecies.Count}");
        Console.WriteLine($"  specie hardcoded in BuiltinSpecies.cs: {builtin.Count}");
        Console.WriteLine($"  DUPLICATE (in entrambi): {overlap.Count}");
        if (overlap.Count > 0)
            Console.WriteLine("    " + string.Join(", ", overlap));
        return overlap.Count;
    }

    private static IEnumerable<string> SpeciesFromJson()
    {
        var path = Path.Combine(ToolRoot.ConfigDir, "species.json");
        if (!File.Exists(path)) return [];
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        if (!doc.RootElement.TryGetProperty("species", out var arr)) return [];
        return arr.EnumerateArray()
            .Where(e => e.TryGetProperty("name", out _))
            .Select(e => e.GetProperty("name").GetString()!)
            .ToList();
    }

    private static IEnumerable<string> SpeciesFromBuiltinSource()
    {
        var path = Path.Combine(ToolRoot.RepoRoot(), "Simulation", "Entities", "BuiltinSpecies.cs");
        if (!File.Exists(path)) return [];
        var src = File.ReadAllText(path);
        return Regex.Matches(src, @"Register(?:Plant|Animal)\(""([^""]+)""")
            .Select(m => m.Groups[1].Value);
    }

    private static bool IsPrimitive(PropertyInfo p) =>
        p.PropertyType.IsPrimitive || p.PropertyType.IsEnum || p.PropertyType == typeof(string);

    private static string Format(object? v) => v is null ? "null" : v.ToString() ?? "null";
}
