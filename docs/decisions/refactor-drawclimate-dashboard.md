# DrawClimateDashboard Refactor Brainstorming

## Context
The `DrawClimateDashboard` method in `UI/InGameUi.cs` had become excessively long and complex, handling multiple different responsibilities within a single monolithic method block.

## Decision
Extracted the logic into four distinct, self-contained private helper methods:
- `DrawClimateGlobalData`: Responsible for rendering seasonal, progress, and global temperature metrics.
- `DrawClimateLocalData`: Responsible for rendering tile-specific climate state (local temperature, wind, extreme events).
- `DrawClimatePopulationData`: Responsible for drawing sparklines and progress bars related to the populations of species.
- `DrawClimateEventsData`: Responsible for parsing and rendering the most recent climate logger events.

## Alternatives Considered
- Keep the monolithic block: Avoids breaking code but worsens readability.
- Move logic to a separate view class: Unnecessary architectural shift given that InGameUi is already the primary controller.

## Consequences
- Better code organization and adherence to the single-responsibility principle.
- Negligible performance overhead.
