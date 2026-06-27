using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class SimulationPipeline
{
    private readonly List<ISimulationSystem> _systems = new();

    public void Add(ISimulationSystem system)
    {
        _systems.Add(system);
    }

    public void Initialize(World world)
    {
        foreach (var sys in _systems)
            sys.Initialize(world);
    }

    public void Tick(Ecosystem ecosystem, GameTime gameTime)
    {
        foreach (var sys in _systems)
            sys.Tick(ecosystem, gameTime);
    }

    public void Reset()
    {
        foreach (var sys in _systems)
            sys.Reset();
    }

    public T? Get<T>() where T : class, ISimulationSystem
    {
        foreach (var sys in _systems)
            if (sys is T t) return t;
        return null;
    }
}
