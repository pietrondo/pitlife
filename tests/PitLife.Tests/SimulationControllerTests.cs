using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SimulationControllerTests
{
    private static (SimulationController c, Ecosystem e, DayNightCycle d) MakeController(int seed = 42)
    {
        var eco = new Ecosystem(64, 48, seed);
        eco.Initialize(h: 5, c: 2, o: 1, p: 10);
        var dayNight = new DayNightCycle();
        return (new SimulationController(eco, dayNight), eco, dayNight);
    }

    [Fact]
    public void Advance_TicksSimulation_WhenRunning()
    {
        var (c, eco, _) = MakeController();
        var initialTime = eco.TotalTime;
        c.Advance(0.5f);
        Assert.True(eco.TotalTime > initialTime);
    }

    [Fact]
    public void Advance_DoesNotTick_WhenPaused()
    {
        var (c, eco, _) = MakeController();
        c.SetPause(true);
        var initialTime = eco.TotalTime;
        c.Advance(0.5f);
        Assert.Equal(initialTime, eco.TotalTime);
    }

    [Fact]
    public void TogglePause_TogglesState()
    {
        var (c, _, _) = MakeController();
        Assert.False(c.IsPaused);
        c.TogglePause();
        Assert.True(c.IsPaused);
        c.TogglePause();
        Assert.False(c.IsPaused);
    }

    [Fact]
    public void SetSpeed_ChangesSpeedLevel()
    {
        var (c, _, _) = MakeController();
        c.SetSpeed(3);
        Assert.Equal(3, c.SpeedLevel);
        Assert.Equal(4f, c.CurrentSpeed);
    }

    [Fact]
    public void SetSpeed_ZeroPausesSimulation()
    {
        var (c, _, _) = MakeController();
        c.SetSpeed(0);
        Assert.True(c.IsPaused);
    }

    [Fact]
    public void CurrentSpeed_ReturnsCorrectMultiplier()
    {
        var (c, _, _) = MakeController();
        Assert.Equal(1f, c.CurrentSpeed);
        c.SetSpeed(2);
        Assert.Equal(2f, c.CurrentSpeed);
    }

    [Fact]
    public void Advance_AccumulatesTime_AndTicksInFixedSteps()
    {
        var (c, eco, _) = MakeController();
        c.Advance(1f);
        Assert.True(eco.TotalTime > 0.9f, $"Should accumulate ~1s, got {eco.TotalTime}");
        Assert.True(eco.TotalTime <= 1.1f, $"Should not overshoot significantly, got {eco.TotalTime}");
    }
}
