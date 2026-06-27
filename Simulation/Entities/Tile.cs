using System;

namespace PitLife.Simulation;

public class Tile
{
    private BiomeType _biome;

    public BiomeType Biome
    {
        get => _biome;
        set
        {
            _biome = value;
            Vegetation = VegetationFor(value);
            MaxGrass = GrassFor(value);
            GrassAmount = MaxGrass;
            Temperature = BaseTemperatureFor(value);
        }
    }
    public float Vegetation { get; set; }
    public float MaxGrass { get; set; }
    public float GrassAmount { get; set; }
    public float SoilNutrients { get; set; } = 1f;
    public float Temperature { get; set; } = 20f;
    public BiomeType? OriginalBiome { get; set; }
    public float CataclysmDamage { get; set; }
    public bool IsPassable => Biome != BiomeType.DeepOcean
                            && Biome != BiomeType.ShallowWater
                            && Biome != BiomeType.Snow
                            && Biome != BiomeType.CoralReef;

    public bool IsPassableFor(bool isAquatic) => isAquatic
        ? Biome is BiomeType.DeepOcean or BiomeType.ShallowWater or BiomeType.CoralReef
        : Biome != BiomeType.DeepOcean && Biome != BiomeType.ShallowWater
            && Biome != BiomeType.Snow && Biome != BiomeType.CoralReef;

    public Tile(BiomeType biome)
    {
        Biome = biome;
    }

    public float EatGrass(float amount)
    {
        if (GrassAmount <= 0) return 0f;
        var eaten = Math.Min(GrassAmount, amount);
        GrassAmount -= eaten;
        return eaten;
    }

    public void RegenerateGrass(float dt)
    {
        var regenRate = 0.02f * SoilNutrients;
        GrassAmount = Math.Min(MaxGrass, GrassAmount + regenRate * dt);
    }

    public void RecoverFromCataclysm(float dt)
    {
        if (CataclysmDamage <= 0 || OriginalBiome == null) return;
        CataclysmDamage -= 0.008f * dt;
        if (CataclysmDamage <= 0)
        {
            CataclysmDamage = 0;
            if (Biome != OriginalBiome.Value)
            {
                Biome = OriginalBiome.Value;
                GrassAmount = MaxGrass * 0.3f;
                SoilNutrients = 0.5f;
            }
            OriginalBiome = null;
        }
    }

    private static float VegetationFor(BiomeType biome) => biome switch
    {
        BiomeType.DenseForest => 1.0f,
        BiomeType.Forest => 0.9f,
        BiomeType.Swamp => 0.8f,
        BiomeType.Grassland => 0.7f,
        BiomeType.Savanna => 0.4f,
        BiomeType.Tundra => 0.2f,
        BiomeType.Desert => 0.1f,
        BiomeType.Beach => 0.1f,
        BiomeType.CoralReef => 0.9f,
        BiomeType.Cave => 0f,
        BiomeType.Volcano => 0.3f,
        _ => 0.0f
    };

    private static float GrassFor(BiomeType biome) => biome switch
    {
        BiomeType.Grassland => 0.8f,
        BiomeType.Forest => 0.4f,
        BiomeType.Savanna => 0.35f,
        BiomeType.Swamp => 0.25f,
        BiomeType.DenseForest => 0.2f,
        BiomeType.Tundra => 0.1f,
        BiomeType.Desert => 0.05f,
        BiomeType.Beach => 0.05f,
        _ => 0f
    };

    private static float BaseTemperatureFor(BiomeType biome) => biome switch
    {
        BiomeType.Desert => 38f,
        BiomeType.Savanna => 32f,
        BiomeType.Grassland => 24f,
        BiomeType.Forest => 20f,
        BiomeType.DenseForest => 18f,
        BiomeType.Swamp => 22f,
        BiomeType.Tundra => 5f,
        BiomeType.Snow => -10f,
        BiomeType.Mountain => 10f,
        BiomeType.Beach => 25f,
        BiomeType.ShallowWater => 22f,
        BiomeType.DeepOcean => 15f,
        BiomeType.CoralReef => 26f,
        BiomeType.Cave => 12f,
        BiomeType.Volcano => 45f,
        _ => 20f
    };
}
