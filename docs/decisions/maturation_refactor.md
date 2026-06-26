# Extract Maturation Ages to Config

## Context
The maturation ages of species are currently hardcoded in a switch statement in `BuiltinSpecies.cs`. The goal is to move these values to a JSON configuration file to support data-driven design, allowing easier tweaking and modifications without recompiling the source code.

## Decision
- Create `Content/config/maturation.json` containing the dictionary of ages and a default age.
- Create `Core/MaturationConfig.cs` following the pattern of `BalanceConfig.cs` to parse and load the JSON on initialization, including safe fallback defaults.
- Refactor `BuiltinSpecies.cs` to access `MaturationConfig.Data` instead of hardcoding values in `GetMaturityAge`.

## Consequences
- The values are decoupled from the code.
- Adds one new config class but strictly aligns with the ongoing data-driven design refactoring initiative.
