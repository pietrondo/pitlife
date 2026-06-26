using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PitLife.Rendering;

/// <summary>
/// Represents the Camera.
/// </summary>
public class Camera
{
    /// <summary>
    /// Gets or sets the Position.
    /// </summary>
    public Vector2 Position { get; set; }
    /// <summary>
    /// Gets or sets the Zoom.
    /// </summary>
    public float Zoom { get; set; } = 1f;
    /// <summary>
    /// Gets or sets the ViewportWidth.
    /// </summary>
    public int ViewportWidth { get; set; }
    /// <summary>
    /// Gets or sets the ViewportHeight.
    /// </summary>
    public int ViewportHeight { get; set; }

    /// <summary>
    /// Gets or sets the TransformMatrix.
    /// </summary>
    public Matrix TransformMatrix =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
        Matrix.CreateScale(Zoom) *
        Matrix.CreateTranslation(ViewportWidth / 2f, ViewportHeight / 2f, 0);

    /// <summary>
    /// Initializes a new instance of the Camera.
    /// </summary>
    /// <param name="viewportWidth">The viewportWidth parameter.</param>
    /// <param name="viewportHeight">The viewportHeight parameter.</param>
    public Camera(int viewportWidth, int viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
        _lastScroll = Mouse.GetState().ScrollWheelValue;
    }

    /// <summary>
    /// Gets or sets the WorldWidth.
    /// </summary>
    public int WorldWidth { get; set; } = 6400;
    /// <summary>
    /// Gets or sets the WorldHeight.
    /// </summary>
    public int WorldHeight { get; set; } = 4800;

    /// <summary>
    /// Executes the HandleInput.
    /// </summary>
    /// <param name="dt">The dt parameter.</param>
    public void HandleInput(float dt)
    {
        var kbd = Keyboard.GetState();
        float speed = 300f / Zoom;
        float nx = Position.X, ny = Position.Y;
        if (kbd.IsKeyDown(Keys.W) || kbd.IsKeyDown(Keys.Up)) ny -= speed * dt;
        if (kbd.IsKeyDown(Keys.S) || kbd.IsKeyDown(Keys.Down)) ny += speed * dt;
        if (kbd.IsKeyDown(Keys.A) || kbd.IsKeyDown(Keys.Left)) nx -= speed * dt;
        if (kbd.IsKeyDown(Keys.D) || kbd.IsKeyDown(Keys.Right)) nx += speed * dt;
        Position = ClampPosition(new Vector2(nx, ny));

        var scroll = Mouse.GetState().ScrollWheelValue;
        int diff = scroll - _lastScroll;
        if (diff != 0)
        {
            float delta = diff / 120f;
            Zoom = MathHelper.Clamp(Zoom + delta * 0.1f, 0.25f, 4f);
            _lastScroll = scroll;
            Position = ClampPosition(Position);
        }
    }

    private int _lastScroll;

    /// <summary>
    /// Executes the ClampToWorldBounds.
    /// </summary>
    public void ClampToWorldBounds()
    {
        Position = ClampPosition(Position);
    }

    private Vector2 ClampPosition(Vector2 position)
    {
        return new Vector2(
            (position.X % WorldWidth + WorldWidth) % WorldWidth,
            (position.Y % WorldHeight + WorldHeight) % WorldHeight);
    }

    /// <summary>
    /// Executes the ScreenToWorld.
    /// </summary>
    /// <param name="screenX">The screenX parameter.</param>
    /// <param name="screenY">The screenY parameter.</param>
    /// <returns>Returns the Vector2 result.</returns>
    public Vector2 ScreenToWorld(int screenX, int screenY)
    {
        return Vector2.Transform(
            new Vector2(screenX, screenY) - new Vector2(ViewportWidth / 2f, ViewportHeight / 2f),
            Matrix.Invert(Matrix.CreateScale(Zoom))
        ) + Position;
    }

    /// <summary>
    /// Gets or sets the VisibleArea.
    /// </summary>
    public Rectangle VisibleArea => new(
        (int)(Position.X - ViewportWidth / 2f / Zoom),
        (int)(Position.Y - ViewportHeight / 2f / Zoom),
        (int)(ViewportWidth / Zoom + 2),
        (int)(ViewportHeight / Zoom + 2)
    );
}
