# Aquatic Species Expansion - Design Spec

**Date**: 2026-06-18
**Status**: Approved
**Author**: opencode (brainstorming session)

## Goal

Aggiungere 4 nuove specie ittiche al simulatore ecosistema PitLife, estendendo il pattern
già stabilito per le 5 specie introdotte nel commit `ee0f498` (Fish, Lizard, Wolf, Bear, Turtle).

## Species

| Name | Type | Diet | Biome | Size (Genome) | Behavior |
|------|------|------|-------|---------------|----------|
| Shark | Carnivore | Fish/creatures | DeepOcean | 1.2x (large apex) | Patrols open water, attacks Fish/Piranha/Salmon |
| Piranha | Carnivore | Small creatures | ShallowWater (fresh) | 0.7x (small) | Swarm hunter, aggressive |
| Salmon | Herbivore | Plants (Moss, BerryBush on shores) | ShallowWater (fresh) | 0.9x (medium) | Fast prey for Bear/Eagle |
| Jellyfish | Omnivore | Plants (plankton-like) + weak prey | ShallowWater/DeepOcean | 0.6x (small) | Drift, passive, low damage |

## Architecture

**Pattern reuse**: stessa architettura delle 5 specie esistenti. Nessuna nuova astrazione.

### Files to modify

- `Simulation/Ecosystem.cs:25-29` — array `HerbivoreSpecies`/`CarnivoreSpecies`/`OmnivoreSpecies`
- `Simulation/Ecosystem.cs` (HashSet) — `AquaticSpecies` (estendere da `["Fish"]` a `["Fish", "Shark", "Piranha", "Salmon", "Jellyfish"]`)
- `Simulation/Ecosystem.cs::SpawnSpecies<T>` — aggiungere 4 case nel switch `typeof(T).Name`
- `Simulation/Herbivore.cs:10` — estendere override `IsAquatic` esistente con `|| Species == "Salmon"`
- `Simulation/Carnivore.cs` — AGGIUNGERE override `IsAquatic` (non esiste): `Species == "Shark" || Species == "Piranha"`
- `Simulation/Omnivore.cs` — AGGIUNGERE override `IsAquatic` (non esiste): `Species == "Jellyfish"`
- `Game1.cs:102-114` — registrare 4 nuove texture via `Texture2D.FromFile`

### Files to create (assets)

- `Content/assets/creatures/fish/shark.png` (34x34 top-down, PixelLab)
- `Content/assets/creatures/fish/piranha.png` (34x34 top-down, PixelLab)
- `Content/assets/creatures/fish/salmon.png` (34x34 top-down, PixelLab)
- `Content/assets/creatures/fish/jellyfish.png` (34x34 top-down, PixelLab)
- `Content/assets/creatures/fish/index.md` (registry dei pesci esistenti + nuovi)

### PixelLab generation

- view: `low top-down` (default, coerente con asset esistenti)
- size: 64px (canvas interno ~90px, output 34x34 simile a Fish esistente)
- description per specie:
  - **Shark**: "top-down pixel art great white shark, gray back, white belly, sharp teeth, menacing"
  - **Piranha**: "top-down pixel art piranha, silver body with red-orange belly, small fins, sharp teeth"
  - **Salmon**: "top-down pixel art salmon, silver-blue body with pinkish hue, streamlined, dotted back"
  - **Jellyfish**: "top-down pixel art jellyfish, translucent dome bell, long flowing tentacles, soft blue-pink"
- 4 oggetti 1-direction (1 view)
- Selezione automatica (single candidate se size > 170; altrimenti review → `select_object_frames` con indice migliore)

## Data Flow

Identico alle 5 specie esistenti:

1. `Ecosystem.Initialize()` → spawn delle 4 nuove specie con `SpawnSpecies<T>`
2. `Ecosystem.SpawnSpecies<T>` → switch su `typeof(T).Name` → chiama `RandomPassablePosition(IsAquatic)` con check su `AquaticSpecies`
3. Creature.Update → `MoveToward/MoveAwayFrom` ricevono `world` → `IsPassableFor(IsAquatic)` permette il passaggio in acqua per le 4 nuove
4. Render: `CreatureRenderer` usa la texture registrata in base a `Species`

## Edge Cases

- **Coesistenza con Fish esistente**: Shark attaccherà Fish/Piranha/Salmon. Piranha potrebbe attaccare Salmon. Bear potrebbe entrare in ShallowWater per prendere Salmon.
- **Jellyfish come invertebrato**: onnivoro + `IsAquatic=true` + basso danno. Non è un pesce vertebrato, ma segue stesso pattern logico (vive in acqua, deriva).
- **Salmon migration**: rimossa (non implementata in questo design — solo erbivoro stanziale in fiumi/laghi).

## Testing

- `dotnet build` deve essere pulito: 0 Avvisi, 0 Errori
- Verifica manuale: avvio del gioco → presenza visiva di Shark, Piranha, Salmon, Jellyfish in bioma acquatico
- Verifica spawn: nessuna `IndexOutOfRangeException` se la prima specie nel switch non viene inizializzata

## Out of Scope

- Riproduzione sessuale (issue `9i9`)
- Spatial grid (issue `v54`)
- Waypoint wandering (issue `fbj`)
- Day/night cycle (issue `b61`)
- Salmon migration tra acqua dolce e oceano
- Catena alimentare acquatica esplicita (Fish/Shark interactions)
- Nuovi biomi acquatici (CoralReef, ecc.)

## Risks

- **Sprite PixelLab scadenti**: 4 generazioni in parallelo (rate limit 8). Se uno sprite fallisce quality check, rigenerare singolarmente.
- **Jellyfish onnivoro vs carnivoro**: scelta di design. Se review mostra problemi di gameplay, spostare in `CarnivoreSpecies` con `AttackDamage` ridotto.
- **Override IsAquatic duplicato**: tre classi con stessa logica. Possibile refactor futuro: mettere check in `Creature.IsAquatic` base + `AquaticSpecies.Contains(Species)` (richiede refactor che esula questo scope).
