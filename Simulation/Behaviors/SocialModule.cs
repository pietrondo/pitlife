using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

internal sealed class SocialModule : IBehaviorModule
{
    [ThreadStatic] private static Creature? ts_self;

    public bool Update(Creature self, World world, Ecosystem ecosystem, float dt)
    {
        return ApplySocialBehavior(self, ecosystem, dt, world);
    }

    public void DefendInfants(Creature self, Ecosystem ecosystem, float dt)
    {
        ts_self = self;
        var buffer = System.Buffers.ArrayPool<Creature>.Shared.Rent(16);
        try
        {
            int count = ecosystem.FindNeighbors(self, 40f, ref buffer, IsInfant);
            if (count == 0) return;

            var infant = buffer[0];
            var predator = ecosystem.FindNearestPredator(infant);
            if (predator == null || infant.DistanceTo(predator) >= 40f) return;

            self.MoveToward(predator.Position, dt, null);
            if (self.DistanceTo(predator) < 10f)
            {
                var damage = 15f * dt;
                predator.Energy -= damage;
                if (predator.Energy <= 0) predator.Die(DeathCause.Combat);
            }
        }
        finally
        {
            ts_self = null;
            System.Buffers.ArrayPool<Creature>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static bool ApplySocialBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var behavior = SpeciesRegistry.Get(self.Species)?.SocialBehavior ?? SocialBehavior.None;
        if (behavior == SocialBehavior.None)
            return false;

        var s = self.Genome.Sociability;
        switch (behavior)
        {
            case SocialBehavior.Herd:
                return ApplyFlocking(self, ecosystem, dt, world,
                    cohesionWeight: SocialConfig.Data.Flocking.CohesionWeight * s,
                    separationWeight: SocialConfig.Data.Flocking.SeparationWeight * s,
                    alignmentWeight: SocialConfig.Data.Flocking.AlignmentWeight * s,
                    separationDist: SocialConfig.Data.Flocking.SeparationDistance,
                    radius: self.VisionPixels * SocialConfig.Data.Flocking.HerdRadius);

            case SocialBehavior.Pack:
                {
                    var flockMoved = ApplyFlocking(self, ecosystem, dt, world, cohesionWeight: 1.2f, separationWeight: 1.0f, alignmentWeight: 1.5f, separationDist: 25f);
                    ts_self = self;
                    var packBuffer = System.Buffers.ArrayPool<Creature>.Shared.Rent(16);
                    try
                    {
                        int packCount = ecosystem.FindNeighbors(self, self.VisionPixels * 0.3f, ref packBuffer, IsSameSpecies);
                        if (packCount > 0)
                        {
                            self.Energy = Math.Min(self.MaxEnergy, self.Energy + 5f * dt);
                        }
                    }
                    finally
                    {
                        ts_self = null;
                        System.Buffers.ArrayPool<Creature>.Shared.Return(packBuffer, clearArray: true);
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
        float separationDist,
        float radius = -1f)
    {
        float actualRadius = radius < 0f ? self.VisionPixels : radius;
        ts_self = self;
        var buffer = System.Buffers.ArrayPool<Creature>.Shared.Rent(64);
        try
        {
            int count = ecosystem.FindNeighbors(self, actualRadius, ref buffer, IsSameSpecies);
            if (count == 0)
                return false;

            var (cohesionForce, separationForce, alignmentForce) = CalculateFlockingForces(self, buffer, count, separationDist);

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
        finally
        {
            ts_self = null;
            System.Buffers.ArrayPool<Creature>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static bool ApplyPairBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var partner = ecosystem.FindNearestSameSpecies(self);
        if (partner == null || self.DistanceTo(partner) >= self.VisionPixels)
            return false;

        var dist = self.DistanceTo(partner);
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

        if (dist < 50f)
        {
            self.Energy = Math.Min(self.MaxEnergy, self.Energy + 2f * dt);
        }
        return true;
    }

    private static bool ApplySolitaryBehavior(Creature self, Ecosystem ecosystem, float dt, World world)
    {
        var neighbor = ecosystem.FindNearestSameSpecies(self);
        if (neighbor == null || self.DistanceTo(neighbor) >= self.VisionPixels * 0.5f)
            return false;

        var dist = self.DistanceTo(neighbor);
        self.MoveAwayFrom(neighbor.Position, dt, world);
        self.Energy -= 3f * dt;

        if (self.Energy < self.MaxEnergy * 0.3f)
        {
            self.MoveAwayFrom(neighbor.Position, dt * 2.0f, world);
            if (ecosystem.Random.NextDouble() < 0.1 * dt)
                Core.Logger.Event("FLEE", $"{self.Species} fled from rival at ({self.Position.X:F0},{self.Position.Y:F0})");
            return true;
        }

        if ((self.CreatureType == CreatureType.Carnivore || self.CreatureType == CreatureType.Omnivore) && dist < SocialConfig.Data.Combat.AggressionFactor)
        {
            self.Energy -= SocialConfig.Data.Combat.CombatDamage * dt * Math.Max(0.2f, 1f - neighbor.Defense / SocialConfig.Data.Combat.DefenseDivisor) * (0.5f + self.Genome.Aggression);
            neighbor.Energy -= SocialConfig.Data.Combat.CombatDamage * dt * Math.Max(0.2f, 1f - self.Defense / SocialConfig.Data.Combat.DefenseDivisor) * (0.5f + neighbor.Genome.Aggression);

            if (self.Energy <= 0) self.Die(DeathCause.Combat);
            if (neighbor.Energy <= 0) neighbor.Die(DeathCause.Combat);

            if (ecosystem.Random.NextDouble() < 0.1 * dt)
            {
                Logger.Event("COMBAT", $"{self.Species} fought same-species rival at ({self.Position.X:F0},{self.Position.Y:F0})");
            }
        }
        return true;
    }

    private static bool IsInfant(Creature c) => c.IsBaby && ts_self != null && c.Parent == ts_self && c.IsAlive;
    private static bool IsSameSpecies(Creature c) => ts_self != null && c.Species == ts_self.Species;

    private static (Vector2 cohesion, Vector2 separation, Vector2 alignment) CalculateFlockingForces(
        Creature self, Creature[] neighbors, int count, float separationDist)
    {
        Vector2 cohesionForce = Vector2.Zero;
        Vector2 separationForce = Vector2.Zero;
        Vector2 alignmentForce = Vector2.Zero;

        Vector2 avgPosition = Vector2.Zero;
        Vector2 avgFacing = Vector2.Zero;
        var separationCount = 0;

        for (int i = 0; i < count; i++)
        {
            var neighbor = neighbors[i];
            avgPosition += neighbor.Position;
            avgFacing += neighbor.Facing;

            var dist = Vector2.Distance(self.Position, neighbor.Position);
            if (dist > 0 && dist < separationDist)
            {
                Vector2 diff = self.Position - neighbor.Position;
                diff /= dist;
                diff /= dist;
                separationForce += diff;
                separationCount++;
            }
        }

        avgPosition /= count;
        cohesionForce = avgPosition - self.Position;
        if (cohesionForce.LengthSquared() > 0.001f)
            cohesionForce = Vector2.Normalize(cohesionForce);

        if (separationCount > 0)
        {
            separationForce /= separationCount;
            if (separationForce.LengthSquared() > 0.001f)
                separationForce = Vector2.Normalize(separationForce);
        }

        avgFacing /= count;
        if (avgFacing.LengthSquared() > 0.001f)
            alignmentForce = Vector2.Normalize(avgFacing);

        return (cohesionForce, separationForce, alignmentForce);
    }
}
