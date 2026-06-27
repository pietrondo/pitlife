using System;
using Moq;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class TrophicDynamicsTests
{
    private Ecosystem CreateMockEcosystem(int herbivores, int carnivores, int plants)
    {
        var eco = new Ecosystem(16, 12, 42);
        // We'll override the counts. We can't set properties easily, so let's mock it using a derived class
        return eco;
    }

    [Fact]
    public void Reset_RestoresDefaults()
    {
        var trophic = new TrophicDynamics();

        // Change some internal states directly or via update
        var eco = new Ecosystem(16, 12, 42);
        // Needs creatures to change things, or just reset empty

        trophic.Reset();

        Assert.Equal(1f, trophic.HerbivoreBirthBonus);
        Assert.Equal(1f, trophic.HerbivoreDeathPenalty);
        Assert.Equal(1f, trophic.CarnivoreBirthBonus);
        Assert.Equal(1f, trophic.CarnivoreDeathPenalty);
        Assert.Equal(0f, trophic.CyclePhase);
        Assert.Equal("Balanced", trophic.CurrentPhaseLabel);
    }

    // Testing specific population ratios


    private Ecosystem CreateConfiguredEcosystem(int herbivores, int carnivores, int plants)
    {
        var eco = new Ecosystem(16, 12, 42);
        // Instead of a derived class, just populate it directly
        for(int i = 0; i < herbivores; i++) eco.AddCreature(new Herbivore(default, PitLife.Simulation.Genome.Random(eco.Random), "Rabbit"));
        for(int i = 0; i < carnivores; i++) eco.AddCreature(new Carnivore(default, PitLife.Simulation.Genome.Random(eco.Random), "Wolf"));
        for(int i = 0; i < plants; i++) eco.AddCreature(new Plant(default, PitLife.Simulation.Genome.Random(eco.Random), "Clover"));

        eco.Tick(new Microsoft.Xna.Framework.GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));
        return eco;
    }

    [Fact]
    public void Update_PreyBoom_WhenRatioIsHigh()
    {
        var eco = CreateConfiguredEcosystem(100, 1, 100);
        var trophic = new TrophicDynamics();
        trophic.Update(eco, 1f);

        Assert.Equal(1f, trophic.CyclePhase);
        Assert.Equal("Prey Boom", trophic.CurrentPhaseLabel);
    }

    [Fact]
    public void Update_PredatorPressure_WhenRatioIsLow()
    {
        var eco = CreateConfiguredEcosystem(10, 50, 100);
        var trophic = new TrophicDynamics();
        trophic.Update(eco, 1f);

        Assert.Equal(-1f, trophic.CyclePhase);
        Assert.Equal("Predator Pressure", trophic.CurrentPhaseLabel);
    }

    [Fact]
    public void Update_Balanced_WhenRatioIsNormal()
    {
        var eco = CreateConfiguredEcosystem(50, 10, 100);
        var trophic = new TrophicDynamics();
        trophic.Update(eco, 1f);

        Assert.Equal(0f, trophic.CyclePhase);
        Assert.Equal("Balanced", trophic.CurrentPhaseLabel);
    }

    [Fact]
    public void Update_Overgrazing_WhenHerbivoresExceedPlants()
    {
        var eco = CreateConfiguredEcosystem(200, 40, 10);
        var trophic = new TrophicDynamics();
        trophic.Update(eco, 1f);

        Assert.Equal("Overgrazing", trophic.CurrentPhaseLabel);
        Assert.True(trophic.HerbivoreDeathPenalty > 1f);
    }

    [Fact]
    public void Update_TracksPeaksAndTroughs()
    {
        var trophic = new TrophicDynamics();

        var eco1 = CreateConfiguredEcosystem(10, 5, 0);
        trophic.Update(eco1, 10f); // Advance time past SampleInterval

        var initialPeakH = trophic.HerbivorePeak;
        var initialPeakC = trophic.CarnivorePeak;

        var eco2 = CreateConfiguredEcosystem(50, 20, 0);
        trophic.Update(eco2, 10f);

        Assert.True(trophic.HerbivorePeak > initialPeakH);
        Assert.True(trophic.CarnivorePeak > initialPeakC);
        Assert.Equal(50, trophic.HerbivorePeak);
        Assert.Equal(20, trophic.CarnivorePeak);
    }

    [Fact]
    public void GetStatusLine_ReturnsFormattedString()
    {
        var trophic = new TrophicDynamics();
        var status = trophic.GetStatusLine();

        Assert.Contains("Trophic: Balanced", status);
        Assert.Contains("H:1.0/1.0", status);
        Assert.Contains("C:1.0/1.0", status);
    }
}
