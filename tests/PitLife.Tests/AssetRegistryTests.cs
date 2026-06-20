using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tests;

public class AssetRegistryTests
{
    [Fact]
    public void AllBuiltinSpecies_HaveTexture()
    {
        var speciesTextures = AssetRegistry.SpeciesTextures.Select(a => a.Species).ToHashSet();
        foreach (var species in SpeciesRegistry.All)
        {
            var def = SpeciesRegistry.Get(species);
            if (def == null || def.Kind == CreatureType.Plant) continue;
            if (species == "Gazelle") continue;
            Assert.Contains(species, speciesTextures);
        }
    }

    [Fact]
    public void AllBiomes_HaveTexture()
    {
        Assert.Equal(12, AssetRegistry.Biomes.Count);
        Assert.Contains(AssetRegistry.Biomes, a => a.Species == nameof(BiomeType.DeepOcean));
        Assert.Contains(AssetRegistry.Biomes, a => a.Species == nameof(BiomeType.Snow));
    }

    [Fact]
    public void GenderedSpecies_HaveBothPaths()
    {
        Assert.All(AssetRegistry.GenderedSpeciesTextures, a =>
        {
            Assert.False(string.IsNullOrEmpty(a.MalePath));
            Assert.False(string.IsNullOrEmpty(a.FemalePath));
            Assert.NotEqual(a.MalePath, a.FemalePath);
        });
    }

    [Fact]
    public void FallbackKeys_AreDistinct()
    {
        Assert.Equal(4, AssetRegistry.Fallbacks.Count);
        var keys = AssetRegistry.Fallbacks.Select(a => a.Species).Distinct().Count();
        Assert.Equal(4, keys);
    }
}
