using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class BehaviorConfig
{
    public static BehaviorConfigData Data { get; private set; } = new();

    static BehaviorConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "behaviors.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<BehaviorConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch { }
    }
}

public class BehaviorConfigData
{
    public int Version { get; set; } = 1;
    public MovementConfig Movement { get; set; } = new();
    public ThirstConfig Thirst { get; set; } = new();
    public FeedingConfig Feeding { get; set; } = new();
    public Dictionary<string, FlockingConfig> Flocking { get; set; } = new()
    {
        ["herd"] = new() { Cohesion = 1.5f, Separation = 2.0f, Alignment = 0.5f, SeparationDistance = 30f },
        ["pack"] = new() { Cohesion = 1.2f, Separation = 1.0f, Alignment = 1.5f, SeparationDistance = 25f, VisionMultiplier = 0.3f, EnergyBonusRate = 5f },
        ["school"] = new() { Cohesion = 1.0f, Separation = 0.8f, Alignment = 2.0f, SeparationDistance = 15f },
        ["swarm"] = new() { Cohesion = 2.0f, Separation = 2.5f, Alignment = 0.0f, SeparationDistance = 30f }
    };
    public PairBehaviorConfig PairBehavior { get; set; } = new();
    public SolitaryConfig Solitary { get; set; } = new();
    public InfantDefenseConfig InfantDefense { get; set; } = new();
    public ThreatDetectionConfig ThreatDetection { get; set; } = new();
    public PlantConfig Plants { get; set; } = new();
}

public class MovementConfig
{
    public float WanderSpeedCarnivore { get; set; } = 100f;
    public float WanderSpeedOmnivore { get; set; } = 90f;
    public float WanderSpeedHerbivore { get; set; } = 80f;
    public float FlockingTargetOffsetMultiplier { get; set; } = 100f;
}

public class ThirstConfig
{
    public float SeekThreshold { get; set; } = 30f;
}

public class FeedingConfig
{
    public float CarnivoreHungerThreshold { get; set; } = 0.8f;
    public float HerbivoreHungerThreshold { get; set; } = 0.6f;
    public float PlantEatDistance { get; set; } = 12f;
    public float PlantConsumptionRate { get; set; } = 10f;
    public float PlantEnergyConversion { get; set; } = 2f;
    public float PlantRecognitionThreshold { get; set; } = 0.5f;
    public float PoisonDamageMultiplier { get; set; } = 3f;
    public float MeleeAttackDistance { get; set; } = 10f;
    public float DefenseDivisor { get; set; } = 25f;
    public float MinDamageMultiplier { get; set; } = 0.2f;
    public float CarnivoreEnergyMultiplier { get; set; } = 1.5f;
    public float ToxicityPenaltyFactor { get; set; } = 0.5f;
    public float OmnivoreHuntThreshold { get; set; } = 0.4f;
    public float OmnivoreHuntProbability { get; set; } = 0.4f;
    public float OmnivoreDefaultDamage { get; set; } = 12f;
    public float OmnivorePlantConsumptionRate { get; set; } = 8f;
    public float OmnivoreEnergyConversion { get; set; } = 1.5f;
    public float MinGrassForGrazing { get; set; } = 0.001f;
    public float GrassEatingRate { get; set; } = 3f;
    public float GrassEnergyConversion { get; set; } = 8f;
    public float CarcassEatDistance { get; set; } = 10f;
    public float CarcassConsumptionRate { get; set; } = 8f;
    public float CarcassEnergyConversion { get; set; } = 1.2f;
}

public class FlockingConfig
{
    public float Cohesion { get; set; }
    public float Separation { get; set; }
    public float Alignment { get; set; }
    public float SeparationDistance { get; set; }
    public float VisionMultiplier { get; set; } = 1f;
    public float EnergyBonusRate { get; set; }
}

public class PairBehaviorConfig
{
    public float OuterDistance { get; set; } = 30f;
    public float ApproachMultiplier { get; set; } = 0.6f;
    public float InnerDistance { get; set; } = 15f;
    public float AvoidMultiplier { get; set; } = 0.4f;
    public float MaintainMultiplier { get; set; } = 0.2f;
    public float ComfortDistance { get; set; } = 50f;
    public float EnergyBonusRate { get; set; } = 2f;
}

public class SolitaryConfig
{
    public float NeighborVisionMultiplier { get; set; } = 0.5f;
    public float EnergyPenaltyRate { get; set; } = 3f;
    public float FleeThreshold { get; set; } = 0.3f;
    public float FleeSpeedMultiplier { get; set; } = 2.0f;
    public float LogEventProbability { get; set; } = 0.1f;
    public float CombatDistance { get; set; } = 20f;
    public float CombatDamageRate { get; set; } = 10f;
}

public class InfantDefenseConfig
{
    public float ScanRadius { get; set; } = 40f;
    public float ThreatScanRadius { get; set; } = 40f;
    public float AttackDistance { get; set; } = 10f;
    public float AttackDamageRate { get; set; } = 15f;
}

public class ThreatDetectionConfig
{
    public float HerbivoreVisionMultiplier { get; set; } = 0.8f;
    public float OmnivoreVisionMultiplier { get; set; } = 0.6f;
    public float CarnivoreVisionMultiplier { get; set; } = 0.0f;
}

public class PlantConfig
{
    public float VegetationSunlightCoefficient { get; set; } = 0.5f;
    public float SunlightBaseOffset { get; set; } = 0.5f;
    public float MaxAge { get; set; } = 300f;
}
