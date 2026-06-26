using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

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
        float ageRatio = 1f - (Lifetime / MaxLifetime);
        if (Poisonous)
            return Color.Lerp(Color.DarkViolet, Color.Purple, ageRatio);
        return Color.Lerp(Color.Red, new Color(139, 69, 19), ageRatio);
    }
}

public sealed class FruitSystem
{

    private const int MaxFruits = 500;
    private readonly Fruit[] _fruits = new Fruit[MaxFruits];
    private int _fruitCount;
    private float _spawnTimer;

    public IReadOnlyList<Fruit> Fruits => new ArraySegment<Fruit>(_fruits, 0, _fruitCount);

    public void Tick(Ecosystem eco, GameTime gameTime) => Update(eco, (float)gameTime.ElapsedGameTime.TotalSeconds * eco.SimulationSpeed);

    public void Initialize(World world) { }

    public void Reset()
    {
        _fruitCount = 0;
        _spawnTimer = 0;
    }

    public void Update(Ecosystem eco, float dt)
    {
        // Decay existing fruits
        for (int i = _fruitCount - 1; i >= 0; i--)
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
        if (_spawnTimer <= 0 && _fruitCount < MaxFruits)
        {
            _spawnTimer = 3f + (float)eco.Random.NextDouble() * 5f;
            SpawnFruits(eco);
        }
    }

    private void SpawnFruits(Ecosystem eco)
    {
        if (eco.Creatures.Count == 0) return;

        int attempts = 10;
        for (int a = 0; a < attempts; a++)
        {
            if (_fruitCount >= MaxFruits) break;

            // Pick a random alive plant
            int idx = eco.Random.Next(eco.Creatures.Count);
            int tries = 0;
            Creature? plant = null;
            while (tries < 50)
            {
                var candidate = eco.Creatures[(idx + tries) % eco.Creatures.Count];
                if (candidate.IsAlive && candidate.CreatureType == CreatureType.Plant)
                {
                    plant = candidate;
                    break;
                }
                tries++;
            }
            if (plant == null) continue;

            var def = SpeciesRegistry.Get(plant.Species);
            if (def == null || def.PlantReproduction != PlantReproductionMode.Seeds) continue;

            // Spawn fruit near the plant
            float offsetX = (float)(eco.Random.NextDouble() - 0.5) * 40f;
            float offsetY = (float)(eco.Random.NextDouble() - 0.5) * 40f;
            var pos = new Vector2(
                Math.Clamp(plant.Position.X + offsetX, 0, eco.World.PixelWidth - 1),
                Math.Clamp(plant.Position.Y + offsetY, 0, eco.World.PixelHeight - 1));

            float energyValue = 5f + (float)eco.Random.NextDouble() * 10f;
            float lifetime = 20f + (float)eco.Random.NextDouble() * 30f;

            bool poisonous = plant.Species is "Belladonna" or "VenusFlyTrap" or "PitcherPlant";
            float toxicity = poisonous ? 0.6f + (float)eco.Random.NextDouble() * 0.3f : 0f;

            _fruits[_fruitCount++] = new Fruit(pos, energyValue, lifetime, plant.Species, poisonous, toxicity);
        }
    }

    public Fruit? TryEatFruit(Vector2 position, float eatDistance)
    {
        for (int i = 0; i < _fruitCount; i++)
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
}
