using System;
using System.Collections.Generic;

namespace PitLife.Simulation;

internal sealed class RiverSystem
{
    private readonly World _world;

    public RiverSystem(World world)
    {
        _world = world;
    }

    public void CarveRivers(int seed)
    {
        Core.Logger.Debug($"Carving rivers with seed {seed}");
        var rng = new Random(seed);
        var riverThreshold = 3 + (int)(rng.NextDouble() * 8f);
        int W = _world.Width, H = _world.Height;
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        float[] dd = { 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f };

        var flowDir = new int[W * H];
        for (var y = 0; y < H; y++)
            for (var x = 0; x < W; x++)
            {
                var idx = y * W + x;
                var e = _world.ElevationField[idx];
                var bestDir = -1;
                var bestDrop = 0f;
                var secondBestDir = -1;
                var secondBestDrop = 0f;
                for (var d = 0; d < 8; d++)
                {
                    var nx = x + dx[d];
                    var ny = y + dy[d];
                    if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                    var ne = _world.ElevationField[ny * W + nx];
                    var drop = (e - ne) / dd[d];
                    if (drop > bestDrop)
                    {
                        secondBestDrop = bestDrop;
                        secondBestDir = bestDir;
                        bestDrop = drop;
                        bestDir = d;
                    }
                    else if (drop > secondBestDrop)
                    {
                        secondBestDrop = drop;
                        secondBestDir = d;
                    }
                }

                if (rng.NextDouble() < 0.15f && secondBestDir != -1 && secondBestDrop > 0f)
                    bestDir = secondBestDir;

                flowDir[idx] = bestDir;
            }

        var cells = new int[W * H];
        for (var i = 0; i < W * H; i++) cells[i] = i;
        Array.Sort(cells, (a, b) => _world.ElevationField[b].CompareTo(_world.ElevationField[a]));

        var flowAccum = new int[W * H];
        for (var i = 0; i < cells.Length; i++)
        {
            var idx = cells[i];
            flowAccum[idx] = Math.Max(1, flowAccum[idx]);
            var d = flowDir[idx];
            if (d < 0) continue;
            var x = idx % W;
            var y = idx / W;
            var nx = x + dx[d];
            var ny = y + dy[d];
            if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
            flowAccum[ny * W + nx] += flowAccum[idx];
        }

        for (var i = 0; i < W * H; i++)
            if (flowAccum[i] > riverThreshold)
            {
                _world.RiverMask[i] = true;
                if (_world.ElevationField[i] > 0.18f) _world.ElevationField[i] = 0.18f;
            }

        for (var i = 0; i < W * H; i++)
            if (_world.RiverMask[i])
            {
                var rx = i % W;
                var ry = i / W;
                _world.Tiles[rx, ry] = new Tile(BiomeType.ShallowWater);
            }

        PruneDisconnectedRivers();
    }

    private void PruneDisconnectedRivers()
    {
        int W = _world.Width, H = _world.Height;
        var visited = new bool[W * H];
        var queue = new Queue<int>();
        for (var i = 0; i < W * H; i++)
        {
            if (!_world.RiverMask[i]) continue;
            var rx = i % W;
            var ry = i / W;
            var oceanAdj = false;
            if (rx > 0 && _world.ContinentMask[i - 1] <= 0.5f) oceanAdj = true;
            else if (rx < W - 1 && _world.ContinentMask[i + 1] <= 0.5f) oceanAdj = true;
            else if (ry > 0 && _world.ContinentMask[i - W] <= 0.5f) oceanAdj = true;
            else if (ry < H - 1 && _world.ContinentMask[i + W] <= 0.5f) oceanAdj = true;
            if (oceanAdj)
            {
                visited[i] = true;
                queue.Enqueue(i);
            }
        }

        int[] offsets = [-1, 1, -W, W];
        while (queue.Count > 0)
        {
            var idx = queue.Dequeue();
            for (var k = 0; k < 4; k++)
            {
                var n = idx + offsets[k];
                if (n < 0 || n >= W * H) continue;
                var nx = n % W;
                var ny = n / W;
                if (k < 2 && (idx % W == 0 || idx % W == W - 1)) continue;
                if (visited[n]) continue;
                if (_world.RiverMask[n] || _world.ElevationField[n] <= 0.5f)
                {
                    visited[n] = true;
                    queue.Enqueue(n);
                }
            }
        }

        for (var i = 0; i < W * H; i++)
            if (_world.RiverMask[i] && !visited[i])
                _world.RiverMask[i] = false;
    }
}
