using System;
using System.Collections.Generic;
using System.Linq;

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

    public void Update(Ecosystem ecosystem)
    {
        TotalTime = ecosystem.TotalTime;
        Plants = ecosystem.PlantCount;
        Herbivores = ecosystem.HerbivoreCount;
        Carnivores = ecosystem.CarnivoreCount;
        Omnivores = ecosystem.OmnivoreCount;
        TotalCreatures = ecosystem.Creatures.Count(c => c != null && c.IsAlive);

        var aliveBySpecies = new Dictionary<string, int>(StringComparer.Ordinal);
        var aliveCreatures = new List<Creature>();
        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || !c.IsAlive) continue;
            aliveCreatures.Add(c);
            aliveBySpecies.TryGetValue(c.Species, out int count);
            aliveBySpecies[c.Species] = count + 1;
        }

        SpeciesPopulations.Clear();
        foreach (var kvp in aliveBySpecies.OrderByDescending(kvp => kvp.Value))
            SpeciesPopulations[kvp.Key] = kvp.Value;

        SpeciesCount = aliveBySpecies.Count;

        if (aliveCreatures.Count > 0)
        {
            int t1 = 0, t2 = 0, t3p = 0;
            foreach (var c in aliveCreatures)
            {
                int level = FoodWeb.TrophicLevel(c.CreatureType);
                if (level == 1) t1++;
                else if (level == 2) t2++;
                else t3p++;
            }
            TrophicLevel1 = t1;
            TrophicLevel2 = t2;
            TrophicLevel3Plus = t3p;
            float totalHet = 0f;
            float totalInb = 0f;
            int geneticCount = 0;
            foreach (var c in aliveCreatures)
            {
                if (c.CreatureType == CreatureType.Plant) continue;
                totalHet += c.Genome.Heterozygosity;
                totalInb += c.InbreedingCoefficient;
                geneticCount++;
            }
            if (geneticCount > 0)
            {
                MeanHeterozygosity = totalHet / geneticCount;
                MeanInbreeding = totalInb / geneticCount;
            }
        }
        else
        {
            MeanHeterozygosity = 0f;
            MeanInbreeding = 0f;
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
