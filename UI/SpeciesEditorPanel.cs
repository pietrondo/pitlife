using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using PitLife.Core;
using PitLife.Localization;
using PitLife.Simulation;

namespace PitLife.UI;

public sealed class SpeciesEditorPanel
{
    private static readonly CreatureType[] Kinds =
        [CreatureType.Herbivore, CreatureType.Carnivore, CreatureType.Omnivore, CreatureType.Plant];
    private static readonly SocialBehavior[] SocialBehaviors =
        [SocialBehavior.Solitary, SocialBehavior.Pair, SocialBehavior.Herd, SocialBehavior.Pack,
            SocialBehavior.School, SocialBehavior.Swarm];
    private static readonly PlantReproductionMode[] PlantReproductionModes =
        [PlantReproductionMode.Seeds, PlantReproductionMode.Spores, PlantReproductionMode.Vegetative,
            PlantReproductionMode.Fragmentation, PlantReproductionMode.BroadcastSpawning];
    private static readonly BiomePreset[] BiomePresets =
    [
        new("Land", [BiomeType.Beach, BiomeType.Desert, BiomeType.Savanna, BiomeType.Grassland,
            BiomeType.Forest, BiomeType.DenseForest, BiomeType.Swamp, BiomeType.Tundra,
            BiomeType.Mountain, BiomeType.Snow]),
        new("Forest", [BiomeType.Forest, BiomeType.DenseForest]),
        new("Grassland", [BiomeType.Savanna, BiomeType.Grassland]),
        new("Desert", [BiomeType.Desert, BiomeType.Savanna]),
        new("Cold", [BiomeType.Tundra, BiomeType.Mountain, BiomeType.Snow]),
        new("Wetland", [BiomeType.Swamp, BiomeType.ShallowWater]),
        new("Water", [BiomeType.ShallowWater, BiomeType.DeepOcean])
    ];

    private readonly SpeciesEditorService _service;
    private readonly UiWindow _window = new("Species Editor", "species-editor")
    {
        Bounds = new Rectangle(300, 90, 680, 610),
        ShowCloseButton = true
    };
    private readonly UiTextInput _key = new() { Placeholder = "SpeciesKey", MaxLength = 40 };
    private readonly UiTextInput _englishName = new() { Placeholder = "English name", MaxLength = 40 };
    private readonly UiTextInput _italianName = new() { Placeholder = "Nome italiano", MaxLength = 40 };
    private readonly UiTextInput _texturePath = new()
    {
        Placeholder = "Content/assets/.../sprite.png",
        MaxLength = 180
    };
    private readonly UiButton _kind = new("");
    private readonly UiButton _social = new("");
    private readonly UiButton _biomes = new("");
    private readonly UiButton _reproduction = new("");
    private readonly UiButton _load = new("LOAD");
    private readonly UiButton _clone = new("CLONE");
    private readonly UiButton _save = new("SAVE") { ShortcutHint = "ENTER" };
    private readonly UiButton _clear = new("CLEAR");

    private int _kindIndex;
    private int _socialIndex;
    private int _biomeIndex;
    private int _reproductionIndex;
    private int _loadedIndex = -1;
    private string _status = "";
    private bool _statusIsError;

    public bool IsOpen => _window.IsOpen;

    public SpeciesEditorPanel(SpeciesEditorService service) => _service = service;

    public void Toggle() => _window.IsOpen = !_window.IsOpen;
    public void Close() => _window.IsOpen = false;

    public bool Update(
        MouseState mouse,
        MouseState previousMouse,
        KeyboardState keyboard,
        KeyboardState previousKeyboard,
        int viewportWidth,
        int viewportHeight)
    {
        if (!IsOpen)
            return false;

        Layout(viewportWidth, viewportHeight);
        if (_window.CloseButtonBounds.Contains(mouse.Position) &&
            mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed)
        {
            Close();
            return true;
        }

        _key.Update(keyboard, previousKeyboard, mouse, previousMouse);
        _englishName.Update(keyboard, previousKeyboard, mouse, previousMouse);
        _italianName.Update(keyboard, previousKeyboard, mouse, previousMouse);
        _texturePath.Update(keyboard, previousKeyboard, mouse, previousMouse);

        if (_kind.WasClicked(mouse, previousMouse))
            _kindIndex = (_kindIndex + 1) % Kinds.Length;
        if (_social.WasClicked(mouse, previousMouse))
            _socialIndex = (_socialIndex + 1) % SocialBehaviors.Length;
        if (_biomes.WasClicked(mouse, previousMouse))
            _biomeIndex = (_biomeIndex + 1) % BiomePresets.Length;
        if (_reproduction.WasClicked(mouse, previousMouse))
            _reproductionIndex = (_reproductionIndex + 1) % PlantReproductionModes.Length;
        if (_load.WasClicked(mouse, previousMouse))
            LoadNext(clone: false);
        if (_clone.WasClicked(mouse, previousMouse))
            LoadNext(clone: true);
        if (_clear.WasClicked(mouse, previousMouse))
            ClearDraft();
        if (_save.WasClicked(mouse, previousMouse))
            SaveDraft();

        RefreshButtonText();
        return true;
    }

    public void Draw(SpriteBatch spriteBatch, Texture2D pixel, SpriteFont font, MouseState mouse,
        int viewportWidth, int viewportHeight)
    {
        if (!IsOpen)
            return;

        Layout(viewportWidth, viewportHeight);
        _window.Title = I18n.T("editor.title");
        _window.Draw(spriteBatch, pixel, font, true, mouse.Position);
        Rectangle content = _window.ContentBounds;
        DrawLabel(spriteBatch, font, content.X, _key.Bounds.Y - 18, I18n.T("editor.key"));
        DrawLabel(spriteBatch, font, content.X, _englishName.Bounds.Y - 18, I18n.T("editor.englishName"));
        DrawLabel(spriteBatch, font, content.X, _italianName.Bounds.Y - 18, I18n.T("editor.italianName"));
        DrawLabel(spriteBatch, font, content.X, _texturePath.Bounds.Y - 18, I18n.T("editor.texture"));
        _key.Draw(spriteBatch, pixel, font, mouse);
        _englishName.Draw(spriteBatch, pixel, font, mouse);
        _italianName.Draw(spriteBatch, pixel, font, mouse);
        _texturePath.Draw(spriteBatch, pixel, font, mouse);

        RefreshButtonText();
        _kind.Draw(spriteBatch, pixel, font, mouse, false);
        _social.Draw(spriteBatch, pixel, font, mouse, false);
        _biomes.Draw(spriteBatch, pixel, font, mouse, false);
        if (Kinds[_kindIndex] == CreatureType.Plant)
            _reproduction.Draw(spriteBatch, pixel, font, mouse, false);
        _load.Draw(spriteBatch, pixel, font, mouse, false);
        _clone.Draw(spriteBatch, pixel, font, mouse, false);
        _clear.Draw(spriteBatch, pixel, font, mouse, false);
        _save.Draw(spriteBatch, pixel, font, mouse, false);

        if (_status.Length > 0)
        {
            spriteBatch.DrawString(font, _status,
                new Vector2(content.X, content.Bottom - 24),
                _statusIsError ? UiTheme.DangerClay : UiTheme.MossSignal);
        }
    }

    internal SpeciesCatalogEntry BuildDraft()
    {
        CreatureType kind = Kinds[_kindIndex];
        PlantReproductionMode? reproduction = kind == CreatureType.Plant
            ? PlantReproductionModes[_reproductionIndex]
            : null;
        return new SpeciesCatalogEntry
        {
            Key = _key.Text.Trim(),
            EnglishName = _englishName.Text.Trim(),
            ItalianName = _italianName.Text.Trim(),
            Kind = kind,
            IsAquatic = BiomePresets[_biomeIndex].Name == "Water",
            SocialBehavior = kind == CreatureType.Plant ? SocialBehavior.None : SocialBehaviors[_socialIndex],
            ValidBiomes = new List<BiomeType>(BiomePresets[_biomeIndex].Biomes),
            DefaultSize = kind switch
            {
                CreatureType.Plant => 0.8f,
                CreatureType.Herbivore => 1f,
                CreatureType.Carnivore => 1f,
                CreatureType.Omnivore => 0.9f,
                _ => 1f
            },
            MaturityAge = kind == CreatureType.Plant ? 15f : 30f,
            TexturePath = _texturePath.Text.Trim(),
            PlantReproduction = reproduction,
            Pollination = reproduction == PlantReproductionMode.Seeds
                ? PollinationMode.Insects
                : PollinationMode.None
        };
    }

    private void SaveDraft()
    {
        IReadOnlyList<SpeciesCatalogValidationError> errors = _service.Upsert(BuildDraft());
        if (errors.Count > 0)
        {
            SpeciesCatalogValidationError error = errors[0];
            _status = $"{error.Field}: {error.Message}";
            _statusIsError = true;
            return;
        }

        _status = I18n.T("editor.saved");
        _statusIsError = false;
    }

    private void LoadNext(bool clone)
    {
        SpeciesCatalogDocument document;
        try
        {
            document = _service.LoadDocument();
        }
        catch (Exception exception)
        {
            _status = exception.Message;
            _statusIsError = true;
            return;
        }

        if (document.Species.Count == 0)
        {
            _status = I18n.T("editor.empty");
            _statusIsError = true;
            return;
        }

        _loadedIndex = (_loadedIndex + 1) % document.Species.Count;
        SpeciesCatalogEntry entry = document.Species[_loadedIndex];
        _key.SetText(clone ? entry.Key + "Copy" : entry.Key);
        _englishName.SetText(clone ? entry.EnglishName + " Copy" : entry.EnglishName);
        _italianName.SetText(clone ? entry.ItalianName + " copia" : entry.ItalianName);
        _texturePath.SetText(entry.TexturePath);
        _kindIndex = Array.IndexOf(Kinds, entry.Kind);
        _socialIndex = Math.Max(0, Array.IndexOf(SocialBehaviors, entry.SocialBehavior));
        _reproductionIndex = Math.Max(0, Array.IndexOf(
            PlantReproductionModes,
            entry.PlantReproduction ?? PlantReproductionMode.Seeds));
        _biomeIndex = FindBestBiomePreset(entry.ValidBiomes);
        _status = clone ? I18n.T("editor.cloned") : I18n.T("editor.loaded");
        _statusIsError = false;
    }

    private int FindBestBiomePreset(IReadOnlyCollection<BiomeType> biomes)
    {
        for (var i = 0; i < BiomePresets.Length; i++)
        {
            if (new HashSet<BiomeType>(BiomePresets[i].Biomes).SetEquals(biomes))
                return i;
        }
        return 0;
    }

    private void ClearDraft()
    {
        _key.Clear();
        _englishName.Clear();
        _italianName.Clear();
        _texturePath.Clear();
        _status = "";
    }

    private void Layout(int viewportWidth, int viewportHeight)
    {
        var width = Math.Min(680, viewportWidth - 32);
        var height = Math.Min(610, viewportHeight - 80);
        _window.Bounds = new Rectangle((viewportWidth - width) / 2, Math.Max(56, (viewportHeight - height) / 2), width, height);
        Rectangle content = _window.ContentBounds;
        var fieldWidth = content.Width;
        _key.Bounds = new Rectangle(content.X, content.Y + 18, fieldWidth, 36);
        _englishName.Bounds = new Rectangle(content.X, content.Y + 82, fieldWidth, 36);
        _italianName.Bounds = new Rectangle(content.X, content.Y + 146, fieldWidth, 36);
        _texturePath.Bounds = new Rectangle(content.X, content.Y + 210, fieldWidth, 36);

        var half = (fieldWidth - 10) / 2;
        _kind.Bounds = new Rectangle(content.X, content.Y + 266, half, 38);
        _social.Bounds = new Rectangle(content.X + half + 10, content.Y + 266, half, 38);
        _biomes.Bounds = new Rectangle(content.X, content.Y + 314, half, 38);
        _reproduction.Bounds = new Rectangle(content.X + half + 10, content.Y + 314, half, 38);
        var quarter = (fieldWidth - 30) / 4;
        _load.Bounds = new Rectangle(content.X, content.Y + 372, quarter, 38);
        _clone.Bounds = new Rectangle(content.X + quarter + 10, content.Y + 372, quarter, 38);
        _clear.Bounds = new Rectangle(content.X + (quarter + 10) * 2, content.Y + 372, quarter, 38);
        _save.Bounds = new Rectangle(content.X + (quarter + 10) * 3, content.Y + 372, quarter, 38);
    }

    private void RefreshButtonText()
    {
        CreatureType kind = Kinds[_kindIndex];
        _kind.Text = $"{I18n.T("editor.kind")}: {I18n.CreatureTypeName(kind)}";
        _social.Text = kind == CreatureType.Plant
            ? $"{I18n.T("editor.social")}: -"
            : $"{I18n.T("editor.social")}: {SocialBehaviors[_socialIndex]}";
        _biomes.Text = $"{I18n.T("editor.biomes")}: {BiomePresets[_biomeIndex].Name}";
        _reproduction.Text = $"{I18n.T("editor.reproduction")}: {PlantReproductionModes[_reproductionIndex]}";
        _load.Text = I18n.T("editor.load");
        _clone.Text = I18n.T("editor.clone");
        _save.Text = I18n.T("editor.save");
        _clear.Text = I18n.T("editor.clear");
    }

    private static void DrawLabel(SpriteBatch spriteBatch, SpriteFont font, int x, int y, string text) =>
        spriteBatch.DrawString(font, text, new Vector2(x, y), UiTheme.MutedStone);

    private sealed record BiomePreset(string Name, BiomeType[] Biomes);
}
