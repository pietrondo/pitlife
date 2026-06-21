using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public static class SaveSystem
{
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
    }

    public class SaveData
    {
        public int Seed { get; set; }
        public int WorldWidth { get; set; }
        public int WorldHeight { get; set; }
        public float TotalTime { get; set; }
        public List<CreatureSaveData> Creatures { get; set; } = new();
    }

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true
    };

    public static void Save(string filePath, Ecosystem ecosystem)
    {
        var data = new SaveData
        {
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
                Genome = new GenomeSaveData
                {
                    Speed = c.Genome.Speed,
                    Size = c.Genome.Size,
                    Metabolism = c.Genome.Metabolism,
                    VisionRange = c.Genome.VisionRange,
                    ColorR = c.Genome.Color.R,
                    ColorG = c.Genome.Color.G,
                    ColorB = c.Genome.Color.B,
                    MutationRate = c.Genome.MutationRate
                }
            });
        }

        string json = JsonSerializer.Serialize(data, Options);
        File.WriteAllText(filePath, json);
    }

    public static SaveData? Load(string filePath)
    {
        if (!File.Exists(filePath)) return null;

        try
        {
            string json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SaveData>(json, Options);
        }
        catch
        {
            return null;
        }
    }
}
