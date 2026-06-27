using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class PlantBehavior : ICreatureBehavior
{
    public void Update(Creature self, World world, Ecosystem ecosystem, float dt)
    {
        if (!self.IsAlive) return;
        if (self is not Plant plant) return;


        var tile = world.GetTileAtPosition(self.Position.X, self.Position.Y);
        float sunlight = tile.Vegetation * 0.5f + 0.5f;
        float energyGain = plant.GrowthRate * sunlight * dt;
        self.Energy = Math.Min(self.Energy + energyGain, self.MaxEnergy);

        if (self.Energy >= self.ReproductionThreshold)
            ecosystem.TrySpreadPlant(plant);

        if (self.Age > 300f) self.Die(DeathCause.OldAge);
    }
}
