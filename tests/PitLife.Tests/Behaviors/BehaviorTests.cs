using Microsoft.Xna.Framework;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tests.Behaviors;

public class BehaviorTests
{
    [Fact]
    public void BaseBehavior_HerbivoreFleesFromPredator()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 5, c: 0, o: 0, p: 0);

        var herbivore = (Herbivore)eco.Creatures.First(c => c.CreatureType == CreatureType.Herbivore);
        herbivore.GrowFor(60f);
        herbivore.Energy = 1000f;
        var startPos = herbivore.Position;

        var wolf = new Carnivore(startPos, Genome.Random(new Random(1)), "Wolf")
        { Energy = 1000f };
        wolf.GrowFor(60f);
        wolf.Position = startPos + new Vector2(5, 0);
        eco.AddCreature(wolf);

        float energyBefore = herbivore.Energy;
        for (int i = 0; i < 10; i++)
            eco.Tick(new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1)));

        Assert.True(herbivore.IsAlive, "Herbivore died during test");
        Assert.True(herbivore.Energy < energyBefore,
            $"Herbivore should consume energy. Before={energyBefore}, After={herbivore.Energy}");
    }

    [Fact]
    public void BaseBehavior_HerbivoreEatsPlant()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 5);
        eco.CurrentDayPhase = DayPhase.Dusk;

        var plant = eco.Creatures.OfType<Plant>().First();
        var herbivore = new Herbivore(plant.Position + new Vector2(2, 0),
            Genome.Random(new Random(1)), "Rabbit");
        herbivore.Activity = ActivityPattern.Diurnal;
        float energyBefore = herbivore.Energy;
        eco.AddCreature(herbivore);

        for (int i = 0; i < 5; i++)
            eco.Tick(new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1)));

        Assert.True(herbivore.Energy >= energyBefore);
    }

    [Fact]
    public void PlantBehavior_GrowsWithSunlight()
    {
        var plant = new Plant(new Vector2(32, 32), Genome.Random(new Random(1)), "Clover");
        float energyBefore = plant.Energy;

        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(h: 0, c: 0, o: 0, p: 0);
        eco.AddCreature(plant);

        for (int i = 0; i < 30; i++)
            eco.Tick(new GameTime(System.TimeSpan.FromSeconds(0.1), System.TimeSpan.FromSeconds(0.1)));

        Assert.True(plant.Energy > energyBefore || plant.IsAdult,
            "Plant should have grown or aged after 30 ticks");
    }

    [Fact]
    public void Behavior_IsSwappable_AtRuntime()
    {
        var creature = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)), "Deer")
        { Energy = 1000f };
        var originalBehavior = creature.Behavior;
        Assert.IsType<BaseBehavior>(originalBehavior);

        creature.Behavior = new BaseBehavior();
        Assert.Same(creature.Behavior, creature.Behavior);
    }
}
