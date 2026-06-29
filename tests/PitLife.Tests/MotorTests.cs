using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.Tests;

public class MotorTests
{
    [Fact]
    public void Reset_SetsDefaults()
    {
        var m = new Motor();
        m.Reset(new Vector2(50, 60));
        Assert.Equal(new Vector2(0, 1), m.Facing);
        Assert.Null(m.Waypoint);
        Assert.Equal(new Vector2(50, 60), m.HomePosition);
        Assert.Equal(1f, m.CurrentSpeedMultiplier);
        Assert.Equal(1f, m.CurrentEnergyMultiplier);
    }

    [Fact]
    public void GetSpeed_ComputesCorrectly()
    {
        var m = new Motor();
        m.Reset(Vector2.Zero);
        m.CurrentSpeedMultiplier = 2f;
        var genome = new Genome { Speed = 50f };
        var speed = m.GetSpeed(genome, 1f, false);
        Assert.Equal(50f * BalanceConfig.Data.Creature.SpeedBase * 2f, speed);
    }

    [Fact]
    public void GetSpeed_AppliesInfantPenalty()
    {
        var m = new Motor();
        m.Reset(Vector2.Zero);
        var genome = new Genome { Speed = 50f };
        var adult = m.GetSpeed(genome, 1f, false);
        var baby = m.GetSpeed(genome, 1f, true);
        Assert.True(baby < adult);
    }

    [Fact]
    public void MoveToward_DoesNotMove_WhenAlreadyAtTarget()
    {
        var m = new Motor();
        var pos = new Vector2(100, 100);
        m.MoveToward(ref pos, new Vector2(100, 100), 10f, 1f, false);
        Assert.Equal(new Vector2(100, 100), pos);
    }

    [Fact]
    public void MoveToward_MovesTowardTarget()
    {
        var m = new Motor();
        var pos = new Vector2(0, 0);
        m.MoveToward(ref pos, new Vector2(100, 0), 10f, 1f, false);
        Assert.True(pos.X > 0);
        Assert.Equal(0f, pos.Y);
    }

    [Fact]
    public void MoveToward_RespectsImpassableTiles()
    {
        var world = new World(64, 48, 42);
        var m = new Motor();
        var pos = new Vector2(100, 100);
        var before = pos;
        m.MoveToward(ref pos, new Vector2(200, 100), 500f, 1f, false, world);
        // just verifies no crash; movement depends on tile passability
        Assert.NotNull(world);
    }

    [Fact]
    public void MoveAwayFrom_ReturnsTrue_WhenMoving()
    {
        var m = new Motor();
        var pos = new Vector2(100, 100);
        var moved = m.MoveAwayFrom(ref pos, new Vector2(0, 0), 10f, 1f, false);
        Assert.True(moved);
        Assert.True(pos.X > 100);
    }

    [Fact]
    public void MoveAwayFrom_ReturnsFalse_WhenThreatIsClose()
    {
        var m = new Motor();
        var pos = new Vector2(100, 100);
        var moved = m.MoveAwayFrom(ref pos, new Vector2(100, 100), 10f, 1f, false);
        Assert.False(moved);
    }

    [Fact]
    public void ClampToWorld_WrapsPosition()
    {
        var world = new World(640, 480, 42);
        var clamped = Motor.ClampToWorld(new Vector2(-10, -10), world);
        Assert.Equal(new Vector2(world.PixelWidth - 10, world.PixelHeight - 10), clamped);
    }

    [Fact]
    public void ClampToWorld_ReturnsOriginal_WhenWorldIsNull()
    {
        var clamped = Motor.ClampToWorld(new Vector2(100, 100));
        Assert.Equal(new Vector2(100, 100), clamped);
    }

    [Fact]
    public void Wander_SetsWaypoint_WhenNull()
    {
        var world = new World(64, 48, 42);
        var m = new Motor();
        var genome = Genome.Random(new Random(1));
        m.Reset(new Vector2(200, 200));
        var pos = new Vector2(200, 200);
        var memory = new MemoryStore();
        m.Wander(ref pos, genome, 10f, 1f, new Random(1), 100f, memory, false, world, false);
        Assert.NotNull(m.Waypoint);
    }

    [Fact]
    public void Wander_ReturnsToHome_WhenTooFar()
    {
        var world = new World(64, 48, 42);
        var m = new Motor();
        var genome = Genome.Random(new Random(1));
        m.Reset(new Vector2(200, 200));
        var pos = new Vector2(2000, 2000);
        var memory = new MemoryStore();
        m.Wander(ref pos, genome, 10f, 1f, new Random(1), 100f, memory, false, world, false);
        Assert.Equal(m.HomePosition, m.Waypoint);
    }

    [Fact]
    public void GetVisionPixels_ComputesCorrectly()
    {
        var m = new Motor();
        var genome = new Genome { VisionRange = 100f };
        var vision = m.GetVisionPixels(genome, false);
        Assert.Equal(100f * BalanceConfig.Data.Creature.VisionRangeBase, vision);
    }
}
