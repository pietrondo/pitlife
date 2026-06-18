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
        _ecosystem.Initialize(initialHerbivores: 40, initialCarnivores: 12, initialOmnivores: 10, initialPlants: 100);
        _camera = new Camera(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
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

        var deepOcean = LoadTexture("Content/assets/biome_deepocean.png") ?? LoadTexture("Content/assets/biome_water.png");
        var shallowWater = LoadTexture("Content/assets/biome_shallow.png") ?? LoadTexture("Content/assets/biome_sand.png");
        var beach = LoadTexture("Content/assets/biome_sand.png");
        var desert = LoadTexture("Content/assets/biome_desert.png");
        var savanna = LoadTexture("Content/assets/biome_savanna.png");
        var grassland = LoadTexture("Content/assets/biome_grass.png");
        var forest = LoadTexture("Content/assets/biome_forest.png");
        var dense = LoadTexture("Content/assets/biome_dense.png");
        var swamp = LoadTexture("Content/assets/biome_swamp.png");
        var tundra = LoadTexture("Content/assets/biome_tundra.png");
        var mountain = LoadTexture("Content/assets/biome_mountain.png");
        var snow = LoadTexture("Content/assets/biome_snow.png");
        _worldRenderer.SetTileTextures(
            deepOcean, shallowWater, beach, desert, savanna, grassland,
            forest, dense, swamp, tundra, mountain, snow);

        _creatureRenderer.SetPlantTexture(LoadTexture("Content/assets/plant.png"));
        _creatureRenderer.SetHerbivoreTexture(LoadTexture("Content/assets/herbivore.png"));
        _creatureRenderer.SetCarnivoreTexture(LoadTexture("Content/assets/carnivore.png"));
        _creatureRenderer.SetOmnivoreTexture(LoadTexture("Content/assets/omnivore.png"));

        _creatureRenderer.RegisterSpeciesTexture("Deer", LoadTexture("Content/assets/deer.png"));
        _creatureRenderer.RegisterSpeciesTexture("Rabbit", LoadTexture("Content/assets/rabbit.png"));
        _creatureRenderer.RegisterSpeciesTexture("Fox", LoadTexture("Content/assets/fox.png"));
        _creatureRenderer.RegisterSpeciesTexture("Boar", LoadTexture("Content/assets/boar.png"));
        _creatureRenderer.RegisterSpeciesTexture("Flowers", LoadTexture("Content/assets/flowers.png"));
        _creatureRenderer.RegisterSpeciesTexture("Mushroom", LoadTexture("Content/assets/mushroom.png"));
        _creatureRenderer.RegisterSpeciesTexture("Sheep", LoadTexture("Content/assets/sheep.png"));
        _creatureRenderer.RegisterSpeciesTexture("Lynx", LoadTexture("Content/assets/lynx.png"));
        _creatureRenderer.RegisterSpeciesTexture("Raccoon", LoadTexture("Content/assets/raccoon.png"));
        _creatureRenderer.RegisterSpeciesTexture("Tiger", LoadTexture("Content/assets/tiger.png"));
        _creatureRenderer.RegisterSpeciesTexture("GrassTuft", LoadTexture("Content/assets/grasstuft.png"));
        _creatureRenderer.RegisterSpeciesTexture("Cactus", LoadTexture("Content/assets/cactus.png"));
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
