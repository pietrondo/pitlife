# Decision Record: Extract BuiltinSpecies

## Context
The `Simulation/Entities/BuiltinSpecies.cs` file contains a very long `RegisterAll` method and many private static helper methods for registering different species of plants and animals. This makes the class too large and difficult to maintain.

## Decision
We decided to extract the registration logic into smaller, more focused helper classes:
- `BuiltinPlants`: Handles registration of all plant species.
- `BuiltinAnimals`: Handles registration of all animal species.

The `BuiltinSpecies` class will retain the shared biome array constants and act as the main entry point by calling `BuiltinPlants.RegisterAll()` and `BuiltinAnimals.RegisterAll()`.

## Consequences
- Improved readability and maintainability of the registration logic.
- Preserved existing functionality and architectural boundaries.
- No god classes: files are kept under the 250 LOC limit.
