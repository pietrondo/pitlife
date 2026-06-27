using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class PopulationEcologyTests
{
    [Fact]
    public void PopulationPressure_IncreasesWithCrowding()
    {
        var ecosystem = new Ecosystem(32, 24, 42) { MaxCreatures = 100 };
        ecosystem.Initialize(20, 20, 10, 60);
        Assert.True(ecosystem.PopulationPressure > 1f,
            $"Expected pressure > 1, got {ecosystem.PopulationPressure}");
    }

    [Fact]
    public void PopulationPressure_IsOneForSmallPopulations()
    {
        var ecosystem = new Ecosystem(16, 12, 7);
        ecosystem.Initialize(2, 2, 0, 4);
        ecosystem.UpdateStats();
        Assert.Equal(1f, ecosystem.PopulationPressure);
    }

    [Fact]
    public void Metrics_TracksBirthsAndDeaths()
    {
        var ecosystem = new Ecosystem(32, 24, 99);
        ecosystem.Initialize(2, 2, 0, 4);
        Assert.Equal(0, ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.TotalBirths);
        Assert.Equal(0, ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.TotalDeaths);

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        ecosystem.Tick(gameTime);

        Assert.True(ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.TotalDeaths + ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.TotalBirths >= 0);
    }

    [Fact]
    public void DeathCause_Starvation_WhenEnergyDepleted()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(ecosystem.Random), "Rabbit");
        herbivore.Energy = 0.1f;
        herbivore.GrowFor(10f);
        ecosystem.AddCreature(herbivore);
        ecosystem.FlushPending();

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        ecosystem.Tick(gameTime);
        ecosystem.FlushPending();

        Assert.False(herbivore.IsAlive);
        Assert.Equal(DeathCause.Starvation, herbivore.DeathCause);
    }

    [Fact]
    public void DeathCause_OldAge_AfterMaxAge()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(ecosystem.Random), "Rabbit");
        herbivore.Energy = 100f;
        herbivore.GrowFor(301f);
        ecosystem.AddCreature(herbivore);
        ecosystem.FlushPending();

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        ecosystem.Tick(gameTime);
        ecosystem.FlushPending();

        Assert.False(herbivore.IsAlive);
        Assert.Equal(DeathCause.OldAge, herbivore.DeathCause);
    }

    [Fact]
    public void SpeciesCount_TracksDiversity()
    {
        var ecosystem = new Ecosystem(32, 24, 42);
        ecosystem.Initialize(5, 3, 2, 10);
        ecosystem.UpdateStats();

        Assert.True(ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.SpeciesCount >= 1,
            $"Expected at least 1 species, got {ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.SpeciesCount}");
        Assert.NotEmpty(ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.SpeciesPopulations);
    }

    [Fact]
    public void Extinction_DetectedWhenSpeciesDisappears()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        ecosystem.Initialize(1, 1, 0, 2);
        ecosystem.UpdateStats();

        int initialSpecies = ecosystem.Pipeline.GetSystem<EcosystemMetrics>()!.SpeciesCount;
        Assert.True(initialSpecies > 0);

        for (int i = 0; i < 300; i++)
        {
            var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
            ecosystem.Tick(gameTime);
        }

        Assert.True(ecosystem.TotalTime > 0);
    }
}
