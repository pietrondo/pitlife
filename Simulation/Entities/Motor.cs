using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public struct Motor
{
    public Vector2 Facing;
    public Vector2? Waypoint;
    public Vector2 HomePosition;
    public float CurrentSpeedMultiplier;
    public float CurrentEnergyMultiplier;

    public void Reset(Vector2 homePosition)
    {
        Facing = new Vector2(0, 1);
        Waypoint = null;
        HomePosition = homePosition;
        CurrentSpeedMultiplier = 1f;
        CurrentEnergyMultiplier = 1f;
    }

    public float GetSpeed(Genome genome, float geneticFitness, bool isBaby)
    {
        return genome.Speed * BalanceConfig.Data.Creature.SpeedBase * CurrentSpeedMultiplier * geneticFitness * (isBaby ? BalanceConfig.Data.Movement.InfantSpeedMultiplier : 1f);
    }

    public float GetVisionPixels(Genome genome, bool isBaby)
    {
        return genome.VisionRange * BalanceConfig.Data.Creature.VisionRangeBase * (isBaby ? BalanceConfig.Data.Movement.InfantSpeedMultiplier : 1f);
    }

    public void MoveToward(ref Vector2 position, Vector2 target, float speed, float dt, bool isAquatic, World? world = null)
    {
        Vector2 dir = target - position;
        float dist = dir.Length();
        if (dist < 1f) return;
        if (dist > 0.001f) dir /= dist;
        float moveAmount = speed * dt;
        Vector2 newPos = ClampToWorld(position + dir * Math.Min(moveAmount, dist), world);
        if (world != null && !world.GetTileAtPosition(newPos.X, newPos.Y).IsPassableFor(isAquatic))
            return;
        position = newPos;
        Facing = dir;
    }

    public bool MoveAwayFrom(ref Vector2 position, Vector2 threat, float speed, float dt, bool isAquatic, World? world = null)
    {
        Vector2 dir = position - threat;
        float dist = dir.Length();
        if (dist < 1f) return false;
        dir /= dist;
        Vector2 newPos = ClampToWorld(position + dir * speed * dt, world);
        if (world != null && !world.GetTileAtPosition(newPos.X, newPos.Y).IsPassableFor(isAquatic))
            return false;
        position = newPos;
        Facing = dir;
        return true;
    }

    public static Vector2 ClampToWorld(Vector2 pos, World? world = null)
    {
        if (world == null) return pos;
        return new Vector2(
            ((pos.X % world.PixelWidth) + world.PixelWidth) % world.PixelWidth,
            ((pos.Y % world.PixelHeight) + world.PixelHeight) % world.PixelHeight);
    }

    public void Wander(ref Vector2 position, Genome genome, float speed, float dt, Random random, float radius, MemoryStore memory, bool isPlant, World world, bool isAquatic)
    {
        if (Waypoint == null || Vector2.Distance(position, Waypoint.Value) < BalanceConfig.Data.Movement.WaypointReachedDistance)
        {
            float distFromHome = Vector2.Distance(position, HomePosition);
            if (distFromHome > radius * 3f)
            {
                Waypoint = HomePosition;
            }
            else if ((memory.RememberedFood != null && memory.RememberedFood.Count > 0) && random.NextDouble() < genome.Intelligence * 0.5f)
            {
                Waypoint = memory.RememberedFood[random.Next(memory.RememberedFood.Count)];
            }
            else if ((memory.RememberedDanger != null && memory.RememberedDanger.Count > 0) && random.NextDouble() < 0.15f)
            {
                var danger = memory.RememberedDanger[random.Next(memory.RememberedDanger.Count)];
                Waypoint = new Vector2(position.X + (position.X - danger.X), position.Y + (position.Y - danger.Y));
            }
            else
            {
                Waypoint = PickWaypoint(position, world, random, radius);
            }
        }

        if (!isPlant)
        {
            var homeTile = world.GetTileAtPosition(HomePosition.X, HomePosition.Y);
            if (homeTile.GrassAmount < 0.05f && random.NextDouble() < 0.0005f)
                HomePosition = position;
        }

        if (Waypoint.HasValue)
        {
            MoveToward(ref position, Waypoint.Value, speed, dt, isAquatic, world);
        }
    }

    private Vector2 PickWaypoint(Vector2 position, World world, Random random, float radius)
    {
        float minDist = radius * 0.35f;
        Vector2 target;
        int attempts = 0;
        do
        {
            float rx = (float)(random.NextDouble() - 0.5) * radius * 2;
            float ry = (float)(random.NextDouble() - 0.5) * radius * 2;
            target = position + new Vector2(rx, ry);
            target.X = Math.Clamp(target.X, 1, world.PixelWidth - 1);
            target.Y = Math.Clamp(target.Y, 1, world.PixelHeight - 1);
            attempts++;
        } while (Vector2.Distance(position, target) < minDist && attempts < 10);
        return target;
    }
}
