using System;
using PitLife.Core;

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
        var config = EvolutionConfig.Data.Herbivore;
        if (genome.WaterAdaptation >= config.WaterAdaptation)
        {
            if (isLandMammal)
            {
                if (genome.Speed >= config.DolphinSpeed) return "Dolphin";
                if (genome.Size >= config.WhaleSize) return "Whale";
                return "Manatee";
            }
            else
            {
                return rng.Next(2) == 0 ? "Tuna" : "Salmon";
            }
        }
        if (genome.DesertAdaptation >= config.KangarooDesertAdaptation && genome.Speed >= config.KangarooSpeed && genome.Size >= config.KangarooSize)
        {
            return "Kangaroo";
        }
        if (genome.DesertAdaptation >= config.LizardDesertAdaptation && genome.Size <= config.LizardSize)
        {
            return "Lizard";
        }
        if (genome.DesertAdaptation >= config.GazelleDesertAdaptation && genome.Speed >= config.GazelleSpeed)
        {
            return "Gazelle";
        }
        if (genome.Size <= config.RabbitSize && genome.Speed >= config.RabbitSpeed)
        {
            return "Rabbit";
        }
        if (genome.ColdAdaptation >= config.GoatColdAdaptation && genome.Size <= config.GoatSize)
        {
            return "Goat";
        }
        if (genome.Size >= config.HorseSize && genome.Speed >= config.HorseSpeed)
        {
            return "Horse";
        }
        if (genome.Size >= config.DeerSize && genome.ForestAdaptation >= config.DeerForestAdaptation)
        {
            return "Deer";
        }
        if (genome.Size >= config.SheepSizeMin && genome.Size <= config.SheepSizeMax && genome.Speed <= config.SheepSpeed)
        {
            return "Sheep";
        }
        return null;
    }

    private static string? DetermineCarnivoreEvolution(Genome genome, bool isLandMammal, Random rng)
    {
        var config = EvolutionConfig.Data.Carnivore;
        if (genome.WaterAdaptation >= config.WaterAdaptation)
        {
            if (isLandMammal)
            {
                if (genome.Size >= config.OrcaSize) return "Orca";
                if (genome.ColdAdaptation >= config.SealColdAdaptation) return "Seal";
                return "SeaLion";
            }
            else
            {
                return rng.Next(2) == 0 ? "Shark" : "Piranha";
            }
        }
        if (genome.Speed >= config.CheetahSpeed && (genome.DesertAdaptation >= config.CheetahDesertAdaptation || genome.ForestAdaptation <= config.CheetahForestAdaptation))
        {
            return "Cheetah";
        }
        if (genome.WaterAdaptation >= config.CrocodileWaterAdaptation && genome.DesertAdaptation >= config.CrocodileDesertAdaptation)
        {
            return "Crocodile";
        }
        if (genome.DesertAdaptation >= config.LionDesertAdaptation && genome.Speed >= config.LionSpeed)
        {
            return "Lion";
        }
        if (genome.ColdAdaptation >= config.WolfColdAdaptation && genome.Size >= config.WolfSize)
        {
            return "Wolf";
        }
        if (genome.ColdAdaptation >= config.LynxColdAdaptation && genome.Size <= config.LynxSize)
        {
            return "Lynx";
        }
        if (genome.ForestAdaptation >= config.TigerForestAdaptation && genome.Size >= config.TigerSize)
        {
            return "Tiger";
        }
        if (genome.ForestAdaptation >= config.LeopardForestAdaptation && genome.Size < config.LeopardSize)
        {
            return "Leopard";
        }
        if (genome.Size <= config.FoxSize)
        {
            return "Fox";
        }
        return null;
    }

    private static string? DetermineOmnivoreEvolution(Genome genome, bool isLandMammal)
    {
        var config = EvolutionConfig.Data.Omnivore;
        if (genome.WaterAdaptation >= config.WaterAdaptation)
        {
            if (isLandMammal)
            {
                if (genome.Size >= config.HippopotamusSize) return "Hippopotamus";
                if (genome.ColdAdaptation >= config.WalrusColdAdaptation) return "Walrus";
                return "Otter";
            }
            else
            {
                return "Jellyfish";
            }
        }
        if (genome.WaterAdaptation >= config.FrogWaterAdaptation && genome.ForestAdaptation >= config.FrogForestAdaptation)
        {
            return "Frog";
        }
        if (genome.Size >= config.BearSize)
        {
            return "Bear";
        }
        if (genome.Size >= config.BoarSizeMin && genome.Size < config.BoarSizeMax)
        {
            return "Boar";
        }
        if (genome.Size < config.RaccoonSize)
        {
            return "Raccoon";
        }
        return null;
    }
}
