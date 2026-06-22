using Microsoft.Xna.Framework;
using PitLife.Rendering;

namespace PitLife.Tests;

public class CameraTests
{
    [Fact]
    public void ClampToWorldBounds_WrapsAround()
    {
        var camera = new Camera(800, 600)
        {
            WorldWidth = 3200,
            WorldHeight = 2400,
            Zoom = 1f,
            Position = new Vector2(3500, 2600)
        };

        camera.ClampToWorldBounds();

        Assert.Equal(new Vector2(300, 200), camera.Position);
    }

    [Fact]
    public void ClampToWorldBounds_WrapsLargePosition()
    {
        var camera = new Camera(800, 600)
        {
            WorldWidth = 3200,
            WorldHeight = 2400,
            Zoom = 1f,
            Position = new Vector2(-500, 7200)
        };
        camera.ClampToWorldBounds();
        Assert.Equal(new Vector2(2700, 0), camera.Position);
    }
}
