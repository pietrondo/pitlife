using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;

namespace PitLife.UI;

public sealed class HelpScreen
{
    private readonly UiWindow _window = new(I18n.T("help.title"));
    private readonly UiButton _backButton = new(I18n.T("common.back")) { ShortcutHint = "ESC" };

    private bool _inputReady = true;

    public bool IsActive { get; set; } = false;

    public void Show()
    {
        IsActive = true;
        _inputReady = false;
    }

    public void Hide()
    {
        IsActive = false;
    }

    public bool Update(
        MouseState mouse,
        MouseState previousMouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int viewportWidth,
        int viewportHeight)
    {
        if (!IsActive)
            return false;

        Layout(viewportWidth, viewportHeight);

        if (!_inputReady)
        {
            _inputReady = keyboard.IsKeyUp(Keys.Enter) &&
                          keyboard.IsKeyUp(Keys.Space) &&
                          keyboard.IsKeyUp(Keys.Escape) &&
                          mouse.LeftButton == ButtonState.Released;
            return false;
        }

        if (_backButton.WasClicked(mouse, previousMouse) ||
            Pressed(keyboard, previousKeyboard, Keys.Enter) ||
            Pressed(keyboard, previousKeyboard, Keys.Escape) ||
            Pressed(keyboard, previousKeyboard, Keys.Space))
        {
            Hide();
            return true;
        }

        return false;
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        int viewportWidth,
        int viewportHeight)
    {
        if (!IsActive)
            return;

        Layout(viewportWidth, viewportHeight);

        // Background scrim
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), UiTheme.MenuScrim);

        // Window
        _window.Draw(spriteBatch, pixel, font, true, Mouse.GetState().Position);

        // Draw help content
        DrawHelpContent(spriteBatch, font);

        // Back button
        MouseState mouse = Mouse.GetState();
        _backButton.Draw(spriteBatch, pixel, font, mouse, true);
    }

    private void DrawHelpContent(SpriteBatch spriteBatch, SpriteFont font)
    {
        var contentX = _window.Bounds.X + 24;
        var contentY = _window.Bounds.Y + 48;
        var lineHeight = 20;
        var scale = 0.9f;

        void DrawLine(string text, Color color, bool isHeader = false)
        {
            var lineScale = isHeader ? 1.0f : scale;
            spriteBatch.DrawString(font, text, new Vector2(contentX, contentY), color, 0f, Vector2.Zero, lineScale, SpriteEffects.None, 0f);
            contentY += (int)(lineHeight * lineScale);
        }

        // Title
        DrawLine(I18n.T("help.objectiveTitle"), UiTheme.MossSignal, true);
        contentY += 8;
        DrawLine(I18n.T("help.objective"), UiTheme.WarmParchment);
        contentY += 16;

        // Controls
        DrawLine(I18n.T("help.controlsTitle"), UiTheme.MossSignal, true);
        contentY += 8;
        DrawLine(I18n.T("help.controls.movement"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.controls.speed"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.controls.interface"), UiTheme.WarmParchment);
        contentY += 16;

        // Spawn
        DrawLine(I18n.T("help.spawnTitle"), UiTheme.MossSignal, true);
        contentY += 8;
        DrawLine(I18n.T("help.spawn"), UiTheme.WarmParchment);
        contentY += 16;

        // Creature types
        DrawLine(I18n.T("help.creaturesTitle"), UiTheme.MossSignal, true);
        contentY += 8;
        DrawLine(I18n.T("help.creatures.plants"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.creatures.herbivores"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.creatures.carnivores"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.creatures.omnivores"), UiTheme.WarmParchment);
        contentY += 16;

        // Tips
        DrawLine(I18n.T("help.tipsTitle"), UiTheme.MossSignal, true);
        contentY += 8;
        DrawLine(I18n.T("help.tips.balance"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.tips.seed"), UiTheme.WarmParchment);
        DrawLine(I18n.T("help.tips.observe"), UiTheme.WarmParchment);
    }

    private void Layout(int viewportWidth, int viewportHeight)
    {
        var panelWidth = Math.Min(700, viewportWidth - 48);
        var panelHeight = Math.Min(600, viewportHeight - 64);
        var panelX = (viewportWidth - panelWidth) / 2;
        var panelY = (viewportHeight - panelHeight) / 2;

        _window.Bounds = new Rectangle(panelX, panelY, panelWidth, panelHeight);
        _window.Title = I18n.T("help.title");

        // Back button at bottom center
        var buttonWidth = 120;
        var buttonHeight = 44;
        _backButton.Bounds = new Rectangle(
            viewportWidth / 2 - buttonWidth / 2,
            panelY + panelHeight - buttonHeight - 16,
            buttonWidth,
            buttonHeight);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);
}
