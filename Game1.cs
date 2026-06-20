using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;
using PitLife.Localization;

namespace PitLife;

public class Game1 : Game
{
    private readonly GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch = null!;
    private Ecosystem _ecosystem = null!;
    private Camera _camera = null!;
    private WorldRenderer _worldRenderer = null!;
    private CreatureRenderer _creatureRenderer = null!;
    private DayNightCycle _dayNight = new();
    private Minimap _minimap = null!;
    private SpriteFont _font = null!;
    private Texture2D? _logo;
    private Texture2D _uiPixel = null!;
    private readonly MainMenu _mainMenu = new();
    private readonly InGameUi _inGameUi = new();
    private readonly SpawnPanel _spawnPanel = new();
    private GameScreen _screen = GameScreen.MainMenu;
    private float _menuInputCooldown = 0.75f;

    private int _displayPlants, _displayHerbivores, _displayCarnivores, _displayOmnivores;
    private float _displayTime;
    private bool _paused;
    private SimulationController _controller = null!;

    private Creature? _selectedCreature;
    private MouseState _prevMouse;
    private KeyboardState _prevKbd;

    private enum GameScreen
    {
        MainMenu,
        Playing
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
    }

    protected override void Initialize()
    {
        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 800;
        _graphics.ApplyChanges();

        _ecosystem = new Ecosystem(200, 150, 42);
        _ecosystem.Initialize(60, 20, 15, 150);
        _camera = new Camera(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight)
        {
            WorldWidth = _ecosystem.World.PixelWidth,
            WorldHeight = _ecosystem.World.PixelHeight,
            Position = new Vector2(_ecosystem.World.PixelWidth / 2f, _ecosystem.World.PixelHeight / 2f)
        };
        _worldRenderer = new WorldRenderer(_ecosystem.World);
        _creatureRenderer = new CreatureRenderer(_ecosystem);
        _minimap = new Minimap(_ecosystem, _camera);
        _controller = new SimulationController(_ecosystem, _dayNight);

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

        _worldRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.Biomes);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.Fallbacks);
        _creatureRenderer.LoadFromRegistry(GraphicsDevice, AssetRegistry.SpeciesTextures);
        _creatureRenderer.LoadGenderedFromRegistry(GraphicsDevice, AssetRegistry.GenderedSpeciesTextures);

        _logo = LoadTexture("Content/assets/logo.png");
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
        var kbd = Keyboard.GetState();
        var mouse = Mouse.GetState();
        bool escapePressed = kbd.IsKeyDown(Keys.Escape) && _prevKbd.IsKeyUp(Keys.Escape);
        bool gamepadBack = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;

        _camera.ViewportWidth = GraphicsDevice.Viewport.Width;
        _camera.ViewportHeight = GraphicsDevice.Viewport.Height;

        if (_screen == GameScreen.MainMenu)
        {
            _ecosystem.SimulationSpeed = 0.35f;
            _ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(dt), TimeSpan.FromSeconds(dt)));
            _dayNight.Update(_ecosystem.TotalTime);
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
                case MenuAction.ToggleFullscreen:
                    _graphics.ToggleFullScreen();
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

        if (escapePressed && _inGameUi.CloseTopWindow())
        {
            _prevKbd = kbd;
            _prevMouse = mouse;
            base.Update(gameTime);
            return;
        }

        if (escapePressed || gamepadBack)
        {
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

        _spawnPanel.SetViewportHeight(GraphicsDevice.Viewport.Height);
        if (kbd.IsKeyDown(Keys.F4) && !_prevKbd.IsKeyDown(Keys.F4))
        {
            _spawnPanel.Toggle();
        }
        bool spawnPanelConsumed = _spawnPanel.Update(mouse, _prevMouse);

        _camera.HandleInput(dt);
        if (kbd.IsKeyDown(Keys.D1) && !_prevKbd.IsKeyDown(Keys.D1)) _controller.SetSpeed(1);
        if (kbd.IsKeyDown(Keys.D2) && !_prevKbd.IsKeyDown(Keys.D2)) _controller.SetSpeed(2);
        if (kbd.IsKeyDown(Keys.D3) && !_prevKbd.IsKeyDown(Keys.D3)) _controller.SetSpeed(3);
        if (kbd.IsKeyDown(Keys.Space) && !_prevKbd.IsKeyDown(Keys.Space)) _controller.TogglePause();
        _prevKbd = kbd;

        _controller.Advance(dt);
        _paused = _controller.IsPaused;
        _displayPlants = _controller.PlantCount;
        _displayHerbivores = _controller.HerbivoreCount;
        _displayCarnivores = _controller.CarnivoreCount;
        _displayOmnivores = _controller.OmnivoreCount;
        _displayTime = _controller.TotalTime;

        if (_spawnPanel.IsOpen && _spawnPanel.SelectedSpecies != null &&
            mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var spawnPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            _ecosystem.SpawnByName(_spawnPanel.SelectedSpecies, spawnPos);
        }
        else if (!pointerOverUi && !spawnPanelConsumed &&
                 mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
        {
            var worldPos = _camera.ScreenToWorld(mouse.X, mouse.Y);
            Creature? closest = null;
            float closestDist = 30f;
            foreach (var c in _ecosystem.Creatures)
            {
                if (c == null || !c.IsAlive) continue;
                float d = Vector2.Distance(worldPos, c.Position);
                if (d < closestDist) { closestDist = d; closest = c; }
            }
            _selectedCreature = closest;
            if (_selectedCreature != null)
                _inGameUi.OpenCreatureWindow();
        }
        _prevMouse = mouse;

        base.Update(gameTime);
    }

    private static Color GetPhaseColor(DayPhase phase) => phase switch
    {
        DayPhase.Dawn => new Color(255, 180, 100),
        DayPhase.Day => Color.White,
        DayPhase.Dusk => new Color(255, 140, 80),
        DayPhase.Night => new Color(120, 140, 220),
        _ => Color.White
    };

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _worldRenderer.Draw(_spriteBatch, _camera);
        _creatureRenderer.Draw(_spriteBatch, _camera);
        if (_screen == GameScreen.Playing && _selectedCreature != null && _selectedCreature.IsAlive)
        {
            var center = _selectedCreature.Position;
            _spriteBatch.DrawString(_font, "X", center - new Vector2(8, 14), Color.Yellow);
        }
        _spriteBatch.End();

        var overlay = _dayNight.GetOverlayColor();
        if (overlay.A > 0)
        {
            _spriteBatch.Begin();
            _spriteBatch.Draw(_uiPixel, new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height), overlay);
            _spriteBatch.End();
        }

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
            _spriteBatch.End();
            base.Draw(gameTime);
            return;
        }

        string speed = _paused ? I18n.T("hud.paused") : $"{_controller.CurrentSpeed}x";
        string hud = I18n.Format("hud.summary", _displayTime, _displayPlants, _displayHerbivores,
            _displayCarnivores, _displayOmnivores, speed);
        _spriteBatch.DrawString(_font, hud, new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, I18n.T("hud.controls"),
            new Vector2(10, 32), new Color(160, 160, 160));
        string phaseLabel = I18n.T($"dayphase.{_dayNight.Phase.ToString().ToLowerInvariant()}");
        _spriteBatch.DrawString(_font, phaseLabel, new Vector2(10, 54), GetPhaseColor(_dayNight.Phase));

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
            GraphicsDevice.Viewport.Height);

        _camera.ViewportWidth = GraphicsDevice.Viewport.Width;
        _camera.ViewportHeight = GraphicsDevice.Viewport.Height;
        _minimap.Draw(_spriteBatch, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height);
        _spawnPanel.Draw(_spriteBatch, _uiPixel, _font, Mouse.GetState());
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
