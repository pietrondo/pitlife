using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public enum UpdatePhase
{
    EarlyUpdate,
    Update,
    LateUpdate
}

public interface ISimulationSystem
{
    UpdatePhase Phase { get; }
    void Initialize(World world);
    void Tick(Ecosystem eco, GameTime gameTime);
    void Reset();
}
