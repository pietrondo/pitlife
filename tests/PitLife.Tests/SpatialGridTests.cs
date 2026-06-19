using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SpatialGridTests
{
    [Fact]
    public void FindNearest_ReturnsClosestMatchingCreatureAcrossCells()
    {
        var seeker = CreatureAt(20, 20, CreatureType.Herbivore);
        var close = CreatureAt(90, 20, CreatureType.Plant);
        var far = CreatureAt(400, 20, CreatureType.Plant);
        var grid = new SpatialGrid(640, 480, 64);
        grid.Rebuild([seeker, far, close]);

        Creature? result = grid.FindNearest(seeker, creature => creature.CreatureType == CreatureType.Plant);

        Assert.Same(close, result);
    }

    [Fact]
    public void Update_WhenCreatureChangesCell_UsesNewPosition()
    {
        var seeker = CreatureAt(20, 20, CreatureType.Herbivore);
        var moving = CreatureAt(500, 20, CreatureType.Plant);
        var stationary = CreatureAt(200, 20, CreatureType.Plant);
        var grid = new SpatialGrid(640, 480, 64);
        grid.Rebuild([seeker, moving, stationary]);
        moving.Position = new Vector2(40, 20);

        grid.Update(moving);
        Creature? result = grid.FindNearest(seeker, creature => creature.CreatureType == CreatureType.Plant);

        Assert.Same(moving, result);
    }

    private static TestCreature CreatureAt(float x, float y, CreatureType type) =>
        new(new Vector2(x, y), type);

    private sealed class TestCreature : Creature
    {
        public TestCreature(Vector2 position, CreatureType type)
            : base(position, TestGenome(), type)
        {
        }

        protected override Creature CreateChild(Vector2 position, Genome genome, Random rng) =>
            new TestCreature(position, CreatureType);

        private static Genome TestGenome() => new()
        {
            Size = 1f,
            Speed = 1f,
            Metabolism = 1f,
            VisionRange = 5f
        };
    }
}
