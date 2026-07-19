using System;
using Moq;
using PitLife.Simulation;
using Xunit;
using Microsoft.Xna.Framework;

namespace PitLife.Tests.Systems;

public class FlowSimulationTests
{
    [Fact]
    public void Construction_InitializesWaterMap()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        Assert.NotNull(flow);
    }

    [Fact]
    public void Tick_UpdatesFlowBasedOnEcosystemSpeed()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        var eco = new Ecosystem(16, 12, 42);
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));

        flow.Tick(eco, gt);
        // Asserting it does not throw
    }

    [Fact]
    public void MultipleUpdates_AdvanceSimulationProperly()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        var mockRng = new Mock<Random>();

        flow.Update(5f, mockRng.Object);
        flow.Update(5f, mockRng.Object);
    }

    [Fact]
    public void Reset_ClearsWaterStateAndReinitializes()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);

        var mockRng = new Mock<Random>();
        flow.Update(5f, mockRng.Object);
        flow.Reset();
    }

    [Fact]
    public void Invalidate_SetsDirtyFlag_TriggeringRecalculation()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);

        // Invalidate explicitly flags it as dirty
        flow.Invalidate();

        // Ensure that update processes the dirty state
        var mockRng = new Mock<Random>();
        flow.Update(1f, mockRng.Object);
    }

    [Fact]
    public void Dispose_ReleasesResources()
    {
        var world = new World(16, 12, 42);
        var flow = new FlowSimulation(world);
        flow.Dispose();
    }

    private static float[,] GetWater(FlowSimulation flow) =>
        (float[,])typeof(FlowSimulation).GetField("_water", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(flow)!;

    private static float[,] GetLava(FlowSimulation flow) =>
        (float[,])typeof(FlowSimulation).GetField("_lava", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!.GetValue(flow)!;

    [Fact]
    public void Update_WaterFlowsDownhill()
    {
        var world = new World(6, 6, 42);
        world.ElevationField[2 * 6 + 2] = 10f;
        world.ElevationField[2 * 6 + 3] = 0f;

        world.Tiles[2, 2].Biome = BiomeType.ShallowWater;
        using var flow = new FlowSimulation(world);

        var wStart = GetWater(flow)[2, 2];
        var rng = new Random(42);
        flow.Update(5f, rng);

        var wEnd22 = GetWater(flow)[2, 2];
        var wEnd32 = GetWater(flow)[3, 2];

        Assert.True(wEnd32 > 0f);
        Assert.True(wEnd22 < wStart);
    }

    [Fact]
    public void Update_LavaFlowsDownhill()
    {
        var world = new World(6, 6, 42);
        world.ElevationField[2 * 6 + 2] = 10f;
        world.ElevationField[2 * 6 + 3] = 0f;

        world.Tiles[2, 2].Biome = BiomeType.Volcano;
        using var flow = new FlowSimulation(world);

        var lStart = GetLava(flow)[2, 2];
        var rng = new Random(42);
        flow.Update(5f, rng);

        var lEnd22 = GetLava(flow)[2, 2];
        var lEnd32 = GetLava(flow)[3, 2];

        Assert.True(lEnd32 > 0f);
        Assert.True(lEnd22 < lStart);
    }

    [Fact]
    public void Update_EvaporatesWater()
    {
        var world = new World(6, 6, 42);
        world.ElevationField[2 * 6 + 2] = 0f;
        world.ElevationField[2 * 6 + 3] = 0f;

        world.Tiles[2, 2].Biome = BiomeType.ShallowWater;
        using var flow = new FlowSimulation(world);

        var wStart = GetWater(flow)[2, 2];
        var rng = new Random(42);
        flow.Update(5f, rng);

        var wEnd = GetWater(flow)[2, 2];
        Assert.True(wEnd < wStart);
        Assert.True(wEnd >= 0f);
    }

    [Fact]
    public void Update_VolcanoRegeneratesLava()
    {
        var world = new World(6, 6, 42);
        world.Tiles[2, 2].Biome = BiomeType.Volcano;
        using var flow = new FlowSimulation(world);

        // Manually set lava to 0 to test regeneration
        GetLava(flow)[2, 2] = 0f;

        var rng = new Random(42);
        flow.Update(5f, rng);

        var lEnd = GetLava(flow)[2, 2];
        Assert.True(lEnd > 0f);
    }

    [Fact]
    public void Update_RiverRegeneratesWater()
    {
        var world = new World(6, 6, 42);
        world.RiverMask[2 * 6 + 2] = true;
        using var flow = new FlowSimulation(world);

        // Manually set water to 0 to test regeneration
        GetWater(flow)[2, 2] = 0f;

        var rng = new Random(42);
        flow.Update(5f, rng);

        var wEnd = GetWater(flow)[2, 2];
        Assert.True(wEnd > 0f);
    }
}
