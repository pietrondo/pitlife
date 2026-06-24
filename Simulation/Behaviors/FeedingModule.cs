using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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
        float threshold = self.CreatureType switch
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
        // Try eating a nearby fruit first
        var fruit = ecosystem.Fruits.TryEatFruit(self.Position, 12f);
        if (fruit.HasValue)
        {
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

        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);

        if (food == null)
            return TryEatCarcass(self, ecosystem, dt);

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            TryGraze(self, world, dt);
            return true;
        }

        float eaten = Math.Min(food.Energy, 10f * dt);
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

        return true;
    }

    private static bool TryFeedCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return false;

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= self.VisionPixels)
        {
            if (TryEatCarcass(self, ecosystem, dt)) return true;
            return false;
        }

        if (self.DistanceTo(prey) >= 10f)
        {
            self.MoveToward(prey.Position, dt, world);
            return true;
        }

        float damage = carn.AttackDamage * dt;
        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
        self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
        if (prey.Energy <= 0) prey.Die(DeathCause.Predation);

        return true;
    }

    private static bool TryFeedOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        bool seekPrey = self.Energy < self.MaxEnergy * 0.4f && ecosystem.Random.NextDouble() < 0.4;
        if (seekPrey)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
            {
                if (self.DistanceTo(prey) >= 10f)
                {
                    self.MoveToward(prey.Position, dt, world);
                    return true;
                }

                float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
                prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
                self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
                if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
                return true;
            }
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null)
            return false;

        if (self.DistanceTo(food) >= 12f)
        {
            self.MoveToward(food.Position, dt, world);
            return true;
        }

        float eaten = Math.Min(food.Energy, 8f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

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

        float eaten = Math.Min(food.Energy, 10f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }

    private static bool TryFeedNearbyCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is not Carnivore carn)
            return TryEatCarcass(self, ecosystem, dt);

        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey == null || self.DistanceTo(prey) >= 10f)
            return TryEatCarcass(self, ecosystem, dt);

        float damage = carn.AttackDamage * dt;
        prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
        self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
        if (prey.Energy <= 0) prey.Die(DeathCause.Predation);

        return true;
    }

    private static bool TryFeedNearbyOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Creature? prey = ecosystem.FindNearestPrey(self);
        if (prey != null && self.DistanceTo(prey) < 10f)
        {
            float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
            prey.Energy -= damage * Math.Max(0.2f, 1f - prey.Defense / 25f);
            self.Energy = Math.Min(self.Energy + damage * 1.5f * (1f - prey.Toxicity * 0.5f), self.MaxEnergy);
            if (prey.Energy <= 0) prey.Die(DeathCause.Predation);
            return true;
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food == null || self.DistanceTo(food) >= 12f)
            return TryEatCarcass(self, ecosystem, dt);

        float eaten = Math.Min(food.Energy, 8f * dt);
        food.Energy -= eaten;
        self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
        if (food.Energy <= 0) food.Die(DeathCause.Predation);

        return true;
    }

    internal static bool TryGraze(Creature self, World world, float dt)
    {
        var tile = world.GetTileAtPosition(self.Position.X, self.Position.Y);
        if (tile.GrassAmount <= 0.001f) return false;
        float grazeRate = 3f * dt;
        float eaten = tile.EatGrass(grazeRate);
        if (eaten > 0)
        {
            self.Energy = Math.Min(self.Energy + eaten * 8f, self.MaxEnergy);
            return true;
        }
        return false;
    }

    private static bool TryEatCarcass(Creature self, Ecosystem ecosystem, float dt)
    {
        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || c.IsAlive || c.CreatureType == CreatureType.Plant) continue;
            if (self.DistanceTo(c) < 10f && c.Energy > 0)
            {
                float eaten = Math.Min(c.Energy, 8f * dt);
                c.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 1.2f, self.MaxEnergy);
                return true;
            }
        }
        return false;
    }
}
