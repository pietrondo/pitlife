using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class CreatureSpawner
{
    private readonly Ecosystem _ecosystem;

    public CreatureSpawner(Ecosystem ecosystem)
    {
        _ecosystem = ecosystem;
    }

    public bool SpawnAt<T>(string species, Vector2 position) where T : Creature
    {
        var def = SpeciesRegistry.Get(species);
        if (def == null || def.CreatureType != typeof(T)) return false;
        return SpawnInternal(def, position);
    }

    public bool SpawnByName(string species, Vector2 position)
    {
        var def = SpeciesRegistry.Get(species);
        if (def == null) return false;
        return SpawnInternal(def, position);
    }

    public bool CanSpawn(string species, Vector2 position)
    {
        var def = SpeciesRegistry.Get(species);
        if (def == null) return false;
        if (_ecosystem.IsFull) return false;
        return def.IsValidBiome(_ecosystem.World.GetTileAtPosition(position.X, position.Y).Biome);
    }

    private bool SpawnInternal(SpeciesDefinition def, Vector2 position)
    {
        if (_ecosystem.IsFull) return false;
        if (!def.IsValidBiome(_ecosystem.World.GetTileAtPosition(position.X, position.Y).Biome))
            return false;

        var genome = Genome.Random(_ecosystem.Random);
        if (def.DefaultSize != 1.0f)
            genome.Size = def.DefaultSize;

        Creature c = (Creature)Activator.CreateInstance(def.CreatureType, position, genome, def.Species)!;
        if (c.CreatureType != CreatureType.Plant)
            c.Gender = _ecosystem.Random.Next(2) == 0 ? Gender.Male : Gender.Female;
        _ecosystem.AddCreature(c);
        return true;
    }
}
