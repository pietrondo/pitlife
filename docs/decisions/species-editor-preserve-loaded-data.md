# Preserve loaded species in the editor

Beads issue: `PitLife-mj53`.

## Approved design

Loading and saving currently rebuilds a catalog entry from the visible controls, losing size, maturity, pollination and custom biome membership. The service correctly replaces the complete entry; the editor must supply complete data.

Keep the loaded entry as a baseline, retaining unexposed attributes and biome order. Apply biome presets only after a biome click; derive reproduction defaults only after changing reproduction or crossing the plant/animal boundary. Cloning retains attributes while changing identity. Clear drops the baseline and retains the existing fresh-draft defaults. No new controls or service behavior changes.

Reconstructing every field from controls was rejected because some fields have no control. Adding controls for every property would expand scope and still risk lossy round trips.

## Implementation plan

Use C# / MonoGame and xUnit. Add `tests/PitLife.Tests/SpeciesEditorPanelTests.cs` covering unchanged save, a single text edit, clone, explicit biome/reproduction changes and clear. Exercise the real panel update path with button clicks and compare complete serialized entries. Run `dotnet test PitLife.sln --filter FullyQualifiedName~SpeciesEditorPanelTests` before implementation and confirm data-loss assertion failures.

Update `UI/SpeciesEditorPanel.cs` to retain the loaded baseline and an explicit biome-edit flag. Preserve loaded size/maturity and pollination where applicable, and copy biome lists to avoid aliasing. Reset baseline on clear. Run focused tests, `dotnet test PitLife.sln`, `dotnet build PitLife.sln`, and `graphify update .`. Parent agent owns issue tracking, independent review and commit/push.
