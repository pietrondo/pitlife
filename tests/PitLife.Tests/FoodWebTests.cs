using PitLife.Simulation;

namespace PitLife.Tests;

public class FoodWebTests
{
    [Fact]
    public void CanEat_HerbivoreCanEatPlants()
    {
        Assert.True(FoodWeb.CanEat(CreatureType.Herbivore, CreatureType.Plant, DietType.Herbivore));
    }

    [Fact]
    public void CanEat_HerbivoreCannotEatAnimals()
    {
        Assert.False(FoodWeb.CanEat(CreatureType.Herbivore, CreatureType.Herbivore, DietType.Herbivore));
        Assert.False(FoodWeb.CanEat(CreatureType.Herbivore, CreatureType.Carnivore, DietType.Herbivore));
    }

    [Fact]
    public void CanEat_CarnivoreCanEatHerbivores()
    {
        Assert.True(FoodWeb.CanEat(CreatureType.Carnivore, CreatureType.Herbivore, DietType.Carnivore));
        Assert.True(FoodWeb.CanEat(CreatureType.Carnivore, CreatureType.Omnivore, DietType.Carnivore));
    }

    [Fact]
    public void CanEat_CarnivoreCannotEatPlants()
    {
        Assert.False(FoodWeb.CanEat(CreatureType.Carnivore, CreatureType.Plant, DietType.Carnivore));
    }

    [Fact]
    public void CanEat_OmnivoreCanEatBoth()
    {
        Assert.True(FoodWeb.CanEat(CreatureType.Omnivore, CreatureType.Plant, DietType.Omnivore));
        Assert.True(FoodWeb.CanEat(CreatureType.Omnivore, CreatureType.Herbivore, DietType.Omnivore));
    }

    [Fact]
    public void TrophicLevel_PlantsAreLevel1()
    {
        Assert.Equal(1, FoodWeb.TrophicLevel(CreatureType.Plant));
    }

    [Fact]
    public void TrophicLevel_HerbivoresAreLevel2()
    {
        Assert.Equal(2, FoodWeb.TrophicLevel(CreatureType.Herbivore));
    }

    [Fact]
    public void TrophicLevel_CarnivoresAreLevel4()
    {
        Assert.Equal(4, FoodWeb.TrophicLevel(CreatureType.Carnivore));
    }

    [Fact]
    public void Creatures_HaveCorrectDietDefault()
    {
        var rng = new System.Random(42);
        var herbivore = new Herbivore(new Microsoft.Xna.Framework.Vector2(100, 100), Genome.Random(rng), "Rabbit");
        var carnivore = new Carnivore(new Microsoft.Xna.Framework.Vector2(100, 100), Genome.Random(rng), "Wolf");
        var omnivore = new Omnivore(new Microsoft.Xna.Framework.Vector2(100, 100), Genome.Random(rng), "Bear");

        Assert.Equal(DietType.Herbivore, herbivore.Diet);
        Assert.Equal(DietType.Carnivore, carnivore.Diet);
        Assert.Equal(DietType.Omnivore, omnivore.Diet);
    }
}
