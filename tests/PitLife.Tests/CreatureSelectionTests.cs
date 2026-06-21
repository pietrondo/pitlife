using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class CreatureSelectionTests
{
    [Fact]
    public void FindClosestCreature_ReturnsNearestLivingCreatureWithinRadius()
    {
        var genome = Genome.Random(new Random(1));
        var farther = new Herbivore(new Vector2(120, 100), genome, "Deer");
        var nearest = new Herbivore(new Vector2(105, 100), genome, "Rabbit");

        Creature? selected = Game1.FindClosestCreature(
            [farther, nearest],
            new Vector2(100, 100));

        Assert.Same(nearest, selected);
    }

    [Fact]
    public void FindClosestCreature_IgnoresDeadAndOutOfRangeCreatures()
    {
        var genome = Genome.Random(new Random(1));
        var dead = new Herbivore(new Vector2(105, 100), genome, "Rabbit");
        dead.Die();
        var farAway = new Herbivore(new Vector2(200, 100), genome, "Deer");

        Creature? selected = Game1.FindClosestCreature(
            [dead, farAway],
            new Vector2(100, 100));

        Assert.Null(selected);
    }
}
