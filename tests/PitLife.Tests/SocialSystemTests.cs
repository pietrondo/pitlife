using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SocialSystemTests
{
    [Fact]
    public void Creature_IsAdult_False_Below30Seconds()
    {
        var c = new Herbivore(new Vector2(10, 10), Genome.Random(new Random(1)));
        c.GrowFor(29f);
        Assert.False(c.IsAdult);
        Assert.True(c.IsBaby);
    }

    [Fact]
    public void Creature_IsAdult_True_At30Seconds()
    {
        var c = new Herbivore(new Vector2(10, 10), Genome.Random(new Random(1)));
        c.GrowFor(30f);
        Assert.True(c.IsAdult);
        Assert.False(c.IsBaby);
    }

    [Fact]
    public void ReproduceWith_SameGender_ReturnsNull()
    {
        var g = Genome.Random(new Random(1));
        var a = new Herbivore(new Vector2(10, 10), g) { Energy = 1000f, Gender = Gender.Male };
        var b = new Herbivore(new Vector2(20, 20), g) { Energy = 1000f, Gender = Gender.Male };
        Assert.Null(a.ReproduceWith(b, new Random(1)));
    }

    [Fact]
    public void ReproduceWith_Baby_ReturnsNull()
    {
        var g = Genome.Random(new Random(1));
        var a = new Herbivore(new Vector2(10, 10), g) { Energy = 1000f, Gender = Gender.Male };
        var b = new Herbivore(new Vector2(20, 20), g) { Energy = 1000f, Gender = Gender.Female };
        a.GrowFor(10f);
        b.GrowFor(60f);
        Assert.Null(a.ReproduceWith(b, new Random(1)));
    }

    [Fact]
    public void IsPackAnimal_Deer_ReturnsTrue()
    {
        Assert.True(Ecosystem.IsPackAnimal("Deer"));
    }

    [Fact]
    public void IsSolitary_Tiger_ReturnsTrue()
    {
        Assert.True(Ecosystem.IsSolitary("Tiger"));
    }

    [Fact]
    public void IsPackAnimal_Tortoise_ReturnsFalse()
    {
        Assert.False(Ecosystem.IsPackAnimal("Tortoise"));
    }

    [Fact]
    public void Spawn_AssignsGender_ForNonPlantCreatures()
    {
        var eco = new Ecosystem(32, 24, 7);
        eco.Initialize(h: 50, c: 0, o: 0, p: 0);
        Assert.All(eco.Creatures.Where(c => c.CreatureType != CreatureType.Plant),
                   c => Assert.True(c.Gender == Gender.Male || c.Gender == Gender.Female));
    }

    [Fact]
    public void AdultMale_FindsAdultFemale_SameSpecies()
    {
        var eco = new Ecosystem(64, 48, 7);
        var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Male, Energy = 1000f };
        var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Female, Energy = 1000f };
        m.GrowFor(60f);
        f.GrowFor(60f);
        eco.AddCreature(m);
        eco.AddCreature(f);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        var mate = eco.FindNearestMate(m);
        Assert.Same(f, mate);
    }

    [Fact]
    public void BabyMale_FindsNoMate()
    {
        var eco = new Ecosystem(64, 48, 7);
        var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Male, Energy = 1000f };
        var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Female, Energy = 1000f };
        m.GrowFor(10f);
        f.GrowFor(60f);
        eco.AddCreature(m);
        eco.AddCreature(f);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        Assert.Null(eco.FindNearestMate(m));
    }

    [Fact]
    public void Integration_AdultPairReproduces_BabyHasNoGenderReproduction()
    {
        var eco = new Ecosystem(64, 48, 42);
        var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Male, Energy = 1000f };
        var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
        { Gender = Gender.Female, Energy = 1000f };
        m.GrowFor(60f);
        f.GrowFor(60f);
        eco.AddCreature(m);
        eco.AddCreature(f);

        var child = m.ReproduceWith(f, new Random(1));
        Assert.NotNull(child);
        Assert.True(child.IsBaby);
        Assert.True(child.Gender == Gender.Male || child.Gender == Gender.Female);

        child.Energy = 1000f;
        var other = new Herbivore(new Vector2(120, 100), Genome.Random(new Random(2)))
        { Gender = child.Gender == Gender.Male ? Gender.Female : Gender.Male, Energy = 1000f };
        other.GrowFor(60f);
        Assert.Null(child.ReproduceWith(other, new Random(1)));
    }
}
