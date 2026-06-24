using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace PitLife.Simulation;

internal static class SpeciesJsonLoader
{
    private sealed record JsonSpeciesEntry(
        string Name,
        string Kind,
        bool IsAquatic = false,
        string? Reproduction = null,
        string? Pollination = null,
        string? SocialBehavior = null,
        List<string>? Biomes = null,
        float Size = 1.0f,
        float? MaturityAge = null,
        bool Hibernates = false,
        float MinTemperature = -30f,
        float MaxTemperature = 50f);

    private sealed record JsonSpeciesDocument(int Version, List<JsonSpeciesEntry> Species);

    private static readonly BiomeType[] LandBiomes =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow, BiomeType.Cave, BiomeType.Volcano
    ];

    private static readonly BiomeType[] ShallowOrDeep =
        [BiomeType.ShallowWater, BiomeType.DeepOcean, BiomeType.CoralReef];

    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        AllowTrailingCommas = true,
        ReadCommentHandling = JsonCommentHandling.Skip
    };

    public static void Load(string path)
    {
        string json = File.ReadAllText(path);
        var doc = JsonSerializer.Deserialize<JsonSpeciesDocument>(json, Options)
                  ?? throw new InvalidDataException("Failed to parse species.json");

        foreach (var entry in doc.Species)
        {
            RegisterFromJson(entry);
        }
    }

    private static void RegisterFromJson(JsonSpeciesEntry e)
    {
        var kind = Enum.Parse<CreatureType>(e.Kind, ignoreCase: true);
        var biomes = ResolveBiomes(e, kind);
        float maturityAge = e.MaturityAge ?? GetDefaultMaturityAge(e.Name);
        Type creatureType = GetCreatureType(kind);

        if (kind == CreatureType.Plant)
        {
            var reproduction = ParseEnum<PlantReproductionMode>(e.Reproduction ?? "Vegetative");
            var pollination = e.Pollination != null
                ? ParseEnum<PollinationMode>(e.Pollination)
                : PollinationMode.None;

            SpeciesRegistry.Register(new SpeciesDefinition(
                species: e.Name,
                creatureType: creatureType,
                kind: kind,
                isAquatic: e.IsAquatic,
                socialBehavior: SocialBehavior.None,
                validBiomes: biomes,
                defaultSize: 1f,
                maturityAge: 0f,
                plantReproduction: reproduction,
                pollination: pollination,
                minTemperature: e.MinTemperature,
                maxTemperature: e.MaxTemperature));
        }
        else
        {
            var social = ParseEnum<SocialBehavior>(e.SocialBehavior ?? "Solitary");
            SpeciesRegistry.Register(new SpeciesDefinition(
                species: e.Name,
                creatureType: creatureType,
                kind: kind,
                isAquatic: e.IsAquatic,
                socialBehavior: social,
                validBiomes: biomes,
                defaultSize: e.Size,
                maturityAge: maturityAge,
                hibernates: e.Hibernates));
        }
    }

    private static BiomeType[] ResolveBiomes(JsonSpeciesEntry e, CreatureType kind)
    {
        if (e.Biomes is { Count: > 0 })
            return e.Biomes.Select(b => Enum.Parse<BiomeType>(b, ignoreCase: true)).ToArray();

        if (kind == CreatureType.Plant)
            return e.IsAquatic ? ShallowOrDeep : LandBiomes;

        return e.IsAquatic ? ShallowOrDeep : LandBiomes;
    }

    private static Type GetCreatureType(CreatureType kind) => kind switch
    {
        CreatureType.Herbivore => typeof(Herbivore),
        CreatureType.Carnivore => typeof(Carnivore),
        CreatureType.Omnivore => typeof(Omnivore),
        _ => typeof(Plant)
    };

    private static float GetDefaultMaturityAge(string species) => species switch
    {
        "Beetle" or "Butterfly" or "Frog" or "Piranha" or "Rabbit" => 10f,
        "Tuna" or "Jellyfish" or "Lizard" or "Salmon" => 15f,
        "Fox" or "Otter" or "Raccoon" => 20f,
        "Boar" or "Cheetah" or "Gazelle" or "Goat" or "Kangaroo" or "Seal" or
            "SeaLion" or "Sheep" => 25f,
        "Horse" or "Hippopotamus" or "Orca" or "Shark" or "Turtle" => 40f,
        "Walrus" or "Whale" => 50f,
        _ => 30f
    };

    private static T ParseEnum<T>(string value) where T : struct, Enum
    {
        if (Enum.TryParse<T>(value, ignoreCase: true, out var result))
            return result;
        throw new InvalidDataException($"Unknown {typeof(T).Name} value: '{value}'");
    }
}
