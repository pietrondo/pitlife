using System.Buffers.Binary;

namespace PitLife.Tests;

public class AssetTests
{
    [Fact]
    public void CreaturePngs_HaveValidDimensionsAndAlphaChannel()
    {
        string root = FindRepositoryRoot();
        string assets = Path.Combine(root, "Content", "assets", "creatures");

        foreach (string file in Directory.EnumerateFiles(assets, "*.png", SearchOption.AllDirectories))
        {
            byte[] bytes = File.ReadAllBytes(file);
            Assert.True(bytes.Length > 256, $"Asset is unexpectedly small: {file}");
            Assert.True(bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
                $"Invalid PNG signature: {file}");

            int width = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4));
            int height = BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4));
            byte colorType = bytes[25];
            Assert.InRange(width, 16, 256);
            Assert.InRange(height, 16, 256);
            Assert.True(colorType is 4 or 6, $"Asset must expose an alpha channel: {file}");
        }
    }

    [Fact]
    public void LionSprite_IsFullSizeReplacement()
    {
        string path = Path.Combine(FindRepositoryRoot(), "Content", "assets", "creatures",
            "mammals", "carnivores", "felids", "lion.png");
        byte[] bytes = File.ReadAllBytes(path);

        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        Assert.True(bytes.Length > 2000);
    }

    [Fact]
    public void TurtleSprite_IsFullSizeTransparentReplacement()
    {
        string path = Path.Combine(FindRepositoryRoot(), "Content", "assets", "creatures",
            "reptiles", "testudines", "turtle.png");
        byte[] bytes = File.ReadAllBytes(path);

        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(16, 4)));
        Assert.Equal(64, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(20, 4)));
        Assert.Equal(6, bytes[25]);
        Assert.True(bytes.Length > 2000);
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PitLife.csproj")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new DirectoryNotFoundException("PitLife repository root not found.");
    }
}
