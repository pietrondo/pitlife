using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

internal sealed class SpatialGrid
{
    private readonly int _cellSize;
    private readonly int _columns;
    private readonly int _rows;
    private readonly Dictionary<(int X, int Y), HashSet<Creature>> _buckets = new();
    private readonly Dictionary<Creature, (int X, int Y)> _locations = new();

    public SpatialGrid(int width, int height, int cellSize)
    {
        _cellSize = Math.Max(1, cellSize);
        _columns = Math.Max(1, (width + _cellSize - 1) / _cellSize);
        _rows = Math.Max(1, (height + _cellSize - 1) / _cellSize);
    }

    public void Rebuild(IEnumerable<Creature> creatures)
    {
        _buckets.Clear();
        _locations.Clear();

        foreach (var creature in creatures)
            Update(creature);
    }

    public void Update(Creature creature)
    {
        if (!creature.IsAlive)
        {
            Remove(creature);
            return;
        }

        var cell = GetCell(creature.Position);
        if (_locations.TryGetValue(creature, out var oldCell))
        {
            if (oldCell == cell)
                return;

            RemoveFromBucket(creature, oldCell);
        }

        if (!_buckets.TryGetValue(cell, out var bucket))
        {
            bucket = new HashSet<Creature>();
            _buckets[cell] = bucket;
        }

        bucket.Add(creature);
        _locations[creature] = cell;
    }

    public void Remove(Creature creature)
    {
        if (!_locations.Remove(creature, out var cell))
            return;

        RemoveFromBucket(creature, cell);
    }

    public List<Creature> GetNeighbors(Creature seeker, float radius, Func<Creature, bool> predicate)
    {
        var results = new List<Creature>();
        var radiusSquared = radius * radius;
        var position = seeker.Position;

        var minCellX = Math.Clamp((int)((position.X - radius) / _cellSize), 0, _columns - 1);
        var maxCellX = Math.Clamp((int)((position.X + radius) / _cellSize), 0, _columns - 1);
        var minCellY = Math.Clamp((int)((position.Y - radius) / _cellSize), 0, _rows - 1);
        var maxCellY = Math.Clamp((int)((position.Y + radius) / _cellSize), 0, _rows - 1);

        for (var y = minCellY; y <= maxCellY; y++)
        {
            for (var x = minCellX; x <= maxCellX; x++)
            {
                if (!_buckets.TryGetValue((x, y), out var bucket))
                    continue;

                foreach (var candidate in bucket)
                {
                    if (candidate == seeker || !candidate.IsAlive || !predicate(candidate))
                        continue;

                    if (Vector2.DistanceSquared(position, candidate.Position) <= radiusSquared)
                    {
                        results.Add(candidate);
                    }
                }
            }
        }

        return results;
    }

    public Creature? FindNearest(Creature seeker, Func<Creature, bool> predicate)
    {
        var center = GetCell(seeker.Position);
        var maxRing = Math.Max(
            Math.Max(center.X, _columns - 1 - center.X),
            Math.Max(center.Y, _rows - 1 - center.Y));

        Creature? best = null;
        var bestDistanceSquared = float.MaxValue;

        for (var ring = 0; ring <= maxRing; ring++)
        {
            VisitRing(center, ring, seeker, predicate, ref best, ref bestDistanceSquared);

            if (best != null && bestDistanceSquared <= DistanceToUnvisitedAreaSquared(seeker.Position, center, ring))
                break;
        }

        return best;
    }

    private void VisitRing(
        (int X, int Y) center,
        int ring,
        Creature seeker,
        Func<Creature, bool> predicate,
        ref Creature? best,
        ref float bestDistanceSquared)
    {
        if (ring == 0)
        {
            VisitCell(center.X, center.Y, seeker, predicate, ref best, ref bestDistanceSquared);
            return;
        }

        var minX = center.X - ring;
        var maxX = center.X + ring;
        var minY = center.Y - ring;
        var maxY = center.Y + ring;

        for (var x = minX; x <= maxX; x++)
        {
            VisitCell(x, minY, seeker, predicate, ref best, ref bestDistanceSquared);
            VisitCell(x, maxY, seeker, predicate, ref best, ref bestDistanceSquared);
        }

        for (var y = minY + 1; y < maxY; y++)
        {
            VisitCell(minX, y, seeker, predicate, ref best, ref bestDistanceSquared);
            VisitCell(maxX, y, seeker, predicate, ref best, ref bestDistanceSquared);
        }
    }

    private void VisitCell(
        int x,
        int y,
        Creature seeker,
        Func<Creature, bool> predicate,
        ref Creature? best,
        ref float bestDistanceSquared)
    {
        if (x < 0 || x >= _columns || y < 0 || y >= _rows || !_buckets.TryGetValue((x, y), out var bucket))
            return;

        foreach (var candidate in bucket)
        {
            if (candidate == seeker || !candidate.IsAlive || !predicate(candidate))
                continue;

            var distanceSquared = Vector2.DistanceSquared(seeker.Position, candidate.Position);
            if (distanceSquared < bestDistanceSquared)
            {
                bestDistanceSquared = distanceSquared;
                best = candidate;
            }
        }
    }

    private float DistanceToUnvisitedAreaSquared(Vector2 position, (int X, int Y) center, int ring)
    {
        var minDistance = float.MaxValue;
        var minX = Math.Max(0, center.X - ring) * _cellSize;
        var maxX = Math.Min(_columns, center.X + ring + 1) * _cellSize;
        var minY = Math.Max(0, center.Y - ring) * _cellSize;
        var maxY = Math.Min(_rows, center.Y + ring + 1) * _cellSize;

        if (minX > 0) minDistance = Math.Min(minDistance, position.X - minX);
        if (maxX < _columns * _cellSize) minDistance = Math.Min(minDistance, maxX - position.X);
        if (minY > 0) minDistance = Math.Min(minDistance, position.Y - minY);
        if (maxY < _rows * _cellSize) minDistance = Math.Min(minDistance, maxY - position.Y);

        return minDistance * minDistance;
    }

    private (int X, int Y) GetCell(Vector2 position)
    {
        var x = Math.Clamp((int)(position.X / _cellSize), 0, _columns - 1);
        var y = Math.Clamp((int)(position.Y / _cellSize), 0, _rows - 1);
        return (x, y);
    }

    private void RemoveFromBucket(Creature creature, (int X, int Y) cell)
    {
        if (!_buckets.TryGetValue(cell, out var bucket))
            return;

        bucket.Remove(creature);
        if (bucket.Count == 0)
            _buckets.Remove(cell);
    }
}
