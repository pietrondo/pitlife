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
            CreatureType.Carnivore => PitLife.Core.FeedingConfig.Instance.HungerThresholdCarnivore,
            _ => PitLife.Core.FeedingConfig.Instance.HungerThresholdHerbivore
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

        Plant? food = FindFoodPlant(self, ecosystem);

        if (food == null)
            return TryScavengeCarcass(self, ecosystem, dt);

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            TryGraze(self, world, dt);
            return true;
        }

        ConsumePlantHerbivore(self, food, dt, PitLife.Core.FeedingConfig.Instance.HerbivoreConsumeRate);
        return true;
    }

    private static bool TryEatNearbyFruit(Creature self, Ecosystem ecosystem)
    {
        var fruit = ecosystem.Fruits.TryEatFruit(self.Position, PitLife.Core.FeedingConfig.Instance.MaxFruitEatRange);
        if (!fruit.HasValue) return false;

        if (fruit.Value.Poisonous && self.Genome.PlantRecognition < 0.5f)
        {
            self.Energy -= fruit.Value.EnergyValue * PitLife.Core.FeedingConfig.Instance.PoisonFruitDamageMultiplier;
            self.RememberDanger(fruit.Value.Position);
            return true;
        }

        self.Energy = Math.Min(self.Energy + fruit.Value.EnergyValue, self.MaxEnergy);
        self.RememberFood(fruit.Value.Position);
        return true;
    }

    private static void ConsumePlantHerbivore(Creature self, Plant food, float dt, float rate)
    {
        var eaten = Math.Min(food.Energy, rate * dt);
        food.Energy -= eaten;
        ProcessPlantDigestion(self, food, eaten);
        self.RememberFood(food.Position);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);
    }

    private static void ProcessPlantDigestion(Creature self, Plant food, float eaten)
    {
        if (food.IsPoisonous && self.Genome.PlantRecognition < 0.5f)
        {
            self.Energy -= eaten * PitLife.Core.FeedingConfig.Instance.PoisonPlantDamageMultiplier;
            self.RememberDanger(food.Position);
            return;
        }

        self.Energy = Math.Min(self.Energy + eaten * PitLife.Core.FeedingConfig.Instance.PlantDigestionRate, self.MaxEnergy);
    }

    private static void ConsumePlantOmnivore(Creature self, Plant food, float dt, float rate)
    {
        var eaten = Math.Min(food.Energy, rate * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * PitLife.Core.FeedingConfig.Instance.OmnivorePlantDigestion, self.MaxEnergy);
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
        var escaped = prey.Energy > 0 && prey.Speed > self.Speed * (1f + PitLife.Core.FeedingConfig.Instance.PreyEscapeThreshold);
        if (escaped) return;

        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / PitLife.Core.FeedingConfig.Instance.DefenseDivisor);
        var cost = self.CreatureType == CreatureType.Carnivore ? PitLife.Core.FeedingConfig.Instance.CarnivoreAttackCost : PitLife.Core.FeedingConfig.Instance.OmnivoreAttackCost;
        self.Energy -= cost;
        self.Energy = Math.Min(self.Energy + damage * PitLife.Core.FeedingConfig.Instance.AttackEnergyGain * (1f - prey.Toxicity * PitLife.Core.FeedingConfig.Instance.ToxicityReduction), self.MaxEnergy);
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

        ConsumePlantOmnivore(self, food, dt, PitLife.Core.FeedingConfig.Instance.OmnivoreConsumeRate);
        return true;
    }

    private static bool TryHuntAsOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var cfg = PitLife.Core.FeedingConfig.Instance;
        var seekPrey = self.Energy < self.MaxEnergy * cfg.OmnivoreHuntThreshold && ecosystem.Random.NextDouble() < cfg.OmnivoreHuntThreshold;
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
        Plant? food = FindFoodPlant(self, ecosystem);

        if (food == null || self.DistanceTo(food) >= 12f)
            return TryGraze(self, world, dt);

        ConsumePlantHerbivore(self, food, dt, PitLife.Core.FeedingConfig.Instance.HerbivoreConsumeRate);
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
        if (TryAttackNearbyPrey(self, ecosystem, dt))
            return true;

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null || self.DistanceTo(food) >= 12f)
            return TryScavengeCarcass(self, ecosystem, dt);

        ConsumePlantOmnivore(self, food, dt, PitLife.Core.FeedingConfig.Instance.OmnivoreConsumeRate);
        return true;
    }

    internal static bool TryGraze(Creature self, World world, float dt)
    {
        var tile = world.GetTileAtPosition(self.Position.X, self.Position.Y);
        if (tile.GrassAmount <= 0.001f) return false;

            var grazeRate = PitLife.Core.FeedingConfig.Instance.GrazeRate * dt;
        var eaten = tile.EatGrass(grazeRate);
        if (eaten <= 0) return false;

        self.Energy = Math.Min(self.Energy + eaten * PitLife.Core.FeedingConfig.Instance.HerbivorePlantEnergy, self.MaxEnergy);
        return true;
    }

    private static bool TryScavengeCarcass(Creature self, Ecosystem ecosystem, float dt)
    {
        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || c.IsAlive || c.CreatureType == CreatureType.Plant) continue;
            if (self.DistanceTo(c) >= PitLife.Core.FeedingConfig.Instance.ScavengeRange || c.Energy <= 0) continue;

            var eaten = Math.Min(c.Energy, PitLife.Core.FeedingConfig.Instance.ScavengeEatRate * dt);
            c.Energy -= eaten;
            self.Energy = Math.Min(self.Energy + eaten * PitLife.Core.FeedingConfig.Instance.ScavengeEnergyGain, self.MaxEnergy);
            return true;
        }
        return false;
    }

    private static Plant? FindFoodPlant(Creature self, Ecosystem ecosystem)
    {
        return self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);
    }

    private static bool TryAttackNearbyPrey(Creature self, Ecosystem ecosystem, float dt)
    {
        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= 10f)
            return false;

        var attackDamage = self is Omnivore om ? om.AttackDamage : 12f;
        AttackPrey(self, prey, attackDamage * dt);
        return true;
    }
}
