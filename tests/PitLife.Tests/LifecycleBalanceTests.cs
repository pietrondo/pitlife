using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit.Abstractions;

namespace PitLife.Tests;

public class LifecycleBalanceTests
{
    private readonly ITestOutputHelper _output;

    public LifecycleBalanceTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void FixedSeed_ProducesStableDeterministicLifecycleMetrics()
    {
        LifecycleMetrics first = RunFixedSeedSimulation(42, 60f);
        LifecycleMetrics second = RunFixedSeedSimulation(42, 60f);
        _output.WriteLine(first.ToString());

        Assert.Equal(first, second);
        Assert.InRange(first.TotalLiving, 20, 500);
        Assert.True(first.Plants > 0);
        Assert.True(first.Plants <= first.PlantCapacity,
            $"Plants exceeded carrying capacity: {first.Plants}/{first.PlantCapacity}");
        Assert.True(first.Animals > 0);
        Assert.True(first.Adults > 0);
        Assert.InRange(first.MaleRatio, 0.2, 0.8);
    }

    [Fact]
    public void AdultMixedSexHerd_ReproducesWithoutPopulationExplosion()
    {
        var ecosystem = new Ecosystem(32, 24, 17) { MaxCreatures = 60 };
        Vector2 origin = FindLandPosition(ecosystem.World);

        for (int i = 0; i < 6; i++)
        {
            var deer = new Herbivore(origin + new Vector2(i * 2f, 0f), StableGenome(), "Deer")
            {
                Gender = i % 2 == 0 ? Gender.Male : Gender.Female
            };
            deer.GrowFor(deer.MaturityAge);
            deer.Energy = deer.MaxEnergy;
            ecosystem.AddCreature(deer);
        }
        ecosystem.FlushPending();

        Advance(ecosystem, 10f);
        Creature[] living = ecosystem.Creatures.Where(creature => creature.IsAlive).ToArray();
        int infants = living.Count(creature => creature.LifeStage == LifeStage.Infant);

        _output.WriteLine($"Herd after 10s: living={living.Length}, infants={infants}");
        Assert.True(infants > 0, "The adult mixed-sex herd produced no offspring");
        Assert.InRange(living.Length, 7, 30);
    }

    [Fact]
    public void Herd_RemainsCohesiveWithinVisionRange()
    {
        var ecosystem = new Ecosystem(32, 24, 23);
        Vector2 origin = FindLandPosition(ecosystem.World);

        for (int i = 0; i < 6; i++)
        {
            var deer = new Herbivore(origin + new Vector2(i * 3f, 0f), StableGenome(), "Deer")
            {
                Gender = Gender.Male
            };
            ecosystem.AddCreature(deer);
        }
        ecosystem.FlushPending();

        Advance(ecosystem, 10f);
        Creature[] herd = ecosystem.Creatures.Where(creature => creature.IsAlive).ToArray();
        double averageNearestDistance = herd.Average(creature =>
            herd.Where(other => other != creature).Min(other => (double)creature.DistanceTo(other)));

        _output.WriteLine($"Herd average nearest distance: {averageNearestDistance:F1}px");
        Assert.InRange(averageNearestDistance, 5d, StableGenome().VisionRange * 32d);
    }

    private static LifecycleMetrics RunFixedSeedSimulation(int seed, float seconds)
    {
        var ecosystem = new Ecosystem(64, 48, seed) { MaxCreatures = 500 };
        ecosystem.Initialize(h: 30, c: 10, o: 8, p: 80);
        Advance(ecosystem, seconds);

        Creature[] living = ecosystem.Creatures.Where(creature => creature.IsAlive).ToArray();
        Creature[] animals = living.Where(creature => creature.CreatureType != CreatureType.Plant).ToArray();
        int males = animals.Count(creature => creature.Gender == Gender.Male);
        int females = animals.Count(creature => creature.Gender == Gender.Female);
        return new LifecycleMetrics(
            living.Length,
            living.Count(creature => creature.CreatureType == CreatureType.Plant),
            ecosystem.PlantCarryingCapacity,
            animals.Length,
            animals.Count(creature => creature.IsAdult),
            animals.Count(creature => creature.IsBaby),
            males,
            females);
    }

    private static void Advance(Ecosystem ecosystem, float seconds)
    {
        const float step = 0.1f;
        int ticks = (int)(seconds / step);
        for (int i = 0; i < ticks; i++)
            ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(i * step), TimeSpan.FromSeconds(step)));
    }

    private static Vector2 FindLandPosition(World world)
    {
        for (int y = 1; y < world.Height - 1; y++)
        for (int x = 1; x < world.Width - 1; x++)
        {
            Tile tile = world.GetTile(x, y);
            if (tile.IsPassable && tile.Biome is not (BiomeType.DeepOcean or BiomeType.ShallowWater))
                return new Vector2((x + 0.5f) * world.TileSize, (y + 0.5f) * world.TileSize);
        }
        throw new InvalidOperationException("No land tile found for balance test");
    }

    private static Genome StableGenome() => new()
    {
        Speed = 1f,
        Size = 1f,
        Metabolism = 0.5f,
        VisionRange = 5f,
        Color = Color.White,
        MutationRate = 0.01f,
        DesertAdaptation = 1f,
        ColdAdaptation = 1f,
        ForestAdaptation = 1f,
        WaterAdaptation = 0f
    };

    private readonly record struct LifecycleMetrics(
        int TotalLiving,
        int Plants,
        int PlantCapacity,
        int Animals,
        int Adults,
        int Infants,
        int Males,
        int Females)
    {
        public double MaleRatio => Animals == 0 ? 0 : Males / (double)Animals;
    }
}
