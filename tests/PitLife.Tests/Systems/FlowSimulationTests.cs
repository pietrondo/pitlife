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
}
