using System;

namespace PitLife.Simulation;

public static class EvolutionRules
{
    public static string DetermineEvolvedSpecies(CreatureType kind, Genome genome, string currentSpecies, Random rng)
    {
        bool isLandMammal = IsLandMammal(currentSpecies);

        string? evolved = kind switch
        {
            CreatureType.Herbivore => DetermineHerbivoreEvolution(genome, isLandMammal, rng),
            CreatureType.Carnivore => DetermineCarnivoreEvolution(genome, isLandMammal, rng),
            CreatureType.Omnivore => DetermineOmnivoreEvolution(genome, isLandMammal),
            _ => null
        };

        return evolved ?? currentSpecies;
    }

    private static bool IsLandMammal(string currentSpecies)
    {
        return currentSpecies switch
        {
            "Rabbit" or "Deer" or "Sheep" or "Horse" or "Goat" or "Gazelle" or "Kangaroo" or
            "Fox" or "Lynx" or "Tiger" or "Lion" or "Leopard" or "Wolf" or "Cheetah" or
            "Boar" or "Raccoon" or "Bear" => true,
            _ => false
        };
    }

    private static string? DetermineHerbivoreEvolution(Genome genome, bool isLandMammal, Random rng)
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
        return null;
    }

    private static string? DetermineCarnivoreEvolution(Genome genome, bool isLandMammal, Random rng)
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
        return null;
    }

    private static string? DetermineOmnivoreEvolution(Genome genome, bool isLandMammal)
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
        return null;
    }
}
