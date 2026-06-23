using System;

namespace PitLife.Simulation;

public record WorldGenOptions
{
    public WorldGenPreset Preset { get; init; }
    public int ContinentCount
    {
        get => field;
        init
        {
            if (value < 1 || value > 6)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Must be between 1 and 6");
            field = value;
        }
    }
    public float SeaLevel
    {
        get => field;
        init
        {
            if (value < 0f || value > 1f)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Must be between 0 and 1");
            field = value;
        }
    }
    public IslandSize IslandSize { get; init; }
    public int MapWidth
    {
        get => field;
        init
        {
            if (value < 16)
                throw new ArgumentOutOfRangeException(nameof(MapWidth), value, "Map must be at least 16 wide");
            field = value;
        }
    }
    public int MapHeight
    {
        get => field;
        init
        {
            if (value < 16)
                throw new ArgumentOutOfRangeException(nameof(MapHeight), value, "Map must be at least 16 tall");
            field = value;
        }
    }
    public float PlanetRadiusKm { get; init; } = 6371f;
    public float OrbitalAU { get; init; } = 1f;
    public float Eccentricity { get; init; } = 0.12f;

    public WorldGenOptions(
        WorldGenPreset preset,
        int continentCount,
        float seaLevel,
        IslandSize islandSize,
        int mapWidth,
        int mapHeight)
    {
        Preset = preset;
        ContinentCount = continentCount;
        SeaLevel = seaLevel;
        IslandSize = islandSize;
        MapWidth = mapWidth;
        MapHeight = mapHeight;
    }

    public static WorldGenOptions Pangea() =>
        new(WorldGenPreset.Pangea, continentCount: 1, seaLevel: 0.40f, islandSize: IslandSize.Large, mapWidth: 96, mapHeight: 72);

    public static WorldGenOptions Continents() =>
        new(WorldGenPreset.Continents, continentCount: 4, seaLevel: 0.45f, islandSize: IslandSize.Medium, mapWidth: 96, mapHeight: 72);

    public static WorldGenOptions Archipelago() =>
        new(WorldGenPreset.Archipelago, continentCount: 6, seaLevel: 0.45f, islandSize: IslandSize.Small, mapWidth: 96, mapHeight: 72);

    public static WorldGenOptions WetWorld() =>
        new(WorldGenPreset.WetWorld, continentCount: 4, seaLevel: 0.60f, islandSize: IslandSize.Medium, mapWidth: 96, mapHeight: 72);

    public static WorldGenOptions DryWorld() =>
        new(WorldGenPreset.DryWorld, continentCount: 4, seaLevel: 0.30f, islandSize: IslandSize.Medium, mapWidth: 96, mapHeight: 72);
}
