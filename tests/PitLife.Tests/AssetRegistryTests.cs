using System.Buffers.Binary;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tests;

public class AssetRegistryTests
{
    [Fact]
    public void AllBuiltinSpecies_HaveTexture()
    {
        var speciesTextures = AssetRegistry.SpeciesTextures.Select(a => a.Species).ToHashSet();
        foreach (var species in SpeciesRegistry.All.Where(s => !s.StartsWith("TestDummy")))
        {
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
    public void GenderedSpecies_AreDistinctValidPngPairs()
    {
        var root = FindRepositoryRoot();
        string[] expectedSpecies = ["Deer", "Goat", "Lion", "Sheep"];
        Assert.Equal(expectedSpecies,
            AssetRegistry.GenderedSpeciesTextures.Select(asset => asset.Species).Order().ToArray());

        foreach (GenderedSpeciesAsset asset in AssetRegistry.GenderedSpeciesTextures)
        {
            var male = ReadAndValidatePng(Path.Combine(root, asset.MalePath));
            var female = ReadAndValidatePng(Path.Combine(root, asset.FemalePath));

            Assert.False(male.SequenceEqual(female),
                $"Male and female sprites must differ for {asset.Species}");
            Assert.Equal(
                BinaryPrimitives.ReadInt32BigEndian(male.AsSpan(16, 4)),
                BinaryPrimitives.ReadInt32BigEndian(female.AsSpan(16, 4)));
            Assert.Equal(
                BinaryPrimitives.ReadInt32BigEndian(male.AsSpan(20, 4)),
                BinaryPrimitives.ReadInt32BigEndian(female.AsSpan(20, 4)));
        }
    }

    [Theory]
    [InlineData("Pine")]
    [InlineData("Crocodile")]
    [InlineData("Fox")]
    [InlineData("Tuna")]
    public void CriticalReplacementSprites_AreTransparent64PixelPngs(string species)
    {
        SpeciesAsset asset = Assert.Single(
            AssetRegistry.SpeciesTextures,
            candidate => candidate.Species == species);
        var bytes = ReadAndValidatePng(Path.Combine(FindRepositoryRoot(), asset.Path));

        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
    }

    [Fact]
    public void FallbackKeys_AreDistinct()
    {
        Assert.Equal(4, AssetRegistry.Fallbacks.Count);
        var keys = AssetRegistry.Fallbacks.Select(a => a.Species).Distinct().Count();
        Assert.Equal(4, keys);
    }

    private static byte[] ReadAndValidatePng(string path)
    {
        Assert.True(File.Exists(path), $"Missing gendered sprite: {path}");
        var bytes = File.ReadAllBytes(path);
        Assert.True(bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
            $"Invalid PNG signature: {path}");
        var width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
        Assert.Equal(width, height);
        Assert.InRange(width, 32, 128);
        Assert.Equal(6, bytes[25]);
        return bytes;
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PitLife.csproj")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("PitLife repository root not found.");
    }
}
