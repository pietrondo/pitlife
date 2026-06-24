using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PitLife.Localization;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Core;

public sealed class SpeciesCatalogRuntime
{
    private readonly HashSet<string> _customKeys = new(StringComparer.Ordinal);

    public IReadOnlyCollection<string> CustomKeys => _customKeys;
    public event Action? CatalogChanged;

    public static string DefaultCatalogPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PitLife",
        "species.json");

    public IReadOnlyList<SpeciesCatalogValidationError> LoadAndApply(string path, string repositoryRoot)
    {
        if (!File.Exists(path))
            return Array.Empty<SpeciesCatalogValidationError>();

        try
        {
            return Apply(SpeciesCatalogStore.Load(path), repositoryRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or System.Text.Json.JsonException)
        {
            return
            [
                new SpeciesCatalogValidationError(-1, "Catalog", exception.Message)
            ];
        }
    }

    public IReadOnlyList<SpeciesCatalogValidationError> Apply(
        SpeciesCatalogDocument document,
        string repositoryRoot)
    {
        IReadOnlyList<SpeciesCatalogValidationError> errors = Validate(document, repositoryRoot);
        if (errors.Count > 0)
            return errors;

        Clear(notify: false);
        foreach (SpeciesCatalogEntry entry in document.Species)
        {
            SpeciesRegistry.Register(entry.ToDefinition());
            AssetRegistry.RegisterCustomSpeciesTexture(entry.Key, entry.TexturePath);
            I18n.RegisterCustomSpecies(entry.Key, entry.EnglishName, entry.ItalianName);
            _customKeys.Add(entry.Key);
        }

        CatalogChanged?.Invoke();
        return Array.Empty<SpeciesCatalogValidationError>();
    }

    public IReadOnlyList<SpeciesCatalogValidationError> Validate(
        SpeciesCatalogDocument document,
        string repositoryRoot)
    {
        string[] builtInKeys = SpeciesRegistry.All
            .Where(key => !_customKeys.Contains(key))
            .ToArray();
        return SpeciesCatalogValidator.Validate(document, repositoryRoot, builtInKeys);
    }

    public IReadOnlyList<SpeciesCatalogValidationError> SaveAndApply(
        SpeciesCatalogDocument document,
        string path,
        string repositoryRoot)
    {
        IReadOnlyList<SpeciesCatalogValidationError> errors = Validate(document, repositoryRoot);
        if (errors.Count > 0)
            return errors;

        try
        {
            SpeciesCatalogStore.Save(path, document, Path.GetDirectoryName(path)!);
            return Apply(document, repositoryRoot);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return
            [
                new SpeciesCatalogValidationError(-1, "Catalog", exception.Message)
            ];
        }
    }

    public void Clear() => Clear(notify: true);

    private void Clear(bool notify)
    {
        foreach (string key in _customKeys)
        {
            SpeciesRegistry.Unregister(key);
            AssetRegistry.UnregisterCustomSpeciesTexture(key);
            I18n.UnregisterCustomSpecies(key);
        }
        _customKeys.Clear();
        if (notify)
            CatalogChanged?.Invoke();
    }
}
