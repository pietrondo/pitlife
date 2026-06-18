using System;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Rendering;
using PitLife.Simulation;

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

    private int _displayPlants, _displayHerbivores, _displayCarnivores, _displayOmnivores;
    private float _displayTime;
    private float _timeAccumulator;
    private bool _paused;
    private int _speedLevel = 1;
    private static readonly float[] SpeedLevels = [0f, 1f, 2f, 4f];

    private Creature? _selectedCreature;
    private MouseState _prevMouse;
    private KeyboardState _prevKbd;

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
            WorldHeight = _ecosystem.World.PixelHeight
        };
        _worldRenderer = new WorldRenderer(_ecosystem.World);
        _creatureRenderer = new CreatureRenderer(_ecosystem);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("Font");
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
    }

    private Texture2D? LoadTexture(string path)
    {
        try { if (File.Exists(path)) return Texture2D.FromFile(GraphicsDevice, path); }
        catch { }
        return null;
    }

    protected override void Update(GameTime gameTime)
    {
        if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
            Keyboard.GetState().IsKeyDown(Keys.Escape))
            Exit();

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        _camera.HandleInput(dt);

        var kbd = Keyboard.GetState();
        if (kbd.IsKeyDown(Keys.D1) && !_prevKbd.IsKeyDown(Keys.D1)) { _speedLevel = 1; _paused = false; }
        if (kbd.IsKeyDown(Keys.D2) && !_prevKbd.IsKeyDown(Keys.D2)) { _speedLevel = 2; _paused = false; }
        if (kbd.IsKeyDown(Keys.D3) && !_prevKbd.IsKeyDown(Keys.D3)) { _speedLevel = 3; _paused = false; }
        if (kbd.IsKeyDown(Keys.Space) && !_prevKbd.IsKeyDown(Keys.Space)) _paused = !_paused;
        _prevKbd = kbd;

        _ecosystem.SimulationSpeed = _paused ? 0f : SpeedLevels[_speedLevel];

        if (!_paused)
        {
            _timeAccumulator += dt * _ecosystem.SimulationSpeed;
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

        var mouse = Mouse.GetState();
        if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
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
        }
        _prevMouse = mouse;

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.TransformMatrix);
        _worldRenderer.Draw(_spriteBatch, _camera);
        _creatureRenderer.Draw(_spriteBatch, _camera);
        if (_selectedCreature != null && _selectedCreature.IsAlive)
        {
            float size = _selectedCreature.CreatureType == CreatureType.Plant ? 18f : 28f;
            float r = size * _selectedCreature.Genome.Size * 0.6f;
            var center = _selectedCreature.Position;
            _spriteBatch.DrawString(_font, "X", center - new Vector2(8, 14), Color.Yellow);
        }
        _spriteBatch.End();

        _spriteBatch.Begin();
        string speed = _paused ? "PAUSED" : $"{SpeedLevels[_speedLevel]}x";
        string hud = $"Time: {_displayTime:F1}s  |  Plants: {_displayPlants}  " +
                     $"Herbivores: {_displayHerbivores}  Carnivores: {_displayCarnivores}  " +
                     $"Omnivores: {_displayOmnivores}  |  Speed: {speed}";
        _spriteBatch.DrawString(_font, hud, new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, "WASD:move  Scroll:zoom  1(1x) 2(2x) 3(4x) Space:pause  Click:select  ESC:quit",
            new Vector2(10, 32), new Color(160, 160, 160));

        if (_selectedCreature != null && _selectedCreature.IsAlive)
        {
            var c = _selectedCreature;
            int px = 10, py = 55;
            _spriteBatch.DrawString(_font, $"[{c.Species}] {c.CreatureType}", new Vector2(px, py), Color.Yellow);
            _spriteBatch.DrawString(_font, $"Energy: {c.Energy:F1}/{c.MaxEnergy:F1}", new Vector2(px, py + 18), Color.White);
            _spriteBatch.DrawString(_font, $"Age: {c.Age:F1}s  Speed: {c.Genome.Speed:F2}", new Vector2(px, py + 36), Color.White);
            _spriteBatch.DrawString(_font, $"Size: {c.Genome.Size:F2}  Metabolism: {c.Genome.Metabolism:F2}", new Vector2(px, py + 54), Color.White);
            _spriteBatch.DrawString(_font, $"Vision: {c.Genome.VisionRange:F1}  Genome: #{c.Genome.Color.R:X2}{c.Genome.Color.G:X2}{c.Genome.Color.B:X2}", new Vector2(px, py + 72), Color.White);
        }
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
