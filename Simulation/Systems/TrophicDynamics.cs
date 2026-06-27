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

        UpdatePopulationTracking(dt, herbivores, carnivores);

        float ratio = carnivores > 0 ? (float)herbivores / carnivores : BalanceConfig.Data.Trophic.LotkaVolterraDefaultRatio;

        UpdatePredatorPreyDynamics(ratio);
        ApplyPlantHerbivoreCoupling(herbivores, plants);

        // Clamp multipliers to reasonable ranges
        HerbivoreBirthBonus = Math.Clamp(HerbivoreBirthBonus, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);
        HerbivoreDeathPenalty = Math.Clamp(HerbivoreDeathPenalty, BalanceConfig.Data.Trophic.MinDeathPenalty, BalanceConfig.Data.Trophic.MaxDeathPenalty);
        CarnivoreBirthBonus = Math.Clamp(CarnivoreBirthBonus, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);
        CarnivoreDeathPenalty = Math.Clamp(CarnivoreDeathPenalty, BalanceConfig.Data.Trophic.MinDeathPenalty, BalanceConfig.Data.Trophic.MaxDeathPenalty);
    }

    /// <summary>
    /// Tracks historical population peaks and troughs.
    /// </summary>
    private void UpdatePopulationTracking(float dt, int herbivores, int carnivores)
    {
        _timeSinceLastSample += dt;
        if (_timeSinceLastSample >= BalanceConfig.Data.Trophic.SampleInterval)
        {
            _timeSinceLastSample = 0f;
            HerbivorePeak = Math.Max(HerbivorePeak, herbivores);
            CarnivorePeak = Math.Max(CarnivorePeak, carnivores);
            HerbivoreTrough = Math.Min(HerbivoreTrough, herbivores);
            CarnivoreTrough = Math.Min(CarnivoreTrough, carnivores);
        }
    }

    /// <summary>
    /// Adjusts birth and death multipliers based on Lotka-Volterra predator-prey ratio.
    /// </summary>
    private void UpdatePredatorPreyDynamics(float ratio)
    {
        if (ratio > BalanceConfig.Data.Trophic.PreyBoomRatioThreshold)
        {
            HerbivoreBirthBonus = BalanceConfig.Data.Trophic.PreyBoomHerbivoreBirthBonus;
            HerbivoreDeathPenalty = BalanceConfig.Data.Trophic.PreyBoomHerbivoreDeathPenalty;
            CarnivoreBirthBonus = BalanceConfig.Data.Trophic.PreyBoomCarnivoreBirthBonus;
            CarnivoreDeathPenalty = BalanceConfig.Data.Trophic.PreyBoomCarnivoreDeathPenalty;
            CurrentPhaseLabel = "Prey Boom";
            CyclePhase = 1f;
        }
        else if (ratio < BalanceConfig.Data.Trophic.PredatorPressureRatioThreshold)
        {
            HerbivoreBirthBonus = BalanceConfig.Data.Trophic.PredatorPressureHerbivoreBirthBonus;
            HerbivoreDeathPenalty = BalanceConfig.Data.Trophic.PredatorPressureHerbivoreDeathPenalty;
            CarnivoreBirthBonus = BalanceConfig.Data.Trophic.PredatorPressureCarnivoreBirthBonus;
            CarnivoreDeathPenalty = BalanceConfig.Data.Trophic.PredatorPressureCarnivoreDeathPenalty;
            CurrentPhaseLabel = "Predator Pressure";
            CyclePhase = -1f;
        }
        else
        {
            HerbivoreBirthBonus = 1f;
            HerbivoreDeathPenalty = 1f;
            CarnivoreBirthBonus = 1f;
            CarnivoreDeathPenalty = 1f;
            CurrentPhaseLabel = "Balanced";
            CyclePhase = 0f;
        }
    }

    /// <summary>
    /// Adjusts herbivore multipliers based on available plant population.
    /// </summary>
    private void ApplyPlantHerbivoreCoupling(int herbivores, int plants)
    {
        if (plants > 0 && herbivores > plants * BalanceConfig.Data.Trophic.OvergrazingPlantRatio)
        {
            HerbivoreDeathPenalty *= BalanceConfig.Data.Trophic.OvergrazingDeathPenaltyMultiplier;
            HerbivoreBirthBonus *= BalanceConfig.Data.Trophic.OvergrazingBirthBonusMultiplier;
            CurrentPhaseLabel = "Overgrazing";
        }
        else if (herbivores > 0 && plants > herbivores * BalanceConfig.Data.Trophic.PlantOvergrowthHerbivoreRatio)
        {
            HerbivoreBirthBonus *= BalanceConfig.Data.Trophic.PlantOvergrowthBirthBonusMultiplier;
        }
    }

    public string GetStatusLine()
    {
        return $"Trophic: {CurrentPhaseLabel} | H:{HerbivoreBirthBonus:F1}/{HerbivoreDeathPenalty:F1} C:{CarnivoreBirthBonus:F1}/{CarnivoreDeathPenalty:F1}";
    }
}
