using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class Ecosystem
{
    public World World { get; }
    public List<Creature> Creatures { get; } = new();
    public Random Random { get; }
    public float SimulationSpeed { get; set; } = 1f;

    public int PlantCount { get; private set; }
    public int HerbivoreCount { get; private set; }
    public int CarnivoreCount { get; private set; }
    public int OmnivoreCount { get; private set; }
    public float TotalTime { get; private set; }

    public event Action? OnStatsChanged;

    public Ecosystem(int worldWidth, int worldHeight, int seed)
    {
        World = new World(worldWidth, worldHeight, seed);
        Random = new Random(seed);
    }

    private static readonly string[] PlantSpecies = ["Plant", "Flowers", "Mushroom", "GrassTuft", "Cactus"];
    private static readonly string[] HerbivoreSpecies = ["Gazelle", "Rabbit", "Deer", "Sheep"];
    private static readonly string[] CarnivoreSpecies = ["Wolf", "Fox", "Lynx", "Tiger"];
    private static readonly string[] OmnivoreSpecies = ["Bear", "Boar", "Raccoon"];

    public void Initialize(int initialHerbivores, int initialCarnivores, int initialOmnivores, int initialPlants)
    {
        for (int i = 0; i < initialPlants; i++)
            SpawnSpecies<Plant>(PlantSpecies, "Plant");
        for (int i = 0; i < initialHerbivores; i++)
            SpawnSpecies<Herbivore>(HerbivoreSpecies, "Gazelle");
        for (int i = 0; i < initialCarnivores; i++)
            SpawnSpecies<Carnivore>(CarnivoreSpecies, "Wolf");
        for (int i = 0; i < initialOmnivores; i++)
            SpawnSpecies<Omnivore>(OmnivoreSpecies, "Bear");
        UpdateStats();
    }

    private void SpawnSpecies<T>(string[] species, string defaultSpecies) where T : Creature
    {
        string name = species[Random.Next(species.Length)];
        var pos = RandomPassablePosition();
        var genome = Genome.Random(Random);
        Creature c = typeof(T).Name switch
        {
            nameof(Plant) => new Plant(pos, genome, name),
            nameof(Herbivore) => new Herbivore(pos, genome, name),
            nameof(Carnivore) => new Carnivore(pos, genome, name),
            nameof(Omnivore) => new Omnivore(pos, genome, name),
            _ => throw new ArgumentException()
        };
        Creatures.Add(c);
    }

    private Vector2 RandomPassablePosition()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = (float)(Random.NextDouble() * (World.PixelWidth - 1));
            float y = (float)(Random.NextDouble() * (World.PixelHeight - 1));
            var tile = World.GetTileAtPosition(x, y);
            if (tile.IsPassable)
                return new Vector2(x, y);
        }
        return new Vector2(World.TileSize, World.TileSize);
    }



    public void AddCreature(Creature c)
    {
        Creatures.Add(c);
    }

    public void Tick(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds * SimulationSpeed;
        var scaledTime = new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(dt));

        TotalTime += dt;

        for (int i = Creatures.Count - 1; i >= 0; i--)
        {
            var c = Creatures[i];
            if (c.IsAlive)
                c.Update(World, this, scaledTime);
        }

        ProcessDeaths();
        UpdateStats();
    }

    private void ProcessDeaths()
    {
        double decomposeChance = 0.1 * SimulationSpeed;
        for (int i = Creatures.Count - 1; i >= 0; i--)
        {
            if (!Creatures[i].IsAlive)
            {
                if (Random.NextDouble() < decomposeChance)
                    Creatures.RemoveAt(i);
            }
        }
    }

    private void UpdateStats()
    {
        int plants = 0, herbivores = 0, carnivores = 0, omnivores = 0;
        foreach (var c in Creatures)
        {
            if (!c.IsAlive) continue;
            switch (c.CreatureType)
            {
                case CreatureType.Plant: plants++; break;
                case CreatureType.Herbivore: herbivores++; break;
                case CreatureType.Carnivore: carnivores++; break;
                case CreatureType.Omnivore: omnivores++; break;
            }
        }
        PlantCount = plants;
        HerbivoreCount = herbivores;
        CarnivoreCount = carnivores;
        OmnivoreCount = omnivores;
        OnStatsChanged?.Invoke();
    }

    public Plant? FindNearestPlant(Herbivore seeker)
    {
        return FindNearest<Plant>(seeker);
    }

    public Plant? FindNearestPlantFor(Creature seeker)
    {
        return FindNearest<Plant>(seeker);
    }

    public Creature? FindNearestPrey(Creature seeker)
    {
        Creature? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (!c.IsAlive || c == seeker) continue;
            if (c.CreatureType == CreatureType.Plant && seeker is Herbivore) continue;
            if (c.CreatureType == CreatureType.Plant && seeker is Omnivore) continue;
            if (c.CreatureType == CreatureType.Carnivore) continue;
            if (c.CreatureType == seeker.CreatureType) continue;
            float d = seeker.DistanceTo(c);
            if (d < bestDist)
            {
                bestDist = d;
                best = c;
            }
        }
        return best;
    }

    public Creature? FindNearestPredator(Creature seeker)
    {
        Creature? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (!c.IsAlive || c == seeker) continue;
            if (c.CreatureType == CreatureType.Carnivore ||
                (c.CreatureType == CreatureType.Omnivore && c is Omnivore omn && omn.AttackDamage > 10f))
            {
                float d = seeker.DistanceTo(c);
                if (d < bestDist)
                {
                    bestDist = d;
                    best = c;
                }
            }
        }
        return best;
    }

    public void TrySpreadPlant(Plant plant)
    {
        if (plant.Energy < plant.ReproductionThreshold) return;
        float angle = (float)(Random.NextDouble() * Math.PI * 2);
        float dist = 30f + (float)Random.NextDouble() * 40f;
        Vector2 newPos = plant.Position + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);

        newPos.X = Math.Clamp(newPos.X, 0, World.PixelWidth - 1);
        newPos.Y = Math.Clamp(newPos.Y, 0, World.PixelHeight - 1);

        var tile = World.GetTileAtPosition(newPos.X, newPos.Y);
        if (!tile.IsPassable) return;

        Genome childGenome = Genome.Reproduce(plant.Genome, plant.Genome, Random);
        var child = new Plant(newPos, childGenome);
        child.Energy = child.MaxEnergy * 0.3f;
        Creatures.Add(child);
        plant.Energy -= plant.MaxEnergy * 0.2f;
    }

    private T? FindNearest<T>(Creature seeker) where T : Creature
    {
        T? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (!c.IsAlive || c == seeker || c is not T t) continue;
            float d = seeker.DistanceTo(c);
            if (d < bestDist)
            {
                bestDist = d;
                best = t;
            }
        }
        return best;
    }
}
