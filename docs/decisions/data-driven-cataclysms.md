# Decision Record: Data-Driven Cataclysms

## Context
As part of the ongoing code health and data-driven design refactoring, the hardcoded values audit (`docs/hardcoded-values-audit.md`) identified that `CataclysmSystem.cs` contains numerous hardcoded parameters for events, including durations, multipliers, radii, and chain reaction probabilities.

## Decision
We extracted the configuration into a new JSON file `Content/config/cataclysms.json` and a corresponding POCO `Core/CataclysmConfig.cs`. `CataclysmSystem.cs` now reads from these dynamic models instead of hardcoded switch blocks, facilitating easier balancing and modding.

## Consequences
- Better modularity.
- Balancers can edit `cataclysms.json` without recompiling code.
- Reduces technical debt and cyclomatic complexity of `CataclysmSystem.cs`.
