using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Systems;

public class EcosystemMetricsTests
{
    [Fact]
    public void RecordBirth_IncrementsTotalBirths()
    {
        var metrics = new EcosystemMetrics();
        Assert.Equal(0, metrics.TotalBirths);

        metrics.RecordBirth();
        metrics.RecordBirth();

        Assert.Equal(2, metrics.TotalBirths);
    }

    [Fact]
    public void RecordDeath_UpdatesDeathCountersAndLastDeathInfo()
    {
        var metrics = new EcosystemMetrics();
        Assert.Equal(0, metrics.TotalDeaths);

        metrics.RecordDeath("Rabbit", DeathCause.Starvation);
        Assert.Equal(1, metrics.TotalDeaths);
        Assert.Equal(1, metrics.StarvationDeaths);
        Assert.Equal(DeathCause.Starvation, metrics.LastDeathCause);
        Assert.Equal("Rabbit", metrics.LastDeathSpecies);

        metrics.RecordDeath("Deer", DeathCause.OldAge);
        Assert.Equal(2, metrics.TotalDeaths);
        Assert.Equal(1, metrics.OldAgeDeaths);
        Assert.Equal(DeathCause.OldAge, metrics.LastDeathCause);
        Assert.Equal("Deer", metrics.LastDeathSpecies);

        metrics.RecordDeath("Wolf", DeathCause.Predation);
        Assert.Equal(3, metrics.TotalDeaths);
        Assert.Equal(1, metrics.PredationDeaths);
        Assert.Equal(DeathCause.Predation, metrics.LastDeathCause);
        Assert.Equal("Wolf", metrics.LastDeathSpecies);

        metrics.RecordDeath("Bear", DeathCause.Combat);
        Assert.Equal(4, metrics.TotalDeaths);
        Assert.Equal(1, metrics.CombatDeaths);
        Assert.Equal(DeathCause.Combat, metrics.LastDeathCause);
        Assert.Equal("Bear", metrics.LastDeathSpecies);
    }

    [Fact]
    public void ResetCounters_ClearsAllCounters()
    {
        var metrics = new EcosystemMetrics();
        metrics.RecordBirth();
        metrics.RecordDeath("Rabbit", DeathCause.Starvation);
        metrics.RecordDeath("Deer", DeathCause.OldAge);
        metrics.RecordDeath("Wolf", DeathCause.Predation);
        metrics.RecordDeath("Bear", DeathCause.Combat);

        metrics.ResetCounters();

        Assert.Equal(0, metrics.TotalBirths);
        Assert.Equal(0, metrics.TotalDeaths);
        Assert.Equal(0, metrics.StarvationDeaths);
        Assert.Equal(0, metrics.OldAgeDeaths);
        Assert.Equal(0, metrics.PredationDeaths);
        Assert.Equal(0, metrics.CombatDeaths);
    }

    [Fact]
    public void Reset_ClearsCollectionsAndCounters()
    {
        var metrics = new EcosystemMetrics();
        metrics.RecordBirth();
        metrics.RecordDeath("Rabbit", DeathCause.Starvation);

        // Simulate some population data populated by Update() normally
        metrics.SpeciesPopulations["Rabbit"] = 5;
        metrics.SubspeciesCounts["Rabbit/White"] = 2;
        metrics.SpeciesFirstAppearance["Rabbit"] = 1.0f;
        metrics.SpeciesMaxPopulation["Rabbit"] = 10;

        metrics.Reset();

        Assert.Equal(0, metrics.TotalBirths);
        Assert.Equal(0, metrics.TotalDeaths);
        Assert.Empty(metrics.SpeciesPopulations);
        Assert.Empty(metrics.SubspeciesCounts);
        Assert.Empty(metrics.SpeciesFirstAppearance);
        Assert.Empty(metrics.SpeciesMaxPopulation);
    }

    [Fact]
    public void Update_CalculatesEcosystemStatistics()
    {
        var eco = new Ecosystem(32, 24, 42);
        var metrics = new EcosystemMetrics();

        // Let's add some mock creatures directly.
        var plant = new Plant(new Vector2(10, 10), Genome.Random(eco.Random), "Clover");
        var herbivore = new Herbivore(new Vector2(20, 20), Genome.Random(eco.Random), "Rabbit");
        var carnivore = new Carnivore(new Vector2(30, 30), Genome.Random(eco.Random), "Wolf");

        herbivore.Subspecies = "White";

        eco.AddCreature(plant);
        eco.AddCreature(herbivore);
        eco.AddCreature(carnivore);

        // We need to trigger a tick to flush pending adds and update ecosystem's own counters first
        eco.Tick(new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1)));

        // Then update the metrics system with the ecosystem state
        metrics.Update(eco);

        Assert.Equal(3, metrics.TotalCreatures);
        Assert.Equal(1, metrics.Plants);
        Assert.Equal(1, metrics.Herbivores);
        Assert.Equal(1, metrics.Carnivores);
        Assert.Equal(0, metrics.Omnivores);

        Assert.Equal(3, metrics.SpeciesCount);
        Assert.Equal(1, metrics.SpeciesPopulations["Clover"]);
        Assert.Equal(1, metrics.SpeciesPopulations["Rabbit"]);
        Assert.Equal(1, metrics.SpeciesPopulations["Wolf"]);

        Assert.Equal(1, metrics.TotalSubspecies);
        Assert.Equal(1, metrics.SubspeciesCounts["Rabbit/White"]);

        Assert.True(metrics.SpeciesMaxPopulation.ContainsKey("Clover"));
        Assert.Equal(1, metrics.SpeciesMaxPopulation["Clover"]);

        Assert.Equal(1, metrics.TrophicLevel1); // Plant
        Assert.Equal(1, metrics.TrophicLevel2); // Herbivore
        Assert.Equal(1, metrics.TrophicLevel3Plus); // Carnivore

        // Heterozygosity/Inbreeding logic doesn't apply to plants, so we average for 2 non-plants.
        Assert.True(metrics.MeanHeterozygosity >= 0f);
        Assert.True(metrics.MeanInbreeding >= 0f);
    }
}
