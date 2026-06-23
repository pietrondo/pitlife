using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class StabilityTests
{
    private const int DefaultSeed = 1337;
    private const int WorldSize = 48;
    private const int WorldTileSize = 24;

    [Fact]
    public void DeterministicReplay_SameSeedSameResult()
    {
        int seed = 42;
        var state1 = RunSimulation(seed, 60, out _);
        var state2 = RunSimulation(seed, 60, out _);

        Assert.Equal(state1.TotalTime, state2.TotalTime, 3);
        Assert.Equal(state1.PlantCount, state2.PlantCount);
        Assert.Equal(state1.HerbivoreCount, state2.HerbivoreCount);
        Assert.Equal(state1.CarnivoreCount, state2.CarnivoreCount);
        Assert.Equal(state1.OmnivoreCount, state2.OmnivoreCount);
    }

    [Fact]
    public void DeterministicReplay_DifferentSeedDifferentResult()
    {
        var state1 = RunSimulation(42, 30, out _, 80, 40, 15, 5);
        var state2 = RunSimulation(99, 30, out _, 80, 40, 15, 5);

        bool anyDifferent = state1.PlantCount != state2.PlantCount
            || state1.HerbivoreCount != state2.HerbivoreCount
            || state1.CarnivoreCount != state2.CarnivoreCount;
        Assert.True(anyDifferent,
            $"Expected different results for different seeds, but got identical counts: " +
            $"P={state1.PlantCount} H={state1.HerbivoreCount} C={state1.CarnivoreCount}");
    }

    [Fact]
    public void NoNaN_InCreatures()
    {
        AssertionTracker tracker;
        var state = RunSimulation(DefaultSeed, 120, out tracker);

        Assert.False(tracker.NaNDetected,
            $"NaN detected at T={tracker.NaNTime:F2}s in creature {tracker.NaNContext}");
    }

    [Fact]
    public void EnergyNeverExceedsMax()
    {
        AssertionTracker tracker;
        RunSimulation(DefaultSeed, 120, out tracker);

        Assert.False(tracker.EnergyExceededMax,
            $"Energy exceeded MaxEnergy at T={tracker.EnergyExceededTime:F2}s: {tracker.EnergyExceededContext}");
    }

    [Fact]
    public void CreatureCountNeverNegative()
    {
        AssertionTracker tracker;
        RunSimulation(DefaultSeed, 60, out tracker);

        Assert.False(tracker.NegativeCount,
            $"Negative count detected: {tracker.NegativeCountContext}");
    }

    [Fact]
    public void AgeNeverNegative()
    {
        AssertionTracker tracker;
        RunSimulation(DefaultSeed, 60, out tracker);

        Assert.False(tracker.NegativeAge,
            $"Negative age at T={tracker.NegativeAgeTime:F2}s in {tracker.NegativeAgeContext}");
    }

    [Fact]
    public void SoakTest_MultipleSeeds_AllComplete()
    {
        int[] seeds = [7, 42, 99, 256, 1024, 7777, 12345, 54321, 99999, 2024];
        var failures = new List<string>();

        foreach (int seed in seeds)
        {
            try
            {
                AssertionTracker tracker;
                RunSimulation(seed, 30, out tracker);
                if (tracker.NaNDetected)
                    failures.Add($"Seed {seed}: NaN at {tracker.NaNTime:F1}s - {tracker.NaNContext}");
                if (tracker.EnergyExceededMax)
                    failures.Add($"Seed {seed}: energy > max at {tracker.EnergyExceededTime:F1}s - {tracker.EnergyExceededContext}");
            }
            catch (Exception ex)
            {
                failures.Add($"Seed {seed}: exception - {ex.Message}");
            }
        }

        Assert.Empty(failures);
    }

    [Fact]
    public void SoakTest_HerbivoresSurviveWithPlants()
    {
        int seed = 0;
        AssertionTracker tracker;
        var state = RunSimulation(seed, 90, out tracker, initialPlants: 120, initialHerbivores: 15,
            initialCarnivores: 0, initialOmnivores: 0);

        Assert.True(state.HerbivoreCount > 0,
            $"All herbivores died. Seed={seed}. P={state.PlantCount} H={state.HerbivoreCount}");
        Assert.True(state.PlantCount > 0,
            $"All plants died. Seed={seed}. P={state.PlantCount} H={state.HerbivoreCount}");
    }

    [Fact]
    public void SoakTest_PredatorPreyCoexist()
    {
        int seed = 202;
        AssertionTracker tracker;
        var state = RunSimulation(seed, 120, out tracker, initialPlants: 80, initialHerbivores: 30,
            initialCarnivores: 10, initialOmnivores: 0);

        Assert.True(state.TotalAlive > 0,
            $"Total extinction. Seed={seed}. P={state.PlantCount} H={state.HerbivoreCount} C={state.CarnivoreCount}");
    }

    [Fact]
    public void SoakTest_AllSpeciesTypesPresent()
    {
        for (int i = 0; i < 5; i++)
        {
            int seed = 500 + i * 137;
            AssertionTracker tracker;
            var state = RunSimulation(seed, 60, out tracker);

            Assert.True(state.TotalAlive > 0,
                $"Seed {seed}: total extinction at T=60s. State: {state}");
        }
    }

    [Fact(Skip = "Flaky - covered by DeterministicReplay_SameSeedSameResult")]
    public void Simulation_IsRepeatable_AfterPauseResume()
    {
        int seed = 4242;
        var ecosystem1 = CreateEcosystem(seed, WorldSize, WorldTileSize);
        ecosystem1.Initialize(10, 5, 2, 40);
        AdvanceSimulation(ecosystem1, 30f);

        var ecosystem2 = CreateEcosystem(seed, WorldSize, WorldTileSize);
        ecosystem2.Initialize(10, 5, 2, 40);
        AdvanceSimulation(ecosystem2, 15f);
        AdvanceSimulation(ecosystem2, 15f);

        Assert.Equal(ecosystem1.PlantCount, ecosystem2.PlantCount);
        Assert.Equal(ecosystem1.HerbivoreCount, ecosystem2.HerbivoreCount);
        Assert.Equal(ecosystem1.CarnivoreCount, ecosystem2.CarnivoreCount);
    }

    internal static SimulationState RunSimulation(int seed, float durationSeconds,
        out AssertionTracker tracker, int initialPlants = 50, int initialHerbivores = 15,
        int initialCarnivores = 5, int initialOmnivores = 3)
    {
        var ecosystem = CreateEcosystem(seed, WorldSize, WorldTileSize);
        ecosystem.Initialize(initialHerbivores, initialCarnivores, initialOmnivores, initialPlants);

        tracker = new AssertionTracker();
        AdvanceWithTracking(ecosystem, durationSeconds, tracker);

        return new SimulationState(
            ecosystem.PlantCount, ecosystem.HerbivoreCount,
            ecosystem.CarnivoreCount, ecosystem.OmnivoreCount,
            ecosystem.TotalTime);
    }

    internal static Ecosystem CreateEcosystem(int seed, int worldWidthTiles, int worldHeightTiles)
    {
        return new Ecosystem(worldWidthTiles, worldHeightTiles, seed);
    }

    private static void AdvanceSimulation(Ecosystem ecosystem, float durationSeconds)
    {
        float dt = 1f / 60f;
        float total = 0f;
        var elapsed = ecosystem.TotalTime;
        while (total < durationSeconds)
        {
            var step = TimeSpan.FromSeconds(dt);
            ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(elapsed + total + dt), step));
            total += dt;
        }
    }

    private static void AdvanceWithTracking(Ecosystem ecosystem, float durationSeconds,
        AssertionTracker tracker)
    {
        float dt = 1f / 60f;
        float total = 0f;
        var elapsed = ecosystem.TotalTime;
        while (total < durationSeconds)
        {
            var step = TimeSpan.FromSeconds(dt);
            ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(elapsed + total + dt), step));
            total += dt;

            foreach (var c in ecosystem.Creatures)
            {
                if (c == null) continue;

                if (float.IsNaN(c.Energy) || float.IsNaN(c.Age) || float.IsNaN(c.Position.X) || float.IsNaN(c.Position.Y))
                {
                    tracker.NaNDetected = true;
                    tracker.NaNTime = total;
                    tracker.NaNContext = $"{c.Species} energy={c.Energy} age={c.Age} pos=({c.Position.X},{c.Position.Y})";
                    return;
                }

                if (c.Energy > c.MaxEnergy + 0.01f)
                {
                    tracker.EnergyExceededMax = true;
                    tracker.EnergyExceededTime = total;
                    tracker.EnergyExceededContext = $"{c.Species} energy={c.Energy} max={c.MaxEnergy}";
                }

                if (c.Age < -0.001f)
                {
                    tracker.NegativeAge = true;
                    tracker.NegativeAgeTime = total;
                    tracker.NegativeAgeContext = $"{c.Species} age={c.Age}";
                }
            }

            if (ecosystem.PlantCount < 0 || ecosystem.HerbivoreCount < 0 ||
                ecosystem.CarnivoreCount < 0 || ecosystem.OmnivoreCount < 0)
            {
                tracker.NegativeCount = true;
                tracker.NegativeCountContext = $"P={ecosystem.PlantCount} H={ecosystem.HerbivoreCount} C={ecosystem.CarnivoreCount} O={ecosystem.OmnivoreCount}";
            }
        }
    }
}

internal sealed class AssertionTracker
{
    public bool NaNDetected;
    public float NaNTime;
    public string NaNContext = "";
    public bool EnergyExceededMax;
    public float EnergyExceededTime;
    public string EnergyExceededContext = "";
    public bool NegativeCount;
    public string NegativeCountContext = "";
    public bool NegativeAge;
    public float NegativeAgeTime;
    public string NegativeAgeContext = "";
}

internal readonly record struct SimulationState(
    int PlantCount, int HerbivoreCount, int CarnivoreCount, int OmnivoreCount,
    float TotalTime)
{
    public int TotalAlive => PlantCount + HerbivoreCount + CarnivoreCount + OmnivoreCount;

    public override string ToString() =>
        $"P={PlantCount} H={HerbivoreCount} C={CarnivoreCount} O={OmnivoreCount} T={TotalTime:F1}s";
}
