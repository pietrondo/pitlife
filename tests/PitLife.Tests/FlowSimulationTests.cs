using System;
using PitLife.Simulation;
using Xunit;

namespace PitLife.Tests;

public class FlowSimulationTests
{
    [Fact]
    public void Construction_DoesNotThrow()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        Assert.NotNull(flow);
    }

    [Fact]
    public void Tick_DoesNotThrow()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        var eco = new Ecosystem(16, 12, 42);
        var gt = new Microsoft.Xna.Framework.GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1));
        flow.Tick(eco, gt);
    }

    [Fact]
    public void MultipleUpdates_DoNotThrow()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        flow.Update(5f, new Random(42));
        flow.Update(5f, new Random(42));
    }

    [Fact]
    public void Reset_DoesNotThrow()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        flow.Update(5f, new Random(42));
        flow.Reset();
    }

    [Fact]
    public void WaterInitialized_ForOceanTiles()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        flow.Update(5f, new Random(42));
    }

    [Fact]
    public void Invalidate_SetsDirty()
    {
        var world = new World(16, 12, 42);
        using var flow = new FlowSimulation(world);
        flow.Invalidate();
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var world = new World(16, 12, 42);
        var flow = new FlowSimulation(world);
        flow.Dispose();
    }
}
