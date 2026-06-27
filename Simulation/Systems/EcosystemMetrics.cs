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

    private int _totalBirths;
    private int _totalDeaths;
    private int _starvationDeaths;
    private int _oldAgeDeaths;
    private int _predationDeaths;
    private int _combatDeaths;

    public void RecordBirth()
    {
        _totalBirths++;
        TotalBirths = _totalBirths;
    }

    public void RecordDeath(string species, DeathCause cause)
    {
        _totalDeaths++;
        TotalDeaths = _totalDeaths;
        LastDeathCause = cause;
        LastDeathSpecies = species;

        switch (cause)
        {
            case DeathCause.Starvation: _starvationDeaths++; StarvationDeaths = _starvationDeaths; break;
            case DeathCause.OldAge: _oldAgeDeaths++; OldAgeDeaths = _oldAgeDeaths; break;
            case DeathCause.Predation: _predationDeaths++; PredationDeaths = _predationDeaths; break;
            case DeathCause.Combat: _combatDeaths++; CombatDeaths = _combatDeaths; break;
        }
    }

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco);

    public void Initialize(World world) { }

    public void Reset() { ResetCounters(); SpeciesPopulations.Clear(); SubspeciesCounts.Clear(); SpeciesFirstAppearance.Clear(); SpeciesMaxPopulation.Clear(); }

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;
        var aliveCreatures = ecosystem.Creatures.Where(c => c != null && c.IsAlive).ToList();

        TotalCreatures = aliveCreatures.Count;
        var byType = aliveCreatures.GroupBy(c => c.CreatureType).ToDictionary(g => g.Key, g => g.Count());
        Plants = byType.GetValueOrDefault(CreatureType.Plant);
        Herbivores = byType.GetValueOrDefault(CreatureType.Herbivore);
        Carnivores = byType.GetValueOrDefault(CreatureType.Carnivore);
        Omnivores = byType.GetValueOrDefault(CreatureType.Omnivore);

        var bySpecies = aliveCreatures.GroupBy(c => c.Species).ToDictionary(g => g.Key, g => g.Count());
        SpeciesPopulations.Clear();
        foreach (var kvp in bySpecies.OrderByDescending(kvp => kvp.Value)) SpeciesPopulations[kvp.Key] = kvp.Value;
        SpeciesCount = bySpecies.Count;

        foreach (var (species, count) in bySpecies)
        {
            if (!SpeciesFirstAppearance.ContainsKey(species)) SpeciesFirstAppearance[species] = TotalTime;
            SpeciesMaxPopulation.TryGetValue(species, out int prevMax);
            if (count > prevMax) SpeciesMaxPopulation[species] = count;
        }

        var bySubspecies = aliveCreatures.Where(c => !string.IsNullOrEmpty(c.Subspecies))
                                         .GroupBy(c => $"{c.Species}/{c.Subspecies}")
                                         .ToDictionary(g => g.Key, g => g.Count());
        SubspeciesCounts.Clear();
        foreach (var kvp in bySubspecies.OrderByDescending(kvp => kvp.Value)) SubspeciesCounts[kvp.Key] = kvp.Value;
        TotalSubspecies = bySubspecies.Count;

        if (TotalCreatures > 0)
        {
            var trophs = aliveCreatures.GroupBy(c => FoodWeb.TrophicLevel(c.CreatureType)).ToDictionary(g => g.Key, g => g.Count());
            TrophicLevel1 = trophs.GetValueOrDefault(1);
            TrophicLevel2 = trophs.GetValueOrDefault(2);
            TrophicLevel3Plus = aliveCreatures.Count(c => FoodWeb.TrophicLevel(c.CreatureType) >= 3);

            var animals = aliveCreatures.Where(c => c.CreatureType != CreatureType.Plant).ToList();
            if (animals.Count > 0)
            {
                MeanHeterozygosity = animals.Average(c => c.Genome.Heterozygosity);
                MeanInbreeding = (float)animals.Average(c => c.InbreedingCoefficient);
            }
            else { MeanHeterozygosity = 0f; MeanInbreeding = 0f; }
        }
        else
        {
            TrophicLevel1 = 0; TrophicLevel2 = 0; TrophicLevel3Plus = 0;
            MeanHeterozygosity = 0f; MeanInbreeding = 0f;
        }
    }

    public void ResetCounters()
    {
        _totalBirths = 0;
        _totalDeaths = 0;
        _starvationDeaths = 0;
        _oldAgeDeaths = 0;
        _predationDeaths = 0;
        _combatDeaths = 0;
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
