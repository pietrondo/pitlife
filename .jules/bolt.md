## 2024-05-19 - Memory Optimization in Render Loops
**Learning:** `SpriteBatch.DrawString` in MonoGame has an overload taking `StringBuilder` to avoid string allocations, but custom localization wrappers like `I18n.Format` use `params object[]` and `string.Format`, forcing boxing and string allocations.
**Action:** When performing zero-allocation optimizations, bypass or refactor custom wrappers like `I18n.Format` that implicitly allocate, and use pre-allocated StringBuilders passed directly to MonoGame's rendering API.

## 2024-05-19 - Culling Off-screen Rendering in Arrays
**Learning:** Looping through arrays (like fruits) without checking visibility wastes GPU fill rate and incurs hundreds of unnecessary `SpriteBatch.Draw()` calls for off-screen entities.
**Action:** Always implement pure mathematical camera bounds culling (`visibleArea.X`, `visibleArea.Right`, etc.) before calling `.Draw()` on entities loaded from arrays or pools.

## 2024-05-19 - Texture Swapping in Grid Rendering
**Learning:** Interleaving different `Texture2D` instances (like a biome base sprite and a 1x1 pixel texture for borders) inside a single spatial loop destroys `SpriteBatch` batching when using `SpriteSortMode.Deferred`. This causes MonoGame to issue potentially thousands of GPU texture swaps per frame, causing massive CPU overhead in the rendering thread.
**Action:** When drawing layered grids, always group `SpriteBatch.Draw` calls by texture across multiple passes (e.g., Pass 1: Base textures, Pass 2: Borders) to drastically reduce state changes and improve GPU utilization.

## 2024-05-19 - Culling Full-Screen Overlays
**Learning:** Drawing full-world sized rectangles (`Rectangle(0, 0, World.PixelWidth, World.PixelHeight)`) when a camera transform is active destroys GPU fill rate because MonoGame attempts to rasterize massive areas far outside the screen bounds.
**Action:** Always intersect transparent overlays (like season tints or temperature blends) with `Camera.VisibleArea` instead of the full world size.
## 2026-07-06 - Terrain Generation (Biome Placement) Optimization
**Learning:** In C#, repeated checks of `HashSet<T>.Contains(item)` within a hot, nested spatial loop (e.g., world array iteration) incur significant unnecessary overhead due to repeated hashing and memory lookups. If the only purpose of the set check is to avoid duplicate placements and short-circuit the loop once an element is placed, introducing a simple boolean flag (`bool placed = false`) allows for immediate loop termination via `break` or `!placed` in the continuation condition. This achieves O(1) loop exit speed, entirely bypassing the HashSet.
**Action:** Replaced `!present.Contains(biome)` in the `y` and `x` loop conditions of `TerrainRefiner.EnsureAllBiomesPresent` with a `!placed` flag, drastically reducing HashSet overhead during fallback biome placement.
