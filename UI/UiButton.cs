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
    public bool IsDisabled { get; set; }
    public string? ShortcutHint { get; set; }

    public UiButton(string text)
    {
        Text = text;
    }

    public bool IsHovered(MouseState mouse) => !IsDisabled && Bounds.Contains(mouse.Position);

    public bool WasClicked(MouseState mouse, MouseState previousMouse) =>
        !IsDisabled &&
        IsHovered(mouse) &&
        mouse.LeftButton == ButtonState.Released &&
        previousMouse.LeftButton == ButtonState.Pressed;

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, MouseState mouse, bool isFocused, Color? activeFillColor = null)
    {
        var hovered = IsHovered(mouse);
        Color fill = IsDisabled ? UiTheme.DeepGrove : (hovered || isFocused ? UiTheme.ForestNight : (activeFillColor ?? UiTheme.DeepGrove));
        Color border = IsDisabled ? UiTheme.BarkEdge : (IsDestructive ? UiTheme.DangerClay : UiTheme.BarkEdge);

        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X + 4, Bounds.Y + 4, Bounds.Width, Bounds.Height), UiTheme.Shadow);
        UiPrimitives.Fill(spriteBatch, pixel, Bounds, fill);

        if (isFocused && !hovered)
            UiPrimitives.Border(spriteBatch, pixel, Bounds, 3, UiTheme.MossSignal);
        else if (hovered && !isFocused)
            UiPrimitives.Border(spriteBatch, pixel, Bounds, 2, Color.Lerp(border, Color.White, 0.2f));
        else if (isFocused && hovered)
            UiPrimitives.Border(spriteBatch, pixel, Bounds, 3, Color.Lerp(UiTheme.MossSignal, Color.White, 0.2f));
        else
            UiPrimitives.Border(spriteBatch, pixel, Bounds, 2, border);

        const float scale = 1.1f;
        Vector2 size = font.MeasureString(Text) * scale;
        Vector2 position = new(
            Bounds.Center.X - size.X / 2f,
            Bounds.Center.Y - size.Y / 2f);

        if (hovered && mouse.LeftButton == ButtonState.Pressed)
            position.Y += 2f;

        Color textColor = IsDisabled ? UiTheme.MutedStone : (hovered ? Color.White : UiTheme.WarmParchment);
        spriteBatch.DrawString(font, Text, position, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        if (!string.IsNullOrEmpty(ShortcutHint))
        {
            var hintScale = 0.8f;
            Vector2 hintSize = font.MeasureString(ShortcutHint) * hintScale;
            Vector2 hintPos = new(
                Bounds.Center.X - hintSize.X / 2f,
                Bounds.Bottom - hintSize.Y - 2);
            spriteBatch.DrawString(font, ShortcutHint, hintPos, IsDisabled ? UiTheme.MutedStone : (hovered ? UiTheme.WarmParchment : UiTheme.MutedStone), 0f, Vector2.Zero, hintScale, SpriteEffects.None, 0f);
        }
    }
}
