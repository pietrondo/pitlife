using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class TrophicDynamics
{

    // Prey (herbivore) population growth rates
    public float HerbivoreBirthBonus { get; private set; } = 1f;
    public float HerbivoreDeathPenalty { get; private set; } = 1f;
    // Predator (carnivore) population growth rates
    public float CarnivoreBirthBonus { get; private set; } = 1f;
    public float CarnivoreDeathPenalty { get; private set; } = 1f;

    // Population history for oscillation tracking (last 60 data points)
    public int HerbivorePeak { get; private set; }
    public int CarnivorePeak { get; private set; }
    public int HerbivoreTrough { get; private set; } = int.MaxValue;
    public int CarnivoreTrough { get; private set; } = int.MaxValue;
    public float CyclePhase { get; private set; } // 0=balanced, +1=prey boom, -1=predator boom
    public string CurrentPhaseLabel { get; private set; } = "Balanced";

    private float _timeSinceLastSample;
    private const float SampleInterval = 5f; // record state every 5 seconds

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world) { }

    public void Reset()
    {
        HerbivoreBirthBonus = 1f;
        HerbivoreDeathPenalty = 1f;
        CarnivoreBirthBonus = 1f;
        CarnivoreDeathPenalty = 1f;
        CyclePhase = 0f;
        CurrentPhaseLabel = "Balanced";
    }

    public void Update(Ecosystem eco, float dt)
    {
        int herbivores = eco.HerbivoreCount;
        int carnivores = eco.CarnivoreCount;
        int plants = eco.PlantCount;

        // Sample population for historical tracking
        _timeSinceLastSample += dt;
        if (_timeSinceLastSample >= SampleInterval)
        {
            _timeSinceLastSample = 0f;
            HerbivorePeak = Math.Max(HerbivorePeak, herbivores);
            CarnivorePeak = Math.Max(CarnivorePeak, carnivores);
            HerbivoreTrough = Math.Min(HerbivoreTrough, herbivores);
            CarnivoreTrough = Math.Min(CarnivoreTrough, carnivores);
        }

        // Lotka-Volterra: compute predator-prey ratio
        // When predators >> prey: prey drops, then predators starve
        // When prey >> predators: prey booms, then predators follow
        float ratio = carnivores > 0 ? (float)herbivores / carnivores : 10f;

        // Prey (herbivore) dynamics
        // Prey reproduce more when predators are scarce (high ratio)
        if (ratio > 5f)
        {
            // Prey boom: lots of herbivores, few carnivores
            HerbivoreBirthBonus = 1.3f;
            HerbivoreDeathPenalty = 0.7f;
            CarnivoreBirthBonus = 1.5f; // predators get bonus from abundant food
            CarnivoreDeathPenalty = 0.5f;
            CurrentPhaseLabel = "Prey Boom";
            CyclePhase = 1f;
        }
        else if (ratio < 2f)
        {
            // Predator pressure: many carnivores per herbivore
            HerbivoreBirthBonus = 0.6f;
            HerbivoreDeathPenalty = 2f; // prey death rate doubles
            CarnivoreBirthBonus = 0.4f; // predator birth slows (overcrowding)
            CarnivoreDeathPenalty = 2f; // predators starve from competition
            CurrentPhaseLabel = "Predator Pressure";
            CyclePhase = -1f;
        }
        else
        {
            // Balanced
            HerbivoreBirthBonus = 1f;
            HerbivoreDeathPenalty = 1f;
            CarnivoreBirthBonus = 1f;
            CarnivoreDeathPenalty = 1f;
            CurrentPhaseLabel = "Balanced";
            CyclePhase = 0f;
        }

        // Additional plant-herbivore coupling
        if (plants > 0 && herbivores > plants * 3)
        {
            // Overgrazing: too many herbivores for available plants
            HerbivoreDeathPenalty *= 1.5f;
            HerbivoreBirthBonus *= 0.5f;
            CurrentPhaseLabel = "Overgrazing";
        }
        else if (herbivores > 0 && plants > herbivores * 5)
        {
            // Plant overgrowth: lots of food, few consumers
            HerbivoreBirthBonus *= 1.3f;
        }

        // Clamp multipliers to reasonable ranges
        HerbivoreBirthBonus = Math.Clamp(HerbivoreBirthBonus, 0.2f, 2f);
        HerbivoreDeathPenalty = Math.Clamp(HerbivoreDeathPenalty, 0.3f, 3f);
        CarnivoreBirthBonus = Math.Clamp(CarnivoreBirthBonus, 0.2f, 2f);
        CarnivoreDeathPenalty = Math.Clamp(CarnivoreDeathPenalty, 0.3f, 3f);
    }

    public string GetStatusLine()
    {
        return $"Trophic: {CurrentPhaseLabel} | H:{HerbivoreBirthBonus:F1}/{HerbivoreDeathPenalty:F1} C:{CarnivoreBirthBonus:F1}/{CarnivoreDeathPenalty:F1}";
    }
}
