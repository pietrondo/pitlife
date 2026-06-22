using PitLife.Simulation;
using PitLife.UI;
using Xunit;

namespace PitLife.Tests;

public class WindowSizingTests
{
    [Fact]
    public void Window_ContentBounds_CalculatedCorrectly()
    {
        var window = new UiWindow("Test", "test")
        {
            Bounds = new Microsoft.Xna.Framework.Rectangle(10, 10, 300, 260)
        };

        Assert.Equal(10 + 16, window.ContentBounds.X);
        Assert.Equal(10 + 56, window.ContentBounds.Y);
        Assert.Equal(300 - 32, window.ContentBounds.Width);
        Assert.Equal(260 - 72, window.ContentBounds.Height);
    }

    [Fact]
    public void Window_Height_EnoughForAllStatsRows_WithMetrics()
    {
        var window = new UiWindow("Stats", "stats")
        {
            Bounds = new Microsoft.Xna.Framework.Rectangle(32, 88, 320, 340)
        };

        int contentHeight = window.ContentBounds.Height;
        int neededHeight = 0;
        neededHeight += 22;  // time
        neededHeight += 32;  // speed
        neededHeight += 22;  // total
        neededHeight += 22;  // births/deaths
        neededHeight += 18;  // death causes
        neededHeight += 18;  // species/het
        neededHeight += 30;  // plants
        neededHeight += 30;  // herbivores
        neededHeight += 30;  // carnivores
        neededHeight += 30;  // omnivores
        neededHeight += 8;   // padding

        Assert.True(contentHeight >= neededHeight,
            $"Content height {contentHeight}px is insufficient for {neededHeight}px of stats rows");
    }

    [Fact]
    public void Window_Height_EnoughForAllStatsRows_WithoutMetrics()
    {
        var window = new UiWindow("Stats", "stats")
        {
            Bounds = new Microsoft.Xna.Framework.Rectangle(32, 88, 320, 280)
        };

        int contentHeight = window.ContentBounds.Height;
        int neededHeight = 0;
        neededHeight += 22;
        neededHeight += 32;
        neededHeight += 22;
        neededHeight += 30;  // plants only (4 bars)
        neededHeight += 30;
        neededHeight += 30;
        neededHeight += 30;
        neededHeight += 8;

        Assert.True(contentHeight >= neededHeight,
            $"Content height {contentHeight}px is insufficient for {neededHeight}px of stats rows");
    }

    [Fact]
    public void Window_AllWindowsAreDraggableByDefault()
    {
        var window = new UiWindow("Test", "test");
        Assert.True(window.IsDraggable);
    }

    [Fact]
    public void Window_DoubleClickTogglesCollapse()
    {
        var window = new UiWindow("Test", "test")
        {
            Bounds = new Microsoft.Xna.Framework.Rectangle(10, 10, 200, 200)
        };
        Assert.False(window.IsCollapsed);
        window.ToggleCollapse();
        Assert.True(window.IsCollapsed);
        Assert.Equal(46, window.Bounds.Height);
        window.ToggleCollapse();
        Assert.False(window.IsCollapsed);
        Assert.Equal(200, window.Bounds.Height);
    }
}

