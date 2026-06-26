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


    /// <summary>
    /// Registers all built-in flora and fauna species into the global registry.
    /// </summary>
    public static void RegisterAll()
    {
        // Execute extracted helper functions for code health
        RegisterFlora();
        RegisterFauna();
    }

    /// <summary>
    /// Registers all flora species, including terrestrial, aquatic, and carnivorous plants.
    /// </summary>
    private static void RegisterFlora()
    {
        RegisterTerrestrialPlants();
        RegisterAquaticPlants();
        RegisterCarnivorousPlants();
    }

    /// <summary>
    /// Registers all fauna species, categorized by their diet and habitat.
    /// </summary>
    private static void RegisterFauna()
    {
        RegisterHerbivores();
        RegisterCarnivores();
        RegisterOmnivores();
        RegisterMarineMammals();
        RegisterPrehistoric();
        RegisterInsects();
        RegisterCrustaceans();
        RegisterAmphibians();
    }

    private static void RegisterTerrestrialPlants()
    {
        RegisterGrassesAndFlowers();
        RegisterFungi();
        RegisterTreesAndShrubs();
        RegisterOtherLandPlants();
    }

    private static void RegisterGrassesAndFlowers()
    {
        RegisterPlant("Clover", PlantReproductionMode.Vegetative,
            minTemperature: -15f, maxTemperature: 35f);
        RegisterPlant("Poppy", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -5f, maxTemperature: 35f);
        RegisterPlant("GrassTuft", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -15f, maxTemperature: 45f);
        RegisterPlant("Lavender", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland, BiomeType.Mountain],
            minTemperature: 0f, maxTemperature: 45f);
        RegisterPlant("Sunflower", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Savanna, BiomeType.Grassland],
            minTemperature: 10f, maxTemperature: 40f);
        RegisterPlant("Belladonna", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Forest, BiomeType.Swamp],
            minTemperature: 5f, maxTemperature: 32f);
    }

    private static void RegisterFungi()
    {
        RegisterCommonFungi();
        RegisterForestFungi();
    }

    private static void RegisterCommonFungi()
    {
        RegisterPlant("Mushroom", PlantReproductionMode.Spores,
            minTemperature: -5f, maxTemperature: 28f);
        RegisterPlant("Toadstool", PlantReproductionMode.Spores,
            minTemperature: -5f, maxTemperature: 30f);
    }

    private static void RegisterForestFungi()
    {
        RegisterPlant("Chanterelle", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest],
            minTemperature: 0f, maxTemperature: 28f);
        RegisterPlant("Morel", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.Grassland],
            minTemperature: 0f, maxTemperature: 32f);
        RegisterPlant("OysterMushroom", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp],
            minTemperature: 0f, maxTemperature: 32f);
    }

    private static void RegisterTreesAndShrubs()
    {
        RegisterPlant("BerryBush", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -10f, maxTemperature: 35f);
        RegisterPlant("Pine", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -25f, maxTemperature: 30f);
        RegisterPlant("OakTree", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -10f, maxTemperature: 32f);
        RegisterPlant("PineTree", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -25f, maxTemperature: 25f);
        RegisterPlant("Juniper", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -20f, maxTemperature: 35f);
        RegisterPlant("Bamboo", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: 5f, maxTemperature: 48f);
    }

    private static void RegisterOtherLandPlants()
    {
        RegisterPlant("Cactus", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: 15f, maxTemperature: 55f);
        RegisterPlant("Moss", PlantReproductionMode.Spores,
            minTemperature: -30f, maxTemperature: 25f);
        RegisterPlant("Fern", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp],
            minTemperature: 0f, maxTemperature: 32f);
    }

    private static void RegisterAquaticPlants()
    {
        // Aquatic Plants
        RegisterAquaticPlant("Seaweed", PlantReproductionMode.Fragmentation,
            minTemperature: -5f, maxTemperature: 35f);
        RegisterAquaticPlant("Algae", PlantReproductionMode.Fragmentation,
            minTemperature: -15f, maxTemperature: 45f);
        RegisterAquaticPlant("Kelp", PlantReproductionMode.Spores,
            minTemperature: -2f, maxTemperature: 28f);
        RegisterAquaticPlant("WaterLily", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: 10f, maxTemperature: 40f);
        RegisterAquaticPlant("Coral", PlantReproductionMode.BroadcastSpawning,
            minTemperature: 18f, maxTemperature: 40f);
    }

    private static void RegisterHerbivores()
    {
        RegisterHoofedHerbivores();
        RegisterAquaticHerbivores();
        RegisterOtherHerbivores();
    }

    private static void RegisterHoofedHerbivores()
    {
        RegisterAnimal("Deer", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Sheep", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Horse", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Goat", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
        RegisterAnimal("Moose", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp,
                BiomeType.Tundra, BiomeType.Mountain, BiomeType.Snow], size: 1.4f);
        RegisterAnimal("Gazelle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land);
    }

    private static void RegisterAquaticHerbivores()
    {
        RegisterAnimal("Tuna", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: Shallow);
        RegisterAnimal("Salmon", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: Shallow, size: 0.9f);
    }

    private static void RegisterOtherHerbivores()
    {
        RegisterAnimal("Rabbit", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land, size: 0.6f);
        RegisterAnimal("Lizard", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.5f, hibernates: true);
        RegisterAnimal("Turtle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, hibernates: true);
        RegisterAnimal("Kangaroo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: Land, size: 1.1f);
    }

    private static void RegisterCarnivores()
    {
        RegisterFelines();
        RegisterCanines();
        RegisterBirdsOfPrey();
        RegisterAquaticCarnivores();
        RegisterOtherCarnivores();
    }

    private static void RegisterFelines()
    {
        RegisterAnimal("Lynx", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Tiger", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Lion", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
        RegisterAnimal("Leopard", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Cheetah", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.9f);
    }

    private static void RegisterCanines()
    {
        RegisterAnimal("Fox", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Wolf", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: Land);
    }

    private static void RegisterBirdsOfPrey()
    {
        RegisterAnimal("Eagle", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair, biomes: Land, size: 0.7f);
        RegisterAnimal("Owl", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp], size: 0.7f);
    }

    private static void RegisterAquaticCarnivores()
    {
        RegisterAnimal("Shark", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Deep, size: 1.2f);
        RegisterAnimal("Piranha", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Shallow, size: 0.7f);
    }

    private static void RegisterOtherCarnivores()
    {
        RegisterAnimal("Crocodile", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, hibernates: true);
        RegisterAnimal("Snake", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.6f, hibernates: true);
    }

    private static void RegisterOmnivores()
    {
        RegisterTerrestrialOmnivores();
        RegisterAquaticOmnivores();
    }

    private static void RegisterTerrestrialOmnivores()
    {
        RegisterAnimal("Boar", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land);
        RegisterAnimal("Raccoon", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, size: 0.6f, hibernates: true);
        RegisterAnimal("Beetle", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: Land, size: 0.15f, hibernates: true);
        RegisterAnimal("Butterfly", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: Land, size: 0.12f);
        RegisterAnimal("Bear", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: Land, hibernates: true);
        RegisterAnimal("Badger", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest], size: 0.8f, hibernates: true);
    }

    private static void RegisterAquaticOmnivores()
    {
        RegisterAnimal("Jellyfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm, biomes: ShallowOrDeep, size: 0.6f);
        RegisterAnimal("Pufferfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: ShallowOrDeep, size: 0.5f);
    }

    private static void RegisterMarineMammals()
    {
        RegisterCetaceans();
        RegisterPinnipeds();
        RegisterOtherSemiAquatic();
    }

    private static void RegisterCetaceans()
    {
        RegisterAnimal("Dolphin", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Pack, biomes: ShallowOrDeep, size: 1.0f);
        RegisterAnimal("Whale", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Herd, biomes: Deep, size: 2.0f);
        RegisterAnimal("Orca", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: Deep, size: 1.8f);
    }

    private static void RegisterPinnipeds()
    {
        RegisterAnimal("Seal", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.0f);
        RegisterAnimal("SeaLion", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.1f);
        RegisterAnimal("Walrus", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: ShallowOrDeep, size: 1.5f);
    }

    private static void RegisterOtherSemiAquatic()
    {
        RegisterAnimal("Manatee", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: Shallow, size: 1.3f);
        RegisterAnimal("Otter", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Pair, biomes: Shallow, size: 0.8f);
        RegisterAnimal("Hippopotamus", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Herd, biomes: LandAndShallow, size: 1.6f);
    }

    private static void RegisterPrehistoric()
    {
        RegisterAnimal("Mammoth", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Tundra, BiomeType.Snow, BiomeType.Mountain], size: 2.5f);
        RegisterAnimal("Sabertooth", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair,
            biomes: Land, size: 1.3f);
        RegisterAnimal("Dodo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.Beach], size: 0.8f);
        RegisterAnimal("Trilobite", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm,
            biomes: Shallow, size: 0.3f);
    }

    private static void RegisterInsects()
    {
        RegisterAnimal("Ant", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.2f, hibernates: true);
        RegisterAnimal("Bee", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.15f);
        RegisterAnimal("Wasp", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: Land, size: 0.2f);
        RegisterAnimal("Mantis", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest], size: 0.3f);
        RegisterAnimal("Dragonfly", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Swamp, BiomeType.ShallowWater], size: 0.25f);
    }

    private static void RegisterCrustaceans()
    {
        RegisterAnimal("Crab", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Beach, BiomeType.ShallowWater], size: 0.3f);
        RegisterAnimal("Lobster", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: [BiomeType.DeepOcean, BiomeType.ShallowWater], size: 0.4f);
        RegisterAnimal("Shrimp", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School,
            biomes: ShallowOrDeep, size: 0.1f);
    }

    private static void RegisterAmphibians()
    {
        RegisterAnimal("PoisonFrog", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Swamp, BiomeType.Forest], size: 0.4f, hibernates: true);
    }

    private static void RegisterCarnivorousPlants()
    {
        RegisterPlant("VenusFlyTrap", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Swamp],
            minTemperature: 10f, maxTemperature: 36f);
        RegisterPlant("PitcherPlant", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Swamp, BiomeType.Forest],
            minTemperature: 8f, maxTemperature: 35f);
    }

    private static void RegisterPlant(
        string name,
        PlantReproductionMode reproduction,
        PollinationMode pollination = PollinationMode.None,
        BiomeType[]? biomes = null,
        float minTemperature = -30f,
        float maxTemperature = 50f) =>
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: typeof(Plant),
            kind: CreatureType.Plant,
            isAquatic: false,
            socialBehavior: SocialBehavior.None,
            validBiomes: biomes ?? Land,
            plantReproduction: reproduction,
            pollination: pollination,
            minTemperature: minTemperature,
            maxTemperature: maxTemperature));

    private static void RegisterAquaticPlant(
        string name,
        PlantReproductionMode reproduction,
        PollinationMode pollination = PollinationMode.None,
        float minTemperature = -5f,
        float maxTemperature = 36f) =>
        SpeciesRegistry.Register(new SpeciesDefinition(
            species: name,
            creatureType: typeof(Plant),
            kind: CreatureType.Plant,
            isAquatic: true,
            socialBehavior: SocialBehavior.None,
            validBiomes: ShallowOrDeep,
            plantReproduction: reproduction,
            pollination: pollination,
            minTemperature: minTemperature,
            maxTemperature: maxTemperature));

    private static void RegisterAnimal(string name, CreatureType kind, bool isAquatic,
        SocialBehavior social, BiomeType[] biomes, float size = 1.0f, bool hibernates = false)
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
            maturityAge: GetMaturityAge(name),
            hibernates: hibernates));
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
