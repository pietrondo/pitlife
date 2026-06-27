using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using PitLife.Core;
using Xunit;

namespace PitLife.Tests.Systems;

public class TrophicDynamicsTests
{
    private Ecosystem CreateEcosystemWithPopulations(int herbivores, int carnivores, int plants)
    {
        var eco = new Ecosystem(32, 24, 42);
        SetCount(eco, "PlantCount", plants);
        SetCount(eco, "HerbivoreCount", herbivores);
        SetCount(eco, "CarnivoreCount", carnivores);
        return eco;
    }

    private void SetCount(Ecosystem eco, string propName, int count)
    {
        typeof(Ecosystem).GetProperty(propName, BindingFlags.Public | BindingFlags.Instance)
            ?.SetValue(eco, count);
    }

    private Herbivore CreateHerbivore()
    {
        var genome = new Genome { Speed = 1f, Size = 1f, Metabolism = 1f };
        return new Herbivore(Vector2.Zero, genome, "Herbivore");
    }

    [Fact]
    public void Update_PreyBoom_WhenPreyExceedsRatioThreshold()
    {
        // 1 carnivore, 60 herbivores (ratio 60 > 5)
        // Adjust plants to avoid PlantOvergrowth or Overgrazing. PlantOvergrowth is plants > herbivores * 5. Overgrazing is herbivores > plants * 3.
        // For 60 herbivores, plants should be > 20 and < 300.
        var eco = CreateEcosystemWithPopulations(60, 1, 100);
        var dynamics = new TrophicDynamics();

        dynamics.Update(eco, 1f);

        Assert.Equal(BalanceConfig.Data.Trophic.PreyBoomHerbivoreBirthBonus, dynamics.HerbivoreBirthBonus);
        Assert.Equal(BalanceConfig.Data.Trophic.PreyBoomHerbivoreDeathPenalty, dynamics.HerbivoreDeathPenalty);
        Assert.Equal(BalanceConfig.Data.Trophic.PreyBoomCarnivoreBirthBonus, dynamics.CarnivoreBirthBonus);
        Assert.Equal(BalanceConfig.Data.Trophic.PreyBoomCarnivoreDeathPenalty, dynamics.CarnivoreDeathPenalty);
        Assert.Equal("Prey Boom", dynamics.CurrentPhaseLabel);
        Assert.Equal(1f, dynamics.CyclePhase);
    }

    [Fact]
    public void Update_PredatorPressure_WhenPredatorsExceedThreshold()
    {
        // 10 carnivores, 10 herbivores (ratio 1 < 2)
        // For 10 herbivores, plants > 3 and plants < 50 to avoid overgrazing / plant overgrowth
        var eco = CreateEcosystemWithPopulations(10, 10, 20);
        var dynamics = new TrophicDynamics();

        dynamics.Update(eco, 1f);

        Assert.Equal(BalanceConfig.Data.Trophic.PredatorPressureHerbivoreBirthBonus, dynamics.HerbivoreBirthBonus);
        Assert.Equal(BalanceConfig.Data.Trophic.PredatorPressureHerbivoreDeathPenalty, dynamics.HerbivoreDeathPenalty);
        Assert.Equal(BalanceConfig.Data.Trophic.PredatorPressureCarnivoreBirthBonus, dynamics.CarnivoreBirthBonus);
        Assert.Equal(BalanceConfig.Data.Trophic.PredatorPressureCarnivoreDeathPenalty, dynamics.CarnivoreDeathPenalty);
        Assert.Equal("Predator Pressure", dynamics.CurrentPhaseLabel);
        Assert.Equal(-1f, dynamics.CyclePhase);
    }

    [Fact]
    public void Update_Balanced_WhenRatioIsWithinThresholds()
    {
        // 3 carnivores, 10 herbivores (ratio 3.33)
        // plants = 20 (between 3 and 50)
        var eco = CreateEcosystemWithPopulations(10, 3, 20);
        var dynamics = new TrophicDynamics();

        dynamics.Update(eco, 1f);

        Assert.Equal(1f, dynamics.HerbivoreBirthBonus);
        Assert.Equal(1f, dynamics.HerbivoreDeathPenalty);
        Assert.Equal(1f, dynamics.CarnivoreBirthBonus);
        Assert.Equal(1f, dynamics.CarnivoreDeathPenalty);
        Assert.Equal("Balanced", dynamics.CurrentPhaseLabel);
        Assert.Equal(0f, dynamics.CyclePhase);
    }

    [Fact]
    public void Update_Overgrazing_AdjustsHerbivoreMultipliers()
    {
        // 40 herbivores, 10 plants (ratio 40 > 10 * 3) -> Overgrazing
        // carnivores = 12 (ratio 40/12 = 3.33, so balanced predator-prey)
        var eco = CreateEcosystemWithPopulations(40, 12, 10);
        var dynamics = new TrophicDynamics();

        dynamics.Update(eco, 1f);

        float expectedDeath = Math.Clamp(1f * BalanceConfig.Data.Trophic.OvergrazingDeathPenaltyMultiplier, BalanceConfig.Data.Trophic.MinDeathPenalty, BalanceConfig.Data.Trophic.MaxDeathPenalty);
        float expectedBirth = Math.Clamp(1f * BalanceConfig.Data.Trophic.OvergrazingBirthBonusMultiplier, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);

        Assert.Equal(expectedDeath, dynamics.HerbivoreDeathPenalty);
        Assert.Equal(expectedBirth, dynamics.HerbivoreBirthBonus);
        Assert.Equal("Overgrazing", dynamics.CurrentPhaseLabel);
    }

    [Fact]
    public void Update_PlantOvergrowth_AdjustsHerbivoreMultipliers()
    {
        // 10 herbivores, 100 plants (ratio 100 > 10 * 5) -> Plant Overgrowth
        // carnivores = 3 (ratio 10/3 = 3.33 -> balanced predator-prey)
        var eco = CreateEcosystemWithPopulations(10, 3, 100);
        var dynamics = new TrophicDynamics();

        dynamics.Update(eco, 1f);

        float expectedBirth = Math.Clamp(1f * BalanceConfig.Data.Trophic.PlantOvergrowthBirthBonusMultiplier, BalanceConfig.Data.Trophic.MinBirthBonus, BalanceConfig.Data.Trophic.MaxBirthBonus);

        Assert.Equal(expectedBirth, dynamics.HerbivoreBirthBonus);
        Assert.Equal("Balanced", dynamics.CurrentPhaseLabel); // Plant overgrowth does not override CurrentPhaseLabel in code
    }

    [Fact]
    public void GetStatusLine_ReturnsNonEmptyFormattedString()
    {
        var dynamics = new TrophicDynamics();
        var status = dynamics.GetStatusLine();

        Assert.False(string.IsNullOrWhiteSpace(status));
        Assert.Contains("Trophic: Balanced | H:1.0/1.0 C:1.0/1.0", status);
    }

    [Fact]
    public void Reset_ClearsState()
    {
        var dynamics = new TrophicDynamics();
        var eco = CreateEcosystemWithPopulations(60, 1, 100);
        dynamics.Update(eco, 1f); // Forces Prey Boom

        dynamics.Reset();

        Assert.Equal(1f, dynamics.HerbivoreBirthBonus);
        Assert.Equal(1f, dynamics.HerbivoreDeathPenalty);
        Assert.Equal(1f, dynamics.CarnivoreBirthBonus);
        Assert.Equal(1f, dynamics.CarnivoreDeathPenalty);
        Assert.Equal("Balanced", dynamics.CurrentPhaseLabel);
        Assert.Equal(0f, dynamics.CyclePhase);
    }

    [Fact]
    public void Update_SampleInterval_UpdatesPeaksAndTroughs()
    {
        var dynamics = new TrophicDynamics();
        var eco1 = CreateEcosystemWithPopulations(50, 10, 100);

        // Update with delta time enough to trigger sampling (default 5s)
        dynamics.Update(eco1, BalanceConfig.Data.Trophic.SampleInterval + 0.1f);

        Assert.Equal(50, dynamics.HerbivorePeak);
        Assert.Equal(10, dynamics.CarnivorePeak);
        Assert.Equal(50, dynamics.HerbivoreTrough);
        Assert.Equal(10, dynamics.CarnivoreTrough);

        var eco2 = CreateEcosystemWithPopulations(100, 5, 100);
        dynamics.Update(eco2, BalanceConfig.Data.Trophic.SampleInterval + 0.1f);

        Assert.Equal(100, dynamics.HerbivorePeak);
        Assert.Equal(10, dynamics.CarnivorePeak); // previous peak was higher (10 > 5)
        Assert.Equal(50, dynamics.HerbivoreTrough); // previous trough was lower (50 < 100)
        Assert.Equal(5, dynamics.CarnivoreTrough);
    }

    [Fact]
    public void EdgeCase_ZeroPopulations_HandledSafely()
    {
        var dynamics = new TrophicDynamics();
        var eco = CreateEcosystemWithPopulations(0, 0, 0);

        var exception = Record.Exception(() => dynamics.Update(eco, 1f));
        Assert.Null(exception); // Should not throw divide by zero

        // When carnivores == 0, ratio = LotkaVolterraDefaultRatio (10). 
        // 10 > PreyBoomRatioThreshold (5), so it's a Prey Boom.
        Assert.Equal("Prey Boom", dynamics.CurrentPhaseLabel);
    }

    [Fact]
    public void Creature_ApplyClimateAndPopulationPressure_UsesTrophicDeathPenalty()
    {
        // 10 carnivores, 10 herbivores (ratio 1 < 2 -> Predator pressure)
        // 20 plants
        var eco = CreateEcosystemWithPopulations(10, 10, 20); 
        eco.Trophic.Update(eco, 1f); // Applies Predator Pressure penalty

        var herbivore = CreateHerbivore();
        herbivore.Energy = 100f;
        
        SetCount(eco, "PopulationPressure", 1);
        
        float energyBefore = herbivore.Energy;
        float tempEnergy = herbivore.Energy; herbivore.EnvironmentState.ApplyClimateAndPopulationPressure(ref tempEnergy, herbivore.EnergyConsumption, herbivore.CreatureType, herbivore.Position, eco); herbivore.Energy = tempEnergy;
        float energyAfter = herbivore.Energy;
        
        Assert.True(energyBefore > energyAfter);
        
        // 3 carnivores, 10 herbivores -> Balanced (plants=20)
        var ecoBalanced = CreateEcosystemWithPopulations(10, 3, 20);
        ecoBalanced.Trophic.Update(ecoBalanced, 1f); 

        var herbivoreBalanced = CreateHerbivore();
        herbivoreBalanced.Energy = 100f;
        float tempEnergyBalanced = herbivoreBalanced.Energy; herbivoreBalanced.EnvironmentState.ApplyClimateAndPopulationPressure(ref tempEnergyBalanced, herbivoreBalanced.EnergyConsumption, herbivoreBalanced.CreatureType, herbivoreBalanced.Position, ecoBalanced); herbivoreBalanced.Energy = tempEnergyBalanced;
        float energyAfterBalanced = herbivoreBalanced.Energy;

        float dropPressure = energyBefore - energyAfter;
        float dropBalanced = 100f - energyAfterBalanced;

        // Since predator pressure death penalty is 2f and balanced is 1f, the drop should be roughly double.
        Assert.True(dropPressure > dropBalanced);
    }
}
