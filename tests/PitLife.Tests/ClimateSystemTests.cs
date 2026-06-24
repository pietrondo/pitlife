using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class ClimateSystemTests
{
    [Fact]
    public void Season_ProgressesThroughAllFour()
    {
        var climate = new ClimateSystem();
        var rng = new System.Random(42);

        climate.Update(0, rng);
        Assert.Equal(Season.Summer, climate.CurrentSeason);

        climate.Update(ClimateSystem.SeasonLength - 1, rng);
        Assert.Equal(Season.Summer, climate.CurrentSeason);

        climate.Update(ClimateSystem.SeasonLength + 5, rng);
        Assert.Equal(Season.Autumn, climate.CurrentSeason);

        climate.Update(ClimateSystem.SeasonLength * 2 + 5, rng);
        Assert.Equal(Season.Winter, climate.CurrentSeason);

        climate.Update(ClimateSystem.SeasonLength * 3 + 5, rng);
        Assert.Equal(Season.Spring, climate.CurrentSeason);
    }

    [Fact]
    public void Season_WrapsAroundYearly()
    {
        var climate = new ClimateSystem();
        var rng = new System.Random(42);

        climate.Update(ClimateSystem.YearLength + 10, rng);
        Assert.Equal(Season.Summer, climate.CurrentSeason);

        climate.Update(ClimateSystem.YearLength * 2 + ClimateSystem.SeasonLength + 5, rng);
        Assert.Equal(Season.Autumn, climate.CurrentSeason);
    }

    [Fact]
    public void GrassRegen_HighestInSpring()
    {
        var climate = new ClimateSystem();
        var rng = new System.Random(42);

        climate.Update(ClimateSystem.SeasonLength * 3.5f, rng);
        float springRegen = climate.GrassRegenModifier;

        climate.Update(ClimateSystem.SeasonLength * 2.5f, rng);
        float winterRegen = climate.GrassRegenModifier;

        Assert.True(springRegen > winterRegen,
            $"Spring regen {springRegen} should exceed winter {winterRegen}");
    }

    [Fact]
    public void EnergyModifier_HigherInWinter()
    {
        var climate = new ClimateSystem();
        var rng = new System.Random(42);

        climate.Update(ClimateSystem.SeasonLength * 0.5f, rng);
        float summerEnergy = climate.EnergyModifier;

        climate.Update(ClimateSystem.SeasonLength * 2.5f, rng);
        float winterEnergy = climate.EnergyModifier;

        Assert.True(winterEnergy > summerEnergy,
            $"Winter energy {winterEnergy} should exceed summer {summerEnergy}");
    }

    [Fact]
    public void Deterministic_SameTimeSameSeason()
    {
        var c1 = new ClimateSystem();
        var c2 = new ClimateSystem();
        var rng = new System.Random(42);

        float t = 150f;
        c1.Update(t, rng);
        c2.Update(t, rng);

        Assert.Equal(c1.CurrentSeason, c2.CurrentSeason);
        Assert.Equal(c1.GrassRegenModifier, c2.GrassRegenModifier);
        Assert.Equal(c1.EnergyModifier, c2.EnergyModifier);
        Assert.Equal(c1.TemperatureModifier, c2.TemperatureModifier);
    }

    [Fact]
    public void SeasonProgress_BetweenZeroAndOne()
    {
        var climate = new ClimateSystem();
        var rng = new System.Random(42);

        for (float t = 0; t < ClimateSystem.YearLength * 2; t += 10f)
        {
            climate.Update(t, rng);
            Assert.InRange(climate.SeasonProgress, 0f, 1f);
        }
    }

    [Fact]
    public void TestOblateSpheroidLatitude()
    {
        var climate = new ClimateSystem();
        // Compare latitude modifier to a standard spherical approach to ensure it applies flattening
        float sphericalLatitude = (100f / 199f - 0.5f) * System.MathF.PI;
        float sphericalCos = System.MathF.Cos(sphericalLatitude);

        float modifier = climate.GetLatitudeModifier(100f, 200);
        // Because of the oblate flattening, the eccentric latitude is slightly different from the pure spherical one
        // meaning the cosine should not be perfectly equal to the unflattened cosine (except at poles/equator).
        Assert.NotEqual(sphericalCos, modifier, 7);
    }
}
