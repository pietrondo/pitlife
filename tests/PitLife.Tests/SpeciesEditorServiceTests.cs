using PitLife.Core;
using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesEditorServiceTests
{
    [Fact]
    public void Add_PersistsAndAppliesValidSpecies()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-editor-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        var runtime = new SpeciesCatalogRuntime();
        try
        {
            var editor = new SpeciesEditorService(runtime, root, "species.json", directory);

            IReadOnlyList<SpeciesCatalogValidationError> errors = editor.Add(ValidEntry());

            Assert.Empty(errors);
            Assert.True(File.Exists(path));
            Assert.True(SpeciesRegistry.Contains("EditorBadger"));
            Assert.Equal("EditorBadger", Assert.Single(SpeciesCatalogStore.Load("species.json", directory).Species).Key);
        }
        finally
        {
            runtime.Clear();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Add_DuplicateDoesNotReplacePersistedCatalog()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-editor-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        var runtime = new SpeciesCatalogRuntime();
        try
        {
            var editor = new SpeciesEditorService(runtime, root, "species.json", directory);
            Assert.Empty(editor.Add(ValidEntry()));
            var original = File.ReadAllText(path);

            IReadOnlyList<SpeciesCatalogValidationError> errors = editor.Add(ValidEntry());

            Assert.Contains(errors, error => error.Message.Contains("Duplicate", StringComparison.Ordinal));
            Assert.Equal(original, File.ReadAllText(path));
            Assert.Single(SpeciesCatalogStore.Load("species.json", directory).Species);
        }
        finally
        {
            runtime.Clear();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Upsert_ModifiesExistingSpeciesWithoutDuplicatingIt()
    {
        var root = FindRepositoryRoot();
        var directory = Path.Combine(Path.GetTempPath(), $"pitlife-editor-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "species.json");
        var runtime = new SpeciesCatalogRuntime();
        try
        {
            var editor = new SpeciesEditorService(runtime, root, "species.json", directory);
            SpeciesCatalogEntry entry = ValidEntry();
            Assert.Empty(editor.Add(entry));
            entry.ItalianName = "Tasso modificato";

            IReadOnlyList<SpeciesCatalogValidationError> errors = editor.Upsert(entry);

            Assert.Empty(errors);
            SpeciesCatalogEntry saved = Assert.Single(SpeciesCatalogStore.Load("species.json", directory).Species);
            Assert.Equal("Tasso modificato", saved.ItalianName);
        }
        finally
        {
            runtime.Clear();
            if (Directory.Exists(directory))
                Directory.Delete(directory, recursive: true);
        }
    }

    private static SpeciesCatalogEntry ValidEntry() => new()
    {
        Key = "EditorBadger",
        EnglishName = "Editor Badger",
        ItalianName = "Tasso editor",
        Kind = CreatureType.Omnivore,
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
