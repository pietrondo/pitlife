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

    public void HandleInput(float dt)
    {
        var kbd = Keyboard.GetState();
        float speed = 300f / Zoom;
        if (kbd.IsKeyDown(Keys.W) || kbd.IsKeyDown(Keys.Up)) Position = new(Position.X, Position.Y - speed * dt);
        if (kbd.IsKeyDown(Keys.S) || kbd.IsKeyDown(Keys.Down)) Position = new(Position.X, Position.Y + speed * dt);
        if (kbd.IsKeyDown(Keys.A) || kbd.IsKeyDown(Keys.Left)) Position = new(Position.X - speed * dt, Position.Y);
        if (kbd.IsKeyDown(Keys.D) || kbd.IsKeyDown(Keys.Right)) Position = new(Position.X + speed * dt, Position.Y);

        var scroll = Mouse.GetState().ScrollWheelValue;
        if (scroll != _lastScroll)
        {
            float delta = (scroll - _lastScroll) / 120f;
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
