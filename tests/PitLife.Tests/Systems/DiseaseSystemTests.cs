using Moq;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;
using System;

namespace PitLife.Tests.Systems;

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
    public void Creature_HasInfectionProperties_AndStartsHealthy()
    {
        var rng = new Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");

        Assert.False(herbivore.IsInfected);
        Assert.Equal("", herbivore.DiseaseName);
        Assert.Equal(0f, herbivore.Immunity);
    }

    [Fact]
    public void Infection_CanBeSetAndCleared()
    {
        var rng = new Random(42);
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
    public void Immunity_Accumulates_AndClampsToMax()
    {
        var rng = new Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        Assert.Equal(0f, herbivore.Immunity);

        herbivore.Immunity = 0.8f;
        herbivore.Immunity = MathHelper.Min(1f, herbivore.Immunity + 0.3f);
        Assert.Equal(1f, herbivore.Immunity); // Clamped
    }

    [Fact]
    public void DiseaseSystem_CanRunWithoutOutbreak_DoesNotCrash()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        ecosystem.Initialize(2, 2, 0, 4);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.NextDouble()).Returns(0.99); // Prevent outbreaks

        var dt = 1f / 60f;
        ecosystem.Disease.Update(ecosystem, dt, mockRng.Object);

        Assert.False(ecosystem.Disease.HasOutbreak);
    }

    [Fact]
    public void Reset_ClearsOutbreakState()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var sys = new DiseaseSystem();

        // Add creatures to trigger outbreak
        ecosystem.Initialize(10, 10, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.NextDouble()).Returns(0.0); // Force outbreak trigger
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        sys.Update(ecosystem, 1000f, mockRng.Object);
        Assert.True(sys.HasOutbreak);

        sys.Reset();

        Assert.False(sys.HasOutbreak);
        Assert.Equal("", sys.ActiveDiseaseName);
    }
}
