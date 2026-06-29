using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using PitLife.Core;

namespace PitLife.Simulation;

public struct Fruit
{
    public Vector2 Position;
    public float EnergyValue;
    public float Lifetime; // seconds remaining
    public float MaxLifetime;
    public string PlantSpecies;
    public bool Poisonous;
    public float Toxicity;
    public bool IsAlive => Lifetime > 0;

    public Fruit(Vector2 position, float energyValue, float lifetime, string plantSpecies, bool poisonous = false, float toxicity = 0f)
    {
        Position = position;
        EnergyValue = energyValue;
        Lifetime = lifetime;
        MaxLifetime = lifetime;
        PlantSpecies = plantSpecies;
        Poisonous = poisonous;
        Toxicity = toxicity;
    }

    public Color GetColor()
    {
        if (!IsAlive) return Color.Transparent;
        var ageRatio = 1f - (Lifetime / MaxLifetime);
        if (Poisonous)
            return Color.Lerp(Color.DarkViolet, Color.Purple, ageRatio);
        return Color.Lerp(Color.Red, new Color(139, 69, 19), ageRatio);
    }
}

public sealed class FruitSystem
{


    private Fruit[] _fruits = new Fruit[FruitConfig.Data.MaxFruits];
    private int _fruitCount;
    private float _spawnTimer;

    public IReadOnlyList<Fruit> Fruits => new ArraySegment<Fruit>(_fruits, 0, _fruitCount);

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world)
    {
        if (_fruits == null || _fruits.Length != FruitConfig.Data.MaxFruits)
        {
            _fruits = new Fruit[FruitConfig.Data.MaxFruits];
        }
    }

    public void Reset()
    {
        _fruitCount = 0;
        _spawnTimer = 0;
    }

    public void Update(Ecosystem eco, float dt)
    {
        // Decay existing fruits
        for (var i = _fruitCount - 1; i >= 0; i--)
        {
            if (_fruits[i].IsAlive)
            {
                _fruits[i].Lifetime -= dt;
            }
            else
            {
                // Remove dead fruit by swapping with last
                _fruitCount--;
                if (i < _fruitCount)
                    _fruits[i] = _fruits[_fruitCount];
            }
        }

        // Spawn new fruits from plants
        _spawnTimer -= dt;
        if (_spawnTimer <= 0 && _fruitCount < _fruits.Length)
        {
            _spawnTimer = FruitConfig.Data.SpawnTimerBase + (float)eco.Random.NextDouble() * FruitConfig.Data.SpawnTimerVariance;
            SpawnFruits(eco);
        }
    }

    private void SpawnFruits(Ecosystem eco)
    {
        if (eco.Creatures.Count == 0) return;

        var attempts = FruitConfig.Data.SpawnAttempts;
        for (var a = 0; a < attempts; a++)
        {
            if (_fruitCount >= _fruits.Length) break;

            // Pick a random alive plant
            Creature? plant = TryFindRandomFruitPlant(eco);
            if (plant == null) continue;

            var def = SpeciesRegistry.Get(plant.Species);
            if (def == null || def.PlantReproduction != PlantReproductionMode.Seeds) continue;

            // Spawn fruit near the plant
            var offsetX = (float)(eco.Random.NextDouble() - 0.5) * FruitConfig.Data.SpawnOffsetMax;
            var offsetY = (float)(eco.Random.NextDouble() - 0.5) * FruitConfig.Data.SpawnOffsetMax;
            var pos = new Vector2(
                Math.Clamp(plant.Position.X + offsetX, 0, eco.World.PixelWidth - 1),
                Math.Clamp(plant.Position.Y + offsetY, 0, eco.World.PixelHeight - 1));

            var energyValue = FruitConfig.Data.EnergyValueBase + (float)eco.Random.NextDouble() * FruitConfig.Data.EnergyValueVariance;
            var lifetime = FruitConfig.Data.LifetimeBase + (float)eco.Random.NextDouble() * FruitConfig.Data.LifetimeVariance;

            var poisonous = plant.Species is "Belladonna" or "VenusFlyTrap" or "PitcherPlant";
            var toxicity = poisonous ? FruitConfig.Data.PoisonousToxicityBase + (float)eco.Random.NextDouble() * FruitConfig.Data.PoisonousToxicityVariance : 0f;

            _fruits[_fruitCount++] = new Fruit(pos, energyValue, lifetime, plant.Species, poisonous, toxicity);
        }
    }

    public Fruit? TryEatFruit(Vector2 position, float eatDistance)
    {
        for (var i = 0; i < _fruitCount; i++)
        {
            if (!_fruits[i].IsAlive) continue;
            if (Vector2.Distance(position, _fruits[i].Position) <= eatDistance)
            {
                var fruit = _fruits[i];
                // Remove fruit by swapping with last
                _fruitCount--;
                if (i < _fruitCount)
                    _fruits[i] = _fruits[_fruitCount];
                return fruit;
            }
        }
        return null;
    }
    private Creature? TryFindRandomFruitPlant(Ecosystem eco)
    {
        var idx = eco.Random.Next(eco.Creatures.Count);
        var tries = 0;
        while (tries < FruitConfig.Data.FindPlantMaxTries)
        {
            var candidate = eco.Creatures[(idx + tries) % eco.Creatures.Count];
            if (candidate.IsAlive && candidate.CreatureType == CreatureType.Plant)
            {
                return candidate;
            }
            tries++;
        }
        return null;
    }
}
