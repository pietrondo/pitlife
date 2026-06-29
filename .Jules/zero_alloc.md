## Zero-Allocation UI Drawing

**Learning:** 
In MonoGame, passing string interpolation directly to `SpriteBatch.DrawString` or other methods creates multiple string allocations per frame. Even using `.ToString()` allocates. Calling `Mouse.GetState()` and `Keyboard.GetState()` multiple times throughout a frame is redundant and inefficient.

**Action:**
1. Replaced multiple calls to `Keyboard.GetState()` and `Mouse.GetState()` with a single cached call at the beginning of `Update()` and `Draw()`, assigning to fields `_currentKbd` and `_currentMouse` that are passed down.
2. Replaced string interpolations inside `DrawHUD` and `DrawDebugOverlay` with `_sb.Append()` (using a `StringBuilder`). We also eliminated `.ToString().ToLowerInvariant()` calls on Enums by utilizing `switch` expressions that map directly to constant strings.
3. Removed LINQ `.Where()` usage in core loops (like `OnSpeciesCatalogChanged`), replacing it with standard `foreach` enumeration into a `List`.
