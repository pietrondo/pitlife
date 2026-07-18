using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;
using PitLife.Core;

namespace PitLife;

public class Game1 : Game
{
    internal readonly GraphicsDeviceManager _graphics;
    internal SpriteBatch _spriteBatch = null!;
    internal Ecosystem _ecosystem = null!;
    internal Camera _camera = null!;
    internal PixelWorldRenderer _worldRenderer = null!;
    internal CreatureRenderer _creatureRenderer = null!;
    internal readonly DayNightCycle _dayNight = new();
    internal Minimap _minimap = null!;
    internal SpriteFont _font = null!;
    internal Texture2D? _logo;
    internal Texture2D _uiPixel = null!;
    internal readonly MainMenu _mainMenu = new();
    internal readonly HelpScreen _helpScreen = new();
    internal readonly InGameUi _inGameUi = new();
    internal readonly SpawnPanel _spawnPanel = new();
    internal readonly CataclysmPanel _cataclysmPanel = new();
    internal readonly WeatherSystem _weather = new();
    internal WaterEffect _waterEffect = null!;
    internal readonly SpeciesCatalogRuntime _speciesCatalogRuntime = new();
    internal SpeciesEditorPanel _speciesEditor = null!;
    internal readonly SpeciesCyclopedia _cyclopedia = new();
    internal readonly System.Text.StringBuilder _sb = new();
    internal float _currentFPS;
    internal float _frametimeMS;
    internal int _frameCount;
    internal double _fpsTimer;
    internal bool _showDebugOverlay;
    internal bool _contentLoaded;
    internal float _showLoadingTimer = 1.5f;
    internal bool _pendingWorldGen;
    internal int? _pendingSeed;
    internal WorldGenOptions? _pendingOptions;
    internal GameScreen _screen = GameScreen.MainMenu;
    internal float _menuInputCooldown = 0.75f;

    internal int _displayPlants, _displayHerbivores, _displayCarnivores, _displayOmnivores;
    internal float _displayTime;
    internal bool _paused;
    internal SimulationController _controller = null!;

    internal Creature? _selectedCreature;
    internal int _cataSelectedFrame;
    internal string? _prevPanelCata;
    internal string? _prevSpawnCata;
    internal string? _prevSpawnSpecies;
    internal int _gameFrame;

    internal InputManager _inputManager = null!;
    internal SimulationOrchestrator _orchestrator = null!;
    private GameLoopCoordinator _coordinator = null!;

    internal enum GameScreen
    {
        MainMenu,
        Playing
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        _speciesEditor = new SpeciesEditorPanel(new SpeciesEditorService(
            _speciesCatalogRuntime,
            Directory.GetCurrentDirectory(),
            "species.json",
            Path.GetDirectoryName(SpeciesCatalogRuntime.DefaultCatalogPath)!));
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _inputManager = new InputManager();
        _orchestrator = new SimulationOrchestrator(this);
        _coordinator = new GameLoopCoordinator(this);

        InitializeServices();
        InitializeGraphics();
        InitializeEcosystem();
        InitializeUI();

        Exiting += (_, _) => Logger.Flush();

        base.Initialize();
    }

    internal void InitializeServices()
    {
        _orchestrator.LoadSettings();
        _speciesCatalogRuntime.CatalogChanged += OnSpeciesCatalogChanged;
        var bundledCatalog = Path.Combine(Content.RootDirectory, "species.json");
        if (File.Exists(bundledCatalog))
        {
            var bundledErrors = _speciesCatalogRuntime.LoadAndApply("species.json", Content.RootDirectory, Directory.GetCurrentDirectory());
            if (bundledErrors.Count > 0)
                Logger.Warn($"Bundled species catalog: {bundledErrors.Count} validation error(s)");
        }
        IReadOnlyList<SpeciesCatalogValidationError> catalogErrors = _speciesCatalogRuntime.LoadAndApply(
            "species.json",
            Path.GetDirectoryName(SpeciesCatalogRuntime.DefaultCatalogPath)!,
            Directory.GetCurrentDirectory());
        foreach (SpeciesCatalogValidationError error in catalogErrors)
            Logger.Warn($"Species catalog [{error.EntryIndex}:{error.Field}] {error.Message}");
    }

    internal void InitializeGraphics()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();
    }

    internal void InitializeEcosystem()
    {
        _ecosystem = new Ecosystem(200, 150, 42);
        _ecosystem.Initialize(30, 8, 5, 100);
        _camera = new Camera(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)
        {
            WorldWidth = _ecosystem.World.PixelWidth,
            WorldHeight = _ecosystem.World.PixelHeight,
            Position = new Vector2(_ecosystem.World.PixelWidth / 2f, _ecosystem.World.PixelHeight / 2f)
        };
        _worldRenderer = new PixelWorldRenderer(_ecosystem.World);
        _creatureRenderer = new CreatureRenderer(_ecosystem);
        _minimap = new Minimap(_ecosystem, _camera);
        _controller = new SimulationController(_ecosystem, _dayNight);
        _waterEffect = new WaterEffect();
    }

    internal void InitializeUI()
    {
        _inGameUi.World = _ecosystem.World;
        _inGameUi.Climate = _ecosystem.Climate;
        _inGameUi.ToolbarButtonClicked += () =>
        {
            if (_cataclysmPanel.IsOpen) _cataclysmPanel.Close();
            if (_spawnPanel.IsOpen) _spawnPanel.Close();
            if (_speciesEditor.IsOpen) _speciesEditor.Close();
        };
    }

    protected override void LoadContent()
    {
        PitLife.Core.FeedingConfig.Load();
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Font");
        _uiPixel = new Texture2D(GraphicsDevice, 1, 1);
        _uiPixel.SetData([Color.White]);
        _worldRenderer.LoadContent(GraphicsDevice);
        _creatureRenderer.LoadContent(GraphicsDevice);
        _minimap.LoadContent(GraphicsDevice);
        _spawnPanel.SetIconTexture(LoadTexture(AssetRegistry.SpawnIcon));

        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.Fallbacks);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.SpeciesTextures);
        _creatureRenderer.LoadGenderedFromRegistry(GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);

        _logo = LoadTexture("Content/logo.png");
        _contentLoaded = true;
    }

    private Texture2D? LoadTexture(string path)
    {
        try { if (File.Exists(path)) return Texture2D.FromFile(GraphicsDevice, path); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load texture '{path}': {ex.Message}");
        }
        return null;
    }

    protected override void Update(GameTime gameTime)
    {
        _coordinator.Update(gameTime);
        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        _coordinator.Draw(gameTime);
        base.Draw(gameTime);
    }

    internal static Creature? FindClosestCreature(
        IEnumerable<Creature> creatures,
        Vector2 worldPosition,
        float selectionRadius = 30f)
    {
        Creature? closest = null;
        var closestDistance = selectionRadius;
        foreach (Creature creature in creatures)
        {
            if (!creature.IsAlive)
                continue;

            var distance = Vector2.Distance(worldPosition, creature.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = creature;
            }
        }
        return closest;
    }

    internal static void SaveLanguagePref()
    {
        try { File.WriteAllText("settings.json", System.Text.Json.JsonSerializer.Serialize(new { language = I18n.CurrentLanguage })); }
        catch (Exception ex) { Logger.Error($"Failed to save settings: {ex.Message}"); }
    }

    private void OnSpeciesCatalogChanged()
    {
        _spawnPanel.RefreshSpeciesCatalog();
        if (!_contentLoaded)
            return;

        var customAssets = new List<SpeciesAsset>();
        foreach (var asset in AssetRegistry.SpeciesTextures)
        {
            if (_speciesCatalogRuntime.CustomKeys.Contains(asset.Species))
            {
                customAssets.Add(asset);
            }
        }
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, customAssets);
    }
}
