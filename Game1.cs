using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;
using PitLife.Core;

namespace PitLife;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Ecosystem _ecosystem = null!;
    private Camera _camera = null!;
    private PixelWorldRenderer _worldRenderer = null!;
    private CreatureRenderer _creatureRenderer = null!;
    private DayNightCycle _dayNight = new();
    private Minimap _minimap = null!;
    private SpriteFont _font = null!;
    private Texture2D? _logo;
    private Texture2D _uiPixel = null!;
    private readonly MainMenu _mainMenu = new();
    private readonly HelpScreen _helpScreen = new();
    private readonly InGameUi _inGameUi = new();
    private readonly SpawnPanel _spawnPanel = new();
    private readonly CataclysmPanel _cataclysmPanel = new();
    private readonly SpeciesCatalogRuntime _speciesCatalogRuntime = new();
    private readonly SpeciesEditorPanel _speciesEditor;
    private float _currentFPS;
    private float _frametimeMS;
    private int _frameCount;
    private double _fpsTimer;
    private bool _showDebugOverlay;
    private bool _contentLoaded;
    private GameScreen _screen = GameScreen.MainMenu;
    private float _menuInputCooldown = 0.75f;

    private int _displayPlants, _displayHerbivores, _displayCarnivores, _displayOmnivores;
    private float _displayTime;
    private bool _paused;
    private SimulationController _controller = null!;

    private Creature? _selectedCreature;
    private MouseState _prevMouse;
    private KeyboardState _prevKbd;
    private int _cataSelectedFrame;
    private string? _prevPanelCata;
    private string? _prevSpawnCata;
    private string? _prevSpawnSpecies;
    private int _gameFrame;

    private enum GameScreen
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
            SpeciesCatalogRuntime.DefaultCatalogPath));
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        LoadSettings();
        _speciesCatalogRuntime.CatalogChanged += OnSpeciesCatalogChanged;
        string bundledCatalog = Path.Combine(Content.RootDirectory, "species.json");
        if (File.Exists(bundledCatalog))
        {
            var bundledErrors = _speciesCatalogRuntime.LoadAndApply(bundledCatalog, Directory.GetCurrentDirectory());
            if (bundledErrors.Count > 0)
                Logger.Warn($"Bundled species catalog: {bundledErrors.Count} validation error(s)");
        }
        IReadOnlyList<SpeciesCatalogValidationError> catalogErrors = _speciesCatalogRuntime.LoadAndApply(
            SpeciesCatalogRuntime.DefaultCatalogPath,
            Directory.GetCurrentDirectory());
        foreach (SpeciesCatalogValidationError error in catalogErrors)
            Logger.Warn($"Species catalog [{error.EntryIndex}:{error.Field}] {error.Message}");

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();

        _ecosystem = new Ecosystem(400, 300, 42);
        _ecosystem.Initialize(50, 15, 10, 200);
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
        _inGameUi.World = _ecosystem.World;

        base.Initialize();
    }

    protected override void LoadContent()
    {
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

        _logo = LoadTexture("Content/assets/logo.png");
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
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        UpdateFPS(gameTime);
        var kbd = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool escapePressed = kbd.IsKeyDown(Keys.Escape) && _prevKbd.IsKeyUp(Keys.Escape);
        bool gamepadBack = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;

        _camera.ViewportWidth = GraphicsDevice.Viewport.Width;
        _camera.ViewportHeight = GraphicsDevice.Viewport.Height;

        if (_screen == GameScreen.MainMenu)
        {
            if (!_mainMenu.IsWorldGenPanelOpen)
            {
                _ecosystem.SimulationSpeed = 0.35f;
                _ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(dt), TimeSpan.FromSeconds(dt)));
                _dayNight.Update(_ecosystem.TotalTime);
            }

            if (_helpScreen.IsActive)
            {
                _helpScreen.Update(
                    mouse,
                    _prevMouse,
                    kbd,
                    _prevKbd,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height);
                if (gamepadBack)
                    _helpScreen.Hide();
                _prevKbd = kbd;
        _prevMouse = mouse;
                base.Update(gameTime);
                return;
            }

            _menuInputCooldown = Math.Max(0f, _menuInputCooldown - dt);
            MenuAction action = _menuInputCooldown > 0f
                ? MenuAction.None
                : _mainMenu.Update(
                    mouse,
                    _prevMouse,
                    kbd,
                    _prevKbd,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height,
                    _graphics.IsFullScreen);

            switch (action)
            {
                case MenuAction.StartGame:
                    _screen = GameScreen.Playing;
                    _paused = false;
                    _controller.SetPause(false);
                    break;
                case MenuAction.NewWorld:
                    _mainMenu.CloseWorldGenPanel();
                    GenerateNewWorld(null, _mainMenu.CurrentOptions);
                    _screen = GameScreen.Playing;
                    _paused = false;
                    _controller.SetPause(false);
                    break;
                case MenuAction.NewWorldWithSeed:
                    _mainMenu.CloseWorldGenPanel();
                    GenerateNewWorld(_mainMenu.Seed, _mainMenu.CurrentOptions);
                    _screen = GameScreen.Playing;
                    _paused = false;
                    _controller.SetPause(false);
                    break;
                case MenuAction.SaveGame:
                    SaveSystem.Save("savegame.json", _ecosystem);
                    _menuInputCooldown = 0.5f;
                    break;
                case MenuAction.LoadGame:
                    try
                    {
                        var saveData = SaveSystem.Load("savegame.json");
                        if (saveData != null)
                        {
                            RestoreLoadedEcosystem(saveData);
                            _screen = GameScreen.Playing;
                            _paused = false;
                            _controller.SetPause(false);
                        }
                    }
                    catch (InvalidDataException ex)
                    {
                        Logger.Error($"Failed to load save: {ex.Message}");
                    }
                    _menuInputCooldown = 0.5f;
                    break;
                case MenuAction.ToggleFullscreen:
                    _graphics.ToggleFullScreen();
                    _menuInputCooldown = 0.5f;
                    break;
                case MenuAction.ShowHelp:
                    _helpScreen.Show();
                    break;
                case MenuAction.Exit:
                    Exit();
                    break;
            }

            if (gamepadBack)
                Exit();

            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (kbd.IsKeyDown(Keys.F1) && _prevKbd.IsKeyUp(Keys.F1))
            _showDebugOverlay = !_showDebugOverlay;
        if (kbd.IsKeyDown(Keys.F7) && _prevKbd.IsKeyUp(Keys.F7))
            _ecosystem.Cataclysms.TriggerManual(_ecosystem, _ecosystem.Random);
        if (kbd.IsKeyDown(Keys.F6) && _prevKbd.IsKeyUp(Keys.F6))
            _speciesEditor.Toggle();

        if (_speciesEditor.IsOpen)
        {
            if (escapePressed)
                _speciesEditor.Close();
            else
                _speciesEditor.Update(mouse, _prevMouse, kbd, _prevKbd,
                    GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);

            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (escapePressed && _inGameUi.CloseTopWindow())
        {
            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (escapePressed || gamepadBack)
        {
            _mainMenu.CloseWorldGenPanel();
            _screen = GameScreen.MainMenu;
            _paused = true;
            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        bool pointerOverUi = _inGameUi.Update(
            mouse,
            _prevMouse,
            kbd,
            _prevKbd,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height);

        if (_inGameUi.WantsToGoToMainMenu)
        {
            _inGameUi.WantsToGoToMainMenu = false;
            _mainMenu.CloseWorldGenPanel();
            _screen = GameScreen.MainMenu;
            _paused = true;
            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        _spawnPanel.SetViewportHeight(GraphicsDevice.Viewport.Height);
        if (kbd.IsKeyDown(Keys.F8) && !_prevKbd.IsKeyDown(Keys.F8))
            _cataclysmPanel.Toggle();
        if (kbd.IsKeyDown(Keys.F4) && !_prevKbd.IsKeyDown(Keys.F4))
        {
            _spawnPanel.Toggle();
        }
        bool spawnPanelConsumed = _spawnPanel.Update(mouse, _prevMouse, kbd, _prevKbd);
        bool cataConsumed = _cataclysmPanel.Update(mouse, _prevMouse);
        spawnPanelConsumed = spawnPanelConsumed || _spawnPanel.HandleCataclysmClick(mouse, _prevMouse);

        // ── Mutual exclusion: only one mode active ────────────
        bool spawnJustSelected = _spawnPanel.SelectedCataclysm != null && _spawnPanel.SelectedCataclysm != _prevSpawnCata;
        bool speciesJustSelected = _spawnPanel.SelectedSpeciesKey != null && _spawnPanel.SelectedSpeciesKey != _prevSpawnSpecies;
        bool panelJustSelected = _cataclysmPanel.SelectedType != null && _cataclysmPanel.SelectedType != _prevPanelCata;

        if (spawnJustSelected || speciesJustSelected)
        {
            _prevSpawnCata = _spawnPanel.SelectedCataclysm;
            _prevSpawnSpecies = _spawnPanel.SelectedSpeciesKey;
            if (spawnJustSelected) _cataSelectedFrame = _gameFrame;
            _cataclysmPanel.SelectedType = null;
            _prevPanelCata = null;
        }
        if (panelJustSelected)
        {
            _prevPanelCata = _cataclysmPanel.SelectedType;
            _cataSelectedFrame = _gameFrame;
            _spawnPanel.SelectedCataclysm = null;
            _spawnPanel.DeselectSpecies();
            _prevSpawnCata = null;
            _prevSpawnSpecies = null;
        }
        if (_cataclysmPanel.SelectedType == null && !_cataclysmPanel.IsOpen) _prevPanelCata = null;
        if (_spawnPanel.SelectedCataclysm == null) _prevSpawnCata = null;

        // ESC: close panel if open, otherwise cancel active mode
        if (kbd.IsKeyDown(Keys.Escape) && _prevKbd.IsKeyUp(Keys.Escape))
        {
            if (_cataclysmPanel.IsOpen)
                _cataclysmPanel.Toggle();
            else if (_spawnPanel.IsOpen)
                _spawnPanel.Toggle();
            if (_cataclysmPanel.SelectedType != null || _spawnPanel.SelectedCataclysm != null || _spawnPanel.SelectedSpeciesKey != null)
            {
                _spawnPanel.SelectedCataclysm = null;
                _spawnPanel.DeselectSpecies();
                _cataclysmPanel.SelectedType = null;
                _prevPanelCata = null;
                _prevSpawnCata = null;
                _prevSpawnSpecies = null;
                _cataSelectedFrame = 0;
            }
        }

        _camera.HandleInput(dt);
        if (kbd.IsKeyDown(Keys.Up) && _prevKbd.IsKeyUp(Keys.Up))
            _controller.SetSpeed(Math.Min(3, _controller.SpeedLevel + 1));
        if (kbd.IsKeyDown(Keys.Down) && _prevKbd.IsKeyUp(Keys.Down))
            _controller.SetSpeed(Math.Max(0, _controller.SpeedLevel - 1));
        if (kbd.IsKeyDown(Keys.D1) && !_prevKbd.IsKeyDown(Keys.D1)) _controller.SetSpeed(1);
        if (kbd.IsKeyDown(Keys.D2) && !_prevKbd.IsKeyDown(Keys.D2)) _controller.SetSpeed(2);
        if (kbd.IsKeyDown(Keys.D3) && !_prevKbd.IsKeyDown(Keys.D3)) _controller.SetSpeed(3);
        if (kbd.IsKeyDown(Keys.Space) && !_prevKbd.IsKeyDown(Keys.Space)) _controller.TogglePause();
        if (_inGameUi.SpeedUpRequested)
        {
            _controller.SetSpeed(Math.Min(3, _controller.SpeedLevel + 1));
            _inGameUi.SpeedUpRequested = false;
        }
        if (_inGameUi.SpeedDownRequested)
        {
            _controller.SetSpeed(Math.Max(0, _controller.SpeedLevel - 1));
            _inGameUi.SpeedDownRequested = false;
        }
        _prevKbd = kbd;

        _controller.Advance(dt);
        _paused = _controller.IsPaused;
        _displayPlants = _controller.PlantCount;
        _displayHerbivores = _controller.HerbivoreCount;
        _displayCarnivores = _controller.CarnivoreCount;
        _displayOmnivores = _controller.OmnivoreCount;
        _displayTime = _controller.TotalTime;

        // Cataclysm placement when selected (require at least 1 frame delay after selection)
        bool cataReady = _cataSelectedFrame > 0 && (_gameFrame - _cataSelectedFrame) >= 1;
        if (_cataclysmPanel.SelectedType != null && cataReady &&
            !pointerOverUi && !spawnPanelConsumed && !cataConsumed &&
            mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var catPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            _ecosystem.Cataclysms.TriggerAt(_ecosystem, _ecosystem.Random, _cataclysmPanel.SelectedType!, catPos);
            _worldRenderer.Invalidate();
            _cataclysmPanel.SelectedType = null;
            _prevPanelCata = null;
            _cataSelectedFrame = 0;
        }

        // Spawn creature only when panel is open, species selected, click is NOT on any panel
        if (_spawnPanel.IsOpen && _spawnPanel.SelectedSpeciesKey != null &&
            !spawnPanelConsumed && !cataConsumed && !pointerOverUi &&
            mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var spawnPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            int spawned = 0;
            for (int i = 0; i < 3; i++)
            {
                var offset = new Vector2((float)(_ecosystem.Random.NextDouble() - 0.5) * 40,
                    (float)(_ecosystem.Random.NextDouble() - 0.5) * 40);
                if (_ecosystem.SpawnByName(_spawnPanel.SelectedSpeciesKey, spawnPos + offset))
                    spawned++;
            }
            if (spawned > 0)
            {
                Logger.Event("SPAWN", $"Player spawned {spawned}x {_spawnPanel.SelectedSpeciesKey} at ({spawnPos.X:F0}, {spawnPos.Y:F0})");
                _spawnPanel.DeselectSpecies();
                _prevSpawnSpecies = null;
            }
            else
            {
                var tile = _ecosystem.World.GetTileAtPosition(spawnPos.X, spawnPos.Y);
                Logger.Warn($"Spawn failed for {_spawnPanel.SelectedSpeciesKey} at ({spawnPos.X:F0}, {spawnPos.Y:F0}) - biome={tile.Biome}. Try a different location.");
                // Keep species selected so player can try another spot
            }
        }
        else if (!pointerOverUi && !spawnPanelConsumed && !cataConsumed && cataReady &&
            _spawnPanel.SelectedCataclysm != null &&
            mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var catPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            _ecosystem.Cataclysms.TriggerAt(_ecosystem, _ecosystem.Random, _spawnPanel.SelectedCataclysm, catPos);
            _worldRenderer.Invalidate();
            _spawnPanel.SelectedCataclysm = null;
            _prevSpawnCata = null;
            _cataSelectedFrame = 0;
        }
        else if (!pointerOverUi && !spawnPanelConsumed && !cataConsumed &&
                 mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var worldPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            _selectedCreature = FindClosestCreature(_ecosystem.Creatures, worldPos);
            if (_selectedCreature != null)
            {
                _inGameUi.OpenCreatureWindow(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }
            else
            {
                int tileX = (int)(worldPos.X / _ecosystem.World.TileSize);
                int tileY = (int)(worldPos.Y / _ecosystem.World.TileSize);
                tileX = Math.Clamp(tileX, 0, _ecosystem.World.Width - 1);
                tileY = Math.Clamp(tileY, 0, _ecosystem.World.Height - 1);
                _inGameUi.SelectedTile = new Point(tileX, tileY);
                _inGameUi.OpenTerrainWindow(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
            }
        }
        _prevMouse = mouse;
        _gameFrame++;

        base.Update(gameTime);
    }

    internal static Creature? FindClosestCreature(
        IEnumerable<Creature> creatures,
        Vector2 worldPosition,
        float selectionRadius = 30f)
    {
        Creature? closest = null;
        float closestDistance = selectionRadius;
        foreach (Creature creature in creatures)
        {
            if (!creature.IsAlive)
                continue;

            float distance = Vector2.Distance(worldPosition, creature.Position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = creature;
            }
        }
        return closest;
    }

    private void GenerateNewWorld(int? seedOverride, Simulation.WorldGenOptions? worldGenOptions = null)
    {
        int seed = seedOverride ?? new Random().Next();
        var wgOpts = worldGenOptions ?? Simulation.WorldGenOptions.Pangea() with { MapWidth = 400, MapHeight = 300 };
        _ecosystem = new Ecosystem(wgOpts, seed);
        _ecosystem.Initialize(60, 20, 15, 150);
        _worldRenderer = new PixelWorldRenderer(_ecosystem.World);
        _creatureRenderer = new CreatureRenderer(_ecosystem);
        _minimap = new Minimap(_ecosystem, _camera);
        _controller = new SimulationController(_ecosystem, _dayNight);
        ResetWorldSessionState();
        
        _worldRenderer.LoadContent(GraphicsDevice);
        _creatureRenderer.LoadContent(GraphicsDevice);
        _minimap.LoadContent(GraphicsDevice);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.Fallbacks);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.SpeciesTextures);
        _creatureRenderer.LoadGenderedFromRegistry(GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);
    }

    private void RestoreLoadedEcosystem(SaveSystem.SaveData data)
    {
        _ecosystem = new Ecosystem(data.WorldWidth, data.WorldHeight, data.Seed);
        _ecosystem.TotalTime = data.TotalTime;

        foreach (var cData in data.Creatures)
        {
            var def = SpeciesRegistry.Get(cData.Species);
            if (def == null) continue;

            var genome = new Genome
            {
                Speed = cData.Genome.Speed,
                Size = cData.Genome.Size,
                Metabolism = cData.Genome.Metabolism,
                VisionRange = cData.Genome.VisionRange,
                Color = new Color(cData.Genome.ColorR, cData.Genome.ColorG, cData.Genome.ColorB),
                MutationRate = cData.Genome.MutationRate,
                DesertAdaptation = cData.Genome.DesertAdaptation,
                ColdAdaptation = cData.Genome.ColdAdaptation,
                ForestAdaptation = cData.Genome.ForestAdaptation,
                WaterAdaptation = cData.Genome.WaterAdaptation,
                Genetics = cData.Genome.Genetics ?? default
            };

            Creature c = (Creature)Activator.CreateInstance(
                def.CreatureType,
                new Vector2(cData.PositionX, cData.PositionY),
                genome,
                def.Species)!;

            c.Energy = cData.Energy;
            c.Gender = cData.Gender;
            c.Facing = new Vector2(cData.FacingX, cData.FacingY);
            c.GrowFor(cData.Age);
            c.RestoreGeneticHistory(
                LineageRecord.Restore(
                    cData.IndividualId,
                    cData.ParentAId,
                    cData.ParentBId,
                    cData.AncestorDepths),
                cData.InbreedingCoefficient);

            _ecosystem.AddCreature(c);
        }

        _ecosystem.FlushPending();
        _ecosystem.UpdateStats();

        _worldRenderer = new PixelWorldRenderer(_ecosystem.World);
        _creatureRenderer = new CreatureRenderer(_ecosystem);
        _minimap = new Minimap(_ecosystem, _camera);
        _controller = new SimulationController(_ecosystem, _dayNight);
        ResetWorldSessionState();

        _worldRenderer.LoadContent(GraphicsDevice);
        _creatureRenderer.LoadContent(GraphicsDevice);
        _minimap.LoadContent(GraphicsDevice);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.Fallbacks);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.SpeciesTextures);
        _creatureRenderer.LoadGenderedFromRegistry(GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);
    }

    private void ResetWorldSessionState()
    {
        _camera.WorldWidth = _ecosystem.World.PixelWidth;
        _camera.WorldHeight = _ecosystem.World.PixelHeight;
        _camera.Zoom = 1f;
        _camera.Position = new Vector2(
            _ecosystem.World.PixelWidth / 2f,
            _ecosystem.World.PixelHeight / 2f);
        _selectedCreature = null;
        _spawnPanel.Close();
        _inGameUi.ResetForWorld(_ecosystem.World);
        _prevSpawnSpecies = null;
        _displayPlants = _ecosystem.PlantCount;
        _displayHerbivores = _ecosystem.HerbivoreCount;
        _displayCarnivores = _ecosystem.CarnivoreCount;
        _displayOmnivores = _ecosystem.OmnivoreCount;
        _displayTime = _ecosystem.TotalTime;
        _dayNight.Update(_ecosystem.TotalTime);
    }

    private static Color GetPhaseColor(DayPhase phase) => phase switch
    {
        DayPhase.Dawn => new Color(255, 180, 100),
        DayPhase.Day => Color.White,
        DayPhase.Dusk => new Color(255, 140, 80),
        DayPhase.Night => new Color(120, 140, 220),
        _ => Color.White
    };

    private static Color GetSeasonColor(Season season) => season switch
    {
        Season.Spring => new Color(140, 220, 100),
        Season.Summer => new Color(255, 200, 60),
        Season.Autumn => new Color(220, 140, 40),
        Season.Winter => new Color(160, 200, 240),
        _ => Color.White
    };

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        // Apply screen shake from cataclysms
        var shake = _ecosystem.Cataclysms.ScreenShake;
        Vector2 savedPos = _camera.Position;
        if (shake != Vector2.Zero)
            _camera.Position = new Vector2(savedPos.X + shake.X, savedPos.Y + shake.Y);

        // Draw the map with Point Clamp (for crisp pixel art matching the minimap)
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _worldRenderer.Draw(_spriteBatch, _camera);
        //_ecosystem.Flow?.DrawOverlay(_spriteBatch, _ecosystem.World.TileSize);
        _spriteBatch.End();

        // Draw the creatures with Point Clamp (for crisp pixel art)
        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _creatureRenderer.Draw(_spriteBatch, _camera, _dayNight.GetOverlayColor(), _font);
        if (_screen == GameScreen.Playing && _selectedCreature != null && _selectedCreature.IsAlive)
        {
            var center = _selectedCreature.Position;
            _spriteBatch.DrawString(_font, "X", center - new Vector2(8, 14), Color.Yellow);
        }
        // Draw cataclysm animations (meteor, fire, frost, etc.)
        _ecosystem.Cataclysms.Draw(_spriteBatch, _uiPixel);
        _spriteBatch.End();

        // Restore camera position
        _camera.Position = savedPos;

        _spriteBatch.Begin();
        if (_screen == GameScreen.MainMenu)
        {
            _mainMenu.Draw(
                _spriteBatch,
                _uiPixel,
                _font,
                _logo,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            _helpScreen.Draw(
                _spriteBatch,
                _uiPixel,
                _font,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height);
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        string speed = _paused ? I18n.T("hud.paused") : $"{_controller.CurrentSpeed}x";
        float years = _displayTime / 480f + 1;
        string hud = $"Year {years:F1} | P:{_displayPlants} H:{_displayHerbivores} C:{_displayCarnivores} O:{_displayOmnivores} | {speed}";
        _spriteBatch.DrawString(_font, hud, new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, I18n.T("hud.controls"),
            new Vector2(10, 32), new Color(160, 160, 160));
        string phaseLabel = I18n.T($"dayphase.{_dayNight.Phase.ToString().ToLowerInvariant()}");
        _spriteBatch.DrawString(_font, phaseLabel, new Vector2(10, 54), GetPhaseColor(_dayNight.Phase));
        string seasonLabel = I18n.T($"season.{_ecosystem.Climate.CurrentSeason}");
        _spriteBatch.DrawString(_font, seasonLabel, new Vector2(120, 54), GetSeasonColor(_ecosystem.Climate.CurrentSeason));
        string seedLabel = $"Seed: {_ecosystem.Seed}";
        _spriteBatch.DrawString(_font, seedLabel, new Vector2(10, 76), UiTheme.WarmParchment);

        DrawDebugOverlay(_spriteBatch, _font);

        if (_logo != null)
        {
            const int logoSize = 96;
            _spriteBatch.Draw(_logo,
                new Rectangle(GraphicsDevice.Viewport.Width - logoSize - 10, 10, logoSize, logoSize),
                Color.White);
        }

        _inGameUi.Draw(
            _spriteBatch,
            _uiPixel,
            _font,
            Mouse.GetState(),
            _selectedCreature,
            _displayPlants,
            _displayHerbivores,
            _displayCarnivores,
            _displayOmnivores,
            _displayTime,
            _paused,
            _controller.CurrentSpeed,
            GraphicsDevice.Viewport.Height,
            _ecosystem.Metrics);

        _camera.ViewportWidth = GraphicsDevice.Viewport.Width;
        _camera.ViewportHeight = GraphicsDevice.Viewport.Height;
        _minimap.Draw(_spriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _spawnPanel.Draw(_spriteBatch, _uiPixel, _font, Mouse.GetState());
        _cataclysmPanel.Draw(_spriteBatch, _uiPixel, _font, Mouse.GetState());
        _speciesEditor.Draw(
            _spriteBatch,
            _uiPixel,
            _font,
            Mouse.GetState(),
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height);
        _spriteBatch.End();

        base.Draw(gameTime);
    }

    private void LoadSettings()
    {
        try
        {
            if (File.Exists("settings.json"))
            {
                var json = File.ReadAllText("settings.json");
                var doc = System.Text.Json.JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("language", out var lang))
                    I18n.SetLanguage(lang.GetString() ?? "it");
            }
        }
        catch { }
    }

    private void SaveSettings()
    {
        try
        {
            var settings = new { language = I18n.CurrentLanguage, fullscreen = _graphics.IsFullScreen };
            File.WriteAllText("settings.json", System.Text.Json.JsonSerializer.Serialize(settings));
        }
        catch { }
    }

    internal static void SaveLanguagePref()
    {
        try { File.WriteAllText("settings.json", System.Text.Json.JsonSerializer.Serialize(new { language = I18n.CurrentLanguage })); }
        catch { }
    }

    private void OnSpeciesCatalogChanged()
    {
        _spawnPanel.RefreshSpeciesCatalog();
        if (!_contentLoaded)
            return;

        var customAssets = AssetRegistry.SpeciesTextures
            .Where(asset => _speciesCatalogRuntime.CustomKeys.Contains(asset.Species));
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, customAssets);
    }

    private void UpdateFPS(GameTime gameTime)
    {
        _frameCount++;
        _fpsTimer += gameTime.ElapsedGameTime.TotalSeconds;
        if (_fpsTimer >= 0.5)
        {
            _currentFPS = _frameCount / (float)_fpsTimer;
            _frametimeMS = (float)(_fpsTimer / _frameCount * 1000.0);
            _frameCount = 0;
            _fpsTimer = 0;
        }
    }

    private void DrawDebugOverlay(SpriteBatch sb, SpriteFont font)
    {
        var m = _ecosystem.Metrics;
        m.FPS = _currentFPS;
        int y = GraphicsDevice.Viewport.Height - 100;
        int x = 8;
        int lineH = 14;

        if (_ecosystem.Disease.HasOutbreak)
        {
            sb.DrawString(font, $"Disease: {_ecosystem.Disease.ActiveDiseaseName}",
                new Vector2(x, y), new Color(220, 60, 60));
            y += lineH;
        }
        if (_ecosystem.Cataclysms.IsActive)
        {
            sb.DrawString(font, $"{_ecosystem.Cataclysms.ActiveEvent} ({_ecosystem.Cataclysms.Timer:F0}s)",
                new Vector2(x, y), new Color(255, 140, 0));
            y += lineH;
        }

        if (!_showDebugOverlay) return;

        sb.DrawString(font, $"FPS:{_currentFPS:F0} B:{m.TotalBirths} D:{m.TotalDeaths} Starve:{m.StarvationDeaths} Old:{m.OldAgeDeaths} Pred:{m.PredationDeaths} Comb:{m.CombatDeaths}",
            new Vector2(x, y), UiTheme.MutedStone);
        y += lineH;
        sb.DrawString(font, $"Sp:{m.SpeciesCount} Het:{m.MeanHeterozygosity:F2} Inb:{m.MeanInbreeding:F2} Sub:{m.TotalSubspecies} Trophic:L1={m.TrophicLevel1}/L2={m.TrophicLevel2}/L3+={m.TrophicLevel3Plus}",
            new Vector2(x, y), UiTheme.MutedStone);
        y += lineH;
        sb.DrawString(font, $"O2:{_ecosystem.Atmosphere.Oxygen:F0}% CO2:{_ecosystem.Atmosphere.CO2:F0}%",
            new Vector2(x, y), UiTheme.MutedStone);
        if (m.LastDeathSpecies.Length > 0)
        {
            y += lineH;
            sb.DrawString(font, $"{m.LastDeathSpecies}: {m.LastDeathCause}",
                new Vector2(x, y), new Color(180, 120, 100));
        }
    }

    

    
}
