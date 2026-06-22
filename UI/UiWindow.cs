using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.UI;

public sealed class UiWindow
{
    public string Id { get; }
    public string Title { get; set; }
    public Rectangle Bounds { get; set; }
    public bool IsOpen { get; set; }
    public bool IsDraggable { get; set; } = true;
    public bool ShowCloseButton { get; set; }
    public bool IsCollapsed { get; private set; }
    private int _originalHeight;

    public Rectangle TitleBarBounds => new(Bounds.X + 3, Bounds.Y + 3, Bounds.Width - 6, 40);
    public Rectangle ContentBounds => new(Bounds.X + 16, Bounds.Y + 56, Bounds.Width - 32, Bounds.Height - 72);
    public Rectangle CloseButtonBounds => new(Bounds.Right - 39, Bounds.Y + 8, 30, 30);

    public UiWindow(string title, string? id = null)
    {
        Title = title;
        Id = id ?? title;
    }

    public void ToggleCollapse()
    {
        if (IsCollapsed)
        {
            IsCollapsed = false;
            Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, _originalHeight);
        }
        else
        {
            _originalHeight = Bounds.Height;
            IsCollapsed = true;
            Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, 46);
        }
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, bool isActive = true, Point mousePosition = default)
    {
        // 3D shadow
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X + 4, Bounds.Y + 4, Bounds.Width, Bounds.Height), UiTheme.Shadow);
        // Window body
        UiPrimitives.Fill(spriteBatch, pixel, Bounds, UiTheme.PanelBeige);
        // 3D border
        UiPrimitives.Border(spriteBatch, pixel, Bounds, 2, UiTheme.ButtonHighlight);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.Right, Bounds.Y, 2, Bounds.Height), UiTheme.ButtonShadow);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X, Bounds.Bottom, Bounds.Width + 2, 2), UiTheme.ButtonShadow);

        Rectangle titleBar = TitleBarBounds;
        UiPrimitives.Fill(spriteBatch, pixel, titleBar, isActive ? UiTheme.TitleBarBlue : UiTheme.DeepGrove);

        if (!IsCollapsed)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(titleBar.X, titleBar.Bottom - 2, titleBar.Width, 2), UiTheme.ButtonShadow);

        Vector2 size = font.MeasureString(Title);
        var position = new Vector2(titleBar.Center.X - size.X / 2f, titleBar.Center.Y - size.Y / 2f);
        spriteBatch.DrawString(font, Title, position, UiTheme.TitleText, 0f, Vector2.Zero, 1f, SpriteEffects.None, 0f);

        if (ShowCloseButton)
        {
            Rectangle close = CloseButtonBounds;
            bool isHovered = close.Contains(mousePosition);
            Color closeColor = isHovered ? Color.Lerp(UiTheme.DangerClay, Color.White, 0.25f) : UiTheme.ButtonFace;
            UiPrimitives.Fill(spriteBatch, pixel, close, closeColor);
            UiPrimitives.Border(spriteBatch, pixel, close, 1, UiTheme.ButtonShadow);
            Vector2 closeSize = font.MeasureString("X");
            spriteBatch.DrawString(font, "X", new Vector2(close.Center.X - closeSize.X / 2f, close.Center.Y - closeSize.Y / 2f), Color.Black);
        }
    }
}
