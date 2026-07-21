namespace PitLife.Core;

public static class EnvironmentConfig
{
    public static EnvironmentConfigData Data { get; private set; } = 
        ConfigLoader.Load<EnvironmentConfigData>("environment.json");
}

public class EnvironmentConfigData
{
    public int Version { get; set; } = 1;
    public float AltitudeThreshold { get; set; } = 0.6f;
    public float AltitudeFactor { get; set; } = 3f;
    public float LotkaPressureWeight { get; set; } = 0.5f;
    public float O2FactorWeight { get; set; } = 0.3f;
    public float TickDivisor { get; set; } = 60f;
    public float TemperaturePenaltyThreshold { get; set; } = 15f;
    public float TemperaturePenaltyFactor { get; set; } = 0.02f;
    public float InvalidBiomePenalty { get; set; } = 4f;
    public float InvalidTemperaturePenalty { get; set; } = 2f;
    public float AquaticSpeedMultiplier { get; set; } = 1f;
    public float AquaticEnergyMultiplier { get; set; } = 1f;
    public float StrandedSpeedMultiplier { get; set; } = 0.3f;
    public float StrandedEnergyMultiplier { get; set; } = 2.5f;
}
