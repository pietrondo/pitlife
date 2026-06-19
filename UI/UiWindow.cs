using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.UI;

public sealed class UiWindow
{
    public string Title { get; set; }
    public Rectangle Bounds { get; set; }

    public UiWindow(string title)
    {
        Title = title;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font)
    {
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X + 8, Bounds.Y + 8, Bounds.Width, Bounds.Height), UiTheme.Shadow);
        UiPrimitives.Fill(spriteBatch, pixel, Bounds, UiTheme.ForestNight);
        UiPrimitives.Border(spriteBatch, pixel, Bounds, 3, UiTheme.BarkEdge);

        var titleBar = new Rectangle(Bounds.X + 3, Bounds.Y + 3, Bounds.Width - 6, 40);
        UiPrimitives.Fill(spriteBatch, pixel, titleBar, UiTheme.DeepGrove);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(titleBar.X, titleBar.Bottom - 2, titleBar.Width, 2), UiTheme.BarkEdge);

        const float scale = 1.15f;
        Vector2 size = font.MeasureString(Title) * scale;
        var position = new Vector2(titleBar.Center.X - size.X / 2f, titleBar.Center.Y - size.Y / 2f);
        spriteBatch.DrawString(font, Title, position, UiTheme.WarmParchment, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }
}
