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

    [Fact]
    public void Outbreak_Triggers_AndInfectsPatientZero()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var sys = new DiseaseSystem();
        ecosystem.Initialize(10, 10, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        sys.Update(ecosystem, 1000f, mockRng.Object);

        Assert.True(sys.HasOutbreak);
        var infectedCount = 0;
        foreach (var c in ecosystem.Creatures)
        {
            if (c.IsInfected) infectedCount++;
        }
        Assert.Equal(1, infectedCount);
    }

    [Fact]
    public void ProcessCarrier_DrainsEnergy_AndCanCauseDeath()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var sys = new DiseaseSystem();
        ecosystem.Initialize(10, 10, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        sys.Update(ecosystem, 1000f, mockRng.Object);

        var infected = ecosystem.Creatures.Find(c => c.IsInfected);
        Assert.NotNull(infected);

        infected.Energy = 1f;
        sys.Update(ecosystem, 1f, mockRng.Object);

        Assert.False(infected.IsAlive);
        Assert.Equal(DeathCause.Starvation, infected.DeathCause);
    }

    [Fact]
    public void ProcessCarrier_Recovers_WhenTimerExpires_AndGainsImmunity()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var sys = new DiseaseSystem();
        ecosystem.Initialize(10, 10, 0, 5);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        sys.Update(ecosystem, 1000f, mockRng.Object);

        var infected = ecosystem.Creatures.Find(c => c.IsInfected);
        Assert.NotNull(infected);

        var initialImmunity = infected.Immunity;
        infected.Energy = 1000f;

        sys.Update(ecosystem, 100f, mockRng.Object);

        Assert.False(infected.IsInfected);
        Assert.True(infected.Immunity > initialImmunity);
    }

    [Fact]
    public void ProcessCarrier_TransmitsDiseaseToNeighbors()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var sys = new DiseaseSystem();

        var rng = new Random(42);
        var c1 = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        var c2 = new Herbivore(new Vector2(102, 100), Genome.Random(rng), "Rabbit");

        ecosystem.Creatures.Add(c1);
        ecosystem.Creatures.Add(c2);
        for(int i=0; i<10; i++) ecosystem.Creatures.Add(new Carnivore(new Vector2(999,999), Genome.Random(rng), "Wolf"));

        ecosystem.Spatial.Rebuild(ecosystem.Creatures);

        var mockRng = new Mock<Random>();
        mockRng.Setup(r => r.Next(It.IsAny<int>())).Returns(0);

        sys.Update(ecosystem, 1000f, mockRng.Object);

        Assert.True(c1.IsInfected);
        Assert.False(c2.IsInfected);

        c1.Energy = 1000f;
        c2.Energy = 1000f;

        mockRng.Setup(r => r.NextDouble()).Returns(0.0);

        sys.Update(ecosystem, 1f, mockRng.Object);

        Assert.True(c2.IsInfected);
    }
}
