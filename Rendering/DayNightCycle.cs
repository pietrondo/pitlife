using Microsoft.Xna.Framework;

namespace PitLife.Rendering;

/// <summary>
/// Represents the DayNightCycle.
/// </summary>
public class DayNightCycle
{
    public const float DayLength = 120f;

    /// <summary>
    /// Gets or sets the TimeOfDay.
    /// </summary>
    public float TimeOfDay { get; private set; }
    /// <summary>
    /// Gets or sets the Phase.
    /// </summary>
    public DayPhase Phase { get; private set; }

    /// <summary>
    /// Executes the Update.
    /// </summary>
    /// <param name="totalTime">The totalTime parameter.</param>
    public void Update(float totalTime)
    {
        TimeOfDay = totalTime % DayLength;
        Phase = GetPhase(TimeOfDay);
    }

    /// <summary>
    /// Executes the GetPhase.
    /// </summary>
    /// <param name="timeOfDay">The timeOfDay parameter.</param>
    /// <returns>Returns the DayPhase result.</returns>
    public static DayPhase GetPhase(float timeOfDay)
    {
        var t = timeOfDay / DayLength;
        return t switch
        {
            < 0.10f => DayPhase.Dawn,
            < 0.45f => DayPhase.Day,
            < 0.55f => DayPhase.Dusk,
            < 0.90f => DayPhase.Night,
            _ => DayPhase.Dawn
        };
    }

    /// <summary>
    /// Executes the GetOverlayColor.
    /// </summary>
    /// <returns>Returns the Color result.</returns>
    public Color GetOverlayColor()
    {
        var t = TimeOfDay / DayLength;
        return t switch
        {
            < 0.10f => LerpColor(NightColor, DawnColor, t / 0.10f),
            < 0.45f => Color.Transparent,
            < 0.55f => LerpColor(Color.Transparent, DuskColor, (t - 0.45f) / 0.10f),
            < 0.90f => LerpColor(DuskColor, NightColor, (t - 0.55f) / 0.35f),
            _ => LerpColor(NightColor, DawnColor, (t - 0.90f) / 0.10f)
        };
    }

    private static readonly Color NightColor = new(20, 30, 80, 100);
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
