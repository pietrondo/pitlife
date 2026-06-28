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

}
