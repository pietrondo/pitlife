using System;

namespace PitLife.Simulation;

internal static class BuiltinPlants
{
    public static void RegisterAll()
    {
        RegisterLandPlants();
        RegisterAquaticPlants();
        RegisterCarnivorousPlants();
    }

    private static void RegisterLandPlants()
    {
        // Plants (Land)
        RegisterPlant("Clover", PlantReproductionMode.Vegetative,
            minTemperature: -15f, maxTemperature: 35f);
        RegisterPlant("Poppy", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -5f, maxTemperature: 35f);
        RegisterPlant("Mushroom", PlantReproductionMode.Spores,
            minTemperature: -5f, maxTemperature: 28f);
        RegisterPlant("GrassTuft", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -15f, maxTemperature: 45f);
        RegisterPlant("Cactus", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: 15f, maxTemperature: 55f);
        RegisterPlant("Moss", PlantReproductionMode.Spores,
            minTemperature: -30f, maxTemperature: 25f);
        RegisterPlant("BerryBush", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -10f, maxTemperature: 35f);
        RegisterPlant("Pine", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -25f, maxTemperature: 30f);
        RegisterPlant("Toadstool", PlantReproductionMode.Spores,
            minTemperature: -5f, maxTemperature: 30f);
        RegisterPlant("OakTree", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -10f, maxTemperature: 32f);
        RegisterPlant("PineTree", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: -25f, maxTemperature: 25f);
        RegisterPlant("Juniper", PlantReproductionMode.Seeds, PollinationMode.Insects,
            minTemperature: -20f, maxTemperature: 35f);
        RegisterPlant("Bamboo", PlantReproductionMode.Seeds, PollinationMode.Wind,
            minTemperature: 5f, maxTemperature: 48f);
        RegisterPlant("Lavender", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland, BiomeType.Mountain],
            minTemperature: 0f, maxTemperature: 45f);
        RegisterPlant("Fern", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp],
            minTemperature: 0f, maxTemperature: 32f);
        RegisterPlant("Sunflower", PlantReproductionMode.Seeds, PollinationMode.Insects,
            [BiomeType.Savanna, BiomeType.Grassland],
            minTemperature: 10f, maxTemperature: 40f);
        RegisterPlant("Chanterelle", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest],
            minTemperature: 0f, maxTemperature: 28f);
        RegisterPlant("Morel", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.Grassland],
            minTemperature: 0f, maxTemperature: 32f);
        RegisterPlant("OysterMushroom", PlantReproductionMode.Spores, biomes:
            [BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp],
            minTemperature: 0f, maxTemperature: 32f);
        RegisterPlant("Belladonna", PlantReproductionMode.Seeds, PollinationMode.Insects,
            biomes: [BiomeType.Forest, BiomeType.Swamp],
            minTemperature: 5f, maxTemperature: 32f);
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
            minTemperature: 5f, maxTemperature: 35f);
        RegisterAquaticPlant("Coral", PlantReproductionMode.BroadcastSpawning,
            minTemperature: 20f, maxTemperature: 28f);
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
            validBiomes: biomes ?? BuiltinSpecies.Land,
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
            validBiomes: BuiltinSpecies.ShallowOrDeep,
            plantReproduction: reproduction,
            pollination: pollination,
            minTemperature: minTemperature,
            maxTemperature: maxTemperature));
}
