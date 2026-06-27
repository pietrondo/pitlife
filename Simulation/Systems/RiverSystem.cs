using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

internal sealed class RiverSystem : ISimulationSystem
{
    public UpdatePhase Phase => UpdatePhase.Update;
    private readonly World _world;

    public RiverSystem(World world)
    {
        _world = world;
    }

    public void CarveRivers(int seed)
    {
        Core.Logger.Debug($"Carving rivers with seed {seed}");
        var rng = new Random(seed);
        int riverThreshold = 3 + (int)(rng.NextDouble() * 8f);
        int W = _world.Width, H = _world.Height;
        int[] dx = { 1, 1, 0, -1, -1, -1, 0, 1 };
        int[] dy = { 0, 1, 1, 1, 0, -1, -1, -1 };
        float[] dd = { 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f, 1f, 1.4142135f };

        int[] flowDir = System.Buffers.ArrayPool<int>.Shared.Rent(W * H);
        try
        {
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    int idx = y * W + x;
                    float e = _world.ElevationField[idx];
                    int bestDir = -1;
                    float bestDrop = 0f;
                    int secondBestDir = -1;
                    float secondBestDrop = 0f;
                    for (int d = 0; d < 8; d++)
                    {
                        int nx = x + dx[d];
                        int ny = y + dy[d];
                        if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                        float ne = _world.ElevationField[ny * W + nx];
                        float drop = (e - ne) / dd[d];
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

            int[] cells = System.Buffers.ArrayPool<int>.Shared.Rent(W * H);
            try
            {
                for (int i = 0; i < W * H; i++) cells[i] = i;
                Array.Sort(cells, 0, W * H, Comparer<int>.Create((a, b) => _world.ElevationField[b].CompareTo(_world.ElevationField[a])));

                int[] flowAccum = System.Buffers.ArrayPool<int>.Shared.Rent(W * H);
                try
                {
                    Array.Clear(flowAccum, 0, W * H);
                    for (int i = 0; i < W * H; i++)
                    {
                        int idx = cells[i];
                        flowAccum[idx] = Math.Max(1, flowAccum[idx]);
                        int d = flowDir[idx];
                        if (d < 0) continue;
                        int x = idx % W;
                        int y = idx / W;
                        int nx = x + dx[d];
                        int ny = y + dy[d];
                        if (nx < 0 || nx >= W || ny < 0 || ny >= H) continue;
                        flowAccum[ny * W + nx] += flowAccum[idx];
                    }

                    for (int i = 0; i < W * H; i++)
                        if (flowAccum[i] > riverThreshold)
                        {
                            _world.RiverMask[i] = true;
                            if (_world.ElevationField[i] > 0.18f) _world.ElevationField[i] = 0.18f;
                        }
                }
                finally { System.Buffers.ArrayPool<int>.Shared.Return(flowAccum); }
            }
            finally { System.Buffers.ArrayPool<int>.Shared.Return(cells); }
        }
        finally { System.Buffers.ArrayPool<int>.Shared.Return(flowDir); }

        for (int i = 0; i < W * H; i++)
            if (_world.RiverMask[i])
            {
                int rx = i % W;
                int ry = i / W;
                _world.Tiles[rx, ry] = new Tile(BiomeType.ShallowWater);
            }

        PruneDisconnectedRivers();
    }

    private void PruneDisconnectedRivers()
    {
        int W = _world.Width, H = _world.Height;
        var visited = System.Buffers.ArrayPool<bool>.Shared.Rent(W * H);
        try
        {
            Array.Clear(visited, 0, W * H);
            var queue = new Queue<int>();
            for (int i = 0; i < W * H; i++)
            {
                if (!_world.RiverMask[i]) continue;
                int rx = i % W;
                int ry = i / W;
                bool oceanAdj = false;
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
                int idx = queue.Dequeue();
                for (int k = 0; k < 4; k++)
                {
                    int n = idx + offsets[k];
                    if (n < 0 || n >= W * H) continue;
                    int nx = n % W;
                    int ny = n / W;
                    if (k < 2 && (idx % W == 0 || idx % W == W - 1)) continue;
                    if (visited[n]) continue;
                    if (_world.RiverMask[n] || _world.ElevationField[n] <= 0.5f)
                    {
                        visited[n] = true;
                        queue.Enqueue(n);
                    }
                }
            }

            for (int i = 0; i < W * H; i++)
                if (_world.RiverMask[i] && !visited[i])
                    _world.RiverMask[i] = false;
        }
        finally
        {
            System.Buffers.ArrayPool<bool>.Shared.Return(visited);
        }
    }

    public void Initialize(World world) { }
    public void Tick(Ecosystem eco, GameTime gameTime) { }
    public void Reset() { }
}
