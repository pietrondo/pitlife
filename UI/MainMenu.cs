using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI;

public enum MenuAction
{
    None,
    StartGame,
    ToggleFullscreen,
    Exit
}

public sealed class MainMenu
{
    private readonly UiWindow _window = new("MENU PRINCIPALE");
    private readonly UiButton[] _mainButtons =
    [
        new("INIZIA"),
        new("OPZIONI"),
        new("ESCI") { IsDestructive = true }
    ];
    private readonly UiButton[] _optionButtons =
    [
        new("SCHERMO INTERO: NO"),
        new("INDIETRO")
    ];

    private bool _showOptions;
    private bool _inputReady;
    private int _focusedIndex;

    public MenuAction Update(
        MouseState mouse,
        MouseState previousMouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int viewportWidth,
        int viewportHeight,
        bool isFullscreen)
    {
        Layout(viewportWidth, viewportHeight);
        _optionButtons[0].Text = $"SCHERMO INTERO: {(isFullscreen ? "SI" : "NO")}";

        if (!_inputReady)
        {
            _inputReady = keyboard.IsKeyUp(Keys.Enter) &&
                          keyboard.IsKeyUp(Keys.Space) &&
                          mouse.LeftButton == ButtonState.Released;
            return MenuAction.None;
        }

        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].IsHovered(mouse))
                _focusedIndex = i;
        }

        if (Pressed(keyboard, previousKeyboard, Keys.Up))
            _focusedIndex = (_focusedIndex - 1 + buttons.Length) % buttons.Length;
        if (Pressed(keyboard, previousKeyboard, Keys.Down))
            _focusedIndex = (_focusedIndex + 1) % buttons.Length;

        if (_showOptions && Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            _showOptions = false;
            _focusedIndex = 1;
            return MenuAction.None;
        }

        int activated = -1;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].WasClicked(mouse, previousMouse))
                activated = i;
        }
        if (Pressed(keyboard, previousKeyboard, Keys.Enter) || Pressed(keyboard, previousKeyboard, Keys.Space))
            activated = _focusedIndex;

        if (activated < 0)
            return MenuAction.None;

        if (_showOptions)
        {
            if (activated == 0)
                return MenuAction.ToggleFullscreen;

            _showOptions = false;
            _focusedIndex = 1;
            return MenuAction.None;
        }

        return activated switch
        {
            0 => MenuAction.StartGame,
            1 => OpenOptions(),
            2 => MenuAction.Exit,
            _ => MenuAction.None
        };
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Texture2D? logo,
        int viewportWidth,
        int viewportHeight)
    {
        Layout(viewportWidth, viewportHeight);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), UiTheme.MenuScrim);

        int logoSize = viewportHeight < 650 ? 96 : 144;
        int logoY = viewportHeight < 650 ? 16 : 28;
        if (logo != null)
        {
            var logoBounds = new Rectangle(viewportWidth / 2 - logoSize / 2, logoY, logoSize, logoSize);
            spriteBatch.Draw(logo, logoBounds, Color.White);
        }

        _window.Draw(spriteBatch, pixel, font);
        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        MouseState mouse = Mouse.GetState();
        for (int i = 0; i < buttons.Length; i++)
            buttons[i].Draw(spriteBatch, pixel, font, mouse, i == _focusedIndex);

        string hint = _showOptions ? "INVIO: seleziona   ESC: indietro" : "FRECCE: naviga   INVIO: seleziona";
        Vector2 hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint, new Vector2(viewportWidth / 2f - hintSize.X / 2f, viewportHeight - 28), UiTheme.MutedStone);
    }

    private MenuAction OpenOptions()
    {
        _showOptions = true;
        _focusedIndex = 0;
        return MenuAction.None;
    }

    private void Layout(int viewportWidth, int viewportHeight)
    {
        int panelWidth = Math.Min(400, viewportWidth - 32);
        int panelHeight = _showOptions ? 220 : 292;
        int logoSize = viewportHeight < 650 ? 96 : 144;
        int logoY = viewportHeight < 650 ? 16 : 28;
        int panelY = Math.Max(logoY + logoSize + 12, (viewportHeight - panelHeight) / 2 + 48);
        panelY = Math.Min(panelY, viewportHeight - panelHeight - 48);
        _window.Bounds = new Rectangle(viewportWidth / 2 - panelWidth / 2, panelY, panelWidth, panelHeight);
        _window.Title = _showOptions ? "OPZIONI" : "MENU PRINCIPALE";

        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        int buttonWidth = panelWidth - 48;
        int buttonHeight = 52;
        int gap = 12;
        int startY = _window.Bounds.Y + 60;
        for (int i = 0; i < buttons.Length; i++)
        {
            buttons[i].Bounds = new Rectangle(
                viewportWidth / 2 - buttonWidth / 2,
                startY + i * (buttonHeight + gap),
                buttonWidth,
                buttonHeight);
        }
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);
}
