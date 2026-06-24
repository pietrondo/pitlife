using System;

namespace PitLife.Simulation;

internal static class BuiltinAnimals
{
    public static void RegisterAll()
    {
        RegisterHerbivores();
        RegisterCarnivores();
        RegisterOmnivores();
        RegisterMarineMammalsAndSemiAquatic();
        RegisterPrehistoric();
        RegisterInsects();
        RegisterCrustaceans();
        RegisterAmphibians();
    }

    private static void RegisterHerbivores()
    {
        // Herbivores
        RegisterAnimal("Rabbit", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land, size: 0.6f);
        RegisterAnimal("Deer", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Sheep", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Horse", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Goat", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Tuna", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: BuiltinSpecies.Shallow);
        RegisterAnimal("Lizard", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, size: 0.5f, hibernates: true);
        RegisterAnimal("Turtle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, hibernates: true);
        RegisterAnimal("Salmon", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.School, biomes: BuiltinSpecies.Shallow, size: 0.9f);
        RegisterAnimal("Moose", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp,
                BiomeType.Tundra, BiomeType.Mountain, BiomeType.Snow], size: 1.4f);
        RegisterAnimal("Gazelle", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Elephant", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land, size: 2.0f);
        RegisterAnimal("Giraffe", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Land, size: 1.8f);
        RegisterAnimal("Kangaroo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Savanna, BiomeType.Grassland, BiomeType.Desert], size: 1.1f);
    }

    private static void RegisterCarnivores()
    {
        // Carnivores
        RegisterAnimal("Fox", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Frog", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.LandAndShallow, size: 0.3f, hibernates: true);
        RegisterAnimal("Panther", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Lynx", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Tiger", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Lion", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Leopard", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Crocodile", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, hibernates: true);
        RegisterAnimal("Snake", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, size: 0.6f, hibernates: true);
        RegisterAnimal("Eagle", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair, biomes: BuiltinSpecies.Land, size: 0.7f);
        RegisterAnimal("Wolf", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pack, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Shark", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Deep, size: 1.2f);
        RegisterAnimal("Piranha", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: BuiltinSpecies.Shallow, size: 0.7f);
        RegisterAnimal("Owl", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp], size: 0.7f);
        RegisterAnimal("Cheetah", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, size: 0.9f);
    }

    private static void RegisterOmnivores()
    {
        // Omnivores
        RegisterAnimal("Boar", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land);
        RegisterAnimal("Raccoon", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, size: 0.6f, hibernates: true);
        RegisterAnimal("Beetle", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: BuiltinSpecies.Land, size: 0.15f, hibernates: true);
        RegisterAnimal("Butterfly", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm, biomes: BuiltinSpecies.Land, size: 0.12f);
        RegisterAnimal("Bear", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Land, hibernates: true);
        RegisterAnimal("Jellyfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm, biomes: BuiltinSpecies.ShallowOrDeep, size: 0.6f);
        RegisterAnimal("Pufferfish", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Solitary,
            biomes: BuiltinSpecies.ShallowOrDeep, size: 0.5f);
        RegisterAnimal("Badger", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.DenseForest], size: 0.8f, hibernates: true);
    }

    private static void RegisterMarineMammalsAndSemiAquatic()
    {
        // New Marine Mammals and Semi-Aquatic Creatures
        RegisterAnimal("Dolphin", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Pack, biomes: BuiltinSpecies.ShallowOrDeep, size: 1.0f);
        RegisterAnimal("Whale", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Herd, biomes: BuiltinSpecies.Deep, size: 2.0f);
        RegisterAnimal("Manatee", CreatureType.Herbivore, isAquatic: true, social: SocialBehavior.Solitary, biomes: BuiltinSpecies.Shallow, size: 1.3f);
        RegisterAnimal("Orca", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Pack, biomes: BuiltinSpecies.Deep, size: 1.8f);
        RegisterAnimal("Seal", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: BuiltinSpecies.ShallowOrDeep, size: 1.0f);
        RegisterAnimal("SeaLion", CreatureType.Carnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: BuiltinSpecies.ShallowOrDeep, size: 1.1f);
        RegisterAnimal("Otter", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Pair, biomes: BuiltinSpecies.Shallow, size: 0.8f);
        RegisterAnimal("Walrus", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Herd, biomes: BuiltinSpecies.ShallowOrDeep, size: 1.5f);
        RegisterAnimal("Hippopotamus", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Herd, biomes: BuiltinSpecies.LandAndShallow, size: 1.6f);
    }

    private static void RegisterPrehistoric()
    {
        RegisterAnimal("Mammoth", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Tundra, BiomeType.Snow, BiomeType.Mountain], size: 2.5f);
        RegisterAnimal("Sabertooth", CreatureType.Carnivore, isAquatic: false, social: SocialBehavior.Pair,
            biomes: BuiltinSpecies.Land, size: 1.3f);
        RegisterAnimal("Dodo", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Herd,
            biomes: [BiomeType.Grassland, BiomeType.Forest, BiomeType.Beach], size: 0.8f);
        RegisterAnimal("Trilobite", CreatureType.Omnivore, isAquatic: true, social: SocialBehavior.Swarm,
            biomes: BuiltinSpecies.Shallow, size: 0.3f);
    }

    private static void RegisterInsects()
    {
        RegisterAnimal("Ant", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: BuiltinSpecies.Land, size: 0.2f, hibernates: true);
        RegisterAnimal("Bee", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: BuiltinSpecies.Land, size: 0.15f);
        RegisterAnimal("Wasp", CreatureType.Omnivore, isAquatic: false, social: SocialBehavior.Swarm,
            biomes: BuiltinSpecies.Land, size: 0.2f);
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
            biomes: BuiltinSpecies.ShallowOrDeep, size: 0.1f);
    }

    private static void RegisterAmphibians()
    {
        RegisterAnimal("PoisonFrog", CreatureType.Herbivore, isAquatic: false, social: SocialBehavior.Solitary,
            biomes: [BiomeType.Swamp, BiomeType.Forest], size: 0.4f, hibernates: true);
    }

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
