using Microsoft.Xna.Framework;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class InGameUiTests
{
    [Fact]
    public void ResetForWorld_ClearsTransientWorldState()
    {
        var ui = new InGameUi
        {
            SelectedTile = new Point(4, 7),
            WantsToGoToMainMenu = true
        };
        var world = new World(16, 12, 42);

        ui.ResetForWorld(world);

        Assert.Same(world, ui.World);
        Assert.Null(ui.SelectedTile);
        Assert.False(ui.WantsToGoToMainMenu);
    }
}
