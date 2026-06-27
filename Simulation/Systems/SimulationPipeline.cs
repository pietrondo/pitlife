using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class SimulationPipeline
{
    private readonly List<ISimulationSystem> _systems = new();
    private readonly Dictionary<Type, ISimulationSystem> _systemLookup = new();

    public void AddSystem(ISimulationSystem system)
    {
        _systems.Add(system);
        _systemLookup[system.GetType()] = system;

        // Ensure stable sort by keeping track of original insertion order
        var sorted = _systems
            .Select((s, index) => new { System = s, Index = index })
            .OrderBy(x => x.System.Phase)
            .ThenBy(x => x.Index)
            .ToList();

        _systems.Clear();
        foreach(var item in sorted)
        {
            _systems.Add(item.System);
        }
    }

    public void Initialize(World world)
    {
        foreach (var system in _systems)
        {
            system.Initialize(world);
        }
    }

    public void Tick(Ecosystem ecosystem, GameTime gameTime)
    {
        foreach (var system in _systems)
        {
            system.Tick(ecosystem, gameTime);
        }
    }

    public void Reset()
    {
        foreach (var system in _systems)
        {
            system.Reset();
        }
    }

    public T? GetSystem<T>() where T : class, ISimulationSystem
    {
        if (_systemLookup.TryGetValue(typeof(T), out var system))
        {
            return (T)system;
        }
        return null;
    }
}
