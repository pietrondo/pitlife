using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class DiseaseSystemTests
{
    [Fact]
    public void DiseaseSystem_HasThreePresets()
    {
        Assert.Equal(3, DiseaseSystem.Presets.Length);
        Assert.Contains(DiseaseSystem.Presets, d => d.Name == "Fever");
        Assert.Contains(DiseaseSystem.Presets, d => d.Name == "Plague");
        Assert.Contains(DiseaseSystem.Presets, d => d.Name == "Parasite");
    }

    [Fact]
    public void Creature_HasInfectionProperties()
    {
        var rng = new System.Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");

        Assert.False(herbivore.IsInfected);
        Assert.Equal("", herbivore.DiseaseName);
        Assert.Equal(0f, herbivore.Immunity);
    }

    [Fact]
    public void Infection_CanBeSetAndCleared()
    {
        var rng = new System.Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");

        herbivore.IsInfected = true;
        herbivore.DiseaseName = "Fever";
        herbivore.DiseaseTimer = 30f;

        Assert.True(herbivore.IsInfected);
        Assert.Equal("Fever", herbivore.DiseaseName);

        herbivore.IsInfected = false;
        herbivore.DiseaseName = "";
        Assert.False(herbivore.IsInfected);
    }

    [Fact]
    public void Immunity_Accumulates()
    {
        var rng = new System.Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        Assert.Equal(0f, herbivore.Immunity);

        herbivore.Immunity = 0.3f;
        herbivore.Immunity = MathHelper.Min(1f, herbivore.Immunity + 0.3f);
        Assert.Equal(0.6f, herbivore.Immunity, 2);
    }

    [Fact]
    public void DiseaseSystem_CanRunWithoutOutbreak()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        ecosystem.Initialize(2, 2, 0, 4);

        var dt = 1f / 60f;
        ecosystem.Pipeline.GetSystem<DiseaseSystem>()!.Update(ecosystem, dt, ecosystem.Random);

        Assert.False(ecosystem.Pipeline.GetSystem<DiseaseSystem>()!.HasOutbreak);
    }
}
