using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Simulation;
using PitLife.UI.Windows;
using System.Linq;

namespace PitLife.UI;

public struct PopSnapshot
{
    public int Plants, Herbivores, Carnivores, Omnivores;
    public PopSnapshot(int p, int h, int c, int o) { Plants = p; Herbivores = h; Carnivores = c; Omnivores = o; }
}

public sealed class InGameUi
{
    public const string StatisticsWindowId = "statistics";
    public const string CreatureWindowId = "creature";
    public const string TerrainWindowId = "terrain";
    public const string CataclysmWindowId = "cataclysm";
    public const string ClimateWindowId = "climate";
    public const string HistoryWindowId = "history";

    public string? SelectedCataclysm
    {
        get => _cataclysmWindow.SelectedCataclysm;
        set => _cataclysmWindow.SelectedCataclysm = value;
    }

    private readonly StringBuilder _speedSb = new StringBuilder(16);

    private Rectangle _toolbarRect;

    private readonly UiWindowManager _windowManager = new();
    private readonly UiButton _statisticsButton = new(I18n.T("toolbar.statistics")) { ShortcutHint = "F2" };
    private readonly UiButton _creatureButton = new(I18n.T("toolbar.creature")) { ShortcutHint = "F3" };
    private readonly UiButton _arrangeButton = new(I18n.T("toolbar.arrange")) { ShortcutHint = "F5" };
    private readonly UiButton _menuButton = new(I18n.T("toolbar.menu")) { ShortcutHint = "ESC" };
    private readonly UiButton _cataclysmButton = new(I18n.T("toolbar.cataclysm")) { ShortcutHint = "F8" };
    private readonly UiButton _climateButton = new(I18n.T("toolbar.climate")) { ShortcutHint = "F9" };
    private readonly UiButton _historyButton = new(I18n.T("toolbar.history")) { ShortcutHint = "F10" };
    private readonly UiButton _speedDownButton = new("<") { ShortcutHint = "DWN" };
    private readonly UiButton _speedUpButton = new(">") { ShortcutHint = "UP" };

    public bool WantsToGoToMainMenu { get; set; } = false;
    public bool SpeedUpRequested { get; set; }
    public bool SpeedDownRequested { get; set; }
    public World? World { get; set; }
    public Simulation.ClimateSystem? Climate { get; set; }
    public Point? SelectedTile { get; set; }
    public Point? HoverTile { get; set; }
    public event Action? ToolbarButtonClicked;

    private readonly PopSnapshot[] _popHistory = new PopSnapshot[60];
    private int _popHistoryCount;
    private float _popRecordTimer;
    private const float PopRecordInterval = 10f;
    private readonly float[] _tempHistory = new float[60];
    private int _tempHistoryCount;

    private readonly StatisticsWindow _statisticsWindow;
    private readonly CreatureDetailsWindow _creatureWindow;
    private readonly TerrainWindow _terrainWindow;
    private readonly CataclysmWindow _cataclysmWindow;
    private readonly ClimateWindow _climateWindow;
    private readonly HistoryWindow _historyWindow;

    public InGameUi()
    {
        _statisticsWindow = new StatisticsWindow(I18n.T("window.statistics"), StatisticsWindowId)
        {
            Bounds = new Rectangle(32, 88, 300, 380),
            IsOpen = true,
            ShowCloseButton = true
        };
        _windowManager.Add(_statisticsWindow);

        _creatureWindow = new CreatureDetailsWindow(I18n.T("window.creature"), CreatureWindowId)
        {
            Bounds = new Rectangle(376, 112, 384, 410),
            ShowCloseButton = true
        };
        _windowManager.Add(_creatureWindow);

        _terrainWindow = new TerrainWindow(I18n.T("window.terrain"), TerrainWindowId)
        {
            Bounds = new Rectangle(32, 352, 320, 180),
            ShowCloseButton = true
        };
        _windowManager.Add(_terrainWindow);

        _cataclysmWindow = new CataclysmWindow(I18n.T("window.cataclysm"), CataclysmWindowId)
        {
            Bounds = new Rectangle(370, 88, 220, 200),
            ShowCloseButton = true
        };
        _windowManager.Add(_cataclysmWindow);

        _climateWindow = new ClimateWindow(I18n.T("window.climate"), ClimateWindowId)
        {
            Bounds = new Rectangle(348, 96, 300, 420),
            ShowCloseButton = true
        };
        _windowManager.Add(_climateWindow);

        _historyWindow = new HistoryWindow(I18n.T("window.history"), HistoryWindowId)
        {
            Bounds = new Rectangle(200, 200, 400, 300),
            ShowCloseButton = true,
        };
        _windowManager.Add(_historyWindow);
    }

    public void OpenCreatureWindow(int vpw, int vph) => _windowManager.Open(CreatureWindowId, vpw, vph);
    public void OpenTerrainWindow(int vpw, int vph) => _windowManager.Open(TerrainWindowId, vpw, vph);

    public void ResetForWorld(World world)
    {
        World = world;
        SelectedTile = null;
        SelectedCataclysm = null;
        WantsToGoToMainMenu = false;
        SpeedUpRequested = false;
        SpeedDownRequested = false;
    }

    public bool CloseTopWindow() => _windowManager.CloseTopWindow();
    public void CloseAllWindows() => _windowManager.CloseAllWindows();

    public void RecordPopSnapshot(int plants, int herbivores, int carnivores, int omnivores, float dt)
    {
        _popRecordTimer += dt;
        if (_popRecordTimer < PopRecordInterval) return;
        _popRecordTimer = 0;
        if (_popHistoryCount < _popHistory.Length)
        {
            _popHistory[_popHistoryCount++] = new PopSnapshot(plants, herbivores, carnivores, omnivores);
        }
        else
        {
            Array.Copy(_popHistory, 1, _popHistory, 0, _popHistory.Length - 1);
            _popHistory[_popHistory.Length - 1] = new PopSnapshot(plants, herbivores, carnivores, omnivores);
        }
        var temp = Climate?.TemperatureModifier * 20f + 20f ?? 20f;
        if (_tempHistoryCount < _tempHistory.Length)
            _tempHistory[_tempHistoryCount++] = temp;
        else
        {
            Array.Copy(_tempHistory, 1, _tempHistory, 0, _tempHistory.Length - 1);
            _tempHistory[_tempHistory.Length - 1] = temp;
        }
    }

    public bool Update(
        MouseState mouse,
        MouseState previousMouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int viewportWidth,
        int viewportHeight)
    {
        RefreshText();
        LayoutToolbar(viewportHeight);

        HandleKeyboardShortcuts(keyboard, previousKeyboard, viewportWidth, viewportHeight);

        var toolbarConsumed = false;
        if (_statisticsButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(StatisticsWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
            ToolbarButtonClicked?.Invoke();
        }
        
        if (_historyWindow.IsOpen && !_historyWindow.IsCollapsed)
        {
            _historyWindow.HandleFilterClick(mouse, previousMouse);
        }

        if (_creatureButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(CreatureWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
            ToolbarButtonClicked?.Invoke();
        }
        if (_arrangeButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.TileWindows(viewportWidth, viewportHeight);
            toolbarConsumed = true;
        }
        if (_menuButton.WasClicked(mouse, previousMouse))
        {
            WantsToGoToMainMenu = true;
            toolbarConsumed = true;
        }
        if (_cataclysmButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(CataclysmWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
            ToolbarButtonClicked?.Invoke();
        }
        if (_historyButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(HistoryWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
            ToolbarButtonClicked?.Invoke();
        }
        if (_climateButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(ClimateWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
            ToolbarButtonClicked?.Invoke();
        }
        if (_speedDownButton.WasClicked(mouse, previousMouse))
        {
            SpeedDownRequested = true;
            toolbarConsumed = true;
        }
        if (_speedUpButton.WasClicked(mouse, previousMouse))
        {
            SpeedUpRequested = true;
            toolbarConsumed = true;
        }

        if (_cataclysmWindow.IsOpen && !_cataclysmWindow.IsCollapsed)
        {
            _cataclysmWindow.HandleCataclysmClick(mouse, previousMouse);
        }

        var overToolbar = _toolbarRect.Contains(mouse.Position);
        return toolbarConsumed || overToolbar || _windowManager.Update(mouse, previousMouse, viewportWidth, viewportHeight);
    }

    private void HandleKeyboardShortcuts(KeyboardState keyboard, KeyboardState previousKeyboard, int viewportWidth, int viewportHeight)
    {
        if (Pressed(keyboard, previousKeyboard, Keys.F2))
        {
            _windowManager.Toggle(StatisticsWindowId, viewportWidth, viewportHeight);
            ToolbarButtonClicked?.Invoke();
        }
        if (Pressed(keyboard, previousKeyboard, Keys.F3))
        {
            _windowManager.Toggle(CreatureWindowId, viewportWidth, viewportHeight);
            ToolbarButtonClicked?.Invoke();
        }
        if (Pressed(keyboard, previousKeyboard, Keys.F5))
            _windowManager.TileWindows(viewportWidth, viewportHeight);
        if (Pressed(keyboard, previousKeyboard, Keys.F8))
        {
            _windowManager.Toggle(CataclysmWindowId, viewportWidth, viewportHeight);
            ToolbarButtonClicked?.Invoke();
        }
        if (Pressed(keyboard, previousKeyboard, Keys.F9))
        {
            _windowManager.Toggle(ClimateWindowId, viewportWidth, viewportHeight);
            ToolbarButtonClicked?.Invoke();
        }
        if (Pressed(keyboard, previousKeyboard, Keys.F10))
        {
            _windowManager.Toggle(HistoryWindowId, viewportWidth, viewportHeight);
            ToolbarButtonClicked?.Invoke();
        }
    }

    public void Draw(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        MouseState mouse,
        Creature? selectedCreature,
        int plantCount,
        int herbivoreCount,
        int carnivoreCount,
        int omnivoreCount,
        float totalTime,
        bool paused,
        float speed,
        int viewportHeight,
        EcosystemMetrics? metrics = null)
    {
        LayoutToolbar(viewportHeight);
        var toolbar = _toolbarRect;
        UiPrimitives.Fill(spriteBatch, pixel, toolbar, new Color(UiTheme.DeepGrove, 235));
        UiPrimitives.Border(spriteBatch, pixel, toolbar, 2, UiTheme.BarkEdge);
        _statisticsButton.Draw(spriteBatch, pixel, font, mouse, false);
        _creatureButton.Draw(spriteBatch, pixel, font, mouse, false);
        _arrangeButton.Draw(spriteBatch, pixel, font, mouse, false);
        _speedDownButton.Draw(spriteBatch, pixel, font, mouse, false);

        // Speed label between arrows
        _speedSb.Clear();
        if (paused) _speedSb.Append(I18n.T("hud.paused"));
        else { _speedSb.Append(Math.Round(speed, 1)); _speedSb.Append('x'); }
        var slSize = font.MeasureString(_speedSb);
        var sx = _speedDownButton.Bounds.Right + (_speedUpButton.Bounds.X - _speedDownButton.Bounds.Right) / 2f - slSize.X / 2f;
        var sy = _speedDownButton.Bounds.Center.Y - slSize.Y / 2;
        spriteBatch.DrawString(font, _speedSb, new Vector2(sx, sy), Color.White);
        _speedUpButton.Draw(spriteBatch, pixel, font, mouse, false);
        _cataclysmButton.Draw(spriteBatch, pixel, font, mouse, false);
        _climateButton.Draw(spriteBatch, pixel, font, mouse, false);
        _historyButton.Draw(spriteBatch, pixel, font, mouse, false);
        _menuButton.Draw(spriteBatch, pixel, font, mouse, false);

        foreach (UiWindow window in _windowManager.Windows)
        {
            if (!window.IsOpen)
                continue;

            var isActive = _windowManager.IsActive(window);
            window.Draw(spriteBatch, pixel, font, isActive, mouse.Position);

            if (window.IsCollapsed)
                continue;

            if (window is StatisticsWindow statsWin)
            {
                statsWin.DrawContent(spriteBatch, pixel, font, plantCount, herbivoreCount, carnivoreCount, omnivoreCount, totalTime, paused, speed, metrics);
            }
            else if (window is CreatureDetailsWindow creatureWin)
            {
                creatureWin.DrawContent(spriteBatch, pixel, font, selectedCreature);
            }
            else if (window is TerrainWindow terrainWin)
            {
                terrainWin.DrawContent(spriteBatch, font, World, SelectedTile);
            }
            else if (window is CataclysmWindow cataWin)
            {
                cataWin.DrawContent(spriteBatch, pixel, font);
            }
            else if (window is ClimateWindow climateWin)
            {
                climateWin.DrawContent(spriteBatch, pixel, font, Climate, World, HoverTile, SelectedTile, plantCount, herbivoreCount, carnivoreCount, omnivoreCount, _popHistory, _popHistoryCount, _tempHistory, _tempHistoryCount);
            }
            else if (window is HistoryWindow historyWin)
            {
                historyWin.DrawContent(spriteBatch, pixel, font, mouse, _popHistory, _popHistoryCount, _tempHistory, _tempHistoryCount);
            }
        }
    }

    private void LayoutToolbar(int viewportHeight)
    {
        var y = viewportHeight - 56;
        _toolbarRect = new Rectangle(8, viewportHeight - 60, 830, 52);

        var x = 12;
        var gap = 6;
        _statisticsButton.Bounds = new Rectangle(x, y, 120, 44); x += 120 + gap;
        _creatureButton.Bounds = new Rectangle(x, y, 110, 44); x += 110 + gap;
        _arrangeButton.Bounds = new Rectangle(x, y, 90, 44); x += 90 + gap;
        _speedDownButton.Bounds = new Rectangle(x, y, 36, 44); x += 36 + 20;
        _speedUpButton.Bounds = new Rectangle(x, y, 36, 44); x += 36 + gap;
        _cataclysmButton.Bounds = new Rectangle(x, y, 120, 44); x += 120 + gap;
        _climateButton.Bounds = new Rectangle(x, y, 80, 44); x += 80 + gap;
        _historyButton.Bounds = new Rectangle(x, y, 80, 44); x += 80 + gap;
        _menuButton.Bounds = new Rectangle(x, y, 80, 44);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);

    private void RefreshText()
    {
        _statisticsButton.Text = I18n.T("toolbar.statistics");
        _creatureButton.Text = I18n.T("toolbar.creature");
        _arrangeButton.Text = I18n.T("toolbar.arrange");
        _cataclysmButton.Text = I18n.T("toolbar.cataclysm");
        _climateButton.Text = I18n.T("toolbar.climate");
        _historyButton.Text = I18n.T("toolbar.history");
        _menuButton.Text = I18n.T("toolbar.menu");

        _statisticsWindow.Title = I18n.T("window.statistics");
        _creatureWindow.Title = I18n.T("window.creature");
        _terrainWindow.Title = I18n.T("window.terrain");
        _cataclysmWindow.Title = I18n.T("window.cataclysm");
        _climateWindow.Title = I18n.T("window.climate");
        _historyWindow.Title = I18n.T("window.history");
        _cataclysmWindow.RefreshText();
    }

    public bool HandleCataclysmClick(MouseState mouse, MouseState prevMouse)
    {
        return _cataclysmWindow.HandleCataclysmClick(mouse, prevMouse);
    }
}
