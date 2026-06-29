# PitLife-iycx: InGameUi.cs Decomposition

**Issue:** `PitLife-iycx` — UI: InGameUi.cs decomposition
**Date:** 2026-06-29
**Status:** Proposed

## Problem
`InGameUi.cs` is a monolithic class exceeding 920 lines of code. It manages the layout, update logic, and specific drawing of six highly distinct overlay windows:
1. **Statistics Window**: Population counts, elapsed time, metrics.
2. **Creature Details Window**: Lineage, genomes, status of selected creatures.
3. **Terrain Window**: Coordinates, tile properties, water/lava heights, biome.
4. **Cataclysm Window**: Cataclysm type selection and activation buttons.
5. **Climate Window**: Temperature history sparklines, seasonal and orbital metrics.
6. **History Window**: circular buffer sparklines for population snapshots and temperature trends.

This mixing of concerns violates the Single Responsibility Principle (SRP) and creates a bloated, hard-to-maintain file.

---

## Explored Alternatives

### Alternative A: Keep `UiWindow` sealed and use wrapper components
* **Pros**: Preserves the `sealed` constraint of `UiWindow`.
* **Cons**: Introduces extra boilerplate. Each panel wrapper has to expose `Bounds`, `IsOpen`, `Title` etc. to delegate to the wrapped window, or `InGameUi` must manage pairs of `(UiWindow, ContentPresenter)`.

### Alternative B: Unseal `UiWindow` and use inheritance for specialized windows
* **Pros**: Subclasses directly inherit window properties, layout rules, and drag/collapse behaviors. The `UiWindowManager` can continue managing them as `UiWindow` elements without changes. Keeps the custom content drawing localized in each subclass.
* **Cons**: Requires modifying `UiWindow.cs` to remove the `sealed` modifier.

---

## Chosen Approach: Alternative B (Subclassing `UiWindow`)

We will unseal `UiWindow` and decompose the layout and drawing code of the windows into six distinct subclasses under a new `UI/Windows/` directory:

```
                  ┌──────────────┐
                  │   UiWindow   │ (unsealed)
                  └──────┬───────┘
                         │
      ┌──────────────────┼─────────────────┐
      ▼                  ▼                 ▼
┌────────────┐    ┌─────────────┐    ┌───────────┐
│ Statistics │    │   Creature  │    │  Terrain  │ ...etc.
│   Window   │    │DetailsWindow│    │  Window   │
└────────────┘    └─────────────┘    └───────────┘
```

### Extracted Classes

1. **`StatisticsWindow`** (`UI/Windows/StatisticsWindow.cs`):
   - Exposes `DrawContent` to draw populations, simulation speed/pause status, and trophic level metrics.
2. **`CreatureDetailsWindow`** (`UI/Windows/CreatureDetailsWindow.cs`):
   - Exposes `DrawContent` to draw selected creature stats, lineage, inbreeding coefficient, age, and genomes.
3. **`TerrainWindow`** (`UI/Windows/TerrainWindow.cs`):
   - Exposes `DrawContent` to draw biome information, coordinates, elevation, water, and temperature of the selected or hovered tile.
4. **`CataclysmWindow`** (`UI/Windows/CataclysmWindow.cs`):
   - Exposes `DrawContent` to draw buttons for selecting the cataclysms (meteor, drought, disease, volcanic, freeze).
5. **`ClimateWindow`** (`UI/Windows/ClimateWindow.cs`):
   - Exposes `DrawContent` to draw climate conditions, seasonal colors, temperature trends, and atmosphere percentages.
6. **`HistoryWindow`** (`UI/Windows/HistoryWindow.cs`):
   - Exposes `DrawContent` to draw sparklines of plant, herbivore, carnivore, omnivore population snapshots, and temperature histories.

---

## Impacted Files
- `UI/UiWindow.cs` &rarr; Unsealed.
- `UI/InGameUi.cs` &rarr; Stripped down to coordinate toolbar, delegate draw/update to specialized window instances, and record history data.
- `UI/Windows/StatisticsWindow.cs` &rarr; **NEW**
- `UI/Windows/CreatureDetailsWindow.cs` &rarr; **NEW**
- `UI/Windows/TerrainWindow.cs` &rarr; **NEW**
- `UI/Windows/CataclysmWindow.cs` &rarr; **NEW**
- `UI/Windows/ClimateWindow.cs` &rarr; **NEW**
- `UI/Windows/HistoryWindow.cs` &rarr; **NEW**

---

## Verification Plan
1. **Compilation**: Run `dotnet build` to ensure all references compile successfully.
2. **Unit Tests**: Ensure all 461 unit tests continue to pass via `dotnet test`.
3. **Layout & Drag Behavior**: Manually test window tiling (`F5`), dragging windows, collapsing via double-click on title bar, closing, and toggling visibility.
