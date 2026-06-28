using Microsoft.Xna.Framework;

namespace PitLife.Rendering;

/// <summary>
/// Represents the DayNightCycle.
/// </summary>
public class DayNightCycle
{
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
        TimeOfDay = totalTime % Core.SimulationConfig.Data.DayNight.DayLength;
        Phase = GetPhase(TimeOfDay);
    }

    /// <summary>
    /// Executes the GetPhase.
    /// </summary>
    /// <param name="timeOfDay">The timeOfDay parameter.</param>
    /// <returns>Returns the DayPhase result.</returns>
    public static DayPhase GetPhase(float timeOfDay)
    {
        var config = Core.SimulationConfig.Data.DayNight;
        var t = timeOfDay / config.DayLength;

        if (t < config.DawnEnd) return DayPhase.Dawn;
        if (t < config.DayEnd) return DayPhase.Day;
        if (t < config.DuskEnd) return DayPhase.Dusk;
        if (t < config.NightEnd) return DayPhase.Night;
        return DayPhase.Dawn;
    }

    /// <summary>
    /// Executes the GetOverlayColor.
    /// </summary>
    /// <returns>Returns the Color result.</returns>
    public Color GetOverlayColor()
    {
        var config = Core.SimulationConfig.Data.DayNight;
        var t = TimeOfDay / config.DayLength;

        if (t < config.DawnEnd)
        {
            return LerpColor(config.NightColorValue, config.DawnColorValue, t / config.DawnEnd);
        }
        if (t < config.DayEnd)
        {
            return Color.Transparent;
        }
        if (t < config.DuskEnd)
        {
            return LerpColor(Color.Transparent, config.DuskColorValue, (t - config.DayEnd) / (config.DuskEnd - config.DayEnd));
        }
        if (t < config.NightEnd)
        {
            return LerpColor(config.DuskColorValue, config.NightColorValue, (t - config.DuskEnd) / (config.NightEnd - config.DuskEnd));
        }

        return LerpColor(config.NightColorValue, config.DawnColorValue, (t - config.NightEnd) / (1f - config.NightEnd));
    }

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
