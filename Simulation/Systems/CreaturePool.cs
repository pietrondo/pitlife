using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class CreaturePool
{
    private readonly Dictionary<string, Stack<Creature>> _pools = new(StringComparer.Ordinal);

    public Creature? Rent(string species, Vector2 position, Genome genome)
    {
        var def = SpeciesRegistry.Get(species);
        if (def == null) return null;

        string key = $"{def.CreatureType.Name}:{species}";
        if (_pools.TryGetValue(key, out var stack) && stack.Count > 0)
        {
            var c = stack.Pop();
            c.ResetForReuse(position, genome);
            return c;
        }

        return (Creature)Activator.CreateInstance(def.CreatureType, position, genome, def.Species)!;
    }

    public void Return(Creature creature)
    {
        if (creature == null) return;
        var def = SpeciesRegistry.Get(creature.Species);
        if (def == null) return;

        string key = $"{def.CreatureType.Name}:{creature.Species}";
        if (!_pools.TryGetValue(key, out var stack))
        {
            stack = new Stack<Creature>();
            _pools[key] = stack;
        }
        stack.Push(creature);
    }
}
