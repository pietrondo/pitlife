using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PitLife.Rendering;

public class Camera
{
    public Vector2 Position { get; set; }
    public float Zoom { get; set; } = 1f;
    public int ViewportWidth { get; set; }
    public int ViewportHeight { get; set; }

    public Matrix TransformMatrix =>
        Matrix.CreateTranslation(-Position.X, -Position.Y, 0) *
        Matrix.CreateScale(Zoom) *
        Matrix.CreateTranslation(ViewportWidth / 2f, ViewportHeight / 2f, 0);

    public Camera(int viewportWidth, int viewportHeight)
    {
        ViewportWidth = viewportWidth;
        ViewportHeight = viewportHeight;
    }

    public int WorldWidth { get; set; } = 6400;
    public int WorldHeight { get; set; } = 4800;

    public void HandleInput(float dt)
    {
        var kbd = Keyboard.GetState();
        float speed = 300f / Zoom;
        float nx = Position.X, ny = Position.Y;
        if (kbd.IsKeyDown(Keys.W) || kbd.IsKeyDown(Keys.Up)) ny -= speed * dt;
        if (kbd.IsKeyDown(Keys.S) || kbd.IsKeyDown(Keys.Down)) ny += speed * dt;
        if (kbd.IsKeyDown(Keys.A) || kbd.IsKeyDown(Keys.Left)) nx -= speed * dt;
        if (kbd.IsKeyDown(Keys.D) || kbd.IsKeyDown(Keys.Right)) nx += speed * dt;
        Position = new(Math.Clamp(nx, 0, WorldWidth), Math.Clamp(ny, 0, WorldHeight));

        var scroll = Mouse.GetState().ScrollWheelValue;
        int diff = scroll - _lastScroll;
        if (diff != 0)
        {
            float delta = diff / 120f;
            Zoom = MathHelper.Clamp(Zoom + delta * 0.1f, 0.25f, 4f);
            _lastScroll = scroll;
        }
    }

    private int _lastScroll;

    public Vector2 ScreenToWorld(int screenX, int screenY)
    {
        return Vector2.Transform(
            new Vector2(screenX, screenY) - new Vector2(ViewportWidth / 2f, ViewportHeight / 2f),
            Matrix.Invert(Matrix.CreateScale(Zoom))
        ) + Position;
    }

    public Rectangle VisibleArea => new(
        (int)(Position.X - ViewportWidth / 2f / Zoom),
        (int)(Position.Y - ViewportHeight / 2f / Zoom),
        (int)(ViewportWidth / Zoom + 2),
        (int)(ViewportHeight / Zoom + 2)
    );
}
