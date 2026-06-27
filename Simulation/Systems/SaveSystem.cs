using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace PitLife.Simulation;

public static class SaveSystem
{
    public const int CurrentSchemaVersion = 1;

    public class GenomeSaveData
    {
        public float Speed { get; set; }
        public float Size { get; set; }
        public float Metabolism { get; set; }
        public float VisionRange { get; set; }
        public byte ColorR { get; set; }
        public byte ColorG { get; set; }
        public byte ColorB { get; set; }
        public float MutationRate { get; set; }
        public float DesertAdaptation { get; set; }
        public float ColdAdaptation { get; set; }
        public float ForestAdaptation { get; set; }
        public float WaterAdaptation { get; set; }
        public GeneticProfile? Genetics { get; set; }
    }

    public class CreatureSaveData
    {
        public string Species { get; set; } = "";
        public float PositionX { get; set; }
        public float PositionY { get; set; }
        public float Energy { get; set; }
        public float Age { get; set; }
        public Gender Gender { get; set; }
        public float FacingX { get; set; }
        public float FacingY { get; set; }
        public GenomeSaveData Genome { get; set; } = new();
        public ulong IndividualId { get; set; }
        public ulong ParentAId { get; set; }
        public ulong ParentBId { get; set; }
        public Dictionary<ulong, byte> AncestorDepths { get; set; } = new();
        public float InbreedingCoefficient { get; set; }
    }

    public class SaveData
    {
        public int SchemaVersion { get; set; } = CurrentSchemaVersion;
        public int Seed { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public float TotalTime { get; set; }
        public List<CreatureSaveData> Creatures { get; set; } = new();
    }

    /// <summary>Legacy V0 format without SchemaVersion, used for migration.</summary>
    private class SaveDataV0
    {
        public int Seed { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public float TotalTime { get; set; }
        public List<CreatureSaveData> Creatures { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        IncludeFields = true
    };

    public static void Save(string filePath, Ecosystem ecosystem)
    {
        var data = new SaveData
        {
            SchemaVersion = CurrentSchemaVersion,
            Seed = ecosystem.Seed,
            WorldWidth = ecosystem.World.Width,
            WorldHeight = ecosystem.World.Height,
            TotalTime = ecosystem.TotalTime
        };

        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || !c.IsAlive) continue;

            data.Creatures.Add(new CreatureSaveData
            {
                Species = c.Species,
                PositionX = c.Position.X,
                PositionY = c.Position.Y,
                Energy = c.Energy,
                Age = c.Age,
                Gender = c.Gender,
                FacingX = c.Facing.X,
                FacingY = c.Facing.Y,
                IndividualId = c.Lineage.IndividualId,
                ParentAId = c.Lineage.ParentAId,
                ParentBId = c.Lineage.ParentBId,
                AncestorDepths = new Dictionary<ulong, byte>(c.Lineage.AncestorDepths),
                InbreedingCoefficient = c.InbreedingCoefficient,
                Genome = new GenomeSaveData
                {
                    Speed = c.Genome.Speed,
                    Size = c.Genome.Size,
                    Metabolism = c.Genome.Metabolism,
                    VisionRange = c.Genome.VisionRange,
                    ColorR = c.Genome.Color.R,
                    ColorG = c.Genome.Color.G,
                    ColorB = c.Genome.Color.B,
                    MutationRate = c.Genome.MutationRate,
                    DesertAdaptation = c.Genome.DesertAdaptation,
                    ColdAdaptation = c.Genome.ColdAdaptation,
                    ForestAdaptation = c.Genome.ForestAdaptation,
                    WaterAdaptation = c.Genome.WaterAdaptation,
                    Genetics = c.Genome.EnsureGeneticProfile()
                }
            });
        }

        var json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public static SaveData? Load(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        var json = File.ReadAllText(filePath);

        // Try current format first (has SchemaVersion)
        try
        {
            var data = JsonSerializer.Deserialize<SaveData>(json, Options);
            if (data != null && data.SchemaVersion >= 1)
            {
                ValidateOrThrow(data);
                return data;
            }
        }
        catch (JsonException) { /* not current format, try V0 migration */ }

        // Try legacy V0 format (no SchemaVersion)
        try
        {
            var v0 = JsonSerializer.Deserialize<SaveDataV0>(json, Options);
            if (v0 != null && v0.Creatures != null)
            {
                var migrated = MigrateFromV0(v0);
                ValidateOrThrow(migrated);
                return migrated;
            }
        }
        catch (JsonException) { /* not V0 either */ }

        return null;
    }

    private static SaveData MigrateFromV0(SaveDataV0 v0)
    {
        return new SaveData
        {
            SchemaVersion = CurrentSchemaVersion,
            Seed = v0.Seed,
            WorldWidth = v0.WorldWidth,
            WorldHeight = v0.WorldHeight,
            TotalTime = v0.TotalTime,
            Creatures = v0.Creatures
        };
    }

    private static void ValidateOrThrow(SaveData data)
    {
        var errors = new List<string>();

        if (data.SchemaVersion > CurrentSchemaVersion)
            errors.Add($"Save was created by a newer version (v{data.SchemaVersion}). Current version: v{CurrentSchemaVersion}. Please update the game.");

        if (data.Seed < 0)
            errors.Add("Invalid seed: must be non-negative.");

        if (data.WorldWidth < 4 || data.WorldWidth > 4096)
            errors.Add($"Invalid world width: {data.WorldWidth}. Must be between 4 and 4096.");

        if (data.WorldHeight < 4 || data.WorldHeight > 4096)
            errors.Add($"Invalid world height: {data.WorldHeight}. Must be between 4 and 4096.");

        if (data.TotalTime < 0)
            errors.Add($"Invalid total time: {data.TotalTime}. Must be non-negative.");

        if (data.Creatures == null)
            errors.Add("Creature list is missing.");

        if (data.Creatures is { Count: > 0 })
        {
            foreach (var c in data.Creatures)
            {
                if (string.IsNullOrWhiteSpace(c.Species))
                    errors.Add($"Creature #{c.IndividualId}: species name is empty.");
                if (!float.IsFinite(c.Energy) || c.Energy < 0)
                    errors.Add($"Creature #{c.IndividualId} ({c.Species}): invalid energy {c.Energy}.");
                if (!float.IsFinite(c.Age) || c.Age < 0)
                    errors.Add($"Creature #{c.IndividualId} ({c.Species}): invalid age {c.Age}.");
                if (!float.IsFinite(c.PositionX) || !float.IsFinite(c.PositionY))
                    errors.Add($"Creature #{c.IndividualId} ({c.Species}): invalid position.");
                if (c.Genome == null)
                    errors.Add($"Creature #{c.IndividualId} ({c.Species}): missing genome data.");
                else if (c.Genome.Size <= 0)
                    errors.Add($"Creature #{c.IndividualId} ({c.Species}): invalid genome size {c.Genome.Size}.");
            }
        }

        if (errors.Count > 0)
            throw new InvalidDataException($"Save validation failed ({errors.Count} error(s)):\n- {string.Join("\n- ", errors)}");
    }
}
