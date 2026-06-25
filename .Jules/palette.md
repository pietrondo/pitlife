## 2024-05-24 - Disabled States and Shortcut Hints
**Learning:** MonoGame lacks native HTML-like properties (ARIA, `disabled` attributes), so UX state (disabled) and context (shortcut hints) must be manually rendered within `Draw` loops using color interpolation and scaled `SpriteBatch.DrawString` calls.
**Action:** When adding UX context in MonoGame, manually manage state (e.g., `IsDisabled`) to block underlying interaction logic (`IsHovered`, `WasClicked`) and explicitly render the visual cues (grayed out text, small hint text) in the UI component's `Draw` method.
