using System;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Simulation;

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
    public string? SelectedCataclysm { get; set; }

    private readonly StringBuilder _speedSb = new StringBuilder(16);
    private readonly StringBuilder _valueSb = new StringBuilder(16);
    private readonly StringBuilder _speciesSb = new StringBuilder(64);
    private readonly StringBuilder _creatureGenSb = new StringBuilder(128);
    private readonly StringBuilder _creatureLineageSb = new StringBuilder(128);

    private readonly StringBuilder _tempSb = new StringBuilder(32);

    private Rectangle _toolbarRect;

    private readonly UiWindowManager _windowManager = new();
    private readonly UiButton _statisticsButton = new(I18n.T("toolbar.statistics")) { ShortcutHint = "F2" };
    private readonly UiButton _creatureButton = new(I18n.T("toolbar.creature")) { ShortcutHint = "F3" };
    private readonly UiButton _arrangeButton = new(I18n.T("toolbar.arrange")) { ShortcutHint = "F5" };
    private readonly UiButton _menuButton = new(I18n.T("toolbar.menu")) { ShortcutHint = "ESC" };
    private readonly UiButton _cataclysmButton = new(I18n.T("toolbar.cataclysm")) { ShortcutHint = "F8" };
    private readonly UiButton _climateButton = new(I18n.T("toolbar.climate")) { ShortcutHint = "F9" };
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

    public InGameUi()
    {
        _windowManager.Add(new UiWindow(I18n.T("window.statistics"), StatisticsWindowId)
        {
            Bounds = new Rectangle(32, 88, 300, 380),
            IsOpen = true,
            ShowCloseButton = true
        });
        _windowManager.Add(new UiWindow(I18n.T("window.creature"), CreatureWindowId)
        {
            Bounds = new Rectangle(376, 112, 384, 410),
            ShowCloseButton = true
        });
        _windowManager.Add(new UiWindow(I18n.T("window.terrain"), TerrainWindowId)
        {
            Bounds = new Rectangle(32, 352, 320, 180),
            ShowCloseButton = true
        });
        _windowManager.Add(new UiWindow(I18n.T("window.cataclysm"), CataclysmWindowId)
        {
            Bounds = new Rectangle(370, 88, 220, 200),
            ShowCloseButton = true
        });
        _windowManager.Add(new UiWindow(I18n.T("window.climate"), ClimateWindowId)
        {
            Bounds = new Rectangle(348, 96, 300, 420),
            ShowCloseButton = true
        });
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

        HandleCataclysmClick(mouse, previousMouse);

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
        _menuButton.Draw(spriteBatch, pixel, font, mouse, false);

        foreach (UiWindow window in _windowManager.Windows)
        {
            if (!window.IsOpen)
                continue;

            var isActive = _windowManager.IsActive(window);
            window.Draw(spriteBatch, pixel, font, isActive, mouse.Position);

            if (window.IsCollapsed)
                continue;

            if (window.Id == StatisticsWindowId)
            {
                var needed = DrawStatistics(spriteBatch, pixel, font, window.ContentBounds,
                    plantCount, herbivoreCount, carnivoreCount, omnivoreCount, totalTime, paused, speed, metrics);
                var totalH = needed + 72;
                if (totalH != window.Bounds.Height)
                    window.Bounds = new Rectangle(window.Bounds.X, window.Bounds.Y, window.Bounds.Width, totalH);
            }
            else if (window.Id == CreatureWindowId)
            {
                DrawCreature(spriteBatch, pixel, font, window.ContentBounds, selectedCreature);
            }
            else if (window.Id == TerrainWindowId)
            {
                DrawTerrainWindow(spriteBatch, pixel, font, window.ContentBounds);
            }
            else if (window.Id == CataclysmWindowId)
            {
                DrawCataclysmWindow(spriteBatch, pixel, font, window.ContentBounds);
            }
            else if (window.Id == ClimateWindowId)
            {
                DrawClimateDashboard(spriteBatch, pixel, font, window.ContentBounds,
                    plantCount, herbivoreCount, carnivoreCount, omnivoreCount);
            }
        }
    }

    private int DrawStatistics(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        int plants,
        int herbivores,
        int carnivores,
        int omnivores,
        float time,
        bool paused,
        float speed,
        EcosystemMetrics? metrics)
    {
        var total = plants + herbivores + carnivores + omnivores;
        var y = content.Y;
        DrawLine(spriteBatch, font, content.X, y, I18n.Format("stats.time", time), UiTheme.WarmParchment);
        y += 18;
        DrawLine(spriteBatch, font, content.X, y,
            paused ? I18n.T("stats.paused") : I18n.Format("stats.speed", speed),
            paused ? UiTheme.DangerClay : UiTheme.MossSignal);
        y += 18;
        DrawLine(spriteBatch, font, content.X, y, I18n.Format("stats.total", total), UiTheme.WarmParchment);
        y += 22;

        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "P", plants, total, UiTheme.MossSignal);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "H", herbivores, total, UiTheme.LakeBlue);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "C", carnivores, total, UiTheme.DangerClay);
        y += 16;
        DrawInlineBar(spriteBatch, pixel, font, content.X, y, "O", omnivores, total, UiTheme.WarmParchment);

        if (metrics != null && metrics.SpeciesPopulations.Count > 0)
        {
            y += 22;
            spriteBatch.DrawString(font, I18n.T("stats.speciesList"),
                new Vector2(content.X, y), UiTheme.MossSignal);
            y += 14;
            var shown = 0;
            foreach (var kvp in metrics.SpeciesPopulations)
            {
                if (shown >= 14) break;
                Color col = kvp.Value > 0 ? UiTheme.WarmParchment : UiTheme.MutedStone;
                _speciesSb.Clear();
                _speciesSb.Append(kvp.Value).Append(' ').Append(I18n.Species(kvp.Key));
                spriteBatch.DrawString(font, _speciesSb, new Vector2(content.X, y), col);
                y += 13;
                shown++;
            }
        }

        return y - content.Y + 8;
    }



    private void DrawInlineBar(SpriteBatch sb, Texture2D pixel, SpriteFont font,
        int x, int y, string label, int value, int total, Color color)
    {
        var barW = (int)(120f * (total > 0 ? value / (float)total : 0));
        var barH = 10;
        sb.DrawString(font, label, new Vector2(x, y - 2), color);
        var bg = new Rectangle(x + 14, y + 1, 122, barH);
        UiPrimitives.Fill(sb, pixel, bg, UiTheme.DeepGrove);
        if (barW > 0)
            UiPrimitives.Fill(sb, pixel, new Rectangle(bg.X, bg.Y, barW, barH), color);
        UiPrimitives.Border(sb, pixel, bg, 1, UiTheme.BarkEdge);

        _valueSb.Clear();
        _valueSb.Append(value);
        sb.DrawString(font, _valueSb, new Vector2(bg.Right + 4, y - 2), UiTheme.WarmParchment);
    }

    private void DrawCreature(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        Creature? creature)
    {
        if (creature == null || !creature.IsAlive)
        {
            DrawLine(spriteBatch, font, content.X, content.Y, I18n.T("creature.none"), UiTheme.MutedStone);
            DrawLine(spriteBatch, font, content.X, content.Y + 24, I18n.T("creature.selectHint"), UiTheme.MutedStone);
            return;
        }

        DrawLine(spriteBatch, font, content.X, content.Y,
            I18n.Format("creature.heading", I18n.Species(creature.Species), I18n.CreatureTypeName(creature.CreatureType)), UiTheme.MossSignal);
        DrawLine(spriteBatch, font, content.X, content.Y + 28, I18n.Format("creature.energy", creature.Energy, creature.MaxEnergy), UiTheme.WarmParchment);
        DrawProgress(spriteBatch, pixel, new Rectangle(content.X, content.Y + 48, content.Width, 14), creature.Energy / creature.MaxEnergy);
        DrawLine(spriteBatch, font, content.X, content.Y + 78, I18n.Format("creature.age", creature.Age), UiTheme.WarmParchment);
        var statusText = creature.Gender == Gender.None
            ? I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")
            : $"{I18n.T(creature.Gender == Gender.Male ? "ui.gender.male" : "ui.gender.female")}  |  {I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")}";
        DrawLine(spriteBatch, font, content.X, content.Y + 100, statusText, UiTheme.MutedStone);
        DrawLine(spriteBatch, font, content.X, content.Y + 122, I18n.Format("creature.speed", creature.Genome.Speed), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 144, I18n.Format("creature.size", creature.Genome.Size), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 166, I18n.Format("creature.metabolism", creature.Genome.Metabolism), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 188, I18n.Format("creature.vision", creature.Genome.VisionRange), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 210, I18n.Format("creature.mutationRate", creature.Genome.MutationRate), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 236, I18n.T("creature.adaptations"), UiTheme.MossSignal);

        var col1X = content.X;
        var col2X = content.X + content.Width / 2;

        DrawLine(spriteBatch, font, col1X, content.Y + 258, I18n.Format("creature.adaptDesert", creature.Genome.DesertAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 258, I18n.Format("creature.adaptCold", creature.Genome.ColdAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col1X, content.Y + 280, I18n.Format("creature.adaptForest", creature.Genome.ForestAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 280, I18n.Format("creature.adaptWater", creature.Genome.WaterAdaptation), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 312, I18n.Format("creature.genome",
            creature.Genome.Color.R, creature.Genome.Color.G, creature.Genome.Color.B), UiTheme.MutedStone);

        // Lineage tree
        DrawLine(spriteBatch, font, content.X, content.Y + 340, "Lineage", UiTheme.MossSignal);
        var lineage = creature.Lineage;
        var genDepth = 0;
        foreach (var kv in lineage.AncestorDepths)
            genDepth = Math.Max(genDepth, kv.Value);
        _creatureLineageSb.Clear();
        if (lineage.ParentAId > 0)
        {
            _creatureLineageSb.Append("Parents: [").Append(lineage.ParentAId).Append(", ").Append(lineage.ParentBId).Append("]  |  ID: ").Append(lineage.IndividualId);
        }
        else
        {
            _creatureLineageSb.Append("ID: ").Append(lineage.IndividualId).Append("  |  Founder");
        }
        DrawLine(spriteBatch, font, content.X, content.Y + 362, _creatureLineageSb, UiTheme.WarmParchment);

        _creatureGenSb.Clear();
        _creatureGenSb.Append("Ancestors: ").Append(lineage.AncestorDepths.Count).Append("  |  MaxGen: ").Append(genDepth)
            .Append("  |  Inbreeding: ").Append(Math.Round(creature.InbreedingCoefficient, 3))
            .Append("  |  Fitness: ").Append(Math.Round(creature.GeneticFitness, 2));
        DrawLine(spriteBatch, font, content.X, content.Y + 382, _creatureGenSb, new Color(200, 180, 140));
    }

    private static void DrawProgress(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, float value)
    {
        UiPrimitives.Fill(spriteBatch, pixel, bounds, UiTheme.DeepGrove);
        var width = (int)((bounds.Width - 4) * MathHelper.Clamp(value, 0f, 1f));
        if (width > 0)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), UiTheme.MossSignal);
        UiPrimitives.Border(spriteBatch, pixel, bounds, 2, UiTheme.BarkEdge);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, StringBuilder text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }



    private void LayoutToolbar(int viewportHeight)
    {
        var y = viewportHeight - 56;
        _toolbarRect = new Rectangle(8, viewportHeight - 60, 720, 52);

        var x = 12;
        var gap = 6;
        _statisticsButton.Bounds = new Rectangle(x, y, 120, 44); x += 120 + gap;
        _creatureButton.Bounds = new Rectangle(x, y, 110, 44); x += 110 + gap;
        _arrangeButton.Bounds = new Rectangle(x, y, 90, 44); x += 90 + gap;
        _speedDownButton.Bounds = new Rectangle(x, y, 36, 44); x += 36 + 20;
        _speedUpButton.Bounds = new Rectangle(x, y, 36, 44); x += 36 + gap;
        _cataclysmButton.Bounds = new Rectangle(x, y, 120, 44); x += 120 + gap;
        _climateButton.Bounds = new Rectangle(x, y, 80, 44); x += 80 + gap;
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
        _menuButton.Text = I18n.T("toolbar.menu");
        foreach (UiWindow window in _windowManager.Windows)
        {
            if (window.Id == StatisticsWindowId)
                window.Title = I18n.T("window.statistics");
            else if (window.Id == CreatureWindowId)
                window.Title = I18n.T("window.creature");
            else if (window.Id == TerrainWindowId)
                window.Title = I18n.T("window.terrain");
        }
    }

    private void DrawTerrainWindow(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content)
    {
        if (World == null || SelectedTile == null)
        {
            DrawLine(spriteBatch, font, content.X, content.Y, I18n.T("terrain.none"), UiTheme.MutedStone);
            DrawLine(spriteBatch, font, content.X, content.Y + 24, I18n.T("terrain.selectHint"), UiTheme.MutedStone);
            return;
        }

        Point p = SelectedTile.Value;
        var x = MathHelper.Clamp(p.X, 0, World.Width - 1);
        var y = MathHelper.Clamp(p.Y, 0, World.Height - 1);

        Tile tile = World.Tiles[x, y];
        var elevation = World.ElevationField[y * World.Width + x];
        var elevationM = (elevation - 0.15f) / 0.85f * 4000f;
        var isRiver = World.RiverMask[y * World.Width + x];

        var passStr = I18n.T(tile.IsPassable ? "common.yes" : "common.no");
        var riverStr = I18n.T(isRiver ? "common.yes" : "common.no");
        var biomeName = I18n.T($"biome.{tile.Biome}");

        DrawLine(spriteBatch, font, content.X, content.Y, I18n.Format("terrain.heading", x, y), UiTheme.MossSignal);
        DrawLine(spriteBatch, font, content.X, content.Y + 28, I18n.Format("terrain.biome", biomeName), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 50, I18n.Format("terrain.elevation", elevationM), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 72, I18n.Format("terrain.passable", passStr), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 94, I18n.Format("terrain.river", riverStr), UiTheme.WarmParchment);
    }

    private readonly UiButton[] _cataclysmButtons = new[]
    {
        new UiButton(I18n.T("cata.asteroid")) { Tag = "Asteroid" },
        new UiButton(I18n.T("cata.iceage")) { Tag = "IceAge" },
        new UiButton(I18n.T("cata.supervolcano")) { Tag = "Supervolcano" },
        new UiButton(I18n.T("cata.earthquake")) { Tag = "Earthquake" },
        new UiButton(I18n.T("cata.drought")) { Tag = "Drought" },
        new UiButton(I18n.T("cata.flood")) { Tag = "Flood" }
    };

    private void DrawCataclysmWindow(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content)
    {
        var y = content.Y;
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

        for (var i = 0; i < _cataclysmButtons.Length; i++)
        {
            var btn = _cataclysmButtons[i];
            btn.Bounds = new Rectangle(content.X, y, content.Width, 22);
            var sel = SelectedCataclysm == (string)btn.Tag!;
            btn.Draw(sb, pixel, font, mouse, sel);
            y += 26;
        }
        if (!string.IsNullOrEmpty(SelectedCataclysm))
            sb.DrawString(font, I18n.T("cata.placeHint"), new Vector2(content.X, content.Bottom - 20), new Color(255, 200, 100));
    }

    public bool HandleCataclysmClick(MouseState mouse, MouseState prevMouse)
    {
        for (var i = 0; i < _cataclysmButtons.Length; i++)
        {
            if (_cataclysmButtons[i].WasClicked(mouse, prevMouse))
            {
                SelectedCataclysm = (string)_cataclysmButtons[i].Tag!;
                return true;
            }
        }
        return false;
    }


    private void DrawClimateDashboard(SpriteBatch sb, Texture2D pixel, SpriteFont font,
        Rectangle content, int plantCount, int herbivoreCount,
        int carnivoreCount, int omnivoreCount)
    {
        var climate = Climate;
        if (climate == null || World == null)
        {
            DrawLine(sb, font, content.X, content.Y, "Climate data unavailable", UiTheme.MutedStone);
            return;
        }

        var y = content.Y;

        DrawClimateGlobalData(sb, pixel, font, content, climate, ref y);
        DrawClimateLocalData(sb, font, content, climate, ref y);
        DrawClimatePopulationData(sb, pixel, font, content, plantCount, herbivoreCount, carnivoreCount, omnivoreCount, ref y);
        DrawClimateEventsData(sb, font, content, ref y);
    }

    private void DrawClimateGlobalData(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        // Global data
        var seasonKey = climate.CurrentSeason switch
        {
            Simulation.Season.Spring => "season.Spring",
            Simulation.Season.Summer => "season.Summer",
            Simulation.Season.Autumn => "season.Autumn",
            Simulation.Season.Winter => "season.Winter",
            _ => "season.Spring"
        };
        DrawLine(sb, font, content.X, y, I18n.Format("climate.season", I18n.T(seasonKey)), UiTheme.MossSignal);
        y += 20;

        float progressW = content.Width - 8;
        DrawProgress(sb, pixel, new Rectangle(content.X, y, (int)progressW, 10), climate.SeasonProgress);
        y += 16;

        _tempSb.Clear();
        _tempSb.Append((int)(20f + climate.TemperatureModifier * 20f)).Append("°C");
        var tempNorm = Math.Clamp((climate.TemperatureModifier + 0.15f) / 0.3f, 0f, 1f);
        DrawLine(sb, font, content.X, y, I18n.Format("climate.temperature", _tempSb.ToString()),
            Color.Lerp(new Color(100, 150, 255), new Color(255, 120, 40), tempNorm));
        y += 18;

        if (_tempHistoryCount >= 3)
        {
            DrawTempSparkline(sb, pixel, content.X, y, content.Width, _tempHistory, _tempHistoryCount);
            y += 12;
        }
    }

    private void DrawClimateLocalData(SpriteBatch sb, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        DrawLocalTileData(sb, font, content, climate, ref y);
        DrawOrbitalData(sb, font, content, climate, ref y);
        DrawWindData(sb, font, content, climate, ref y);
        DrawExtremeEventData(sb, font, content, climate, ref y);
    }

    private void DrawLocalTileData(SpriteBatch sb, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        // Local tile data
        Point h = HoverTile ?? SelectedTile ?? new Point(World!.Width / 2, World!.Height / 2);
        var lx = Math.Clamp(h.X, 0, World!.Width - 1);
        var ly = Math.Clamp(h.Y, 0, World!.Height - 1);
        Simulation.Tile localTile = World!.GetTile(lx, ly);
        var localTemp = climate.GetTileTemperature(localTile, ly, World!.Height);
        Simulation.Season localSeason = climate.GetLocalSeason(ly, World!.Height);
        var localSeasonKey = localSeason switch
        {
            Simulation.Season.Spring => "season.Spring",
            Simulation.Season.Summer => "season.Summer",
            Simulation.Season.Autumn => "season.Autumn",
            Simulation.Season.Winter => "season.Winter",
            _ => "season.Spring"
        };
        var localSeasonName = I18n.T(localSeasonKey);
        var biomeName = I18n.T($"biome.{localTile.Biome}");
        DrawLine(sb, font, content.X, y,
            I18n.Format("climate.local", lx, ly, biomeName, (int)localTemp), UiTheme.WarmParchment);
        y += 18;
        DrawLine(sb, font, content.X, y,
            I18n.Format("climate.localseason", localSeasonName), UiTheme.MossSignal);
        y += 22;
    }

    private void DrawOrbitalData(SpriteBatch sb, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        DrawLine(sb, font, content.X, y,
            I18n.Format("climate.orbit", climate.SunDistanceAU, climate.OrbitalAngle * 180f / MathF.PI, climate.OrbitalSpeedKmS),
            UiTheme.WarmParchment);
        y += 18;
    }

    private void DrawWindData(SpriteBatch sb, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        DrawLine(sb, font, content.X, y,
            I18n.Format("climate.wind", climate.WindSpeed, climate.WindDirection * 180f / MathF.PI),
            UiTheme.WarmParchment);
        y += 18;
    }

    private void DrawExtremeEventData(SpriteBatch sb, SpriteFont font, Rectangle content, Simulation.ClimateSystem climate, ref int y)
    {
        if (climate.IsExtremeEvent)
        {
            DrawLine(sb, font, content.X, y,
                I18n.Format("climate.extreme", climate.ExtremeEventName),
                climate.ExtremeEventName == "Heatwave" ? new Color(255, 140, 40) : new Color(120, 180, 255));
            y += 18;
        }
    }

    private void DrawClimatePopulationData(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content, int plantCount, int herbivoreCount, int carnivoreCount, int omnivoreCount, ref int y)
    {
        y += 6;
        DrawLine(sb, font, content.X, y, I18n.T("climate.populations"), UiTheme.MossSignal);
        y += 20;
        var totalPop = plantCount + herbivoreCount + carnivoreCount + omnivoreCount;
        DrawInlineBar(sb, pixel, font, content.X, y, "P", plantCount, totalPop, UiTheme.MossSignal);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "H", herbivoreCount, totalPop, UiTheme.LakeBlue);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "C", carnivoreCount, totalPop, UiTheme.DangerClay);
        y += 16;
        DrawInlineBar(sb, pixel, font, content.X, y, "O", omnivoreCount, totalPop, UiTheme.WarmParchment);
        y += 20;

        if (_popHistoryCount >= 3)
        {
            DrawSparkline(sb, pixel, content.X, y, content.Width, _popHistory, _popHistoryCount, 0, UiTheme.MossSignal);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, _popHistory, _popHistoryCount, 1, UiTheme.LakeBlue);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, _popHistory, _popHistoryCount, 2, UiTheme.DangerClay);
            y += 14;
            DrawSparkline(sb, pixel, content.X, y, content.Width, _popHistory, _popHistoryCount, 3, UiTheme.WarmParchment);
            y += 20;
        }
    }

    private void DrawClimateEventsData(SpriteBatch sb, SpriteFont font, Rectangle content, ref int y)
    {
        DrawLine(sb, font, content.X, y, I18n.T("climate.events"), UiTheme.MossSignal);
        y += 20;
        var recent = Core.Logger.RecentEvents;
        var maxShow = Math.Min(5, recent.Count);
        for (var i = recent.Count - maxShow; i < recent.Count; i++)
        {
            var ev = TranslateEvent(recent[i]);
            if (ev.Length > 44) ev = ev.Substring(0, 44);
            DrawLine(sb, font, content.X, y, ev, new Color(180, 160, 140));
            y += 16;
        }
    }

    private static void DrawSparkline(SpriteBatch sb, Texture2D pixel, int baseX, int baseY, int width,
        PopSnapshot[] history, int count, int field, Color color)
    {
        var w = width - 8;
        var h = 10;
        var xStep = count > 1 ? (float)w / (count - 1) : 0;

        var maxVal = 1;
        for (var i = 0; i < count; i++)
        {
            var v = field switch { 0 => history[i].Plants, 1 => history[i].Herbivores, 2 => history[i].Carnivores, _ => history[i].Omnivores };
            if (v > maxVal) maxVal = v;
        }
        if (maxVal < 1) maxVal = 1;

        for (var i = 1; i < count; i++)
        {
            var v0 = field switch { 0 => history[i - 1].Plants, 1 => history[i - 1].Herbivores, 2 => history[i - 1].Carnivores, _ => history[i - 1].Omnivores };
            var v1 = field switch { 0 => history[i].Plants, 1 => history[i].Herbivores, 2 => history[i].Carnivores, _ => history[i].Omnivores };
            var x0 = baseX + (int)((i - 1) * xStep);
            var y0 = baseY + h - (int)(v0 * h / (float)maxVal);
            var x1 = baseX + (int)(i * xStep);
            var y1 = baseY + h - (int)(v1 * h / (float)maxVal);
            DrawLineSegment(sb, pixel, new Point(x0, y0), new Point(x1, y1), color);
        }
    }

    private static void DrawLineSegment(SpriteBatch sb, Texture2D pixel, Point p0, Point p1, Color color)
    {
        int x0 = p0.X, y0 = p0.Y;
        int x1 = p1.X, y1 = p1.Y;
        int dx = Math.Abs(x1 - x0), dy = Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        var err = dx - dy;
        while (true)
        {
            sb.Draw(pixel, new Rectangle(x0, y0, 1, 1), color);
            if (x0 == x1 && y0 == y1) break;
            var e2 = 2 * err;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private static void DrawTempSparkline(SpriteBatch sb, Texture2D pixel, int baseX, int baseY, int width,
        float[] history, int count)
    {
        int w = width - 8, h = 12;
        var xStep = count > 1 ? (float)w / (count - 1) : 0;
        float min = float.MaxValue, max = float.MinValue;
        for (var i = 0; i < count; i++) { if (history[i] < min) min = history[i]; if (history[i] > max) max = history[i]; }
        var range = max - min; if (range < 1f) range = 1f;
        for (var i = 1; i < count; i++)
        {
            float v0 = (history[i - 1] - min) / range, v1 = (history[i] - min) / range;
            Color c0 = Color.Lerp(new Color(80, 120, 220), new Color(230, 80, 40), v0);
            Color c1 = Color.Lerp(new Color(80, 120, 220), new Color(230, 80, 40), v1);
            int x0 = baseX + (int)((i - 1) * xStep), y0 = baseY + h - (int)(v0 * h);
            int x1 = baseX + (int)(i * xStep), y1 = baseY + h - (int)(v1 * h);
            DrawLineSegment(sb, pixel, new Point(x0, y0), new Point(x1, y1), c0);
        }
    }

    private static string TranslateEvent(string ev)
    {
        ev = ev.Replace("[SEASON]", I18n.T("evt.season"))
               .Replace("[CATACLYSM]", I18n.T("evt.cataclysm"))
               .Replace("[CLIMATE]", I18n.T("evt.climate"))
               .Replace("[DEATH]", I18n.T("evt.death"))
               .Replace("[SPAWN]", I18n.T("evt.spawn"))
               .Replace("[TERRAIN]", I18n.T("evt.terrain"));
        ev = ev.Replace("Season changed to ", I18n.T("evt.msg.season"))
               .Replace(" at T=", " " + I18n.T("evt.msg.at") + "=")
               .Replace(" at (", " " + I18n.T("evt.msg.at") + " (")
               .Replace("Player ", I18n.T("evt.msg.player") + " ")
               .Replace("MASS EXTINCTION: ", I18n.T("evt.msg.extinction") + " ")
               .Replace("Extreme event '", I18n.T("evt.msg.extreme") + " '")
               .Replace("' started", "' " + I18n.T("evt.msg.started"))
               .Replace("' ended", "' " + I18n.T("evt.msg.ended"))
               .Replace("Player spawned ", I18n.T("evt.msg.spawned") + " ")
               .Replace("crater at ", I18n.T("evt.msg.crater") + " ")
               .Replace("r=", I18n.T("evt.msg.radius") + "=")
               .Replace("duration=", I18n.T("evt.msg.duration") + "=")
               .Replace("Cataclysm ended", I18n.T("evt.msg.cataend"))
               .Replace("died at age ", I18n.T("evt.msg.died"))
               .Replace("(dist=", "(" + I18n.T("evt.msg.dist") + "=");

        foreach (var sp in Simulation.SpeciesRegistry.All)
        {
            var tr = Localization.I18n.Species(sp);
            if (tr != sp && ev.Contains(sp))
            {
                var idx = ev.IndexOf(sp);
                if (idx > 0 && !char.IsLetterOrDigit(ev[idx - 1]))
                    ev = ev.Replace(sp, tr);
            }
        }
        return ev;
    }
}
