using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

/// <summary>
/// Represents the WaterEffect.
/// </summary>
public sealed class WaterEffect
{
    private float _time;

    /// <summary>
    /// Executes the Update.
    /// </summary>
    /// <param name="dt">The dt parameter.</param>
    public void Update(float dt)
    {
        _time += dt;
    }

    /// <summary>
    /// Executes the Draw.
    /// </summary>
    /// <param name="sb">The sb parameter.</param>
    /// <param name="pixel">The pixel parameter.</param>
    /// <param name="world">The world parameter.</param>
    /// <param name="camera">The camera parameter.</param>
    public void Draw(SpriteBatch sb, Texture2D pixel, World world, Camera camera)
    {
        int tileSize = world.TileSize;
        int viewLeft = Math.Max(0, (int)(camera.Position.X - camera.ViewportWidth / 2f) / tileSize - 1);
        int viewTop = Math.Max(0, (int)(camera.Position.Y - camera.ViewportHeight / 2f) / tileSize - 1);
        int viewRight = Math.Min(world.Width, viewLeft + camera.ViewportWidth / tileSize + 3);
        int viewBottom = Math.Min(world.Height, viewTop + camera.ViewportHeight / tileSize + 3);

        for (int ty = viewTop; ty < viewBottom; ty++)
        {
            for (int tx = viewLeft; tx < viewRight; tx++)
            {
                var biome = world.Tiles[tx, ty].Biome;
                if (biome != BiomeType.DeepOcean && biome != BiomeType.ShallowWater && biome != BiomeType.CoralReef)
                    continue;

                float wave = MathF.Sin(tx * 0.7f + ty * 1.1f + _time * 2f) * 0.3f + 0.5f;
                var color = biome == BiomeType.CoralReef
                    ? new Color(30, 180, 200) * ((30 + wave * 40) / 255f)
                    : biome == BiomeType.ShallowWater
                        ? new Color(80, 160, 220) * ((15 + wave * 25) / 255f)
                        : new Color(40, 80, 200) * ((10 + wave * 20) / 255f);

                int px = tx * tileSize;
                int py = ty * tileSize;

                // Draw 2-3 wave pixels per tile
                int waveOffset = (int)(wave * (tileSize / 3));
                sb.Draw(pixel, new Rectangle(px + tileSize / 3 + waveOffset, py + tileSize / 2, 2, 1), color);
                sb.Draw(pixel, new Rectangle(px + tileSize * 2 / 3 - waveOffset, py + tileSize / 3, 1, 2), color);
                sb.Draw(pixel, new Rectangle(px + tileSize / 2, py + tileSize * 2 / 3 + waveOffset, 2, 1), color);
            }
        }
    }
}
