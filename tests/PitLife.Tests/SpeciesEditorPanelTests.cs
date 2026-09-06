using System.Reflection;
using System.Text.Json;
using Microsoft.Xna.Framework.Input;
using PitLife.Core;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public sealed class SpeciesEditorPanelTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"pitlife-panel-{Guid.NewGuid():N}");
    private readonly SpeciesEditorPanel _panel;
    private readonly SpeciesCatalogRuntime _runtime = new();
    private readonly SpeciesCatalogEntry _original = new()
    {
        Key = "CustomPlant", EnglishName = "Custom plant", ItalianName = "Pianta",
        Kind = CreatureType.Plant, SocialBehavior = SocialBehavior.None,
        IsAquatic = true, ValidBiomes = [BiomeType.CoralReef, BiomeType.ShallowWater],
        DefaultSize = 1.2345f, MaturityAge = 47.125f,
        PlantReproduction = PlantReproductionMode.Seeds, Pollination = PollinationMode.Wind,
        TexturePath = "Content/assets/creatures/mammals/omnivores/mustelids/badger.png"
    };

    public SpeciesEditorPanelTests()
    {
        SpeciesCatalogStore.Save("species.json", new SpeciesCatalogDocument { Species = [_original] }, _directory);
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "PitLife.csproj")))
            root = root.Parent;
        _panel = new SpeciesEditorPanel(new SpeciesEditorService(_runtime,
            root!.FullName, "species.json", _directory));
        _panel.Toggle();
        _panel.Update(default, default, default, default, 1000, 800);
    }

    [Fact]
    public void Load_BuildDraft_PreservesEveryAttribute()
    {
        Click("_load");
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void Load_Save_PersistsEveryAttribute()
    {
        Click("_load");
        Click("_save");
        Assert.False(Field<bool>("_statusIsError"));
        AssertEntry(_original, Assert.Single(SpeciesCatalogStore.Load("species.json", _directory).Species));
    }

    [Fact]
    public void Load_PreservesTextBeyondControlDisplayLimit()
    {
        _original.EnglishName = new string('A', 60);
        SpeciesCatalogStore.Save("species.json", new SpeciesCatalogDocument { Species = [_original] }, _directory);
        Click("_load");
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void EditName_ChangesOnlyName()
    {
        Click("_load");
        Field<UiTextInput>("_englishName").SetText("Renamed");
        _original.EnglishName = "Renamed";
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void Clone_PreservesAttributesAndChangesIdentity()
    {
        Click("_clone");
        _original.Key += "Copy";
        _original.EnglishName += " Copy";
        _original.ItalianName += " copia";
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void BiomeClick_ReplacesCustomBiomesOnly()
    {
        Click("_load");
        Click("_biomes");
        _original.ValidBiomes = [BiomeType.Forest, BiomeType.DenseForest];
        _original.IsAquatic = false;
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void ReproductionClick_NormalizesPollinationOnly()
    {
        Click("_load");
        Click("_reproduction");
        _original.PlantReproduction = PlantReproductionMode.Spores;
        _original.Pollination = PollinationMode.None;
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void KindClick_ToAnimal_NormalizesPlantFieldsOnly()
    {
        Click("_load");
        Click("_kind");
        _original.Kind = CreatureType.Herbivore;
        _original.SocialBehavior = SocialBehavior.Solitary;
        _original.PlantReproduction = null;
        _original.Pollination = PollinationMode.None;
        AssertEntry(_original, _panel.BuildDraft());
    }

    [Fact]
    public void Clear_DiscardsLoadedAttributes()
    {
        Click("_load");
        Click("_clear");
        var draft = _panel.BuildDraft();
        Assert.Equal("", draft.Key);
        Assert.Equal(0.8f, draft.DefaultSize);
        Assert.Equal(15f, draft.MaturityAge);
        Assert.Equal(PollinationMode.Insects, draft.Pollination);
        Assert.False(draft.IsAquatic);
    }

    [Fact]
    public void BuildDraft_DoesNotExposeBaselineBiomeList()
    {
        Click("_load");
        _panel.BuildDraft().ValidBiomes.Clear();
        AssertEntry(_original, _panel.BuildDraft());
    }

    private T Field<T>(string name) => (T)typeof(SpeciesEditorPanel)
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_panel)!;

    private void Click(string name)
    {
        var point = Field<UiButton>(name).Bounds.Center;
        var released = new MouseState(point.X, point.Y, 0, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var pressed = new MouseState(point.X, point.Y, 0, ButtonState.Pressed,
            ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        _panel.Update(released, pressed, default, default, 1000, 800);
    }

    private static void AssertEntry(SpeciesCatalogEntry expected, SpeciesCatalogEntry actual) =>
        Assert.Equal(JsonSerializer.Serialize(expected), JsonSerializer.Serialize(actual));

    public void Dispose()
    {
        _runtime.Clear();
        Directory.Delete(_directory, recursive: true);
    }
}
