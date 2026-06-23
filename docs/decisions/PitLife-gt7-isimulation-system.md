# PitLife-gt7: ISimulationSystem Interface Design

**Issue:** `PitLife-gt7` — Introduce ISimulationSystem interface
**Date:** 2026-06-23
**Status:** Approved (Approach A)

## Problem

Ecosystem.cs (408 LOC) hardcodes invocations of 6 system classes, each with different `Update` signatures:

```csharp
Climate.Update(TotalTime, Random);
Disease.Update(this, dt, Random);
Atmosphere.Update(PlantCount, animalCount, dt);
Cataclysms.Update(this, dt, Random);
Cataclysms.UpdateVolcanoes(this, dt, Random);
Flow?.Update(dt, Random);
```

Adding a new system requires editing Ecosystem.cs Tick() body. No common interface exists.

## Chosen Approach: ISimulationSystem with Tick(Ecosystem, GameTime) + Phase

### Interface

```csharp
public enum SimulationPhase
{
    EarlyUpdate = 0,
    Update = 1,
    LateUpdate = 2
}

public interface ISimulationSystem
{
    SimulationPhase Phase { get; }
    void Tick(Ecosystem ecosystem, GameTime gameTime);
    void Initialize(World world);
    void Reset();
}
```

### Rationale
- `Tick(Ecosystem, GameTime)`: every system extracts what it needs (TotalTime, Random, lists, dt from gameTime.ElapsedGameTime). Pragmatic — Ecosystem is already the data hub.
- `Phase` enum: explicit ordering without hardcoded call sequence. EarlyUpdate for pre-creature state, Update for core systems, LateUpdate for post-processing.
- `Initialize(World)`: hook for systems that need world reference (FlowSimulation: elevation, grid).
- `Reset()`: clear state for world regeneration / new game.

### Phase Assignment

| System | Phase | Reason |
|--------|-------|--------|
| ClimateSystem | EarlyUpdate | Season modifiers consumed by other systems |
| AtmosphereSystem | EarlyUpdate | O2/CO2 levels affect creature behavior |
| DiseaseSystem | Update | Needs up-to-date creature positions from spatial grid |
| CataclysmSystem | Update | Cataclysms + Volcanoes (merged into single Tick) |
| FlowSimulation | Update | Water/lava flow after creature movement |
| EcosystemMetrics | LateUpdate | Aggregation after all systems have run |

### Ecosystem Changes

```csharp
// BEFORE:
public ClimateSystem Climate { get; } = new();
public DiseaseSystem Disease { get; } = new();
// ... each property-initialized

// AFTER:
private readonly List<ISimulationSystem> _systems = new();

public Ecosystem(WorldGenOptions options, int seed)
{
    // ... world init ...
    _systems.Add(new ClimateSystem());
    _systems.Add(new AtmosphereSystem());
    _systems.Add(new DiseaseSystem());
    _systems.Add(new CataclysmSystem());
    _systems.Add(new FlowSimulation(world));
    _systems.Add(new EcosystemMetrics());
    // Sort by Phase
    _systems = _systems.OrderBy(s => s.Phase).ToList();
    foreach (var s in _systems) s.Initialize(World);
}

public void Tick(GameTime gameTime)
{
    // ... creature updates (unchanged) ...
    FlushPending();
    ProcessDeaths(dt);
    
    // System orchestration:
    foreach (var system in _systems)
        system.Tick(this, gameTime);
    
    // Post-system logic (grass regen, stats) — unchanged
}
```

### Per-System Migration

Each system's existing `Update` method is renamed/adapted to `Tick(Ecosystem, GameTime)`. Internal logic unchanged. Example:

```csharp
// ClimateSystem BEFORE:
public void Update(float totalTime, Random rng) { ... }

// ClimateSystem AFTER:
public SimulationPhase Phase => SimulationPhase.EarlyUpdate;
public void Tick(Ecosystem eco, GameTime gt) => Update(eco.TotalTime, eco.Random);
public void Initialize(World w) { }
public void Reset() { /* reset extreme events */ }
```

**CataclysmSystem merges `Update()` + `UpdateVolcanoes()` into a single `Tick()`.**

### Impact: 6 files changed
1. `ISimulationSystem.cs` — NEW
2. `SimulationPhase.cs` — NEW (or inline in interface file)
3. `Ecosystem.cs` — orchestrator refactor (~15 lines changed)
4. `ClimateSystem.cs` — implement interface (~10 lines)
5. `DiseaseSystem.cs` — implement interface (~10 lines)  
6. `AtmosphereSystem.cs` — implement interface (~10 lines)
7. `CataclysmSystem.cs` — implement interface, merge Update+UpdateVolcanoes (~15 lines)
8. `FlowSimulation.cs` — implement interface (~10 lines)
9. `EcosystemMetrics.cs` — implement interface (~10 lines)

### Non-Goals
- No ECS framework (too big a leap)
- No dependency injection container (pragmatic — systems are stateless except internal state)
- No breaking change to save/load (WorldState serialization unaffected)

### Verification
- `dotnet build` green
- Existing tests pass
- `graphify update .` after changes
