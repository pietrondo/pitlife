using System.Text.Json.Serialization;
using Microsoft.Xna.Framework;

namespace PitLife.Core;

public static class SimulationConfig
{
    public static SimulationConfigData Data { get; private set; } = 
        ConfigLoader.Load<SimulationConfigData>("simulation.json");
}

public class SimulationConfigData
{
    public int Version { get; set; } = 1;
    public float TickInterval { get; set; } = 0.1f;
    public float[] SpeedLevels { get; set; } = [0f, 1f, 2f, 4f];
    public DayNightConfig DayNight { get; set; } = new();
}

public class DayNightConfig
{
    public float DayLength { get; set; } = 120f;
    public float DawnEnd { get; set; } = 0.10f;
    public float DayEnd { get; set; } = 0.45f;
    public float DuskEnd { get; set; } = 0.55f;
    public float NightEnd { get; set; } = 0.90f;

    public int[] NightColor { get; set; } = [20, 30, 80, 100];
    public int[] DawnColor { get; set; } = [255, 180, 100, 50];
    public int[] DuskColor { get; set; } = [255, 120, 60, 70];

    [JsonIgnore]
    public Color NightColorValue => GetColor(NightColor);
    [JsonIgnore]
    public Color DawnColorValue => GetColor(DawnColor);
    [JsonIgnore]
    public Color DuskColorValue => GetColor(DuskColor);

    private static Color GetColor(int[] values)
    {
        if (values == null || values.Length < 4) return Color.Transparent;
        return new Color(values[0], values[1], values[2], values[3]);
    }
}
