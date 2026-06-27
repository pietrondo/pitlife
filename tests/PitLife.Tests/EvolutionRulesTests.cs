using System;
using Xunit;
using Moq;
using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.Tests;

public class EvolutionRulesTests
{
    private delegate void GenomeModifier(ref Genome genome);

    private static Genome CreateGenomeRef(GenomeModifier? modifier = null)
    {
        var genome = Genome.Random(new Random(0));

        // Overwrite fields to safe defaults so they don't unexpectedly trigger other branches
        genome.Speed = 1f;
        genome.Size = 1f;
        genome.Metabolism = 1f;
        genome.VisionRange = 5f;
        genome.MutationRate = 0.05f;
        genome.DesertAdaptation = 0f;
        genome.ColdAdaptation = 0f;
        genome.ForestAdaptation = 0f;
        genome.WaterAdaptation = 0f;

        if (modifier != null)
        {
            modifier(ref genome);
        }

        return genome;
    }

    private static Random CreateDeterministicRandom(int nextResult = 0)
    {
        var mock = new Mock<Random>();
        mock.Setup(r => r.Next(It.IsAny<int>())).Returns(nextResult);
        return mock.Object;
    }

    [Theory]
    [InlineData("Rabbit", "Dolphin")]
    [InlineData("Deer", "Dolphin")]
    [InlineData("Bear", "Dolphin")]
    public void DetermineEvolvedSpecies_Herbivore_WaterAdaptation_LandMammal_Dolphin(string startSpecies, string expected)
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Herbivore.WaterAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.DolphinSpeed + 0.1f;
        });

        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, startSpecies, new Random(0));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Rabbit", "Whale")]
    public void DetermineEvolvedSpecies_Herbivore_WaterAdaptation_LandMammal_Whale(string startSpecies, string expected)
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Herbivore.WaterAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.DolphinSpeed - 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.WhaleSize + 0.1f;
        });

        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, startSpecies, new Random(0));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Rabbit", "Manatee")]
    public void DetermineEvolvedSpecies_Herbivore_WaterAdaptation_LandMammal_Manatee(string startSpecies, string expected)
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Herbivore.WaterAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.DolphinSpeed - 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.WhaleSize - 0.1f;
        });

        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, startSpecies, new Random(0));
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(0, "Tuna")]
    [InlineData(1, "Salmon")]
    public void DetermineEvolvedSpecies_Herbivore_WaterAdaptation_NotLandMammal(int randomResult, string expected)
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Herbivore.WaterAdaptation + 0.1f;
        });
        var rng = CreateDeterministicRandom(randomResult);
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", rng);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Kangaroo()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.DesertAdaptation = EvolutionConfig.Data.Herbivore.KangarooDesertAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.KangarooSpeed + 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.KangarooSize + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Kangaroo", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Lizard()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.DesertAdaptation = EvolutionConfig.Data.Herbivore.LizardDesertAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.LizardSize - 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.KangarooSpeed - 0.1f; // Prevent Kangaroo
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Lizard", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Gazelle()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.DesertAdaptation = EvolutionConfig.Data.Herbivore.GazelleDesertAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.GazelleSpeed + 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.LizardSize + 0.1f; // Prevent Lizard
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Gazelle", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Rabbit()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Herbivore.RabbitSize - 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.RabbitSpeed + 0.1f;
            g.DesertAdaptation = 0f; // Prevent Desert animals
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Rabbit", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Goat()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.ColdAdaptation = EvolutionConfig.Data.Herbivore.GoatColdAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Herbivore.GoatSize - 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.RabbitSpeed - 0.1f; // Prevent Rabbit
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Goat", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Horse()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Herbivore.HorseSize + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.HorseSpeed + 0.1f;
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Horse", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Deer()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Herbivore.DeerSize + 0.1f;
            g.ForestAdaptation = EvolutionConfig.Data.Herbivore.DeerForestAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Herbivore.HorseSpeed - 0.1f; // Prevent Horse
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Deer", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_Sheep()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = (EvolutionConfig.Data.Herbivore.SheepSizeMin + EvolutionConfig.Data.Herbivore.SheepSizeMax) / 2f;
            g.Speed = EvolutionConfig.Data.Herbivore.SheepSpeed - 0.1f;
            g.ForestAdaptation = 0f;
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "Bird", new Random(0));
        Assert.Equal("Sheep", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Herbivore_NoEvolution()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = 0f;
            g.DesertAdaptation = 0f;
            g.ColdAdaptation = 0f;
            g.ForestAdaptation = 0f;
            g.Size = (EvolutionConfig.Data.Herbivore.SheepSizeMin + EvolutionConfig.Data.Herbivore.SheepSizeMax) / 2f;
            g.Speed = EvolutionConfig.Data.Herbivore.SheepSpeed + 0.1f; // Too fast for Sheep
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Herbivore, genome, "BaseHerbivore", new Random(0));
        Assert.Equal("BaseHerbivore", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Orca()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Carnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.OrcaSize + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Wolf", new Random(0));
        Assert.Equal("Orca", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Seal()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Carnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.OrcaSize - 0.1f;
            g.ColdAdaptation = EvolutionConfig.Data.Carnivore.SealColdAdaptation + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Wolf", new Random(0));
        Assert.Equal("Seal", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_SeaLion()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Carnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.OrcaSize - 0.1f;
            g.ColdAdaptation = EvolutionConfig.Data.Carnivore.SealColdAdaptation - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Wolf", new Random(0));
        Assert.Equal("SeaLion", result);
    }

    [Theory]
    [InlineData(0, "Shark")]
    [InlineData(1, "Piranha")]
    public void DetermineEvolvedSpecies_Carnivore_NotLandMammal(int randomResult, string expected)
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Carnivore.WaterAdaptation + 0.1f;
        });
        var rng = CreateDeterministicRandom(randomResult);
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", rng);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Cheetah_Desert()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Speed = EvolutionConfig.Data.Carnivore.CheetahSpeed + 0.1f;
            g.DesertAdaptation = EvolutionConfig.Data.Carnivore.CheetahDesertAdaptation + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Cheetah", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Cheetah_Forest()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Speed = EvolutionConfig.Data.Carnivore.CheetahSpeed + 0.1f;
            g.DesertAdaptation = 0f;
            g.ForestAdaptation = EvolutionConfig.Data.Carnivore.CheetahForestAdaptation - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Cheetah", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Crocodile()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Carnivore.CrocodileWaterAdaptation + 0.1f;
            g.DesertAdaptation = EvolutionConfig.Data.Carnivore.CrocodileDesertAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Carnivore.CheetahSpeed - 0.1f; // Prevent Cheetah
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Crocodile", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Lion()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.DesertAdaptation = EvolutionConfig.Data.Carnivore.LionDesertAdaptation + 0.1f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed + 0.1f;
            g.WaterAdaptation = 0f; // Prevent Crocodile
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Lion", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Wolf()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.ColdAdaptation = EvolutionConfig.Data.Carnivore.WolfColdAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.WolfSize + 0.1f;
            g.DesertAdaptation = 0f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f;
            g.WaterAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Wolf", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Lynx()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.ColdAdaptation = EvolutionConfig.Data.Carnivore.LynxColdAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.WolfSize - 0.1f; // Prevent Wolf which is checked first!
            g.DesertAdaptation = 0f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f;
            g.WaterAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Lynx", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Tiger()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.ForestAdaptation = EvolutionConfig.Data.Carnivore.TigerForestAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.TigerSize + 0.1f;
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
            g.WaterAdaptation = 0f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Tiger", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Leopard()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.ForestAdaptation = EvolutionConfig.Data.Carnivore.LeopardForestAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.LeopardSize - 0.1f;
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
            g.WaterAdaptation = 0f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Leopard", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_Fox()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Carnivore.FoxSize - 0.1f;
            g.ForestAdaptation = 0f;
            g.ColdAdaptation = 0f;
            g.DesertAdaptation = 0f;
            g.WaterAdaptation = 0f;
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "Bird", new Random(0));
        Assert.Equal("Fox", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Carnivore_NoEvolution()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = 0f;
            g.DesertAdaptation = 0f;
            g.ColdAdaptation = 0f;
            g.ForestAdaptation = EvolutionConfig.Data.Carnivore.LeopardForestAdaptation - 0.1f;
            g.Size = EvolutionConfig.Data.Carnivore.FoxSize + 0.05f; // not fox
            g.Speed = EvolutionConfig.Data.Carnivore.LionSpeed - 0.1f; // not cheetah or lion
        });

        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Carnivore, genome, "BaseCarnivore", new Random(0));
        Assert.Equal("BaseCarnivore", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Hippopotamus()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Omnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Omnivore.HippopotamusSize + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Boar", new Random(0));
        Assert.Equal("Hippopotamus", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Walrus()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Omnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Omnivore.HippopotamusSize - 0.1f;
            g.ColdAdaptation = EvolutionConfig.Data.Omnivore.WalrusColdAdaptation + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Boar", new Random(0));
        Assert.Equal("Walrus", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Otter()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Omnivore.WaterAdaptation + 0.1f;
            g.Size = EvolutionConfig.Data.Omnivore.HippopotamusSize - 0.1f;
            g.ColdAdaptation = EvolutionConfig.Data.Omnivore.WalrusColdAdaptation - 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Boar", new Random(0));
        Assert.Equal("Otter", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Jellyfish()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Omnivore.WaterAdaptation + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Bird", new Random(0));
        Assert.Equal("Jellyfish", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Frog()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = EvolutionConfig.Data.Omnivore.FrogWaterAdaptation + 0.1f;
            g.ForestAdaptation = EvolutionConfig.Data.Omnivore.FrogForestAdaptation + 0.1f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Bird", new Random(0));
        Assert.Equal("Frog", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Bear()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Omnivore.BearSize + 0.1f;
            g.WaterAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Bird", new Random(0));
        Assert.Equal("Bear", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Boar()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = (EvolutionConfig.Data.Omnivore.BoarSizeMin + EvolutionConfig.Data.Omnivore.BoarSizeMax) / 2f;
            g.WaterAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Bird", new Random(0));
        Assert.Equal("Boar", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_Raccoon()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.Size = EvolutionConfig.Data.Omnivore.RaccoonSize - 0.1f;
            g.WaterAdaptation = 0f;
        });
        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "Bird", new Random(0));
        Assert.Equal("Raccoon", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_Omnivore_NoEvolution()
    {
        var genome = CreateGenomeRef((ref Genome g) => {
            g.WaterAdaptation = 0f;
            g.ForestAdaptation = 0f;
            g.Size = float.NaN; // The only way to not match Bear, Boar, or Raccoon sizes!
        });

        string result = EvolutionRules.DetermineEvolvedSpecies(CreatureType.Omnivore, genome, "BaseOmnivore", new Random(0));
        Assert.Equal("BaseOmnivore", result);
    }

    [Fact]
    public void DetermineEvolvedSpecies_NullCreatureType()
    {
        var genome = CreateGenomeRef();
        string result = EvolutionRules.DetermineEvolvedSpecies((CreatureType)999, genome, "BaseSpecies", new Random(0));
        Assert.Equal("BaseSpecies", result);
    }
}
