using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public enum SimulationPhase
{
    EarlyUpdate = 0,
    Update = 1,
    LateUpdate = 2
}

public interface ISimulationSystem
{
    SimulationPhase Phase { get; }
    void Tick(Ecosystem ecosystem, GameTime gameTime);
    void Initialize(World world);
    void Reset();
}
