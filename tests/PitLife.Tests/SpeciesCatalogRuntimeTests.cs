using PitLife.Core;
using PitLife.Localization;
using PitLife.Rendering;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class SpeciesCatalogRuntimeTests
{
    [Fact]
    public void Apply_UpdatesSimulationAssetsLocalizationAndSpawnCategories()
    {
        string root = FindRepositoryRoot();
        var runtime = new SpeciesCatalogRuntime();
        int changeNotifications = 0;
        runtime.CatalogChanged += () => changeNotifications++;
        try
        {
            IReadOnlyList<SpeciesCatalogValidationError> errors = runtime.Apply(
                new SpeciesCatalogDocument { Species = [ValidEntry()] }, root);

            Assert.Empty(errors);
            Assert.True(SpeciesRegistry.Contains("RuntimeBadger"));
            Assert.Contains(AssetRegistry.SpeciesTextures, asset =>
                asset.Species == "RuntimeBadger" && asset.Path.EndsWith("badger.png", StringComparison.Ordinal));
            I18n.SetLanguage("it");
            Assert.Equal("Tasso runtime", I18n.Species("RuntimeBadger"));
            Assert.Contains("RuntimeBadger", SpawnPanel.SpeciesForCategory("Omnivores"));
            Assert.Equal(1, changeNotifications);
        }
        finally
        {
            runtime.Clear();
            I18n.SetLanguage("it");
        }

        Assert.False(SpeciesRegistry.Contains("RuntimeBadger"));
        Assert.DoesNotContain(AssetRegistry.SpeciesTextures, asset => asset.Species == "RuntimeBadger");
    }

    [Fact]
    public void Apply_InvalidCatalogDoesNotMutateRuntime()
    {
        string root = FindRepositoryRoot();
        var runtime = new SpeciesCatalogRuntime();
        SpeciesCatalogEntry entry = ValidEntry();
        entry.TexturePath = "missing.png";

        IReadOnlyList<SpeciesCatalogValidationError> errors = runtime.Apply(
            new SpeciesCatalogDocument { Species = [entry] }, root);

        Assert.NotEmpty(errors);
        Assert.False(SpeciesRegistry.Contains("RuntimeBadger"));
        Assert.Empty(runtime.CustomKeys);
    }

    private static SpeciesCatalogEntry ValidEntry() => new()
    {
        Key = "RuntimeBadger",
        EnglishName = "Runtime Badger",
        ItalianName = "Tasso runtime",
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
