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

        bool closed = manager.CloseTopWindow();

        Assert.True(closed);
        Assert.True(first.IsOpen);
        Assert.False(second.IsOpen);
    }
}
