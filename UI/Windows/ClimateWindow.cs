using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI.Windows;

public class ClimateWindow : UiWindow
{
    private readonly StringBuilder _tempSb = new(32);
    private readonly StringBuilder _valueSb = new(16);
    private readonly System.Collections.Generic.List<string> _cachedTranslatedEvents = [];
    private string? _lastFirstEvent;
    private string? _lastLastEvent;
    private int _lastEventCount;

    public ClimateWindow(string title, string id) : base(title, id)
    {
    }

    public void DrawContent(
        SpriteBatch sb,
        Texture2D pixel,
        SpriteFont font,
        ClimateSystem? climate,
        World? world,
        Point? hoverTile,
        Point? selectedTile,
        int plantCount,
        int herbivoreCount,
        int carnivoreCount,
        int omnivoreCount,
        PopSnapshot[] popHistory,
        int popHistoryCount,
        float[] tempHistory,
        int tempHistoryCount)
    {
        var content = ContentBounds;

        if (climate == null || world == null)
        {
            DrawLine(sb, font, content.X, content.Y, "Climate data unavailable", UiTheme.MutedStone);
            return;
        }

        var y = content.Y;

        DrawClimateGlobalData(sb, pixel, font, content, climate, tempHistory, tempHistoryCount, ref y);
        DrawClimateLocalData(sb, font, content, climate, world, hoverTile, selectedTile, ref y);
        DrawClimatePopulationData(sb, pixel, font, content, plantCount, herbivoreCount, carnivoreCount, omnivoreCount, popHistory, popHistoryCount, ref y);
        DrawClimateEventsData(sb, font, content, ref y);
    }

    private void DrawClimateGlobalData(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content, ClimateSystem climate, float[] tempHistory, int tempHistoryCount, ref int y)
    {
        var seasonKey = climate.CurrentSeason switch
        {
            Season.Spring => "season.Spring",
            Season.Summer => "season.Summer",
            Season.Autumn => "season.Autumn",
            Season.Winter => "season.Winter",
            _ => "season.Spring"
        };
        DrawLine(sb, font, content.X, y, I18n.Format("climate.season", I18n.T(seasonKey)), UiTheme.MossSignal);
        y += 20;

        float progressW = content.Width - 8;
        DrawProgress(sb, pixel, new Rectangle(content.X, y, (int)progressW, 10), climate.SeasonProgress);
        y += 16;

        _tempSb.Clear();
        _tempSb.Append((int)(20f + climate.TemperatureModifier * 20f)).Append("°C");
        var tempNorm = Math.Clamp((climate.TemperatureModifier + 0.15f) / 0.3f, 0f, 1f);
        _valueSb.Clear();
        var tempStr = I18n.T("climate.temperature");
        var idx = tempStr.IndexOf("{0}");
        if (idx >= 0)
        {
            _valueSb.Append(tempStr.AsSpan(0, idx));
            _valueSb.Append(_tempSb);
            _valueSb.Append(tempStr.AsSpan(idx + 3));
        }
        else
        {
            _valueSb.Append(tempStr);
        }
        DrawLine(sb, font, content.X, y, _valueSb,
            Color.Lerp(new Color(100, 150, 255), new Color(255, 120, 40), tempNorm));
        y += 18;

        if (tempHistoryCount >= 3)
        {
            DrawTempSparkline(sb, pixel, content.X, y, content.Width, tempHistory, tempHistoryCount);
            y += 12;
        }
    }

    private void DrawClimateLocalData(SpriteBatch sb, SpriteFont font, Rectangle content, ClimateSystem climate, World world, Point? hoverTile, Point? selectedTile, ref int y)
    {
        DrawLocalTileData(sb, font, content, climate, world, hoverTile, selectedTile, ref y);
        DrawOrbitalData(sb, font, content, climate, ref y);
        DrawWindData(sb, font, content, climate, ref y);
        DrawExtremeEventData(sb, font, content, climate, ref y);
    }

    private static void AppendFormattedLocal(StringBuilder sb, string format, int lx, int ly, string biomeName, int localTemp)
    {
        int p = 0;
        while (p < format.Length)
        {
            int next = format.IndexOf('{', p);
            if (next < 0) { sb.Append(format.AsSpan(p)); break; }
            sb.Append(format.AsSpan(p, next - p));
            int end = format.IndexOf('}', next);
            if (end > next)
            {
                var token = format.AsSpan(next + 1, end - next - 1);
                var colon = token.IndexOf(':');
                var indexSpan = colon >= 0 ? token.Slice(0, colon) : token;
                if (int.TryParse(indexSpan, out int index))
                {
                    if (index == 0) sb.Append(lx);
                    else if (index == 1) sb.Append(ly);
                    else if (index == 2) sb.Append(biomeName);
                    else if (index == 3) sb.Append(localTemp);
                }
                p = end + 1;
            }
            else
            {
                sb.Append('{');
                p = next + 1;
            }
        }
    }

    private static void AppendFormattedSeason(StringBuilder sb, string format, string localSeasonName)
    {
        int p = 0;
        while (p < format.Length)
        {
            int next = format.IndexOf('{', p);
            if (next < 0) { sb.Append(format.AsSpan(p)); break; }
            sb.Append(format.AsSpan(p, next - p));
            int end = format.IndexOf('}', next);
            if (end > next)
            {
                var token = format.AsSpan(next + 1, end - next - 1);
                var colon = token.IndexOf(':');
                var indexSpan = colon >= 0 ? token.Slice(0, colon) : token;
                if (int.TryParse(indexSpan, out int index))
                {
                    if (index == 0) sb.Append(localSeasonName);
                }
                p = end + 1;
            }
            else
            {
                sb.Append('{');
                p = next + 1;
            }
        }
    }

    private static void AppendFormattedOrbit(StringBuilder sb, string format, float dist, float angle, float speed)
    {
        int p = 0;
        while (p < format.Length)
        {
            int next = format.IndexOf('{', p);
            if (next < 0) { sb.Append(format.AsSpan(p)); break; }
            sb.Append(format.AsSpan(p, next - p));
            int end = format.IndexOf('}', next);
            if (end > next)
            {
                var token = format.AsSpan(next + 1, end - next - 1);
                var colon = token.IndexOf(':');
                var indexSpan = colon >= 0 ? token.Slice(0, colon) : token;
                if (int.TryParse(indexSpan, out int index))
                {
                    if (index == 0) sb.Append($"{dist:F3}");
                    else if (index == 1) sb.Append($"{angle:F0}");
                    else if (index == 2) sb.Append($"{speed:F1}");
                }
                p = end + 1;
            }
            else
            {
                sb.Append('{');
                p = next + 1;
            }
        }
    }

    private static void AppendFormattedWind(StringBuilder sb, string format, float speed, float dir)
    {
        int p = 0;
        while (p < format.Length)
        {
            int next = format.IndexOf('{', p);
            if (next < 0) { sb.Append(format.AsSpan(p)); break; }
            sb.Append(format.AsSpan(p, next - p));
            int end = format.IndexOf('}', next);
            if (end > next)
            {
                var token = format.AsSpan(next + 1, end - next - 1);
                var colon = token.IndexOf(':');
                var indexSpan = colon >= 0 ? token.Slice(0, colon) : token;
                if (int.TryParse(indexSpan, out int index))
                {
                    if (index == 0) sb.Append($"{speed:F1}");
                    else if (index == 1) sb.Append($"{dir:F0}");
                }
                p = end + 1;
            }
            else
            {
                sb.Append('{');
                p = next + 1;
            }
        }
    }

    private static void AppendFormattedExtreme(StringBuilder sb, string format, string extremeName)
    {
        int p = 0;
        while (p < format.Length)
        {
            int next = format.IndexOf('{', p);
            if (next < 0) { sb.Append(format.AsSpan(p)); break; }
            sb.Append(format.AsSpan(p, next - p));
            int end = format.IndexOf('}', next);
            if (end > next)
            {
                var token = format.AsSpan(next + 1, end - next - 1);
                var colon = token.IndexOf(':');
                var indexSpan = colon >= 0 ? token.Slice(0, colon) : token;
                if (int.TryParse(indexSpan, out int index))
                {
                    if (index == 0) sb.Append(extremeName);
                }
                p = end + 1;
            }
            else
            {
                sb.Append('{');
                p = next + 1;
            }
        }
    }

    private void DrawLocalTileData(SpriteBatch sb, SpriteFont font, Rectangle content, ClimateSystem climate, World world, Point? hoverTile, Point? selectedTile, ref int y)
    {
        Point h = hoverTile ?? selectedTile ?? new Point(world.Width / 2, world.Height / 2);
        var lx = Math.Clamp(h.X, 0, world.Width - 1);
        var ly = Math.Clamp(h.Y, 0, world.Height - 1);
        Tile localTile = world.GetTile(lx, ly);
        var localTemp = climate.GetTileTemperature(localTile, ly, world.Height);
        Season localSeason = climate.GetLocalSeason(ly, world.Height);
        var localSeasonKey = localSeason switch
        {
            Season.Spring => "season.Spring",
            Season.Summer => "season.Summer",
            Season.Autumn => "season.Autumn",
            Season.Winter => "season.Winter",
            _ => "season.Spring"
        };
        var localSeasonName = I18n.T(localSeasonKey);
        var biomeName = I18n.T($"biome.{localTile.Biome}");
        _tempSb.Clear();
        AppendFormattedLocal(_tempSb, I18n.T("climate.local"), lx, ly, biomeName, (int)localTemp);
        DrawLine(sb, font, content.X, y,
            _tempSb, UiTheme.WarmParchment);
        y += 18;
        _tempSb.Clear();
        AppendFormattedSeason(_tempSb, I18n.T("climate.localseason"), localSeasonName);
        DrawLine(sb, font, content.X, y,
            _tempSb, UiTheme.MossSignal);
        y += 22;
    }

    private void DrawOrbitalData(SpriteBatch sb, SpriteFont font, Rectangle content, ClimateSystem climate, ref int y)
    {
        _tempSb.Clear();
        AppendFormattedOrbit(_tempSb, I18n.T("climate.orbit"), climate.SunDistanceAU, climate.OrbitalAngle * 180f / MathF.PI, climate.OrbitalSpeedKmS);
        DrawLine(sb, font, content.X, y,
            _tempSb,
            UiTheme.WarmParchment);
        y += 18;
    }

    private void DrawWindData(SpriteBatch sb, SpriteFont font, Rectangle content, ClimateSystem climate, ref int y)
    {
        _tempSb.Clear();
        AppendFormattedWind(_tempSb, I18n.T("climate.wind"), climate.WindSpeed, climate.WindDirection * 180f / MathF.PI);
        DrawLine(sb, font, content.X, y,
            _tempSb,
            UiTheme.WarmParchment);
        y += 18;
    }

    private void DrawExtremeEventData(SpriteBatch sb, SpriteFont font, Rectangle content, ClimateSystem climate, ref int y)
    {
        if (climate.IsExtremeEvent)
        {
            _tempSb.Clear();
            AppendFormattedExtreme(_tempSb, I18n.T("climate.extreme"), climate.ExtremeEventName);
            DrawLine(sb, font, content.X, y,
                _tempSb,
                climate.ExtremeEventName == "Heatwave" ? new Color(255, 140, 40) : new Color(120, 180, 255));
            y += 18;
        }
    }

    private void DrawClimatePopulationData(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content, int plantCount, int herbivoreCount, int carnivoreCount, int omnivoreCount, PopSnapshot[] popHistory, int popHistoryCount, ref int y)
    {
        y += 6;
        DrawLine(sb, font, content.X, y, I18n.T("climate.populations"), UiTheme.MossSignal);
        y += 20;
        var totalPop = plantCount + herbivoreCount + carnivoreCount + omnivoreCount;
        DrawInlineBar(sb, pixel, font, content.X, y, "P", plantCount, totalPop, UiTheme.MossSignal);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "H", herbivoreCount, totalPop, UiTheme.LakeBlue);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "C", carnivoreCount, totalPop, UiTheme.DangerClay);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "O", omnivoreCount, totalPop, UiTheme.WarmParchment);
        y += 20;

        if (popHistoryCount >= 3)
        {
            DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 0, UiTheme.MossSignal);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 1, UiTheme.LakeBlue);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 2, UiTheme.DangerClay);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, popHistory, popHistoryCount, 3, UiTheme.WarmParchment);
            y += 20;
        }
    }

    private void DrawClimateEventsData(SpriteBatch sb, SpriteFont font, Rectangle content, ref int y)
    {
        DrawLine(sb, font, content.X, y, I18n.T("climate.events"), UiTheme.MossSignal);
        y += 20;
        var recent = Core.Logger.RecentEvents;

        bool changed = recent.Count != _lastEventCount;
        if (recent.Count > 0 && !changed)
        {
            if (recent[0] != _lastFirstEvent || recent[^1] != _lastLastEvent)
            {
                changed = true;
            }
        }

        if (changed || (_cachedTranslatedEvents.Count == 0 && recent.Count > 0))
        {
            _lastEventCount = recent.Count;
            if (recent.Count > 0)
            {
                _lastFirstEvent = recent[0];
                _lastLastEvent = recent[^1];
            }

            _cachedTranslatedEvents.Clear();
            var maxShow = Math.Min(5, recent.Count);
            for (var i = recent.Count - maxShow; i < recent.Count; i++)
            {
                var ev = TranslateEvent(recent[i]);
                if (ev.Length > 44) ev = ev.Substring(0, 44);
                _cachedTranslatedEvents.Add(ev);
            }
        }

        foreach (var ev in _cachedTranslatedEvents)
        {
            DrawLine(sb, font, content.X, y, ev, new Color(180, 160, 140));
            y += 16;
        }
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

    private static void DrawProgress(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, float value)
    {
        UiPrimitives.Fill(spriteBatch, pixel, bounds, UiTheme.DeepGrove);
        var width = (int)((bounds.Width - 4) * MathHelper.Clamp(value, 0f, 1f));
        if (width > 0)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), UiTheme.MossSignal);
        UiPrimitives.Border(spriteBatch, pixel, bounds, 2, UiTheme.BarkEdge);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, StringBuilder text, Color color)
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

    private static string TranslateEvent(string ev)
    {
        ev = ev.Replace("[SEASON]", I18n.T("evt.season"))
               .Replace("[CATACLYSM]", I18n.T("evt.cataclysm"))
               .Replace("[CLIMATE]", I18n.T("evt.climate"))
               .Replace("[DEATH]", I18n.T("evt.death"))
               .Replace("[SPAWN]", I18n.T("evt.spawn"))
               .Replace("[TERRAIN]", I18n.T("evt.terrain"));
        ev = ev.Replace("Season changed to ", I18n.T("evt.msg.season"))
               .Replace(" at T=", " " + I18n.T("evt.msg.at") + "=")
               .Replace(" at (", " " + I18n.T("evt.msg.at") + " (")
               .Replace("Player ", I18n.T("evt.msg.player") + " ")
               .Replace("MASS EXTINCTION: ", I18n.T("evt.msg.extinction") + " ")
               .Replace("Extreme event '", I18n.T("evt.msg.extreme") + " '")
               .Replace("' started", "' " + I18n.T("evt.msg.started"))
               .Replace("' ended", "' " + I18n.T("evt.msg.ended"))
               .Replace("Player spawned ", I18n.T("evt.msg.spawned") + " ")
               .Replace("crater at ", I18n.T("evt.msg.crater") + " ")
               .Replace("r=", I18n.T("evt.msg.radius") + "=")
               .Replace("duration=", I18n.T("evt.msg.duration") + "=")
               .Replace("Cataclysm ended", I18n.T("evt.msg.cataend"))
               .Replace("died at age ", I18n.T("evt.msg.died"))
               .Replace("(dist=", "(" + I18n.T("evt.msg.dist") + "=");

        foreach (var sp in SpeciesRegistry.All)
        {
            var tr = I18n.Species(sp);
            if (tr != sp && ev.Contains(sp))
            {
                var idx = ev.IndexOf(sp);
                if (idx > 0 && !char.IsLetterOrDigit(ev[idx - 1]))
                    ev = ev.Replace(sp, tr);
            }
        }
        return ev;
    }
}
