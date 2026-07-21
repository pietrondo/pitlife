namespace PitLife.Core;

public static class GenomeConfig
{
    public static GenomeConfigData Data { get; private set; } = 
        ConfigLoader.Load<GenomeConfigData>("genome.json");
}

public class GenomeConfigData
{
    public int Version { get; set; } = 1;
    public float TraitDriftAmplitude { get; set; } = 0.02f;
    public float TraitMin { get; set; } = 0.5f;
    public float TraitMax { get; set; } = 2f;
    public float AdaptationDriftAmplitude { get; set; } = 0.01f;
    public float AdaptationMin { get; set; } = 0f;
    public float AdaptationMax { get; set; } = 1f;
}
