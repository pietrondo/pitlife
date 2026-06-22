using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI;

public sealed class UiButton
{
    public string Text { get; set; }
    public Rectangle Bounds { get; set; }
    public bool IsDestructive { get; init; }
    public object? Tag { get; set; }

    public UiButton(string text)
    {
        Text = text;
    }

    public bool IsHovered(MouseState mouse) => Bounds.Contains(mouse.Position);

    public bool WasClicked(MouseState mouse, MouseState previousMouse) =>
        IsHovered(mouse) &&
        mouse.LeftButton == ButtonState.Released &&
        previousMouse.LeftButton == ButtonState.Pressed;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, MouseState mouse, bool isFocused)
    {
        bool hovered = IsHovered(mouse);
        Color fill = hovered ? UiTheme.ButtonFace : UiTheme.PanelBeige;
        Color border = IsDestructive ? UiTheme.DangerClay : UiTheme.ButtonShadow;

        UiPrimitives.Fill(spriteBatch, pixel, Bounds, fill);
        // 3D raised border
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 2), UiTheme.ButtonHighlight);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X, Bounds.Y, 2, Bounds.Height), UiTheme.ButtonHighlight);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X, Bounds.Bottom - 2, Bounds.Width, 2), UiTheme.ButtonShadow);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.Right - 2, Bounds.Y, 2, Bounds.Height), UiTheme.ButtonShadow);

        if (isFocused)
            UiPrimitives.Border(spriteBatch, pixel, Bounds, 3, UiTheme.MossSignal);

        Vector2 size = font.MeasureString(Text);
        Vector2 position = new(
            Bounds.Center.X - size.X / 2f,
            Bounds.Center.Y - size.Y / 2f);
        spriteBatch.DrawString(font, Text, position, Color.Black, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);
    }
}
