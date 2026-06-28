using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

/// <summary>
/// Represents the WorldRenderer.
/// </summary>
public class WorldRenderer
{
    private readonly World _world;
    private Texture2D? _pixel;
    private int _borderWidth = 5;

    private Texture2D? _texDeepOcean;
    private Texture2D? _texShallowWater;
    private Texture2D? _texBeach;
    private Texture2D? _texDesert;
    private Texture2D? _texSavanna;
    private Texture2D? _texGrassland;
    private Texture2D? _texForest;
    private Texture2D? _texDenseForest;
    private Texture2D? _texSwamp;
    private Texture2D? _texTundra;
    private Texture2D? _texMountain;
    private Texture2D? _texSnow;

    private static readonly Color[] BiomeColors =
    {
        new(15, 40, 120),    // DeepOcean
        new(50, 100, 200),   // ShallowWater
        new(220, 200, 150),  // Beach
        new(210, 180, 120),  // Desert
        new(180, 190, 80),   // Savanna
        new(100, 180, 60),   // Grassland
        new(40, 130, 40),    // Forest
        new(20, 90, 30),     // DenseForest
        new(90, 100, 60),    // Swamp
        new(140, 130, 110),  // Tundra
        new(140, 120, 100),  // Mountain
        new(230, 235, 240),  // Snow
    };

    private static readonly Color[] EdgeColors =
    {
        new(50, 80, 160),    // DeepOcean edge
        new(100, 150, 220),  // ShallowWater edge
        new(235, 220, 180),  // Beach edge
        new(230, 210, 160),  // Desert edge
        new(200, 210, 120),  // Savanna edge
        new(170, 210, 130),  // Grassland edge
        new(100, 170, 90),   // Forest edge
        new(70, 130, 70),    // DenseForest edge
        new(130, 140, 100),  // Swamp edge
        new(170, 160, 140),  // Tundra edge
        new(170, 160, 130),  // Mountain edge
        new(240, 245, 250),  // Snow edge
    };

    /// <summary>
    /// Initializes a new instance of the WorldRenderer.
    /// </summary>
    /// <param name="world">The world parameter.</param>
    public WorldRenderer(World world) => _world = world;

    /// <summary>
    /// Executes the LoadContent.
    /// </summary>
    /// <param name="gd">The gd parameter.</param>
    public void LoadContent(GraphicsDevice gd)
    {
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData([Color.White]);
    }

    /// <summary>
    /// Executes the SetTileTextures.
    /// </summary>
    /// <param name="ocean">The ocean parameter.</param>
    /// <param name="shallow">The shallow parameter.</param>
    /// <param name="beach">The beach parameter.</param>
    /// <param name="desert">The desert parameter.</param>
    /// <param name="savanna">The savanna parameter.</param>
    /// <param name="grass">The grass parameter.</param>
    /// <param name="forest">The forest parameter.</param>
    /// <param name="dense">The dense parameter.</param>
    /// <param name="swamp">The swamp parameter.</param>
    /// <param name="tundra">The tundra parameter.</param>
    /// <param name="mountain">The mountain parameter.</param>
    /// <param name="snow">The snow parameter.</param>
    public void SetTileTextures(
        Texture2D? ocean, Texture2D? shallow, Texture2D? beach,
        Texture2D? desert, Texture2D? savanna, Texture2D? grass,
        Texture2D? forest, Texture2D? dense, Texture2D? swamp,
        Texture2D? tundra, Texture2D? mountain, Texture2D? snow)
    {
        _texDeepOcean = ocean;
        _texShallowWater = shallow;
        _texBeach = beach;
        _texDesert = desert;
        _texSavanna = savanna;
        _texGrassland = grass;
        _texForest = forest;
        _texDenseForest = dense;
        _texSwamp = swamp;
        _texTundra = tundra;
        _texMountain = mountain;
        _texSnow = snow;
    }

    /// <summary>
    /// Executes the LoadFromRegistry.
    /// </summary>
    /// <param name="gd">The gd parameter.</param>
    /// <param name="assets">The assets parameter.</param>
    public void LoadFromRegistry(GraphicsDevice gd, IEnumerable<SpeciesAsset> assets)
    {
        foreach (var a in assets)
        {
            var tex = LoadTexture(gd, a.Path);
            switch (a.Species)
            {
                case nameof(BiomeType.DeepOcean): _texDeepOcean = tex; break;
                case nameof(BiomeType.ShallowWater): _texShallowWater = tex; break;
                case nameof(BiomeType.Beach): _texBeach = tex; break;
                case nameof(BiomeType.Desert): _texDesert = tex; break;
                case nameof(BiomeType.Savanna): _texSavanna = tex; break;
                case nameof(BiomeType.Grassland): _texGrassland = tex; break;
                case nameof(BiomeType.Forest): _texForest = tex; break;
                case nameof(BiomeType.DenseForest): _texDenseForest = tex; break;
                case nameof(BiomeType.Swamp): _texSwamp = tex; break;
                case nameof(BiomeType.Tundra): _texTundra = tex; break;
                case nameof(BiomeType.Mountain): _texMountain = tex; break;
                case nameof(BiomeType.Snow): _texSnow = tex; break;
            }
        }
    }

    private static Texture2D? LoadTexture(GraphicsDevice gd, string path)
    {
        try { if (File.Exists(path)) return Texture2D.FromFile(gd, path); }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Unable to load texture '{path}': {ex.Message}");
        }
        return null;
    }

    private Texture2D? TextureFor(BiomeType b) => b switch
    {
        BiomeType.DeepOcean => _texDeepOcean,
        BiomeType.ShallowWater => _texShallowWater,
        BiomeType.Beach => _texBeach,
        BiomeType.Desert => _texDesert,
        BiomeType.Savanna => _texSavanna,
        BiomeType.Grassland => _texGrassland,
        BiomeType.Forest => _texForest,
        BiomeType.DenseForest => _texDenseForest,
        BiomeType.Swamp => _texSwamp,
        BiomeType.Tundra => _texTundra,
        BiomeType.Mountain => _texMountain,
        BiomeType.Snow => _texSnow,
        _ => null
    };

    /// <summary>
    /// Executes the Draw.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="camera">The camera parameter.</param>
    public void Draw(SpriteBatch sb, Camera camera)
    {
        Rectangle v = camera.VisibleArea;
        var ts = _world.TileSize;
        var sx = Math.Max(0, v.X / ts - 1);
        var sy = Math.Max(0, v.Y / ts - 1);
        var ex = Math.Min(_world.Width, (v.X + v.Width) / ts + 2);
        var ey = Math.Min(_world.Height, (v.Y + v.Height) / ts + 2);

        // Pass 1: Draw base biome textures
        for (var y = sy; y < ey; y++)
        {
            for (var x = sx; x < ex; x++)
            {
                var t = _world.Tiles[x, y];
                BiomeType b = t.Biome;
                var r = new Rectangle(x * ts, y * ts, ts, ts);

                var tex = TextureFor(b);
                if (tex != null)
                    sb.Draw(tex, r, Color.White);
                else
                    sb.Draw(_pixel, r, BiomeColors[(int)b]);
            }
        }

        // Pass 2: Draw borders using only the pixel texture to prevent texture swaps
        for (var y = sy; y < ey; y++)
        {
            for (var x = sx; x < ex; x++)
            {
                var t = _world.Tiles[x, y];
                BiomeType b = t.Biome;

                var (top, bot, l, r_) = GetNB(x, y);

                if (top != b) sb.Draw(_pixel, new Rectangle(x * ts, y * ts, ts, _borderWidth), EdgeColors[(int)top] * 0.40f);
                if (bot != b) sb.Draw(_pixel, new Rectangle(x * ts, (y + 1) * ts - _borderWidth, ts, _borderWidth), EdgeColors[(int)bot] * 0.40f);
                if (l != b) sb.Draw(_pixel, new Rectangle(x * ts, y * ts, _borderWidth, ts), EdgeColors[(int)l] * 0.40f);
                if (r_ != b) sb.Draw(_pixel, new Rectangle((x + 1) * ts - _borderWidth, y * ts, _borderWidth, ts), EdgeColors[(int)r_] * 0.40f);
            }
        }
    }

    private (BiomeType t, BiomeType b, BiomeType l, BiomeType r) GetNB(int x, int y) => (
        _world.GetTile(x, y - 1).Biome,
        _world.GetTile(x, y + 1).Biome,
        _world.GetTile(x - 1, y).Biome,
        _world.GetTile(x + 1, y).Biome
    );
}
