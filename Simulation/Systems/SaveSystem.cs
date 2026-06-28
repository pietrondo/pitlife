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

        try
        {
            return JsonSerializer.Deserialize<SaveData>(json, Options);
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
