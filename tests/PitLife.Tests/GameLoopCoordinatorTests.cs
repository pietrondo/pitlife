using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using Moq;
using PitLife;
using PitLife.Core;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Rendering;
using Xunit;

namespace PitLife.Tests;

public class GameLoopCoordinatorTests
{
    private static void SetReadonlyField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
        field!.SetValue(target, value);
    }

    [Fact]
    public void GameLoopCoordinator_Update_DecrementsShowLoadingTimer()
    {
        using var game = new Game1();
        var coordinator = new GameLoopCoordinator(game);

        game._showLoadingTimer = 1.5f;
        game._pendingWorldGen = true;
        game._pendingSeed = 12345;

        game._inputManager = new InputManager();

        var mockOrchestrator = new Mock<SimulationOrchestrator>(game);
        game._orchestrator = mockOrchestrator.Object;

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.5f), TimeSpan.FromSeconds(0.5f));
        coordinator.Update(gameTime);

        Assert.Equal(1.0f, game._showLoadingTimer);
        Assert.True(game._pendingWorldGen);
        mockOrchestrator.Verify(o => o.GenerateNewWorld(It.IsAny<int?>(), It.IsAny<WorldGenOptions>()), Times.Never);
    }

    [Fact]
    public void GameLoopCoordinator_Update_TriggersWorldGeneration_WhenTimerCrossesThreshold()
    {
        using var game = new Game1();
        var coordinator = new GameLoopCoordinator(game);

        game._showLoadingTimer = 0.9f;
        game._pendingWorldGen = true;
        game._pendingSeed = 12345;

        game._inputManager = new InputManager();

        var mockOrchestrator = new Mock<SimulationOrchestrator>(game);
        game._orchestrator = mockOrchestrator.Object;

        var ecosystem = new Ecosystem(20, 20, 1);
        var dayNight = new DayNightCycle();
        game._controller = new SimulationController(ecosystem, dayNight);

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.2f), TimeSpan.FromSeconds(0.2f));
        coordinator.Update(gameTime);

        Assert.Equal(0.7f, game._showLoadingTimer, 3);
        Assert.False(game._pendingWorldGen);
        Assert.Equal(Game1.GameScreen.Playing, game._screen);
        Assert.False(game._paused);
        mockOrchestrator.Verify(o => o.GenerateNewWorld(12345, It.IsAny<WorldGenOptions>()), Times.Once);
    }

    [Fact]
    public void GameLoopCoordinator_UpdateMainMenu_HandlesStartGameAction()
    {
        using var game = new Game1();
        var coordinator = new GameLoopCoordinator(game);

        game._showLoadingTimer = 0f;
        game._screen = Game1.GameScreen.MainMenu;
        game._menuInputCooldown = 0f;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        var mockMainMenu = new Mock<MainMenu>();
        mockMainMenu.Setup(m => m.Update(It.IsAny<MouseState>(), It.IsAny<MouseState>(), It.IsAny<KeyboardState>(), It.IsAny<KeyboardState>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(MenuAction.StartGame);
        SetReadonlyField(game, "_mainMenu", mockMainMenu.Object);

        var ecosystem = new Ecosystem(20, 20, 1);
        var dayNight = new DayNightCycle();
        game._controller = new SimulationController(ecosystem, dayNight);

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016));
        coordinator.Update(gameTime);

        Assert.Equal(Game1.GameScreen.Playing, game._screen);
        Assert.False(game._paused);
    }

    [Fact]
    public void GameLoopCoordinator_UpdateMainMenu_HandlesNewWorldAction()
    {
        using var game = new Game1();
        var coordinator = new GameLoopCoordinator(game);

        game._showLoadingTimer = 0f;
        game._screen = Game1.GameScreen.MainMenu;
        game._menuInputCooldown = 0f;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        var mockMainMenu = new Mock<MainMenu>();
        mockMainMenu.Setup(m => m.Update(It.IsAny<MouseState>(), It.IsAny<MouseState>(), It.IsAny<KeyboardState>(), It.IsAny<KeyboardState>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .Returns(MenuAction.NewWorld);
        SetReadonlyField(game, "_mainMenu", mockMainMenu.Object);

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016));
        coordinator.Update(gameTime);

        Assert.True(game._pendingWorldGen);
        Assert.Null(game._pendingSeed);
        Assert.Equal(1.5f, game._showLoadingTimer);
    }
}
