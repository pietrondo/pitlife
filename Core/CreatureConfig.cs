namespace PitLife.Core;

public static class CreatureConfig
{
    public static CreatureConfigData Data { get; private set; } = 
        ConfigLoader.Load<CreatureConfigData>("creatures.json");
}

public class CreatureConfigData
{
    public int Version { get; set; } = 1;
    public float ReproduceEnergyCostRatio { get; set; } = 0.3f;
    public float ChildOffsetRadius { get; set; } = 30f;
    public float ChildInitialEnergyRatio { get; set; } = 0.5f;
    public float GeneticDriftChance { get; set; } = 0.0001f;
    public float ScarcityGrassThreshold { get; set; } = 0.2f;
    public float ScarcityPenaltyFactor { get; set; } = 5f;
    public float ColdPrefTemp { get; set; } = 10f;
    public float HotPrefTemp { get; set; } = 35f;
    public float NeutralPrefTemp { get; set; } = 22f;
    public float InitialReproductionOffset { get; set; } = -60f;
    
    // Plant specific 
    public float ChildEnergyRatio { get; set; } = 0.5f;
    public float ReproductionEnergyCostRatio { get; set; } = 0.2f;
}
