using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Core;

public static class CataclysmConfig
{
    public static CataclysmConfigDoc Data { get; } = ConfigLoader.Load("cataclysms.json", Fallback);

    private static readonly CataclysmConfigDoc Fallback = new CataclysmConfigDoc(
        Version: 1,
        MassExtinctions: new List<MassExtinctionDef>
        {
            new MassExtinctionDef { Type = 0, Name = "Asteroid Impact", GrassMultiplier = 0f, Duration = 60f, Radius = 8, Color = new ColorDef { R = 255, G = 100, B = 30, A = 200 } },
            new MassExtinctionDef { Type = 1, Name = "Ice Age", GrassMultiplier = 0.1f, Duration = 120f, Radius = 0, Color = new ColorDef { R = 100, G = 200, B = 255, A = 150 } },
            new MassExtinctionDef { Type = 2, Name = "Supervolcano", GrassMultiplier = 0.05f, Duration = 90f, Radius = 5, Color = new ColorDef { R = 255, G = 50, B = 10, A = 200 } }
        },
        PlayerEvents: new List<PlayerEventDef>
        {
            new PlayerEventDef { Name = "Asteroid", Duration = 40f, GrassMultiplier = 0.2f, Radius = 6, Color = new ColorDef { R = 255, G = 100, B = 30, A = 200 }, InnerBiome = "Volcano", OuterBiome = "Desert", GrassAmount = 0f, SoilNutrients = 0.1f },
            new PlayerEventDef { Name = "Supervolcano", Duration = 40f, GrassMultiplier = 0.2f, Radius = 5, Color = new ColorDef { R = 255, G = 50, B = 10, A = 200 }, InnerBiome = "Volcano", OuterBiome = "Desert", GrassAmount = 0f, SoilNutrients = 0.1f },
            new PlayerEventDef { Name = "Earthquake", Duration = 40f, GrassMultiplier = 0.2f, Radius = 8, Color = new ColorDef { R = 180, G = 140, B = 100, A = 150 }, InnerBiome = "Cave", OuterBiome = "Mountain", GrassAmount = 0f, SoilNutrients = 0.1f },
            new PlayerEventDef { Name = "IceAge", Duration = 40f, GrassMultiplier = 0.2f, Radius = 3, Color = new ColorDef { R = 100, G = 200, B = 255, A = 150 }, InnerBiome = "Snow", OuterBiome = "Tundra", GrassAmount = 0f, SoilNutrients = 0.1f },
            new PlayerEventDef { Name = "Drought", Duration = 40f, GrassMultiplier = 0.1f, Radius = 3, Color = new ColorDef { R = 255, G = 180, B = 40, A = 150 }, InnerBiome = "Desert", OuterBiome = "Desert", GrassAmount = 0f, SoilNutrients = 0.1f },
            new PlayerEventDef { Name = "Flood", Duration = 40f, GrassMultiplier = 2.5f, Radius = 3, Color = new ColorDef { R = 40, G = 140, B = 255, A = 150 }, InnerBiome = "ShallowWater", OuterBiome = "None", GrassAmount = 1f, SoilNutrients = 2f }
        },
        ChainReactions: new ChainReactionDef
        {
            EarthquakeTsunamiChance = 0.4f,
            EarthquakeTsunamiRadius = 3,
            SupervolcanoWinterChance = 0.5f,
            SupervolcanoWinterGrassMultiplier = 0.2f,
            TsunamiDuration = 25f,
            TsunamiGrassMultiplier = 2.5f,
            WinterMinDuration = 60f
        },
        Chances: new ChanceDef
        {
            RandomTriggerChance = 0.3f,
            MassExtinctionChance = 0.05f,
            MassExtinctionMinTime = 120f,
            RandomCooldownMin = 180f,
            RandomCooldownSpread = 420f,
            RandomEvents = new List<RandomEventDef>
            {
                new RandomEventDef { Name = "Drought", GrassMultiplier = 0.1f, BaseDuration = 30f, DurationSpread = 30f },
                new RandomEventDef { Name = "Flood", GrassMultiplier = 2.5f, BaseDuration = 15f, DurationSpread = 15f },
                new RandomEventDef { Name = "Firestorm", GrassMultiplier = 0f, BaseDuration = 10f, DurationSpread = 10f },
                new RandomEventDef { Name = "Bloom", GrassMultiplier = 3f, BaseDuration = 20f, DurationSpread = 20f }
            }
        }
    );

    public sealed record CataclysmConfigDoc(
        int Version,
        List<MassExtinctionDef> MassExtinctions,
        List<PlayerEventDef> PlayerEvents,
        ChainReactionDef ChainReactions,
        ChanceDef Chances
    );

    public sealed record MassExtinctionDef
    {
        public int Type { get; init; }
        public string Name { get; init; } = "";
        public float GrassMultiplier { get; init; }
        public float Duration { get; init; }
        public int Radius { get; init; }
        public ColorDef Color { get; init; } = new();
    }

    public sealed record PlayerEventDef
    {
        public string Name { get; init; } = "";
        public float Duration { get; init; }
        public float GrassMultiplier { get; init; }
        public int Radius { get; init; }
        public ColorDef Color { get; init; } = new();
        public string InnerBiome { get; init; } = "";
        public string OuterBiome { get; init; } = "";
        public float GrassAmount { get; init; }
        public float SoilNutrients { get; init; }
    }

    public sealed record ChainReactionDef
    {
        public float EarthquakeTsunamiChance { get; init; }
        public int EarthquakeTsunamiRadius { get; init; }
        public float SupervolcanoWinterChance { get; init; }
        public float SupervolcanoWinterGrassMultiplier { get; init; }
        public float TsunamiDuration { get; init; }
        public float TsunamiGrassMultiplier { get; init; }
        public float WinterMinDuration { get; init; }
    }

    public sealed record ChanceDef
    {
        public float RandomTriggerChance { get; init; }
        public float MassExtinctionChance { get; init; }
        public float MassExtinctionMinTime { get; init; }
        public float RandomCooldownMin { get; init; }
        public float RandomCooldownSpread { get; init; }
        public List<RandomEventDef> RandomEvents { get; init; } = new();
    }

    public sealed record ColorDef
    {
        public int R { get; init; }
        public int G { get; init; }
        public int B { get; init; }
        public int A { get; init; }

        public Color ToColor() => new Color(R, G, B, A);
    }

    public sealed record RandomEventDef
    {
        public string Name { get; init; } = "";
        public float GrassMultiplier { get; init; }
        public float BaseDuration { get; init; }
        public float DurationSpread { get; init; }
    }
}