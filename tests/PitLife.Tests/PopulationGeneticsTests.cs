using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class PopulationGeneticsTests
{
    private sealed class TestCreature : Creature
    {
        public TestCreature(Vector2 position, Genome genome, string species = "TestSpecies")
            : base(position, genome, CreatureType.Herbivore)
        {
            Species = species;
        }

        protected override Creature CreateChild(Vector2 position, Genome genome, Random rng) =>
            new TestCreature(position, genome, Species);
    }

    [Fact]
    public void DiploidLocus_ExpressesDominanceWeightedPhenotype()
    {
        var locus = new DiploidLocus(
            new GeneticAllele(2f, 0.8f),
            new GeneticAllele(1f, 0.2f));

        Assert.Equal(1.8f, locus.ExpressedValue, precision: 5);
    }

    [Fact]
    public void RandomAndRecombination_AreDeterministicForFixedSeed()
    {
        Genome firstA = Genome.Random(new Random(10));
        Genome secondA = Genome.Random(new Random(20));
        Genome childA = Genome.Reproduce(firstA, secondA, new Random(30));
        Genome firstB = Genome.Random(new Random(10));
        Genome secondB = Genome.Random(new Random(20));
        Genome childB = Genome.Reproduce(firstB, secondB, new Random(30));

        Assert.Equal(firstA.Genetics, firstB.Genetics);
        Assert.Equal(secondA.Genetics, secondB.Genetics);
        Assert.Equal(childA.Genetics, childB.Genetics);
        Assert.Equal(childA.Speed, childB.Speed);
        Assert.Equal(childA.Heterozygosity, childB.Heterozygosity);
    }

    [Fact]
    public void SiblingMating_ProducesExpectedInbreedingAndFitnessPenalty()
    {
        var ecosystem = new Ecosystem(32, 24, 8);
        var father = Adult(Gender.Male, Genome.Random(new Random(1)));
        var mother = Adult(Gender.Female, Genome.Random(new Random(2)));
        ecosystem.AddCreature(father);
        ecosystem.AddCreature(mother);
        ecosystem.FlushPending();

        TestCreature firstChild = Assert.IsType<TestCreature>(father.ReproduceWith(mother, new Random(3)));
        father.Energy = mother.Energy = 1000f;
        TestCreature secondChild = Assert.IsType<TestCreature>(father.ReproduceWith(mother, new Random(4)));
        ecosystem.AddCreature(firstChild);
        ecosystem.AddCreature(secondChild);
        ecosystem.FlushPending();

        firstChild.Gender = Gender.Male;
        secondChild.Gender = Gender.Female;
        firstChild.GrowFor(60f);
        secondChild.GrowFor(60f);
        firstChild.Energy = secondChild.Energy = 1000f;
        float unrelatedMaxEnergy = 50f * firstChild.Genome.Size;

        TestCreature inbredChild = Assert.IsType<TestCreature>(
            firstChild.ReproduceWith(secondChild, new Random(5)));

        Assert.Equal(0.25f, firstChild.RelatednessTo(secondChild), precision: 5);
        Assert.Equal(0.25f, inbredChild.InbreedingCoefficient, precision: 5);
        Assert.Equal(0.875f, inbredChild.GeneticFitness, precision: 5);
        Assert.True(inbredChild.MaxEnergy < 50f * inbredChild.Genome.Size);
        Assert.True(firstChild.MaxEnergy <= unrelatedMaxEnergy);
    }

    [Fact]
    public void PopulationMetrics_DetectMarkerDiversity()
    {
        Genome baseGenome = LegacyGenome();
        GeneticProfile uniformProfile = baseGenome.EnsureGeneticProfile();
        uniformProfile.MarkerHaplotypeA = 0;
        uniformProfile.MarkerHaplotypeB = 0;
        baseGenome.Genetics = uniformProfile;
        var uniform = new Creature[]
        {
            new TestCreature(Vector2.Zero, baseGenome),
            new TestCreature(Vector2.One, baseGenome)
        };

        Genome diverseGenome = baseGenome;
        GeneticProfile diverseProfile = uniformProfile;
        diverseProfile.MarkerHaplotypeA = ulong.MaxValue;
        diverseProfile.MarkerHaplotypeB = ulong.MaxValue;
        diverseGenome.Genetics = diverseProfile;
        var diverse = new Creature[]
        {
            new TestCreature(Vector2.Zero, baseGenome),
            new TestCreature(Vector2.One, diverseGenome)
        };

        PopulationGeneticMetrics uniformMetrics = PopulationGenetics.Calculate(uniform);
        PopulationGeneticMetrics diverseMetrics = PopulationGenetics.Calculate(diverse);

        Assert.Equal(0f, uniformMetrics.ExpectedMarkerHeterozygosity);
        Assert.Equal(64, diverseMetrics.PolymorphicMarkerCount);
        Assert.True(diverseMetrics.ExpectedMarkerHeterozygosity > uniformMetrics.ExpectedMarkerHeterozygosity);
    }

    private static TestCreature Adult(Gender gender, Genome genome)
    {
        var creature = new TestCreature(new Vector2(100, 100), genome)
        {
            Gender = gender,
            Energy = 1000f
        };
        creature.GrowFor(60f);
        return creature;
    }

    private static Genome LegacyGenome() => new()
    {
        Speed = 1f,
        Size = 1f,
        Metabolism = 1f,
        VisionRange = 5f,
        MutationRate = 0.05f,
        Color = Color.White
    };
}
