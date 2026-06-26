using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace PitLife.Core;

public static class CataclysmConfig
{
    public static CataclysmConfigData Data { get; private set; } = new();

    static CataclysmConfig()
    {
        try
        {
            string path = Path.Combine("Content", "config", "cataclysm.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<CataclysmConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch { /* use defaults */ }
    }
}

public class CataclysmConfigData
{
    public float InitialCooldown { get; set; } = 120f;
    public float CooldownMin { get; set; } = 180f;
    public float CooldownMax { get; set; } = 420f;
    public float AsteroidDuration { get; set; } = 60f;
    public float IceAgeDuration { get; set; } = 120f;

    public CataclysmRadiiConfig Radii { get; set; } = new();
    public CataclysmColorsConfig Colors { get; set; } = new();
}

public class CataclysmRadiiConfig
{
    public int Asteroid { get; set; } = 6;
    public int Supervolcano { get; set; } = 5;
    public int Earthquake { get; set; } = 8;
    public int DefaultRadius { get; set; } = 3;
}

public class CataclysmColorsConfig
{
    public CataclysmColorConfig Asteroid { get; set; } = new() { R = 255, G = 100, B = 30, A = 200 };
    public CataclysmColorConfig Supervolcano { get; set; } = new() { R = 255, G = 50, B = 10, A = 200 };
    public CataclysmColorConfig Earthquake { get; set; } = new() { R = 180, G = 140, B = 100, A = 150 };
    public CataclysmColorConfig IceAge { get; set; } = new() { R = 100, G = 200, B = 255, A = 150 };
    public CataclysmColorConfig Drought { get; set; } = new() { R = 255, G = 180, B = 40, A = 150 };
    public CataclysmColorConfig Flood { get; set; } = new() { R = 40, G = 140, B = 255, A = 150 };
    public CataclysmColorConfig Tsunami { get; set; } = new() { R = 30, G = 100, B = 200, A = 180 };
}

public class CataclysmColorConfig
{
    public int R { get; set; }
    public int G { get; set; }
    public int B { get; set; }
    public int A { get; set; }

    public Color ToColor() => new Color(R, G, B, A);
}
