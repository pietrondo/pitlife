using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public struct MemoryStore
{
    public List<Vector2> RememberedFood { get; }
    public List<Vector2> RememberedDanger { get; }

    public MemoryStore()
    {
        RememberedFood = new List<Vector2>();
        RememberedDanger = new List<Vector2>();
    }

    public void Reset()
    {
        RememberedFood?.Clear();
        RememberedDanger?.Clear();
    }

    public void RememberFood(Vector2 pos)
    {
        if (RememberedFood == null) return;
        if (RememberedFood.Count >= BalanceConfig.Data.Creature.MaxMemories) RememberedFood.RemoveAt(0);
        RememberedFood.Add(pos);
    }

    public void RememberDanger(Vector2 pos)
    {
        if (RememberedDanger == null) return;
        if (RememberedDanger.Count >= BalanceConfig.Data.Creature.MaxMemories) RememberedDanger.RemoveAt(0);
        RememberedDanger.Add(pos);
    }

    public void DecayMemories(Random rng, Genome genome)
    {
        if (RememberedFood == null || RememberedDanger == null) return;
        float decayRate = BalanceConfig.Data.Creature.MemoryDecayRateBase * (1f - genome.MemorySpan * BalanceConfig.Data.Creature.MemoryDecayMemorySpanFactor);
        if (rng.NextDouble() < decayRate && RememberedFood.Count > 0)
            RememberedFood.RemoveAt(rng.Next(RememberedFood.Count));
        if (rng.NextDouble() < decayRate && RememberedDanger.Count > 0)
            RememberedDanger.RemoveAt(rng.Next(RememberedDanger.Count));
    }
}
