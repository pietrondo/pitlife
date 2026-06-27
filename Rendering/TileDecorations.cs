using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using PitLife.Simulation;

namespace PitLife.Rendering;

/// <summary>
/// Represents the TileDecorations.
/// </summary>
public static class TileDecorations
{
    public enum DecorationType { None, Tree, Rock, Flower, Bush, Mushroom, Cactus, Coral }

    /// <summary>
    /// Executes the GetDecoration.
    /// </summary>
    /// <param name="biome">The biome parameter.</param>
    /// <param name="tileX">The tileX parameter.</param>
    /// <param name="tileY">The tileY parameter.</param>
    /// <param name="seed">The seed parameter.</param>
    /// <param name="vegetation">The vegetation parameter.</param>
    /// <returns>Returns the DecorationType result.</returns>
    public static DecorationType GetDecoration(BiomeType biome, int tileX, int tileY, int seed, float vegetation)
    {
        var rng = new Random(seed ^ (tileX * 73856093) ^ (tileY * 19349663));
        float roll = (float)rng.NextDouble();

        return biome switch
        {
            BiomeType.Forest or BiomeType.DenseForest => roll switch
            {
                < 0.15f when vegetation > 0.5f => DecorationType.Tree,
                < 0.25f when vegetation > 0.3f => DecorationType.Bush,
                < 0.30f => DecorationType.Flower,
                < 0.33f => DecorationType.Mushroom,
                _ => DecorationType.None
            },
            BiomeType.Grassland or BiomeType.Savanna => roll switch
            {
                < 0.05f when vegetation > 0.3f => DecorationType.Tree,
                < 0.12f => DecorationType.Bush,
                < 0.18f => DecorationType.Flower,
                _ => DecorationType.None
            },
            BiomeType.Swamp => roll switch
            {
                < 0.08f when vegetation > 0.4f => DecorationType.Tree,
                < 0.15f => DecorationType.Mushroom,
                < 0.20f => DecorationType.Bush,
                _ => DecorationType.None
            },
            BiomeType.Desert => roll switch
            {
                < 0.03f => DecorationType.Cactus,
                < 0.06f => DecorationType.Rock,
                _ => DecorationType.None
            },
            BiomeType.Mountain => roll switch
            {
                < 0.08f => DecorationType.Rock,
                < 0.12f => DecorationType.Tree,
                _ => DecorationType.None
            },
            BiomeType.Beach => roll < 0.04f ? DecorationType.Rock : DecorationType.None,
            BiomeType.Tundra => roll < 0.04f ? DecorationType.Rock : DecorationType.None,
            BiomeType.CoralReef => roll < 0.15f ? DecorationType.Coral : DecorationType.None,
            BiomeType.Cave => roll < 0.03f ? DecorationType.Rock : DecorationType.None,
            BiomeType.Volcano => roll < 0.06f ? DecorationType.Rock : DecorationType.None,
            _ => DecorationType.None
        };
    }

    /// <summary>
    /// Executes the GetColor.
    /// </summary>
    /// <param name="deco">The deco parameter.</param>
    /// <param name="tileX">The tileX parameter.</param>
    /// <param name="tileY">The tileY parameter.</param>
    /// <param name="seed">The seed parameter.</param>
    /// <returns>Returns the Color result.</returns>
    public static Color GetColor(DecorationType deco, int tileX, int tileY, int seed)
    {
        var rng = new Random(seed ^ (tileX * 19349663) ^ (tileY * 73856093));
        return deco switch
        {
            DecorationType.Tree => rng.Next(2) switch
            {
                0 => new Color(34, 139, 34),
                _ => new Color(0, 100, 0)
            },
            DecorationType.Rock => rng.Next(3) switch
            {
                0 => new Color(128, 128, 128),
                1 => new Color(105, 105, 105),
                _ => new Color(169, 169, 169)
            },
            DecorationType.Flower => rng.Next(4) switch
            {
                0 => Color.Yellow,
                1 => Color.HotPink,
                2 => Color.Orange,
                _ => Color.White
            },
            DecorationType.Bush => new Color(50, 130, 50),
            DecorationType.Mushroom => rng.Next(2) switch
            {
                0 => new Color(220, 20, 60),
                _ => new Color(139, 69, 19)
            },
            DecorationType.Cactus => new Color(85, 107, 47),
            DecorationType.Coral => rng.Next(3) switch
            {
                0 => Color.Coral,
                1 => Color.OrangeRed,
                _ => new Color(255, 127, 80)
            },
            _ => Color.Transparent
        };
    }
}
