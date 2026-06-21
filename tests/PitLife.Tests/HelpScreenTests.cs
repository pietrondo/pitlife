using Microsoft.Xna.Framework.Input;
using PitLife.UI;

namespace PitLife.Tests;

public class HelpScreenTests
{
    [Fact]
    public void EscapeClosesScreenOnlyAfterOpeningInputIsReleased()
    {
        var screen = new HelpScreen();
        var mouse = new MouseState();
        var released = new KeyboardState();
        var escape = new KeyboardState(Keys.Escape);

        screen.Show();
        Assert.True(screen.IsActive);
        Assert.False(screen.Update(mouse, mouse, escape, released, 800, 720));
        Assert.True(screen.IsActive);

        screen.Update(mouse, mouse, released, escape, 800, 720);
        Assert.True(screen.Update(mouse, mouse, escape, released, 800, 720));
        Assert.False(screen.IsActive);
    }
}
