using PitLife.Simulation;
using Xunit;

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
    public void EnergyTransfer_SingleStepIs15Percent()
    {
        float efficiency = FoodWeb.EnergyTransferEfficiency(CreatureType.Plant, CreatureType.Herbivore);
        Assert.Equal(0.15f, efficiency, 2);
    }

    [Fact]
    public void EnergyTransfer_TwoStepsIs2Percent()
    {
        float efficiency = FoodWeb.EnergyTransferEfficiency(CreatureType.Plant, CreatureType.Carnivore);
        Assert.True(efficiency < 0.05f);
    }

    [Fact]
    public void BuildTrophicChain_ProducesCorrectOrder()
    {
        var chain = FoodWeb.BuildTrophicChain(CreatureType.Carnivore, 3);
        Assert.True(chain.Count >= 3);
        Assert.Equal("Carnivore", chain[0]);
        Assert.Equal("Plant", chain[^1]);
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
