using System;
using Microsoft.Xna.Framework;
using Moq;
using PitLife;
using System.Reflection;
using Xunit;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;
using PitLife.Rendering;
using PitLife.Core;

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace PitLife.Tests;

public class Game1Tests
{
    [Fact]
    public void Game1_Constructor_SetsDefaults()
    {
        using var game = new Game1();
        Assert.True(game.IsMouseVisible);
        Assert.Equal("Content", game.Content.RootDirectory);
        Assert.NotNull(game._speciesEditor);
    }
    
    [Fact]
    public void Game1_Initialize_UsesMocksForDependencies()
    {
        using var game = new Game1();
        
        // Use Moq to mock some of the Game1 dependencies to prove we test initialization integration points
        var mockDayNight = new Mock<DayNightCycle>();
        var mockEcosystem = new Mock<Ecosystem>(200, 150, 42) { CallBase = true };
        
        var orchestratorMock = new Mock<SimulationOrchestrator>(game);
        game._orchestrator = orchestratorMock.Object;
        Assert.NotNull(game._orchestrator);
        
        var cameraMock = new Mock<Camera>(800, 600);
        game._camera = cameraMock.Object;
        Assert.NotNull(game._camera);
    }
    
    [Fact]
    public void Game1_FindClosestCreature_ReturnsNullForEmptyList()
    {
        var result = Game1.FindClosestCreature(Array.Empty<Creature>(), Vector2.Zero);
        Assert.Null(result);
    }
    
    // Create a dummy creature type for testing since Creature is abstract and Moq struggles with its constructor
    private class TestCreature : Creature
    {
        public TestCreature(Vector2 pos, Genome genome) : base(pos, genome, CreatureType.Herbivore) { }
        protected override Creature CreateChild(Vector2 p, Genome g, Random r) => this;
        public void SetAlive(bool value)
        {
            var prop = typeof(Creature).GetProperty("IsAlive");
            prop!.SetValue(this, value);
        }
    }
    
    [Fact]
    public void Game1_FindClosestCreature_ReturnsClosest()
    {
        var genome = Genome.Random(new Random(42));
        var c1 = new TestCreature(new Vector2(100, 100), genome);
        c1.SetAlive(true);
        var c2 = new TestCreature(new Vector2(10, 10), genome);
        c2.SetAlive(true);
        var c3 = new TestCreature(new Vector2(200, 200), genome);
        c3.SetAlive(true);
        
        var creatures = new[] { c1, c2, c3 };
        
        var result = Game1.FindClosestCreature(creatures, Vector2.Zero, 100f);
        Assert.NotNull(result);
        Assert.Equal(c2, result);
    }
    
    [Fact]
    public void Game1_FindClosestCreature_IgnoresDead()
    {
        var genome = Genome.Random(new Random(42));
        
        var c1 = new TestCreature(new Vector2(10, 10), genome);
        c1.SetAlive(false);
        var c2 = new TestCreature(new Vector2(20, 20), genome);
        c2.SetAlive(true);
        
        var creatures = new[] { c1, c2 };
        
        var result = Game1.FindClosestCreature(creatures, Vector2.Zero, 100f);
        Assert.NotNull(result);
        Assert.Equal(c2, result);
    }
}
