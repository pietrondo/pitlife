namespace PitLife.Simulation;

internal interface IBehaviorModule
{
    bool Update(Creature self, World world, Ecosystem ecosystem, float dt);
}
