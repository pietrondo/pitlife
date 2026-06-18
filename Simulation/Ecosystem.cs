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
    public int MaxCreatures { get; set; } = 3000;

    public int PlantCount { get; private set; }
    public int HerbivoreCount { get; private set; }
    public int CarnivoreCount { get; private set; }
    public int OmnivoreCount { get; private set; }
    public float TotalTime { get; private set; }

    private readonly List<Creature> _pendingAdd = new();
    private readonly List<Creature> _pendingRemove = new();
    private readonly object _lock = new();

    private static readonly string[] PlantSpecies = ["Plant", "Flowers", "Mushroom", "GrassTuft", "Cactus"];
    private static readonly string[] HerbivoreSpecies = ["Gazelle", "Rabbit", "Deer", "Sheep", "Horse", "Goat", "Moose", "Bison", "Antelope"];
    private static readonly string[] CarnivoreSpecies = ["Wolf", "Fox", "Lynx", "Tiger", "Cougar", "Hyena"];
    private static readonly string[] OmnivoreSpecies = ["Bear", "Boar", "Raccoon", "Skunk", "Hedgehog"];

    public Ecosystem(int worldWidth, int worldHeight, int seed)
    {
        World = new World(worldWidth, worldHeight, seed);
        Random = new Random(seed);
    }

    public void Initialize(int h, int c, int o, int p)
    {
        for (int i = 0; i < p; i++) SpawnSpecies<Plant>(PlantSpecies, "Plant");
        for (int i = 0; i < h; i++) SpawnSpecies<Herbivore>(HerbivoreSpecies, "Gazelle");
        for (int i = 0; i < c; i++) SpawnSpecies<Carnivore>(CarnivoreSpecies, "Wolf");
        for (int i = 0; i < o; i++) SpawnSpecies<Omnivore>(OmnivoreSpecies, "Bear");
        FlushPending();
        UpdateStats();
    }

    public void AddCreature(Creature c)
    {
        lock (_lock) { _pendingAdd.Add(c); }
    }

    public void QueueRemove(Creature c)
    {
        lock (_lock) { _pendingRemove.Add(c); }
    }

    private void FlushPending()
    {
        lock (_lock)
        {
            if (_pendingRemove.Count > 0)
            {
                var remove = new HashSet<Creature>(_pendingRemove);
                Creatures.RemoveAll(c => remove.Contains(c));
                _pendingRemove.Clear();
            }
            if (_pendingAdd.Count > 0)
            {
                foreach (var c in _pendingAdd)
                {
                    if (Creatures.Count < MaxCreatures)
                        Creatures.Add(c);
                }
                _pendingAdd.Clear();
            }
        }
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
        AddCreature(c);
    }

    private Vector2 RandomPassablePosition()
    {
        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = (float)(Random.NextDouble() * Math.Max(1, World.PixelWidth - 1));
            float y = (float)(Random.NextDouble() * Math.Max(1, World.PixelHeight - 1));
            var tile = World.GetTileAtPosition(x, y);
            if (tile.IsPassable)
                return new Vector2(x, y);
        }
        return new Vector2(World.TileSize, World.TileSize);
    }

    public void Tick(GameTime gameTime)
    {
        try
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds * SimulationSpeed;
            TotalTime += dt;

            int count = Creatures.Count;
            for (int i = count - 1; i >= 0; i--)
            {
                if (i >= Creatures.Count) continue;
                var c = Creatures[i];
                if (c == null) continue;
                if (c.IsAlive)
                {
                    try { c.Update(World, this, new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(dt))); }
                    catch { c.Die(); }
                }
            }

            FlushPending();
            ProcessDeaths();
            UpdateStats();
        }
        catch { }
    }

    private void ProcessDeaths()
    {
        double decomposeChance = 0.05 * SimulationSpeed;
        for (int i = Creatures.Count - 1; i >= 0; i--)
        {
            if (i >= Creatures.Count) continue;
            var c = Creatures[i];
            if (c == null) continue;
            if (!c.IsAlive && Random.NextDouble() < decomposeChance)
            {
                Creatures.RemoveAt(i);
            }
        }
        FlushPending();
    }

    private void UpdateStats()
    {
        int plants = 0, herbivores = 0, carnivores = 0, omnivores = 0;
        foreach (var c in Creatures)
        {
            if (c == null || !c.IsAlive) continue;
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
    }

    public Plant? FindNearestPlant(Herbivore seeker) => FindNearest<Plant>(seeker);
    public Plant? FindNearestPlantFor(Creature seeker) => FindNearest<Plant>(seeker);

    public Creature? FindNearestPrey(Creature seeker)
    {
        Creature? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (c == null || !c.IsAlive || c == seeker) continue;
            if (c.CreatureType == CreatureType.Carnivore) continue;
            if (c.CreatureType == seeker.CreatureType) continue;
            if (c.CreatureType == CreatureType.Plant && (seeker is Herbivore || seeker is Omnivore)) continue;
            float d = seeker.DistanceTo(c);
            if (d < bestDist) { bestDist = d; best = c; }
        }
        return best;
    }

    public Creature? FindNearestPredator(Creature seeker)
    {
        Creature? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (c == null || !c.IsAlive || c == seeker) continue;
            bool isPredator = c.CreatureType == CreatureType.Carnivore;
            if (isPredator)
            {
                float d = seeker.DistanceTo(c);
                if (d < bestDist) { bestDist = d; best = c; }
            }
        }
        return best;
    }

    public void TrySpreadPlant(Plant plant)
    {
        if (plant == null || !plant.IsAlive) return;
        if (Creatures.Count >= MaxCreatures) return;
        if (plant.Energy < plant.ReproductionThreshold) return;

        float angle = (float)(Random.NextDouble() * Math.PI * 2);
        float dist = 30f + (float)Random.NextDouble() * 40f;
        Vector2 newPos = plant.Position + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);

        newPos.X = Math.Clamp(newPos.X, 0, World.PixelWidth - 1);
        newPos.Y = Math.Clamp(newPos.Y, 0, World.PixelHeight - 1);

        var tile = World.GetTileAtPosition(newPos.X, newPos.Y);
        if (!tile.IsPassable) return;

        Genome childGenome = Genome.Reproduce(plant.Genome, plant.Genome, Random);
        var child = new Plant(newPos, childGenome, plant.Species);
        child.Energy = child.MaxEnergy * 0.3f;
        AddCreature(child);
        plant.Energy -= plant.MaxEnergy * 0.2f;
    }

    private T? FindNearest<T>(Creature seeker) where T : Creature
    {
        T? best = null;
        float bestDist = float.MaxValue;
        foreach (var c in Creatures)
        {
            if (c == null || !c.IsAlive || c == seeker || c is not T t) continue;
            float d = seeker.DistanceTo(c);
            if (d < bestDist) { bestDist = d; best = t; }
        }
        return best;
    }
}
