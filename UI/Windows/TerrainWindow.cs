using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI.Windows;

public class TerrainWindow : UiWindow
{
    public TerrainWindow(string title, string id) : base(title, id)
    {
    }

    public void DrawContent(SpriteBatch spriteBatch, SpriteFont font, World? world, Point? selectedTile)
    {
        var content = ContentBounds;
        DrawTerrain(spriteBatch, font, content, world, selectedTile);
    }

    private void DrawTerrain(
        SpriteBatch spriteBatch,
        SpriteFont font,
        Rectangle content,
        World? world,
        Point? selectedTile)
    {
        if (world == null || selectedTile == null)
        {
            DrawLine(spriteBatch, font, content.X, content.Y, I18n.T("terrain.none"), UiTheme.MutedStone);
            DrawLine(spriteBatch, font, content.X, content.Y + 24, I18n.T("terrain.selectHint"), UiTheme.MutedStone);
            return;
        }

        Point p = selectedTile.Value;
        var x = MathHelper.Clamp(p.X, 0, world.Width - 1);
        var y = MathHelper.Clamp(p.Y, 0, world.Height - 1);

        Tile tile = world.Tiles[x, y];
        var elevation = world.ElevationField[y * world.Width + x];
        var elevationM = (elevation - 0.15f) / 0.85f * 4000f;
        var isRiver = world.RiverMask[y * world.Width + x];

        var passStr = I18n.T(tile.IsPassable ? "common.yes" : "common.no");
        var riverStr = I18n.T(isRiver ? "common.yes" : "common.no");
        var biomeName = I18n.T($"biome.{tile.Biome}");

        DrawLine(spriteBatch, font, content.X, content.Y, I18n.Format("terrain.heading", x, y), UiTheme.MossSignal);
        DrawLine(spriteBatch, font, content.X, content.Y + 28, I18n.Format("terrain.biome", biomeName), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 50, I18n.Format("terrain.elevation", elevationM), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 72, I18n.Format("terrain.passable", passStr), UiTheme.WarmParchment);
        DrawLine(spriteBatch, font, content.X, content.Y + 94, I18n.Format("terrain.river", riverStr), UiTheme.WarmParchment);
    }

    private void DrawLine(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text, Color color)
    {
        spriteBatch.DrawString(font, text, new Vector2(x, y), color);
    }
}
