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
        var tile = _ecosystem.World.GetTileAtPosition(position.X, position.Y);
        int tileY = (int)(position.Y / _ecosystem.World.TileSize);
        float effectiveTemp = _ecosystem.Climate.GetTileTemperature(tile, tileY, _ecosystem.World.Height);
        return def.IsValidClimate(tile.Biome, effectiveTemp);
    }

    private bool SpawnInternal(SpeciesDefinition def, Vector2 position)
    {
        if (_ecosystem.IsFull) return false;
        var tile = _ecosystem.World.GetTileAtPosition(position.X, position.Y);
        int tileY = (int)(position.Y / _ecosystem.World.TileSize);
        float effectiveTemp = _ecosystem.Climate.GetTileTemperature(tile, tileY, _ecosystem.World.Height);
        if (!def.IsValidClimate(tile.Biome, effectiveTemp))
            return false;

        var genome = Genome.Random(_ecosystem.Random);
        genome.Size = def.DefaultSize;

        Creature c = (Creature)Activator.CreateInstance(def.CreatureType, position, genome, def.Species)!;
        if (c.CreatureType != CreatureType.Plant)
            c.Gender = _ecosystem.Random.Next(2) == 0 ? Gender.Male : Gender.Female;
        if (c.Species is "Pufferfish" or "PoisonFrog" or "Belladonna" or "VenusFlyTrap" or "PitcherPlant")
        {
            c.Toxicity = 0.8f;
            c.IsPoisonous = true;
        }
        _ecosystem.AddCreature(c);
        return true;
    }
}
