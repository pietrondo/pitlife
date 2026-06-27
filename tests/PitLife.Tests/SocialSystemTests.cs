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

    [Theory]
    [InlineData("Rabbit", 10f)]
    [InlineData("Turtle", 40f)]
    [InlineData("Whale", 50f)]
    public void LifeStage_TransitionsAtSpeciesMaturityAge(string species, float maturityAge)
    {
        var creature = new Herbivore(
            new Vector2(10, 10),
            Genome.Random(new Random(1)),
            species);

        creature.GrowFor(maturityAge - 0.01f);
        Assert.Equal(LifeStage.Infant, creature.LifeStage);
        Assert.False(creature.IsAdult);

        creature.GrowFor(0.01f);
        Assert.Equal(LifeStage.Adult, creature.LifeStage);
        Assert.True(creature.IsAdult);
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
    public void ReproduceWith_UnassignedGender_ReturnsNull()
    {
        var genome = Genome.Random(new Random(1));
        var unassigned = new Herbivore(new Vector2(10, 10), genome, "Deer")
        { Energy = 1000f, Gender = Gender.None };
        var female = new Herbivore(new Vector2(20, 20), genome, "Deer")
        { Energy = 1000f, Gender = Gender.Female };
        unassigned.GrowFor(60f);
        female.GrowFor(60f);

        Assert.Null(unassigned.ReproduceWith(female, new Random(1)));
    }

    [Fact]
    public void ReproduceWith_DifferentSpecies_ReturnsNull()
    {
        var genome = Genome.Random(new Random(1));
        var deer = new Herbivore(new Vector2(10, 10), genome, "Deer")
        { Energy = 1000f, Gender = Gender.Male };
        var rabbit = new Herbivore(new Vector2(20, 20), genome, "Rabbit")
        { Energy = 1000f, Gender = Gender.Female };
        deer.GrowFor(60f);
        rabbit.GrowFor(60f);

        Assert.Null(deer.ReproduceWith(rabbit, new Random(1)));
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
        var mate = eco.Spatial.FindNearestMate(m);
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
        Assert.Null(eco.Spatial.FindNearestMate(m));
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

    [Fact]
    public void DetermineEvolvedSpecies_TerrestrialMammalToWater_EvolvesToMarineMammal()
    {
        var rng = new Random(42);

        // Herbivore land mammal (e.g. Rabbit) with high water adaptation evolves to Dolphin/Whale/Manatee
        var g1 = new Genome { WaterAdaptation = 0.8f, Speed = 1.3f, Size = 1.0f };
        var evolved1 = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Herbivore, g1, "Rabbit", rng);
        Assert.Equal("Dolphin", evolved1);

        var g2 = new Genome { WaterAdaptation = 0.8f, Speed = 1.0f, Size = 1.4f };
        var evolved2 = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Herbivore, g2, "Rabbit", rng);
        Assert.Equal("Whale", evolved2);

        // Carnivore land mammal (e.g. Wolf) with high water adaptation evolves to Orca/Seal/SeaLion
        var g3 = new Genome { WaterAdaptation = 0.8f, Size = 1.3f };
        var evolved3 = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Carnivore, g3, "Wolf", rng);
        Assert.Equal("Orca", evolved3);

        // Omnivore land mammal (e.g. Bear) with high water adaptation evolves to Hippo/Walrus/Otter
        var g4 = new Genome { WaterAdaptation = 0.8f, Size = 1.5f };
        var evolved4 = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Omnivore, g4, "Bear", rng);
        Assert.Equal("Hippopotamus", evolved4);

        // Fish stays fish
        var evolvedFish = SpeciesRegistry.DetermineEvolvedSpecies(CreatureType.Herbivore, g1, "Tuna", rng);
        Assert.True(evolvedFish == "Tuna" || evolvedFish == "Salmon");
    }

    [Fact]
    public void BaseBehavior_SolitaryCarnivores_FightAndLoseEnergy()
    {
        var eco = new Ecosystem(200, 200, 42);

        // Create two solitary carnivores (Tigers) extremely close
        var g = new Genome { Size = 1.0f, Speed = 1.0f, VisionRange = 3.0f }; // VisionPixels = 96
        var t1 = new Carnivore(new Vector2(100, 100), g) { Species = "Tiger", Energy = 50f };
        var t2 = new Carnivore(new Vector2(105, 100), g) { Species = "Tiger", Energy = 50f };

        eco.AddCreature(t1);
        eco.AddCreature(t2);
        eco.FlushPending();

        // Run tick to update spatial grid and execute base behavior
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        // Solitary stress + combat reduces energy significantly below starting value
        Assert.True(t1.Energy < 49f);
        Assert.True(t2.Energy < 49f);
    }

    [Fact]
    public void ApplySolitaryBehavior_WeakAnimalsFlee()
    {
        var eco = new Ecosystem(200, 200, 42);

        var g = new Genome { Size = 1.0f, Speed = 10.0f, VisionRange = 3.0f };
        var t1 = new Carnivore(new Vector2(100, 100), g) { Species = "Tiger", Energy = 5f };
        var t2 = new Carnivore(new Vector2(105, 100), g) { Species = "Tiger", Energy = 5f };

        // Grow to adult because babies might not execute solitary combat logic.
        t1.GrowFor(50f);
        t2.GrowFor(50f);

        eco.AddCreature(t1);
        eco.AddCreature(t2);
        eco.FlushPending();

        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        // If they fought, energy drops by >1f. So < 4f.
        // If they flee, energy drops by ~0.3f. So energy > 4f.
        Assert.True(t1.Energy > 4f, $"t1 Energy: {t1.Energy}");
        Assert.True(t2.Energy > 4f, $"t2 Energy: {t2.Energy}");
    }
}

