using System.Collections.Generic;
using PitLife.Simulation;

namespace PitLife.Rendering;

public sealed record SpeciesAsset(string Species, string Path);
public sealed record GenderedSpeciesAsset(string Species, string MalePath, string FemalePath);

public static class AssetRegistry
{
    public const string FallbackPlant = "_fallback_plant";
    public const string FallbackHerbivore = "_fallback_herbivore";
    public const string FallbackCarnivore = "_fallback_carnivore";
    public const string FallbackOmnivore = "_fallback_omnivore";

    public static readonly IReadOnlyList<SpeciesAsset> Biomes = new SpeciesAsset[]
    {
        new(BiomeType.DeepOcean.ToString(), "Content/assets/biomes/biome_water.png"),
        new(BiomeType.ShallowWater.ToString(), "Content/assets/biomes/biome_shallow.png"),
        new(BiomeType.Beach.ToString(), "Content/assets/biomes/biome_sand.png"),
        new(BiomeType.Desert.ToString(), "Content/assets/biomes/biome_desert.png"),
        new(BiomeType.Savanna.ToString(), "Content/assets/biomes/biome_grass.png"),
        new(BiomeType.Grassland.ToString(), "Content/assets/biomes/biome_grass.png"),
        new(BiomeType.Forest.ToString(), "Content/assets/biomes/biome_forest.png"),
        new(BiomeType.DenseForest.ToString(), "Content/assets/biomes/biome_dense.png"),
        new(BiomeType.Swamp.ToString(), "Content/assets/biomes/biome_swamp.png"),
        new(BiomeType.Tundra.ToString(), "Content/assets/biomes/biome_tundra.png"),
        new(BiomeType.Mountain.ToString(), "Content/assets/biomes/biome_mountain.png"),
        new(BiomeType.Snow.ToString(), "Content/assets/biomes/biome_snow.png"),
    };

    public static readonly IReadOnlyList<SpeciesAsset> Fallbacks = new SpeciesAsset[]
    {
        new(FallbackPlant, "Content/assets/creatures/plants/shrubs/plant.png"),
        new(FallbackHerbivore, "Content/assets/creatures/mammals/herbivores/ungulates/herbivore.png"),
        new(FallbackCarnivore, "Content/assets/creatures/mammals/carnivores/felids/carnivore.png"),
        new(FallbackOmnivore, "Content/assets/creatures/mammals/omnivores/suids/omnivore.png"),
    };

    public static readonly IReadOnlyList<SpeciesAsset> SpeciesTextures = new SpeciesAsset[]
    {
        new("Deer", "Content/assets/creatures/mammals/herbivores/ungulates/deer.png"),
        new("Rabbit", "Content/assets/creatures/mammals/herbivores/lagomorphs/rabbit.png"),
        new("Fox", "Content/assets/creatures/mammals/carnivores/canids/fox.png"),
        new("Boar", "Content/assets/creatures/mammals/omnivores/suids/boar.png"),
        new("Flowers", "Content/assets/creatures/plants/flowers/flowers.png"),
        new("Mushroom", "Content/assets/creatures/plants/fungi/mushroom.png"),
        new("Sheep", "Content/assets/creatures/mammals/herbivores/ungulates/sheep.png"),
        new("Lynx", "Content/assets/creatures/mammals/carnivores/felids/lynx.png"),
        new("Raccoon", "Content/assets/creatures/mammals/omnivores/procyonids/raccoon.png"),
        new("Tiger", "Content/assets/creatures/mammals/carnivores/felids/tiger.png"),
        new("GrassTuft", "Content/assets/creatures/plants/grasses/grasstuft.png"),
        new("Cactus", "Content/assets/creatures/plants/succulents/cactus.png"),
        new("Horse", "Content/assets/creatures/mammals/herbivores/ungulates/horse.png"),
        new("Goat", "Content/assets/creatures/mammals/herbivores/ungulates/goat.png"),
        new("Lion", "Content/assets/creatures/mammals/carnivores/felids/lion.png"),
        new("Leopard", "Content/assets/creatures/mammals/carnivores/felids/leopard.png"),
        new("Crocodile", "Content/assets/creatures/reptiles/crocodilians/crocodile.png"),
        new("Butterfly", "Content/assets/creatures/invertebrates/insects/butterfly.png"),
        new("Moss", "Content/assets/creatures/plants/grasses/moss.png"),
        new("BerryBush", "Content/assets/creatures/plants/shrubs/berrybush.png"),
        new("Pine", "Content/assets/creatures/plants/trees/pine.png"),
        new("Toadstool", "Content/assets/creatures/plants/fungi/toadstool.png"),
        new("Snake", "Content/assets/creatures/reptiles/squamates/snake.png"),
        new("Eagle", "Content/assets/creatures/birds/raptors/eagle.png"),
        new("Frog", "Content/assets/creatures/amphibians/anurans/frog.png"),
        new("Beetle", "Content/assets/creatures/invertebrates/insects/beetle.png"),
        new("Fish", "Content/assets/creatures/fish/fish.png"),
        new("Lizard", "Content/assets/creatures/reptiles/squamates/lizard.png"),
        new("Wolf", "Content/assets/creatures/mammals/carnivores/canids/wolf.png"),
        new("Bear", "Content/assets/creatures/mammals/omnivores/ursids/bear.png"),
        new("Turtle", "Content/assets/creatures/reptiles/testudines/turtle.png"),
        new("Shark", "Content/assets/creatures/fish/shark.png"),
        new("Piranha", "Content/assets/creatures/fish/piranha.png"),
        new("Salmon", "Content/assets/creatures/fish/salmon.png"),
        new("Jellyfish", "Content/assets/creatures/fish/jellyfish.png"),
        // Nuove texture piante pixel art
        new("OakTree", "Content/assets/plants/trees/oak_tree.png"),
        new("PineTree", "Content/assets/plants/trees/pine_tree.png"),
        new("Bush", "Content/assets/plants/bushes/bush.png"),
        new("Grass", "Content/assets/plants/grass/grass.png"),
        new("Seaweed", "Content/assets/plants/aquatic/seaweed.png"),
        new("Kelp", "Content/assets/plants/aquatic/kelp.png"),
        new("WaterLily", "Content/assets/plants/aquatic/water_lily.png"),
        new("Coral", "Content/assets/plants/aquatic/coral.png"),
    };

    public static readonly IReadOnlyList<GenderedSpeciesAsset> GenderedSpeciesTextures = new GenderedSpeciesAsset[]
    {
        new("Lion",
            "Content/assets/creatures/mammals/carnivores/felids/lion_male.png",
            "Content/assets/creatures/mammals/carnivores/felids/lion_female.png"),
        new("Deer",
            "Content/assets/creatures/mammals/herbivores/ungulates/deer_male.png",
            "Content/assets/creatures/mammals/herbivores/ungulates/deer_female.png"),
        new("Sheep",
            "Content/assets/creatures/mammals/herbivores/ungulates/sheep_male.png",
            "Content/assets/creatures/mammals/herbivores/ungulates/sheep_female.png"),
        new("Goat",
            "Content/assets/creatures/mammals/herbivores/ungulates/goat_male.png",
            "Content/assets/creatures/mammals/herbivores/ungulates/goat_female.png"),
    };

    public const string SpawnIcon = "Content/assets/ui/spawn_icon.png";
}
