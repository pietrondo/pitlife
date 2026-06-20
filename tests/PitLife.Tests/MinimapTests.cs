using PitLife.Rendering;

namespace PitLife.Tests;

public class MinimapTests
{
    [Fact]
    public void Minimap_Creates_WithValidEcosystemAndCamera()
    {
        var eco = new PitLife.Simulation.Ecosystem(64, 48, 42);
        var cam = new Camera(800, 600);
        var minimap = new Minimap(eco, cam);
        Assert.NotNull(minimap);
    }

    [Fact]
    public void Minimap_Constants_ArePositive()
    {
        Assert.True(Minimap.Size > 0);
        Assert.True(Minimap.Margin >= 0);
    }
}
