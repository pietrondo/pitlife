using System;
using System.Reflection;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests.Entities;

public class EvolutionRulesTests
{
    private class FakeRandom : Random
    {
        private readonly int _fixedValue;
        public FakeRandom(int fixedValue) { _fixedValue = fixedValue; }
        public override int Next(int maxValue) => _fixedValue;
        public override int Next(int minValue, int maxValue) => _fixedValue;
    }

    private static string? InvokeDetermineHerbivoreEvolution(Genome genome, bool isLandMammal, Random rng)
    {
        var method = typeof(EvolutionRules).GetMethod("DetermineHerbivoreEvolution", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, new object[] { genome, isLandMammal, rng });
    }

    private static string? InvokeDetermineCarnivoreEvolution(Genome genome, bool isLandMammal, Random rng)
    {
        var method = typeof(EvolutionRules).GetMethod("DetermineCarnivoreEvolution", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, new object[] { genome, isLandMammal, rng });
    }

    private static string? InvokeDetermineOmnivoreEvolution(Genome genome, bool isLandMammal)
    {
        var method = typeof(EvolutionRules).GetMethod("DetermineOmnivoreEvolution", BindingFlags.NonPublic | BindingFlags.Static);
        return (string?)method?.Invoke(null, new object[] { genome, isLandMammal });
    }

    [Fact]
    public void ZeroGenome_ReturnsExpectedSpecies()
    {
        var genome = new Genome(); // All zero
        var rng = new FakeRandom(0);

        Assert.Null(InvokeDetermineHerbivoreEvolution(genome, false, rng));
        Assert.Equal("Fox", InvokeDetermineCarnivoreEvolution(genome, false, rng));
        Assert.Equal("Raccoon", InvokeDetermineOmnivoreEvolution(genome, false));
    }

    [Fact]
    public void DetermineEvolvedSpecies_NullGenome_IsHandledCorrectly()
    {
        // Actually, Genome is a struct, so it cannot be null in standard code.
        // But if someone tried to reflectively invoke with null, we just test that DetermineEvolvedSpecies handles struct properly.
        // We will call the public entrypoint with the default struct.
        var genome = default(Genome);
        var rng = new FakeRandom(0);

        Assert.Equal("Unknown", EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Unknown", rng));
    }

    [Theory]
    // 1. WaterAdaptation >= 0.65, isLandMammal = true
    [InlineData(0.65f, 1.2f, 0f, 0f, 0f, 0f, true, "Dolphin")]
    [InlineData(0.65f, 1.19f, 1.2f, 0f, 0f, 0f, true, "Whale")]
    [InlineData(0.65f, 1.19f, 1.19f, 0f, 0f, 0f, true, "Manatee")]
    // 2. DesertAdaptation >= 0.45 && Speed >= 1.2 && Size >= 1.0
    [InlineData(0.64f, 1.2f, 1.0f, 0.45f, 0f, 0f, false, "Kangaroo")]
    // 3. DesertAdaptation >= 0.65 && Size <= 0.8
    [InlineData(0.64f, 1.19f, 0.8f, 0.65f, 0f, 0f, false, "Lizard")]
    // 4. DesertAdaptation >= 0.55 && Speed >= 1.1 (fails previous)
    [InlineData(0.64f, 1.1f, 0.9f, 0.55f, 0f, 0f, false, "Gazelle")]
    // 5. Size <= 0.75 && Speed >= 1.1 (fails previous)
    [InlineData(0.64f, 1.1f, 0.75f, 0.54f, 0f, 0f, false, "Rabbit")]
    // 6. ColdAdaptation >= 0.55 && Size <= 1.1 (fails previous)
    [InlineData(0.64f, 1.09f, 1.1f, 0.54f, 0.55f, 0f, false, "Goat")]
    // 7. Size >= 1.25 && Speed >= 1.1 (fails previous)
    [InlineData(0.64f, 1.1f, 1.25f, 0.54f, 0.54f, 0f, false, "Horse")]
    // 8. Size >= 1.0 && ForestAdaptation >= 0.5 (fails previous)
    [InlineData(0.64f, 1.09f, 1.0f, 0.54f, 0.54f, 0.5f, false, "Deer")]
    // 9. Size >= 0.8 && Size <= 1.2 && Speed <= 0.9 (fails previous)
    [InlineData(0.64f, 0.9f, 0.8f, 0.54f, 0.54f, 0.49f, false, "Sheep")]
    [InlineData(0.64f, 0.9f, 1.2f, 0.54f, 0.54f, 0.49f, false, "Sheep")]
    // 10. Fallback (fails all)
    [InlineData(0.64f, 0.91f, 0.9f, 0.54f, 0.54f, 0.49f, false, null)]
    public void DetermineHerbivoreEvolution_ReturnsCorrectSpecies(
        float water, float speed, float size, float desert, float cold, float forest, bool isLandMammal, string? expected)
    {
        var genome = new Genome
        {
            WaterAdaptation = water,
            Speed = speed,
            Size = size,
            DesertAdaptation = desert,
            ColdAdaptation = cold,
            ForestAdaptation = forest
        };
        var rng = new FakeRandom(0);

        string? result = InvokeDetermineHerbivoreEvolution(genome, isLandMammal, rng);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineHerbivoreEvolution_NotLandMammal_TunaOrSalmon()
    {
        var genome = new Genome { WaterAdaptation = 0.65f };
        var rng0 = new FakeRandom(0);
        var rng1 = new FakeRandom(1);

        Assert.Equal("Tuna", InvokeDetermineHerbivoreEvolution(genome, false, rng0));
        Assert.Equal("Salmon", InvokeDetermineHerbivoreEvolution(genome, false, rng1));
    }

    [Theory]
    // 1. WaterAdaptation >= 0.65, isLandMammal = true
    [InlineData(0.65f, 0f, 1.2f, 0f, 0f, 0f, true, "Orca")]
    [InlineData(0.65f, 0f, 1.19f, 0f, 0.5f, 0f, true, "Seal")]
    [InlineData(0.65f, 0f, 1.19f, 0f, 0.49f, 0f, true, "SeaLion")]
    // 2. Speed >= 1.4 && (DesertAdaptation >= 0.4 || ForestAdaptation <= 0.4)
    [InlineData(0.64f, 1.4f, 0f, 0.4f, 0f, 0.5f, false, "Cheetah")] // Desert
    [InlineData(0.64f, 1.4f, 0f, 0.39f, 0f, 0.4f, false, "Cheetah")] // Forest
    // 3. WaterAdaptation >= 0.45 && DesertAdaptation >= 0.45
    [InlineData(0.45f, 1.39f, 0f, 0.45f, 0f, 0.41f, false, "Crocodile")]
    // 4. DesertAdaptation >= 0.45 && Speed >= 1.1
    [InlineData(0.44f, 1.1f, 0f, 0.45f, 0f, 0.41f, false, "Lion")]
    // 5. ColdAdaptation >= 0.45 && Size >= 0.9
    [InlineData(0.44f, 1.09f, 0.9f, 0.44f, 0.45f, 0.41f, false, "Wolf")]
    // 6. ColdAdaptation >= 0.55 && Size <= 1.0
    [InlineData(0.44f, 1.09f, 0.89f, 0.44f, 0.55f, 0.41f, false, "Lynx")]
    // 7. ForestAdaptation >= 0.65 && Size >= 1.2
    [InlineData(0.44f, 1.09f, 1.2f, 0.44f, 0.44f, 0.65f, false, "Tiger")]
    // 8. ForestAdaptation >= 0.5 && Size < 1.2
    [InlineData(0.44f, 1.09f, 1.19f, 0.44f, 0.44f, 0.5f, false, "Leopard")]
    // 9. Size <= 0.8
    [InlineData(0.44f, 1.09f, 0.8f, 0.44f, 0.44f, 0.49f, false, "Fox")]
    // 10. Fallback (Size is 0.81 so fails Size <= 0.8)
    [InlineData(0.44f, 1.09f, 0.81f, 0.44f, 0.44f, 0.49f, false, null)]
    public void DetermineCarnivoreEvolution_ReturnsCorrectSpecies(
        float water, float speed, float size, float desert, float cold, float forest, bool isLandMammal, string? expected)
    {
        var genome = new Genome
        {
            WaterAdaptation = water,
            Speed = speed,
            Size = size,
            DesertAdaptation = desert,
            ColdAdaptation = cold,
            ForestAdaptation = forest
        };
        var rng = new FakeRandom(0);

        string? result = InvokeDetermineCarnivoreEvolution(genome, isLandMammal, rng);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineCarnivoreEvolution_NotLandMammal_SharkOrPiranha()
    {
        var genome = new Genome { WaterAdaptation = 0.65f };
        var rng0 = new FakeRandom(0);
        var rng1 = new FakeRandom(1);

        Assert.Equal("Shark", InvokeDetermineCarnivoreEvolution(genome, false, rng0));
        Assert.Equal("Piranha", InvokeDetermineCarnivoreEvolution(genome, false, rng1));
    }

    [Theory]
    // 1. WaterAdaptation >= 0.65, isLandMammal = true
    [InlineData(0.65f, 1.3f, 0f, 0f, true, "Hippopotamus")]
    [InlineData(0.65f, 1.29f, 0.5f, 0f, true, "Walrus")]
    [InlineData(0.65f, 1.29f, 0.49f, 0f, true, "Otter")]
    // WaterAdaptation >= 0.65, isLandMammal = false
    [InlineData(0.65f, 1.3f, 0f, 0f, false, "Jellyfish")]
    // 2. WaterAdaptation >= 0.4 && ForestAdaptation >= 0.4
    [InlineData(0.4f, 0f, 0f, 0.4f, false, "Frog")]
    // 3. Size >= 1.3
    [InlineData(0.39f, 1.3f, 0f, 0.39f, false, "Bear")]
    // 4. Size >= 0.9 && Size < 1.3
    [InlineData(0.39f, 0.9f, 0f, 0.39f, false, "Boar")]
    [InlineData(0.39f, 1.29f, 0f, 0.39f, false, "Boar")]
    // 5. Size < 0.9
    [InlineData(0.39f, 0.89f, 0f, 0.39f, false, "Raccoon")]
    // 6. Fallback - actually unreachable due to Size < 0.9, but if logic changes, we can have a fallback.
    // Since Size is always >= 0.9 or < 0.9 or >= 1.3, it always hits Bear, Boar or Raccoon unless it's NaN.
    [InlineData(0f, float.NaN, 0f, 0f, false, null)]
    public void DetermineOmnivoreEvolution_ReturnsCorrectSpecies(
        float water, float size, float cold, float forest, bool isLandMammal, string? expected)
    {
        var genome = new Genome
        {
            WaterAdaptation = water,
            Size = size,
            ColdAdaptation = cold,
            ForestAdaptation = forest
        };

        string? result = InvokeDetermineOmnivoreEvolution(genome, isLandMammal);
        Assert.Equal(expected, result);
    }
}
