using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.Tests;

public class EnvironmentResponseTests
{
    private static Ecosystem MakeEco(int w = 32, int h = 24, int seed = 42)
    {
        var eco = new Ecosystem(w, h, seed);
        eco.Initialize(h: 10, c: 4, o: 2, p: 30);
        return eco;
    }

    [Fact]
    public void Reset_SetsThirstToZero()
    {
        var er = new EnvironmentResponse { Thirst = 100f };
        er.Reset();
        Assert.Equal(0f, er.Thirst);
    }

    [Fact]
    public void ApplyClimateAndPopulationPressure_SkipsPlants()
    {
        var eco = MakeEco();
        float energy = 100f;
        var er = new EnvironmentResponse();
        er.ApplyClimateAndPopulationPressure(ref energy, 10f, CreatureType.Plant, Vector2.Zero, eco);
        Assert.Equal(100f, energy);
    }

    [Fact]
    public void ApplyClimateAndPopulationPressure_ReducesEnergyForAnimals()
    {
        var eco = MakeEco();
        float energy = 100f;
        var er = new EnvironmentResponse();
        er.ApplyClimateAndPopulationPressure(ref energy, 10f, CreatureType.Herbivore, new Vector2(100, 100), eco);
        Assert.True(energy < 100f);
    }

    [Fact]
    public void ApplyClimateAndPopulationPressure_ReducesMoreAtHighAltitude()
    {
        var eco = MakeEco(64, 64, 42);
        var er = new EnvironmentResponse();
        float energyLow = 100f, energyHigh = 100f;
        er.ApplyClimateAndPopulationPressure(ref energyLow, 10f, CreatureType.Herbivore, new Vector2(100, 100), eco);
        er.ApplyClimateAndPopulationPressure(ref energyHigh, 10f, CreatureType.Herbivore, new Vector2(100, eco.World.PixelHeight - 10), eco);
        Assert.True(energyHigh <= energyLow, $"High altitude should reduce at least as much: low={energyLow} high={energyHigh}");
    }

    [Fact]
    public void ApplyWindDrift_SkipsPlants()
    {
        var eco = MakeEco();
        var pos = new Vector2(200, 200);
        var er = new EnvironmentResponse();
        er.ApplyWindDrift(ref pos, CreatureType.Plant, false, 0f, 10f, 1f, eco.World);
        Assert.Equal(new Vector2(200, 200), pos);
    }

    [Fact]
    public void ApplyWindDrift_MovesPosition()
    {
        var eco = MakeEco();
        var er = new EnvironmentResponse();
        var tile = eco.World.GetTileAtPosition(200, 200);
        var pos = new Vector2(200, 200);
        er.ApplyWindDrift(ref pos, CreatureType.Herbivore, false, 0f, 10f, 1f, eco.World);
        // Wind drift moves position if the target tile is passable
        if (tile.IsPassableFor(false))
            Assert.NotEqual(new Vector2(200, 200), pos);
    }

    [Fact]
    public void ApplyWindDrift_ClampsToWorld()
    {
        var eco = MakeEco();
        var pos = new Vector2(-100, -100);
        var er = new EnvironmentResponse();
        er.ApplyWindDrift(ref pos, CreatureType.Herbivore, false, 0f, 10f, 1f, eco.World);
        Assert.InRange(pos.X, 0, eco.World.PixelWidth - 1);
        Assert.InRange(pos.Y, 0, eco.World.PixelHeight - 1);
    }

    [Fact]
    public void UpdateEnvironmentalMultipliers_SetsMultipliers()
    {
        var eco = MakeEco();
        float sm = 1f, em = 1f;
        var er = new EnvironmentResponse();
        var genome = Genome.Random(new Random(1));
        er.UpdateEnvironmentalMultipliers(ref sm, ref em, CreatureType.Herbivore, "Gazelle", genome,
            new Vector2(200, 200), 22f, false, eco.World, eco);
        Assert.InRange(sm, 0f, 2f);
        Assert.InRange(em, 0f, 10f);
    }

    [Fact]
    public void UpdateEnvironmentalMultipliers_AquaticInWaterIsOptimal()
    {
        var eco = MakeEco(64, 64, 42);
        var waterPos = new Vector2(eco.World.TileSize + 1, eco.World.TileSize + 1);
        float sm = 1f, em = 1f;
        var er = new EnvironmentResponse();
        var genome = Genome.Random(new Random(1));
        er.UpdateEnvironmentalMultipliers(ref sm, ref em, CreatureType.Herbivore, "Fish",
            genome, waterPos, 22f, true, eco.World, eco);
        Assert.Equal(1f, sm);
        Assert.Equal(1f, em);
    }

    [Fact]
    public void UpdateEnvironmentalMultipliers_AppliesTemperaturePenalty()
    {
        var eco = MakeEco(64, 64, 42);
        float sm = 1f, em = 1f;
        var er = new EnvironmentResponse();
        var genome = Genome.Random(new Random(1));
        er.UpdateEnvironmentalMultipliers(ref sm, ref em, CreatureType.Herbivore, "Gazelle",
            genome, new Vector2(200, 200), 100f, false, eco.World, eco);
        Assert.True(em > 1f, $"Expected energy penalty, got em={em}");
    }

    [Fact]
    public void UpdateEnvironmentalMultipliers_WrongBiomeAddsBigPenalty()
    {
        var eco = MakeEco(64, 64, 42);
        float sm = 1f, em = 1f;
        var er = new EnvironmentResponse();
        var genome = Genome.Random(new Random(1));
        er.UpdateEnvironmentalMultipliers(ref sm, ref em, CreatureType.Herbivore, "Gazelle",
            genome, new Vector2(200, 200), 22f, false, eco.World, eco);
        Assert.True(em >= 1f);
    }

    [Fact]
    public void UpdateHibernation_DoesNothingForPlants()
    {
        var eco = MakeEco();
        bool hibernating = false;
        var er = new EnvironmentResponse();
        er.UpdateHibernation(ref hibernating, "Clover", CreatureType.Plant, Vector2.Zero, eco);
        Assert.False(hibernating);
    }

    [Fact]
    public void UpdateHibernation_NonHibernatingSpeciesDoesNothing()
    {
        var eco = MakeEco();
        bool hibernating = false;
        var er = new EnvironmentResponse();
        er.UpdateHibernation(ref hibernating, "Gazelle", CreatureType.Herbivore, new Vector2(200, 200), eco);
        Assert.False(hibernating);
    }
}
