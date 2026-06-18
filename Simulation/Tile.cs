namespace PitLife.Simulation;

public class Tile
{
    public BiomeType Biome { get; set; }
    public float Vegetation { get; set; }
    public bool IsPassable => Biome != BiomeType.Water && Biome != BiomeType.Mountain;

    public Tile(BiomeType biome)
    {
        Biome = biome;
        Vegetation = biome switch
        {
            BiomeType.Forest => 1.0f,
            BiomeType.Grassland => 0.8f,
            BiomeType.Desert => 0.1f,
            _ => 0.0f
        };
    }
}
