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
    public float PopulationPressure { get; private set; } = 1f;
    public PhylogeneticGraph Phylogeny { get; } = new();
    public CreaturePool Pool { get; } = new();
    public SpatialGrid Spatial { get; }
    public EcosystemMetrics Metrics { get; } = new();
    public ClimateSystem Climate { get; } = new();
    public DiseaseSystem Disease { get; } = new();
    public AtmosphereSystem Atmosphere { get; } = new();
    public CataclysmSystem Cataclysms { get; } = new();
    public TrophicDynamics Trophic { get; } = new();
    public FruitSystem Fruits { get; } = new();
    public FlowSimulation Flow { get; private set; } = null!;
    public DayPhase CurrentDayPhase { get; set; } = DayPhase.Day;
    private HashSet<string> _knownSpecies = new(StringComparer.Ordinal);
    public float TotalTime { get; set; }

    private readonly List<Creature> _pendingAdd = new();
    private readonly List<Creature> _pendingRemove = new();
    private readonly object _lock = new();
    private CreatureSpawner _spawner = null!;
    private ulong _nextIndividualId = 1;

    // GROUP SPAWN_
    private const int SPAWN_AQUATIC_DIVISOR = 4;
    private const int MAX_POSITION_ATTEMPTS = 100;
    private const float PROXIMITY_SQ_DIST = 3600f;
    private const int MAX_NEARBY = 3;

    // GROUP SPREAD_
    private const float WIND_BIAS_THRESHOLD = 0.3f;
    private const float WIND_BIAS_LOCAL_WEIGHT = 0.3f;
    private const float WIND_BIAS_WIND_WEIGHT = 0.7f;
    private const float SPREAD_MIN_DIST = 30f;
    private const float SPREAD_RANGE = 40f;
    private const float NEIGHBOR_RADIUS = 80f;
    private const int MAX_SAME_SPECIES_NEARBY = 4;
    
    

    // GROUP DECAY_
    private const double DECOMPOSE_RATE = 0.02;
    private const float DECAY_GRASS_BOOST = 0.3f;
    private const float SOIL_MAX = 2f;
    private const float SOIL_BOOST = 0.1f;

    // GROUP PRESSURE_
    private const float SOFT_CAP_RATIO = 0.7f;
    private const float PRESSURE_RANGE_RATIO = 0.3f;
    private const float MAX_PRESSURE_MULTIPLIER = 1.5f;

    // GROUP STAGGER_
    private const float MAX_STAGGER_SECONDS = 120f;

    // GROUP LOG_
    private const int STATS_LOG_INTERVAL = 60;

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

    public Ecosystem(int worldWidth, int worldHeight, int seed)
    {
        Seed = seed;
        World = new World(worldWidth, worldHeight, seed);
        Random = new Random(seed);
        Spatial = new SpatialGrid(World.PixelWidth, World.PixelHeight, World.TileSize * 2);
        _spawner = new CreatureSpawner(this);
        InitSystems();
        Logger.Event("ECO", $"Ecosystem created: {worldWidth}x{worldHeight}, seed={seed}");
    }

    public Ecosystem(WorldGenOptions options, int seed)
    {
        Seed = seed;
        World = new World(options, seed);
        Random = new Random(seed);
        Spatial = new SpatialGrid(World.PixelWidth, World.PixelHeight, World.TileSize * 2);
        _spawner = new CreatureSpawner(this);
        InitSystems();
        Logger.Event("ECO", $"Ecosystem created: {options.MapWidth}x{options.MapHeight}, seed={seed}, preset={options.Preset}");
    }

    private void InitSystems()
    {
        Flow = new FlowSimulation(World);
        Fruits.Initialize(World);
    }

    public void Initialize(int h, int c, int o, int p)
    {
        for (var i = 0; i < p; i++) SpawnSpecies<Plant>(PlantSpecies, "Clover");
        for (var i = 0; i < p / BalanceConfig.Data.Ecosystem.SpawnAquaticDivisor; i++) SpawnSpecies<Plant>(AquaticPlantSpecies, "Seaweed");
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
        var maxSpecies = Math.Max(2, count / 5);
        var take = Math.Min(maxSpecies, species.Length);
        var selected = species.ToArray();
        for (int i = selected.Length - 1; i > 0; i--)
        {
            int j = Random.Next(i + 1);
            (selected[i], selected[j]) = (selected[j], selected[i]);
        }
        selected = selected[..take];
        for (var i = 0; i < count; i++)
        {
            var name = selected[Random.Next(selected.Length)];
            var pos = RandomPassablePosition(name);
            var nearby = 0;
            foreach (var c in Creatures)
                if (c.Species == name && Vector2.DistanceSquared(c.Position, pos) < BalanceConfig.Data.Ecosystem.ProximitySqDist)
                    nearby++;
            if (nearby >= BalanceConfig.Data.Ecosystem.MaxNearby)
            {
                Logger.Warn($"Spawn: skipping {name} at ({pos.X:F0},{pos.Y:F0}) - {nearby} nearby");
                continue;
            }
            if (!_spawner.SpawnByName(name, pos))
                Logger.Warn($"Spawn: {name} failed at ({pos.X:F0},{pos.Y:F0})");
        }
    }

    private void StaggerPlantAges()
    {
        foreach (var c in Creatures)
        {
            if (c is Plant && c.IsAlive)
                c.GrowFor((float)Random.NextDouble() * BalanceConfig.Data.Ecosystem.MaxStaggerSeconds);
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
            FlushRemovals();
            FlushAdditions();
        }
    }

    private void FlushRemovals()
    {
        if (_pendingRemove.Count == 0) return;
        var remove = new HashSet<Creature>(_pendingRemove);
        foreach (var creature in remove)
            Spatial.Remove(creature);
        Creatures.RemoveAll(c => remove.Contains(c));
        _pendingRemove.Clear();
    }

    private void FlushAdditions()
    {
        if (_pendingAdd.Count == 0) return;
        foreach (var c in _pendingAdd)
        {
            if (Creatures.Count >= MaxCreatures) break;
            Creatures.Add(c);
            Spatial.Update(c);
        }
        _pendingAdd.Clear();
    }

    private void SpawnSpecies<T>(string[] species, string defaultSpecies) where T : Creature
    {
        var name = species[Random.Next(species.Length)];
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
        var isAquatic = def?.IsAquatic ?? false;

        for (var attempt = 0; attempt < BalanceConfig.Data.Ecosystem.MaxPositionAttempts; attempt++)
        {
            var x = (float)(Random.NextDouble() * Math.Max(1, World.PixelWidth - 1));
            var y = (float)(Random.NextDouble() * Math.Max(1, World.PixelHeight - 1));
            var tile = World.GetTileAtPosition(x, y);
            if (tile.IsPassableFor(isAquatic) && (def == null || def.IsValidClimate(tile.Biome,
                    Climate.GetTileTemperature(tile, y / World.TileSize, World.Height))))
                return new Vector2(x, y);
        }

        var start = Random.Next(World.Width * World.Height);
        for (var offset = 0; offset < World.Width * World.Height; offset++)
        {
            var index = (start + offset) % (World.Width * World.Height);
            var tileX = index % World.Width;
            var tileY = index / World.Width;
            var tile = World.GetTile(tileX, tileY);
            if (tile.IsPassableFor(isAquatic) && (def == null || def.IsValidClimate(tile.Biome,
                    Climate.GetTileTemperature(tile, tileY, World.Height))))
                return new Vector2((tileX + 0.5f) * World.TileSize, (tileY + 0.5f) * World.TileSize);
        }

        return new Vector2(World.TileSize, World.TileSize);
    }

    public void Tick(GameTime gameTime)
    {
        var dt = (float)gameTime.ElapsedGameTime.TotalSeconds * SimulationSpeed;
        TotalTime += dt;
        Spatial.Rebuild(Creatures);

        var count = Creatures.Count;
        for (var i = count - 1; i >= 0; i--)
        {
            var c = Creatures[i];
            if (c.IsAlive)
            {
                try
                {
                    c.Update(World, this, dt);
                }
                catch (Exception ex)
                {
                    Logger.Error($"Creature update failed for {c.Species}: {ex.Message}");
                    Spatial.Remove(c);
                    Pool.Return(c);
                    Creatures.RemoveAt(i);
                    continue;
                }
                Spatial.Update(c);
                c.ApplyWindDrift(Climate.WindDirection, Climate.WindSpeed, dt, World);
            }
        }

        FlushPending();
        ProcessDeaths(dt);

        Climate.Tick(this, gameTime);
        Atmosphere.Tick(this, gameTime);
        Trophic.Tick(this, gameTime);

        Cataclysms.Tick(this, gameTime);
        Flow.Tick(this, gameTime);
        Fruits.Tick(this, gameTime);
        Disease.Tick(this, gameTime);

        Metrics.Tick(this, gameTime);

        var grassFactor = Climate.GrassRegenModifier * Cataclysms.GrassMultiplier;
        World.RegenerateGrass(dt * grassFactor);
        World.ProcessRecovery(dt);
        UpdateStats();
    }

    private void ProcessDeaths(float dt)
    {
        var decomposeChance = 1.0 - Math.Exp(-BalanceConfig.Data.Ecosystem.DecomposeRate * dt);
        for (var i = Creatures.Count - 1; i >= 0; i--)
        {
            var c = Creatures[i];
            if (!c.IsAlive)
            {
                if (Random.NextDouble() < decomposeChance)
                {
                    var tile = World.GetTileAtPosition(c.Position.X, c.Position.Y);
                    if (tile.GrassAmount < tile.MaxGrass)
                        tile.GrassAmount = Math.Min(tile.MaxGrass, tile.GrassAmount + BalanceConfig.Data.Ecosystem.DecayGrassBoost);
                    tile.SoilNutrients = Math.Min(BalanceConfig.Data.Ecosystem.SoilMax, tile.SoilNutrients + BalanceConfig.Data.Ecosystem.SoilBoost);
                    Spatial.Remove(c);
                    Pool.Return(c);
                    var last = Creatures.Count - 1;
                    Creatures[i] = Creatures[last];
                    Creatures.RemoveAt(last);
                }
            }
        }
        FlushPending();
    }

    private int _logCounter = 0;

    public void UpdateStats()
    {
        Metrics.Update(this);
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

        var softCap = MaxCreatures * BalanceConfig.Data.Ecosystem.SoftCapRatio;
        var aliveCount = plants + herbivores + carnivores + omnivores;
        PopulationPressure = aliveCount > softCap
            ? 1f + (aliveCount - softCap) / (MaxCreatures * BalanceConfig.Data.Ecosystem.PressureRangeRatio) * BalanceConfig.Data.Ecosystem.MaxPressureMultiplier
            : 1f;

        _logCounter++;
        if (_logCounter % BalanceConfig.Data.Ecosystem.StatsLogInterval == 0) // Log every ~1 second at 60 FPS
        {
            Logger.Event("STATS", $"T={TotalTime:F1}s P={plants} H={herbivores} C={carnivores} O={omnivores} Total={Creatures.Count}");
        }
    }

    public Plant? FindNearestPlant(Herbivore seeker) => Spatial.FindNearestPlant(seeker);
    public Plant? FindNearestPlantFor(Creature seeker) => Spatial.FindNearestPlantFor(seeker);
    public Creature? FindNearestPrey(Creature seeker) => Spatial.FindNearestPrey(seeker);
    public Creature? FindNearestSameSpecies(Creature seeker) => Spatial.FindNearestSameSpecies(seeker);
    public Creature? FindNearestMate(Creature seeker) => Spatial.FindNearestMate(seeker);
    public Creature? FindNearestPredator(Creature seeker) => Spatial.FindNearestPredator(seeker);

    public void TrySpreadPlant(Plant plant)
    {
        if (plant == null || !plant.IsAlive) return;
        if (Creatures.Count >= MaxCreatures) return;
        if (PlantCount + CountPendingPlants() >= PlantCarryingCapacity) return;
        if (plant.Energy < plant.ReproductionThreshold) return;

        var angle = (float)(Random.NextDouble() * Math.PI * 2);
        var windBias = Climate.WindDirection;
        if (plant.Genome.ForestAdaptation > BalanceConfig.Data.Ecosystem.WindBiasThreshold)
            angle = (angle * BalanceConfig.Data.Ecosystem.WindBiasLocalWeight + windBias * BalanceConfig.Data.Ecosystem.WindBiasWindWeight + (float)Math.PI * 0.5f) % ((float)Math.PI * 2);
        var dist = BalanceConfig.Data.Ecosystem.SpreadMinDist + (float)Random.NextDouble() * BalanceConfig.Data.Ecosystem.SpreadRange;
        Vector2 newPos = plant.Position + new Vector2((float)Math.Cos(angle) * dist, (float)Math.Sin(angle) * dist);

        newPos.X = Math.Clamp(newPos.X, 0, World.PixelWidth - 1);
        newPos.Y = Math.Clamp(newPos.Y, 0, World.PixelHeight - 1);

        var tile = World.GetTileAtPosition(newPos.X, newPos.Y);
        if (!tile.IsPassable) return;

        var def = SpeciesRegistry.Get(plant.Species);
        if (def != null)
        {
            var tileY = (int)(newPos.Y / World.TileSize);
            if (!def.IsValidClimate(tile.Biome, Climate.GetTileTemperature(tile, tileY, World.Height)))
                return;
        }

        var sameSpeciesNearby = FindNeighbors(plant, BalanceConfig.Data.Ecosystem.NeighborRadius, c => c is Plant p && p.Species == plant.Species);
        if (sameSpeciesNearby.Count >= BalanceConfig.Data.Ecosystem.MaxSameSpeciesNearby)
            return;

        Genome childGenome = Genome.Reproduce(plant.Genome, plant.Genome, Random);
        var child = new Plant(newPos, childGenome, plant.Species);
        child.InheritAsexualLineage(plant);
        child.Energy = child.MaxEnergy * Core.CreatureConfig.Data.ChildEnergyRatio;
        AddCreature(child);
        plant.Energy -= plant.MaxEnergy * Core.CreatureConfig.Data.ReproductionEnergyCostRatio;
    }

    private int CountPendingPlants()
    {
        lock (_lock)
        {
            var count = 0;
            foreach (Creature creature in _pendingAdd)
                if (creature.CreatureType == CreatureType.Plant)
                    count++;
            return count;
        }
    }

    public List<Creature> FindNeighbors(Creature seeker, float radius, Func<Creature, bool> predicate)
    {
        return Spatial.GetNeighbors(seeker, radius, predicate);
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
            Spatial.Rebuild(Array.Empty<Creature>());
            _nextIndividualId = 1;
            Climate.Reset();
            Atmosphere.Reset();
            Trophic.Reset();
            Disease.Reset();
            Cataclysms.Reset();
            Flow.Reset();
            Fruits.Reset();
            Metrics.Reset();
        }
    }
}
