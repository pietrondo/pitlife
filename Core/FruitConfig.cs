namespace PitLife.Core;

public static class FruitConfig
{
    public static FruitConfigData Data { get; private set; } = 
        ConfigLoader.Load<FruitConfigData>("fruit.json");
}

public class FruitConfigData
{
    public int Version { get; set; } = 1;
    public int MaxFruits { get; set; } = 500;
    public float SpawnTimerBase { get; set; } = 3f;
    public float SpawnTimerVariance { get; set; } = 5f;
    public int SpawnAttempts { get; set; } = 10;
    public int FindPlantMaxTries { get; set; } = 50;
    public float SpawnOffsetMax { get; set; } = 40f;
    public float EnergyValueBase { get; set; } = 5f;
    public float EnergyValueVariance { get; set; } = 10f;
    public float LifetimeBase { get; set; } = 20f;
    public float LifetimeVariance { get; set; } = 30f;
    public float PoisonousToxicityBase { get; set; } = 0.6f;
    public float PoisonousToxicityVariance { get; set; } = 0.3f;
}
