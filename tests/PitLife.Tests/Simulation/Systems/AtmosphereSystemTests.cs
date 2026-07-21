using System;
using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Simulation.Systems;

public class AtmosphereSystemTests
{
    public AtmosphereSystemTests()
    {
        // Ensure static config is loaded/initialized. AtmosphereConfig.Data has defaults.
    }

    [Fact]
    public void Constructor_SetsInitialValues()
    {
        var system = new AtmosphereSystem();
        Assert.Equal(AtmosphereConfig.Data.InitialOxygen, system.Oxygen);
        Assert.Equal(AtmosphereConfig.Data.InitialCO2, system.CO2);
    }

    [Fact]
    public void Reset_RestoresInitialValues()
    {
        var system = new AtmosphereSystem();
        system.Update(0, 100, 10f); // change values

        Assert.NotEqual(AtmosphereConfig.Data.InitialOxygen, system.Oxygen);

        system.Reset();

        Assert.Equal(AtmosphereConfig.Data.InitialOxygen, system.Oxygen);
        Assert.Equal(AtmosphereConfig.Data.InitialCO2, system.CO2);
    }

    [Fact]
    public void Update_WithPlants_IncreasesOxygen()
    {
        var system = new AtmosphereSystem();
        var initialOxygen = system.Oxygen;

        system.Update(plantCount: 10, animalCount: 0, dt: 1.0f);

        Assert.True(system.Oxygen > initialOxygen, $"Expected Oxygen to increase. Init: {initialOxygen}, Final: {system.Oxygen}");
        // CO2 should also decay
        Assert.True(system.CO2 < AtmosphereConfig.Data.InitialCO2, "Expected CO2 to decay when no animals present.");
    }

    [Fact]
    public void Update_WithAnimals_DecreasesOxygenAndIncreasesCO2()
    {
        var system = new AtmosphereSystem();
        var initialOxygen = system.Oxygen;
        var initialCO2 = system.CO2;

        // Large number of animals to overcome CO2 decay
        system.Update(plantCount: 0, animalCount: 1000, dt: 1.0f);

        Assert.True(system.Oxygen < initialOxygen, $"Expected Oxygen to decrease. Init: {initialOxygen}, Final: {system.Oxygen}");
        Assert.True(system.CO2 > initialCO2, $"Expected CO2 to increase. Init: {initialCO2}, Final: {system.CO2}");
    }

    [Fact]
    public void Update_ClampsValuesToLimits()
    {
        var system = new AtmosphereSystem();

        // Try to increase O2 way past MaxLevel
        system.Update(plantCount: 1000000, animalCount: 0, dt: 10.0f);
        Assert.Equal(AtmosphereConfig.Data.MaxLevel, system.Oxygen);

        // Try to increase CO2 way past MaxLevel
        system.Update(plantCount: 0, animalCount: 1000000, dt: 10.0f);
        Assert.Equal(AtmosphereConfig.Data.MaxLevel, system.CO2);

        // Try to decrease O2 below 0
        system.Update(plantCount: 0, animalCount: 1000000, dt: 10.0f);
        Assert.Equal(0f, system.Oxygen);
    }

    [Fact]
    public void Modifiers_AreCalculatedCorrectly()
    {
        var system = new AtmosphereSystem();

        // Base modifiers should be near 1.0f when values are near initial
        // system.Oxygen / AtmosphereConfig.Data.OxygenModifierBase -> 50 / 50 = 1.0
        Assert.Equal(MathHelper.Clamp(AtmosphereConfig.Data.InitialOxygen / AtmosphereConfig.Data.OxygenModifierBase, AtmosphereConfig.Data.OxygenModifierMin, AtmosphereConfig.Data.OxygenModifierMax), system.OxygenModifier);

        Assert.Equal(MathHelper.Clamp(AtmosphereConfig.Data.InitialCO2 / AtmosphereConfig.Data.Co2ModifierBase, AtmosphereConfig.Data.Co2ModifierMin, AtmosphereConfig.Data.Co2ModifierMax), system.CO2Modifier);
    }
}
