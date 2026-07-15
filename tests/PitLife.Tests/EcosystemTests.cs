using System;
using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class EcosystemTests
{
    private sealed class ThrowingCreature : Creature
    {
        public ThrowingCreature(Vector2 position, Genome genome, string species)
            : base(position, genome, CreatureType.Herbivore)
        {
            Species = species;
        }

        public override bool IsAquatic => false;

        public override void Update(World world, Ecosystem ecosystem, float dt)
        {
            throw new InvalidOperationException("boom");
        }

        protected override Creature CreateChild(Vector2 position, Genome genome, Random rng) => null!;
    }

    [Fact]
    public void Tick_AppliesSimulationSpeedExactlyOnce()
    {
        var ecosystem = new Ecosystem(32, 24, 7)
        {
            SimulationSpeed = 4f
        };
        var elapsed = TimeSpan.FromSeconds(0.1);

        ecosystem.Tick(new GameTime(elapsed, elapsed));

        Assert.Equal(0.4f, ecosystem.TotalTime, precision: 4);
    }

    [Fact]
    public void Tick_QuarantinesFailedCreatureWithoutMarkingItDead()
    {
        var ecosystem = new Ecosystem(32, 24, 7);
        var creature = new ThrowingCreature(new Vector2(10, 10), Genome.Random(new Random(7)), "Crashy");

        ecosystem.AddCreature(creature);
        ecosystem.FlushPending();
        ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        Assert.DoesNotContain(ecosystem.Creatures, c => c.Species == "Crashy");
        Assert.Equal(0, ecosystem.Metrics.TotalDeaths);
    }
}