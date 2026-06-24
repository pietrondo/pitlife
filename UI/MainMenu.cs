using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI;

public enum MenuAction
{
    None,
    StartGame,
    NewWorld,
    NewWorldWithSeed,
    ToggleFullscreen,
    ShowHelp,
    Exit,
    SaveGame,
    LoadGame
}

public sealed class MainMenu
{
    private readonly UiWindow _window = new(I18n.T("menu.mainTitle"));
    private readonly UiButton[] _mainButtons =
    [
        new(I18n.T("menu.start")),
        new(I18n.T("menu.newWorld")),
        new(I18n.T("menu.saveGame")),
        new(I18n.T("menu.loadGame")),
        new(I18n.T("menu.options")),
        new(I18n.T("menu.help")),
        new(I18n.T("menu.exit")) { IsDestructive = true }
    ];
    private readonly UiButton[] _optionButtons =
    [
        new(I18n.Format("menu.fullscreen", I18n.T("common.no"))),
        new("EN / IT"),
        new(I18n.T("common.back"))
    ];
    private readonly UiButton[] _worldGenButtons =
    [
        new(I18n.T("menu.generate"))
    ];

    private readonly UiTextInput _seedInput = new();
    private readonly UiTextInput _continentInput = new();
    private readonly UiTextInput _seaLevelInput = new();
    private readonly UiButton _presetButton = new("");
    private readonly UiButton _islandSizeButton = new("");
    private readonly UiButton _mapSizeButton = new("");
    private readonly UiButton _planetButton = new("");

    private bool _showOptions;
    private bool _showWorldGenPanel;
    private bool _inputReady;
    private int _focusedIndex;
    private int _presetIndex;
    private int _planetIndex;
    private int _islandSizeIndex;
    private int _mapSizeIndex = 2;
    public bool GameInProgress { get; set; }

    private static readonly (string Label, int Width, int Height)[] MapSizes =
    [
        ("96\u00d772", 96, 72),
        ("200\u00d7150", 200, 150),
        ("400\u00d7300", 400, 300),
        ("800\u00d7600", 800, 600),
    ];

    public int? Seed => _seedInput.Text.Length > 0 ? _seedInput.GetNumericValue() : null;
    public bool IsWorldGenPanelOpen => _showWorldGenPanel;

    public WorldGenOptions CurrentOptions => new(
        (WorldGenPreset)_presetIndex,
        ParseIntSafe(_continentInput.Text, 1),
        ParseIntSafe(_seaLevelInput.Text, 40) / 100f,
        (IslandSize)_islandSizeIndex,
        MapSizes[_mapSizeIndex].Width,
        MapSizes[_mapSizeIndex].Height)
    {
        PlanetRadiusKm = PlanetPresets[_planetIndex].RadiusKm,
        OrbitalAU = PlanetPresets[_planetIndex].OrbitalAU,
        Eccentricity = PlanetPresets[_planetIndex].Eccentricity
    };

    private static readonly (string Label, float RadiusKm, float OrbitalAU, float Eccentricity)[] PlanetPresets =
    [
        ("Earth-like", 6371f, 1.00f, 0.12f),
        ("Small Cold", 4200f, 1.40f, 0.08f),
        ("Large Hot", 9800f, 0.72f, 0.15f),
        ("Super-Earth", 12000f, 0.90f, 0.10f)
    ];

    private static int ParseIntSafe(string text, int fallback)
    {
        return int.TryParse(text, out int val) ? val : fallback;
    }

    public MenuAction Update(
        MouseState mouse,
        MouseState previousMouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int viewportWidth,
        int viewportHeight,
        bool isFullscreen)
    {
        Layout(viewportWidth, viewportHeight);
        RefreshText(isFullscreen);

        if (!IsInputReady(mouse, keyboard)) return MenuAction.None;
        if (HandleEscapeKey(keyboard, previousKeyboard)) return MenuAction.None;

        int prevFocused = _focusedIndex;
        HandleKeyboardNavigation(keyboard, previousKeyboard);
        HandleSeedInput(keyboard, previousKeyboard, mouse, previousMouse, prevFocused);
        UpdateHoverStates(mouse);

        if (_showOptions && Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            _showOptions = false;
            _focusedIndex = 4; // Focus Options button in main menu
            return MenuAction.None;
        }

        int activated = GetClickedButton(mouse, previousMouse);
        if (IsActivatePressed(keyboard, previousKeyboard))
            activated = _focusedIndex;

        if (_showWorldGenPanel)
        {
            return UpdateWorldGenPanel(mouse, previousMouse, keyboard, previousKeyboard);
        }

        if (activated < 0)
            return MenuAction.None;

        if (_showOptions)
        {
            return HandleOptionsActivation(activated, isFullscreen);
        }

        return HandleMainMenuActivation(activated);
    }

    private bool IsInputReady(MouseState mouse, KeyboardState keyboard)
    {
        if (!_inputReady)
        {
            _inputReady = keyboard.IsKeyUp(Keys.Enter) &&
                           keyboard.IsKeyUp(Keys.Space) &&
                           mouse.LeftButton == ButtonState.Released;
        }
        return _inputReady;
    }

    private bool HandleEscapeKey(KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        if (_showWorldGenPanel && Pressed(keyboard, previousKeyboard, Keys.Escape))
        {
            _showWorldGenPanel = false;
            return true;
        }
        return false;
    }

    private void HandleKeyboardNavigation(KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        int totalElements = _showOptions ? 3 : 8;

        // Keyboard navigation
        if (Pressed(keyboard, previousKeyboard, Keys.Up))
            _focusedIndex = (_focusedIndex - 1 + totalElements) % totalElements;
        if (Pressed(keyboard, previousKeyboard, Keys.Down))
            _focusedIndex = (_focusedIndex + 1) % totalElements;

        // Left/Right navigation for side-by-side buttons
        if (!_showOptions)
        {
            if (Pressed(keyboard, previousKeyboard, Keys.Right))
            {
                if (_focusedIndex == 3) _focusedIndex = 4; // Save -> Load
                else if (_focusedIndex == 5) _focusedIndex = 6; // Options -> Help
            }
            if (Pressed(keyboard, previousKeyboard, Keys.Left))
            {
                if (_focusedIndex == 4) _focusedIndex = 3; // Load -> Save
                else if (_focusedIndex == 6) _focusedIndex = 5; // Help -> Options
            }
        }
    }

    private void HandleSeedInput(KeyboardState keyboard, KeyboardState previousKeyboard, MouseState mouse, MouseState previousMouse, int prevFocused)
    {
        // Sync keyboard navigation to seed input focus
        if (!_showOptions && _focusedIndex != prevFocused)
        {
            if (_focusedIndex == 2)
                _seedInput.IsFocused = true;
            else
                _seedInput.IsFocused = false;
        }

        // Update seed input
        _seedInput.Update(keyboard, previousKeyboard, mouse, previousMouse);

        // Sync mouse click focus from seed input to _focusedIndex
        if (!_showOptions && _seedInput.IsFocused)
        {
            _focusedIndex = 2;
        }
    }

    private void UpdateHoverStates(MouseState mouse)
    {
        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].IsHovered(mouse))
            {
                _focusedIndex = _showOptions ? i : (i < 2 ? i : i + 1);
            }
        }
    }

    private int GetClickedButton(MouseState mouse, MouseState previousMouse)
    {
        int activated = -1;
        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].WasClicked(mouse, previousMouse))
            {
                activated = _showOptions ? i : (i < 2 ? i : i + 1);
            }
        }
        return activated;
    }

    private bool IsActivatePressed(KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        bool activatePressed = Pressed(keyboard, previousKeyboard, Keys.Enter);
        if (!(_seedInput.IsFocused && !_showOptions))
        {
            activatePressed = activatePressed || Pressed(keyboard, previousKeyboard, Keys.Space);
        }
        return activatePressed;
    }

    private MenuAction UpdateWorldGenPanel(MouseState mouse, MouseState previousMouse, KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        _continentInput.Update(keyboard, previousKeyboard, mouse, previousMouse);
        _seaLevelInput.Update(keyboard, previousKeyboard, mouse, previousMouse);

        if (_presetButton.WasClicked(mouse, previousMouse))
            CyclePreset();
        if (_islandSizeButton.WasClicked(mouse, previousMouse))
            CycleIslandSize();
        if (_mapSizeButton.WasClicked(mouse, previousMouse))
            CycleMapSize();
        if (_planetButton.WasClicked(mouse, previousMouse))
            CyclePlanet();

        if (_worldGenButtons[0].WasClicked(mouse, previousMouse))
        {
            return Seed.HasValue ? MenuAction.NewWorldWithSeed : MenuAction.NewWorld;
        }
        return MenuAction.None;
    }

    private MenuAction HandleOptionsActivation(int activated, bool isFullscreen)
    {
        if (activated == 0)
            return MenuAction.ToggleFullscreen;
        if (activated == 1)
        {
            I18n.SetLanguage(I18n.CurrentLanguage == "it" ? "en" : "it");
            Game1.SaveLanguagePref();
            RefreshText(isFullscreen);
            return MenuAction.None;
        }

        _showOptions = false;
        _focusedIndex = 4;
        return MenuAction.None;
    }

    private MenuAction HandleMainMenuActivation(int activated)
    {
        return activated switch
        {
            0 => MenuAction.StartGame,
            1 => OpenWorldGenPanel(),
            2 => OpenWorldGenPanel(),
            3 => MenuAction.SaveGame,
            4 => MenuAction.LoadGame,
            5 => OpenOptions(),
            6 => MenuAction.ShowHelp,
            7 => MenuAction.Exit,
            _ => MenuAction.None
        };
    }


    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Texture2D? logo,
        int viewportWidth,
        int viewportHeight)
    {
        Layout(viewportWidth, viewportHeight);
        UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(0, 0, viewportWidth, viewportHeight), UiTheme.MenuScrim);

        int logoSize = viewportHeight < 650 ? 96 : 144;
        int logoY = viewportHeight < 650 ? 16 : 28;
        if (logo != null)
        {
            var logoBounds = new Rectangle(viewportWidth / 2 - logoSize / 2, logoY, logoSize, logoSize);
            spriteBatch.Draw(logo, logoBounds, Color.White);
        }

        _window.Draw(spriteBatch, pixel, font, true, Mouse.GetState().Position);
        MouseState mouse = Mouse.GetState();

        if (_showWorldGenPanel)
        {
            int wgCenterX = viewportWidth / 2;

            _presetButton.Draw(spriteBatch, pixel, font, mouse, false);
            _mapSizeButton.Draw(spriteBatch, pixel, font, mouse, false);
            _planetButton.Draw(spriteBatch, pixel, font, mouse, false);

            int labelX = _continentInput.Bounds.X - 8;
            string contLabel = I18n.T("menu.continents") + ":";
            Vector2 contLabelSize = font.MeasureString(contLabel);
            spriteBatch.DrawString(font, contLabel, new Vector2(labelX - contLabelSize.X, _continentInput.Bounds.Y + 8), Color.White);
            _continentInput.Draw(spriteBatch, pixel, font, mouse);

            string seaLabel = I18n.T("menu.seaLevel") + ":";
            Vector2 seaLabelSize = font.MeasureString(seaLabel);
            spriteBatch.DrawString(font, seaLabel, new Vector2(labelX - seaLabelSize.X, _seaLevelInput.Bounds.Y + 8), Color.White);
            _seaLevelInput.Draw(spriteBatch, pixel, font, mouse);

            _islandSizeButton.Draw(spriteBatch, pixel, font, mouse, false);

            _worldGenButtons[0].Draw(spriteBatch, pixel, font, mouse, true);
            string wgHint = I18n.T("menu.worldGenHint");
            Vector2 wgHintSize = font.MeasureString(wgHint);
            spriteBatch.DrawString(font, wgHint, new Vector2(viewportWidth / 2f - wgHintSize.X / 2f, viewportHeight - 28), UiTheme.MutedStone);
            return;
        }

        UiButton[] buttons = _showOptions ? _optionButtons : _mainButtons;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool isFocused = _showOptions 
                ? (i == _focusedIndex) 
                : (i < 2 ? i == _focusedIndex : i + 1 == _focusedIndex);
            buttons[i].Draw(spriteBatch, pixel, font, mouse, isFocused);
        }

        // Draw seed input
        if (!_showOptions)
        {
            _seedInput.Draw(spriteBatch, pixel, font, mouse);
        }

        string hint = I18n.T(_showOptions ? "menu.optionsHint" : "menu.hint");
        Vector2 hintSize = font.MeasureString(hint);
        spriteBatch.DrawString(font, hint, new Vector2(viewportWidth / 2f - hintSize.X / 2f, viewportHeight - 28), UiTheme.MutedStone);
    }

    private MenuAction OpenOptions()
    {
        _showOptions = true;
        _focusedIndex = 0;
        return MenuAction.None;
    }

    private bool _worldGenInitialized;

    private MenuAction OpenWorldGenPanel()
    {
        if (!_worldGenInitialized)
        {
            var defaults = WorldGenOptions.Pangea() with { MapWidth = 400, MapHeight = 300 };
            _presetIndex = (int)defaults.Preset;
            _continentInput.SetText(defaults.ContinentCount.ToString());
            _continentInput.IsNumericOnly = true;
            _continentInput.MaxLength = 1;
            _seaLevelInput.SetText(((int)(defaults.SeaLevel * 100)).ToString());
            _seaLevelInput.IsNumericOnly = true;
            _seaLevelInput.MaxLength = 3;
            _islandSizeIndex = (int)defaults.IslandSize;
            _mapSizeIndex = Array.FindIndex(MapSizes, m => m.Width == defaults.MapWidth && m.Height == defaults.MapHeight);
            if (_mapSizeIndex < 0) _mapSizeIndex = 2;
            _worldGenInitialized = true;
        }
        _presetButton.Text = I18n.T("menu.preset") + ": " + ((WorldGenPreset)_presetIndex);
        _islandSizeButton.Text = I18n.T("menu.islandSize") + ": " + ((IslandSize)_islandSizeIndex);
        _mapSizeButton.Text = I18n.T("menu.mapSize") + ": " + MapSizes[_mapSizeIndex].Label;
        _planetButton.Text = I18n.T("menu.planet") + ": " + PlanetPresets[_planetIndex].Label;
        _showWorldGenPanel = true;
        return MenuAction.None;
    }

    private void ApplyPreset(WorldGenOptions preset)
    {
        _presetIndex = (int)preset.Preset;
        _continentInput.SetText(preset.ContinentCount.ToString());
        _seaLevelInput.SetText(((int)(preset.SeaLevel * 100)).ToString());
        _islandSizeIndex = (int)preset.IslandSize;
        _presetButton.Text = I18n.T("menu.preset") + ": " + preset.Preset;
        _islandSizeButton.Text = I18n.T("menu.islandSize") + ": " + preset.IslandSize;
    }

    private void CyclePreset()
    {
        var presets = new[] { WorldGenOptions.Pangea(), WorldGenOptions.Continents(), WorldGenOptions.Archipelago(), WorldGenOptions.WetWorld(), WorldGenOptions.DryWorld() };
        _presetIndex = (_presetIndex + 1) % presets.Length;
        ApplyPreset(presets[_presetIndex]);
    }

    private void CycleIslandSize()
    {
        _islandSizeIndex = (_islandSizeIndex + 1) % 3;
        _islandSizeButton.Text = I18n.T("menu.islandSize") + ": " + ((IslandSize)_islandSizeIndex);
    }

    private void CycleMapSize()
    {
        _mapSizeIndex = (_mapSizeIndex + 1) % MapSizes.Length;
        _mapSizeButton.Text = I18n.T("menu.mapSize") + ": " + MapSizes[_mapSizeIndex].Label;
    }

    private void CyclePlanet()
    {
        _planetIndex = (_planetIndex + 1) % PlanetPresets.Length;
        _planetButton.Text = I18n.T("menu.planet") + ": " + PlanetPresets[_planetIndex].Label;
    }

    public void CloseWorldGenPanel()
    {
        _showWorldGenPanel = false;
    }

    private Rectangle CalculateWindowBounds(int viewportWidth, int viewportHeight)
    {
        int panelWidth = Math.Min(400, viewportWidth - 32);
        int panelHeight = _showWorldGenPanel ? 580 : (_showOptions ? 260 : 436);
        int logoSize = viewportHeight < 650 ? 96 : 144;
        int logoY = viewportHeight < 650 ? 16 : 28;
        int panelY = Math.Max(logoY + logoSize + 12, (viewportHeight - panelHeight) / 2 + 48);
        panelY = Math.Min(panelY, viewportHeight - panelHeight - 48);
        return new Rectangle(viewportWidth / 2 - panelWidth / 2, panelY, panelWidth, panelHeight);
    }

    private string GetWindowTitle()
    {
        return I18n.T(_showWorldGenPanel ? "menu.worldGenTitle" : _showOptions ? "menu.optionsTitle" : "menu.mainTitle");
    }

    private void Layout(int viewportWidth, int viewportHeight)
    {
        _window.Bounds = CalculateWindowBounds(viewportWidth, viewportHeight);
        _window.Title = GetWindowTitle();

        int panelWidth = _window.Bounds.Width;

        if (_showWorldGenPanel)
        {
            LayoutWorldGenPanel(viewportWidth, panelWidth);
            return;
        }

        int buttonHeight = 52;
        int gap = 12;
        int startY = _window.Bounds.Y + 60;

        if (_showOptions)
        {
            LayoutOptionsPanel(viewportWidth, panelWidth, startY, buttonHeight, gap);
        }
        else
        {
            LayoutMainPanel(viewportWidth, panelWidth, startY, buttonHeight, gap);
        }
    }

    private void LayoutWorldGenPanel(int viewportWidth, int panelWidth)
    {
        int wgButtonWidth = panelWidth - 48;
        int wgButtonHeight = 46;
        int wgGap = 8;
        int wgStartY = _window.Bounds.Y + 56;
        int wgCenterX = viewportWidth / 2;

        LayoutWorldGenTopButtons(wgCenterX, wgButtonWidth, wgButtonHeight, wgGap, wgStartY);
        LayoutWorldGenInputs(wgCenterX, wgButtonWidth, wgButtonHeight, wgGap, wgStartY);
        LayoutWorldGenBottomButtons(wgCenterX, wgButtonWidth, wgButtonHeight, wgGap, wgStartY);
    }

    private void LayoutWorldGenTopButtons(int wgCenterX, int wgButtonWidth, int wgButtonHeight, int wgGap, int wgStartY)
    {
        _presetButton.Bounds = new Rectangle(wgCenterX - wgButtonWidth / 2, wgStartY, wgButtonWidth, wgButtonHeight);
        _mapSizeButton.Bounds = new Rectangle(wgCenterX - wgButtonWidth / 2, wgStartY + (wgButtonHeight + wgGap), wgButtonWidth, wgButtonHeight);
        _planetButton.Bounds = new Rectangle(wgCenterX - wgButtonWidth / 2, wgStartY + 2 * (wgButtonHeight + wgGap), wgButtonWidth, wgButtonHeight);
    }

    private void LayoutWorldGenInputs(int wgCenterX, int wgButtonWidth, int wgButtonHeight, int wgGap, int wgStartY)
    {
        int wgInputWidth = 80;
        int wgInputHeight = 40;
        int labelWidth = (wgButtonWidth - wgInputWidth - 8) / 2 + wgInputWidth + 8;

        _continentInput.Bounds = new Rectangle(wgCenterX + labelWidth / 2 - wgInputWidth, wgStartY + 3 * (wgButtonHeight + wgGap) + 2, wgInputWidth, wgInputHeight);
        _seaLevelInput.Bounds = new Rectangle(wgCenterX + labelWidth / 2 - wgInputWidth, wgStartY + 4 * (wgButtonHeight + wgGap) + 2, wgInputWidth, wgInputHeight);
    }

    private void LayoutWorldGenBottomButtons(int wgCenterX, int wgButtonWidth, int wgButtonHeight, int wgGap, int wgStartY)
    {
        _islandSizeButton.Bounds = new Rectangle(wgCenterX - wgButtonWidth / 2, wgStartY + 5 * (wgButtonHeight + wgGap), wgButtonWidth, wgButtonHeight);

        int genY = wgStartY + 6 * (wgButtonHeight + wgGap) + wgGap;
        _worldGenButtons[0].Bounds = new Rectangle(wgCenterX - wgButtonWidth / 2, genY, wgButtonWidth, wgButtonHeight);
    }

    private void LayoutOptionsPanel(int viewportWidth, int panelWidth, int startY, int buttonHeight, int gap)
    {
        int buttonWidth = panelWidth - 48;
        for (int i = 0; i < _optionButtons.Length; i++)
        {
            _optionButtons[i].Bounds = new Rectangle(
                viewportWidth / 2 - buttonWidth / 2,
                startY + i * (buttonHeight + gap),
                buttonWidth,
                buttonHeight);
        }
    }

    private void LayoutMainPanel(int viewportWidth, int panelWidth, int startY, int buttonHeight, int gap)
    {
        int buttonWidth = panelWidth - 48;
        int halfWidth = (buttonWidth - gap) / 2;
        int centerX = viewportWidth / 2;

        int inputY = startY + 2 * (buttonHeight + gap);
        int saveLoadY = inputY + 40 + gap;
        int optionsHelpY = saveLoadY + buttonHeight + gap;
        int exitY = optionsHelpY + buttonHeight + gap;

        LayoutPrimaryButtons(centerX, buttonWidth, startY, buttonHeight, gap);
        LayoutSeedInput(centerX, buttonWidth, inputY);
        LayoutSecondaryButtons(halfWidth, saveLoadY, optionsHelpY, buttonHeight, gap);
        LayoutExitButton(centerX, buttonWidth, exitY, buttonHeight);
    }

    private void LayoutPrimaryButtons(int centerX, int buttonWidth, int startY, int buttonHeight, int gap)
    {
        _mainButtons[0].Bounds = new Rectangle(centerX - buttonWidth / 2, startY, buttonWidth, buttonHeight);
        _mainButtons[1].Bounds = new Rectangle(centerX - buttonWidth / 2, startY + (buttonHeight + gap), buttonWidth, buttonHeight);
    }

    private void LayoutSeedInput(int centerX, int buttonWidth, int inputY)
    {
        _seedInput.Placeholder = I18n.T("menu.seedPlaceholder");
        _seedInput.IsNumericOnly = true;
        _seedInput.MaxLength = 10;
        _seedInput.Bounds = new Rectangle(centerX - buttonWidth / 2, inputY, buttonWidth, 40);
    }

    private void LayoutSecondaryButtons(int halfWidth, int saveLoadY, int optionsHelpY, int buttonHeight, int gap)
    {
        int startX = _window.Bounds.X + 24;

        // Save & Load
        _mainButtons[2].Bounds = new Rectangle(startX, saveLoadY, halfWidth, buttonHeight);
        _mainButtons[3].Bounds = new Rectangle(startX + halfWidth + gap, saveLoadY, halfWidth, buttonHeight);

        // Options & Help
        _mainButtons[4].Bounds = new Rectangle(startX, optionsHelpY, halfWidth, buttonHeight);
        _mainButtons[5].Bounds = new Rectangle(startX + halfWidth + gap, optionsHelpY, halfWidth, buttonHeight);
    }

    private void LayoutExitButton(int centerX, int buttonWidth, int exitY, int buttonHeight)
    {
        _mainButtons[6].Bounds = new Rectangle(centerX - buttonWidth / 2, exitY, buttonWidth, buttonHeight);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);

    private void RefreshText(bool isFullscreen)
    {
        _mainButtons[0].Text = GameInProgress ? I18n.T("menu.continue") : I18n.T("menu.start");
        _mainButtons[1].Text = I18n.T("menu.newWorld");
        _mainButtons[2].Text = I18n.T("menu.saveGame");
        _mainButtons[3].Text = I18n.T("menu.loadGame");
        _mainButtons[4].Text = I18n.T("menu.options");
        _mainButtons[5].Text = I18n.T("menu.help");
        _mainButtons[6].Text = I18n.T("menu.exit");
        _optionButtons[0].Text = I18n.Format("menu.fullscreen", I18n.T(isFullscreen ? "common.yes" : "common.no"));
        _optionButtons[1].Text = I18n.T("common.back");
    }
}
