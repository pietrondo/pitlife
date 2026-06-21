using System.Buffers.Binary;
using System.IO.Compression;
using System.Security.Cryptography;
using PitLife.Rendering;

namespace PitLife.Tests;

public class AssetQualityTests
{
    private static readonly IReadOnlyDictionary<string, int> ExpectedStandardSizes =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Content/assets/creatures/mammals/herbivores/lagomorphs/rabbit.png"] = 32,
            ["Content/assets/creatures/mammals/herbivores/ungulates/deer.png"] = 64,
            ["Content/assets/creatures/mammals/herbivores/ungulates/goat.png"] = 64,
            ["Content/assets/creatures/mammals/herbivores/ungulates/herbivore.png"] = 64,
            ["Content/assets/creatures/mammals/herbivores/ungulates/horse.png"] = 64,
            ["Content/assets/creatures/mammals/herbivores/ungulates/sheep.png"] = 64,
            ["Content/assets/creatures/mammals/omnivores/procyonids/raccoon.png"] = 32,
            ["Content/assets/creatures/mammals/omnivores/ursids/bear.png"] = 64,
            ["Content/assets/creatures/plants/trees/tree.png"] = 64,
            ["Content/assets/creatures/reptiles/squamates/lizard.png"] = 32,
            ["Content/assets/creatures/mammals/herbivores/ungulates/moose.png"] = 64,
            ["Content/assets/creatures/mammals/herbivores/ungulates/gazelle.png"] = 64,
            ["Content/assets/creatures/mammals/omnivores/mustelids/badger.png"] = 64,
            ["Content/assets/creatures/birds/owls/owl.png"] = 32,
            ["Content/assets/creatures/plants/flowers/lavender.png"] = 32,
            ["Content/assets/creatures/plants/ferns/fern.png"] = 32,
            ["Content/assets/creatures/plants/flowers/sunflower.png"] = 32,
            ["Content/assets/creatures/plants/fungi/chanterelle.png"] = 32,
            ["Content/assets/creatures/plants/fungi/morel.png"] = 32,
            ["Content/assets/creatures/plants/fungi/oyster_mushroom.png"] = 32,
            ["Content/assets/plants/aquatic/coral.png"] = 32,
            ["Content/assets/plants/aquatic/kelp.png"] = 32,
            ["Content/assets/plants/aquatic/seaweed.png"] = 32,
            ["Content/assets/plants/aquatic/water_lily.png"] = 32,
            ["Content/assets/plants/aquatic/algae.png"] = 32,
            ["Content/assets/plants/bushes/bush.png"] = 32,
            ["Content/assets/plants/grass/grass.png"] = 32,
            ["Content/assets/plants/trees/oak_tree.png"] = 64,
            ["Content/assets/plants/trees/pine_tree.png"] = 64,
        };

    private static readonly string[] NewSpeciesAssets =
    [
        "Content/assets/creatures/mammals/herbivores/ungulates/moose.png",
        "Content/assets/creatures/mammals/omnivores/mustelids/badger.png",
        "Content/assets/creatures/birds/owls/owl.png",
        "Content/assets/creatures/plants/flowers/lavender.png",
        "Content/assets/creatures/plants/ferns/fern.png",
        "Content/assets/creatures/plants/flowers/sunflower.png",
        "Content/assets/creatures/plants/fungi/chanterelle.png",
        "Content/assets/creatures/plants/fungi/morel.png",
        "Content/assets/creatures/plants/fungi/oyster_mushroom.png",
    ];

    [Fact]
    public void RegisteredSpeciesTextures_AreValidRgbaPngs()
    {
        string root = FindRepositoryRoot();
        foreach (SpeciesAsset asset in AssetRegistry.SpeciesTextures)
        {
            PngPixels image = DecodeRgbaPng(Path.Combine(root, asset.Path));
            Assert.InRange(image.Width, 32, 128);
            Assert.Equal(image.Width, image.Height);
        }
    }

    [Fact]
    public void GeneratedAndStandardizedAssets_HaveExpectedDimensionsAndQuality()
    {
        string root = FindRepositoryRoot();
        foreach ((string relativePath, int expectedSize) in ExpectedStandardSizes)
        {
            PngPixels image = DecodeRgbaPng(Path.Combine(root, relativePath));
            Assert.Equal(expectedSize, image.Width);
            Assert.Equal(expectedSize, image.Height);

            int area = image.Width * image.Height;
            Assert.InRange(image.VisiblePixels, Math.Max(16, area / 50), area * 9 / 10);
            Assert.True(image.TransparentPixels > area / 20, $"Asset needs transparent margin: {relativePath}");
            Assert.True(image.DistinctVisibleColors >= 8, $"Asset has insufficient color detail: {relativePath}");
        }
    }

    [Fact]
    public void NewSpeciesAssets_AreVisuallyDistinctFiles()
    {
        string root = FindRepositoryRoot();
        string[] hashes = NewSpeciesAssets
            .Select(path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(root, path)))))
            .ToArray();

        Assert.Equal(hashes.Length, hashes.Distinct(StringComparer.Ordinal).Count());
    }

    private static PngPixels DecodeRgbaPng(string path)
    {
        Assert.True(File.Exists(path), $"Missing PNG asset: {path}");
        byte[] png = File.ReadAllBytes(path);
        ReadOnlySpan<byte> signature = [137, 80, 78, 71, 13, 10, 26, 10];
        Assert.True(png.AsSpan(0, 8).SequenceEqual(signature), $"Invalid PNG signature: {path}");

        int width = 0;
        int height = 0;
        using var idat = new MemoryStream();
        int offset = 8;
        while (offset < png.Length)
        {
            int length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            string type = System.Text.Encoding.ASCII.GetString(png, offset + 4, 4);
            ReadOnlySpan<byte> data = png.AsSpan(offset + 8, length);
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
                Assert.Equal(8, data[8]);
                Assert.Equal(6, data[9]);
                Assert.Equal(0, data[12]);
            }
            else if (type == "IDAT")
            {
                idat.Write(data);
            }

            offset += length + 12;
        }

        Assert.True(width > 0 && height > 0 && idat.Length > 0, $"Incomplete PNG: {path}");
        idat.Position = 0;
        using var decompressed = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
            zlib.CopyTo(decompressed);

        byte[] filtered = decompressed.ToArray();
        int stride = width * 4;
        Assert.Equal((stride + 1) * height, filtered.Length);
        byte[] pixels = new byte[stride * height];
        for (int y = 0; y < height; y++)
        {
            int sourceRow = y * (stride + 1);
            int targetRow = y * stride;
            byte filter = filtered[sourceRow];
            for (int x = 0; x < stride; x++)
            {
                byte raw = filtered[sourceRow + 1 + x];
                byte left = x >= 4 ? pixels[targetRow + x - 4] : (byte)0;
                byte up = y > 0 ? pixels[targetRow - stride + x] : (byte)0;
                byte upperLeft = y > 0 && x >= 4 ? pixels[targetRow - stride + x - 4] : (byte)0;
                pixels[targetRow + x] = filter switch
                {
                    0 => raw,
                    1 => unchecked((byte)(raw + left)),
                    2 => unchecked((byte)(raw + up)),
                    3 => unchecked((byte)(raw + ((left + up) >> 1))),
                    4 => unchecked((byte)(raw + Paeth(left, up, upperLeft))),
                    _ => throw new InvalidDataException($"Unsupported PNG filter {filter}: {path}")
                };
            }
        }

        int visible = 0;
        var colors = new HashSet<int>();
        for (int i = 0; i < pixels.Length; i += 4)
        {
            if (pixels[i + 3] == 0)
                continue;

            visible++;
            colors.Add((pixels[i] << 16) | (pixels[i + 1] << 8) | pixels[i + 2]);
        }

        return new PngPixels(width, height, visible, width * height - visible, colors.Count);
    }

    private static byte Paeth(byte left, byte up, byte upperLeft)
    {
        int prediction = left + up - upperLeft;
        int leftDistance = Math.Abs(prediction - left);
        int upDistance = Math.Abs(prediction - up);
        int upperLeftDistance = Math.Abs(prediction - upperLeft);
        return leftDistance <= upDistance && leftDistance <= upperLeftDistance
            ? left
            : upDistance <= upperLeftDistance ? up : upperLeft;
    }

    private sealed record PngPixels(
        int Width,
        int Height,
        int VisiblePixels,
        int TransparentPixels,
        int DistinctVisibleColors);

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PitLife.csproj")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("PitLife repository root not found.");
    }
}
