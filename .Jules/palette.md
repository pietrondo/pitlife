## 2024-05-24 - Disabled States and Shortcut Hints
**Learning:** MonoGame lacks native HTML-like properties (ARIA, `disabled` attributes), so UX state (disabled) and context (shortcut hints) must be manually rendered within `Draw` loops using color interpolation and scaled `SpriteBatch.DrawString` calls.
**Action:** When adding UX context in MonoGame, manually manage state (e.g., `IsDisabled`) to block underlying interaction logic (`IsHovered`, `WasClicked`) and explicitly render the visual cues (grayed out text, small hint text) in the UI component's `Draw` method.
## 2024-11-19 - Add visual collapse affordance to windows
**Learning:** Hidden double-click interactions (like window collapsing) require explicit visual affordances to be discoverable.
**Action:** Always add visual indicators (like +/- or arrows) for toggleable states, even if the primary interaction is a double-click.
## 2026-06-27 - Title Bar Collapse Indicator Hover State
**Learning:** Hidden interactions in UI elements (like double-clicking a title bar to collapse the window) need explicit visual affordances to be discoverable. Adding hover states to existing indicator icons bridges the gap between static text and interactive elements.
**Action:** Always add subtle color transitions or hover states to visual indicators linked to hidden interactions.

## 2024-06-28 - Missing Shortcut Hints
**Learning:** Some primary UI buttons lacked shortcut hints, forcing users to guess keyboard controls for navigation (like ESC) and time manipulation (like UP/DWN for speed controls).
**Action:** Consistently use the `ShortcutHint` property on `UiButton` for all actions that have a keyboard equivalent to improve discoverability.

## 2024-11-20 - Contextual Button Shortcut Hints vs Global Text Hints
**Learning:** While global text hints at the bottom of the screen (e.g., "ENTER: select ESC: back") are useful, users might not immediately associate them with specific actions in deep menus like options or world generation panels.
**Action:** Always add inline `ShortcutHint` properties directly on the specific `UiButton` elements corresponding to the actions (like "Back" or "Generate") to clearly map the keyboard shortcut to the specific interaction right where the user's focus is.
## 2024-07-06 - Button Text Hover Affordances
**Learning:** In MonoGame UIs without standard HTML hover states, users rely entirely on color changes to understand interactivity. If a button's background changes but the text color remains static, the visual affordance feels incomplete.
**Action:** When implementing hover states for custom MonoGame UI components, ensure that both the background elements and the foreground text/icons react dynamically to the `isHovered` state (e.g., brightening the text color to white) to provide a complete and satisfying visual cue.
## 2025-02-12 - Added Titlebar Hover Feedback
**Learning:** In MonoGame/PitLife's immediate-mode UI, elements that are interactive but lack traditional button shapes (like draggable window titlebars) require explicit hover feedback (e.g., color tinting from `DeepGrove` to `ForestNight`) to afford interactivity. Re-using the hover boolean for both background shape and foreground icons ensures complete visual affordance.
**Action:** Always verify that interactive custom UI elements (like titlebars or headers) provide immediate visual feedback via `UiTheme` color shifts when hovered, matching the interaction pattern established for standard buttons.
## 2026-07-20 - Custom Toggle Button UX Parity
**Learning:** In PitLife's UI framework, custom interactive elements (like the toggle buttons in `SpawnPanel` and `CataclysmPanel`) do not inherit from `UiButton`. Consequently, standard UX features such as visual hover states (changing foreground text/icon color to white) and keyboard shortcut hints must be manually implemented in their custom drawing routines to maintain UX parity across the application.
**Action:** When working with custom UI panels, always manually add `hover` boolean checks to text color interpolation and render explicit `hintText` below the buttons to match the established `UiButton` accessibility patterns.
