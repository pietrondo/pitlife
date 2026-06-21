using System.Text.Json;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesCatalogTests
{
    [Fact]
    public void Store_RoundTripsVersionedCatalog()
    {
        string root = FindRepositoryRoot();
        string directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "species.json");
        try
        {
            var document = new SpeciesCatalogDocument { Species = [ValidAnimal()] };

            SpeciesCatalogStore.Save(path, document);
            SpeciesCatalogDocument loaded = SpeciesCatalogStore.Load(path);

            SpeciesCatalogEntry entry = Assert.Single(loaded.Species);
            Assert.Equal(SpeciesCatalogDocument.CurrentSchemaVersion, loaded.SchemaVersion);
            Assert.Equal("TestBadger", entry.Key);
            Assert.Equal(CreatureType.Omnivore, entry.Kind);
            Assert.Empty(SpeciesCatalogValidator.Validate(loaded, root));
        }
        finally
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Validator_RejectsDuplicateAndBuiltinKeys()
    {
        string root = FindRepositoryRoot();
        SpeciesCatalogEntry duplicate = ValidAnimal();
        var document = new SpeciesCatalogDocument { Species = [ValidAnimal(), duplicate] };

        IReadOnlyList<SpeciesCatalogValidationError> errors =
            SpeciesCatalogValidator.Validate(document, root, ["TestBadger"]);

        Assert.Contains(errors, error => error.Message.Contains("cannot be overridden", StringComparison.Ordinal));
        Assert.Contains(errors, error => error.Message.Contains("Duplicate", StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_RejectsUnsafeAssetPathAndInvalidPlantReproduction()
    {
        string root = FindRepositoryRoot();
        var entry = new SpeciesCatalogEntry
        {
            Key = "TestFern",
            EnglishName = "Test Fern",
            ItalianName = "Felce test",
            Kind = CreatureType.Plant,
            SocialBehavior = SocialBehavior.None,
            ValidBiomes = [BiomeType.Forest],
            DefaultSize = 1f,
            MaturityAge = 20f,
            TexturePath = "../fern.png",
            PlantReproduction = PlantReproductionMode.Spores,
            Pollination = PollinationMode.Insects
        };

        IReadOnlyList<SpeciesCatalogValidationError> errors = SpeciesCatalogValidator.Validate(
            new SpeciesCatalogDocument { Species = [entry] }, root);

        Assert.Contains(errors, error => error.Field == nameof(entry.TexturePath));
        Assert.Contains(errors, error => error.Field == nameof(entry.Pollination));
    }

    [Fact]
    public void Validator_AcceptsSeedPlantWithPollination()
    {
        string root = FindRepositoryRoot();
        var entry = new SpeciesCatalogEntry
        {
            Key = "TestLavender",
            EnglishName = "Test Lavender",
            ItalianName = "Lavanda test",
            Kind = CreatureType.Plant,
            SocialBehavior = SocialBehavior.None,
            ValidBiomes = [BiomeType.Grassland],
            DefaultSize = 0.7f,
            MaturityAge = 15f,
            TexturePath = "Content/assets/creatures/plants/flowers/lavender.png",
            PlantReproduction = PlantReproductionMode.Seeds,
            Pollination = PollinationMode.Insects
        };

        Assert.Empty(SpeciesCatalogValidator.Validate(
            new SpeciesCatalogDocument { Species = [entry] }, root));
    }

    [Fact]
    public void Store_RejectsUnknownJsonFields()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "species.json");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"species\":[],\"unexpected\":true}");
            Assert.Throws<JsonException>(() => SpeciesCatalogStore.Load(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Store_RejectsNullRequiredCollections()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        string path = Path.Combine(directory, "species.json");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"species\":null}");
            Assert.Throws<JsonException>(() => SpeciesCatalogStore.Load(path));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static SpeciesCatalogEntry ValidAnimal() => new()
    {
        Key = "TestBadger",
        EnglishName = "Test Badger",
        ItalianName = "Tasso test",
        Kind = CreatureType.Omnivore,
        IsAquatic = false,
        SocialBehavior = SocialBehavior.Solitary,
        ValidBiomes = [BiomeType.Forest],
        DefaultSize = 0.8f,
        MaturityAge = 25f,
        TexturePath = "Content/assets/creatures/mammals/omnivores/mustelids/badger.png"
    };

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "PitLife.csproj")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("PitLife repository root not found.");
    }
}
