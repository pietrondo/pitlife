using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using PitLife.Core;
using PitLife.Rendering;

namespace PitLife.Simulation;

public class Ecosystem
{
    public World World { get; }
    public List<Creature> Creatures { get; } = new();
    public Random Random { get; }
    public float SimulationSpeed { get; set; } = 1f;
    public int MaxCreatures { get; set; } = 3000;
    public bool IsFull => Creatures.Count + _pendingAdd.Count >= MaxCreatures;
    public int PlantCarryingCapacity => Math.Max(1,
        Math.Min((int)(MaxCreatures * 0.5f), World.Width * World.Height / 8));

    public int PlantCount { get; private set; }
    public int HerbivoreCount { get; private set; }
    public int CarnivoreCount { get; private set; }
    public int OmnivoreCount { get; private set; }
    public EcosystemMetrics Metrics { get; } = new();
    public float PopulationPressure { get; private set; } = 1f;
    public ClimateSystem Climate { get; } = new();
    public DiseaseSystem Disease { get; } = new();
    public PhylogeneticGraph Phylogeny { get; } = new();
    public AtmosphereSystem Atmosphere { get; } = new();
    public CataclysmSystem Cataclysms { get; } = new();
    public FlowSimulation? Flow { get; private set; }
    public DayPhase CurrentDayPhase { get; set; } = DayPhase.Day;
    private HashSet<string> _knownSpecies = new(StringComparer.Ordinal);
    public float TotalTime { get; set; }

    private readonly List<Creature> _pendingAdd = new();
    private readonly List<Creature> _pendingRemove = new();
    private readonly object _lock = new();
    private readonly SpatialGrid _spatialGrid;
    private CreatureSpawner _spawner = null!;
    private ulong _nextIndividualId = 1;

    private static readonly string[] PlantSpecies = [.. SpeciesRegistry.OfType(CreatureType.Plant)];
    private static readonly string[] AquaticPlantSpecies = [.. SpeciesRegistry.OfType(CreatureType.Plant).Where(s =>
    {
        var def = SpeciesRegistry.Get(s);
        return def != null && def.IsAquatic;
    })];
    private static readonly string[] HerbivoreSpecies = [.. SpeciesRegistry.OfType(CreatureType.Herbivore)];
    private static readonly string[] CarnivoreSpecies = [.. SpeciesRegistry.OfType(CreatureType.Carnivore)];
    private static readonly string[] OmnivoreSpecies = [.. SpeciesRegistry.OfType(CreatureType.Omnivore)];

    public static bool IsPackAnimal(string species) => SpeciesRegistry.IsPackAnimal(species);
    public static bool IsSolitary(string species) => SpeciesRegistry.IsSolitary(species);

    public int Seed { get; }

    private readonly List<ISimulationSystem> _systems = new();

    public Ecosystem(int worldWidth, int worldHeight, int seed)
    {
        Seed = seed;
        World = new World(worldWidth, worldHeight, seed);
        Random = new Random(seed);
        _spatialGrid = new SpatialGrid(World.PixelWidth, World.PixelHeight, World.TileSize * 2);
        _spawner = new CreatureSpawner(this);
        Flow = new FlowSimulation(World);
        InitSystems();
        Logger.Event("ECO", $"Ecosystem created: {worldWidth}x{worldHeight}, seed={seed}");
    }

    public Ecosystem(WorldGenOptions options, int seed)
    {
        Seed = seed;
        World = new World(options, seed);
        Random = new Random(seed);
        _spatialGrid = new SpatialGrid(World.PixelWidth, World.PixelHeight, World.TileSize * 2);
        _spawner = new CreatureSpawner(this);
        Flow = new FlowSimulation(World);
        InitSystems();
        Logger.Event("ECO", $"Ecosystem created: {options.MapWidth}x{options.MapHeight}, seed={seed}, preset={options.Preset}");
    }

    private void InitSystems()
    {
        _systems.Add(Climate);
        _systems.Add(Disease);
        _systems.Add(Atmosphere);
        _systems.Add(Cataclysms);
        if (Flow != null) _systems.Add(Flow);
        _systems.Add(Metrics);
        var sorted = _systems.OrderBy(s => s.Phase).ToList();
        _systems.Clear();
        _systems.AddRange(sorted);
        foreach (var sys in _systems)
            sys.Initialize(World);
    }

    public void Initialize(int h, int c, int o, int p)
    {
        for (int i = 0; i < p; i++) SpawnSpecies<Plant>(PlantSpecies, "Clover");
        for (int i = 0; i < p / 4; i++) SpawnSpecies<Plant>(AquaticPlantSpecies, "Seaweed");
        SpawnSubset<Herbivore>(HerbivoreSpecies, h, "Gazelle");
        SpawnSubset<Carnivore>(CarnivoreSpecies, c, "Wolf");
        SpawnSubset<Omnivore>(OmnivoreSpecies, o, "Bear");
        FlushPending();
        StaggerPlantAges();
        UpdateStats();
        Logger.Event("ECO", $"Initialized: P={p} H={h} C={c} O={o} | Total={Creatures.Count}");
    }

    private void SpawnSubset<T>(string[] species, int count, string fallback) where T : Creature
    {
        int maxSpecies = Math.Max(2, count / 5);
        var selected = species.OrderBy(_ => Random.Next()).Take(Math.Min(maxSpecies, species.Length)).ToArray();
        for (int i = 0; i < count; i++)
        {
            string name = selected[Random.Next(selected.Length)];
            var pos = RandomPassablePosition(name);
            _spawner.SpawnByName(name, pos);
        }
    }

    private void StaggerPlantAges()
    {
        foreach (var c in Creatures)
        {
            if (c is Plant && c.IsAlive)
                c.GrowFor((float)Random.NextDouble() * 120f);
        }
    }

    public void AddCreature(Creature c)
    {
        lock (_lock)
        {
            if (c.Lineage.IndividualId == 0)
                c.AssignIndividualId(_nextIndividualId++);
            else
                _nextIndividualId = Math.Max(_nextIndividualId, c.Lineage.IndividualId + 1);
            _pendingAdd.Add(c);
        }
        Logger.Event("SPAWN", $"{c.Species} at ({c.Position.X:F0},{c.Position.Y:F0})");
    }

    public void QueueRemove(Creature c)
    {
        lock (_lock) { _pendingRemove.Add(c); }
        Metrics.RecordDeath(c.Species, c.DeathCause);
        Logger.Event("DEATH", $"{c.Species} age={c.Age:F1}s energy={c.Energy:F1} cause={c.DeathCause}");
    }

    public void FlushPending()
    {
        lock (_lock)
        {
            if (_pendingRemove.Count > 0)
            {
                var remove = new HashSet<Creature>(_pendingRemove);
                foreach (var creature in remove)
                    _spatialGrid.Remove(creature);
                Creatures.RemoveAll(c => remove.Contains(c));
                _pendingRemove.Clear();
            }
            if (_pendingAdd.Count > 0)
            {
                foreach (var c in _pendingAdd)
                {
                    if (Creatures.Count < MaxCreatures)
                    {
                        Creatures.Add(c);
                        _spatialGrid.Update(c);
                    }
                }
                _pendingAdd.Clear();
            }
        }
    }

    private void SpawnSpecies<T>(string[] species, string defaultSpecies) where T : Creature
    {
        string name = species[Random.Next(species.Length)];
        var pos = RandomPassablePosition(name);
        _spawner.SpawnByName(name, pos);
    }

    public bool SpawnAt<T>(string species, Vector2 position) where T : Creature =>
        _spawner.SpawnAt<T>(species, position);

    public bool SpawnByName(string species, Vector2 position) =>
        _spawner.SpawnByName(species, position);

    private Vector2 RandomPassablePosition(string species)
    {
        var def = SpeciesRegistry.Get(species);
        bool isAquatic = def?.IsAquatic ?? false;

        for (int attempt = 0; attempt < 100; attempt++)
        {
            float x = (float)(Random.NextDouble() * Math.Max(1, World.PixelWidth - 1));
            float y = (float)(Random.NextDouble() * Math.Max(1, World.PixelHeight - 1));
            var tile = World.GetTileAtPosition(x, y);
            if (tile.IsPassableFor(isAquatic) && (def == null || def.IsValidBiome(tile.Biome)))
                return new Vector2(x, y);
        }

        int start = Random.Next(World.Width * World.Height);
        for (int offset = 0; offset < World.Width * World.Height; offset++)
        {
            int index = (start + offset) % (World.Width * World.Height);
            int tileX = index % World.Width;
            int tileY = index / World.Width;
            var tile = World.GetTile(tileX, tileY);
            if (tile.IsPassableFor(isAquatic) && (def == null || def.IsValidBiome(tile.Biome)))
                return new Vector2((tileX + 0.5f) * World.TileSize, (tileY + 0.5f) * World.TileSize);
        }

        return new Vector2(World.TileSize, World.TileSize);
    }

    public void Tick(GameTime gameTime)
    {
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds * SimulationSpeed;
        TotalTime += dt;
        _spatialGrid.Rebuild(Creatures);

        int count = Creatures.Count;
        for (int i = count - 1; i >= 0; i--)
        {
            if (i >= Creatures.Count) continue;
            var c = Creatures[i];
            if (c == null) continue;
            if (c.IsAlive)
            {
                try
                {
                    c.Update(World, this, new GameTime(gameTime.TotalGameTime, TimeSpan.FromSeconds(dt)));
                }
                catch (Exception ex)
                {
                    Logger.Error($"Creature update failed for {c.Species}: {ex.Message}");
                    c.Die();
                }
                _spatialGrid.Update(c);
            }
        }

        FlushPending();
        ProcessDeaths(dt);
        foreach (var sys in _systems)
            sys.Tick(this, gameTime);
        float grassFactor = Climate.GrassRegenModifier * Cataclysms.GrassMultiplier;
        World.RegenerateGrass(dt * grassFactor);
        UpdateStats();
    }

    private void ProcessDeaths(float dt)
    {
        double decomposeChance = 1.0 - Math.Exp(-0.02 * dt);
        for (int i = Creatures.Count - 1; i >= 0; i--)
        {
            if (i >= Creatures.Count) continue;
            var c = Creatures[i];
            if (c == null) continue;
            if (!c.IsAlive)
            {
                if (Random.NextDouble() < decomposeChance)
                {
                    var tile = World.GetTileAtPosition(c.Position.X, c.Position.Y);
                    if (tile.GrassAmount < tile.MaxGrass)
                        tile.GrassAmount = Math.Min(tile.MaxGrass, tile.GrassAmount + 0.3f);
                    tile.SoilNutrients = Math.Min(2f, tile.SoilNutrients + 0.1f);
                    _spatialGrid.Remove(c);
                    Creatures.RemoveAt(i);
                }
            }
        }
        FlushPending();
    }

    private int _logCounter = 0;
    
    public void UpdateStats()
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
        Metrics.Update(this);

        foreach (var species in _knownSpecies.ToArray())
        {
            if (!Metrics.SpeciesPopulations.ContainsKey(species) && !string.IsNullOrEmpty(species))
            {
                Logger.Event("EXTINCT", $"Species '{species}' has gone extinct at T={TotalTime:F1}s");
                _knownSpecies.Remove(species);
            }
        }
        foreach (var species in Metrics.SpeciesPopulations.Keys)
            _knownSpecies.Add(species);

        float softCap = MaxCreatures * 0.7f;
        int aliveCount = plants + herbivores + carnivores + omnivores;
        PopulationPressure = aliveCount > softCap
            ? 1f + (aliveCount - softCap) / (MaxCreatures * 0.3f) * 1.5f
            : 1f;
        
        _logCounter++;
        if (_logCounter % 60 == 0) // Log every ~1 second at 60 FPS
        {
            Logger.Event("STATS", $"T={TotalTime:F1}s P={plants} H={herbivores} C={carnivores} O={omnivores} Total={Creatures.Count}");
        }
    }

    public Plant? FindNearestPlant(Herbivore seeker) => FindNearest<Plant>(seeker);
    public Plant? FindNearestPlantFor(Creature seeker) => FindNearest<Plant>(seeker);

    public Creature? FindNearestPrey(Creature seeker)
    {
        return _spatialGrid.FindNearest(seeker, c =>
            c.CreatureType != CreatureType.Carnivore &&
            c.CreatureType != seeker.CreatureType &&
            (c.CreatureType != CreatureType.Plant || seeker is not Herbivore and not Omnivore));
    }

    public Creature? FindNearestSameSpecies(Creature seeker)
    {
        return _spatialGrid.FindNearest(seeker, c => c != seeker && c.Species == seeker.Species);
    }

    public Creature? FindNearestMate(Creature seeker)
    {
        if (!seeker.IsAdult || seeker.Gender is not (Gender.Male or Gender.Female))
            return null;
        return _spatialGrid.FindNearest(seeker, seeker.CanMateWith);
    }

    public Creature? FindNearestPredator(Creature seeker)
    {
        return _spatialGrid.FindNearest(seeker, c => c.CreatureType == CreatureType.Carnivore);
    }

    public void TrySpreadPlant(Plant plant)
    {
        if (plant == null || !plant.IsAlive) return;
        if (Creatures.Count >= MaxCreatures) return;
        if (PlantCount + CountPendingPlants() >= PlantCarryingCapacity) return;
        if (plant.Energy < plant.ReproductionThreshold) return;

        float angle = (float)(Random.NextDouble() * Math.PI * 2);
        float dist = 30f + (float)Random.NextDouble() * 40f;
        Vector2 newPos = plant.Position + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);

        newPos.X = Math.Clamp(newPos.X, 0, World.PixelWidth - 1);
        newPos.Y = Math.Clamp(newPos.Y, 0, World.PixelHeight - 1);

        var tile = World.GetTileAtPosition(newPos.X, newPos.Y);
        if (!tile.IsPassable) return;

        var sameSpeciesNearby = FindNeighbors(plant, 80f, c => c is Plant p && p.Species == plant.Species);
        if (sameSpeciesNearby.Count >= 4)
            return;

        Genome childGenome = Genome.Reproduce(plant.Genome, plant.Genome, Random);
        var child = new Plant(newPos, childGenome, plant.Species);
        child.InheritAsexualLineage(plant);
        child.Energy = child.MaxEnergy * 0.5f;
        AddCreature(child);
        plant.Energy -= plant.MaxEnergy * 0.2f;
    }

    private int CountPendingPlants()
    {
        lock (_lock)
        {
            int count = 0;
            foreach (Creature creature in _pendingAdd)
                if (creature.CreatureType == CreatureType.Plant)
                    count++;
            return count;
        }
    }

    private T? FindNearest<T>(Creature seeker) where T : Creature
    {
        return _spatialGrid.FindNearest(seeker, c => c is T) as T;
    }

    public List<Creature> FindNeighbors(Creature seeker, float radius, Func<Creature, bool> predicate)
    {
        return _spatialGrid.GetNeighbors(seeker, radius, predicate);
    }

    public void Clear()
    {
        lock (_lock)
        {
            Creatures.Clear();
            _pendingAdd.Clear();
            _pendingRemove.Clear();
            TotalTime = 0f;
            PlantCount = 0;
            HerbivoreCount = 0;
            CarnivoreCount = 0;
            OmnivoreCount = 0;
            _spatialGrid.Rebuild(Array.Empty<Creature>());
            _nextIndividualId = 1;
            foreach (var sys in _systems)
                sys.Reset();
        }
    }
}
