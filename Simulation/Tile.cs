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
        }
    }
    public float Vegetation { get; set; }
    public bool IsPassable => Biome != BiomeType.DeepOcean
                           && Biome != BiomeType.ShallowWater
                           && Biome != BiomeType.Snow;

    public bool IsPassableFor(bool isAquatic) => isAquatic
        ? Biome is BiomeType.DeepOcean or BiomeType.ShallowWater
        : IsPassable;

    public Tile(BiomeType biome)
    {
        Biome = biome;
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
            _ => 0.0f
        };
}
