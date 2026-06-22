using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI;

public sealed class CataclysmPanel
{
    public bool IsOpen { get; private set; }
    public string? SelectedType { get; set; }

    private Rectangle _toggleBounds;
    private Rectangle _panelBounds;
    private const int ToggleSize = 44;
    private const int Margin = 10;
    private const int PanelW = 160;
    private const int PanelH = 200;

    private readonly UiButton[] _buttons = new[]
    {
        new UiButton("Asteroid") { Tag = "Asteroid" },
        new UiButton("Ice Age") { Tag = "IceAge" },
        new UiButton("Supervolcano") { Tag = "Supervolcano" },
        new UiButton("Earthquake") { Tag = "Earthquake" },
        new UiButton("Drought") { Tag = "Drought" },
        new UiButton("Flood") { Tag = "Flood" }
    };

    public void Toggle() => IsOpen = !IsOpen;
    public void Close() => IsOpen = false;

    public bool Update(MouseState mouse, MouseState prevMouse)
    {
        if (!IsOpen) return false;

        int y = _panelBounds.Y + 10;
        foreach (var btn in _buttons)
        {
            btn.Bounds = new Rectangle(_panelBounds.X + 8, y, PanelW - 16, 22);
            if (btn.WasClicked(mouse, prevMouse))
            {
                SelectedType = (string)btn.Tag!;
                return true;
            }
            y += 26;
        }
        return false;
    }

    public void Draw(SpriteBatch sb, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        int toggleY = Margin + 10 + ToggleSize + Margin + 10;
        _toggleBounds = new Rectangle(Margin, toggleY, ToggleSize, ToggleSize);

        bool hover = _toggleBounds.Contains(mouse.Position);
        Color bg = IsOpen ? new Color(200, 60, 30, 230) : (hover ? new Color(80, 30, 20, 240) : new Color(60, 20, 10, 200));
        UiPrimitives.Fill(sb, pixel, _toggleBounds, bg);
        UiPrimitives.Border(sb, pixel, _toggleBounds, 2, IsOpen ? UiTheme.DangerClay : UiTheme.BarkEdge);
        var ts = font.MeasureString("C");
        sb.DrawString(font, "C", new Vector2(_toggleBounds.Center.X - ts.X/2, _toggleBounds.Center.Y - ts.Y/2), IsOpen ? Color.White : UiTheme.DangerClay);

        if (!IsOpen) return;

        int panelY = toggleY + ToggleSize + 4;
        _panelBounds = new Rectangle(Margin, panelY, PanelW, PanelH);
        UiPrimitives.Fill(sb, pixel, _panelBounds, new Color(30, 15, 10, 235));
        UiPrimitives.Border(sb, pixel, _panelBounds, 2, UiTheme.DangerClay);

        sb.DrawString(font, "Cataclisma", new Vector2(_panelBounds.X + 8, _panelBounds.Y + 6), UiTheme.DangerClay);

        int y = _panelBounds.Y + 28;
        foreach (var btn in _buttons)
        {
            btn.Bounds = new Rectangle(_panelBounds.X + 8, y, PanelW - 16, 22);
            bool sel = SelectedType == (string)btn.Tag!;
            btn.Draw(sb, pixel, font, mouse, sel);
            y += 26;
        }

        if (!string.IsNullOrEmpty(SelectedType))
            sb.DrawString(font, "Clicca mappa", new Vector2(_panelBounds.X + 8, _panelBounds.Bottom - 18), UiTheme.MossSignal);
    }

    public bool HandleClick(MouseState mouse, MouseState prevMouse)
    {
        if (!IsOpen) return false;
        return _toggleBounds.Contains(mouse.Position) ||
               _panelBounds.Contains(mouse.Position);
    }
}
