using System.Text.Json;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesCatalogTests
{

    [Fact]
    public void Store_RejectsPathTraversal()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        var document = new SpeciesCatalogDocument { Species = [ValidAnimal()] };
        var path = Path.GetFullPath("../../../../../../../../../../../../tmp/test.json");
        if (Environment.OSVersion.Platform == PlatformID.Unix) path = "/tmp/test.json";

        Assert.Throws<UnauthorizedAccessException>(() => SpeciesCatalogStore.Save(path, document, directory));
    }

    [Fact]
    public void Store_RoundTripsVersionedCatalog()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        try
        {
            var document = new SpeciesCatalogDocument { Species = [ValidAnimal()] };

            SpeciesCatalogStore.Save("species.json", document, directory);
            SpeciesCatalogDocument loaded = SpeciesCatalogStore.Load("species.json", directory);

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
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
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
        var root = FindRepositoryRoot();
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
    public void Validator_RejectsInvalidMaturityAge()
    {
        var root = FindRepositoryRoot();

        var tooYoung = ValidAnimal();
        tooYoung.Key = "TooYoung";
        tooYoung.MaturityAge = 0.5f;

        var tooOld = ValidAnimal();
        tooOld.Key = "TooOld";
        tooOld.MaturityAge = 2000f;

        var nanAge = ValidAnimal();
        nanAge.Key = "NanAge";
        nanAge.MaturityAge = float.NaN;

        var errors = SpeciesCatalogValidator.Validate(
            new SpeciesCatalogDocument { Species = [tooYoung, tooOld, nanAge] }, root);

        Assert.Equal(3, errors.Count(e => e.Field == "MaturityAge"));
        Assert.All(errors.Where(e => e.Field == "MaturityAge"), error =>
            Assert.Contains("Maturity age must be between 1 and 1000 seconds.", error.Message, StringComparison.Ordinal));
    }

    [Fact]
    public void Store_RejectsUnknownJsonFields()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"species\":[],\"unexpected\":true}");
            Assert.Throws<JsonException>(() => SpeciesCatalogStore.Load("species.json", directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Store_RejectsNullRequiredCollections()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-catalog-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(path, "{\"schemaVersion\":1,\"species\":null}");
            Assert.Throws<JsonException>(() => SpeciesCatalogStore.Load("species.json", directory));
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
