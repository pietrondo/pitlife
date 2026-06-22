using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class SubspeciesTests
{
    [Fact]
    public void Creature_HasSubspeciesProperty()
    {
        var rng = new System.Random(42);
        var herbivore = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        Assert.Equal("", herbivore.Subspecies);

        herbivore.Subspecies = "arcticus";
        Assert.Equal("arcticus", herbivore.Subspecies);
    }

    [Fact]
    public void CanMateWithSubspecies_SameSpecies_Allowed()
    {
        var rng = new System.Random(42);
        var male = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        male.Gender = Gender.Male;
        male.GrowFor(30f);
        male.Subspecies = "arcticus";

        var female = new Herbivore(new Vector2(120, 100), Genome.Random(rng), "Rabbit");
        female.Gender = Gender.Female;
        female.GrowFor(30f);
        female.Subspecies = "vulgaris";

        Assert.True(male.CanMateWithSubspecies(female));
        Assert.True(male.CanMateWith(female));
    }

    [Fact]
    public void CanMateWith_DifferentSpecies_Denied()
    {
        var rng = new System.Random(42);
        var rabbit = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        rabbit.Gender = Gender.Male;
        rabbit.GrowFor(30f);

        var deer = new Herbivore(new Vector2(120, 100), Genome.Random(rng), "Deer");
        deer.Gender = Gender.Female;
        deer.GrowFor(30f);

        Assert.False(rabbit.CanMateWith(deer));
    }

    [Fact]
    public void Metrics_TracksSubspecies()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var rng = new System.Random(42);

        var r1 = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        r1.Gender = Gender.Male;
        r1.Subspecies = "arcticus";
        ecosystem.AddCreature(r1);

        var r2 = new Herbivore(new Vector2(120, 100), Genome.Random(rng), "Rabbit");
        r2.Gender = Gender.Female;
        r2.Subspecies = "vulgaris";
        ecosystem.AddCreature(r2);

        var r3 = new Herbivore(new Vector2(140, 100), Genome.Random(rng), "Rabbit");
        r3.Gender = Gender.Female;
        r3.Subspecies = "arcticus";
        ecosystem.AddCreature(r3);

        ecosystem.FlushPending();
        ecosystem.UpdateStats();

        Assert.Equal(2, ecosystem.Metrics.TotalSubspecies);
        Assert.Contains("Rabbit/arcticus", ecosystem.Metrics.SubspeciesCounts.Keys);
        Assert.Contains("Rabbit/vulgaris", ecosystem.Metrics.SubspeciesCounts.Keys);
        Assert.Equal(2, ecosystem.Metrics.SubspeciesCounts["Rabbit/arcticus"]);
    }

    [Fact]
    public void Subspecies_NotCountedWhenEmpty()
    {
        var ecosystem = new Ecosystem(16, 12, 42);
        var rng = new System.Random(42);

        var r1 = new Herbivore(new Vector2(100, 100), Genome.Random(rng), "Rabbit");
        r1.Gender = Gender.Male;
        ecosystem.AddCreature(r1);

        ecosystem.FlushPending();
        ecosystem.UpdateStats();

        Assert.Equal(0, ecosystem.Metrics.TotalSubspecies);
    }
}
