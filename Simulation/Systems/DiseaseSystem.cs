using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class DiseaseSystem
{
    public readonly struct DiseaseDef
    {
        public string Name { get; init; }
        public float TransmissionRate { get; init; }
        public float Lethality { get; init; }
        public float RecoveryTime { get; init; }
        public float EnergyDrain { get; init; }
    }

    public static readonly DiseaseDef[] Presets = LoadPresets();

    private static DiseaseDef[] LoadPresets()
    {
        var entries = DiseaseConfig.Diseases;
        var result = new DiseaseDef[entries.Count];
        for (var i = 0; i < entries.Count; i++)
        {
            var e = entries[i];
            result[i] = new DiseaseDef
            {
                Name = e.Name,
                TransmissionRate = e.TransmissionRate,
                Lethality = e.Lethality,
                RecoveryTime = e.RecoveryTime,
                EnergyDrain = e.EnergyDrain
            };
        }
        return result;
    }

    private DiseaseDef _activeDisease;
    private float _outbreakTimer = DiseaseConfig.Outbreak.InitialTimerSeconds;
    private bool _hasOutbreak;

    public bool HasOutbreak => _hasOutbreak;
    public string ActiveDiseaseName => _hasOutbreak ? _activeDisease.Name : "";

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed, eco.Random);

    public void Initialize(World world) { }
    public void Reset() { _hasOutbreak = false; _outbreakTimer = DiseaseConfig.Outbreak.InitialTimerSeconds; }

    public void Update(Ecosystem ecosystem, float dt, Random rng)
    {
        if (_hasOutbreak)
        {
            SpreadAndProgress(ecosystem, dt, rng);
            return;
        }

        _outbreakTimer -= dt;
        if (_outbreakTimer <= 0 && ecosystem.Creatures.Count > DiseaseConfig.Outbreak.MinCreatures)
        {
            _activeDisease = Presets[rng.Next(Presets.Length)];
            var candidates = new List<Creature>();
            foreach (var c in ecosystem.Creatures)
            {
                if (c == null || !c.IsAlive || c.CreatureType == CreatureType.Plant) continue;
                candidates.Add(c);
            }
            if (candidates.Count > 0)
            {
                var patientZero = candidates[rng.Next(candidates.Count)];
                patientZero.IsInfected = true;
                patientZero.DiseaseTimer = _activeDisease.RecoveryTime;
                patientZero.DiseaseName = _activeDisease.Name;
                _hasOutbreak = true;
                Logger.Event("DISEASE", $"Outbreak of {_activeDisease.Name} started with {patientZero.Species} at T={ecosystem.TotalTime:F1}s");
            }
            _outbreakTimer = 120f + (float)rng.NextDouble() * 240f;
        }

        if (_outbreakTimer <= 0)
            _outbreakTimer = 120f + (float)rng.NextDouble() * 240f;
    }

    private void SpreadAndProgress(Ecosystem ecosystem, float dt, Random rng)
    {
        var infected = new List<Creature>();
        var aliveAnimals = 0;
        foreach (var c in ecosystem.Creatures)
        {
            if (c == null || !c.IsAlive || c.CreatureType == CreatureType.Plant) continue;
            aliveAnimals++;
            if (c.IsInfected) infected.Add(c);
        }

        if (infected.Count == 0 || aliveAnimals == 0)
        {
            _hasOutbreak = false;
            return;
        }

        foreach (var carrier in infected)
        {
            ProcessCarrier(ecosystem, carrier, dt, rng);
        }
    }

    private void ProcessCarrier(Ecosystem ecosystem, Creature carrier, float dt, Random rng)
    {
        carrier.DiseaseTimer -= dt;
        carrier.Energy -= _activeDisease.EnergyDrain * dt;

        if (carrier.Energy <= 0)
        {
            carrier.Die(DeathCause.Starvation);
            return;
        }

        if (carrier.DiseaseTimer <= 0)
        {
            carrier.IsInfected = false;
            carrier.DiseaseName = "";
            carrier.Immunity = Math.Min(1f, carrier.Immunity + 0.3f);
            Logger.Event("DISEASE", $"{carrier.Species} recovered from {_activeDisease.Name} at T={ecosystem.TotalTime:F1}s");
            return;
        }

        var transmissionChance = _activeDisease.TransmissionRate * dt;
        if (rng.NextDouble() >= transmissionChance) return;

        var neighbors = ecosystem.FindNeighbors(carrier, 30f,
            n => n.IsAlive && !n.IsInfected && n.CreatureType != CreatureType.Plant
                 && n.Species == carrier.Species);

        foreach (var n in neighbors)
        {
            if (rng.NextDouble() < (1f - n.Immunity))
            {
                n.IsInfected = true;
                n.DiseaseTimer = _activeDisease.RecoveryTime;
                n.DiseaseName = _activeDisease.Name;
            }
        }
    }
}
