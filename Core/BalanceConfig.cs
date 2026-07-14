using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class BalanceConfig
{
    public static BalanceConfigData Data { get; private set; } = new();

    static BalanceConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "balance.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<BalanceConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch { /* use defaults */ }
    }
}

public class BalanceConfigData
{
    public int Version { get; set; } = 1;
    public CreatureBalance Creature { get; set; } = new();
    public ThirstBalance Thirst { get; set; } = new();
    public HibernationBalance Hibernation { get; set; } = new();
    public SleepBalance Sleep { get; set; } = new();
    public WindBalance Wind { get; set; } = new();
    public MovementBalance Movement { get; set; } = new();
    public InbreedingBalance Inbreeding { get; set; } = new();
    public TrophicBalance Trophic { get; set; } = new();
    public EcosystemBalance Ecosystem { get; set; } = new();
}

public class CreatureBalance
{
    public float MaxAge { get; set; } = 300f;
    public float InitialEnergyRatio { get; set; } = 0.5f;
    public float MaxEnergyBaseMultiplier { get; set; } = 50f;
    public float EnergyConsumptionBaseMultiplier { get; set; } = 0.5f;
    public float SpeedBase { get; set; } = 30f;
    public float VisionRangeBase { get; set; } = 32f;
    public float ReproductionThresholdRatio { get; set; } = 0.7f;
    public float LitterSizeMultiplier { get; set; } = 1.5f;
    public float ReproductionCooldownBase { get; set; } = 30f;
    public float ReproductionCooldownMetabolismFactor { get; set; } = 30f;
    public float DefenseSizeMultiplier { get; set; } = 5f;
    public float DefenseMetabolismMultiplier { get; set; } = 3f;
    public float AttackSpeedMultiplier { get; set; } = 4f;
    public float AttackSizeMultiplier { get; set; } = 2f;
    public float NutritionalValueSizeMultiplier { get; set; } = 5f;
    public float NutritionalValueToxicityRatio { get; set; } = 0.7f;
    public int MaxMemories { get; set; } = 5;
    public float MemoryDecayChance { get; set; } = 0.001f;
    public float MemoryDecayRateBase { get; set; } = 0.002f;
    public float MemoryDecayMemorySpanFactor { get; set; } = 0.8f;
}

public class ThirstBalance
{
    public float MaxThirst { get; set; } = 100f;
    public float BaseRate { get; set; } = 2f;
    public float EnergyMultiplierRate { get; set; } = 4f;
    public float PenaltyThresholdRatio { get; set; } = 0.9f;
    public float EnergyDrainMultiplier { get; set; } = 3f;
}

public class HibernationBalance
{
    public float EnterTemperature { get; set; } = 5f;
    public float WakeTemperature { get; set; } = 12f;
    public float TimeMultiplier { get; set; } = 0.05f;
}

public class SleepBalance
{
    public float TimeMultiplier { get; set; } = 0.3f;
}

public class WindBalance
{
    public float DriftSpeedLand { get; set; } = 6f;
    public float DriftSpeedAquaticMultiplier { get; set; } = 0.3f;
}

public class MovementBalance
{
    public float WaypointReachedDistance { get; set; } = 14f;
    public float InfantSpeedMultiplier { get; set; } = 0.5f;
}

public class InbreedingBalance
{
    public float CoefficientImpact { get; set; } = 0.5f;
    public float MinFitness { get; set; } = 0.5f;
}

public class TrophicBalance
{
    public float SampleInterval { get; set; } = 5f;
    public float LotkaVolterraDefaultRatio { get; set; } = 10f;
    public float PreyBoomRatioThreshold { get; set; } = 5f;
    public float PredatorPressureRatioThreshold { get; set; } = 2f;

    public float PreyBoomHerbivoreBirthBonus { get; set; } = 1.3f;
    public float PreyBoomHerbivoreDeathPenalty { get; set; } = 0.7f;
    public float PreyBoomCarnivoreBirthBonus { get; set; } = 1.5f;
    public float PreyBoomCarnivoreDeathPenalty { get; set; } = 0.5f;

    public float PredatorPressureHerbivoreBirthBonus { get; set; } = 0.6f;
    public float PredatorPressureHerbivoreDeathPenalty { get; set; } = 2f;
    public float PredatorPressureCarnivoreBirthBonus { get; set; } = 0.4f;
    public float PredatorPressureCarnivoreDeathPenalty { get; set; } = 2f;

    public float OvergrazingPlantRatio { get; set; } = 3f;
    public float OvergrazingDeathPenaltyMultiplier { get; set; } = 1.5f;
    public float OvergrazingBirthBonusMultiplier { get; set; } = 0.5f;

    public float PlantOvergrowthHerbivoreRatio { get; set; } = 5f;
    public float PlantOvergrowthBirthBonusMultiplier { get; set; } = 1.3f;

    public float MinBirthBonus { get; set; } = 0.2f;
    public float MaxBirthBonus { get; set; } = 2f;
    public float MinDeathPenalty { get; set; } = 0.3f;
    public float MaxDeathPenalty { get; set; } = 3f;
}

public class EcosystemBalance
{
    public int SpawnAquaticDivisor { get; set; } = 4;
    public int MaxPositionAttempts { get; set; } = 100;
    public float ProximitySqDist { get; set; } = 3600f;
    public int MaxNearby { get; set; } = 3;

    public float WindBiasThreshold { get; set; } = 0.3f;
    public float WindBiasLocalWeight { get; set; } = 0.3f;
    public float WindBiasWindWeight { get; set; } = 0.7f;
    public float SpreadMinDist { get; set; } = 30f;
    public float SpreadRange { get; set; } = 40f;
    public float NeighborRadius { get; set; } = 80f;
    public int MaxSameSpeciesNearby { get; set; } = 4;

    public double DecomposeRate { get; set; } = 0.02;
    public float DecayGrassBoost { get; set; } = 0.3f;
    public float SoilMax { get; set; } = 2f;
    public float SoilBoost { get; set; } = 0.1f;

    public float SoftCapRatio { get; set; } = 0.7f;
    public float PressureRangeRatio { get; set; } = 0.3f;
    public float MaxPressureMultiplier { get; set; } = 1.5f;

    public float MaxStaggerSeconds { get; set; } = 120f;

    public int StatsLogInterval { get; set; } = 60;
}
