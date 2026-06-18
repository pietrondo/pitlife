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

    private int _displayPlants;
    private int _displayHerbivores;
    private int _displayCarnivores;
    private int _displayOmnivores;
    private float _displayTime;

    private float _timeAccumulator;

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
            LoadTexture("Content/assets/biomes/biome_water.png"),    // DeepOcean
            LoadTexture("Content/assets/biomes/biome_snow.png"),     // ShallowWater (fallback snow/light)
            LoadTexture("Content/assets/biomes/biome_sand.png"),     // Beach
            LoadTexture("Content/assets/biomes/biome_desert.png"),   // Desert
            LoadTexture("Content/assets/biomes/biome_grass.png"),    // Savanna (questo e erba secca!)
            LoadTexture("Content/assets/biomes/biome_swamp.png"),    // Grassland (questo e prato!)
            LoadTexture("Content/assets/biomes/biome_dense.png"),    // Forest
            LoadTexture("Content/assets/biomes/biome_tundra.png"),   // DenseForest
            LoadTexture("Content/assets/biomes/biome_forest.png"),   // Swamp (questo e palude!)
            LoadTexture("Content/assets/biomes/biome_mountain.png"), // Tundra (fallback montagna)
            LoadTexture("Content/assets/biomes/biome_mountain.png"), // Mountain
            LoadTexture("Content/assets/biomes/biome_snow.png"));    // Snow

        _creatureRenderer.SetPlantTexture(LoadTexture("Content/assets/creatures/plants/plant.png"));
        _creatureRenderer.SetHerbivoreTexture(LoadTexture("Content/assets/creatures/herbivores/herbivore.png"));
        _creatureRenderer.SetCarnivoreTexture(LoadTexture("Content/assets/creatures/carnivores/carnivore.png"));
        _creatureRenderer.SetOmnivoreTexture(LoadTexture("Content/assets/creatures/omnivores/omnivore.png"));

        _creatureRenderer.RegisterSpeciesTexture("Deer", LoadTexture("Content/assets/creatures/herbivores/deer.png"));
        _creatureRenderer.RegisterSpeciesTexture("Rabbit", LoadTexture("Content/assets/creatures/herbivores/rabbit.png"));
        _creatureRenderer.RegisterSpeciesTexture("Fox", LoadTexture("Content/assets/creatures/carnivores/fox.png"));
        _creatureRenderer.RegisterSpeciesTexture("Boar", LoadTexture("Content/assets/creatures/omnivores/boar.png"));
        _creatureRenderer.RegisterSpeciesTexture("Flowers", LoadTexture("Content/assets/creatures/plants/flowers.png"));
        _creatureRenderer.RegisterSpeciesTexture("Mushroom", LoadTexture("Content/assets/creatures/plants/mushroom.png"));
        _creatureRenderer.RegisterSpeciesTexture("Sheep", LoadTexture("Content/assets/creatures/herbivores/sheep.png"));
        _creatureRenderer.RegisterSpeciesTexture("Lynx", LoadTexture("Content/assets/creatures/carnivores/lynx.png"));
        _creatureRenderer.RegisterSpeciesTexture("Raccoon", LoadTexture("Content/assets/creatures/omnivores/raccoon.png"));
        _creatureRenderer.RegisterSpeciesTexture("Tiger", LoadTexture("Content/assets/creatures/carnivores/tiger.png"));
        _creatureRenderer.RegisterSpeciesTexture("GrassTuft", LoadTexture("Content/assets/creatures/plants/grasstuft.png"));
        _creatureRenderer.RegisterSpeciesTexture("Cactus", LoadTexture("Content/assets/creatures/plants/cactus.png"));
        _creatureRenderer.RegisterSpeciesTexture("Horse", LoadTexture("Content/assets/creatures/herbivores/horse.png"));
        _creatureRenderer.RegisterSpeciesTexture("Goat", LoadTexture("Content/assets/creatures/herbivores/goat.png"));
    }

    private Texture2D? LoadTexture(string path)
    {
        try
        {
            if (File.Exists(path))
                return Texture2D.FromFile(GraphicsDevice, path);
        }
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

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.Black);

        _spriteBatch.Begin(
            samplerState: SamplerState.PointClamp,
            transformMatrix: _camera.TransformMatrix
        );
        _worldRenderer.Draw(_spriteBatch, _camera);
        _creatureRenderer.Draw(_spriteBatch, _camera);
        _spriteBatch.End();

        _spriteBatch.Begin();
        string hud = $"Time: {_displayTime:F1}s    " +
                     $"Plants: {_displayPlants}  " +
                     $"Herbivores: {_displayHerbivores}  " +
                     $"Carnivores: {_displayCarnivores}  " +
                     $"Omnivores: {_displayOmnivores}";
        _spriteBatch.DrawString(_font, hud, new Vector2(10, 10), Color.White);
        _spriteBatch.DrawString(_font, "WASD/Arrows: move   Scroll: zoom   ESC: quit",
            new Vector2(10, 32), new Color(180, 180, 180));
        _spriteBatch.End();

        base.Draw(gameTime);
    }
}
