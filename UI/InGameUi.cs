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

    private readonly UiWindowManager _windowManager = new();
    private readonly UiButton _statisticsButton = new(I18n.T("toolbar.statistics"));
    private readonly UiButton _creatureButton = new(I18n.T("toolbar.creature"));
    private readonly UiButton _arrangeButton = new(I18n.T("toolbar.arrange"));
    private readonly UiButton _menuButton = new(I18n.T("toolbar.menu"));

    public bool WantsToGoToMainMenu { get; set; } = false;
    public World? World { get; set; }
    public Point? SelectedTile { get; set; }

    public InGameUi()
    {
        _windowManager.Add(new UiWindow(I18n.T("window.statistics"), StatisticsWindowId)
        {
            Bounds = new Rectangle(32, 88, 320, 248),
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

        bool overToolbar = _statisticsButton.Bounds.Contains(mouse.Position) || 
                           _creatureButton.Bounds.Contains(mouse.Position) ||
                           _arrangeButton.Bounds.Contains(mouse.Position) ||
                           _menuButton.Bounds.Contains(mouse.Position);
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
                DrawStatistics(spriteBatch, pixel, font, window.ContentBounds,
                    plantCount, herbivoreCount, carnivoreCount, omnivoreCount, totalTime, paused, speed, metrics);
            }
            else if (window.Id == CreatureWindowId)
            {
                DrawCreature(spriteBatch, pixel, font, window.ContentBounds, selectedCreature);
            }
            else if (window.Id == TerrainWindowId)
            {
                DrawTerrainWindow(spriteBatch, pixel, font, window.ContentBounds);
            }
        }
    }

    private static void DrawStatistics(
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
        y += 22;
        DrawLine(spriteBatch, font, content.X, y,
            paused ? I18n.T("stats.paused") : I18n.Format("stats.speed", speed),
            paused ? UiTheme.DangerClay : UiTheme.MossSignal);
        y += 32;
        DrawLine(spriteBatch, font, content.X, y, I18n.Format("stats.total", total), UiTheme.WarmParchment);

        if (metrics != null)
        {
            y += 22;
            DrawLine(spriteBatch, font, content.X, y,
                $"Births: {metrics.TotalBirths}  Deaths: {metrics.TotalDeaths}", UiTheme.MutedStone);
            y += 18;
            DrawLine(spriteBatch, font, content.X, y,
                $"Starve: {metrics.StarvationDeaths}  Age: {metrics.OldAgeDeaths}  Pred: {metrics.PredationDeaths}  Comb: {metrics.CombatDeaths}",
                new Color(180, 150, 130));
            y += 18;
            DrawLine(spriteBatch, font, content.X, y,
                $"Species: {metrics.SpeciesCount}  Het: {metrics.MeanHeterozygosity:F3}  Inb: {metrics.MeanInbreeding:F3}",
                UiTheme.MutedStone);
        }

        y += 30;
        DrawPopulationRow(spriteBatch, pixel, font, content, y, I18n.Format("stats.plants", plants), plants, total, UiTheme.MossSignal);
        y += 30;
        DrawPopulationRow(spriteBatch, pixel, font, content, y, I18n.Format("stats.herbivores", herbivores), herbivores, total, UiTheme.LakeBlue);
        y += 30;
        DrawPopulationRow(spriteBatch, pixel, font, content, y, I18n.Format("stats.carnivores", carnivores), carnivores, total, UiTheme.DangerClay);
        y += 30;
        DrawPopulationRow(spriteBatch, pixel, font, content, y, I18n.Format("stats.omnivores", omnivores), omnivores, total, UiTheme.WarmParchment);
    }

    private static void DrawPopulationRow(
        SpriteBatch spriteBatch,
        Texture2D pixel,
        SpriteFont font,
        Rectangle content,
        int y,
        string label,
        int value,
        int total,
        Color color)
    {
        DrawLine(spriteBatch, font, content.X, y, label, UiTheme.MutedStone);
        var track = new Rectangle(content.X + 112, y + 3, content.Width - 112, 10);
        UiPrimitives.Fill(spriteBatch, pixel, track, UiTheme.DeepGrove);
        int width = total == 0 ? 0 : (int)(track.Width * (value / (float)total));
        if (width > 0)
            UiPrimitives.Fill(spriteBatch, pixel, new Rectangle(track.X, track.Y, width, track.Height), color);
        UiPrimitives.Border(spriteBatch, pixel, track, 1, UiTheme.BarkEdge);
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
        _statisticsButton.Bounds = new Rectangle(12, y, 120, 44);
        _creatureButton.Bounds = new Rectangle(140, y, 120, 44);
        _arrangeButton.Bounds = new Rectangle(268, y, 120, 44);
        _menuButton.Bounds = new Rectangle(396, y, 120, 44);
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
}
