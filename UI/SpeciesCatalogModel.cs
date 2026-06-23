using System.Collections.Generic;
using System.Linq;
using PitLife.Simulation;

namespace PitLife.UI;

public static class SpeciesCatalogModel
{
    public static Dictionary<string, string[]> Build()
    {
        var byCategory = new Dictionary<string, string[]>
        {
            ["Plants"] = [.. SpeciesRegistry.OfType(CreatureType.Plant).Where(s => SpeciesRegistry.Get(s)?.IsAquatic != true)],
            ["AquaticPlants"] = [.. SpeciesRegistry.OfType(CreatureType.Plant).Where(s => SpeciesRegistry.Get(s)?.IsAquatic == true)],
            ["Herbivores"] = [.. SpeciesRegistry.OfType(CreatureType.Herbivore)],
            ["Carnivores"] = [.. SpeciesRegistry.OfType(CreatureType.Carnivore)],
            ["Omnivores"] = [.. SpeciesRegistry.OfType(CreatureType.Omnivore)]
        };
        return byCategory;
    }
}
