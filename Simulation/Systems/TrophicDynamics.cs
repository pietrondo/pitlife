using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class TrophicDynamics
{

    // Prey (herbivore) population growth rates
    public float HerbivoreBirthBonus { get; private set; } = 1f;
    public float HerbivoreDeathPenalty { get; private set; } = 1f;
    // Predator (carnivore) population growth rates
    public float CarnivoreBirthBonus { get; private set; } = 1f;
    public float CarnivoreDeathPenalty { get; private set; } = 1f;

    // Population history for oscillation tracking (last 60 data points)
    public int HerbivorePeak { get; private set; }
    public int CarnivorePeak { get; private set; }
    public int HerbivoreTrough { get; private set; } = int.MaxValue;
    public int CarnivoreTrough { get; private set; } = int.MaxValue;
    public float CyclePhase { get; private set; } // 0=balanced, +1=prey boom, -1=predator boom
    public string CurrentPhaseLabel { get; private set; } = "Balanced";

    private float _timeSinceLastSample;


    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world) { }

    public void Reset()
    {
        HerbivoreBirthBonus = 1f;
        HerbivoreDeathPenalty = 1f;
        CarnivoreBirthBonus = 1f;
        CarnivoreDeathPenalty = 1f;
        CyclePhase = 0f;
        CurrentPhaseLabel = "Balanced";
    }

    public void Update(Ecosystem eco, float dt)
    {
        int herbivores = eco.HerbivoreCount;
        int carnivores = eco.CarnivoreCount;
        int plants = eco.PlantCount;

        // Sample population for historical tracking
        _timeSinceLastSample += dt;
        if (_timeSinceLastSample >= BalanceConfig.Data.Trophic.SampleInterval)
        {
            _timeSinceLastSample = 0f;
            HerbivorePeak = Math.Max(HerbivorePeak, herbivores);
            CarnivorePeak = Math.Max(CarnivorePeak, carnivores);
            HerbivoreTrough = Math.Min(HerbivoreTrough, herbivores);
            CarnivoreTrough = Math.Min(CarnivoreTrough, carnivores);
        }

        // Lotka-Volterra: compute predator-prey ratio
        // When predators >> prey: prey drops, then predators starve
        // When prey >> predators: prey booms, then predators follow
        float ratio = carnivores > 0 ? (float)herbivores / carnivores : BalanceConfig.Data.Trophic.LotkaVolterraDefaultRatio;

        // Prey (herbivore) dynamics
        // Prey reproduce more when predators are scarce (high ratio)
        if (ratio > BalanceConfig.Data.Trophic.PreyBoomRatioThreshold)
        {
            // Prey boom: lots of herbivores, few carnivores
            HerbivoreBirthBonus = BalanceConfig.Data.Trophic.PreyBoomHerbivoreBirthBonus;
            HerbivoreDeathPenalty = BalanceConfig.Data.Trophic.PreyBoomHerbivoreDeathPenalty;
            CarnivoreBirthBonus = BalanceConfig.Data.Trophic.PreyBoomCarnivoreBirthBonus; // predators get bonus from abundant food
            CarnivoreDeathPenalty = BalanceConfig.Data.Trophic.PreyBoomCarnivoreDeathPenalty;
            CurrentPhaseLabel = "Prey Boom";
            CyclePhase = 1f;
        }
        else if (ratio < BalanceConfig.Data.Trophic.PredatorPressureRatioThreshold)
        {
            // Predator pressure: many carnivores per herbivore
            HerbivoreBirthBonus = BalanceConfig.Data.Trophic.PredatorPressureHerbivoreBirthBonus;
            HerbivoreDeathPenalty = BalanceConfig.Data.Trophic.PredatorPressureHerbivoreDeathPenalty; // prey death rate doubles
            CarnivoreBirthBonus = BalanceConfig.Data.Trophic.PredatorPressureCarnivoreBirthBonus; // predator birth slows (overcrowding)
            CarnivoreDeathPenalty = BalanceConfig.Data.Trophic.PredatorPressureCarnivoreDeathPenalty; // predators starve from competition
            CurrentPhaseLabel = "Predator Pressure";
            CyclePhase = -1f;
        }
        else
        {
            // Balanced
            HerbivoreBirthBonus = 1f;
            HerbivoreDeathPenalty = 1f;
            CarnivoreBirthBonus = 1f;
            CarnivoreDeathPenalty = 1f;
            CurrentPhaseLabel = "Balanced";
            CyclePhase = 0f;
        }

        // Additional plant-herbivore coupling
        if (plants > 0 && herbivores > plants * BalanceConfig.Data.Trophic.OvergrazingPlantRatio)
        {
            // Overgrazing: too many herbivores for available plants
            HerbivoreDeathPenalty *= BalanceConfig.Data.Trophic.OvergrazingDeathPenaltyMultiplier;
            HerbivoreBirthBonus *= BalanceConfig.Data.Trophic.OvergrazingBirthBonusMultiplier;
            CurrentPhaseLabel = "Overgrazing";
        }
        else if (herbivores > 0 && plants > herbivores * BalanceConfig.Data.Trophic.PlantOvergrowthHerbivoreRatio)
        {
            // Plant overgrowth: lots of food, few consumers
            HerbivoreBirthBonus *= BalanceConfig.Data.Trophic.PlantOvergrowthBirthBonusMultiplier;
        }

        // Clamp multipliers to reasonable ranges
        HerbivoreBirthBonus = Math.Clamp(HerbivoreBirthBonus, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);
        HerbivoreDeathPenalty = Math.Clamp(HerbivoreDeathPenalty, BalanceConfig.Data.Trophic.MinDeathPenalty, BalanceConfig.Data.Trophic.MaxDeathPenalty);
        CarnivoreBirthBonus = Math.Clamp(CarnivoreBirthBonus, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);
        CarnivoreDeathPenalty = Math.Clamp(CarnivoreDeathPenalty, BalanceConfig.Data.Trophic.MinDeathPenalty, BalanceConfig.Data.Trophic.MaxDeathPenalty);
    }

    public string GetStatusLine()
    {
        return $"Trophic: {CurrentPhaseLabel} | H:{HerbivoreBirthBonus:F1}/{HerbivoreDeathPenalty:F1} C:{CarnivoreBirthBonus:F1}/{CarnivoreDeathPenalty:F1}";
    }
}
