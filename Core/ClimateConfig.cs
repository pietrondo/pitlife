using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class ClimateConfig
{
    public static ClimateConfigData Data { get; private set; } = new();

    static ClimateConfig()
    {
        try
        {
            var path = Path.Combine("Content", "config", "climate.json");
            if (File.Exists(path))
            {
                var json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<ClimateConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load climate.json: {ex.Message}");
        }
    }
}

public class ClimateConfigData
{
    public int Version { get; set; } = 1;
    public OrbitalConfig Orbital { get; set; } = new();
    public Dictionary<string, SeasonModifier> Seasons { get; set; } = new()
    {
        ["spring"] = new() { GrassRegenModifier = 1.3f, EnergyModifier = 1.0f },
        ["summer"] = new() { GrassRegenModifier = 0.9f, EnergyModifier = 1.0f },
        ["autumn"] = new() { GrassRegenModifier = 0.7f, EnergyModifier = 1.0f },
        ["winter"] = new() { GrassRegenModifier = 0.3f, EnergyModifier = 1.2f }
    };
    public ExtremeEventsConfig ExtremeEvents { get; set; } = new();
    public TemperatureConfig Temperature { get; set; } = new();
    public WindConfig Wind { get; set; } = new();
    public SpheroidConfig Spheroid { get; set; } = new();
}

public class OrbitalConfig
{
    public float PlanetRadiusKm { get; set; } = 6371f;
    public float DefaultOrbitalAU { get; set; } = 1f;
    public float DefaultEccentricity { get; set; } = 0.12f;
    public float OrbitalPeriod { get; set; } = 120f;
    public float BaseSurfaceTempK { get; set; } = 288f;
    public float BaseOrbitalSpeedKmS { get; set; } = 29.8f;
    public float TemperatureModifierAmplitude { get; set; } = 0.15f;
    public float OrbitalBoostRatio { get; set; } = 0.3f;
    public float KmPerAU { get; set; } = 149600000f;
}

public class SeasonModifier
{
    public float GrassRegenModifier { get; set; } = 1f;
    public float EnergyModifier { get; set; } = 1f;
}

public class ExtremeEventsConfig
{
    public float CooldownMin { get; set; } = 60f;
    public float CooldownMax { get; set; } = 180f;
    public float DurationBase { get; set; } = 15f;
    public float RainMultiplierBase { get; set; } = 1.5f;
    public float RainDampingFactor { get; set; } = 0.5f;
    public float HeatwaveTemperatureBoost { get; set; } = 10f;
    public float ColdSnapTemperatureDrop { get; set; } = 15f;
    public float StormWindBoost { get; set; } = 3f;
    public float StormWindMax { get; set; } = 5f;
}

public class TemperatureConfig
{
    public float TropicsLatitude { get; set; } = 30f;
    public float ArcticLatitude { get; set; } = 60f;
    public float EquatorBaseTemp { get; set; } = 35f;
    public float PoleBaseTemp { get; set; } = -20f;
    public float MidLatitudeBaseTemp { get; set; } = 20f;
    public float HeightTempLapseRate { get; set; } = 0.0065f;
    public float OceanModeration { get; set; } = 5f;
    public Dictionary<string, float> BiomeTempOffsets { get; set; } = new()
    {
        ["DeepOcean"] = 0f,
        ["ShallowWater"] = 0f,
        ["CoralReef"] = 0f,
        ["Beach"] = 2f,
        ["Desert"] = 8f,
        ["Savanna"] = 4f,
        ["Grassland"] = 0f,
        ["Forest"] = -2f,
        ["DenseForest"] = -4f,
        ["Swamp"] = 0f,
        ["Tundra"] = -5f,
        ["Mountain"] = -8f,
        ["Snow"] = -15f,
        ["Cave"] = -5f,
        ["Volcano"] = 10f
    };
}

public class WindConfig
{
    public float BaseSpeed { get; set; } = 0.5f;
    public float DirectionChangeRate { get; set; } = 0.5f;
    public float SpeedChangeRate { get; set; } = 0.3f;
    public float MinSpeed { get; set; } = 0f;
    public float MaxSpeed { get; set; } = 3f;
    public float StormSpeed { get; set; } = 6f;
}

public class SpheroidConfig
{
    public float EquatorialRadius { get; set; } = 6378f;
    public float PolarRadius { get; set; } = 6357f;
    public float Flattening { get; set; } = 0.00335f;
}
