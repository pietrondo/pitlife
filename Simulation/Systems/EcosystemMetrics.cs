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
    public int Plants { get; private set; }
    public int Herbivores { get; private set; }
    public int Carnivores { get; private set; }
    public int Omnivores { get; private set; }
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
    public int TrophicLevel1 { get; private set; }
    public int TrophicLevel2 { get; private set; }
    public int TrophicLevel3Plus { get; private set; }
    public Dictionary<string, int> SubspeciesCounts { get; } = new(StringComparer.Ordinal);
    public int TotalSubspecies { get; private set; }
    public Dictionary<string, float> SpeciesFirstAppearance { get; } = new(StringComparer.Ordinal);
    public Dictionary<string, int> SpeciesMaxPopulation { get; } = new(StringComparer.Ordinal);

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

    public void Initialize(World world) { }

    public void Reset() { ResetCounters(); SpeciesPopulations.Clear(); SubspeciesCounts.Clear(); SpeciesFirstAppearance.Clear(); SpeciesMaxPopulation.Clear(); }

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;
        var alive = ecosystem.Creatures.Where(c => c != null && c.IsAlive).ToList();

        TotalCreatures = alive.Count;
        Plants = alive.Count(c => c.CreatureType == CreatureType.Plant);
        Herbivores = alive.Count(c => c.CreatureType == CreatureType.Herbivore);
        Carnivores = alive.Count(c => c.CreatureType == CreatureType.Carnivore);
        Omnivores = alive.Count(c => c.CreatureType == CreatureType.Omnivore);

        var pops = alive.GroupBy(c => c.Species).ToDictionary(g => g.Key, g => g.Count());
        SpeciesPopulations.Clear();
        foreach (var kvp in pops.OrderByDescending(x => x.Value)) SpeciesPopulations[kvp.Key] = kvp.Value;

        var subs = alive.Where(c => !string.IsNullOrEmpty(c.Subspecies))
                        .GroupBy(c => $"{c.Species}/{c.Subspecies}")
                        .ToDictionary(g => g.Key, g => g.Count());
        SubspeciesCounts.Clear();
        foreach (var kvp in subs.OrderByDescending(x => x.Value)) SubspeciesCounts[kvp.Key] = kvp.Value;

        SpeciesCount = SpeciesPopulations.Count;
        TotalSubspecies = SubspeciesCounts.Count;

        foreach (var kvp in SpeciesPopulations)
        {
            if (!SpeciesFirstAppearance.ContainsKey(kvp.Key)) SpeciesFirstAppearance[kvp.Key] = TotalTime;
            if (kvp.Value > SpeciesMaxPopulation.GetValueOrDefault(kvp.Key, 0)) SpeciesMaxPopulation[kvp.Key] = kvp.Value;
        }

        TrophicLevel1 = alive.Count(c => FoodWeb.TrophicLevel(c.CreatureType) == 1);
        TrophicLevel2 = alive.Count(c => FoodWeb.TrophicLevel(c.CreatureType) == 2);
        TrophicLevel3Plus = alive.Count(c => FoodWeb.TrophicLevel(c.CreatureType) >= 3);

        var animals = alive.Where(c => c.CreatureType != CreatureType.Plant).ToList();
        MeanHeterozygosity = animals.Count > 0 ? animals.Average(c => c.Genome.Heterozygosity) : 0f;
        MeanInbreeding = animals.Count > 0 ? animals.Average(c => (float)c.InbreedingCoefficient) : 0f;
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
