# Social & Lifecycle System Design

## Overview

Add gender, pack/solitary behavior, and age stages (baby/adult) to the creature simulation. These features deepen the ecological simulation by introducing realistic social dynamics and lifecycles.

## Gender

- `enum Gender { Male, Female }` — new file `Simulation/Gender.cs`
- Every `Creature` gets a `Gender` property, randomly assigned at birth
- Reproduction (`ReproduceWith`) requires opposite genders (previously any two creatures)
- Visual indicator: render a small ♂ red / ♀ blue icon below the creature

## Pack vs Solitary

Defined as static species sets in `Ecosystem.cs`:

**Pack animals (social):** Deer, Sheep, Horse, Goat, Rabbit, Wolf, Lion, Fish, Salmon, Gazelle  
**Solitary (territorial):** Tiger, Leopard, Bear, Crocodile, Snake, Frog, Lizard, Beetle, Butterfly, Eagle, Fox, Lynx, Shark, Piranha, Jellyfish, Turtle, Tortoise

### Behavior
- **Pack animals**: `MoveToward` nearest same-species creature if one is within vision range and they're not already near a group. Gain a small energy bonus when near ≥2 same-species creatures.
- **Solitary animals**: `MoveAwayFrom` nearest same-species creature if within a threshold distance (half vision range). Lose energy faster when crowded.

Implemented as a new phase in each subclass's `Update()` or a shared method in `Creature.cs`.

## Age Stages

- `bool IsAdult => Age >= 30f` (in `Creature.cs`)
- **Babies** (Age < 30s):
  - Cannot reproduce
  - Render at 60% of genome size visually
  - Slightly lower speed (multiply by 0.7)
  - Stay closer to parent/herd
- **Adults** (Age ≥ 30s):
  - Full reproduction capability
  - Full size and speed

## Files Affected

| File | Change |
|------|--------|
| `Simulation/Gender.cs` (new) | Gender enum |
| `Simulation/Creature.cs` | Gender, IsAdult, social behavior hooks |
| `Simulation/Ecosystem.cs` | PackAnimal species set, SolitarySpecies set, M/F reproduction |
| `Simulation/Herbivore.cs` | Add social behavior in Update |
| `Simulation/Carnivore.cs` | Add social behavior in Update |
| `Simulation/Omnivore.cs` | Add social behavior in Update |
| `Rendering/CreatureRenderer.cs` | Baby render size, gender icon |
| `Localization/I18n.cs` | Gender labels |
| `UI/InGameUi.cs` | Show gender, age stage |

## Dependencies

- Previous changes (Butterfly size, Turtle aquatic, Tortoise) already committed
