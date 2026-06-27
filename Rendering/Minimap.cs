using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Simulation;

namespace PitLife.Rendering;

/// <summary>
/// Represents the Minimap.
/// </summary>
public class Minimap
{
    public const int Size = 160;
    public const int Margin = 10;

    private readonly Ecosystem _ecosystem;
    private readonly Camera _camera;
    private Texture2D? _pixelTexture;
    private Color[]? _terrainBuffer;

    /// <summary>
    /// Initializes a new instance of the Minimap.
    /// </summary>
    /// <param name="ecosystem">The ecosystem parameter.</param>
    /// <param name="camera">The camera parameter.</param>
    public Minimap(Ecosystem ecosystem, Camera camera)
    {
        _ecosystem = ecosystem;
        _camera = camera;
    }

    /// <summary>
    /// Executes the LoadContent.
    /// </summary>
    /// <param name="gd">The gd parameter.</param>
    public void LoadContent(GraphicsDevice gd)
    {
        _pixelTexture = new Texture2D(gd, 1, 1);
        _pixelTexture.SetData([Color.White]);
        RebuildTerrainBuffer();
    }

    /// <summary>
    /// Executes the RebuildTerrainBuffer.
    /// </summary>
    public void RebuildTerrainBuffer()
    {
        var worldW = _ecosystem.World.PixelWidth;
        var worldH = _ecosystem.World.PixelHeight;
        _terrainBuffer = new Color[worldW * worldH];

        for (var y = 0; y < worldH; y++)
        {
            for (var x = 0; x < worldW; x++)
            {
                var tile = _ecosystem.World.GetTileAtPosition(x, y);
                _terrainBuffer[y * worldW + x] = GetBiomeColor(tile.Biome);
            }
        }
    }

    /// <summary>
    /// Executes the Draw.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="viewportWidth">The viewportWidth parameter.</param>
    /// <param name="viewportHeight">The viewportHeight parameter.</param>
    public void Draw(SpriteBatch sb, int viewportWidth, int viewportHeight)
    {
        if (_pixelTexture == null || _terrainBuffer == null) return;

        var x = viewportWidth - Size - Margin;
        var y = viewportHeight - Size - Margin - 52;

        var borderRect = new Rectangle(x - 2, y - 2, Size + 4, Size + 4);
        UiPrimitivesHelper.Fill(sb, _pixelTexture, borderRect, new Color(11, 23, 18, 235));
        UiPrimitivesHelper.Border(sb, _pixelTexture, borderRect, 2, new Color(107, 81, 55));

        DrawTerrain(sb, x, y);
        DrawCreatures(sb, x, y);
        DrawViewportRect(sb, x, y);
    }

    /// <summary>
    /// Executes the GetBounds.
    /// </summary>
    /// <param name="viewportWidth">The viewportWidth parameter.</param>
    /// <param name="viewportHeight">The viewportHeight parameter.</param>
    /// <returns>Returns the Rectangle result.</returns>
    public Rectangle GetBounds(int viewportWidth, int viewportHeight)
    {
        var x = viewportWidth - Size - Margin;
        var y = viewportHeight - Size - Margin - 52;
        return new Rectangle(x, y, Size, Size);
    }

    /// <summary>
    /// Executes the HandleClick.
    /// </summary>
    /// <param name="mouse">The mouse parameter.</param>
    /// <param name="viewportWidth">The viewportWidth parameter.</param>
    /// <param name="viewportHeight">The viewportHeight parameter.</param>
    /// <returns>Returns the bool result.</returns>
    public bool HandleClick(MouseState mouse, int viewportWidth, int viewportHeight)
    {
        var bounds = GetBounds(viewportWidth, viewportHeight);
        if (!bounds.Contains(mouse.Position)) return false;
        MoveCameraToMinimap(mouse.Position, bounds);
        return true;
    }

    /// <summary>
    /// Executes the HandleDrag.
    /// </summary>
    /// <param name="mouse">The mouse parameter.</param>
    /// <param name="viewportWidth">The viewportWidth parameter.</param>
    /// <param name="viewportHeight">The viewportHeight parameter.</param>
    /// <returns>Returns the bool result.</returns>
    public bool HandleDrag(MouseState mouse, int viewportWidth, int viewportHeight)
    {
        var bounds = GetBounds(viewportWidth, viewportHeight);
        if (!bounds.Contains(mouse.Position)) return false;
        MoveCameraToMinimap(mouse.Position, bounds);
        return true;
    }

    private void MoveCameraToMinimap(Point mousePos, Rectangle bounds)
    {
        float worldW = _ecosystem.World.PixelWidth;
        var scaleX = Size / worldW;
        float localX = mousePos.X - bounds.X;
        var worldX = localX / scaleX;
        _camera.Position = new Vector2(worldX, _camera.Position.Y);

        float worldH = _ecosystem.World.PixelHeight;
        var scaleY = Size / worldH;
        float localY = mousePos.Y - bounds.Y;
        var worldY = localY / scaleY;
        _camera.Position = new Vector2(_camera.Position.X, worldY);
    }

    private void DrawTerrain(SpriteBatch sb, int x, int y)
    {
        if (_terrainBuffer == null) return;
        var worldW = _ecosystem.World.PixelWidth;
        var worldH = _ecosystem.World.PixelHeight;
        var scaleX = Size / (float)worldW;
        var scaleY = Size / (float)worldH;

        for (var py = 0; py < Size; py++)
        {
            var srcY = (int)(py / scaleY);
            if (srcY >= worldH) break;
            for (var px = 0; px < Size; px++)
            {
                var srcX = (int)(px / scaleX);
                if (srcX >= worldW) break;
                sb.Draw(_pixelTexture, new Rectangle(x + px, y + py, 1, 1), _terrainBuffer[srcY * worldW + srcX]);
            }
        }
    }

    private void DrawCreatures(SpriteBatch sb, int x, int y)
    {
        var worldW = _ecosystem.World.PixelWidth;
        var worldH = _ecosystem.World.PixelHeight;
        var scaleX = Size / (float)worldW;
        var scaleY = Size / (float)worldH;

        foreach (var creature in _ecosystem.Creatures)
        {
            if (creature == null || !creature.IsAlive) continue;
            var px = x + (int)(creature.Position.X * scaleX);
            var py = y + (int)(creature.Position.Y * scaleY);
            Color color = GetCreatureColor(creature.CreatureType);
            sb.Draw(_pixelTexture, new Rectangle(px - 1, py - 1, 2, 2), color);
        }
    }

    private void DrawViewportRect(SpriteBatch sb, int x, int y)
    {
        if (_pixelTexture == null) return;
        var worldW = _ecosystem.World.PixelWidth;
        var worldH = _ecosystem.World.PixelHeight;
        var scaleX = Size / (float)worldW;
        var scaleY = Size / (float)worldH;

        var halfW = _camera.ViewportWidth / (2f * _camera.Zoom);
        var halfH = _camera.ViewportHeight / (2f * _camera.Zoom);

        var rx = x + (int)((_camera.Position.X - halfW) * scaleX);
        var ry = y + (int)((_camera.Position.Y - halfH) * scaleY);
        var rw = Math.Max(4, (int)(halfW * 2 * scaleX));
        var rh = Math.Max(4, (int)(halfH * 2 * scaleY));

        var rect = new Rectangle(rx, ry, rw, rh);
        UiPrimitivesHelper.Border(sb, _pixelTexture, rect, 1, new Color(255, 255, 255, 180));
    }

    private static Color GetBiomeColor(BiomeType biome) => biome switch
    {
        BiomeType.DeepOcean => new Color(28, 60, 110),
        BiomeType.ShallowWater => new Color(58, 118, 168),
        BiomeType.Beach => new Color(220, 200, 140),
        BiomeType.Desert => new Color(220, 190, 110),
        BiomeType.Savanna => new Color(200, 190, 110),
        BiomeType.Grassland => new Color(120, 180, 80),
        BiomeType.Forest => new Color(60, 130, 60),
        BiomeType.DenseForest => new Color(35, 95, 45),
        BiomeType.Swamp => new Color(80, 100, 70),
        BiomeType.Tundra => new Color(180, 185, 170),
        BiomeType.Mountain => new Color(120, 110, 100),
        BiomeType.Snow => new Color(235, 240, 245),
        BiomeType.CoralReef => new Color(0, 180, 160),
        BiomeType.Cave => new Color(60, 50, 45),
        BiomeType.Volcano => new Color(170, 50, 20),
        _ => Color.Magenta
    };

    // Static accessor for testing - returns colors in biome order
    /// <summary>
    /// Executes the GetBiomeColors.
    /// </summary>
    /// <returns>Returns the Color[] result.</returns>
    public static Color[] GetBiomeColors() =>
    [
        new Color(28, 60, 110),    // DeepOcean
        new Color(58, 118, 168),   // ShallowWater
        new Color(220, 200, 140),  // Beach
        new Color(220, 190, 110),  // Desert
        new Color(200, 190, 110),  // Savanna
        new Color(120, 180, 80),   // Grassland
        new Color(60, 130, 60),    // Forest
        new Color(35, 95, 45),     // DenseForest
        new Color(80, 100, 70),    // Swamp
        new Color(180, 185, 170),   // Tundra
        new Color(120, 110, 100),  // Mountain
        new Color(235, 240, 245),  // Snow
        new Color(0, 180, 160),    // CoralReef
        new Color(70, 60, 50),     // Cave
        new Color(170, 50, 20),    // Volcano
    ];

    private static Color GetCreatureColor(CreatureType type) => type switch
    {
        CreatureType.Plant => new Color(120, 200, 80),
        CreatureType.Herbivore => new Color(78, 156, 181),
        CreatureType.Carnivore => new Color(200, 90, 74),
        CreatureType.Omnivore => new Color(242, 230, 201),
        _ => Color.White
    };
}

internal static class UiPrimitivesHelper
{
    /// <summary>
    /// Executes the Fill.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="pixel">The pixel parameter.</param>
    /// <param name="r">The r parameter.</param>
    /// <param name="c">The c parameter.</param>
    public static void Fill(SpriteBatch sb, Texture2D pixel, Rectangle r, Color c) =>
        sb.Draw(pixel, r, c);

    /// <summary>
    /// Executes the Border.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="pixel">The pixel parameter.</param>
    /// <param name="r">The r parameter.</param>
    /// <param name="thickness">The thickness parameter.</param>
    /// <param name="c">The c parameter.</param>
    public static void Border(SpriteBatch sb, Texture2D pixel, Rectangle r, int thickness, Color c)
    {
        sb.Draw(pixel, new Rectangle(r.X, r.Y, r.Width, thickness), c);
        sb.Draw(pixel, new Rectangle(r.X, r.Y + r.Height - thickness, r.Width, thickness), c);
        sb.Draw(pixel, new Rectangle(r.X, r.Y, thickness, r.Height), c);
        sb.Draw(pixel, new Rectangle(r.X + r.Width - thickness, r.Y, thickness, r.Height), c);
    }
}
