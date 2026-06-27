using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class EcosystemMetrics : ISimulationSystem
{
    public UpdatePhase Phase => UpdatePhase.LateUpdate;
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

    private readonly List<KeyValuePair<string, int>> _speciesListCache = new();
    private readonly List<KeyValuePair<string, int>> _subspeciesListCache = new();

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;
        int totalCreatures = 0;
        int plants = 0;
        int herbivores = 0;
        int carnivores = 0;
        int omnivores = 0;

        SpeciesPopulations.Clear();
        SubspeciesCounts.Clear();

        int troph1 = 0;
        int troph2 = 0;
        int troph3Plus = 0;

        float sumHetero = 0f;
        float sumInbreeding = 0f;
        int animalCount = 0;

        for (int i = 0; i < ecosystem.Creatures.Count; i++)
        {
            var c = ecosystem.Creatures[i];
            if (c == null || !c.IsAlive) continue;

            totalCreatures++;
            switch (c.CreatureType)
            {
                case CreatureType.Plant: plants++; break;
                case CreatureType.Herbivore: herbivores++; break;
                case CreatureType.Carnivore: carnivores++; break;
                case CreatureType.Omnivore: omnivores++; break;
            }

            if (SpeciesPopulations.TryGetValue(c.Species, out int count))
                SpeciesPopulations[c.Species] = count + 1;
            else
                SpeciesPopulations[c.Species] = 1;

            if (!string.IsNullOrEmpty(c.Subspecies))
            {
                string subKey = $"{c.Species}/{c.Subspecies}";
                if (SubspeciesCounts.TryGetValue(subKey, out int subCount))
                    SubspeciesCounts[subKey] = subCount + 1;
                else
                    SubspeciesCounts[subKey] = 1;
            }

            int troph = FoodWeb.TrophicLevel(c.CreatureType);
            if (troph == 1) troph1++;
            else if (troph == 2) troph2++;
            else if (troph >= 3) troph3Plus++;

            if (c.CreatureType != CreatureType.Plant)
            {
                sumHetero += c.Genome.Heterozygosity;
                sumInbreeding += (float)c.InbreedingCoefficient;
                animalCount++;
            }
        }

        TotalCreatures = totalCreatures;
        Plants = plants;
        Herbivores = herbivores;
        Carnivores = carnivores;
        Omnivores = omnivores;

        SpeciesCount = SpeciesPopulations.Count;
        TotalSubspecies = SubspeciesCounts.Count;

        _speciesListCache.Clear();
        foreach (var kvp in SpeciesPopulations) _speciesListCache.Add(kvp);
        _speciesListCache.Sort((a, b) => b.Value.CompareTo(a.Value));
        SpeciesPopulations.Clear();
        for (int i = 0; i < _speciesListCache.Count; i++)
            SpeciesPopulations[_speciesListCache[i].Key] = _speciesListCache[i].Value;

        _subspeciesListCache.Clear();
        foreach (var kvp in SubspeciesCounts) _subspeciesListCache.Add(kvp);
        _subspeciesListCache.Sort((a, b) => b.Value.CompareTo(a.Value));
        SubspeciesCounts.Clear();
        for (int i = 0; i < _subspeciesListCache.Count; i++)
            SubspeciesCounts[_subspeciesListCache[i].Key] = _subspeciesListCache[i].Value;

        foreach (var kvp in SpeciesPopulations)
        {
            if (!SpeciesFirstAppearance.ContainsKey(kvp.Key)) SpeciesFirstAppearance[kvp.Key] = TotalTime;
            SpeciesMaxPopulation.TryGetValue(kvp.Key, out int prevMax);
            if (kvp.Value > prevMax) SpeciesMaxPopulation[kvp.Key] = kvp.Value;
        }

        TrophicLevel1 = troph1;
        TrophicLevel2 = troph2;
        TrophicLevel3Plus = troph3Plus;

        if (animalCount > 0)
        {
            MeanHeterozygosity = sumHetero / animalCount;
            MeanInbreeding = sumInbreeding / animalCount;
        }
        else
        {
            MeanHeterozygosity = 0f;
            MeanInbreeding = 0f;
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
