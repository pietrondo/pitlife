namespace PitLife.Simulation;

internal sealed class ThreatModule : IBehaviorModule
{
    public bool Update(Creature self, World world, Ecosystem ecosystem, float dt)
    {
        Creature? threat = ecosystem.FindNearestPredator(self);
        if (threat != null && self.DistanceTo(threat) < self.VisionPixels * VisionScale(self))
        {
            self.MoveAwayFrom(threat.Position, dt, world);
            self.RememberDanger(threat.Position);
            return true;
        }
        return false;
    }

    private static float VisionScale(Creature self) => self.CreatureType switch
    {
        CreatureType.Herbivore => 0.8f,
        CreatureType.Omnivore => 0.6f,
        _ => 0f
    };
}
