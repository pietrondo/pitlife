using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class BaseBehavior : ICreatureBehavior
{
    public void Update(Creature self, World world, Ecosystem ecosystem, GameTime gameTime)
    {
        if (!self.IsAlive) return;
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        if (dt <= 0) return;

        // 1. Threat avoidance first
        Creature? threat = ecosystem.FindNearestPredator(self);
        if (threat != null && self.DistanceTo(threat) < self.VisionPixels * PredatorVisionScale(self))
        {
            self.MoveAwayFrom(threat.Position, dt, world);
            return;
        }

        // 2. Food search if hungry
        if (IsHungry(self) && TryFeed(self, ecosystem, dt, world))
        {
            return;
        }

        // 3. Social/group steering if not hungry
        if (ApplySocialBehavior(self, ecosystem, dt, world))
        {
            return;
        }

        // 4. Try to feed on nearby food even if not hungry
        if (TryFeedNearby(self, ecosystem, dt, world))
        {
            return;
        }

        // 5. Normal wandering as fallback
        float wanderSpeed = self.CreatureType switch
        {
            CreatureType.Carnivore => 100f,
            CreatureType.Omnivore => 90f,
            _ => 80f
        };
        self.Wander(world, dt, ecosystem.Random, wanderSpeed);
    }

    private static float PredatorVisionScale(Creature self) => self.CreatureType switch
    {
        CreatureType.Herbivore => 0.8f,
        CreatureType.Omnivore => 0.6f,
        _ => 0f
    };

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
        if (self.CreatureType == CreatureType.Herbivore)
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
                return true;
            }
        }
        else if (self.CreatureType == CreatureType.Carnivore)
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
                    return true;
                }
            }
        }
        else if (self.CreatureType == CreatureType.Omnivore)
        {
            bool seekPrey = self.Energy < self.MaxEnergy * 0.4f && ecosystem.Random.NextDouble() < 0.4;
            if (seekPrey)
            {
                Creature? prey = ecosystem.FindNearestPrey(self);
                if (prey != null && self.DistanceTo(prey) < self.VisionPixels)
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
                    return true;
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
                return true;
            }
        }
        return false;
    }

    private static bool TryFeedNearby(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        if (self.CreatureType == CreatureType.Herbivore)
        {
            Plant? food = self is Herbivore h
                ? ecosystem.FindNearestPlant(h)
                : ecosystem.FindNearestPlantFor(self);
            if (food != null && self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 10f * dt);
                food.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 2f, self.MaxEnergy);
                if (food.Energy <= 0) food.Die();
                return true;
            }
        }
        else if (self.CreatureType == CreatureType.Carnivore)
        {
            if (self is Carnivore carn)
            {
                Creature? prey = ecosystem.FindNearestPrey(self);
                if (prey != null && self.DistanceTo(prey) < 10f)
                {
                    float damage = carn.AttackDamage * dt;
                    prey.Energy -= damage;
                    self.Energy = Math.Min(self.Energy + damage * 1.5f, self.MaxEnergy);
                    if (prey.Energy <= 0) prey.Die();
                    return true;
                }
            }
        }
        else if (self.CreatureType == CreatureType.Omnivore)
        {
            Creature? prey = ecosystem.FindNearestPrey(self);
            if (prey != null && self.DistanceTo(prey) < 10f)
            {
                float damage = (self is Omnivore om ? om.AttackDamage : 12f) * dt;
                prey.Energy -= damage;
                self.Energy = Math.Min(self.Energy + damage * 1.5f, self.MaxEnergy);
                if (prey.Energy <= 0) prey.Die();
                return true;
            }

            Plant? food = ecosystem.FindNearestPlantFor(self);
            if (food != null && self.DistanceTo(food) < 12f)
            {
                float eaten = Math.Min(food.Energy, 8f * dt);
                food.Energy -= eaten;
                self.Energy = Math.Min(self.Energy + eaten * 1.5f, self.MaxEnergy);
                if (food.Energy <= 0) food.Die();
                return true;
            }
        }
        return false;
    }

    private static bool ApplySocialBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var behavior = SpeciesRegistry.Get(self.Species)?.SocialBehavior ?? SocialBehavior.None;
        if (behavior == SocialBehavior.None)
            return false;

        switch (behavior)
        {
            case SocialBehavior.Herd:
                return ApplyFlocking(self, ecosystem, dt, world, cohesionWeight: 1.5f, separationWeight: 2.0f, alignmentWeight: 0.5f, separationDist: 30f);
            
            case SocialBehavior.Pack:
                {
                    bool flockMoved = ApplyFlocking(self, ecosystem, dt, world, cohesionWeight: 1.2f, separationWeight: 1.0f, alignmentWeight: 1.5f, separationDist: 25f);
                    // Packs get an energy boost when close to packmates
                    var neighbors = ecosystem.FindNeighbors(self, self.VisionPixels * 0.3f, n => n.Species == self.Species);
                    if (neighbors.Count > 0)
                    {
                        self.Energy = Math.Min(self.MaxEnergy, self.Energy + 5f * dt);
                    }
                    return flockMoved;
                }
            
            case SocialBehavior.School:
                return ApplyFlocking(self, ecosystem, dt, world, cohesionWeight: 1.0f, separationWeight: 0.8f, alignmentWeight: 2.0f, separationDist: 15f);
            
            case SocialBehavior.Swarm:
                return ApplyFlocking(self, ecosystem, dt, world, cohesionWeight: 2.0f, separationWeight: 2.5f, alignmentWeight: 0.0f, separationDist: 30f);
            
            case SocialBehavior.Pair:
                return ApplyPairBehavior(self, ecosystem, dt, world);
            
            case SocialBehavior.Solitary:
                return ApplySolitaryBehavior(self, ecosystem, dt, world);
        }

        return false;
    }

    private static bool ApplyFlocking(
        Creature self,
        Ecosystem ecosystem,
        float dt,
        World world,
        float cohesionWeight,
        float separationWeight,
        float alignmentWeight,
        float separationDist)
    {
        var neighbors = ecosystem.FindNeighbors(self, self.VisionPixels, n => n.Species == self.Species);
        if (neighbors.Count == 0)
            return false;

        Vector2 cohesionForce = Vector2.Zero;
        Vector2 separationForce = Vector2.Zero;
        Vector2 alignmentForce = Vector2.Zero;

        Vector2 avgPosition = Vector2.Zero;
        Vector2 avgFacing = Vector2.Zero;
        int separationCount = 0;

        foreach (var neighbor in neighbors)
        {
            avgPosition += neighbor.Position;
            avgFacing += neighbor.Facing;

            float dist = Vector2.Distance(self.Position, neighbor.Position);
            if (dist > 0 && dist < separationDist)
            {
                Vector2 diff = self.Position - neighbor.Position;
                diff /= dist; // Normalize
                diff /= dist; // Weight by distance
                separationForce += diff;
                separationCount++;
            }
        }

        // 1. Cohesion
        avgPosition /= neighbors.Count;
        cohesionForce = avgPosition - self.Position;
        if (cohesionForce.LengthSquared() > 0.001f)
            cohesionForce = Vector2.Normalize(cohesionForce);

        // 2. Separation
        if (separationCount > 0)
        {
            separationForce /= separationCount;
            if (separationForce.LengthSquared() > 0.001f)
                separationForce = Vector2.Normalize(separationForce);
        }

        // 3. Alignment
        avgFacing /= neighbors.Count;
        if (avgFacing.LengthSquared() > 0.001f)
            alignmentForce = Vector2.Normalize(avgFacing);

        // Combine steering vectors
        Vector2 steerDir = cohesionForce * cohesionWeight +
                           separationForce * separationWeight +
                           alignmentForce * alignmentWeight;

        if (steerDir.LengthSquared() > 0.001f)
        {
            steerDir = Vector2.Normalize(steerDir);
            Vector2 target = self.Position + steerDir * 100f;
            self.MoveToward(target, dt, world);
            return true;
        }

        return false;
    }

    private static bool ApplyPairBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var partner = ecosystem.FindNearestSameSpecies(self);
        if (partner != null && self.DistanceTo(partner) < self.VisionPixels)
        {
            float dist = self.DistanceTo(partner);
            if (dist > 30f)
            {
                self.MoveToward(partner.Position, dt * 0.6f, world);
            }
            else if (dist < 15f)
            {
                self.MoveAwayFrom(partner.Position, dt * 0.4f, world);
            }
            else
            {
                self.MoveToward(partner.Position, dt * 0.2f, world);
            }

            // Energy boost when close
            if (dist < 50f)
            {
                self.Energy = Math.Min(self.MaxEnergy, self.Energy + 2f * dt);
            }
            return true;
        }
        return false;
    }

    private static bool ApplySolitaryBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var neighbor = ecosystem.FindNearestSameSpecies(self);
        if (neighbor != null && self.DistanceTo(neighbor) < self.VisionPixels * 0.5f)
        {
            float dist = self.DistanceTo(neighbor);
            self.MoveAwayFrom(neighbor.Position, dt, world);
            self.Energy -= 3f * dt;

            // Combat for carnivores/omnivores when extremely close
            if ((self.CreatureType == CreatureType.Carnivore || self.CreatureType == CreatureType.Omnivore) && dist < 20f)
            {
                self.Energy -= 10f * dt;
                neighbor.Energy -= 10f * dt;

                if (ecosystem.Random.NextDouble() < 0.1 * dt)
                {
                    Logger.Event("COMBAT", $"{self.Species} fought same-species rival at ({self.Position.X:F0},{self.Position.Y:F0})");
                }
            }
            return true;
        }
        return false;
    }
}
