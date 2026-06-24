using System;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public sealed class AtmosphereSystem : ISimulationSystem
{
    public SimulationPhase Phase => SimulationPhase.Update;
    public float Oxygen { get; private set; } = AtmosphereConfig.Data.InitialOxygen;
    public float CO2 { get; private set; } = AtmosphereConfig.Data.InitialCO2;
    public float OxygenModifier => MathHelper.Clamp(Oxygen / AtmosphereConfig.Data.OxygenModifierBase, AtmosphereConfig.Data.OxygenModifierMin, AtmosphereConfig.Data.OxygenModifierMax);
    public float CO2Modifier => MathHelper.Clamp(CO2 / AtmosphereConfig.Data.Co2ModifierBase, AtmosphereConfig.Data.Co2ModifierMin, AtmosphereConfig.Data.Co2ModifierMax);

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco.PlantCount, eco.HerbivoreCount + eco.CarnivoreCount + eco.OmnivoreCount, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world) { }
    public void Reset() { Oxygen = AtmosphereConfig.Data.InitialOxygen; CO2 = AtmosphereConfig.Data.InitialCO2; }

    public void Update(int plantCount, int animalCount, float dt)
    {
        Oxygen += plantCount * AtmosphereConfig.Data.O2PerPlant * dt;
        Oxygen -= animalCount * AtmosphereConfig.Data.O2PerAnimal * dt;
        Oxygen = MathHelper.Clamp(Oxygen, 0f, AtmosphereConfig.Data.MaxLevel);

        CO2 += animalCount * AtmosphereConfig.Data.Co2PerAnimal * dt;
        CO2 -= AtmosphereConfig.Data.Co2DecayRate * dt;
        CO2 = MathHelper.Clamp(CO2, 0f, AtmosphereConfig.Data.MaxLevel);
    }
}
