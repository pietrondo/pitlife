using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

public class WorldRenderer
{
    private readonly World _world;
    private Texture2D? _pixel;
    private int _borderWidth = 6;

    private Texture2D? _tileOcean;
    private Texture2D? _tileSand;
    private Texture2D? _tileGrass;
    private Texture2D? _tileForest;
    private Texture2D? _tileMountain;
    private Texture2D? _tileDesert;

    private static readonly Color[] BiomeColors =
    {
        new(100, 180, 60),
        new(40, 120, 30),
        new(210, 190, 130),
        new(50, 100, 200),
        new(140, 130, 120),
    };

    private static readonly Color[] EdgeColors =
    {
        new(170, 200, 130), // Grassland → edge
        new(100, 150, 70),  // Forest → edge
        new(230, 210, 160), // Desert → edge
        new(80, 120, 210),  // Water → edge
        new(170, 160, 150), // Mountain → edge
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
    }

    private static readonly BiomeType[] EdgeBiomes =
    {
        BiomeType.Water, BiomeType.Desert, BiomeType.Grassland,
        BiomeType.Forest, BiomeType.Mountain,
    };

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
        int ts = _world.TileSize;
        int startX = Math.Max(0, visible.X / ts - 1);
        int startY = Math.Max(0, visible.Y / ts - 1);
        int endX = Math.Min(_world.Width, (visible.X + visible.Width) / ts + 2);
        int endY = Math.Min(_world.Height, (visible.Y + visible.Height) / ts + 2);

        for (int y = startY; y < endY; y++)
        {
            for (int x = startX; x < endX; x++)
            {
                var tile = _world.Tiles[x, y];
                BiomeType b = tile.Biome;
                Rectangle dest = new(x * ts, y * ts, ts, ts);

                var tex = TextureFor(b);
                if (tex != null)
                    spriteBatch.Draw(tex, dest, Color.White);
                else
                    spriteBatch.Draw(_pixel, dest, BiomeColors[(int)b]);

                var nb = GetNeighborBiomes(x, y);

                if (nb.top != b)
                    spriteBatch.Draw(_pixel, new Rectangle(x * ts, y * ts, ts, _borderWidth),
                        EdgeColors[(int)nb.top] * 0.45f);
                if (nb.bottom != b)
                    spriteBatch.Draw(_pixel, new Rectangle(x * ts, (y + 1) * ts - _borderWidth, ts, _borderWidth),
                        EdgeColors[(int)nb.bottom] * 0.45f);
                if (nb.left != b)
                    spriteBatch.Draw(_pixel, new Rectangle(x * ts, y * ts, _borderWidth, ts),
                        EdgeColors[(int)nb.left] * 0.45f);
                if (nb.right != b)
                    spriteBatch.Draw(_pixel, new Rectangle((x + 1) * ts - _borderWidth, y * ts, _borderWidth, ts),
                        EdgeColors[(int)nb.right] * 0.45f);
            }
        }
    }

    private (BiomeType top, BiomeType bottom, BiomeType left, BiomeType right)
        GetNeighborBiomes(int x, int y)
    {
        var top = _world.GetTile(x, y - 1).Biome;
        var bottom = _world.GetTile(x, y + 1).Biome;
        var left = _world.GetTile(x - 1, y).Biome;
        var right = _world.GetTile(x + 1, y).Biome;
        return (top, bottom, left, right);
    }
}
