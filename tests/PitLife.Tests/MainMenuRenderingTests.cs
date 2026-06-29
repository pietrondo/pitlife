using Microsoft.Xna.Framework;
using PitLife.UI;
using Xunit;
using System.Reflection;

namespace PitLife.Tests;

public class MainMenuRenderingTests
{
    [Fact]
    public void Layout_SetsWindowBounds_AndButtons()
    {
        var menu = new MainMenu();
        var method = typeof(MainMenu).GetMethod("Layout", BindingFlags.NonPublic | BindingFlags.Instance);

        method!.Invoke(menu, new object[] { 800, 600 });

        var windowField = typeof(MainMenu).GetField("_window", BindingFlags.NonPublic | BindingFlags.Instance);
        var window = (UiWindow)windowField!.GetValue(menu)!;

        Assert.True(window.Bounds.Width > 0);
        Assert.True(window.Bounds.Height > 0);

        var mainButtonsField = typeof(MainMenu).GetField("_mainButtons", BindingFlags.NonPublic | BindingFlags.Instance);
        var mainButtons = (UiButton[])mainButtonsField!.GetValue(menu)!;

        Assert.Equal(7, mainButtons.Length);
        Assert.True(mainButtons[0].Bounds.Width > 0);
    }

    [Fact]
    public void Layout_WorldGenPanel_SetsBounds()
    {
        var menu = new MainMenu();
        var showWorldGenPanelField = typeof(MainMenu).GetField("_showWorldGenPanel", BindingFlags.NonPublic | BindingFlags.Instance);
        showWorldGenPanelField!.SetValue(menu, true);

        var method = typeof(MainMenu).GetMethod("Layout", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(menu, new object[] { 800, 600 });

        var windowField = typeof(MainMenu).GetField("_window", BindingFlags.NonPublic | BindingFlags.Instance);
        var window = (UiWindow)windowField!.GetValue(menu)!;

        Assert.True(window.Bounds.Height > 400); // World gen panel is taller

        var presetButtonField = typeof(MainMenu).GetField("_presetButton", BindingFlags.NonPublic | BindingFlags.Instance);
        var presetButton = (UiButton)presetButtonField!.GetValue(menu)!;

        Assert.True(presetButton.Bounds.Width > 0);
    }

    [Fact]
    public void Layout_OptionsPanel_SetsBounds()
    {
        var menu = new MainMenu();
        var showOptionsField = typeof(MainMenu).GetField("_showOptions", BindingFlags.NonPublic | BindingFlags.Instance);
        showOptionsField!.SetValue(menu, true);

        var method = typeof(MainMenu).GetMethod("Layout", BindingFlags.NonPublic | BindingFlags.Instance);
        method!.Invoke(menu, new object[] { 800, 600 });

        var optionButtonsField = typeof(MainMenu).GetField("_optionButtons", BindingFlags.NonPublic | BindingFlags.Instance);
        var optionButtons = (UiButton[])optionButtonsField!.GetValue(menu)!;

        Assert.Equal(3, optionButtons.Length);
        Assert.True(optionButtons[0].Bounds.Width > 0);
    }
}
