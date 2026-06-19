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
    private SpriteFont _font = null!;
    private Texture2D? _logo;
    private Texture2D _uiPixel = null!;
    private readonly MainMenu _mainMenu = new();
    private readonly InGameUi _inGameUi = new();
    private GameScreen _screen = GameScreen.MainMenu;
    private float _menuInputCooldown = 0.75f;

    private int _displayPlants, _displayHerbivores, _displayCarnivores, _displayOmnivores;
    private float _displayTime;
    private float _timeAccumulator;
    private bool _paused;
    private int _speedLevel = 1;
    private static readonly float[] SpeedLevels = [0f, 1f, 2f, 4f];

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

        _worldRenderer.SetTileTextures(
            LoadTexture("Content/assets/biomes/biome_water.png"),
            LoadTexture("Content/assets/biomes/biome_snow.png"),
            LoadTexture("Content/assets/biomes/biome_sand.png"),
            LoadTexture("Content/assets/biomes/biome_desert.png"),
            LoadTexture("Content/assets/biomes/biome_grass.png"),
            LoadTexture("Content/assets/biomes/biome_swamp.png"),
            LoadTexture("Content/assets/biomes/biome_dense.png"),
            LoadTexture("Content/assets/biomes/biome_tundra.png"),
            LoadTexture("Content/assets/biomes/biome_forest.png"),
            LoadTexture("Content/assets/biomes/biome_mountain.png"),
            LoadTexture("Content/assets/biomes/biome_mountain.png"),
            LoadTexture("Content/assets/biomes/biome_snow.png"));

        _creatureRenderer.SetPlantTexture(LoadTexture("Content/assets/creatures/plants/shrubs/plant.png"));
        _creatureRenderer.SetHerbivoreTexture(LoadTexture("Content/assets/creatures/mammals/herbivores/ungulates/herbivore.png"));
        _creatureRenderer.SetCarnivoreTexture(LoadTexture("Content/assets/creatures/mammals/carnivores/felids/carnivore.png"));
        _creatureRenderer.SetOmnivoreTexture(LoadTexture("Content/assets/creatures/mammals/omnivores/suids/omnivore.png"));

        _creatureRenderer.RegisterSpeciesTexture("Deer", LoadTexture("Content/assets/creatures/mammals/herbivores/ungulates/deer.png"));
        _creatureRenderer.RegisterSpeciesTexture("Rabbit", LoadTexture("Content/assets/creatures/mammals/herbivores/lagomorphs/rabbit.png"));
        _creatureRenderer.RegisterSpeciesTexture("Fox", LoadTexture("Content/assets/creatures/mammals/carnivores/canids/fox.png"));
        _creatureRenderer.RegisterSpeciesTexture("Boar", LoadTexture("Content/assets/creatures/mammals/omnivores/suids/boar.png"));
        _creatureRenderer.RegisterSpeciesTexture("Flowers", LoadTexture("Content/assets/creatures/plants/flowers/flowers.png"));
        _creatureRenderer.RegisterSpeciesTexture("Mushroom", LoadTexture("Content/assets/creatures/plants/fungi/mushroom.png"));
        _creatureRenderer.RegisterSpeciesTexture("Sheep", LoadTexture("Content/assets/creatures/mammals/herbivores/ungulates/sheep.png"));
        _creatureRenderer.RegisterSpeciesTexture("Lynx", LoadTexture("Content/assets/creatures/mammals/carnivores/felids/lynx.png"));
        _creatureRenderer.RegisterSpeciesTexture("Raccoon", LoadTexture("Content/assets/creatures/mammals/omnivores/procyonids/raccoon.png"));
        _creatureRenderer.RegisterSpeciesTexture("Tiger", LoadTexture("Content/assets/creatures/mammals/carnivores/felids/tiger.png"));
        _creatureRenderer.RegisterSpeciesTexture("GrassTuft", LoadTexture("Content/assets/creatures/plants/grasses/grasstuft.png"));
        _creatureRenderer.RegisterSpeciesTexture("Cactus", LoadTexture("Content/assets/creatures/plants/succulents/cactus.png"));
        _creatureRenderer.RegisterSpeciesTexture("Horse", LoadTexture("Content/assets/creatures/mammals/herbivores/ungulates/horse.png"));
        _creatureRenderer.RegisterSpeciesTexture("Goat", LoadTexture("Content/assets/creatures/mammals/herbivores/ungulates/goat.png"));
        _creatureRenderer.RegisterSpeciesTexture("Lion", LoadTexture("Content/assets/creatures/mammals/carnivores/felids/lion.png"));
        _creatureRenderer.RegisterSpeciesTexture("Leopard", LoadTexture("Content/assets/creatures/mammals/carnivores/felids/leopard.png"));
        _creatureRenderer.RegisterSpeciesTexture("Crocodile", LoadTexture("Content/assets/creatures/reptiles/crocodilians/crocodile.png"));
        _creatureRenderer.RegisterSpeciesTexture("Butterfly", LoadTexture("Content/assets/creatures/invertebrates/insects/butterfly.png"));
        _creatureRenderer.RegisterSpeciesTexture("Moss", LoadTexture("Content/assets/creatures/plants/grasses/moss.png"));
        _creatureRenderer.RegisterSpeciesTexture("BerryBush", LoadTexture("Content/assets/creatures/plants/shrubs/berrybush.png"));
        _creatureRenderer.RegisterSpeciesTexture("Pine", LoadTexture("Content/assets/creatures/plants/trees/pine.png"));
        _creatureRenderer.RegisterSpeciesTexture("Toadstool", LoadTexture("Content/assets/creatures/plants/fungi/toadstool.png"));
        _creatureRenderer.RegisterSpeciesTexture("Snake", LoadTexture("Content/assets/creatures/reptiles/squamates/snake.png"));
        _creatureRenderer.RegisterSpeciesTexture("Eagle", LoadTexture("Content/assets/creatures/birds/raptors/eagle.png"));
        _creatureRenderer.RegisterSpeciesTexture("Frog", LoadTexture("Content/assets/creatures/amphibians/anurans/frog.png"));
        _creatureRenderer.RegisterSpeciesTexture("Beetle", LoadTexture("Content/assets/creatures/invertebrates/insects/beetle.png"));
        _creatureRenderer.RegisterSpeciesTexture("Fish", LoadTexture("Content/assets/creatures/fish/fish.png"));
        _creatureRenderer.RegisterSpeciesTexture("Lizard", LoadTexture("Content/assets/creatures/reptiles/squamates/lizard.png"));
        _creatureRenderer.RegisterSpeciesTexture("Wolf", LoadTexture("Content/assets/creatures/mammals/carnivores/canids/wolf.png"));
        _creatureRenderer.RegisterSpeciesTexture("Bear", LoadTexture("Content/assets/creatures/mammals/omnivores/ursids/bear.png"));
        _creatureRenderer.RegisterSpeciesTexture("Turtle", LoadTexture("Content/assets/creatures/reptiles/testudines/turtle.png"));
        _creatureRenderer.RegisterSpeciesTexture("Shark", LoadTexture("Content/assets/creatures/fish/shark.png"));
        _creatureRenderer.RegisterSpeciesTexture("Piranha", LoadTexture("Content/assets/creatures/fish/piranha.png"));
        _creatureRenderer.RegisterSpeciesTexture("Salmon", LoadTexture("Content/assets/creatures/fish/salmon.png"));
        _creatureRenderer.RegisterSpeciesTexture("Jellyfish", LoadTexture("Content/assets/creatures/fish/jellyfish.png"));

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
            AdvanceSimulation(dt, 0.35f);
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

        _camera.HandleInput(dt);
        if (kbd.IsKeyDown(Keys.D1) && !_prevKbd.IsKeyDown(Keys.D1)) { _speedLevel = 1; _paused = false; }
        if (kbd.IsKeyDown(Keys.D2) && !_prevKbd.IsKeyDown(Keys.D2)) { _speedLevel = 2; _paused = false; }
        if (kbd.IsKeyDown(Keys.D3) && !_prevKbd.IsKeyDown(Keys.D3)) { _speedLevel = 3; _paused = false; }
        if (kbd.IsKeyDown(Keys.Space) && !_prevKbd.IsKeyDown(Keys.Space)) _paused = !_paused;
        _prevKbd = kbd;

        AdvanceSimulation(dt, _paused ? 0f : SpeedLevels[_speedLevel]);

        if (!pointerOverUi && mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
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

    private void AdvanceSimulation(float dt, float speed)
    {
        _ecosystem.SimulationSpeed = speed;
        if (speed <= 0f)
            return;

        _timeAccumulator += dt;
        while (_timeAccumulator >= 1f / 10f)
        {
            _ecosystem.Tick(new GameTime(TimeSpan.FromSeconds(1f / 10f), TimeSpan.FromSeconds(1f / 10f)));
            _timeAccumulator -= 1f / 10f;
            _displayPlants = _ecosystem.PlantCount;
            _displayHerbivores = _ecosystem.HerbivoreCount;
            _displayCarnivores = _ecosystem.CarnivoreCount;
            _displayOmnivores = _ecosystem.OmnivoreCount;
            _displayTime = _ecosystem.TotalTime;
        }
    }

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

        string speed = _paused ? I18n.T("hud.paused") : $"{SpeedLevels[_speedLevel]}x";
        string hud = I18n.Format("hud.summary", _displayTime, _displayPlants, _displayHerbivores,
            _displayCarnivores, _displayOmnivores, speed);
        _spriteBatch.DrawString(_font, hud, new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, I18n.T("hud.controls"),
            new Vector2(10, 32), new Color(160, 160, 160));

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
            SpeedLevels[_speedLevel],
            GraphicsDevice.Viewport.Height);
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
