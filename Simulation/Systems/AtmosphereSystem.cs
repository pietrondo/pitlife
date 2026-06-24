using System;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public sealed class AtmosphereSystem : ISimulationSystem
{
    public SimulationPhase Phase => SimulationPhase.Update;
    public float Oxygen { get; private set; } = 50f;
    public float CO2 { get; private set; } = 30f;
    public float OxygenModifier => MathHelper.Clamp(Oxygen / 50f, 0.3f, 2f);
    public float CO2Modifier => MathHelper.Clamp(CO2 / 30f, 0.5f, 2f);

    private const float MaxLevel = 100f;
    private const float O2PerPlant = 0.003f;
    private const float O2PerAnimal = 0.002f;
    private const float CO2PerAnimal = 0.001f;
    private const float CO2DecayRate = 0.01f;

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco.PlantCount, eco.HerbivoreCount + eco.CarnivoreCount + eco.OmnivoreCount, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world) { }
    public void Reset() { Oxygen = 50f; CO2 = 30f; }

    public void Update(int plantCount, int animalCount, float dt)
    {
        Oxygen += plantCount * O2PerPlant * dt;
        Oxygen -= animalCount * O2PerAnimal * dt;
        Oxygen = MathHelper.Clamp(Oxygen, 0f, MaxLevel);

        CO2 += animalCount * CO2PerAnimal * dt;
        CO2 -= CO2DecayRate * dt;
        CO2 = MathHelper.Clamp(CO2, 0f, MaxLevel);
    }
}
