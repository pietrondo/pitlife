## 2024-05-19 - Memory Optimization in Render Loops
**Learning:** `SpriteBatch.DrawString` in MonoGame has an overload taking `StringBuilder` to avoid string allocations, but custom localization wrappers like `I18n.Format` use `params object[]` and `string.Format`, forcing boxing and string allocations.
**Action:** When performing zero-allocation optimizations, bypass or refactor custom wrappers like `I18n.Format` that implicitly allocate, and use pre-allocated StringBuilders passed directly to MonoGame's rendering API.

## 2024-05-19 - Culling Off-screen Rendering in Arrays
**Learning:** Looping through arrays (like fruits) without checking visibility wastes GPU fill rate and incurs hundreds of unnecessary `SpriteBatch.Draw()` calls for off-screen entities.
**Action:** Always implement pure mathematical camera bounds culling (`visibleArea.X`, `visibleArea.Right`, etc.) before calling `.Draw()` on entities loaded from arrays or pools.

## 2024-05-19 - Texture Swapping in Grid Rendering
**Learning:** Interleaving different `Texture2D` instances (like a biome base sprite and a 1x1 pixel texture for borders) inside a single spatial loop destroys `SpriteBatch` batching when using `SpriteSortMode.Deferred`. This causes MonoGame to issue potentially thousands of GPU texture swaps per frame, causing massive CPU overhead in the rendering thread.
**Action:** When drawing layered grids, always group `SpriteBatch.Draw` calls by texture across multiple passes (e.g., Pass 1: Base textures, Pass 2: Borders) to drastically reduce state changes and improve GPU utilization.
