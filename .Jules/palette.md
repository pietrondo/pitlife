## 2024-05-24 - Disabled States and Shortcut Hints
**Learning:** MonoGame lacks native HTML-like properties (ARIA, `disabled` attributes), so UX state (disabled) and context (shortcut hints) must be manually rendered within `Draw` loops using color interpolation and scaled `SpriteBatch.DrawString` calls.
**Action:** When adding UX context in MonoGame, manually manage state (e.g., `IsDisabled`) to block underlying interaction logic (`IsHovered`, `WasClicked`) and explicitly render the visual cues (grayed out text, small hint text) in the UI component's `Draw` method.
## 2024-11-19 - Add visual collapse affordance to windows
**Learning:** Hidden double-click interactions (like window collapsing) require explicit visual affordances to be discoverable.
**Action:** Always add visual indicators (like +/- or arrows) for toggleable states, even if the primary interaction is a double-click.
## 2026-06-27 - Title Bar Collapse Indicator Hover State
**Learning:** Hidden interactions in UI elements (like double-clicking a title bar to collapse the window) need explicit visual affordances to be discoverable. Adding hover states to existing indicator icons bridges the gap between static text and interactive elements.
**Action:** Always add subtle color transitions or hover states to visual indicators linked to hidden interactions.
