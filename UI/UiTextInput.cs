using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Linq;

namespace PitLife.UI;

public sealed class UiTextInput
{
    public string Text { get; private set; } = "";
    public string Placeholder { get; set; } = "";
    public Rectangle Bounds { get; set; }
    public int MaxLength { get; set; } = 20;
    public bool IsNumericOnly { get; set; } = false;
    public bool IsFocused { get; set; }

    private double _cursorBlinkTimer;
    private bool _showCursor;
    private Keys[] _prevPressedKeys = Array.Empty<Keys>();

    public void Update(KeyboardState keyboard, KeyboardState previousKeyboard, MouseState mouse, MouseState previousMouse)
    {
        // Check focus
        if (mouse.LeftButton == ButtonState.Pressed &&
            previousMouse.LeftButton == ButtonState.Released)
        {
            IsFocused = Bounds.Contains(mouse.Position);
        }

        if (!IsFocused) return;

        // Handle text input
        var pressedKeys = keyboard.GetPressedKeys();
        var newlyPressed = pressedKeys.Where(k => !_prevPressedKeys.Contains(k)).ToArray();

        foreach (var key in newlyPressed)
        {
            if (key == Keys.Back && Text.Length > 0)
            {
                Text = Text.Substring(0, Text.Length - 1);
            }
            else if (key == Keys.Delete)
            {
                Text = "";
            }
            else if (key == Keys.Space && !IsNumericOnly && Text.Length < MaxLength)
            {
                Text += " ";
            }
            else if (key >= Keys.D0 && key <= Keys.D9 && Text.Length < MaxLength)
            {
                Text += (key - Keys.D0).ToString();
            }
            else if (key >= Keys.NumPad0 && key <= Keys.NumPad9 && Text.Length < MaxLength)
            {
                Text += (key - Keys.NumPad0).ToString();
            }
            else if (key >= Keys.A && key <= Keys.Z && !IsNumericOnly && Text.Length < MaxLength)
            {
                bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                char c = shift ? char.ToUpper((char)('A' + (key - Keys.A))) : char.ToLower((char)('A' + (key - Keys.A)));
                Text += c;
            }
            else if (!IsNumericOnly && Text.Length < MaxLength)
            {
                bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);
                Text += key switch
                {
                    Keys.OemPeriod => ".",
                    Keys.OemQuestion => shift ? "?" : "/",
                    Keys.OemMinus => shift ? "_" : "-",
                    Keys.OemBackslash => "\\",
                    _ => ""
                };
            }
        }

        _prevPressedKeys = pressedKeys;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, MouseState mouse)
    {
        bool hovered = Bounds.Contains(mouse.Position);
        Color fill = IsFocused ? UiTheme.ForestNight : (hovered ? UiTheme.DeepGrove : UiTheme.Shadow);
        Color border = IsFocused ? UiTheme.MossSignal : UiTheme.BarkEdge;

        // Background
        UiPrimitives.Fill(spriteBatch, pixel, Bounds, fill);
        UiPrimitives.Border(spriteBatch, pixel, Bounds, 2, border);

        // Text
        string displayText = Text.Length > 0 ? Text : Placeholder;
        Color textColor = Text.Length > 0 ? UiTheme.WarmParchment : UiTheme.MutedStone;

        // Measure and clip text if too long
        Vector2 textSize = font.MeasureString(displayText);
        float scale = 1.0f;
        if (textSize.X * scale > Bounds.Width - 16)
        {
            scale = (Bounds.Width - 16) / textSize.X;
        }

        Vector2 position = new(
            Bounds.X + 8,
            Bounds.Center.Y - (textSize.Y * scale) / 2f);

        spriteBatch.DrawString(font, displayText, position, textColor, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);

        // Cursor
        if (IsFocused && _showCursor)
        {
            _cursorBlinkTimer += 0.05;
            if (_cursorBlinkTimer > 1.0)
            {
                _showCursor = !_showCursor;
                _cursorBlinkTimer = 0;
            }

            if (_showCursor)
            {
                string textBeforeCursor = Text;
                Vector2 cursorPos = font.MeasureString(textBeforeCursor) * scale;
                int cursorX = Bounds.X + 8 + (int)cursorPos.X;
                if (cursorX < Bounds.Right - 4)
                {
                    UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(cursorX, Bounds.Y + 6, 2, Bounds.Height - 12), UiTheme.WarmParchment);
                }
            }
        }
        else
        {
            _cursorBlinkTimer = 0;
            _showCursor = true;
        }
    }

    public int GetNumericValue()
    {
        if (int.TryParse(Text, out int result))
            return result;
        return 0;
    }

    public void SetText(string text)
    {
        Text = text.Length > MaxLength ? text.Substring(0, MaxLength) : text;
    }

    public void Clear()
    {
        Text = "";
    }
}
