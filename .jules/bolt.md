## 2024-05-19 - Memory Optimization in Render Loops
**Learning:** `SpriteBatch.DrawString` in MonoGame has an overload taking `StringBuilder` to avoid string allocations, but custom localization wrappers like `I18n.Format` use `params object[]` and `string.Format`, forcing boxing and string allocations.
**Action:** When performing zero-allocation optimizations, bypass or refactor custom wrappers like `I18n.Format` that implicitly allocate, and use pre-allocated StringBuilders passed directly to MonoGame's rendering API.
