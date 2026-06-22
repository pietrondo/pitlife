using System;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using PitLife.Simulation;
using Xunit;
using Xunit.Abstractions;

namespace PitLife.Tests;

public class PerformanceTests
{
    private readonly ITestOutputHelper _output;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void DeterministicReplay_LargePopulation_SameSeedSameResult()
    {
        int seed = 42;
        int herbivores = 40, carnivores = 15, omnivores = 10, plants = 80;

        var state1 = RunLargeSim(seed, 30, herbivores, carnivores, omnivores, plants, out _);
        var state2 = RunLargeSim(seed, 30, herbivores, carnivores, omnivores, plants, out _);

        Assert.Equal(state1.PlantCount, state2.PlantCount);
        Assert.Equal(state1.HerbivoreCount, state2.HerbivoreCount);
        Assert.Equal(state1.CarnivoreCount, state2.CarnivoreCount);
        Assert.Equal(state1.OmnivoreCount, state2.OmnivoreCount);
        Assert.Equal(state1.TotalTime, state2.TotalTime, 3);
    }

    [Fact]
    public void TickPerformance_UnderBudget()
    {
        var ecosystem = new Ecosystem(64, 48, 7);
        ecosystem.Initialize(30, 20, 10, 60);
        var dt = TimeSpan.FromSeconds(1f / 10f);
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), dt);

        var sw = Stopwatch.StartNew();
        int iterations = 100;
        for (int i = 0; i < iterations; i++)
            ecosystem.Tick(gameTime);
        sw.Stop();

        double avgMs = sw.Elapsed.TotalMilliseconds / iterations;
        _output.WriteLine($"Avg tick time: {avgMs:F2}ms (over {iterations} ticks, {ecosystem.Creatures.Count} creatures)");

        Assert.True(avgMs < 100,
            $"Average tick time {avgMs:F1}ms exceeds 100ms budget with {ecosystem.Creatures.Count} creatures");
    }

    [Fact]
    public void LargePopulation_NoExceptions()
    {
        for (int seed = 0; seed < 3; seed++)
        {
            var ecosystem = new Ecosystem(64, 48, seed * 137 + 42);
            ecosystem.Initialize(50, 30, 15, 100);

            for (int t = 0; t < 60; t++)
            {
                var dt = TimeSpan.FromSeconds(1f / 10f);
                var gameTime = new GameTime(TimeSpan.FromSeconds(t * dt.TotalSeconds), dt);
                try
                {
                    ecosystem.Tick(gameTime);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Exception at seed={seed} T={ecosystem.TotalTime:F1}s: {ex.Message}");
                }
            }
        }
    }

    [Fact]
    public void LargePopulation_DeterminismPreserved()
    {
        int totalTests = 5;
        for (int i = 0; i < totalTests; i++)
        {
            int seed = 1000 + i * 73;
            var r1 = RunLargeSim(seed, 15, 30, 10, 5, 50, out _);
            var r2 = RunLargeSim(seed, 15, 30, 10, 5, 50, out _);

            Assert.Equal(r1.PlantCount, r2.PlantCount);
            Assert.Equal(r1.HerbivoreCount, r2.HerbivoreCount);
            Assert.Equal(r1.CarnivoreCount, r2.CarnivoreCount);
        }
    }

    [Fact]
    public void MemoryStable_OverExtendedSimulation()
    {
        var ecosystem = new Ecosystem(48, 36, 42);
        ecosystem.Initialize(20, 15, 5, 40);

        GC.Collect();
        long memBefore = GC.GetTotalMemory(forceFullCollection: true);

        for (int t = 0; t < 300; t++)
        {
            var dt = TimeSpan.FromSeconds(1f / 10f);
            var gameTime = new GameTime(TimeSpan.FromSeconds(t * dt.TotalSeconds), dt);
            ecosystem.Tick(gameTime);
        }

        GC.Collect();
        long memAfter = GC.GetTotalMemory(forceFullCollection: true);
        long growth = memAfter - memBefore;

        _output.WriteLine($"Memory before: {memBefore / 1024}KB, after: {memAfter / 1024}KB, growth: {growth / 1024}KB");
        Assert.True(growth < 50 * 1024 * 1024,
            $"Memory growth {growth / 1024}KB exceeds 50MB limit");
    }

    private static SimulationState RunLargeSim(int seed, float seconds,
        int herbivores, int carnivores, int omnivores, int plants,
        out AssertionTracker tracker)
    {
        return StabilityTests.RunSimulation(seed, seconds, out tracker, plants, herbivores, carnivores, omnivores);
    }
}
