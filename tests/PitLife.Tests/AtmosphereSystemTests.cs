using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

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
    public void Oxygen_ClampedToMax()
    {
        var atmos = new AtmosphereSystem();
        atmos.Update(100000, 0, 10f);
        Assert.True(atmos.Oxygen <= 100f);
    }

    [Fact]
    public void CO2_ClampedToMax()
    {
        var atmos = new AtmosphereSystem();
        atmos.Update(0, 100000, 10f);
        Assert.True(atmos.CO2 <= 100f);
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
