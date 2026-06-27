using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace PitLife.Simulation;

public sealed class SpeciesCatalogDocument
{
    public const int CurrentSchemaVersion = 1;

    public int SchemaVersion { get; set; } = CurrentSchemaVersion;
    public List<SpeciesCatalogEntry> Species { get; set; } = new();
}

public sealed class SpeciesCatalogEntry
{
    public string Key { get; set; } = "";
    public string EnglishName { get; set; } = "";
    public string ItalianName { get; set; } = "";
    public CreatureType Kind { get; set; }
    public bool IsAquatic { get; set; }
    public SocialBehavior SocialBehavior { get; set; }
    public List<BiomeType> ValidBiomes { get; set; } = new();
    public float DefaultSize { get; set; } = 1f;
    public float MaturityAge { get; set; } = 30f;
    public string TexturePath { get; set; } = "";
    public PlantReproductionMode? PlantReproduction { get; set; }
    public PollinationMode Pollination { get; set; }

    public SpeciesDefinition ToDefinition()
    {
        Type creatureClass = Kind switch
        {
            CreatureType.Plant => typeof(Plant),
            CreatureType.Herbivore => typeof(Herbivore),
            CreatureType.Carnivore => typeof(Carnivore),
            CreatureType.Omnivore => typeof(Omnivore),
            _ => throw new InvalidOperationException($"Unsupported creature type: {Kind}")
        };

        return new SpeciesDefinition(
            Key,
            creatureClass,
            Kind,
            IsAquatic,
            SocialBehavior,
            ValidBiomes,
            DefaultSize,
            MaturityAge,
            PlantReproduction,
            Pollination);
    }
}

public sealed record SpeciesCatalogValidationError(int EntryIndex, string Field, string Message);

public static partial class SpeciesCatalogValidator
{
    public static IReadOnlyList<SpeciesCatalogValidationError> Validate(
        SpeciesCatalogDocument document,
        string repositoryRoot,
        IEnumerable<string>? reservedKeys = null)
    {
        var errors = new List<SpeciesCatalogValidationError>();
        if (document.SchemaVersion != SpeciesCatalogDocument.CurrentSchemaVersion)
        {
            errors.Add(new SpeciesCatalogValidationError(
                -1,
                nameof(document.SchemaVersion),
                $"Unsupported schema version {document.SchemaVersion}."));
        }

        if (document.Species is null)
        {
            errors.Add(new SpeciesCatalogValidationError(-1, nameof(document.Species), "Species list is required."));
            return errors;
        }

        if (document.Species.Count > 500)
        {
            errors.Add(new SpeciesCatalogValidationError(
                -1,
                nameof(document.Species),
                "A catalog cannot contain more than 500 species."));
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var reserved = reservedKeys?.ToHashSet(StringComparer.Ordinal) ?? new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < document.Species.Count; index++)
        {
            SpeciesCatalogEntry entry = document.Species[index];
            ValidateEntry(entry, index, repositoryRoot, seen, reserved, errors);
        }

        return errors;
    }

    private static void ValidateEntry(
        SpeciesCatalogEntry entry,
        int index,
        string repositoryRoot,
        HashSet<string> seen,
        HashSet<string> reserved,
        List<SpeciesCatalogValidationError> errors)
    {
        void Error(string field, string message) =>
            errors.Add(new SpeciesCatalogValidationError(index, field, message));

        if (string.IsNullOrWhiteSpace(entry.Key) || !SpeciesKeyPattern().IsMatch(entry.Key))
            Error(nameof(entry.Key), "Key must start with an uppercase letter and contain 2-40 ASCII letters or digits.");
        else if (!seen.Add(entry.Key))
            Error(nameof(entry.Key), $"Duplicate species key '{entry.Key}'.");
        else if (reserved.Contains(entry.Key))
            Error(nameof(entry.Key), $"Built-in species '{entry.Key}' cannot be overridden.");

        if (string.IsNullOrWhiteSpace(entry.EnglishName))
            Error(nameof(entry.EnglishName), "English display name is required.");
        if (string.IsNullOrWhiteSpace(entry.ItalianName))
            Error(nameof(entry.ItalianName), "Italian display name is required.");
        if (!Enum.IsDefined(entry.Kind))
            Error(nameof(entry.Kind), "Creature type is invalid.");
        if (!Enum.IsDefined(entry.SocialBehavior))
            Error(nameof(entry.SocialBehavior), "Social behavior is invalid.");
        if (entry.ValidBiomes is null || entry.ValidBiomes.Count == 0)
            Error(nameof(entry.ValidBiomes), "At least one biome is required.");
        else if (entry.ValidBiomes.Count != entry.ValidBiomes.Distinct().Count())
            Error(nameof(entry.ValidBiomes), "Biomes must not contain duplicates.");
        else if (entry.ValidBiomes.Any(biome => !Enum.IsDefined(biome)))
            Error(nameof(entry.ValidBiomes), "One or more biomes are invalid.");
        if (!float.IsFinite(entry.DefaultSize) || entry.DefaultSize is < 0.1f or > 5f)
            Error(nameof(entry.DefaultSize), "Default size must be between 0.1 and 5.");
        if (!float.IsFinite(entry.MaturityAge) || entry.MaturityAge is < 1f or > 1000f)
            Error(nameof(entry.MaturityAge), "Maturity age must be between 1 and 1000 seconds.");

        ValidateReproduction(entry, Error);
        ValidateTexturePath(entry.TexturePath, repositoryRoot, Error);
    }

    private static void ValidateReproduction(SpeciesCatalogEntry entry, Action<string, string> error)
    {
        if (entry.Kind == CreatureType.Plant)
        {
            if (entry.SocialBehavior != SocialBehavior.None)
                error(nameof(entry.SocialBehavior), "Plants must use SocialBehavior.None.");
            if (entry.PlantReproduction is null)
                error(nameof(entry.PlantReproduction), "Plants require a reproduction mode.");
            else if (!Enum.IsDefined(entry.PlantReproduction.Value))
                error(nameof(entry.PlantReproduction), "Plant reproduction mode is invalid.");
            if (entry.PlantReproduction is not PlantReproductionMode.Seeds &&
                entry.Pollination != PollinationMode.None)
            {
                error(nameof(entry.Pollination), "Only seed-producing plants can define pollination.");
            }
        }
        else
        {
            if (entry.SocialBehavior == SocialBehavior.None)
                error(nameof(entry.SocialBehavior), "Animals require a social behavior.");
            if (entry.PlantReproduction is not null || entry.Pollination != PollinationMode.None)
                error(nameof(entry.PlantReproduction), "Animals cannot define plant reproduction settings.");
        }
    }

    private static void ValidateTexturePath(
        string texturePath,
        string repositoryRoot,
        Action<string, string> error)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            error(nameof(SpeciesCatalogEntry.TexturePath), "Texture path is required.");
            return;
        }

        var normalized = texturePath.Replace('\\', '/');
        if (Path.IsPathRooted(texturePath) ||
            !normalized.StartsWith("Content/assets/", StringComparison.Ordinal) ||
            !normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            normalized.Split('/').Contains("..", StringComparer.Ordinal))
        {
            error(nameof(SpeciesCatalogEntry.TexturePath),
                "Texture path must be a PNG below Content/assets without parent traversal.");
            return;
        }

        var fullPath = Path.GetFullPath(Path.Combine(repositoryRoot, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var assetsRoot = Path.GetFullPath(Path.Combine(repositoryRoot, "Content", "assets")) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(assetsRoot, StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
        {
            error(nameof(SpeciesCatalogEntry.TexturePath), "Texture file does not exist inside Content/assets.");
            return;
        }

        var header = new byte[26];
        using FileStream stream = File.OpenRead(fullPath);
        if (stream.Read(header, 0, header.Length) != header.Length ||
            !header.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }))
        {
            error(nameof(SpeciesCatalogEntry.TexturePath), "Texture file is not a valid PNG.");
            return;
        }

        var width = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(16, 4));
        var height = BinaryPrimitives.ReadInt32BigEndian(header.AsSpan(20, 4));
        if (width != height || width is not (32 or 64))
            error(nameof(SpeciesCatalogEntry.TexturePath), "Texture must be a square 32x32 or 64x64 PNG.");
        if (header[24] != 8 || header[25] != 6)
            error(nameof(SpeciesCatalogEntry.TexturePath), "Texture must use 8-bit RGBA color.");
    }

    [GeneratedRegex("^[A-Z][A-Za-z0-9]{1,39}$", RegexOptions.CultureInvariant)]
    private static partial Regex SpeciesKeyPattern();
}

public static class SpeciesCatalogStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static SpeciesCatalogDocument Load(string path)
    {
        if (path.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Path traversal is not allowed.", nameof(path));

        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<SpeciesCatalogDocument>(json, Options)
            ?? throw new InvalidDataException("Species catalog is empty.");
    }

    public static void Save(string path, SpeciesCatalogDocument document, string baseDirectory)
    {
        if (path.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Path traversal is not allowed.", nameof(path));

        var fullBaseDir = Path.GetFullPath(baseDirectory);
        if (!fullBaseDir.EndsWith(Path.DirectorySeparatorChar))
            fullBaseDir += Path.DirectorySeparatorChar;

        var fullPath = Path.GetFullPath(Path.Combine(baseDirectory, path));
        if (!fullPath.StartsWith(fullBaseDir, StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal is not allowed.");

        var directory = Path.GetDirectoryName(fullPath);
        if (directory is not null)
            Directory.CreateDirectory(directory);

        var temporaryPath = fullPath + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(document, Options));
            File.Move(temporaryPath, fullPath, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }
}
