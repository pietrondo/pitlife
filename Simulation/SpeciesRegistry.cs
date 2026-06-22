using System;
using System.Collections.Generic;
using System.Linq;

namespace PitLife.Simulation;

public enum SocialBehavior
{
    None,
    Solitary,
    Pair,
    Herd,
    Pack,
    School,
    Swarm
}

public enum PlantReproductionMode
{
    Seeds,
    Spores,
    Vegetative,
    Fragmentation,
    BroadcastSpawning
}

public enum PollinationMode
{
    None,
    Wind,
    Insects,
    Self,
    Water
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
    public float MaturityAge { get; }
    public PlantReproductionMode? PlantReproduction { get; }
    public PollinationMode Pollination { get; }

    public SpeciesDefinition(
        string species,
        Type creatureType,
        CreatureType kind,
        bool isAquatic,
        SocialBehavior socialBehavior,
        IEnumerable<BiomeType> validBiomes,
        float defaultSize = 1.0f,
        float maturityAge = 30f,
        PlantReproductionMode? plantReproduction = null,
        PollinationMode pollination = PollinationMode.None)
    {
        if (kind == global::PitLife.Simulation.CreatureType.Plant && plantReproduction is null)
            throw new ArgumentException("Plant species require a reproduction mode.", nameof(plantReproduction));
        if (kind != global::PitLife.Simulation.CreatureType.Plant &&
            (plantReproduction is not null || pollination != PollinationMode.None))
            throw new ArgumentException("Animal species cannot use plant reproduction settings.", nameof(plantReproduction));
        if (plantReproduction is not PlantReproductionMode.Seeds && pollination != PollinationMode.None)
            throw new ArgumentException("Only seed-producing species can define pollination.", nameof(pollination));

        Species = species;
        CreatureType = creatureType;
        Kind = kind;
        IsAquatic = isAquatic;
        SocialBehavior = socialBehavior;
        ValidBiomes = new HashSet<BiomeType>(validBiomes);
        DefaultSize = defaultSize;
        MaturityAge = maturityAge;
        PlantReproduction = plantReproduction;
        Pollination = pollination;
    }

    public bool IsValidBiome(BiomeType biome) => ValidBiomes.Contains(biome);
}

public static class SpeciesRegistry
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, SpeciesDefinition> _bySpecies = new(StringComparer.Ordinal);
    private static readonly Dictionary<CreatureType, List<string>> _byKind = new();

    static SpeciesRegistry()
    {
        BuiltinSpecies.RegisterAll();
    }

    public static void Register(SpeciesDefinition def)
    {
        lock (Sync)
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
    }

    public static SpeciesDefinition? Get(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) ? def : null;
    }

    public static bool Contains(string species)
    {
        lock (Sync)
            return _bySpecies.ContainsKey(species);
    }

    public static IEnumerable<string> All
    {
        get
        {
            lock (Sync)
                return _bySpecies.Keys.ToArray();
        }
    }

    public static IEnumerable<string> OfType(CreatureType kind)
    {
        lock (Sync)
            return _byKind.TryGetValue(kind, out var list) ? list.ToArray() : Array.Empty<string>();
    }

    public static bool IsPackAnimal(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) &&
                (def.SocialBehavior == SocialBehavior.Pack ||
                 def.SocialBehavior == SocialBehavior.Herd ||
                 def.SocialBehavior == SocialBehavior.School ||
                 def.SocialBehavior == SocialBehavior.Swarm);
    }

    public static bool IsSolitary(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) && def.SocialBehavior == SocialBehavior.Solitary;
    }

    public static bool IsValidBiome(string species, BiomeType biome)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) && def.IsValidBiome(biome);
    }

    internal static void Reset() { /* no-op: builtins registered in static ctor */ }

    internal static bool Unregister(string species)
    {
        lock (Sync)
        {
            if (!_bySpecies.Remove(species, out SpeciesDefinition? definition))
                return false;

            if (_byKind.TryGetValue(definition.Kind, out List<string>? speciesOfKind))
                speciesOfKind.Remove(species);
            return true;
        }
    }

    public static string DetermineEvolvedSpecies(CreatureType kind, Genome genome, string currentSpecies, Random rng)
    {
        bool isLandMammal = currentSpecies switch
        {
            "Rabbit" or "Deer" or "Sheep" or "Horse" or "Goat" or "Gazelle" or "Kangaroo" or
            "Fox" or "Lynx" or "Tiger" or "Lion" or "Leopard" or "Wolf" or "Cheetah" or
            "Boar" or "Raccoon" or "Bear" => true,
            _ => false
        };

        if (kind == CreatureType.Herbivore)
        {
            if (genome.WaterAdaptation >= 0.65f)
            {
                if (isLandMammal)
                {
                    if (genome.Speed >= 1.2f) return "Dolphin";
                    if (genome.Size >= 1.2f) return "Whale";
                    return "Manatee";
                }
                else
                {
                    return rng.Next(2) == 0 ? "Tuna" : "Salmon";
                }
            }
            if (genome.DesertAdaptation >= 0.45f && genome.Speed >= 1.2f && genome.Size >= 1.0f)
            {
                return "Kangaroo";
            }
            if (genome.DesertAdaptation >= 0.65f && genome.Size <= 0.8f)
            {
                return "Lizard";
            }
            if (genome.DesertAdaptation >= 0.55f && genome.Speed >= 1.1f)
            {
                return "Gazelle";
            }
            if (genome.Size <= 0.75f && genome.Speed >= 1.1f)
            {
                return "Rabbit";
            }
            if (genome.ColdAdaptation >= 0.55f && genome.Size <= 1.1f)
            {
                return "Goat";
            }
            if (genome.Size >= 1.25f && genome.Speed >= 1.1f)
            {
                return "Horse";
            }
            if (genome.Size >= 1.0f && genome.ForestAdaptation >= 0.5f)
            {
                return "Deer";
            }
            if (genome.Size >= 0.8f && genome.Size <= 1.2f && genome.Speed <= 0.9f)
            {
                return "Sheep";
            }
        }
        else if (kind == CreatureType.Carnivore)
        {
            if (genome.WaterAdaptation >= 0.65f)
            {
                if (isLandMammal)
                {
                    if (genome.Size >= 1.2f) return "Orca";
                    if (genome.ColdAdaptation >= 0.5f) return "Seal";
                    return "SeaLion";
                }
                else
                {
                    return rng.Next(2) == 0 ? "Shark" : "Piranha";
                }
            }
            if (genome.Speed >= 1.4f && (genome.DesertAdaptation >= 0.4f || genome.ForestAdaptation <= 0.4f))
            {
                return "Cheetah";
            }
            if (genome.WaterAdaptation >= 0.45f && genome.DesertAdaptation >= 0.45f)
            {
                return "Crocodile";
            }
            if (genome.DesertAdaptation >= 0.45f && genome.Speed >= 1.1f)
            {
                return "Lion";
            }
            if (genome.ColdAdaptation >= 0.45f && genome.Size >= 0.9f)
            {
                return "Wolf";
            }
            if (genome.ColdAdaptation >= 0.55f && genome.Size <= 1.0f)
            {
                return "Lynx";
            }
            if (genome.ForestAdaptation >= 0.65f && genome.Size >= 1.2f)
            {
                return "Tiger";
            }
            if (genome.ForestAdaptation >= 0.5f && genome.Size < 1.2f)
            {
                return "Leopard";
            }
            if (genome.Size <= 0.8f)
            {
                return "Fox";
            }
        }
        else if (kind == CreatureType.Omnivore)
        {
            if (genome.WaterAdaptation >= 0.65f)
            {
                if (isLandMammal)
                {
                    if (genome.Size >= 1.3f) return "Hippopotamus";
                    if (genome.ColdAdaptation >= 0.5f) return "Walrus";
                    return "Otter";
                }
                else
                {
                    return "Jellyfish";
                }
            }
            if (genome.WaterAdaptation >= 0.4f && genome.ForestAdaptation >= 0.4f)
            {
                return "Frog";
            }
            if (genome.Size >= 1.3f)
            {
                return "Bear";
            }
            if (genome.Size >= 0.9f && genome.Size < 1.3f)
            {
                return "Boar";
            }
            if (genome.Size < 0.9f)
            {
                return "Raccoon";
            }
        }

        return currentSpecies;
    }
}

internal static class BuiltinSpecies
{
    private static readonly BiomeType[] Land =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow, BiomeType.Cave, BiomeType.Volcano
    ];

    private static readonly BiomeType[] Shallow = [BiomeType.ShallowWater];
    private static readonly BiomeType[] Deep = [BiomeType.DeepOcean];
    private static readonly BiomeType[] ShallowOrDeep = [BiomeType.ShallowWater, BiomeType.DeepOcean, BiomeType.CoralReef];
    private static readonly BiomeType[] LandAndShallow =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow, BiomeType.ShallowWater
    ];

    public static void RegisterAll()
    {
        // Plants (Land)
        RegisterPlant("Clover", PlantReproductionMode.Vegetative);
        RegisterPlant("Poppy", PlantReproductionMode.Seeds, PollinationMode.Insects);
        RegisterPlant("Mushroom", PlantReproductionMode.Spores);
        RegisterPlant("GrassTuft", PlantReproductionMode.Seeds, PollinationMode.Wind);
        RegisterPlant("Cactus", PlantReproductionMode.Seeds, PollinationMode.Insects);
        RegisterPlant("Moss", PlantReproductionMode.Spores);
        RegisterPlant("BerryBush", PlantReproductionMode.Seeds, PollinationMode.Insects);
        RegisterPlant("Pine", PlantReproductionMode.Seeds, PollinationMode.Wind);
        RegisterPlant("Toadstool", PlantReproductionMode.Spores);
        RegisterPlant("OakTree", PlantReproductionMode.Seeds, PollinationMode.Wind);
        RegisterPlant("PineTree", PlantReproductionMode.Seeds, PollinationMode.Wind);
        RegisterPlant("Juniper", PlantReproductionMode.Seeds, PollinationMode.Insects);
        RegisterPlant("Bamboo", PlantReproductionMode.Seeds, PollinationMode.Wind);
        RegisterPlant("Lavender", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland, BiomeType.Mountain]);
        RegisterPlant("Fern", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp]);
        RegisterPlant("Sunflower", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Savanna, BiomeType.Grassland]);
        RegisterPlant("Chanterelle", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest]);
        RegisterPlant("Morel", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.Grassland]);
        RegisterPlant("OysterMushroom", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp]);

        // Aquatic Plants
        RegisterAquaticPlant("Seaweed", PlantReproductionMode.Fragmentation);
        RegisterAquaticPlant("Algae", PlantReproductionMode.Fragmentation);
        RegisterAquaticPlant("Kelp", PlantReproductionMode.Spores);
        RegisterAquaticPlant("WaterLily", PlantReproductionMode.Seeds, PollinationMode.Insects);
        RegisterAquaticPlant("Coral", PlantReproductionMode.BroadcastSpawning);

        // Herbivores
        RegisterAnimal("Rabbit", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land, size: 0.6f);
        RegisterAnimal("Deer", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Sheep", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Horse", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Goat", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Tuna", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: Shallow);
        RegisterAnimal("Lizard", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.5f);
        RegisterAnimal("Turtle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Salmon", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: Shallow, size: 0.9f);
        RegisterAnimal("Moose", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp,
                BiomeType.Tundra, BiomeType.Mountain, BiomeType.Snow], size: 1.4f);

        // Carnivores
        RegisterAnimal("Fox", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Lynx", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Tiger", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Lion", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Leopard", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Crocodile", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Snake", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.6f);
        RegisterAnimal("Eagle", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair, biomes: Land, size: 0.7f);
        RegisterAnimal("Wolf", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Shark", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Deep, size: 1.2f);
        RegisterAnimal("Piranha", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Shallow, size: 0.7f);
        RegisterAnimal("Owl", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp], size: 0.7f);

        // Omnivores
        RegisterAnimal("Boar", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Raccoon", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.6f);
        RegisterAnimal("Beetle", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: Land, size: 0.15f);
        RegisterAnimal("Butterfly", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: Land, size: 0.12f);
        RegisterAnimal("Bear", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Jellyfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm, biomes: ShallowOrDeep, size: 0.6f);
        RegisterAnimal("Badger", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest], size: 0.8f);

        // Misc
        RegisterAnimal("Gazelle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Kangaroo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land, size: 1.1f);
        RegisterAnimal("Cheetah", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.9f);

        // New Marine Mammals and Semi-Aquatic Creatures
        RegisterAnimal("Dolphin", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Pack, biomes: ShallowOrDeep, size: 1.0f);
        RegisterAnimal("Whale", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Herd, biomes: Deep, size: 2.0f);
        RegisterAnimal("Manatee", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Shallow, size: 1.3f);
        RegisterAnimal("Orca", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Deep, size: 1.8f);
        RegisterAnimal("Seal", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.0f);
        RegisterAnimal("SeaLion", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.1f);
        RegisterAnimal("Otter", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Pair, biomes: Shallow, size: 0.8f);
        RegisterAnimal("Walrus", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.5f);
        RegisterAnimal("Hippopotamus", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Herd, biomes: LandAndShallow, size: 1.6f);

        RegisterAnimal("Mammoth", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Tundra, BiomeType.Snow, BiomeType.Mountain], size: 2.5f);
        RegisterAnimal("Sabertooth", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair,
            biomes: Land, size: 1.3f);
        RegisterAnimal("Dodo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.Beach], size: 0.8f);
        RegisterAnimal("Trilobite", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm,
            biomes: Shallow, size: 0.3f);
        RegisterAnimal("Pufferfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: ShallowOrDeep, size: 0.5f);
        RegisterAnimal("PoisonFrog", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Swamp, BiomeType.Forest], size: 0.4f);
        RegisterPlant("Belladonna", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Forest, BiomeType.Swamp]);
        RegisterPlant("VenusFlyTrap", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Swamp]);
        RegisterPlant("PitcherPlant", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Swamp, BiomeType.Forest]);
        RegisterAnimal("Ant", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.2f);
        RegisterAnimal("Bee", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.15f);
        RegisterAnimal("Wasp", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.2f);
        RegisterAnimal("Mantis", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest], size: 0.3f);
        RegisterAnimal("Dragonfly", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Swamp, BiomeType.ShallowWater], size: 0.25f);
        RegisterAnimal("Crab", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Beach, BiomeType.ShallowWater], size: 0.3f);
        RegisterAnimal("Lobster", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: [BiomeType.DeepOcean, BiomeType.ShallowWater], size: 0.4f);
        RegisterAnimal("Shrimp", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School,
            biomes: ShallowOrDeep, size: 0.1f);
    }

    private static void RegisterPlant(
        string name,
        PlantReproductionMode reproduction,
        PollinationMode pollination = PollinationMode.None,
        BiomeType[]? biomes = null) =>
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: typeof(Plant),
            kind: CreatureType.Plant,
            isAquatic: false,
            socialBehavior: SocialBehavior.None,
            validBiomes: biomes ?? Land,
            plantReproduction: reproduction,
            pollination: pollination));

    private static void RegisterAquaticPlant(
        string name,
        PlantReproductionMode reproduction,
        PollinationMode pollination = PollinationMode.None) =>
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: typeof(Plant),
            kind: CreatureType.Plant,
            isAquatic: true,
            socialBehavior: SocialBehavior.None,
            validBiomes: ShallowOrDeep,
            plantReproduction: reproduction,
            pollination: pollination));

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
            defaultSize: size,
            maturityAge: GetMaturityAge(name)));
    }

    private static float GetMaturityAge(string species) => species switch
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
}
