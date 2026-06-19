using Microsoft.Xna.Framework;
using PitLife.Rendering;

namespace PitLife.Tests;

public class CameraTests
{
    [Fact]
    public void ClampToWorldBounds_KeepsViewportInsideWorld()
    {
        var camera = new Camera(800, 600)
        {
            WorldWidth = 3200,
            WorldHeight = 2400,
            Zoom = 1f,
            Position = new Vector2(-100, 5000)
        };

        camera.ClampToWorldBounds();

        Assert.Equal(new Vector2(400, 2100), camera.Position);
    }

    [Fact]
    public void ClampToWorldBounds_WhenViewportExceedsWorld_CentersCamera()
    {
        var camera = new Camera(1600, 1200)
        {
            WorldWidth = 640,
            WorldHeight = 480,
            Zoom = 1f,
            Position = Vector2.Zero
        };

        camera.ClampToWorldBounds();

        Assert.Equal(new Vector2(320, 240), camera.Position);
    }
}
