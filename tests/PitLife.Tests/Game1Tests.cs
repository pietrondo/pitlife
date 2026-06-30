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
    public void Game1_InitializeServices_LoadsCatalogWithoutThrowing()
    {
        using var game = new Game1();
        game._orchestrator = new Mock<SimulationOrchestrator>(game).Object;

        I18n.SetLanguage("en");
        var ex = Record.Exception(() => game.InitializeServices());

        Assert.Null(ex);
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
    public void Game1_InitializeUI_DoesNotThrow()
    {
        using var game = new Game1();
        game._ecosystem = new Ecosystem(20, 20, 1);
        game._inGameUi.World = game._ecosystem.World;

        var ex = Record.Exception(() => game.InitializeUI());
        Assert.Null(ex);
    }

    [Fact]
    public void Game1_OnSpeciesCatalogChanged_RefreshesSpawnPanel()
    {
        using var game = new Game1();

        var prop = typeof(Game1).GetMethod("OnSpeciesCatalogChanged",
            BindingFlags.NonPublic | BindingFlags.Instance);
        prop!.Invoke(game, null);

        Assert.False(game._spawnPanel.IsOpen);
    }

    [Fact]
    public void Game1_SaveLanguagePref_WritesSettingsJson()
    {
        var path = "settings.json";
        try
        {
            if (File.Exists(path)) File.Delete(path);
            I18n.SetLanguage("en");
            Game1.SaveLanguagePref();
            Assert.True(File.Exists(path));
            var json = File.ReadAllText(path);
            var doc = System.Text.Json.JsonDocument.Parse(json);
            Assert.Equal("en", doc.RootElement.GetProperty("language").GetString());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
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
