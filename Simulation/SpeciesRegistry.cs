using System;
using System.Collections.Generic;

namespace PitLife.Simulation;

public enum SocialBehavior
{
    None,
    Pack,
    Solitary
}

public sealed class SpeciesDefinition
{
    public string Species { get; }
    public Type CreatureType { get; }
    public CreatureType Kind { get; }
    public bool IsAquatic { get; }
    public SocialBehavior SocialBehavior { get; }
    public HashSet<BiomeType> ValidBiomes { get; }
    public float DefaultSize { get; }

    public SpeciesDefinition(
        string species,
        Type creatureType,
        CreatureType kind,
        bool isAquatic,
        SocialBehavior socialBehavior,
        IEnumerable<BiomeType> validBiomes,
        float defaultSize = 1.0f)
    {
        Species = species;
        CreatureType = creatureType;
        Kind = kind;
        IsAquatic = isAquatic;
        SocialBehavior = socialBehavior;
        ValidBiomes = new HashSet<BiomeType>(validBiomes);
        DefaultSize = defaultSize;
    }

    public bool IsValidBiome(BiomeType biome) => ValidBiomes.Contains(biome);
}

public static class SpeciesRegistry
{
    private static readonly Dictionary<string, SpeciesDefinition> _bySpecies = new(StringComparer.Ordinal);
    private static readonly Dictionary<CreatureType, List<string>> _byKind = new();

    static SpeciesRegistry()
    {
        BuiltinSpecies.RegisterAll();
    }

    public static void Register(SpeciesDefinition def)
    {
        _bySpecies[def.Species] = def;
        if (!_byKind.TryGetValue(def.Kind, out var list))
        {
            list = new List<string>();
            _byKind[def.Kind] = list;
        }
        if (!list.Contains(def.Species))
            list.Add(def.Species);
    }

    public static SpeciesDefinition? Get(string species) =>
        _bySpecies.TryGetValue(species, out var def) ? def : null;

    public static IEnumerable<string> All => _bySpecies.Keys;

    public static IEnumerable<string> OfType(CreatureType kind) =>
        _byKind.TryGetValue(kind, out var list) ? list : Array.Empty<string>();

    public static bool IsPackAnimal(string species) =>
        _bySpecies.TryGetValue(species, out var def) && def.SocialBehavior == SocialBehavior.Pack;

    public static bool IsSolitary(string species) =>
        _bySpecies.TryGetValue(species, out var def) && def.SocialBehavior == SocialBehavior.Solitary;

    public static bool IsValidBiome(string species, BiomeType biome) =>
        _bySpecies.TryGetValue(species, out var def) && def.IsValidBiome(biome);

    internal static void Reset() { /* no-op: builtins registered in static ctor */ }
}

internal static class BuiltinSpecies
{
    private static readonly BiomeType[] Land =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow
    ];

    private static readonly BiomeType[] Shallow = [BiomeType.ShallowWater];
    private static readonly BiomeType[] Deep = [BiomeType.DeepOcean];
    private static readonly BiomeType[] ShallowOrDeep = [BiomeType.ShallowWater, BiomeType.DeepOcean];

    public static void RegisterAll()
    {
        // Plants
        RegisterPlant("Plant");
        RegisterPlant("Flowers");
        RegisterPlant("Mushroom");
        RegisterPlant("GrassTuft");
        RegisterPlant("Cactus");
        RegisterPlant("Moss");
        RegisterPlant("BerryBush");
        RegisterPlant("Pine");
        RegisterPlant("Toadstool");

        // Herbivores
        RegisterAnimal("Rabbit", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Deer", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Sheep", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Horse", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Goat", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Fish", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Shallow);
        RegisterAnimal("Lizard", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Turtle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Salmon", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Shallow, size: 0.9f);

        // Carnivores
        RegisterAnimal("Fox", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Lynx", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Tiger", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Lion", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Leopard", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Crocodile", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Snake", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Eagle", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Wolf", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Shark", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Deep, size: 1.2f);
        RegisterAnimal("Piranha", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Shallow, size: 0.7f);

        // Omnivores
        RegisterAnimal("Boar", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Raccoon", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Frog", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Beetle", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Butterfly", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Bear", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Jellyfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: ShallowOrDeep, size: 0.6f);

        // Misc
        RegisterAnimal("Gazelle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
    }

    private static void RegisterPlant(string name) =>
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: typeof(Plant),
            kind: CreatureType.Plant,
            isAquatic: false,
            socialBehavior: SocialBehavior.None,
            validBiomes: Land));

    private static void RegisterAnimal(string name, CreatureType kind, bool isAquatic,
        SocialBehavior social, BiomeType[] biomes, float size = 1.0f)
    {
        Type type = kind switch
        {
            CreatureType.Herbivore => typeof(Herbivore),
            CreatureType.Carnivore => typeof(Carnivore),
            CreatureType.Omnivore => typeof(Omnivore),
            _ => typeof(Creature)
        };
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: type,
            kind: kind,
            isAquatic: isAquatic,
            socialBehavior: social,
            validBiomes: biomes,
            defaultSize: size));
    }
}
