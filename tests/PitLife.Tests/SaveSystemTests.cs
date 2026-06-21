using System;
using System.IO;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class SaveSystemTests
{
    [Fact]
    public void SaveAndLoad_RestoresEcosystemCorrectly()
    {
        string filePath = "test_savegame.json";
        
        // 1. Setup a custom ecosystem
        var original = new Ecosystem(32, 24, 42);
        original.Initialize(2, 1, 1, 3); // Spawn some creatures
        original.TotalTime = 123.45f;

        // Ensure we have some creatures
        Assert.NotEmpty(original.Creatures);

        try
        {
            // 2. Save the ecosystem state
            SaveSystem.Save(filePath, original);
            Assert.True(File.Exists(filePath));

            // 3. Load the ecosystem state
            var loadedData = SaveSystem.Load(filePath);
            Assert.NotNull(loadedData);

            // 4. Verify save data properties
            Assert.Equal(original.Seed, loadedData.Seed);
            Assert.Equal(original.World.Width, loadedData.WorldWidth);
            Assert.Equal(original.World.Height, loadedData.WorldHeight);
            Assert.Equal(original.TotalTime, loadedData.TotalTime);
            Assert.Equal(original.Creatures.Count, loadedData.Creatures.Count);

            // 5. Test reconstruction on a new ecosystem
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
                    MutationRate = cData.Genome.MutationRate
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

                restored.AddCreature(c);
            }

            restored.FlushPending();
            restored.UpdateStats();

            // Verify restored ecosystem properties
            Assert.Equal(original.Seed, restored.Seed);
            Assert.Equal(original.TotalTime, restored.TotalTime);
            Assert.Equal(original.Creatures.Count, restored.Creatures.Count);

            // Compare specific creatures
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
            }
        }
        finally
        {
            // Cleanup
            if (File.Exists(filePath))
                File.Delete(filePath);
        }
    }
}
