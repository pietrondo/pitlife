using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI.Windows;

public readonly record struct StatisticsData(
    int Plants,
    int Herbivores,
    int Carnivores,
    int Omnivores,
    float Time,
    bool Paused,
    float Speed,
    EcosystemMetrics? Metrics
);

public class StatisticsWindow : UiWindow
{
    private readonly StringBuilder _speciesSb = new(64);
    private readonly StringBuilder _valueSb = new(16);

    public StatisticsWindow(string title, string id) : base(title, id)
    {
    }

    public void DrawContent(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        in StatisticsData data)
    {
        var content = ContentBounds;
        var needed = DrawStatistics(spriteBatch, pixel, font, content, in data);
        var totalH = needed + 72;
        if (totalH != Bounds.Height)
        {
            Bounds = new Rectangle(Bounds.X, Bounds.Y, Bounds.Width, totalH);
        }
    }

    private int DrawStatistics(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        in StatisticsData data)
    {
        var total = data.Plants + data.Herbivores + data.Carnivores + data.Omnivores;
        var y = content.Y;
        DrawLine(spriteBatch, font, content.X, y, I18n.Format("stats.time", data.Time), UiTheme.WarmParchment);
        y += 18;
        DrawLine(spriteBatch, font, content.X, y,
            data.Paused ? I18n.T("stats.paused") : I18n.Format("stats.speed", data.Speed),
            data.Paused ? UiTheme.DangerClay : UiTheme.MossSignal);
        y += 18;
        DrawLine(spriteBatch, font, content.X, y, I18n.Format("stats.total", total), UiTheme.WarmParchment);
        y += 22;

        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "P", data.Plants, total, UiTheme.MossSignal);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "H", data.Herbivores, total, UiTheme.LakeBlue);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "C", data.Carnivores, total, UiTheme.DangerClay);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "O", data.Omnivores, total, UiTheme.WarmParchment);

        if (data.Metrics != null && data.Metrics.SpeciesPopulations.Count > 0)
        {
            y += 22;
            spriteBatch.DrawString(font, I18n.T("stats.speciesList"),
                new Vector2(content.X, y), UiTheme.MossSignal);
            y += 14;
            var shown = 0;
            foreach (var kvp in data.Metrics.SpeciesPopulations)
            {
                if (shown >= 14) break;
                Color col = kvp.Value > 0 ? UiTheme.WarmParchment : UiTheme.MutedStone;
                _speciesSb.Clear();
                _speciesSb.Append(kvp.Value).Append(' ').Append(I18n.Species(kvp.Key));
                spriteBatch.DrawString(font, _speciesSb, new Vector2(content.X, y), col);
                y += 13;
                shown++;
            }
        }

        return y - content.Y + 8;
    }

    private void DrawInlineBar(SpriteBatch sb, Texture2D pixel, SpriteFont font,
        int x, int y, string label, int value, int total, Color color)
    {
        var barW = (int)(120f * (total > 0 ? value / (float)total : 0));
        var barH = 10;
        sb.DrawString(font, label, new Vector2(x, y - 2), color);
        var bg = new Rectangle(x + 14, y + 1, 122, barH);
        UiPrimitives.Fill(sb, pixel, bg, UiTheme.DeepGrove);
        if (barW > 0)
            UiPrimitives.Fill(sb, pixel, new Rectangle(bg.X, bg.Y, barW, barH), color);
        UiPrimitives.Border(sb, pixel, bg, 1, UiTheme.BarkEdge);

        _valueSb.Clear();
        _valueSb.Append(value);
        sb.DrawString(font, _valueSb, new Vector2(bg.Right + 4, y - 2), UiTheme.WarmParchment);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }
}
