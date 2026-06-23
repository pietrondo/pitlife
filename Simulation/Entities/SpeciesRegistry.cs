using System;
using System.Collections.Generic;
using System.Linq;

namespace PitLife.Simulation;

public enum SocialBehavior
{
    None,
    Solitary,
    Pair,
    Herd,
    Pack,
    School,
    Swarm
}

public enum PlantReproductionMode
{
    Seeds,
    Spores,
    Vegetative,
    Fragmentation,
    BroadcastSpawning
}

public enum PollinationMode
{
    None,
    Wind,
    Insects,
    Self,
    Water
}

public sealed class SpeciesDefinition
{
    public string Species { get; }
    public Type CreatureType { get; }
    public CreatureType Kind { get; }
    public bool IsAquatic { get; }
    public SocialBehavior SocialBehavior { get; }
    public HashSet<BiomeType> ValidBiomes { get; }
    public float DefaultSize { get; }
    public float MaturityAge { get; }
    public PlantReproductionMode? PlantReproduction { get; }
    public PollinationMode Pollination { get; }

    public SpeciesDefinition(
        string species,
        Type creatureType,
        CreatureType kind,
        bool isAquatic,
        SocialBehavior socialBehavior,
        IEnumerable<BiomeType> validBiomes,
        float defaultSize = 1.0f,
        float maturityAge = 30f,
        PlantReproductionMode? plantReproduction = null,
        PollinationMode pollination = PollinationMode.None)
    {
        if (kind == global::PitLife.Simulation.CreatureType.Plant && plantReproduction is null)
            throw new ArgumentException("Plant species require a reproduction mode.", nameof(plantReproduction));
        if (kind != global::PitLife.Simulation.CreatureType.Plant &&
            (plantReproduction is not null || pollination != PollinationMode.None))
            throw new ArgumentException("Animal species cannot use plant reproduction settings.", nameof(plantReproduction));
        if (plantReproduction is not PlantReproductionMode.Seeds && pollination != PollinationMode.None)
            throw new ArgumentException("Only seed-producing species can define pollination.", nameof(pollination));

        Species = species;
        CreatureType = creatureType;
        Kind = kind;
        IsAquatic = isAquatic;
        SocialBehavior = socialBehavior;
        ValidBiomes = new HashSet<BiomeType>(validBiomes);
        DefaultSize = defaultSize;
        MaturityAge = maturityAge;
        PlantReproduction = plantReproduction;
        Pollination = pollination;
    }

    public bool IsValidBiome(BiomeType biome) => ValidBiomes.Contains(biome);
}

public static class SpeciesRegistry
{
    private static readonly object Sync = new();
    private static readonly Dictionary<string, SpeciesDefinition> _bySpecies = new(StringComparer.Ordinal);
    private static readonly Dictionary<CreatureType, List<string>> _byKind = new();

    static SpeciesRegistry()
    {
        BuiltinSpecies.RegisterAll();
    }

    public static void Register(SpeciesDefinition def)
    {
        lock (Sync)
        {
            _bySpecies[def.Species] = def;
            if (!_byKind.TryGetValue(def.Kind, out var list))
            {
                list = new List<string>();
                _byKind[def.Kind] = list;
            }
            if (!list.Contains(def.Species))
                list.Add(def.Species);
        }
    }

    public static SpeciesDefinition? Get(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) ? def : null;
    }

    public static bool Contains(string species)
    {
        lock (Sync)
            return _bySpecies.ContainsKey(species);
    }

    public static IEnumerable<string> All
    {
        get
        {
            lock (Sync)
                return _bySpecies.Keys.ToArray();
        }
    }

    public static IEnumerable<string> OfType(CreatureType kind)
    {
        lock (Sync)
            return _byKind.TryGetValue(kind, out var list) ? list.ToArray() : Array.Empty<string>();
    }

    public static bool IsPackAnimal(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) &&
                (def.SocialBehavior == SocialBehavior.Pack ||
                 def.SocialBehavior == SocialBehavior.Herd ||
                 def.SocialBehavior == SocialBehavior.School ||
                 def.SocialBehavior == SocialBehavior.Swarm);
    }

    public static bool IsSolitary(string species)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) && def.SocialBehavior == SocialBehavior.Solitary;
    }

    public static bool IsValidBiome(string species, BiomeType biome)
    {
        lock (Sync)
            return _bySpecies.TryGetValue(species, out var def) && def.IsValidBiome(biome);
    }

    internal static void Reset() { /* no-op: builtins registered in static ctor */ }

    internal static bool Unregister(string species)
    {
        lock (Sync)
        {
            if (!_bySpecies.Remove(species, out SpeciesDefinition? definition))
                return false;

            if (_byKind.TryGetValue(definition.Kind, out List<string>? speciesOfKind))
                speciesOfKind.Remove(species);
            return true;
        }
    }

    public static string DetermineEvolvedSpecies(CreatureType kind, Genome genome, string currentSpecies, Random rng)
        => EvolutionRules.DetermineEvolvedSpecies(kind, genome, currentSpecies, rng);
}
