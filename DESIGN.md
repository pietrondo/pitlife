# PitLife Design System

## 1. Visual Theme & Atmosphere

A naturalist pixel-art interface layered over the living ecosystem. Solid, dark timber-like panels keep text readable while moss and water accents connect controls to the simulated world. Decoration stays restrained so creatures remain the visual focus.

## 2. Color Palette & Roles

- Forest Night (#14251D) - primary window surface.
- Deep Grove (#0B1712) - overlay and deepest surface.
- Moss Signal (#78C850) - primary action and keyboard focus.
- Lake Blue (#4E9CB5) - secondary action and option state.
- Warm Parchment (#F2E6C9) - primary text.
- Bark Edge (#6B5137) - panel and button borders.
- Muted Stone (#A9B5A7) - hints and secondary text.
- Danger Clay (#C85A4A) - destructive actions.

## 3. Typography Rules

| Role | Font | Size | Weight | Line Height | Letter Spacing | Features | Notes |
|---|---|---|---|---|---|---|---|
| Window title | MonoGame SpriteFont / Arial | 16px | 700 | 1.2 | 0 | Uppercase | Centered |
| Button | MonoGame SpriteFont / Arial | 14px | 700 | 1.2 | 0 | Uppercase | Minimum 44px target |
| Body | MonoGame SpriteFont / Arial | 12px | 400 | 1.5 | 0 | None | Warm parchment on dark surfaces |
| Hint | MonoGame SpriteFont / Arial | 12px | 400 | 1.5 | 0 | None | Muted stone only |

Use the bundled SpriteFont until a pixel font with complete Italian glyph coverage is introduced.

## 4. Component Stylings

- Buttons: 52px high, 4px corner treatment, 2px Bark Edge border. Moss Signal focus outline. Pressed state shifts content by 2px.
- Windows: Forest Night surface, 3px Bark Edge outer border, Deep Grove title bar, 24px inner padding.
- Modal overlay: Deep Grove at approximately 80% opacity; modal interaction blocks the layer below.
- Inputs and toggles: same height and focus treatment as buttons; state is always expressed with text as well as color.
- Navigation: Up/Down changes focus, Enter activates, Escape closes or returns.

## 5. Layout Principles

- Base spacing unit: 8px; supported steps are 8, 16, 24, 32, 48, and 64px.
- Menus remain centered and use at most 400px width.
- Screen edges retain at least 16px clearance.
- Radius scale: 0px for pixel assets, 4px for controls, 8px for large windows.

## 6. Depth & Elevation

- Layer 0: simulated world.
- Layer 10: HUD and persistent controls.
- Layer 20: menu scrim.
- Layer 30: windows.
- Layer 40: modal windows and focused control outlines.
- Depth uses solid borders and offset shadows, never blur or glassmorphism.

## 7. Do's and Don'ts

- DO: Keep panels solid enough for reliable text contrast.
- DON'T: Use purple gradients, glass effects, or soft SaaS-style cards.
- DO: Use Moss Signal for focus and primary actions.
- DON'T: communicate selection using color alone; retain borders and text.
- DO: preserve pixel edges with PointClamp rendering.
- DON'T: animate more than the ecosystem background and one transition at a time.

## 8. Responsive Behavior

- Minimum supported viewport: 640x480.
- Window width is clamped to viewport width minus 32px.
- Logo scales from 96px on short viewports to 160px on larger viewports.
- Buttons remain at least 44px high and stack vertically.
- Text hints may shorten before primary controls shrink.

## 9. Agent Prompt Guide

### Quick Color Reference

- Primary action/focus: Moss Signal (#78C850)
- Secondary action: Lake Blue (#4E9CB5)
- Window: Forest Night (#14251D)
- Overlay: Deep Grove (#0B1712)
- Primary text: Warm Parchment (#F2E6C9)
- Border: Bark Edge (#6B5137)
- Muted text: Muted Stone (#A9B5A7)
- Destructive: Danger Clay (#C85A4A)

### Example Component Prompts

- "Build a MonoGame menu button 52px high with #14251D fill, 2px #6B5137 border, #F2E6C9 centered text, and a 3px #78C850 keyboard-focus outline."
- "Build a MonoGame modal window with #14251D fill, 3px #6B5137 border, #0B1712 title bar, 24px padding, and keyboard Escape support."
- "Build a HUD panel using #0B1712 at 80% opacity, #F2E6C9 primary text, and #A9B5A7 hint text."
- "Build an options toggle that expresses state in its Italian label and uses #4E9CB5 only as a supporting state color."

### Iteration Guide

1. Use only the named palette colors for UI surfaces and states.
2. Keep interactive targets at least 44px high.
3. Every mouse action must have a keyboard equivalent.
4. Escape always provides a safe way back.
5. Use PointClamp and integer rectangles for pixel-sharp rendering.
6. Keep one modal layer at a time and block interaction below it.
7. Prefer solid panels and offset borders over gradients, blur, or glow.
