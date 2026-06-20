# Aquatic Species Expansion Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add 4 new aquatic species (Shark, Piranha, Salmon, Jellyfish) to the PitLife ecosystem, following the exact same pattern as the 5 species already shipped in commit `ee0f498`.

**Architecture:** Reuse existing pattern. New sprites via PixelLab, file-based textures (no Content Pipeline), `IsAquatic` virtual property override per subclass, single-species arrays in `Ecosystem.cs`, `AquaticSpecies` HashSet whitelist, `typeof(T).Name` switch in `SpawnSpecies<T>`. NO new abstractions, NO refactoring of base classes.

**Tech Stack:** MonoGame 3.8 DesktopGL (.NET 9), PixelLab MCP, C# 12, git, PowerShell 7+.

**Reference spec:** `docs/superpowers/specs/2026-06-18-aquatic-species-design.md`

---

## File Structure

**New files:**
- `Content/assets/creatures/fish/shark.png` — sprite 64x64
- `Content/assets/creatures/fish/piranha.png` — sprite 64x64
- `Content/assets/creatures/fish/salmon.png` — sprite 64x64
- `Content/assets/creatures/fish/jellyfish.png` — sprite 64x64
- `Content/assets/creatures/fish/index.md` — species registry (file does NOT exist yet, must be created from scratch)

**Modified files:**
- `Simulation/Herbivore.cs:10` — extend `IsAquatic` override
- `Simulation/Carnivore.cs` — add `IsAquatic` override (currently absent)
- `Simulation/Omnivore.cs` — add `IsAquatic` override (currently absent)
- `Simulation/Ecosystem.cs:25-30` — add 4 species to arrays, extend `AquaticSpecies` HashSet
- `Simulation/Ecosystem.cs` `SpawnSpecies<T>` — add 4 switch cases
- `Game1.cs` (after line 114) — register 4 new textures

**Project test infrastructure:** None. Project is a MonoGame game loop with no xUnit/MSTest. Verification is `dotnet build` + manual game launch.

---

## Task 1: Create 4 PixelLab sprite jobs in parallel

**Files:** No file changes. Only PixelLab API calls.

- [ ] **Step 1: Verify no leftover aquatic species exist in PixelLab**

```bash
bd create "Generate 4 aquatic species sprites via PixelLab" -d "Shark, Piranha, Salmon, Jellyfish - 64px low top-down" -t task -p 1 --json
```

Expected: JSON with new issue id. Note it for later `close`.

- [ ] **Step 2: Launch 4 PixelLab jobs in parallel**

Call `pixellab_create_1_direction_object` 4 times in a single message (rate limit allows 8 concurrent):

```
create_1_direction_object(description="top-down pixel art great white shark, gray back, white belly, sharp teeth, menacing", size=64, view="low top-down")
create_1_direction_object(description="top-down pixel art piranha, silver body with red-orange belly, small fins, sharp teeth", size=64, view="low top-down")
create_1_direction_object(description="top-down pixel art salmon, silver-blue body with pinkish hue, streamlined, dotted back", size=64, view="low top-down")
create_1_direction_object(description="top-down pixel art jellyfish, translucent dome bell, long flowing tentacles, soft blue-pink", size=64, view="low top-down")
```

Expected: 4 object_ids returned immediately. Save them.

- [ ] **Step 3: Poll each job to confirm completion**

For each object_id, call `pixellab_get_object(object_id=...)` until `status: "completed"`. For 64px objects, expect 4 candidate frames in `review` status. Single-candidate keep is automatic.

- [ ] **Step 4: If review status, select best frame**

For each object in `review` status, call `pixellab_get_object` and inspect candidates. Call `pixellab_select_object_frames(object_id=..., indices=[<best_index>])` to finalize. Otherwise note the frame URL from completed status.

- [ ] **Step 5: Record the 4 object_ids and their rotation/frame image URLs**

Expected: 4 URLs of form `https://backblaze.pixellab.ai/.../<objectId>/.../south.png` (or frame 0 for 1-direction).

---

## Task 2: Download 4 sprites to Content/assets/creatures/fish/

**Files:** Create 4 new png files.

- [ ] **Step 1: Verify target directory exists**

```powershell
Test-Path "Content/assets/creatures/fish"
```

Expected: `True`. (Already contains `fish.png`.)

- [ ] **Step 2: Download each sprite via Invoke-WebRequest**

For each of the 4 image URLs from Task 1:

```powershell
Invoke-WebRequest -Uri "<image_url>" -OutFile "Content/assets/creatures/fish/<name>.png"
```

Repeat for: `shark.png`, `piranha.png`, `salmon.png`, `jellyfish.png`.

- [ ] **Step 3: Verify file sizes are non-trivial**

```powershell
Get-ChildItem "Content/assets/creatures/fish" -File | Select-Object Name, Length
```

Expected: 4 new files, each 1000+ bytes (valid PNG).

- [ ] **Step 4: Inspect each sprite visually**

For each new file, call `describe_image_describe_image` with image=absolute path and prompt: "Describe this pixel art sprite briefly. Is it clearly the intended creature (shark/piranha/salmon/jellyfish)?"

Expected: Each description matches the intended species. If a sprite is wrong, delete the file and re-generate with a tweaked description in PixelLab.

---

## Task 3: Create fish/index.md registry

**Files:** Create `Content/assets/creatures/fish/index.md` (does NOT exist yet).

- [ ] **Step 1: Verify file does not exist**

```bash
ls Content/assets/creatures/fish/index.md
```

Expected: file not found.

- [ ] **Step 2: Write registry file**

Write to `Content/assets/creatures/fish/index.md`:

```markdown
# Fish (aquatic species)

All species in this folder can only live in water biomes (`DeepOcean`, `ShallowWater`).

| Species | File | Diet | Size | Biomes |
|---------|------|------|------|--------|
| Fish | fish.png | Herbivore | 1.0x | ShallowWater |
| Shark | shark.png | Carnivore | 1.2x | DeepOcean |
| Piranha | piranha.png | Carnivore | 0.7x | ShallowWater |
| Salmon | salmon.png | Herbivore | 0.9x | ShallowWater |
| Jellyfish | jellyfish.png | Omnivore | 0.6x | ShallowWater, DeepOcean |
```

Expected: file created, ~500 bytes.

- [ ] **Step 3: Verify file written**

```bash
Get-Content "Content/assets/creatures/fish/index.md"
```

Expected: registry content shown.

---

## Task 4: Extend Herbivore.IsAquatic override

**Files:** Modify `Simulation/Herbivore.cs:10`.

- [ ] **Step 1: Read current file**

```bash
Read "Simulation/Herbivore.cs"
```

Confirm line 10 currently reads: `public override bool IsAquatic => Species == "Fish";`

- [ ] **Step 2: Edit line 10**

In `Simulation/Herbivore.cs`, replace line 10:

```csharp
public override bool IsAquatic => Species == "Fish" || Species == "Salmon";
```

- [ ] **Step 3: Verify edit**

```bash
Read "Simulation/Herbivore.cs" -Limit 15
```

Expected: line 10 now matches new content.

---

## Task 5: Add IsAquatic override in Carnivore

**Files:** Modify `Simulation/Carnivore.cs` (add new line after line 11, inside class).

- [ ] **Step 1: Read current file**

```bash
Read "Simulation/Carnivore.cs"
```

Note: `Carnivore` currently has NO `IsAquatic` override. Default in `Creature` is `false`.

- [ ] **Step 2: Add override after the constructor**

In `Simulation/Carnivore.cs`, immediately after the constructor (after line 11), insert:

```csharp
    public override bool IsAquatic => Species == "Shark" || Species == "Piranha";
```

Preserve existing 4-space indentation. The line should be a class member at the same level as `AttackDamage`.

- [ ] **Step 3: Verify edit**

```bash
Read "Simulation/Carnivore.cs" -Limit 20
```

Expected: new `IsAquatic` property visible at expected location.

---

## Task 6: Add IsAquatic override in Omnivore

**Files:** Modify `Simulation/Omnivore.cs` (add new line after line 11, inside class).

- [ ] **Step 1: Read current file**

```bash
Read "Simulation/Omnivore.cs" -Limit 15
```

- [ ] **Step 2: Add override after the constructor**

In `Simulation/Omnivore.cs`, immediately after the constructor (after line 11), insert:

```csharp
    public override bool IsAquatic => Species == "Jellyfish";
```

Preserve existing 4-space indentation.

- [ ] **Step 3: Verify edit**

```bash
Read "Simulation/Omnivore.cs" -Limit 20
```

Expected: new `IsAquatic` property visible.

---

## Task 7: Extend species arrays in Ecosystem

**Files:** Modify `Simulation/Ecosystem.cs:25-27` (three arrays) and line 30 (HashSet).

- [ ] **Step 1: Read current arrays**

```bash
Read "Simulation/Ecosystem.cs" -Limit 35 -Offset 20
```

Confirm current arrays:
- Line 25-27: `HerbivoreSpecies`, `CarnivoreSpecies`, `OmnivoreSpecies`
- Line 30: `AquaticSpecies` HashSet

- [ ] **Step 2: Extend HerbivoreSpecies array**

In `Simulation/Ecosystem.cs`, append `"Salmon"` to the `HerbivoreSpecies` array. If array uses collection initializer:

```csharp
private static readonly string[] HerbivoreSpecies = new[]
{
    "Rabbit", "Deer", "Sheep", "Horse", "Goat", "Fish", "Lizard", "Turtle", "Salmon"
};
```

Match existing format (one per line or comma-separated, whichever is current).

- [ ] **Step 3: Extend CarnivoreSpecies array**

In `Simulation/Ecosystem.cs`, append `"Shark"` and `"Piranha"` to `CarnivoreSpecies`:

```csharp
private static readonly string[] CarnivoreSpecies = new[]
{
    "Fox", "Lynx", "Tiger", "Lion", "Leopard", "Crocodile", "Snake", "Eagle", "Wolf", "Shark", "Piranha"
};
```

- [ ] **Step 4: Extend OmnivoreSpecies array**

In `Simulation/Ecosystem.cs`, append `"Jellyfish"` to `OmnivoreSpecies`:

```csharp
private static readonly string[] OmnivoreSpecies = new[]
{
    "Boar", "Raccoon", "Frog", "Beetle", "Butterfly", "Bear", "Jellyfish"
};
```

- [ ] **Step 5: Extend AquaticSpecies HashSet**

In `Simulation/Ecosystem.cs`, modify the HashSet at line 30:

```csharp
private static readonly HashSet<string> AquaticSpecies = new() { "Fish", "Shark", "Piranha", "Salmon", "Jellyfish" };
```

- [ ] **Step 6: Verify all four edits**

```bash
Read "Simulation/Ecosystem.cs" -Limit 40
```

Expected: all 4 changes visible in output.

---

## Task 8: Extend SpawnSpecies switch

**Files:** Modify `Simulation/Ecosystem.cs` method `SpawnSpecies<T>` (add 4 cases to existing switch).

- [ ] **Step 1: Locate switch statement**

```bash
grep -n "SpawnSpecies" "Simulation/Ecosystem.cs"
```

Read the `SpawnSpecies<T>` method to find the switch on `typeof(T).Name`.

- [ ] **Step 2: Add 4 new cases**

In the switch on `typeof(T).Name`, add these cases (maintain alphabetical/logical order matching existing style):

```csharp
case "Shark": genome = new Genome { Size = 1.2f, /* ... existing Genome defaults ... */ }; targetBiomes = new[] { BiomeType.DeepOcean }; break;
case "Piranha": genome = new Genome { Size = 0.7f, /* ... existing Genome defaults ... */ }; targetBiomes = new[] { BiomeType.ShallowWater }; break;
case "Salmon": genome = new Genome { Size = 0.9f, /* ... existing Genome defaults ... */ }; targetBiomes = new[] { BiomeType.ShallowWater }; break;
case "Jellyfish": genome = new Genome { Size = 0.6f, /* ... existing Genome defaults ... */ }; targetBiomes = new[] { BiomeType.ShallowWater, BiomeType.DeepOcean }; break;
```

NOTE: Inspect existing cases first (e.g., `case "Fish"`) to copy the exact Genome default values, color, and pattern. Do not invent defaults — copy from existing aquatic cases.

- [ ] **Step 3: Verify edit**

```bash
Read "Simulation/Ecosystem.cs"
```

Read the full method. Confirm 4 new cases match existing pattern.

---

## Task 9: Register 4 new textures in Game1.cs

**Files:** Modify `Game1.cs` (after line 114, in `LoadContent`).

- [ ] **Step 1: Locate texture registration block**

```bash
Read "Game1.cs" -Limit 130 -Offset 95
```

Find the section where existing creature textures are registered (Fish, Lizard, Wolf, Bear, Turtle).

- [ ] **Step 2: Add 4 texture field declarations**

In `Game1.cs` class field declarations (around line 20, where existing texture fields live), add:

```csharp
private Texture2D? _shark;
private Texture2D? _piranha;
private Texture2D? _salmon;
private Texture2D? _jellyfish;
```

Match existing private field naming convention.

- [ ] **Step 3: Add 4 texture load statements**

In `LoadContent` after the existing creature texture registrations, add:

```csharp
_shark = Texture2D.FromFile(GraphicsDevice, "Content/assets/creatures/fish/shark.png");
_piranha = Texture2D.FromFile(GraphicsDevice, "Content/assets/creatures/fish/piranha.png");
_salmon = Texture2D.FromFile(GraphicsDevice, "Content/assets/creatures/fish/salmon.png");
_jellyfish = Texture2D.FromFile(GraphicsDevice, "Content/assets/creatures/fish/jellyfish.png");
```

- [ ] **Step 4: Verify edits**

```bash
Read "Game1.cs" -Limit 30
Read "Game1.cs" -Limit 140 -Offset 95
```

Expected: 4 fields and 4 load statements visible.

---

## Task 10: Build verification

**Files:** None modified. Validation step.

- [ ] **Step 1: Run filtered build**

```powershell
dotnet build 2>&1 | Select-String -Pattern "error|Build succeeded|Build FAILED|warning CS"
```

Expected: ONLY `Build succeeded` line, no `error` and no `warning CS`. If any errors/warnings appear, fix them before proceeding.

- [ ] **Step 2: Confirm zero warnings explicitly**

```powershell
dotnet build 2>&1 | Select-String -Pattern "Avviso|warning"
```

Expected: no matches. (Italian: `Avviso` = Warning. Localized message.)

---

## Task 11: Manual test in game

**Files:** None modified. Manual verification.

- [ ] **Step 1: Launch game**

```powershell
dotnet run
```

Expected: window opens, ecosystem renders.

- [ ] **Step 2: Visual verification**

Wait ~10 seconds for spawn. Confirm visually:
- 5 distinct fish-like sprites visible in water (blue biomes)
- Sharks only in deep ocean (darkest blue)
- Piranhas + Salmon in shallow water (lighter blue)
- Jellyfish distributed across both water biomes

- [ ] **Step 3: Verify no crash, no log spam**

Press Ctrl+C to stop. Confirm clean shutdown (no unhandled exception traceback).

- [ ] **Step 4: If behavior wrong, debug**

Common failures:
- All new species spawn on land → IsAquatic override missing/wrong
- Sharks spawn in shallow water → BiomeType assignment wrong in SpawnSpecies
- Jellyfish missing from deep ocean → BiomeType array wrong

Re-check Tasks 4-8.

---

## Task 12: Commit

**Files:** All changes.

- [ ] **Step 1: Stage all changes**

```bash
git add -A
```

- [ ] **Step 2: Verify staged files**

```bash
git status --short
```

Expected: only new png files, new index.md, modified .cs files. NO `.beads/issues.jsonl` (auto-generated, should not be staged). If present, `git reset HEAD .beads/issues.jsonl`.

- [ ] **Step 3: Commit with conventional message**

```bash
git commit -m "Add 4 aquatic species (Shark, Piranha, Salmon, Jellyfish)"
```

Expected: commit hash returned.

- [ ] **Step 4: Close PixelLab task in beads**

```bash
bd close <pixelab_task_id> --reason "4 aquatic sprites generated and downloaded" --json
```

Use the issue id from Task 1 Step 1.

- [ ] **Step 5: Push to remote**

```bash
git push
```

Expected: `git status` shows "up to date with origin".

- [ ] **Step 6: Sync beads**

```bash
bd dolt push
```

Expected: synced (or help message if no remote).

---

## Self-Review Checklist (run before declaring plan complete)

- [ ] All 4 species from spec are covered: Shark, Piranha, Salmon, Jellyfish
- [ ] Shark: Carnivore, DeepOcean, 1.2x
- [ ] Piranha: Carnivore, ShallowWater, 0.7x
- [ ] Salmon: Herbivore, ShallowWater, 0.9x
- [ ] Jellyfish: Omnivore, ShallowWater+DeepOcean, 0.6x
- [ ] All 4 species added to correct species array in Ecosystem.cs
- [ ] All 4 species added to AquaticSpecies HashSet
- [ ] Herbivore.IsAquatic extended with Salmon
- [ ] Carnivore.IsAquatic override added (Shark, Piranha)
- [ ] Omnivore.IsAquatic override added (Jellyfish)
- [ ] SpawnSpecies switch has 4 new cases
- [ ] 4 texture files exist in Content/assets/creatures/fish/
- [ ] 4 textures registered in Game1.cs
- [ ] fish/index.md registry created and lists all 5 species
- [ ] No code refactor of base classes (scope out)
- [ ] No new abstractions introduced
- [ ] All steps have exact file paths and code
- [ ] No placeholders ("TBD", "TODO", "similar to")

---

## Execution Handoff

Plan complete and saved to `docs/superpowers/plans/2026-06-18-aquatic-species.md`. Two execution options:

1. **Subagent-Driven (recommended)** — I dispatch a fresh subagent per task, review between tasks, fast iteration
2. **Inline Execution** — Execute tasks in this session using executing-plans, batch execution with checkpoints

Which approach?
