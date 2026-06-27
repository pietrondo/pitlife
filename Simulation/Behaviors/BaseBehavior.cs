using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class BaseBehavior : ICreatureBehavior
{
    private readonly ThreatModule _threat = new();
    private readonly FeedingModule _feeding = new();
    private readonly SocialModule _social = new();

    public void Update(Creature self, World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!self.IsAlive) return;
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0) return;

        if (_threat.Update(self, world, ecosystem, dt))
            return;

        if (self.Thirst > 30f && self.CreatureType != CreatureType.Plant)
        {
            var tile = world.GetTileAtPosition(self.Position.X, self.Position.Y);
            var nearWater = tile.Biome is BiomeType.ShallowWater or BiomeType.DeepOcean or BiomeType.CoralReef
                || world.IsRiverAt(self.Position.X, self.Position.Y);
            if (nearWater)
            {
                self.Thirst = 0f;
                return;
            }
        }

        if (self.IsBaby && self.Parent != null && self.Parent.IsAlive)
        {
            self.MoveToward(self.Parent.Position, dt, world);
            FeedingModule.TryGraze(self, world, dt);
            return;
        }

        if (self.IsAdult && self.CreatureType != CreatureType.Plant)
        {
            _social.DefendInfants(self, ecosystem, dt);
        }

        if (_feeding.Update(self, world, ecosystem, dt))
            return;

        if (_social.Update(self, world, ecosystem, dt))
            return;

        var wanderSpeed = self.CreatureType switch
        {
            CreatureType.Carnivore => 100f,
            CreatureType.Omnivore => 90f,
            _ => 80f
        };
        self.Wander(world, dt, ecosystem.Random, wanderSpeed);
    }
}
