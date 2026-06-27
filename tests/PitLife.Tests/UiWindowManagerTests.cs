using PitLife.UI;

namespace PitLife.Tests;

public class UiWindowManagerTests
{
    [Fact]
    public void Open_BringsWindowToFront()
    {
        var first = new UiWindow("First", "first") { IsOpen = true };
        var second = new UiWindow("Second", "second") { IsOpen = true };
        var manager = new UiWindowManager();
        manager.Add(first);
        manager.Add(second);

        manager.Open("first");

        Assert.Same(first, manager.Windows[^1]);
    }

    [Fact]
    public void CloseTopWindow_ClosesOnlyFrontmostOpenWindow()
    {
        var first = new UiWindow("First", "first") { IsOpen = true };
        var second = new UiWindow("Second", "second") { IsOpen = true };
        var manager = new UiWindowManager();
        manager.Add(first);
        manager.Add(second);

        var closed = manager.CloseTopWindow();

        Assert.True(closed);
        Assert.True(first.IsOpen);
        Assert.False(second.IsOpen);
    }

    [Fact]
    public void IsActive_ReturnsTrueOnlyForFrontmostOpenWindow()
    {
        var first = new UiWindow("First", "first") { IsOpen = true };
        var second = new UiWindow("Second", "second") { IsOpen = true };
        var manager = new UiWindowManager();
        manager.Add(first);
        manager.Add(second);

        // Initially second is at the front
        Assert.False(manager.IsActive(first));
        Assert.True(manager.IsActive(second));

        // Close second, first should become active
        second.IsOpen = false;
        Assert.True(manager.IsActive(first));
        Assert.False(manager.IsActive(second));

        // Open second again, it should become active
        manager.Open("second");
        Assert.False(manager.IsActive(first));
        Assert.True(manager.IsActive(second));
    }

    [Fact]
    public void TileWindows_ArrangesOpenWindowsSideBySide()
    {
        var first = new UiWindow("First", "first") { IsOpen = true, Bounds = new Microsoft.Xna.Framework.Rectangle(0, 0, 100, 100) };
        var second = new UiWindow("Second", "second") { IsOpen = true, Bounds = new Microsoft.Xna.Framework.Rectangle(0, 0, 150, 120) };
        var manager = new UiWindowManager();
        manager.Add(first);
        manager.Add(second);

        manager.TileWindows(800, 600);

        // The first open window starts at X=32, Y=88
        Assert.Equal(32, first.Bounds.X);
        Assert.Equal(88, first.Bounds.Y);

        // The second open window starts at first.X + first.Width + gap (16) = 32 + 100 + 16 = 148
        Assert.Equal(148, second.Bounds.X);
        Assert.Equal(88, second.Bounds.Y);
    }

    [Fact]
    public void ToggleCollapse_CollapsesAndRestoresHeightCorrectly()
    {
        var window = new UiWindow("First", "first") { Bounds = new Microsoft.Xna.Framework.Rectangle(10, 10, 200, 300) };

        Assert.False(window.IsCollapsed);

        window.ToggleCollapse();
        Assert.True(window.IsCollapsed);
        Assert.Equal(46, window.Bounds.Height); // Height is reduced to title bar size

        window.ToggleCollapse();
        Assert.False(window.IsCollapsed);
        Assert.Equal(300, window.Bounds.Height); // Height is restored to original size
    }
}
