using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class AdaptationTests
{
    private class TestCreature : Creature
    {
        public TestCreature(Vector2 position, Genome genome) : base(position, genome, CreatureType.Herbivore)
        {
            Species = "TestDummy";
        }

        protected override Creature CreateChild(Vector2 position, Genome genome, Random rng)
        {
            return new TestCreature(position, genome);
        }
        
        // Expose ConsumeEnergy for testing
        public void TestConsumeEnergy(float dt) => ConsumeEnergy(dt);
    }

    [Fact]
    public void Creature_DesertAdaptation_ReducesEnergyDrainAndSpeedPenalty()
    {
        var world = new World(10, 10, 42);
        // Set all tiles to Desert to ensure creature is on desert
        for (int x = 0; x < world.Width; x++)
        {
            for (int y = 0; y < world.Height; y++)
            {
                world.Tiles[x, y].Biome = BiomeType.Desert;
            }
        }

        var unadaptedGenome = new Genome
        {
            Speed = 1f,
            Size = 1f,
            Metabolism = 1f,
            DesertAdaptation = 0.0f // No adaptation
        };

        var adaptedGenome = new Genome
        {
            Speed = 1f,
            Size = 1f,
            Metabolism = 1f,
            DesertAdaptation = 1.0f // Full adaptation
        };

        var ecosystem = new Ecosystem(10, 10, 42);
        var unadaptedCreature = new TestCreature(new Vector2(32, 32), unadaptedGenome);
        var adaptedCreature = new TestCreature(new Vector2(32, 32), adaptedGenome);

        // Update once to apply environmental multipliers
                unadaptedCreature.Update(world, ecosystem, 1.0f);
        adaptedCreature.Update(world, ecosystem, 1.0f);

        // Assert adapted has higher speed than unadapted
        Assert.True(adaptedCreature.Speed > unadaptedCreature.Speed);

        // Assert adapted has lower energy multiplier (less drain) than unadapted
        Assert.True(adaptedCreature.CurrentEnergyMultiplier < unadaptedCreature.CurrentEnergyMultiplier);
        
        // Exact expected values:
        // Unadapted energy multiplier: 1.0 + (1.0 - 0.0) * 1.0 = 2.0
        // Adapted energy multiplier: 1.0 + (1.0 - 1.0) * 1.0 = 1.0
        Assert.True(adaptedCreature.CurrentEnergyMultiplier < unadaptedCreature.CurrentEnergyMultiplier);
        Assert.Equal(1.0f, adaptedCreature.CurrentEnergyMultiplier);

        // Unadapted speed multiplier: 0.6 + 0.0 * 0.4 = 0.6
        // Adapted speed multiplier: 0.6 + 1.0 * 0.4 = 1.0
        Assert.Equal(0.6f, unadaptedCreature.CurrentSpeedMultiplier);
        Assert.Equal(1.0f, adaptedCreature.CurrentSpeedMultiplier);
    }

    [Fact]
    public void Genome_Reproduce_MutatesAdaptationFieldsCorrectly()
    {
        var rng = new Random(12345);
        var p1 = Genome.Random(rng);
        var p2 = Genome.Random(rng);

        // Force a reproduction and verify child genes are bounded between 0 and 1
        for (int i = 0; i < 100; i++)
        {
            var child = Genome.Reproduce(p1, p2, rng);
            Assert.InRange(child.DesertAdaptation, 0f, 1f);
            Assert.InRange(child.ColdAdaptation, 0f, 1f);
            Assert.InRange(child.ForestAdaptation, 0f, 1f);
            Assert.InRange(child.WaterAdaptation, 0f, 1f);
            Assert.InRange(child.MutationRate, 0.01f, 0.2f);
        }
    }

    [Fact]
    public void Genome_Reproduce_InheritsOneAlleleFromEachParent()
    {
        var first = new Genome
        {
            Speed = 0.51f, Size = 0.61f, Metabolism = 0.71f, VisionRange = 1.1f,
            MutationRate = 0f, DesertAdaptation = 0.11f, ColdAdaptation = 0.21f,
            ForestAdaptation = 0.31f, WaterAdaptation = 0.41f, Color = Color.Red
        };
        var second = new Genome
        {
            Speed = 1.51f, Size = 1.61f, Metabolism = 1.71f, VisionRange = 9.1f,
            MutationRate = 0f, DesertAdaptation = 0.91f, ColdAdaptation = 0.81f,
            ForestAdaptation = 0.72f, WaterAdaptation = 0.62f, Color = Color.Blue
        };

        Genome child = Genome.Reproduce(first, second, new Random(7));
        Assert.Equal(first.Speed, child.Genetics.Speed.AlleleA.Value);
        Assert.Equal(second.Speed, child.Genetics.Speed.AlleleB.Value);
        Assert.Equal(first.Size, child.Genetics.Size.AlleleA.Value);
        Assert.Equal(second.Size, child.Genetics.Size.AlleleB.Value);
        Assert.InRange(child.Speed, first.Speed, second.Speed);
        Assert.InRange(child.Size, first.Size, second.Size);
    }

    [Fact]
    public void Creatures_CanEvolveIntoDifferentSpecies_WhenGenomeChanges()
    {
        var rng = new Random(42);

        // A standard herbivore genome that matches Kangaroo conditions:
        // DesertAdaptation >= 0.45f, Speed >= 1.2f, Size >= 1.0f
        var kangarooGenome = new Genome
        {
            DesertAdaptation = 0.6f,
            Speed = 1.3f,
            Size = 1.1f
        };

        string evolved = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Herbivore, kangarooGenome, "Rabbit", rng);
        Assert.Equal("Kangaroo", evolved);

        // A standard carnivore genome that matches Cheetah conditions:
        // Speed >= 1.4f, and low forest adaptation (or high desert)
        var cheetahGenome = new Genome
        {
            Speed = 1.5f,
            DesertAdaptation = 0.5f,
            ForestAdaptation = 0.2f
        };

        string evolvedCarnivore = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Carnivore, cheetahGenome, "Fox", rng);
        Assert.Equal("Cheetah", evolvedCarnivore);
    }
}
