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
        var field = typeof(MainMenu).GetField("_focusedIndex", BindingFlags.NonPublic | BindingFlags.Instance);
        return (int)field.GetValue(menu);
    }

    private static bool GetSeedInputFocused(MainMenu menu)
    {
        var field = typeof(MainMenu).GetField("_seedInput", BindingFlags.NonPublic | BindingFlags.Instance);
        var seedInput = (UiTextInput)field.GetValue(menu);
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
}
