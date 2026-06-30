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
        game._camera = new Moq.Mock<PitLife.Rendering.Camera>(800, 600) { CallBase = true }.Object;
        game._pendingWorldGen = true;
        game._pendingSeed = 12345;

        var mockOrchestrator = new Mock<SimulationOrchestrator>(game);
        game._orchestrator = mockOrchestrator.Object;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.5f), TimeSpan.FromSeconds(0.5f));
        coordinator.Update(gameTime);

        // Timer should decrement by 0.5f -> 1.0f (which is still > 0.8f, so GenerateNewWorld shouldn't run yet)
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
        game._camera = new Moq.Mock<PitLife.Rendering.Camera>(800, 600) { CallBase = true }.Object;
        game._pendingWorldGen = true;
        game._pendingSeed = 12345;

        var mockOrchestrator = new Mock<SimulationOrchestrator>(game);
        game._orchestrator = mockOrchestrator.Object;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        var mockController = new Mock<SimulationController>(new Mock<Ecosystem>(20, 20, 1).Object, new Mock<DayNightCycle>().Object);
        game._controller = mockController.Object;

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.2f), TimeSpan.FromSeconds(0.2f));
        coordinator.Update(gameTime);

        // Timer is now 0.7f (<= 0.8f), so GenerateNewWorld should have been called
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

        game._screen = Game1.GameScreen.MainMenu;
        game._menuInputCooldown = 0f;
        game._showLoadingTimer = 0f;
        game._camera = new Moq.Mock<PitLife.Rendering.Camera>(800, 600) { CallBase = true }.Object;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        SetReadonlyField(game, "_mainMenu", new TestMainMenuStart());

        var mockController = new Mock<SimulationController>(new Mock<Ecosystem>(20, 20, 1).Object, new Mock<PitLife.Rendering.DayNightCycle>().Object);
        game._controller = mockController.Object;
        game._ecosystem = new Mock<Ecosystem>(20, 20, 1).Object;
        SetReadonlyField(game, "_dayNight", new Mock<PitLife.Rendering.DayNightCycle>().Object);

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

        game._screen = Game1.GameScreen.MainMenu;
        game._menuInputCooldown = 0f;
        game._showLoadingTimer = 0f;
        game._camera = new Moq.Mock<PitLife.Rendering.Camera>(800, 600) { CallBase = true }.Object;

        var mockInput = new Mock<InputManager>();
        game._inputManager = mockInput.Object;

        SetReadonlyField(game, "_mainMenu", new TestMainMenuNewWorld());
        game._ecosystem = new Mock<Ecosystem>(20, 20, 1).Object;
        SetReadonlyField(game, "_dayNight", new Mock<PitLife.Rendering.DayNightCycle>().Object);

        var gameTime = new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016));
        coordinator.Update(gameTime);

        Assert.True(game._pendingWorldGen);
        Assert.Null(game._pendingSeed);
        Assert.Equal(1.5f, game._showLoadingTimer);
    }
}


public class TestMainMenuStart : MainMenu
{
    public override bool IsWorldGenPanelOpen { get => false; set { } }
    public override MenuAction Update(Microsoft.Xna.Framework.Input.MouseState mouse, Microsoft.Xna.Framework.Input.MouseState previousMouse, Microsoft.Xna.Framework.Input.KeyboardState keyboard, Microsoft.Xna.Framework.Input.KeyboardState previousKeyboard, int viewportWidth, int viewportHeight, bool isFullscreen)
    {
        return MenuAction.StartGame;
    }
}

public class TestMainMenuNewWorld : MainMenu
{
    public override bool IsWorldGenPanelOpen { get => false; set { } }
    public override MenuAction Update(Microsoft.Xna.Framework.Input.MouseState mouse, Microsoft.Xna.Framework.Input.MouseState previousMouse, Microsoft.Xna.Framework.Input.KeyboardState keyboard, Microsoft.Xna.Framework.Input.KeyboardState previousKeyboard, int viewportWidth, int viewportHeight, bool isFullscreen)
    {
        return MenuAction.NewWorld;
    }
}
