# Refactor MainMenu.Update()

## Problem
The `Update` method in `UI/MainMenu.cs` was extremely long and handled too many responsibilities (input readiness check, keyboard navigation, options navigation, seed input focus/update, hover states, click handling, and world gen panel state). This violated the single responsibility principle and made the code hard to read and maintain.

## Explored Alternatives
- **Keep as-is**: Not a viable long-term solution.
- **Architectural overhaul**: Create separate classes or state machines for each menu view (Main Menu, Options, World Gen). This was deemed overkill since the problem specifically required improving readability without requiring architectural changes.

## Chosen Approach
We broke down the large `Update` method into smaller, sharply focused private helper functions while preserving the exact original logic execution order:
- `IsInputReady`
- `HandleEscapeKey`
- `HandleKeyboardNavigation`
- `HandleSeedInput`
- `UpdateHoverStates`
- `GetClickedButton`
- `IsActivatePressed`
- `UpdateWorldGenPanel`
- `HandleOptionsActivation`
- `HandleMainMenuActivation`

This cleanly segregates concerns within the `MainMenu` class, achieving better readability and maintainability without changing external behavior or class structure.

## Impacted Files
- `UI/MainMenu.cs`
