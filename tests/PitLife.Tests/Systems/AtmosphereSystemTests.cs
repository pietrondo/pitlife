using Moq;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;
using System;

namespace PitLife.Tests.Systems;

public class AtmosphereSystemTests
{
    [Fact]
    public void InitialValues_AreSet()
    {
        var atmos = new AtmosphereSystem();
        Assert.Equal(50f, atmos.Oxygen);
        Assert.Equal(30f, atmos.CO2);
    }

    [Fact]
    public void Tick_UpdatesBasedOnEcosystemState()
    {
        var atmos = new AtmosphereSystem();
        var eco = new Ecosystem(16, 12, 42);
        eco.Initialize(20, 10, 0, 5); // Some plants and animals
        var mockTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        float initialO2 = atmos.Oxygen;
        float initialCo2 = atmos.CO2;

        atmos.Tick(eco, mockTime);

        Assert.NotEqual(initialO2, atmos.Oxygen);
        Assert.NotEqual(initialCo2, atmos.CO2);
    }

    [Fact]
    public void OxygenIncreases_WithPlants()
    {
        var atmos = new AtmosphereSystem();
        float before = atmos.Oxygen;
        atmos.Update(100, 0, 10f);
        Assert.True(atmos.Oxygen > before);
    }

    [Fact]
    public void OxygenDecreases_WithAnimals()
    {
        var atmos = new AtmosphereSystem();
        float before = atmos.Oxygen;
        atmos.Update(0, 100, 10f);
        Assert.True(atmos.Oxygen < before);
    }

    [Fact]
    public void CO2Increases_WithAnimals()
    {
        var atmos = new AtmosphereSystem();
        float before = atmos.CO2;
        atmos.Update(0, 50, 10f);
        Assert.True(atmos.CO2 > before);
    }

    [Fact]
    public void CO2Decays_WithoutAnimals()
    {
        var atmos = new AtmosphereSystem();
        float before = atmos.CO2;
        atmos.Update(0, 0, 10f);
        Assert.True(atmos.CO2 < before);
    }

    [Fact]
    public void EdgeCase_EmptyEcosystem_DecaysCo2AndO2RemainsStable()
    {
        var atmos = new AtmosphereSystem();
        float o2Before = atmos.Oxygen;
        float co2Before = atmos.CO2;

        atmos.Update(0, 0, 100f);

        Assert.Equal(o2Before, atmos.Oxygen);
        Assert.True(atmos.CO2 < co2Before);
    }

    [Fact]
    public void ExtremeValues_ClampedToMaxAndMin()
    {
        var atmos = new AtmosphereSystem();

        atmos.Update(1000000, 0, 1000f);
        Assert.Equal(100f, atmos.Oxygen);

        atmos.Update(0, 1000000, 1000f);
        Assert.Equal(100f, atmos.CO2);
        Assert.Equal(0f, atmos.Oxygen);
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var atmos = new AtmosphereSystem();
        atmos.Update(500, 500, 10f);
        atmos.Reset();
        Assert.Equal(50f, atmos.Oxygen);
        Assert.Equal(30f, atmos.CO2);
    }

    [Fact]
    public void OxygenModifier_AboveOne_WhenHigh()
    {
        var atmos = new AtmosphereSystem();
        atmos.Update(10000, 0, 10f);
        Assert.True(atmos.OxygenModifier > 1f);
    }

    [Fact]
    public void OxygenModifier_Clamped_Low()
    {
        var atmos = new AtmosphereSystem();
        atmos.Update(0, 50000, 10f);
        Assert.True(atmos.OxygenModifier >= 0.3f);
    }
}
