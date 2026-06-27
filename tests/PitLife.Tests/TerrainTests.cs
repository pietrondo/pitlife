using Microsoft.Xna.Framework;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class TerrainTests
{
    [Fact]
    public void TerrainWindow_SelectedTile_ReturnsCorrectCoordinates()
    {
        var ui = new InGameUi();
        var world = new World(100, 100, 42);
        ui.World = world;

        var targetPoint = new Point(50, 60);
        ui.SelectedTile = targetPoint;

        Assert.NotNull(ui.SelectedTile);
        Assert.Equal(targetPoint, ui.SelectedTile.Value);
    }

    [Fact]
    public void TerrainWindow_WorldTileData_IsConsistentWithSimulationWorld()
    {
        var ui = new InGameUi();
        var world = new World(10, 10, 42);
        ui.World = world;

        var targetPoint = new Point(3, 4);
        ui.SelectedTile = targetPoint;

        // Verify that UI references retrieve identical tiles and elevation/river statuses
        var worldTile = world.GetTile(3, 4);
        var uiWorldTile = ui.World.GetTile(3, 4);
        Assert.Same(worldTile, uiWorldTile);

        var elevation = world.ElevationField[4 * world.Width + 3];
        var uiElevation = ui.World.ElevationField[4 * ui.World.Width + 3];
        Assert.Equal(elevation, uiElevation);

        var isRiver = world.RiverMask[4 * world.Width + 3];
        var uiIsRiver = ui.World.RiverMask[4 * ui.World.Width + 3];
        Assert.Equal(isRiver, uiIsRiver);
    }
}
