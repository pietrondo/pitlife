using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Benchmarks;

[MemoryDiagnoser]
[ShortRunJob]
public class EcosystemBenchmarks
{
    private Ecosystem _eco = null!;

    [GlobalSetup]
    public void Setup()
    {
        _eco = new Ecosystem(200, 150, 42);
        _eco.Initialize(100, 20, 10, 50);
    }

    [Benchmark]
    public void Tick_OneSecond()
    {
        _eco.SimulationSpeed = 1f;
        _eco.Tick(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
    }

    [Benchmark]
    public void Tick_FourXSpeed()
    {
        _eco.SimulationSpeed = 4f;
        _eco.Tick(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class WorldGenBenchmarks
{
    [Benchmark]
    [Arguments(96, 72)]
    [Arguments(200, 150)]
    [Arguments(400, 300)]
    public World Generate(int width, int height)
    {
        return new World(width, height, 42);
    }
}

[MemoryDiagnoser]
[ShortRunJob]
public class SpatialGridBenchmarks
{
    private SpatialGrid _grid = null!;
    private Ecosystem _eco = null!;

    [GlobalSetup]
    public void Setup()
    {
        _eco = new Ecosystem(200, 150, 42);
        _eco.Initialize(100, 20, 10, 50);
        _grid = new SpatialGrid(_eco.World.PixelWidth, _eco.World.PixelHeight, _eco.World.TileSize * 2);
        foreach (var c in _eco.Creatures)
            _grid.Update(c);
    }

    [Benchmark]
    public void GetNeighbors_FromCenter()
    {
        var center = _eco.Creatures[_eco.Creatures.Count / 2];
        _grid.GetNeighbors(center, 100f, _ => true);
    }
}

[MemoryDiagnoser]
[SimpleJob(launchCount: 1, warmupCount: 1, iterationCount: 3)]
public class EcosystemTickBenchmarks
{
    private Ecosystem _eco = null!;

    [Params(1, 10, 100, 1000)]
    public int CreatureCount { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _eco = new Ecosystem(200, 150, 42);
        _eco.Initialize(CreatureCount, 0, 0, 0); // h, c, o, p
    }

    [Benchmark]
    public void TickLoop()
    {
        for (var i = 0; i < 60; i++)
        {
            _eco.Tick(new GameTime(TimeSpan.FromSeconds(1.0 / 60.0), TimeSpan.FromSeconds(1.0 / 60.0)));
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run(
        [
            typeof(EcosystemBenchmarks),
            typeof(WorldGenBenchmarks),
            typeof(SpatialGridBenchmarks),
            typeof(EcosystemTickBenchmarks)
        ]);
    }
}
