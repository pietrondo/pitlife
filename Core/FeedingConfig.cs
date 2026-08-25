namespace PitLife.Core;

public static class FeedingConfig
{
    private static readonly FeedingConfigData Fallback = new();

    public static FeedingConfigData Data { get; } = ConfigLoader.Load("feeding.json", Fallback);
}

public class FeedingConfigData
{
    public int Version { get; set; } = 1;
    public float HungerThresholdHerbivore { get; set; } = 0.6f;
    public float HungerThresholdCarnivore { get; set; } = 0.8f;
    public float AttackEnergyGain { get; set; } = 3.0f;
    public float ToxicityReduction { get; set; } = 0.5f;
    public float PreyEscapeThreshold { get; set; } = 0.3f;
    public float FleeSpeedMultiplier { get; set; } = 0.7f;
    public float AttackRange { get; set; } = 10f;
    public float PlantEatRange { get; set; } = 12f;
    public float FallbackAttackDamage { get; set; } = 12f;
    public float PlantRecognitionThreshold { get; set; } = 0.5f;
    public float GrassAmountThreshold { get; set; } = 0.001f;
    public float MinAttackDamageFactor { get; set; } = 0.2f;
    public float ScavengeRange { get; set; } = 80f;
    public float HerbivorePlantEnergy { get; set; } = 8f;
    public float CarnivoreAttackCost { get; set; } = 3f;
    public float OmnivoreAttackCost { get; set; } = 4f;
    public float PlantDigestionRate { get; set; } = 4f;
    public float MaxFruitEatRange { get; set; } = 60f;
    public float HerbivoreConsumeRate { get; set; } = 10f;
    public float OmnivoreConsumeRate { get; set; } = 8f;
    public float OmnivorePlantDigestion { get; set; } = 1.5f;
    public float GrazeRate { get; set; } = 3f;
    public float ScavengeEatRate { get; set; } = 8f;
    public float ScavengeEnergyGain { get; set; } = 1.2f;
    public float PoisonFruitDamageMultiplier { get; set; } = 2f;
    public float PoisonPlantDamageMultiplier { get; set; } = 3f;
    public float OmnivoreHuntThreshold { get; set; } = 0.4f;
    public float DefenseDivisor { get; set; } = 25f;
}
