using System;
using Microsoft.Xna.Framework;

namespace PitLife.Rendering;

public class DayNightCycle
{
    public const float DayLength = 120f;

    public float TimeOfDay { get; private set; }
    public DayPhase Phase { get; private set; }

    public void Update(float totalTime)
    {
        TimeOfDay = totalTime % DayLength;
        Phase = GetPhase(TimeOfDay);
    }

    public static DayPhase GetPhase(float timeOfDay)
    {
        float t = timeOfDay / DayLength;
        return t switch
        {
            < 0.10f => DayPhase.Dawn,
            < 0.45f => DayPhase.Day,
            < 0.55f => DayPhase.Dusk,
            < 0.90f => DayPhase.Night,
            _ => DayPhase.Dawn
        };
    }

    public Color GetOverlayColor()
    {
        float t = TimeOfDay / DayLength;
        return t switch
        {
            < 0.10f => LerpColor(NightColor, DawnColor, t / 0.10f),
            < 0.45f => Color.Transparent,
            < 0.55f => LerpColor(Color.Transparent, DuskColor, (t - 0.45f) / 0.10f),
            < 0.90f => LerpColor(DuskColor, NightColor, (t - 0.55f) / 0.35f),
            _ => LerpColor(NightColor, DawnColor, (t - 0.90f) / 0.10f)
        };
    }

    private static readonly Color NightColor = new(10, 15, 50, 140);
    private static readonly Color DawnColor = new(255, 180, 100, 50);
    private static readonly Color DuskColor = new(255, 120, 60, 70);

    private static Color LerpColor(Color a, Color b, float t)
    {
        t = MathHelper.Clamp(t, 0f, 1f);
        return new Color(
            (int)(a.R + (b.R - a.R) * t),
            (int)(a.G + (b.G - a.G) * t),
            (int)(a.B + (b.B - a.B) * t),
            (int)(a.A + (b.A - a.A) * t));
    }
}

public enum DayPhase
{
    Dawn,
    Day,
    Dusk,
    Night
}
