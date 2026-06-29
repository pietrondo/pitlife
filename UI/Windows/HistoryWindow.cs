using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PitLife.UI.Windows;

public class HistoryWindow : UiWindow
{
    private bool _historyShowPlants = true;
    private bool _historyShowHerbivores = true;
    private bool _historyShowCarnivores = true;
    private bool _historyShowOmnivores = true;

    private readonly UiButton _filterPButton = new("P");
    private readonly UiButton _filterHButton = new("H");
    private readonly UiButton _filterCButton = new("C");
    private readonly UiButton _filterOButton = new("O");

    public HistoryWindow(string title, string id) : base(title, id)
    {
    }

    public void HandleFilterClick(MouseState mouse, MouseState previousMouse)
    {
        if (_filterPButton.WasClicked(mouse, previousMouse)) _historyShowPlants = !_historyShowPlants;
        if (_filterHButton.WasClicked(mouse, previousMouse)) _historyShowHerbivores = !_historyShowHerbivores;
        if (_filterCButton.WasClicked(mouse, previousMouse)) _historyShowCarnivores = !_historyShowCarnivores;
        if (_filterOButton.WasClicked(mouse, previousMouse)) _historyShowOmnivores = !_historyShowOmnivores;
    }

    public void DrawContent(
        SpriteBatch sb,
        Texture2D pixel,
        SpriteFont font,
        MouseState mouse,
        PopSnapshot[] popHistory,
        int popHistoryCount,
        float[] tempHistory,
        int tempHistoryCount)
    {
        var content = ContentBounds;
        var needed = DrawHistoryPanel(sb, pixel, font, content, mouse, popHistory, popHistoryCount, tempHistory, tempHistoryCount);
        var totalH = needed + 72;
        if (totalH != Bounds.Height)
        {
            Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, totalH);
        }
    }

    private int DrawHistoryPanel(
        SpriteBatch sb,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        MouseState mouse,
        PopSnapshot[] popHistory,
        int popHistoryCount,
        float[] tempHistory,
        int tempHistoryCount)
    {
        var startY = content.Y;
        var y = startY;

        var btnW = 30;
        var x = content.X;

        _filterPButton.Bounds = new Rectangle(x, y, btnW, 24); x += btnW + 8;
        _filterHButton.Bounds = new Rectangle(x, y, btnW, 24); x += btnW + 8;
        _filterCButton.Bounds = new Rectangle(x, y, btnW, 24); x += btnW + 8;
        _filterOButton.Bounds = new Rectangle(x, y, btnW, 24);

        _filterPButton.Draw(sb, pixel, font, mouse, false, _historyShowPlants ? UiTheme.MossSignal : null);
        _filterHButton.Draw(sb, pixel, font, mouse, false, _historyShowHerbivores ? UiTheme.LakeBlue : null);
        _filterCButton.Draw(sb, pixel, font, mouse, false, _historyShowCarnivores ? UiTheme.DangerClay : null);
        _filterOButton.Draw(sb, pixel, font, mouse, false, _historyShowOmnivores ? UiTheme.WarmParchment : null);

        y += 32;

        DrawLine(sb, font, content.X, y, "Population", UiTheme.MossSignal);
        y += 20;

        if (popHistoryCount >= 3)
        {
            if (_historyShowPlants) DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 0, UiTheme.MossSignal);
            if (_historyShowHerbivores) DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 1, UiTheme.LakeBlue);
            if (_historyShowCarnivores) DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 2, UiTheme.DangerClay);
            if (_historyShowOmnivores) DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 3, UiTheme.WarmParchment);
        }
        y += 60; // Sparkline area height

        DrawLine(sb, font, content.X, y, "Climate Temperature", UiTheme.LakeBlue);
        y += 20;

        if (tempHistoryCount >= 3)
        {
            DrawTempSparkline(sb, pixel, content.X, y, content.Width, tempHistory, tempHistoryCount);
        }
        y += 60;

        return y - startY;
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

    private static void DrawSparkline(SpriteBatch sb, Texture2D pixel, int baseX, int baseY, int width,
        PopSnapshot[] history, int count, int field, Color color)
    {
        var w = width - 8;
        var h = 10;
        var xStep = count > 1 ? (float)w / (count - 1) : 0;

        var maxVal = 1;
        for (var i = 0; i < count; i++)
        {
            var v = field switch { 0 => history[i].Plants, 1 => history[i].Herbivores, 2 => history[i].Carnivores, _ => history[i].Omnivores };
            if (v > maxVal) maxVal = v;
        }
        if (maxVal < 1) maxVal = 1;

        for (var i = 1; i < count; i++)
        {
            var v0 = field switch { 0 => history[i - 1].Plants, 1 => history[i - 1].Herbivores, 2 => history[i - 1].Carnivores, _ => history[i - 1].Omnivores };
            var v1 = field switch { 0 => history[i].Plants, 1 => history[i].Herbivores, 2 => history[i].Carnivores, _ => history[i].Omnivores };
            var x0 = baseX + (int)((i - 1) * xStep);
            var y0 = baseY + h - (int)(v0 * h / (float)maxVal);
            var x1 = baseX + (int)(i * xStep);
            var y1 = baseY + h - (int)(v1 * h / (float)maxVal);
            DrawLineSegment(sb, pixel, new Point(x0, y0), new Point(x1, y1), color);
        }
    }

    private static void DrawLineSegment(SpriteBatch sb, Texture2D pixel, Point p0, Point p1, Color color)
    {
        int x0 = p0.X, y0 = p0.Y;
        int x1 = p1.X, y1 = p1.Y;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            sb.Draw(pixel, new Rectangle(x0, y0, 1, 1), color);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawTempSparkline(SpriteBatch sb, Texture2D pixel, int baseX, int baseY, int width,
        float[] history, int count)
    {
        int w = width - 8, h = 12;
        var xStep = count > 1 ? (float)w / (count - 1) : 0;
        float min = float.MaxValue, max = float.MinValue;
        for (var i = 0; i < count; i++) { if (history[i] < min) min = history[i]; if (history[i] > max) max = history[i]; }
        var range = max - min; if (range < 1f) range = 1f;
        for (var i = 1; i < count; i++)
        {
            float v0 = (history[i - 1] - min) / range, v1 = (history[i] - min) / range;
            Color c0 = Color.Lerp(new Color(80, 120, 220), new Color(230, 80, 40), v0);
            Color c1 = Color.Lerp(new Color(80, 120, 220), new Color(230, 80, 40), v1);
            int x0 = baseX + (int)((i - 1) * xStep), y0 = baseY + h - (int)(v0 * h);
            int x1 = baseX + (int)(i * xStep), y1 = baseY + h - (int)(v1 * h);
            DrawLineSegment(sb, pixel, new Point(x0, y0), new Point(x1, y1), c0);
        }
    }
}
