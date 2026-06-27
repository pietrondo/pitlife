using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public struct EnvironmentResponse
{
    public float Thirst;

    public void Reset()
    {
        Thirst = 0f;
    }

    public void ApplyClimateAndPopulationPressure(ref float energy, float energyConsumption, CreatureType type, Vector2 position, Ecosystem ecosystem)
    {
        if (type == CreatureType.Plant) return;
        float seasonalFactor = ecosystem.Climate.EnergyModifier;
        float pressureFactor = ecosystem.PopulationPressure;
        float o2Factor = 2f - ecosystem.Atmosphere.OxygenModifier;
        float altitude = ecosystem.World.GetElevation(position.X, position.Y);
        float altitudeFactor = altitude > 0.6f ? (altitude - 0.6f) * 3f : 0f;

        // Lotka-Volterra trophic dynamics: adjust death rate based on predator-prey balance
        float trophicDeathMultiplier = type switch
        {
            CreatureType.Herbivore => ecosystem.Trophic.HerbivoreDeathPenalty,
            CreatureType.Carnivore => ecosystem.Trophic.CarnivoreDeathPenalty,
            _ => 1f
        };

        float combinedFactor = seasonalFactor - 1f + (pressureFactor - 1f) * 0.5f + o2Factor * 0.3f + altitudeFactor;
        energy -= energyConsumption * combinedFactor * trophicDeathMultiplier * (1f / 60f);
    }

    public void ApplyWindDrift(ref Vector2 position, CreatureType type, bool isAquatic, float windDir, float windSpeed, float dt, World world)
    {
        if (type == CreatureType.Plant) return;
        float drift = windSpeed * BalanceConfig.Data.Wind.DriftSpeedLand * dt;
        if (isAquatic) drift *= BalanceConfig.Data.Wind.DriftSpeedAquaticMultiplier;
        Vector2 push = new Vector2(MathF.Cos(windDir) * drift, MathF.Sin(windDir) * drift);
        Vector2 newPos = Motor.ClampToWorld(position + push, world);
        if (world.GetTileAtPosition(newPos.X, newPos.Y).IsPassableFor(isAquatic))
            position = newPos;
    }

    public void UpdateEnvironmentalMultipliers(ref float currentSpeedMultiplier, ref float currentEnergyMultiplier, CreatureType type, string species, Genome genome, Vector2 position, float temperaturePreference, bool isAquatic, World world, Ecosystem ecosystem)
    {
        var tile = world.GetTileAtPosition(position.X, position.Y);
        if (tile == null)
        {
            currentSpeedMultiplier = 1f;
            currentEnergyMultiplier = 1f;
            return;
        }

        if (isAquatic)
        {
            UpdateAquaticMultipliers(ref currentSpeedMultiplier, ref currentEnergyMultiplier, tile);
            return;
        }

        UpdateTerrestrialMultipliers(ref currentSpeedMultiplier, ref currentEnergyMultiplier, genome, tile);

        int tileY = (int)(position.Y / ecosystem.World.TileSize);
        float tileTemp = ecosystem.Climate.GetTileTemperature(tile, tileY, ecosystem.World.Height);
        float tempDiff = Math.Abs(tileTemp - temperaturePreference);
        if (tempDiff > 15f && type != CreatureType.Plant)
            currentEnergyMultiplier += tempDiff * 0.02f;

        if (type != CreatureType.Plant)
        {
            var def = SpeciesRegistry.Get(species);
            if (def != null)
            {
                if (!def.IsValidBiome(tile.Biome))
                    currentEnergyMultiplier += 4.0f;
                if (!def.IsValidTemperature(tileTemp))
                    currentEnergyMultiplier += 2.0f;
            }
        }
    }

    private void UpdateAquaticMultipliers(ref float currentSpeedMultiplier, ref float currentEnergyMultiplier, Tile tile)
    {
        bool inWater = tile.Biome is BiomeType.DeepOcean or BiomeType.ShallowWater or BiomeType.CoralReef;
        if (inWater)
        {
            currentSpeedMultiplier = 1.0f;
            currentEnergyMultiplier = 1.0f;
        }
        else
        {
            currentSpeedMultiplier = 0.3f;
            currentEnergyMultiplier = 2.5f;
        }
    }

    private void UpdateTerrestrialMultipliers(ref float currentSpeedMultiplier, ref float currentEnergyMultiplier, Genome genome, Tile tile)
    {
        switch (tile.Biome)
        {
            case BiomeType.Desert:
            case BiomeType.Savanna:
            case BiomeType.Beach:
                currentEnergyMultiplier = 1.0f + (1.0f - genome.DesertAdaptation) * 1.0f;
                currentSpeedMultiplier = 0.6f + genome.DesertAdaptation * 0.4f;
                break;

            case BiomeType.Tundra:
            case BiomeType.Snow:
            case BiomeType.Mountain:
                currentEnergyMultiplier = 1.0f + (1.0f - genome.ColdAdaptation) * 1.5f;
                currentSpeedMultiplier = 0.5f + genome.ColdAdaptation * 0.5f;
                break;

            case BiomeType.Forest:
            case BiomeType.DenseForest:
            case BiomeType.Swamp:
                currentEnergyMultiplier = 1.0f + (1.0f - genome.ForestAdaptation) * 0.3f;
                currentSpeedMultiplier = 0.6f + genome.ForestAdaptation * 0.4f;
                break;

            case BiomeType.DeepOcean:
            case BiomeType.ShallowWater:
                currentEnergyMultiplier = 1.0f + (1.0f - genome.WaterAdaptation) * 0.8f;
                currentSpeedMultiplier = 0.4f + genome.WaterAdaptation * 0.6f;
                break;

            default: // Grassland or others with no penalty
                currentSpeedMultiplier = 1.0f;
                currentEnergyMultiplier = 1.0f;
                break;
        }
    }

    public void UpdateHibernation(ref bool hibernating, string species, CreatureType type, Vector2 position, Ecosystem ecosystem)
    {
        if (type == CreatureType.Plant) return;
        var def = SpeciesRegistry.Get(species);
        if (def == null || !def.Hibernates) return;
        var tile = ecosystem.World.GetTileAtPosition(position.X, position.Y);
        float temp = ecosystem.Climate.GetTileTemperature(tile, position.Y / ecosystem.World.TileSize, ecosystem.World.Height);
        if (temp < BalanceConfig.Data.Hibernation.EnterTemperature && !hibernating)
        {
            hibernating = true;
            Logger.Event("HIBERNATE", $"{species} entered hibernation at ({position.X:F0},{position.Y:F0}) T={temp:F0}°C");
        }
        else if (temp > BalanceConfig.Data.Hibernation.WakeTemperature && hibernating)
        {
            hibernating = false;
            Logger.Event("HIBERNATE", $"{species} woke from hibernation at ({position.X:F0},{position.Y:F0}) T={temp:F0}°C");
        }
    }
}
