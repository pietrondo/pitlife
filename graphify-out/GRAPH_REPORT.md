# Graph Report - PitLife  (2026-06-21)

## Corpus Check
- 83 files · ~44,161 words
- Verdict: corpus is large enough that graph structure adds value.

## Summary
- 937 nodes · 1383 edges · 80 communities (60 shown, 20 thin omitted)
- Extraction: 100% EXTRACTED · 0% INFERRED · 0% AMBIGUOUS · INFERRED: 1 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Graph Freshness
- Built from commit: `1bdac549`
- Run `git rev-parse HEAD` and compare to check if the graph is stale.
- Run `graphify update .` after code changes (no API cost).

## Community Hubs (Navigation)
- [[_COMMUNITY_Community 0|Community 0]]
- [[_COMMUNITY_Community 1|Community 1]]
- [[_COMMUNITY_Community 2|Community 2]]
- [[_COMMUNITY_Community 3|Community 3]]
- [[_COMMUNITY_Community 4|Community 4]]
- [[_COMMUNITY_Community 5|Community 5]]
- [[_COMMUNITY_Community 6|Community 6]]
- [[_COMMUNITY_Community 7|Community 7]]
- [[_COMMUNITY_Community 8|Community 8]]
- [[_COMMUNITY_Community 9|Community 9]]
- [[_COMMUNITY_Community 10|Community 10]]
- [[_COMMUNITY_Community 11|Community 11]]
- [[_COMMUNITY_Community 12|Community 12]]
- [[_COMMUNITY_Community 13|Community 13]]
- [[_COMMUNITY_Community 14|Community 14]]
- [[_COMMUNITY_Community 15|Community 15]]
- [[_COMMUNITY_Community 16|Community 16]]
- [[_COMMUNITY_Community 17|Community 17]]
- [[_COMMUNITY_Community 18|Community 18]]
- [[_COMMUNITY_Community 19|Community 19]]
- [[_COMMUNITY_Community 20|Community 20]]
- [[_COMMUNITY_Community 21|Community 21]]
- [[_COMMUNITY_Community 22|Community 22]]
- [[_COMMUNITY_Community 23|Community 23]]
- [[_COMMUNITY_Community 24|Community 24]]
- [[_COMMUNITY_Community 25|Community 25]]
- [[_COMMUNITY_Community 26|Community 26]]
- [[_COMMUNITY_Community 27|Community 27]]
- [[_COMMUNITY_Community 28|Community 28]]
- [[_COMMUNITY_Community 29|Community 29]]
- [[_COMMUNITY_Community 30|Community 30]]
- [[_COMMUNITY_Community 31|Community 31]]
- [[_COMMUNITY_Community 32|Community 32]]
- [[_COMMUNITY_Community 33|Community 33]]
- [[_COMMUNITY_Community 34|Community 34]]
- [[_COMMUNITY_Community 35|Community 35]]
- [[_COMMUNITY_Community 36|Community 36]]
- [[_COMMUNITY_Community 37|Community 37]]
- [[_COMMUNITY_Community 38|Community 38]]
- [[_COMMUNITY_Community 39|Community 39]]
- [[_COMMUNITY_Community 40|Community 40]]
- [[_COMMUNITY_Community 41|Community 41]]
- [[_COMMUNITY_Community 42|Community 42]]
- [[_COMMUNITY_Community 43|Community 43]]
- [[_COMMUNITY_Community 44|Community 44]]
- [[_COMMUNITY_Community 45|Community 45]]
- [[_COMMUNITY_Community 46|Community 46]]
- [[_COMMUNITY_Community 47|Community 47]]
- [[_COMMUNITY_Community 48|Community 48]]
- [[_COMMUNITY_Community 49|Community 49]]
- [[_COMMUNITY_Community 50|Community 50]]
- [[_COMMUNITY_Community 51|Community 51]]
- [[_COMMUNITY_Community 52|Community 52]]
- [[_COMMUNITY_Community 53|Community 53]]
- [[_COMMUNITY_Community 54|Community 54]]
- [[_COMMUNITY_Community 55|Community 55]]
- [[_COMMUNITY_Community 56|Community 56]]
- [[_COMMUNITY_Community 57|Community 57]]
- [[_COMMUNITY_Community 58|Community 58]]
- [[_COMMUNITY_Community 59|Community 59]]
- [[_COMMUNITY_Community 60|Community 60]]
- [[_COMMUNITY_Community 61|Community 61]]
- [[_COMMUNITY_Community 62|Community 62]]
- [[_COMMUNITY_Community 71|Community 71]]
- [[_COMMUNITY_Community 72|Community 72]]
- [[_COMMUNITY_Community 73|Community 73]]
- [[_COMMUNITY_Community 74|Community 74]]
- [[_COMMUNITY_Community 75|Community 75]]
- [[_COMMUNITY_Community 76|Community 76]]
- [[_COMMUNITY_Community 77|Community 77]]
- [[_COMMUNITY_Community 78|Community 78]]
- [[_COMMUNITY_Community 79|Community 79]]

## God Nodes (most connected - your core abstractions)
1. `Game1` - 31 edges
2. `Ecosystem` - 30 edges
3. `SpawnPanel` - 22 edges
4. `Creature` - 18 edges
5. `InGameUi` - 18 edges
6. `UiWindowManager` - 18 edges
7. `CreatureRenderer` - 16 edges
8. `PixelWorldRenderer` - 16 edges
9. `FastNoiseLite` - 16 edges
10. `WorldGenerator` - 16 edges

## Surprising Connections (you probably didn't know these)
- `BaseBehavior` --implements--> `ICreatureBehavior`  [EXTRACTED]
  Simulation/Behaviors/BaseBehavior.cs → Simulation/Behaviors/ICreatureBehavior.cs
- `PlantBehavior` --implements--> `ICreatureBehavior`  [EXTRACTED]
  Simulation/Behaviors/PlantBehavior.cs → Simulation/Behaviors/ICreatureBehavior.cs

## Import Cycles
- None detected.

## Communities (80 total, 20 thin omitted)

### Community 0 - "Community 0"
Cohesion: 0.33
Nodes (5): Creature, Carnivore, Genome, Random, Vector2

### Community 1 - "Community 1"
Cohesion: 0.09
Nodes (15): byte, FastNoiseLite, IDisposable, NoiseType, bool, Camera, Color, float (+7 more)

### Community 2 - "Community 2"
Cohesion: 0.09
Nodes (14): CreatureSpawner, Herbivore, Plant, Creature, Func, GameTime, int, List (+6 more)

### Community 3 - "Community 3"
Cohesion: 0.07
Nodes (27): CreatureRenderer, Game, bool, Camera, Color, Creature, DayNightCycle, DayPhase (+19 more)

### Community 4 - "Community 4"
Cohesion: 0.14
Nodes (15): BaseBehavior, ICreatureBehavior, PlantBehavior, Creature, Ecosystem, GameTime, World, Creature (+7 more)

### Community 5 - "Community 5"
Cohesion: 0.17
Nodes (13): Color, Creature, KeyboardState, Keys, MouseState, Rectangle, SpriteBatch, SpriteFont (+5 more)

### Community 6 - "Community 6"
Cohesion: 0.13
Nodes (9): Dictionary, int, List, MouseState, Rectangle, SpriteBatch, SpriteFont, Texture2D (+1 more)

### Community 7 - "Community 7"
Cohesion: 0.29
Nodes (20): install.sh script, build_from_source(), check_go(), create_beads_alias(), detect_platform(), download_file(), install_from_release(), install_with_go() (+12 more)

### Community 8 - "Community 8"
Cohesion: 0.16
Nodes (12): BiomeType, Camera, Color, CreatureType, Ecosystem, GraphicsDevice, int, Rectangle (+4 more)

### Community 9 - "Community 9"
Cohesion: 0.17
Nodes (8): Creature, Ecosystem, float, GameTime, Genome, Random, Vector2, World

### Community 10 - "Community 10"
Cohesion: 0.12
Nodes (15): b, l, r, BiomeType, Camera, Color, GraphicsDevice, IEnumerable (+7 more)

### Community 11 - "Community 11"
Cohesion: 0.14
Nodes (12): GenderedSpeciesAsset, HashSet, CreatureRenderer, Camera, Color, Dictionary, Ecosystem, GraphicsDevice (+4 more)

### Community 12 - "Community 12"
Cohesion: 0.13
Nodes (10): BuiltinSpecies, BiomeType, CreatureType, Dictionary, Genome, IEnumerable, Random, SpeciesDefinition (+2 more)

### Community 13 - "Community 13"
Cohesion: 0.14
Nodes (13): MenuAction, bool, int, KeyboardState, Keys, MouseState, SpriteBatch, SpriteFont (+5 more)

### Community 14 - "Community 14"
Cohesion: 0.22
Nodes (10): Creature, Dictionary, Func, IEnumerable, int, List, Vector2, SpatialGrid (+2 more)

### Community 15 - "Community 15"
Cohesion: 0.11
Nodes (18): Convenzioni, File da modificare, File NON toccati, File structure map, Nuovi file, Piano d'implementazione — Sistema sociale (Genere, Branco/Solitario, Cucciolo/Adulto), Rischio e mitigazione, Scope check (+10 more)

### Community 16 - "Community 16"
Cohesion: 0.11
Nodes (18): Come Giocare, Consigli, Controlli, Crediti, Da Codice Sorgente, Installazione, Interfaccia, Licenza (+10 more)

### Community 17 - "Community 17"
Cohesion: 0.22
Nodes (5): BiomeType, float, Random, World, WorldGenerator

### Community 18 - "Community 18"
Cohesion: 0.16
Nodes (10): bool, KeyboardState, Keys, MouseState, SpriteBatch, SpriteFont, Texture2D, UiButton (+2 more)

### Community 19 - "Community 19"
Cohesion: 0.21
Nodes (8): c, d, e, SimulationControllerTests, DayNightCycle, Ecosystem, Fact, SimulationController

### Community 20 - "Community 20"
Cohesion: 0.12
Nodes (16): Aquatic Species Expansion Implementation Plan, Execution Handoff, File Structure, Self-Review Checklist (run before declaring plan complete), Task 10: Build verification, Task 11: Manual test in game, Task 12: Commit, Task 1: Create 4 PixelLab sprite jobs in parallel (+8 more)

### Community 21 - "Community 21"
Cohesion: 0.18
Nodes (8): DateTime, List, MouseState, Point, Rectangle, string, UiWindow, UiWindowManager

### Community 23 - "Community 23"
Cohesion: 0.14
Nodes (9): double, bool, KeyboardState, Keys, MouseState, SpriteBatch, SpriteFont, Texture2D (+1 more)

### Community 24 - "Community 24"
Cohesion: 0.14
Nodes (13): 1. Visual Theme & Atmosphere, 2. Color Palette & Roles, 3. Typography Rules, 4. Component Stylings, 5. Layout Principles, 6. Depth & Elevation, 7. Do's and Don'ts, 8. Responsive Behavior (+5 more)

### Community 25 - "Community 25"
Cohesion: 0.20
Nodes (5): IReadOnlyCollection, IReadOnlyDictionary, CreatureType, Dictionary, I18n

### Community 27 - "Community 27"
Cohesion: 0.23
Nodes (5): int, List, object, string, Logger

### Community 28 - "Community 28"
Cohesion: 0.29
Nodes (5): SpawnTests, BiomeType, Ecosystem, Fact, Vector2

### Community 29 - "Community 29"
Cohesion: 0.15
Nodes (12): Aquatic Species Expansion - Design Spec, Architecture, Data Flow, Edge Cases, Files to create (assets), Files to modify, Goal, Out of Scope (+4 more)

### Community 30 - "Community 30"
Cohesion: 0.20
Nodes (6): bool, DayNightCycle, Ecosystem, float, int, SimulationController

### Community 31 - "Community 31"
Cohesion: 0.36
Nodes (3): BalanceTests, Fact, GameTime

### Community 32 - "Community 32"
Cohesion: 0.33
Nodes (5): CreatureSpawnerTests, BiomeType, Ecosystem, Fact, Vector2

### Community 34 - "Community 34"
Cohesion: 0.22
Nodes (7): coverlet.collector (6.0.2), Microsoft.NET.Test.Sdk (17.12.0), System.Drawing.Common (8.0.0), xunit (2.9.2), xunit.runner.visualstudio (2.8.2), net9.0, Microsoft.NET.Sdk

### Community 36 - "Community 36"
Cohesion: 0.33
Nodes (4): Color, DayPhase, float, DayNightCycle

### Community 37 - "Community 37"
Cohesion: 0.36
Nodes (4): CreatureSpawner, Ecosystem, Vector2, SpeciesDefinition

### Community 38 - "Community 38"
Cohesion: 0.22
Nodes (8): Age Stages, Behavior, Dependencies, Files Affected, Gender, Overview, Pack vs Solitary, Social & Lifecycle System Design

### Community 39 - "Community 39"
Cohesion: 0.33
Nodes (5): MouseState, SpriteBatch, SpriteFont, Texture2D, UiButton

### Community 40 - "Community 40"
Cohesion: 0.25
Nodes (5): InlineData, TileTests, BiomeType, Fact, Theory

### Community 41 - "Community 41"
Cohesion: 0.36
Nodes (4): ITestOutputHelper, MainMenuTests, Fact, MainMenu

### Community 42 - "Community 42"
Cohesion: 0.36
Nodes (3): Camera, int, Vector2

### Community 43 - "Community 43"
Cohesion: 0.43
Nodes (5): Color, Rectangle, SpriteBatch, Texture2D, UiPrimitives

### Community 49 - "Community 49"
Cohesion: 0.33
Nodes (5): Accessibility, Brand, PitLife Design Context, Product, Reference Sites

### Community 51 - "Community 51"
Cohesion: 0.22
Nodes (6): int, Point, SpriteBatch, SpriteFont, Texture2D, UiWindow

### Community 52 - "Community 52"
Cohesion: 0.40
Nodes (4): MonoGame.Content.Builder.Task (3.8.*), MonoGame.Framework.DesktopGL (3.8.*), net9.0, Microsoft.NET.Sdk

### Community 56 - "Community 56"
Cohesion: 0.80
Nodes (4): Genome, Mutate(), Random(), Reproduce()

### Community 59 - "Community 59"
Cohesion: 0.50
Nodes (3): IReadOnlyList, AssetRegistry, string

### Community 71 - "Community 71"
Cohesion: 0.25
Nodes (6): JsonSerializerOptions, CreatureSaveData, Ecosystem, GenomeSaveData, SaveData, SaveSystem

### Community 74 - "Community 74"
Cohesion: 0.21
Nodes (6): AdaptationTests, TestCreature, Fact, Genome, Random, Vector2

### Community 75 - "Community 75"
Cohesion: 0.33
Nodes (4): TestCreature, Genome, Random, Vector2

### Community 76 - "Community 76"
Cohesion: 0.43
Nodes (4): SpatialGridTests, TestCreature, CreatureType, Fact

### Community 77 - "Community 77"
Cohesion: 0.29
Nodes (4): Genome, Random, Vector2, Plant

### Community 78 - "Community 78"
Cohesion: 0.33
Nodes (4): Genome, Random, Vector2, Herbivore

### Community 79 - "Community 79"
Cohesion: 0.33
Nodes (4): Genome, Random, Vector2, Omnivore

## Knowledge Gaps
- **277 isolated node(s):** `string`, `object`, `List`, `int`, `GraphicsDeviceManager` (+272 more)
  These have ≤1 connection - possible missing edges or undocumented components.
- **20 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **What connects `string`, `object`, `List` to the rest of the system?**
  _277 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `Community 1` be split into smaller, more focused modules?**
  _Cohesion score 0.08907563025210084 - nodes in this community are weakly interconnected._
- **Should `Community 2` be split into smaller, more focused modules?**
  _Cohesion score 0.09103840682788052 - nodes in this community are weakly interconnected._
- **Should `Community 3` be split into smaller, more focused modules?**
  _Cohesion score 0.06984126984126984 - nodes in this community are weakly interconnected._
- **Should `Community 4` be split into smaller, more focused modules?**
  _Cohesion score 0.13793103448275862 - nodes in this community are weakly interconnected._
- **Should `Community 6` be split into smaller, more focused modules?**
  _Cohesion score 0.13230769230769232 - nodes in this community are weakly interconnected._
- **Should `Community 10` be split into smaller, more focused modules?**
  _Cohesion score 0.1225296442687747 - nodes in this community are weakly interconnected._