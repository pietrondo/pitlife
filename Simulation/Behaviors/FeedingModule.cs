using System;

namespace PitLife.Simulation;

internal sealed class FeedingModule : IBehaviorModule
{
    public bool Update(Creature self, World world, Ecosystem ecosystem, float dt)
    {
        if (IsHungry(self) && TryFeed(self, ecosystem, dt, world))
            return true;

        if (TryFeedNearby(self, ecosystem, dt, world))
            return true;

        return false;
    }

    private static bool IsHungry(Creature self)
    {
        var threshold = self.CreatureType switch
        {
            CreatureType.Carnivore => 0.8f,
            _ => 0.6f
        };
        return self.Energy < self.MaxEnergy * threshold;
    }

    private static bool TryFeed(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        return self.CreatureType switch
        {
            CreatureType.Herbivore => TryFeedHerbivore(self, ecosystem, dt, world),
            CreatureType.Carnivore => TryFeedCarnivore(self, ecosystem, dt, world),
            CreatureType.Omnivore => TryFeedOmnivore(self, ecosystem, dt, world),
            _ => false
        };
    }

    private static bool TryFeedHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (TryEatNearbyFruit(self, ecosystem)) return true;

        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food == null)
            return TryScavengeCarcass(self, ecosystem, dt);

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            TryGraze(self, world, dt);
            return true;
        }

        ConsumePlantHerbivore(self, food, dt, 10f);
        return true;
    }

    private static bool TryEatNearbyFruit(Creature self, Ecosystem ecosystem)
    {
        var fruit = ecosystem.Fruits.TryEatFruit(self.Position, 12f);
        if (!fruit.HasValue) return false;

        if (fruit.Value.Poisonous && self.Genome.PlantRecognition < 0.5f)
        {
            self.Energy -= fruit.Value.EnergyValue * 2f;
            self.RememberDanger(fruit.Value.Position);
        }
        else
        {
            self.Energy = Math.Min(self.Energy + fruit.Value.EnergyValue, self.MaxEnergy);
            self.RememberFood(fruit.Value.Position);
        }
        return true;
    }

    private static void ConsumePlantHerbivore(Creature self, Plant food, float dt, float rate)
    {
        var eaten = Math.Min(food.Energy, rate * dt);
        food.Energy -= eaten;
        if (food.IsPoisonous && self.Genome.PlantRecognition < 0.5f)
        {
            self.Energy -= eaten * 3f;
            self.RememberDanger(food.Position);
        }
        else
        {
            self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
        }
        self.RememberFood(food.Position);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);
    }

    private static void ConsumePlantOmnivore(Creature self, Plant food, float dt, float rate)
    {
        var eaten = Math.Min(food.Energy, rate * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);
    }

    private static bool TryFeedCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return false;

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= self.VisionPixels)
            return TryScavengeCarcass(self, ecosystem, dt);

        if (self.DistanceTo(prey) >= 10f)
        {
            self.MoveToward(prey.Position, dt, world);
            return true;
        }

        AttackPrey(self, prey, carn.AttackDamage * dt);
        return true;
    }

    private static void AttackPrey(Creature self, Creature prey, float damage)
    {
        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
        self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
        if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
    }

    private static bool TryFeedOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (TryHuntAsOmnivore(self, ecosystem, dt, world)) return true;

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null)
            return false;

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            return true;
        }

        ConsumePlantOmnivore(self, food, dt, 8f);
        return true;
    }

    private static bool TryHuntAsOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var seekPrey = self.Energy < self.MaxEnergy * 0.4f && ecosystem.Random.NextDouble() < 0.4;
        if (!seekPrey) return false;

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= self.VisionPixels) return false;

        if (self.DistanceTo(prey) >= 10f)
        {
            self.MoveToward(prey.Position, dt, world);
            return true;
        }

        var attackDamage = self is Omnivore om ? om.AttackDamage : 12f;
        AttackPrey(self, prey, attackDamage * dt);
        return true;
    }

    private static bool TryFeedNearby(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        return self.CreatureType switch
        {
            CreatureType.Herbivore => TryFeedNearbyHerbivore(self, ecosystem, dt, world),
            CreatureType.Carnivore => TryFeedNearbyCarnivore(self, ecosystem, dt, world),
            CreatureType.Omnivore => TryFeedNearbyOmnivore(self, ecosystem, dt, world),
            _ => false
        };
    }

    private static bool TryFeedNearbyHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food == null || self.DistanceTo(food) >= 12f)
            return TryGraze(self, world, dt);

        ConsumePlantHerbivore(self, food, dt, 10f);
        return true;
    }

    private static bool TryFeedNearbyCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return TryScavengeCarcass(self, ecosystem, dt);

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= 10f)
            return TryScavengeCarcass(self, ecosystem, dt);

        AttackPrey(self, prey, carn.AttackDamage * dt);
        return true;
    }

    private static bool TryFeedNearbyOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey != null && self.DistanceTo(prey) < 10f)
        {
            var attackDamage = self is Omnivore om ? om.AttackDamage : 12f;
            AttackPrey(self, prey, attackDamage * dt);
            return true;
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null || self.DistanceTo(food) >= 12f)
            return TryScavengeCarcass(self, ecosystem, dt);

        ConsumePlantOmnivore(self, food, dt, 8f);
        return true;
    }

    internal static bool TryGraze(Creature self, World world, float dt)
    {
        var tile = world.GetTileAtPosition(self.Position.X, self.Position.Y);
        if (tile.GrassAmount <= 0.001f) return false;
        var grazeRate = 3f * dt;
        var eaten = tile.EatGrass(grazeRate);
        if (eaten > 0)
        {
            self.Energy = Math.Min(self.Energy + eaten * 8f, self.MaxEnergy);
            return true;
        }
        return false;
    }

    private static bool TryScavengeCarcass(Creature self, Ecosystem ecosystem, float dt)
    {
        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || c.IsAlive || c.CreatureType == CreatureType.Plant) continue;
            if (self.DistanceTo(c) < 10f && c.Energy > 0)
            {
                var eaten = Math.Min(c.Energy, 8f * dt);
                c.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 1.2f, self.MaxEnergy);
                return true;
            }
        }
        return false;
    }
}
