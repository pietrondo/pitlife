using System;

namespace PitLife.Simulation;

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
