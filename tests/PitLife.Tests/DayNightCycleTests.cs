using PitLife.Rendering;

namespace PitLife.Tests;

public class DayNightCycleTests
{
    [Fact]
    public void Phase_IsDay_AtMidCycle()
    {
        Assert.Equal(DayPhase.Day, DayNightCycle.GetPhase(DayNightCycle.DayLength * 0.25f));
    }

    [Fact]
    public void Phase_IsNight_AtThreeQuarters()
    {
        Assert.Equal(DayPhase.Night, DayNightCycle.GetPhase(DayNightCycle.DayLength * 0.75f));
    }

    [Fact]
    public void Phase_IsDawn_AtStart()
    {
        Assert.Equal(DayPhase.Dawn, DayNightCycle.GetPhase(0f));
    }

    [Fact]
    public void Phase_IsDusk_AtHalfCycle()
    {
        Assert.Equal(DayPhase.Dusk, DayNightCycle.GetPhase(DayNightCycle.DayLength * 0.50f));
    }

    [Fact]
    public void Overlay_IsTransparent_DuringDay()
    {
        var cycle = new DayNightCycle();
        cycle.Update(DayNightCycle.DayLength * 0.25f);
        Assert.Equal(0, cycle.GetOverlayColor().A);
    }

    [Fact]
    public void Overlay_IsOpaque_DuringNight()
    {
        var cycle = new DayNightCycle();
        cycle.Update(DayNightCycle.DayLength * 0.75f);
        Assert.True(cycle.GetOverlayColor().A >= 80);
    }

    [Fact]
    public void Update_WrapsAround_AfterFullCycle()
    {
        var cycle = new DayNightCycle();
        cycle.Update(DayNightCycle.DayLength * 2.25f);
        Assert.Equal(DayPhase.Day, cycle.Phase);
    }
}
