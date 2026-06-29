# PitLife-uin5: Game1.cs Decomposition

**Issue:** `PitLife-uin5` — UI: Game1.cs decomposition (1050 → 500 LOC)
**Date:** 2026-06-29
**Status:** Proposed

## Problem
`Game1.cs` is the monolithic entry point of the game, spanning over 1,100 lines of code. It violates the Single Responsibility Principle (SRP) by managing:
1. **MonoGame Lifecycle & Window Management**: Setting up graphics, fullscreen toggle, loading settings.
2. **State & Screen Routing**: Managing whether the game is in the Main Menu, Loading, or Playing screen.
3. **Input Tracking & Logic**: Polling keyboard/mouse, checking edge triggers (just-pressed), handling camera zoom/pan, hotkeys for speed, toggling overlays, and selecting creatures/terrain.
4. **Gameplay & UI Coordination**: Updating minimap, spawn panels, cataclysm panels, species editor, day-night cycle, weather, and in-game UI.
5. **Simulation & Save/Load Lifecycle**: Triggering world generation, saving/loading savegame JSON files, and syncing with the `SimulationController`.
6. **Low-level Rendering**: Drawing the world grid, weather overlay, water effects, creature textures, HUD, debug metrics, and the loading bar.

This makes the code hard to read, maintain, and unit-test.

---

## Explored Alternatives

### Alternative A: Partial Classes (`Game1.Input.cs`, `Game1.Render.cs`, etc.)
* **Pros**: Instantly reduces `Game1.cs` file size; requires zero architectural redesign or signature changes.
* **Cons**: Purely cosmetic. It does not solve the tight coupling problem, since all parts still belong to the same monolithic class with shared access to private state. No improved testability.

### Alternative B: Full UI Framework & ECS Integration
* **Pros**: Cleanest separation of concerns, separating data and systems completely.
* **Cons**: Overkill for the scope. Rewriting the UI and game loops to fit a strict ECS/UI hierarchy would introduce huge risks, change runtime behavior, and exceed the time budget.

### Alternative C: Dedicated Collaborators (`InputManager`, `SimulationOrchestrator`, `GameLoopCoordinator`)
* **Pros**: Extract concerns into testable, cohesive classes. Keeps the main `Game1` class as a thin shell that delegates to its components. Very minimal risk of breaking behavior if we keep the delegation transparent.
* **Cons**: Requires passing reference dependencies (like `Ecosystem`, `Camera`, `GraphicsDeviceManager`) to the extracted classes.

---

## Chosen Approach: Alternative C (Decomposition into Collaborators)

We will decompose the monolithic responsibilities of `Game1` into three distinct collaborator classes:

```
┌────────────────────────────────────────────────────────┐
│                        Game1                           │
│  (MonoGame Host, State Hub, Content & Asset Registry)  │
└───────┬───────────────────┬───────────────────┬────────┘
        │ delegates         │ delegates         │ delegates
        ▼                   ▼                   ▼
┌──────────────┐    ┌──────────────┐    ┌─────────────────────┐
│ InputManager │    │ Simulation-  │    │ GameLoopCoordinator │
│              │    │ Orchestrator │    │                     │
│ (Input, Drag │    │ (World Gen,  │    │  (Update/Draw Loop  │
│  Zoom, Keys) │    │  Save/Load)  │    │   Screen Routing)   │
└──────────────┘    └──────────────┘    └─────────────────────┘
```

### 1. `InputManager`
Handles mouse/keyboard state tracking and key press mappings.
* **Responsibilities**:
  - Encapsulate `MouseState` and `KeyboardState` transitions (current vs. previous).
  - Map specific hotkeys to actions (F1 for debug overlay, G for cyclopedia, F7 for manual cataclysms, F6 for species editor, C for cataclysms, F4 for spawn panel).
  - Encapsulate camera zoom/pan inputs.
  - Expose clean helper query methods: `IsKeyJustPressed(Keys)`, `IsLeftClickJustPressed()`, etc.

### 2. `SimulationOrchestrator`
Manages world generation, loading, saving, and settings persistence.
* **Responsibilities**:
  - `GenerateNewWorld(int? seed, WorldGenOptions? options)`
  - `RestoreLoadedEcosystem(SaveData data)`
  - `ResetWorldSessionState()`
  - `LoadSettings()`, `SaveSettings()`

### 3. `GameLoopCoordinator`
Orchestrates the main update and draw logic.
* **Responsibilities**:
  - Route the `Update` loop to menu handling, loading screens, or gameplay updates.
  - Route the `Draw` loop to `DrawLoadingScreen`, `DrawMainMenu`, or `DrawWorld`.
  - Encapsulate the sub-rendering routines (`DrawWorld`, `DrawFruits`, `DrawHUD`, `DrawDebugOverlay`, `DrawLoadingScreen`).

---

## Impacted Files
- `Game1.cs` &rarr; Stripped down to ~300-400 LOC, delegating to the collaborators.
- `Core/InputManager.cs` &rarr; **NEW**
- `Core/SimulationOrchestrator.cs` &rarr; **NEW**
- `Core/GameLoopCoordinator.cs` &rarr; **NEW**

---

## Verification Plan
1. **Compilation**: `dotnet build` must compile with 0 errors.
2. **Unit Tests**: Run `dotnet test` to verify that all existing 461 test cases pass.
3. **Behavioral Integrity**: Ensure that starting a new game, generating a seed, loading a save, and opening UI overlays (History, Climate, Terrain) behaves identically.
