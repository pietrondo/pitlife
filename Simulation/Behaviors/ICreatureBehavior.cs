using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public interface ICreatureBehavior
{
    void Update(Creature self, World world, Ecosystem ecosystem, float dt);
}
