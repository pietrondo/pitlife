using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PitLife.UI;
using Xunit;
using Xunit.Abstractions;
using System.Reflection;

namespace PitLife.Tests;

public class MainMenuTests
{
    private readonly ITestOutputHelper _output;

    public MainMenuTests(ITestOutputHelper output)
    {
        _output = output;
    }

    private static int GetFocusedIndex(MainMenu menu)
    {
        var field = typeof(MainMenu).GetField("_focusedIndex", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (int)field.GetValue(menu)!;
    }

    private static bool GetSeedInputFocused(MainMenu menu)
    {
        var field = typeof(MainMenu).GetField("_seedInput", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var seedInput = (UiTextInput)field.GetValue(menu)!;
        return seedInput.IsFocused;
    }

    [Fact]
    public void TestStepByStep()
    {
        var menu = new MainMenu();
        var mouse = new MouseState();
        var prevMouse = new MouseState();

        _output.WriteLine($"Start: focusedIndex={GetFocusedIndex(menu)}, seedFocused={GetSeedInputFocused(menu)}");

        // Prime menu input ready
        menu.Update(mouse, prevMouse, new KeyboardState(), new KeyboardState(), 800, 600, false);
        _output.WriteLine($"After Prime: focusedIndex={GetFocusedIndex(menu)}, seedFocused={GetSeedInputFocused(menu)}");

        // 1. Press Down
        var kbdDown1 = new KeyboardState(Keys.Down);
        menu.Update(mouse, prevMouse, kbdDown1, new KeyboardState(), 800, 600, false);
        _output.WriteLine($"After Down 1: focusedIndex={GetFocusedIndex(menu)}, seedFocused={GetSeedInputFocused(menu)}");

        // Release Down
        menu.Update(mouse, prevMouse, new KeyboardState(), kbdDown1, 800, 600, false);
        _output.WriteLine($"After Release 1: focusedIndex={GetFocusedIndex(menu)}, seedFocused={GetSeedInputFocused(menu)}");

        // 2. Press Down again
        var kbdDown2 = new KeyboardState(Keys.Down);
        menu.Update(mouse, prevMouse, kbdDown2, new KeyboardState(), 800, 600, false);
        _output.WriteLine($"After Down 2: focusedIndex={GetFocusedIndex(menu)}, seedFocused={GetSeedInputFocused(menu)}");

        Assert.True(GetSeedInputFocused(menu));
    }

    [Fact]
    public void NewWorldButton_RequestsRandomWorld_WhenSeedIsEmpty()
    {
        var menu = new MainMenu();
        MouseState released = MouseAt(400, 340, ButtonState.Released);
        menu.Update(released, released, new KeyboardState(), new KeyboardState(), 800, 720, false);

        menu.Update(MouseAt(400, 340, ButtonState.Pressed), released,
            new KeyboardState(),
            new KeyboardState(),
            800,
            720,
            false);
        MenuAction action = menu.Update(
            released,
            MouseAt(400, 340, ButtonState.Pressed),
            new KeyboardState(),
            new KeyboardState(),
            800,
            720,
            false);

        Assert.Equal(MenuAction.NewWorld, action);
    }

    [Fact]
    public void NewWorldButton_UsesEnteredSeed()
    {
        var menu = new MainMenu();
        MouseState releasedInput = MouseAt(400, 390, ButtonState.Released);
        menu.Update(releasedInput, releasedInput, new KeyboardState(), new KeyboardState(), 800, 720, false);
        menu.Update(MouseAt(400, 390, ButtonState.Pressed), releasedInput,
            new KeyboardState(), new KeyboardState(), 800, 720, false);
        menu.Update(releasedInput, MouseAt(400, 390, ButtonState.Pressed),
            new KeyboardState(Keys.D4), new KeyboardState(), 800, 720, false);
        menu.Update(releasedInput, releasedInput,
            new KeyboardState(), new KeyboardState(Keys.D4), 800, 720, false);
        menu.Update(releasedInput, releasedInput,
            new KeyboardState(Keys.D2), new KeyboardState(), 800, 720, false);

        menu.Update(MouseAt(400, 340, ButtonState.Pressed),
            MouseAt(400, 340, ButtonState.Released),
            new KeyboardState(),
            new KeyboardState(Keys.D2),
            800,
            720,
            false);
        MenuAction action = menu.Update(
            MouseAt(400, 340, ButtonState.Released),
            MouseAt(400, 340, ButtonState.Pressed),
            new KeyboardState(),
            new KeyboardState(),
            800,
            720,
            false);

        Assert.Equal(MenuAction.NewWorldWithSeed, action);
        Assert.Equal(42, menu.Seed);
    }

    private static MouseState MouseAt(int x, int y, ButtonState state) =>
        new(x, y, 0, state, ButtonState.Released, ButtonState.Released,
            ButtonState.Released, ButtonState.Released);
}
