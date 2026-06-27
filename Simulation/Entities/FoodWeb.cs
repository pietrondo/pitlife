using System.Collections.Generic;

namespace PitLife.Simulation;

public enum DietType
{
    Herbivore,
    Carnivore,
    Omnivore,
    Insectivore,
    Piscivore
}

public static class FoodWeb
{
    public static bool CanEat(CreatureType eater, CreatureType target, DietType diet)
    {
        return diet switch
        {
            DietType.Herbivore => target == CreatureType.Plant,
            DietType.Carnivore => target is CreatureType.Herbivore or CreatureType.Omnivore,
            DietType.Omnivore => target is CreatureType.Plant or CreatureType.Herbivore or CreatureType.Omnivore,
            DietType.Insectivore => target is CreatureType.Herbivore or CreatureType.Omnivore,
            DietType.Piscivore => target is CreatureType.Herbivore or CreatureType.Omnivore,
            _ => false
        };
    }

    public static bool IsPredatorOf(CreatureType eater, CreatureType prey)
    {
        return eater switch
        {
            CreatureType.Carnivore => prey is CreatureType.Herbivore or CreatureType.Omnivore,
            CreatureType.Omnivore => prey is CreatureType.Herbivore or CreatureType.Omnivore,
            _ => false
        };
    }

    public static int TrophicLevel(CreatureType type) => type switch
    {
        CreatureType.Plant => 1,
        CreatureType.Herbivore => 2,
        CreatureType.Omnivore => 3,
        CreatureType.Carnivore => 4,
        _ => 1
    };

    public static float EnergyTransferEfficiency(CreatureType from, CreatureType to)
    {
        var fromLevel = TrophicLevel(from);
        var toLevel = TrophicLevel(to);
        if (toLevel <= fromLevel) return 0f;
        var steps = toLevel - fromLevel;
        return steps switch
        {
            1 => 0.15f,
            2 => 0.02f,
            _ => 0f
        };
    }

    public static string Describe(DietType diet) => diet switch
    {
        DietType.Herbivore => "Plants",
        DietType.Carnivore => "Meat (herbivores/omnivores)",
        DietType.Omnivore => "Plants and meat",
        DietType.Insectivore => "Small creatures",
        DietType.Piscivore => "Aquatic prey",
        _ => "Unknown"
    };

    public static List<string> BuildTrophicChain(CreatureType top, int depth = 3)
    {
        var chain = new List<string> { top.ToString() };
        var current = top;
        for (var i = 0; i < depth && current != CreatureType.Plant; i++)
        {
            current = current switch
            {
                CreatureType.Carnivore => CreatureType.Herbivore,
                CreatureType.Omnivore => CreatureType.Herbivore,
                CreatureType.Herbivore => CreatureType.Plant,
                _ => CreatureType.Plant
            };
            chain.Add(current.ToString());
        }
        return chain;
    }
}
