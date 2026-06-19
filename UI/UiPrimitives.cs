using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.UI;

internal static class UiPrimitives
{
    public static void Fill(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, Color color)
    {
        spriteBatch.Draw(pixel, bounds, color);
    }

    public static void Border(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, int thickness, Color color)
    {
        Fill(spriteBatch, pixel, new Rectangle(bounds.X, bounds.Y, bounds.Width, thickness), color);
        Fill(spriteBatch, pixel, new Rectangle(bounds.X, bounds.Bottom - thickness, bounds.Width, thickness), color);
        Fill(spriteBatch, pixel, new Rectangle(bounds.X, bounds.Y, thickness, bounds.Height), color);
        Fill(spriteBatch, pixel, new Rectangle(bounds.Right - thickness, bounds.Y, thickness, bounds.Height), color);
    }
}
