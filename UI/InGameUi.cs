using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI;

public sealed class InGameUi
{
    public const string StatisticsWindowId = "statistics";
    public const string CreatureWindowId = "creature";
    public const string TerrainWindowId = "terrain";
    public const string CataclysmWindowId = "cataclysm";
    public string? SelectedCataclysm { get; set; }

    private readonly UiWindowManager _windowManager = new();
    private readonly UiButton _statisticsButton = new(I18n.T("toolbar.statistics"));
    private readonly UiButton _creatureButton = new(I18n.T("toolbar.creature"));
    private readonly UiButton _arrangeButton = new(I18n.T("toolbar.arrange"));
    private readonly UiButton _menuButton = new(I18n.T("toolbar.menu"));
    private readonly UiButton _cataclysmButton = new("CATACLYSM");

    public bool WantsToGoToMainMenu { get; set; } = false;
    public World? World { get; set; }
    public Point? SelectedTile { get; set; }

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
    }

    public void OpenCreatureWindow(int vpw, int vph) => _windowManager.Open(CreatureWindowId, vpw, vph);
    public void OpenTerrainWindow(int vpw, int vph) => _windowManager.Open(TerrainWindowId, vpw, vph);

    public void ResetForWorld(World world)
    {
        World = world;
        SelectedTile = null;
        WantsToGoToMainMenu = false;
    }

    public bool CloseTopWindow() => _windowManager.CloseTopWindow();

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

        if (Pressed(keyboard, previousKeyboard, Keys.F2))
            _windowManager.Toggle(StatisticsWindowId, viewportWidth, viewportHeight);
        if (Pressed(keyboard, previousKeyboard, Keys.F3))
            _windowManager.Toggle(CreatureWindowId, viewportWidth, viewportHeight);
        if (Pressed(keyboard, previousKeyboard, Keys.F5))
            _windowManager.TileWindows(viewportWidth, viewportHeight);
        if (Pressed(keyboard, previousKeyboard, Keys.F8))
            _windowManager.Toggle(CataclysmWindowId, viewportWidth, viewportHeight);

        bool toolbarConsumed = false;
        if (_statisticsButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(StatisticsWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
        }
        if (_creatureButton.WasClicked(mouse, previousMouse))
        {
            _windowManager.Toggle(CreatureWindowId, viewportWidth, viewportHeight);
            toolbarConsumed = true;
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
        }

        HandleCataclysmClick(mouse, previousMouse);

        bool overToolbar = _statisticsButton.Bounds.Contains(mouse.Position) || 
                           _creatureButton.Bounds.Contains(mouse.Position) ||
                           _arrangeButton.Bounds.Contains(mouse.Position) ||
                           _menuButton.Bounds.Contains(mouse.Position) ||
                           _cataclysmButton.Bounds.Contains(mouse.Position);
        return toolbarConsumed || overToolbar || _windowManager.Update(mouse, previousMouse, viewportWidth, viewportHeight);
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
        var toolbar = new Rectangle(8, viewportHeight - 60, 522, 52);
        UiPrimitives.Fill(spriteBatch, pixel, toolbar, new Color(UiTheme.DeepGrove, 235));
        UiPrimitives.Border(spriteBatch, pixel, toolbar, 2, UiTheme.BarkEdge);
        _statisticsButton.Draw(spriteBatch, pixel, font, mouse, false);
        _creatureButton.Draw(spriteBatch, pixel, font, mouse, false);
        _arrangeButton.Draw(spriteBatch, pixel, font, mouse, false);
        _cataclysmButton.Draw(spriteBatch, pixel, font, mouse, false);
        _menuButton.Draw(spriteBatch, pixel, font, mouse, false);

        foreach (UiWindow window in _windowManager.Windows)
        {
            if (!window.IsOpen)
                continue;

            bool isActive = _windowManager.IsActive(window);
            window.Draw(spriteBatch, pixel, font, isActive, mouse.Position);

            if (window.IsCollapsed)
                continue;

            if (window.Id == StatisticsWindowId)
            {
                int needed = DrawStatistics(spriteBatch, pixel, font, window.ContentBounds,
                    plantCount, herbivoreCount, carnivoreCount, omnivoreCount, totalTime, paused, speed, metrics);
                int totalH = needed + 72;
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
        }
    }

    private static int DrawStatistics(
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
        int total = plants + herbivores + carnivores + omnivores;
        int y = content.Y;
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
            int shown = 0;
            foreach (var kvp in metrics.SpeciesPopulations)
            {
                if (shown >= 14) break;
                Color col = kvp.Value > 0 ? UiTheme.WarmParchment : UiTheme.MutedStone;
                spriteBatch.DrawString(font, $"{kvp.Value} {I18n.Species(kvp.Key)}",
                    new Vector2(content.X, y), col);
                y += 13;
                shown++;
            }
        }

        return y - content.Y + 8;
    }

    

    private static void DrawInlineBar(SpriteBatch sb, Texture2D pixel, SpriteFont font,
        int x, int y, string label, int value, int total, Color color)
    {
        int barW = (int)(120f * (total > 0 ? value / (float)total : 0));
        int barH = 10;
        sb.DrawString(font, label, new Vector2(x, y - 2), color);
        var bg = new Rectangle(x + 14, y + 1, 122, barH);
        UiPrimitives.Fill(sb, pixel, bg, UiTheme.DeepGrove);
        if (barW > 0)
            UiPrimitives.Fill(sb, pixel, new Rectangle(bg.X, bg.Y, barW, barH), color);
        UiPrimitives.Border(sb, pixel, bg, 1, UiTheme.BarkEdge);
        sb.DrawString(font, value.ToString(), new Vector2(bg.Right + 4, y - 2), UiTheme.WarmParchment);
    }

    private static void DrawCreature(
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
        string statusText = creature.Gender == Gender.None
            ? I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")
            : $"{I18n.T(creature.Gender == Gender.Male ? "ui.gender.male" : "ui.gender.female")}  |  {I18n.T(creature.IsAdult ? "ui.status.adult" : "ui.status.baby")}";
        DrawLine(spriteBatch, font, content.X, content.Y + 100, statusText, UiTheme.MutedStone);
        DrawLine(spriteBatch, font, content.X, content.Y + 122, I18n.Format("creature.speed", creature.Genome.Speed), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 144, I18n.Format("creature.size", creature.Genome.Size), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 166, I18n.Format("creature.metabolism", creature.Genome.Metabolism), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 188, I18n.Format("creature.vision", creature.Genome.VisionRange), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 210, I18n.Format("creature.mutationRate", creature.Genome.MutationRate), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 236, I18n.T("creature.adaptations"), UiTheme.MossSignal);

        int col1X = content.X;
        int col2X = content.X + content.Width / 2;

        DrawLine(spriteBatch, font, col1X, content.Y + 258, I18n.Format("creature.adaptDesert", creature.Genome.DesertAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 258, I18n.Format("creature.adaptCold", creature.Genome.ColdAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col1X, content.Y + 280, I18n.Format("creature.adaptForest", creature.Genome.ForestAdaptation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, col2X, content.Y + 280, I18n.Format("creature.adaptWater", creature.Genome.WaterAdaptation), UiTheme.WarmParchment);

        DrawLine(spriteBatch, font, content.X, content.Y + 312, I18n.Format("creature.genome",
            creature.Genome.Color.R, creature.Genome.Color.G, creature.Genome.Color.B), UiTheme.MutedStone);
    }

    private static void DrawProgress(SpriteBatch spriteBatch, Texture2D pixel, Rectangle bounds, float value)
    {
        UiPrimitives.Fill(spriteBatch, pixel, bounds, UiTheme.DeepGrove);
        int width = (int)((bounds.Width - 4) * MathHelper.Clamp(value, 0f, 1f));
        if (width > 0)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(bounds.X + 2, bounds.Y + 2, width, bounds.Height - 4), UiTheme.MossSignal);
        UiPrimitives.Border(spriteBatch, pixel, bounds, 2, UiTheme.BarkEdge);
    }

    private static void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }

    private void LayoutToolbar(int viewportHeight)
    {
        int y = viewportHeight - 56;
        _statisticsButton.Bounds = new Rectangle(12, y, 100, 44);
        _creatureButton.Bounds = new Rectangle(118, y, 100, 44);
        _arrangeButton.Bounds = new Rectangle(224, y, 100, 44);
        _cataclysmButton.Bounds = new Rectangle(330, y, 100, 44);
        _menuButton.Bounds = new Rectangle(436, y, 100, 44);
    }

    private static bool Pressed(KeyboardState current, KeyboardState previous, Keys key) =>
        current.IsKeyDown(key) && previous.IsKeyUp(key);

    private void RefreshText()
    {
        _statisticsButton.Text = I18n.T("toolbar.statistics");
        _creatureButton.Text = I18n.T("toolbar.creature");
        _arrangeButton.Text = I18n.T("toolbar.arrange");
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
        int x = MathHelper.Clamp(p.X, 0, World.Width - 1);
        int y = MathHelper.Clamp(p.Y, 0, World.Height - 1);

        Tile tile = World.Tiles[x, y];
        float elevation = World.ElevationField[y * World.Width + x];
        bool isRiver = World.RiverMask[y * World.Width + x];

        string passStr = I18n.T(tile.IsPassable ? "common.yes" : "common.no");
        string riverStr = I18n.T(isRiver ? "common.yes" : "common.no");
        string biomeName = I18n.T($"biome.{tile.Biome}");

        DrawLine(spriteBatch, font, content.X, content.Y, I18n.Format("terrain.heading", x, y), UiTheme.MossSignal);
        DrawLine(spriteBatch, font, content.X, content.Y + 28, I18n.Format("terrain.biome", biomeName), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 50, I18n.Format("terrain.elevation", elevation), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 72, I18n.Format("terrain.passable", passStr), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 94, I18n.Format("terrain.river", riverStr), UiTheme.WarmParchment);
    }

    private readonly UiButton[] _cataclysmButtons = new[]
    {
        new UiButton("Asteroid") { Tag = "Asteroid" },
        new UiButton("Ice Age") { Tag = "IceAge" },
        new UiButton("Supervolcano") { Tag = "Supervolcano" },
        new UiButton("Earthquake") { Tag = "Earthquake" },
        new UiButton("Drought") { Tag = "Drought" },
        new UiButton("Flood") { Tag = "Flood" }
    };

    private void DrawCataclysmWindow(SpriteBatch sb, Texture2D pixel, SpriteFont font, Rectangle content)
    {
        int y = content.Y;
        var mouse = Microsoft.Xna.Framework.Input.Mouse.GetState();

        for (int i = 0; i < _cataclysmButtons.Length; i++)
        {
            var btn = _cataclysmButtons[i];
            btn.Bounds = new Rectangle(content.X, y, content.Width, 22);
            bool sel = SelectedCataclysm == (string)btn.Tag!;
            btn.Draw(sb, pixel, font, mouse, sel);
            y += 26;
        }
        if (!string.IsNullOrEmpty(SelectedCataclysm))
            sb.DrawString(font, "Click on map to place", new Vector2(content.X, content.Bottom - 20), new Color(255,200,100));
    }

    public bool HandleCataclysmClick(MouseState mouse, MouseState prevMouse)
    {
        for (int i = 0; i < _cataclysmButtons.Length; i++)
        {
            if (_cataclysmButtons[i].WasClicked(mouse, prevMouse))
            {
                SelectedCataclysm = (string)_cataclysmButtons[i].Tag!;
                return true;
            }
        }
        return false;
    }
}
