using System;

namespace PitLife.Simulation;

internal static class BuiltinSpecies
{
    internal static readonly BiomeType[] Land =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow, BiomeType.Cave, BiomeType.Volcano
    ];

    internal static readonly BiomeType[] Shallow = [BiomeType.ShallowWater];
    internal static readonly BiomeType[] Deep = [BiomeType.DeepOcean];
    internal static readonly BiomeType[] ShallowOrDeep = [BiomeType.ShallowWater, BiomeType.DeepOcean, BiomeType.CoralReef];
    internal static readonly BiomeType[] LandAndShallow =
    [
        BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
        BiomeType.Mountain, BiomeType.Snow, BiomeType.ShallowWater
    ];

    public static void RegisterAll()
    {
        BuiltinPlants.RegisterAll();
        BuiltinAnimals.RegisterAll();
    }
}
