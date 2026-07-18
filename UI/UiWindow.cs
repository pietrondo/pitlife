using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PitLife.UI;

public class UiWindow
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
    public Rectangle CollapseButtonBounds => new(TitleBarBounds.X + 8, TitleBarBounds.Center.Y - 10, 20, 20);

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
        var c = isActive ? UiTheme.MossSignal : UiTheme.BarkEdge;

        // Draw shadow
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X + 8, Bounds.Y + 8, Bounds.Width, Bounds.Height), UiTheme.Shadow);
        // Draw window surface
        UiPrimitives.Fill(spriteBatch, pixel, Bounds, UiTheme.ForestNight);

        // Top highlight band
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(Bounds.X + 3, Bounds.Y + 3, Bounds.Width - 6, 4), Color.Lerp(UiTheme.DeepGrove, UiTheme.MossSignal, 0.07f));

        // Draw border: active/focused windows get a bright Moss Signal border, inactive ones get a Bark Edge border
        UiPrimitives.Border(spriteBatch, pixel, Bounds, 3, c);

        // Corner decorations (small L-shapes inside each corner)
        var d = 6;
        var tl = new Point(Bounds.X + 3, Bounds.Y + 3);
        var br = new Point(Bounds.Right - 4, Bounds.Bottom - 4);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(tl.X, tl.Y, 1, d), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(tl.X, tl.Y, d, 1), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(br.X, tl.Y, 1, d), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(br.X - d + 1, tl.Y, d, 1), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(tl.X, br.Y - d + 1, 1, d), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(tl.X, br.Y, d, 1), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(br.X, br.Y - d + 1, 1, d), c);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(br.X - d + 1, br.Y, d, 1), c);

        Rectangle titleBar = TitleBarBounds;
        bool isTitleHovered = titleBar.Contains(mousePosition) && IsDraggable;
        UiPrimitives.Fill(spriteBatch, pixel, titleBar, isTitleHovered ? UiTheme.ForestNight : UiTheme.DeepGrove);

        // Only draw the separator line between title and content if the window is NOT collapsed
        if (!IsCollapsed)
        {
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(titleBar.X, titleBar.Bottom - 2, titleBar.Width, 2), UiTheme.BarkEdge);
        }

        const float scale = 1.15f;
        Vector2 size = font.MeasureString(Title) * scale;
        var position = new Vector2(titleBar.Center.X - size.X / 2f, titleBar.Center.Y - size.Y / 2f);
        spriteBatch.DrawString(font, Title, position, UiTheme.WarmParchment, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        if (IsDraggable)
        {
            var iconRect = CollapseButtonBounds;
            var isHovered = iconRect.Contains(mousePosition);
            var collapseIcon = IsCollapsed ? "[+]" : "[-]";
            Vector2 iconSize = font.MeasureString(collapseIcon) * scale;

            // Apply a color transition/hover state bridging the gap between static text and interactive element
            Color iconColor;
            if (isHovered)
                iconColor = Color.White;
            else if (isTitleHovered)
                iconColor = UiTheme.WarmParchment;
            else
                iconColor = UiTheme.MutedStone;

            spriteBatch.DrawString(font, collapseIcon, new Vector2(titleBar.X + 8, titleBar.Center.Y - iconSize.Y / 2f), iconColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        if (ShowCloseButton)
        {
            Rectangle close = CloseButtonBounds;
            var isHovered = close.Contains(mousePosition);
            // Highlight close button when hovered
            Color closeColor = isHovered ? Color.Lerp(UiTheme.DangerClay, Color.White, 0.25f) : UiTheme.DangerClay;

            UiPrimitives.Fill(spriteBatch, pixel, close, closeColor);
            UiPrimitives.Border(spriteBatch, pixel, close, 2, UiTheme.WarmParchment);
            Vector2 closeSize = font.MeasureString("X");
            spriteBatch.DrawString(font, "X", new Vector2(close.Center.X - closeSize.X / 2f, close.Center.Y - closeSize.Y / 2f), isHovered ? Color.White : UiTheme.WarmParchment);
        }
    }
}
