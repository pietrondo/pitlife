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

    public void Initialize(World world) { }

    public void Reset() { ResetCounters(); SpeciesPopulations.Clear(); SubspeciesCounts.Clear(); SpeciesFirstAppearance.Clear(); SpeciesMaxPopulation.Clear(); }

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;

        TotalCreatures = 0;
        Plants = 0;
        Herbivores = 0;
        Carnivores = 0;
        Omnivores = 0;

        TrophicLevel1 = 0;
        TrophicLevel2 = 0;
        TrophicLevel3Plus = 0;

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

            switch (c.CreatureType)
            {
                case CreatureType.Plant:
                    Plants++;
                    break;
                case CreatureType.Herbivore:
                    Herbivores++;
                    break;
                case CreatureType.Carnivore:
                    Carnivores++;
                    break;
                case CreatureType.Omnivore:
                    Omnivores++;
                    break;
            }

            // Trophic Levels
            int trophic = FoodWeb.TrophicLevel(c.CreatureType);
            if (trophic == 1) TrophicLevel1++;
            else if (trophic == 2) TrophicLevel2++;
            else if (trophic >= 3) TrophicLevel3Plus++;

            // Animals stats
            if (c.CreatureType != CreatureType.Plant)
            {
                animalCount++;
                totalHeterozygosity += c.Genome.Heterozygosity;
                totalInbreeding += (float)c.InbreedingCoefficient;
            }

            // Species population
            var species = c.Species;
            if (species != null)
            {
                SpeciesPopulations.TryGetValue(species, out int pop);
                SpeciesPopulations[species] = pop + 1;
            }

            // Subspecies
            var sub = c.Subspecies;
            if (!string.IsNullOrEmpty(sub) && species != null)
            {
                string key = species + "/" + sub;
                SubspeciesCounts.TryGetValue(key, out int count);
                SubspeciesCounts[key] = count + 1;
            }
        }

        // Sort dictionaries - we need to rebuild them sorted
        // Buffer populations
        _speciesBuffer.Clear();
        foreach (var kvp in SpeciesPopulations)
            _speciesBuffer.Add(kvp);

        _speciesBuffer.Sort((a, b) => b.Value.CompareTo(a.Value));

        SpeciesPopulations.Clear();
        for (int i = 0; i < _speciesBuffer.Count; i++)
        {
            var kvp = _speciesBuffer[i];
            SpeciesPopulations[kvp.Key] = kvp.Value;

            // Max Population / First appearance update
            if (!SpeciesFirstAppearance.ContainsKey(kvp.Key)) SpeciesFirstAppearance[kvp.Key] = TotalTime;

            SpeciesMaxPopulation.TryGetValue(kvp.Key, out int maxPop);
            if (kvp.Value > maxPop) SpeciesMaxPopulation[kvp.Key] = kvp.Value;
        }

        // Buffer subspecies
        _subspeciesBuffer.Clear();
        foreach (var kvp in SubspeciesCounts)
            _subspeciesBuffer.Add(kvp);

        _subspeciesBuffer.Sort((a, b) => b.Value.CompareTo(a.Value));

        SubspeciesCounts.Clear();
        for (int i = 0; i < _subspeciesBuffer.Count; i++)
        {
            var kvp = _subspeciesBuffer[i];
            SubspeciesCounts[kvp.Key] = kvp.Value;
        }

        SpeciesCount = SpeciesPopulations.Count;
        TotalSubspecies = SubspeciesCounts.Count;

        MeanHeterozygosity = animalCount > 0 ? totalHeterozygosity / animalCount : 0f;
        MeanInbreeding = animalCount > 0 ? totalInbreeding / animalCount : 0f;
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
