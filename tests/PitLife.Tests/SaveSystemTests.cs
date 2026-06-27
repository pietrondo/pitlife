using System;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;
using Moq;

namespace PitLife.Tests;

public class SaveSystemTests
{
    [Fact]
    public void SaveAndLoad_RestoresEcosystemCorrectly()
    {
        string filePath = "test_savegame.json";
        
        var original = new Ecosystem(32, 24, 42);
        original.Initialize(2, 1, 1, 3);
        original.TotalTime = 123.45f;

        Assert.NotEmpty(original.Creatures);

        try
        {
            SaveSystem.Save(filePath, original);
            Assert.True(File.Exists(filePath));

            var loadedData = SaveSystem.Load(filePath);
            Assert.NotNull(loadedData);

            Assert.Equal(original.Seed, loadedData.Seed);
            Assert.Equal(original.World.Width, loadedData.WorldWidth);
            Assert.Equal(original.World.Height, loadedData.WorldHeight);
            Assert.Equal(original.TotalTime, loadedData.TotalTime);
            Assert.Equal(original.Creatures.Count, loadedData.Creatures.Count);

            var restored = new Ecosystem(loadedData.WorldWidth, loadedData.WorldHeight, loadedData.Seed);
            restored.TotalTime = loadedData.TotalTime;

            foreach (var cData in loadedData.Creatures)
            {
                var def = SpeciesRegistry.Get(cData.Species);
                Assert.NotNull(def);

                var genome = new Genome
                {
                    Speed = cData.Genome.Speed,
                    Size = cData.Genome.Size,
                    Metabolism = cData.Genome.Metabolism,
                    VisionRange = cData.Genome.VisionRange,
                    Color = new Color(cData.Genome.ColorR, cData.Genome.ColorG, cData.Genome.ColorB),
                    MutationRate = cData.Genome.MutationRate,
                    DesertAdaptation = cData.Genome.DesertAdaptation,
                    ColdAdaptation = cData.Genome.ColdAdaptation,
                    ForestAdaptation = cData.Genome.ForestAdaptation,
                    WaterAdaptation = cData.Genome.WaterAdaptation,
                    Genetics = cData.Genome.Genetics ?? default
                };

                Creature c = (Creature)Activator.CreateInstance(
                    def.CreatureType,
                    new Vector2(cData.PositionX, cData.PositionY),
                    genome,
                    def.Species)!;

                c.Energy = cData.Energy;
                c.Gender = cData.Gender;
                c.Facing = new Vector2(cData.FacingX, cData.FacingY);
                c.GrowFor(cData.Age);
                c.RestoreGeneticHistory(
                    LineageRecord.Restore(
                        cData.IndividualId,
                        cData.ParentAId,
                        cData.ParentBId,
                        cData.AncestorDepths),
                    cData.InbreedingCoefficient);

                restored.AddCreature(c);
            }

            restored.FlushPending();
            restored.UpdateStats();

            Assert.Equal(original.Seed, restored.Seed);
            Assert.Equal(original.TotalTime, restored.TotalTime);
            Assert.Equal(original.Creatures.Count, restored.Creatures.Count);

            for (int i = 0; i < original.Creatures.Count; i++)
            {
                var origC = original.Creatures[i];
                var restC = restored.Creatures[i];

                Assert.Equal(origC.Species, restC.Species);
                Assert.Equal(origC.Position.X, restC.Position.X, precision: 2);
                Assert.Equal(origC.Position.Y, restC.Position.Y, precision: 2);
                Assert.Equal(origC.Energy, restC.Energy, precision: 2);
                Assert.Equal(origC.Age, restC.Age, precision: 2);
                Assert.Equal(origC.Gender, restC.Gender);
                Assert.Equal(origC.Facing.X, restC.Facing.X, precision: 2);
                Assert.Equal(origC.Facing.Y, restC.Facing.Y, precision: 2);
                Assert.Equal(origC.Genome.Speed, restC.Genome.Speed);
                Assert.Equal(origC.Genome.Size, restC.Genome.Size);
                Assert.Equal(origC.Genome.Metabolism, restC.Genome.Metabolism);
                Assert.Equal(origC.Genome.VisionRange, restC.Genome.VisionRange);
                Assert.Equal(origC.Genome.Color, restC.Genome.Color);
                Assert.Equal(origC.Genome.Genetics, restC.Genome.Genetics);
                Assert.Equal(origC.Lineage.IndividualId, restC.Lineage.IndividualId);
                Assert.Equal(origC.Lineage.ParentAId, restC.Lineage.ParentAId);
                Assert.Equal(origC.Lineage.ParentBId, restC.Lineage.ParentBId);
                Assert.Equal(origC.Lineage.AncestorDepths, restC.Lineage.AncestorDepths);
                Assert.Equal(origC.InbreedingCoefficient, restC.InbreedingCoefficient);
            }
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Save_WritesCurrentSchemaVersion()
    {
        string filePath = "test_versioned_save.json";
        var ecosystem = new Ecosystem(16, 12, 7);
        ecosystem.Initialize(1, 1, 0, 2);

        try
        {
            SaveSystem.Save(filePath, ecosystem);
            Assert.True(File.Exists(filePath));

            string json = File.ReadAllText(filePath);
            Assert.Contains("\"SchemaVersion\":", json);
            Assert.Contains($"\"SchemaVersion\": {SaveSystem.CurrentSchemaVersion}", json);

            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);
            Assert.Equal(SaveSystem.CurrentSchemaVersion, loaded.SchemaVersion);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_MigratesV0SaveWithoutSchemaVersion()
    {
        string filePath = "test_v0_save.json";
        var ecosystem = new Ecosystem(16, 12, 42);
        ecosystem.Initialize(2, 1, 0, 4);

        try
        {
            // Manually write a V0-format save (no SchemaVersion)
            string v0Json = @"{
  ""Seed"": 42,
  ""WorldWidth"": 16,
  ""WorldHeight"": 12,
  ""TotalTime"": 99.5,
  ""Creatures"": []
}";
            File.WriteAllText(filePath, v0Json);

            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);
            Assert.Equal(42, loaded.Seed);
            Assert.Equal(16, loaded.WorldWidth);
            Assert.Equal(12, loaded.WorldHeight);
            Assert.Equal(99.5f, loaded.TotalTime);
            Assert.Equal(SaveSystem.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Empty(loaded.Creatures);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_RejectsCorruptJson()
    {
        string filePath = "test_corrupt_save.json";
        File.WriteAllText(filePath, "not valid json {{{");

        try
        {
            var loaded = SaveSystem.Load(filePath);
            Assert.Null(loaded);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_ThrowsOnInvalidSeed()
    {
        string filePath = "test_invalid_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": -1,
  ""WorldWidth"": 16,
  ""WorldHeight"": 12,
  ""TotalTime"": 0,
  ""Creatures"": []
}";
        File.WriteAllText(filePath, json);

        try
        {
            Assert.Throws<InvalidDataException>(() => SaveSystem.Load(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_ThrowsOnInvalidWorldDimensions()
    {
        string filePath = "test_invalid_dim_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": 1,
  ""WorldWidth"": 2,
  ""WorldHeight"": 5000,
  ""TotalTime"": 0,
  ""Creatures"": []
}";
        File.WriteAllText(filePath, json);

        try
        {
            var ex = Assert.Throws<InvalidDataException>(() => SaveSystem.Load(filePath));
            Assert.Contains("width", ex.Message.ToLowerInvariant());
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_ThrowsOnNegativeTotalTime()
    {
        string filePath = "test_negative_time_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": 1,
  ""WorldWidth"": 10,
  ""WorldHeight"": 10,
  ""TotalTime"": -5,
  ""Creatures"": []
}";
        File.WriteAllText(filePath, json);

        try
        {
            Assert.Throws<InvalidDataException>(() => SaveSystem.Load(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_ThrowsOnInvalidCreatureEnergy()
    {
        string filePath = "test_invalid_creature_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": 1,
  ""WorldWidth"": 10,
  ""WorldHeight"": 10,
  ""TotalTime"": 10,
  ""Creatures"": [
    {
      ""Species"": ""Clover"",
      ""PositionX"": 100,
      ""PositionY"": 200,
      ""Energy"": -10,
      ""Age"": 5,
      ""Gender"": 0,
      ""FacingX"": 0,
      ""FacingY"": 1,
      ""Genome"": { ""Size"": 1 },
      ""IndividualId"": 1,
      ""ParentAId"": 0,
      ""ParentBId"": 0,
      ""AncestorDepths"": {},
      ""InbreedingCoefficient"": 0
    }
  ]
}";
        File.WriteAllText(filePath, json);

        try
        {
            Assert.Throws<InvalidDataException>(() => SaveSystem.Load(filePath));
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void SaveLoadRoundTrip_PreservesAllData()
    {
        string filePath = "test_roundtrip.json";
        var original = new Ecosystem(24, 18, 123);
        original.Initialize(3, 2, 1, 5);

        // Advance simulation a bit to generate non-trivial state
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        original.Tick(gameTime);

        try
        {
            SaveSystem.Save(filePath, original);
            var json1 = File.ReadAllText(filePath);

            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);

            var restored = new Ecosystem(loaded.WorldWidth, loaded.WorldHeight, loaded.Seed);
            restored.TotalTime = loaded.TotalTime;
            foreach (var cData in loaded.Creatures)
            {
                var def = SpeciesRegistry.Get(cData.Species);
                Assert.NotNull(def);
                var genome = new Genome
                {
                    Speed = cData.Genome.Speed, Size = cData.Genome.Size,
                    Metabolism = cData.Genome.Metabolism, VisionRange = cData.Genome.VisionRange,
                    Color = new Color(cData.Genome.ColorR, cData.Genome.ColorG, cData.Genome.ColorB),
                    MutationRate = cData.Genome.MutationRate,
                    DesertAdaptation = cData.Genome.DesertAdaptation,
                    ColdAdaptation = cData.Genome.ColdAdaptation,
                    ForestAdaptation = cData.Genome.ForestAdaptation,
                    WaterAdaptation = cData.Genome.WaterAdaptation,
                    Genetics = cData.Genome.Genetics ?? default
                };
                Creature c = (Creature)Activator.CreateInstance(def.CreatureType,
                    new Vector2(cData.PositionX, cData.PositionY), genome, def.Species)!;
                c.Energy = cData.Energy; c.Gender = cData.Gender;
                c.Facing = new Vector2(cData.FacingX, cData.FacingY);
                c.GrowFor(cData.Age);
                c.RestoreGeneticHistory(LineageRecord.Restore(cData.IndividualId,
                    cData.ParentAId, cData.ParentBId, cData.AncestorDepths),
                    cData.InbreedingCoefficient);
                restored.AddCreature(c);
            }
            restored.FlushPending();
            restored.UpdateStats();

            string filePath2 = "test_roundtrip2.json";
            SaveSystem.Save(filePath2, restored);
            var json2 = File.ReadAllText(filePath2);

            Assert.Equal(json1, json2);
            File.Delete(filePath2);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_MissingOptionalFields_UsesDefaults()
    {
        string filePath = "test_partial_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": 99,
  ""WorldWidth"": 16,
  ""WorldHeight"": 12,
  ""TotalTime"": 1.5,
  ""Creatures"": [
    {
      ""Species"": ""Clover"",
      ""PositionX"": 5,
      ""PositionY"": 5,
      ""Energy"": 10,
      ""Age"": 1,
      ""Genome"": { ""Size"": 2.0 }
    }
  ]
}";
        File.WriteAllText(filePath, json);

        try
        {
            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);
            Assert.Single(loaded.Creatures);
            
            var c = loaded.Creatures[0];
            Assert.Equal("Clover", c.Species);
            Assert.Equal(5, c.PositionX);
            Assert.Equal(5, c.PositionY);
            Assert.Equal(10, c.Energy);
            Assert.Equal(1, c.Age);
            
            // Check default fields that were missing
            Assert.Equal(Gender.None, c.Gender); // Default enum value
            Assert.Equal(0, c.FacingX); // Default
            Assert.Equal(0, c.FacingY); // Default
            Assert.Equal(2.0f, c.Genome.Size);
            Assert.Equal(0, c.Genome.Speed); // Default float
            Assert.Equal(0uL, c.IndividualId); // Default ulong
            Assert.Empty(c.AncestorDepths);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void SaveLoad_EmptyWorld_WorksCorrectly()
    {
        string filePath = "test_empty_save.json";
        var ecosystem = new Ecosystem(20, 15, 12345);
        // Ecosystem created but NOT initialized, so it has no creatures.

        try
        {
            SaveSystem.Save(filePath, ecosystem);
            Assert.True(File.Exists(filePath));

            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);
            Assert.Equal(20, loaded.WorldWidth);
            Assert.Equal(15, loaded.WorldHeight);
            Assert.Equal(12345, loaded.Seed);
            Assert.Empty(loaded.Creatures);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Save_SerializesEcosystemState()
    {
        string filePath = "test_serialize_save.json";
        var mockEcosystem = new Mock<Ecosystem>(10, 10, 42) { CallBase = true };
        
        var genome = new Genome { Size = 1.0f };
        var creature = new Plant(new Vector2(5, 5), genome, "Clover");
        mockEcosystem.Object.AddCreature(creature);
        mockEcosystem.Object.FlushPending();

        try
        {
            SaveSystem.Save(filePath, mockEcosystem.Object);
            Assert.True(File.Exists(filePath));
            
            string json = File.ReadAllText(filePath);
            Assert.Contains("\"Seed\": 42", json);
            Assert.Contains("\"WorldWidth\": 10", json);
            Assert.Contains("\"WorldHeight\": 10", json);
            Assert.Contains("\"Species\": \"Clover\"", json);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

    [Fact]
    public void Load_DeserializesEcosystemState()
    {
        string filePath = "test_deserialize_save.json";
        string json = @"{
  ""SchemaVersion"": 1,
  ""Seed"": 77,
  ""WorldWidth"": 8,
  ""WorldHeight"": 8,
  ""TotalTime"": 5.5,
  ""Creatures"": [
    {
      ""Species"": ""Clover"",
      ""PositionX"": 2,
      ""PositionY"": 3,
      ""Energy"": 15,
      ""Age"": 2,
      ""Genome"": { ""Size"": 1.5 }
    }
  ]
}";
        File.WriteAllText(filePath, json);

        try
        {
            var loaded = SaveSystem.Load(filePath);
            Assert.NotNull(loaded);
            Assert.Equal(77, loaded.Seed);
            Assert.Equal(8, loaded.WorldWidth);
            Assert.Equal(8, loaded.WorldHeight);
            Assert.Equal(5.5f, loaded.TotalTime);
            
            Assert.Single(loaded.Creatures);
            var c = loaded.Creatures[0];
            Assert.Equal("Clover", c.Species);
            Assert.Equal(2, c.PositionX);
            Assert.Equal(3, c.PositionY);
            Assert.Equal(15, c.Energy);
            Assert.Equal(2, c.Age);
        }
        finally
        {
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }

}
