using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class BaseBehavior : ICreatureBehavior
{
    public void Update(Creature self, World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!self.IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        Creature? threat = ecosystem.FindNearestPredator(self);
        if (threat != null && self.DistanceTo(threat) < self.VisionPixels * PredatorVisionScale(self))
        {
            self.MoveAwayFrom(threat.Position, dt, world);
            return;
        }

        ApplySocialBehavior(self, ecosystem, dt, world);

        if (self.CreatureType == CreatureType.Herbivore)
            HandleHerbivore(self, ecosystem, dt, world);
        else if (self.CreatureType == CreatureType.Carnivore)
            HandleCarnivore(self, ecosystem, dt, world);
        else
            HandleOmnivore(self, ecosystem, dt, world);
    }

    private static float PredatorVisionScale(Creature self) => self.CreatureType switch
    {
        CreatureType.Herbivore => 0.8f,
        CreatureType.Omnivore => 0.6f,
        _ => 0f
    };

    private static void ApplySocialBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (SpeciesRegistry.IsPackAnimal(self.Species))
        {
            var neighbor = self.FindNearestSameSpecies(ecosystem);
            if (neighbor != null && self.DistanceTo(neighbor) < self.VisionPixels)
            {
                self.MoveToward(neighbor.Position, dt * 0.3f, world);
                if (self.DistanceTo(neighbor) < self.VisionPixels * 0.3f)
                    self.Energy += 5f * dt;
            }
        }
        else if (SpeciesRegistry.IsSolitary(self.Species))
        {
            var neighbor = self.FindNearestSameSpecies(ecosystem);
            if (neighbor != null && self.DistanceTo(neighbor) < self.VisionPixels * 0.5f)
            {
                self.MoveAwayFrom(neighbor.Position, dt, world);
                self.Energy -= 3f * dt;
            }
        }
    }

    private static void HandleHerbivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        Plant? food = self is Herbivore h
            ? ecosystem.FindNearestPlant(h)
            : ecosystem.FindNearestPlantFor(self);
        if (food != null)
        {
            if (self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 10f * dt);
                food.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
                if (food.Energy <= 0) food.Die();
            }
            else
            {
                self.MoveToward(food.Position, dt, world);
            }
        }
        else
        {
            self.Wander(world, dt, ecosystem.Random, 80f);
        }
    }

    private static void HandleCarnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self is Carnivore carn)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
            {
                if (self.DistanceTo(prey) < 10f)
                {
                    float damage = carn.AttackDamage * dt;
                    prey.Energy -= damage;
                    self.Energy = Math.Min(self.Energy + damage * 1.5f, self.MaxEnergy);
                    if (prey.Energy <= 0) prey.Die();
                }
                else
                {
                    self.MoveToward(prey.Position, dt, world);
                }
            }
            else
            {
                self.Wander(world, dt, ecosystem.Random, 100f);
            }
        }
    }

    private static void HandleOmnivore(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        bool isHungry = self.Energy < self.MaxEnergy * 0.4f;
        if (isHungry || self.Energy < self.ReproductionThreshold * 0.8f)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < self.VisionPixels && ecosystem.Random.NextDouble() < 0.4)
            {
                if (self.DistanceTo(prey) < 10f)
                {
                    float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
                    prey.Energy -= damage;
                    self.Energy = Math.Min(self.Energy + damage * 1.5f, self.MaxEnergy);
                    if (prey.Energy <= 0) prey.Die();
                }
                else
                {
                    self.MoveToward(prey.Position, dt, world);
                }
                return;
            }
        }

        Plant? food = ecosystem.FindNearestPlantFor(self);
        if (food != null)
        {
            if (self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 8f * dt);
                food.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
                if (food.Energy <= 0) food.Die();
            }
            else
            {
                self.MoveToward(food.Position, dt, world);
            }
        }
        else
        {
            self.Wander(world, dt, ecosystem.Random, 90f);
        }
    }
}
