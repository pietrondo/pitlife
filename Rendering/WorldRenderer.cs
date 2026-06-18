using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

public class WorldRenderer
{
    private readonly World _world;
    private Texture2D? _pixel;

    private Texture2D? _tileOcean;
    private Texture2D? _tileSand;
    private Texture2D? _tileGrass;
    private Texture2D? _tileForest;
    private Texture2D? _tileMountain;
    private Texture2D? _tileDesert;
    private Texture2D? _tileDirt;

    private static readonly Color[] BiomeColors =
    {
        new(100, 180, 60),
        new(40, 120, 30),
        new(210, 190, 130),
        new(50, 100, 200),
        new(140, 130, 120),
    };

    public WorldRenderer(World world)
    {
        _world = world;
    }

    public void LoadContent(GraphicsDevice graphicsDevice)
    {
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void SetTileTextures(
        Texture2D? ocean, Texture2D? sand, Texture2D? grass,
        Texture2D? forest, Texture2D? mountain, Texture2D? desert,
        Texture2D? dirt)
    {
        _tileOcean = ocean;
        _tileSand = sand;
        _tileGrass = grass;
        _tileForest = forest;
        _tileMountain = mountain;
        _tileDesert = desert;
        _tileDirt = dirt;
    }

    private Texture2D? TextureFor(BiomeType biome) => biome switch
    {
        BiomeType.Water => _tileOcean,
        BiomeType.Desert => _tileDesert ?? _tileSand,
        BiomeType.Grassland => _tileGrass,
        BiomeType.Forest => _tileForest,
        BiomeType.Mountain => _tileMountain,
        _ => null
    };

    public void Draw(SpriteBatch spriteBatch, Camera camera)
    {
        Rectangle visible = camera.VisibleArea;
        int startX = Math.Max(0, visible.X / _world.TileSize - 1);
        int startY = Math.Max(0, visible.Y / _world.TileSize - 1);
        int endX = Math.Min(_world.Width, (visible.X + visible.Width) / _world.TileSize + 2);
        int endY = Math.Min(_world.Height, (visible.Y + visible.Height) / _world.TileSize + 2);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var tile = _world.Tiles[x, y];
                Rectangle dest = new(x * _world.TileSize, y * _world.TileSize, _world.TileSize, _world.TileSize);

                var tex = TextureFor(tile.Biome);
                if (tex != null)
                {
                    spriteBatch.Draw(tex, dest, Color.White);
                }
                else
                {
                    Color color = BiomeColors[(int)tile.Biome];
                    spriteBatch.Draw(_pixel, dest, color);
                }
            }
        }
    }
}
