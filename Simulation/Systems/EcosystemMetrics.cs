using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class EcosystemMetrics
{
    public float TotalTime { get; private set; }
    public float FPS { get; set; }
    public int TotalCreatures { get; private set; }
    public int Plants => _typeCounts.GetValueOrDefault(CreatureType.Plant);
    public int Herbivores => _typeCounts.GetValueOrDefault(CreatureType.Herbivore);
    public int Carnivores => _typeCounts.GetValueOrDefault(CreatureType.Carnivore);
    public int Omnivores => _typeCounts.GetValueOrDefault(CreatureType.Omnivore);
    public int SpeciesCount { get; private set; }
    public int TotalBirths { get; private set; }
    public int TotalDeaths { get; private set; }
    public int StarvationDeaths { get; private set; }
    public int OldAgeDeaths { get; private set; }
    public int PredationDeaths { get; private set; }
    public int CombatDeaths { get; private set; }
    public float MeanHeterozygosity { get; private set; }
    public float MeanInbreeding { get; private set; }
    public Dictionary<string, int> SpeciesPopulations { get; } = new(StringComparer.Ordinal);
    public DeathCause LastDeathCause { get; private set; }
    public string LastDeathSpecies { get; private set; } = "";
    public int TrophicLevel1 => _trophicCounts.GetValueOrDefault(1);
    public int TrophicLevel2 => _trophicCounts.GetValueOrDefault(2);
    public int TrophicLevel3Plus => _trophicCounts.Where(kvp => kvp.Key >= 3).Sum(kvp => kvp.Value);
    public Dictionary<string, int> SubspeciesCounts { get; } = new(StringComparer.Ordinal);
    public int TotalSubspecies { get; private set; }
    public Dictionary<string, float> SpeciesFirstAppearance { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> SpeciesMaxPopulation { get; } = new(StringComparer.Ordinal);

    private readonly Dictionary<CreatureType, int> _typeCounts = new();
    private readonly Dictionary<int, int> _trophicCounts = new();
    private readonly List<KeyValuePair<string, int>> _speciesBuffer = new();
    private readonly List<KeyValuePair<string, int>> _subspeciesBuffer = new();

    public void RecordBirth()
    {
        TotalBirths++;
    }

    public void RecordDeath(string species, DeathCause cause)
    {
        TotalDeaths++;
        LastDeathCause = cause;
        LastDeathSpecies = species;

        switch (cause)
        {
            case DeathCause.Starvation: StarvationDeaths++; break;
            case DeathCause.OldAge: OldAgeDeaths++; break;
            case DeathCause.Predation: PredationDeaths++; break;
            case DeathCause.Combat: CombatDeaths++; break;
        }
    }

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco);

    public void Reset()
    {
        ResetCounters();
        SpeciesPopulations.Clear();
        SubspeciesCounts.Clear();
        SpeciesFirstAppearance.Clear();
        SpeciesMaxPopulation.Clear();
    }

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;
        TotalCreatures = 0;
        _typeCounts.Clear();
        _trophicCounts.Clear();

        float totalHeterozygosity = 0f;
        float totalInbreeding = 0f;
        int animalCount = 0;

        SpeciesPopulations.Clear();
        SubspeciesCounts.Clear();

        var creatures = ecosystem.Creatures;
        for (int i = 0; i < creatures.Count; i++)
        {
            var c = creatures[i];
            if (c == null || !c.IsAlive) continue;

            TotalCreatures++;
            _typeCounts[c.CreatureType] = _typeCounts.GetValueOrDefault(c.CreatureType) + 1;

            int trophic = FoodWeb.TrophicLevel(c.CreatureType);
            _trophicCounts[trophic] = _trophicCounts.GetValueOrDefault(trophic) + 1;

            if (c.CreatureType != CreatureType.Plant)
            {
                animalCount++;
                totalHeterozygosity += c.Genome.Heterozygosity;
                totalInbreeding += (float)c.InbreedingCoefficient;
            }

            var species = c.Species;
            if (species != null)
            {
                SpeciesPopulations[species] = SpeciesPopulations.GetValueOrDefault(species) + 1;
            }

            var sub = c.Subspecies;
            if (!string.IsNullOrEmpty(sub) && species != null)
            {
                string key = species + "/" + sub;
                SubspeciesCounts[key] = SubspeciesCounts.GetValueOrDefault(key) + 1;
            }
        }

        RebuildSortedDictionary(SpeciesPopulations, _speciesBuffer);
        foreach (var kvp in SpeciesPopulations)
        {
            if (!SpeciesFirstAppearance.ContainsKey(kvp.Key))
                SpeciesFirstAppearance[kvp.Key] = TotalTime;

            SpeciesMaxPopulation[kvp.Key] = Math.Max(SpeciesMaxPopulation.GetValueOrDefault(kvp.Key), kvp.Value);
        }

        RebuildSortedDictionary(SubspeciesCounts, _subspeciesBuffer);

        SpeciesCount = SpeciesPopulations.Count;
        TotalSubspecies = SubspeciesCounts.Count;

        MeanHeterozygosity = animalCount > 0 ? totalHeterozygosity / animalCount : 0f;
        MeanInbreeding = animalCount > 0 ? totalInbreeding / animalCount : 0f;
    }

    private static void RebuildSortedDictionary(Dictionary<string, int> source, List<KeyValuePair<string, int>> buffer)
    {
        buffer.Clear();
        buffer.AddRange(source);
        buffer.Sort((a, b) => b.Value.CompareTo(a.Value));

        source.Clear();
        for (int i = 0; i < buffer.Count; i++)
        {
            var kvp = buffer[i];
            source[kvp.Key] = kvp.Value;
        }
    }

    public void ResetCounters()
    {
        TotalBirths = 0;
        TotalDeaths = 0;
        StarvationDeaths = 0;
        OldAgeDeaths = 0;
        PredationDeaths = 0;
        CombatDeaths = 0;
    }
}

public enum DeathCause
{
    Starvation,
    OldAge,
    Predation,
    Combat,
    Unknown
}
