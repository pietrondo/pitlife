using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;

namespace PitLife.UI.Windows;

public class CataclysmWindow : UiWindow
{
    private readonly UiButton[] _cataclysmButtons;

    public string? SelectedCataclysm { get; set; }

    public CataclysmWindow(string title, string id) : base(title, id)
    {
        _cataclysmButtons = new[]
        {
            new UiButton(I18n.T("cata.asteroid")) { Tag = "Asteroid" },
            new UiButton(I18n.T("cata.iceage")) { Tag = "IceAge" },
            new UiButton(I18n.T("cata.supervolcano")) { Tag = "Supervolcano" },
            new UiButton(I18n.T("cata.earthquake")) { Tag = "Earthquake" },
            new UiButton(I18n.T("cata.drought")) { Tag = "Drought" },
            new UiButton(I18n.T("cata.flood")) { Tag = "Flood" }
        };
    }

    public void RefreshText()
    {
        _cataclysmButtons[0].Text = I18n.T("cata.asteroid");
        _cataclysmButtons[1].Text = I18n.T("cata.iceage");
        _cataclysmButtons[2].Text = I18n.T("cata.supervolcano");
        _cataclysmButtons[3].Text = I18n.T("cata.earthquake");
        _cataclysmButtons[4].Text = I18n.T("cata.drought");
        _cataclysmButtons[5].Text = I18n.T("cata.flood");
    }

    public void DrawContent(SpriteBatch sb, Texture2D pixel, SpriteFont font)
    {
        var content = ContentBounds;
        var y = content.Y;
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

        for (var i = 0; i < _cataclysmButtons.Length; i++)
        {
            var btn = _cataclysmButtons[i];
            btn.Bounds = new Rectangle(content.X, y, content.Width, 22);
            var sel = SelectedCataclysm == (string)btn.Tag!;
            btn.Draw(sb, pixel, font, mouse, sel);
            y += 26;
        }
        if (!string.IsNullOrEmpty(SelectedCataclysm))
            sb.DrawString(font, I18n.T("cata.placeHint"), new Vector2(content.X, content.Bottom - 20), new Color(255, 200, 100));
    }

    public bool HandleCataclysmClick(MouseState mouse, MouseState prevMouse)
    {
        for (var i = 0; i < _cataclysmButtons.Length; i++)
        {
            if (_cataclysmButtons[i].WasClicked(mouse, prevMouse))
            {
                SelectedCataclysm = (string)_cataclysmButtons[i].Tag!;
                return true;
            }
        }
        return false;
    }
}
