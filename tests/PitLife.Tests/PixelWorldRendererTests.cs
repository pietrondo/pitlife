using Microsoft.Xna.Framework;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tests;

public class PixelWorldRendererTests
{
    [Fact]
    public void GenderIndicators_AreDistinctForMaleAndFemale()
    {
        Color? male = CreatureRenderer.GetGenderIndicatorColor(Gender.Male);
        Color? female = CreatureRenderer.GetGenderIndicatorColor(Gender.Female);

        Assert.NotNull(male);
        Assert.NotNull(female);
        Assert.NotEqual(male, female);
        Assert.Null(CreatureRenderer.GetGenderIndicatorColor(Gender.None));
    }
    [Fact]
    public void PixelWorldRenderer_BiomeColors_AreDefinedForAllBiomes()
    {
        // Verify all 13 biomes have colors defined
        var baseColors = PixelWorldRenderer.GetBiomeBaseColors();
        var detailColors = PixelWorldRenderer.GetBiomeDetailColors();
        var highlightColors = PixelWorldRenderer.GetBiomeHighlightColors();

        Assert.Equal(13, baseColors.Length);
        Assert.Equal(13, detailColors.Length);
        Assert.Equal(13, highlightColors.Length);

        // Verify each biome has non-black colors
        for (int i = 0; i < 13; i++)
        {
            Assert.False(baseColors[i].R == 0 && baseColors[i].G == 0 && baseColors[i].B == 0,
                $"Biome {i} has black base color");
        }
    }

    [Fact]
    public void PixelWorldRenderer_BiomeColors_MatchMinimapColors()
    {
        // Get colors from both renderers
        var pixelColors = PixelWorldRenderer.GetBiomeBaseColors();
        var minimapColors = Minimap.GetBiomeColors();

        // Verify same number of biomes
        Assert.Equal(pixelColors.Length, minimapColors.Length);

        // Verify colors match (or are very close)
        for (int i = 0; i < pixelColors.Length; i++)
        {
            var pc = pixelColors[i];
            var mc = minimapColors[i];

            // Allow small tolerance for color differences
            Assert.True(Math.Abs(pc.R - mc.R) <= 5, $"Biome {i}: R mismatch {pc.R} vs {mc.R}");
            Assert.True(Math.Abs(pc.G - mc.G) <= 5, $"Biome {i}: G mismatch {pc.G} vs {mc.G}");
            Assert.True(Math.Abs(pc.B - mc.B) <= 5, $"Biome {i}: B mismatch {pc.B} vs {mc.B}");
        }
    }

    [Fact]
    public void PixelWorldRenderer_RenderScale_IsOne()
    {
        // Verify render scale is 1 for pixel-perfect rendering
        Assert.Equal(1, PixelWorldRenderer.RenderScale);
    }

    [Fact]
    public void PixelWorldRenderer_WorldGeneration_CreatesMultipleBiomes()
    {
        // Create a world and verify it has multiple biome types
        var world = new World(64, 48, 42);
        var biomes = new HashSet<BiomeType>();

        for (int y = 0; y < world.Height; y++)
        {
            for (int x = 0; x < world.Width; x++)
            {
                biomes.Add(world.GetTile(x, y).Biome);
            }
        }

        // Should have at least 4 different biomes (ocean, land, etc.)
        Assert.True(biomes.Count >= 4,
            $"Expected at least 4 biomes, found {biomes.Count}: {string.Join(", ", biomes)}");

        // Should have ocean
        Assert.Contains(BiomeType.DeepOcean, biomes);

        // Should have some land
        Assert.True(biomes.Any(b => b != BiomeType.DeepOcean && b != BiomeType.ShallowWater),
            "Expected some land biomes");
    }
}
