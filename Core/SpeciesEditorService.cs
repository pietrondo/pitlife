using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using PitLife.Simulation;

namespace PitLife.Core;

public sealed class SpeciesEditorService
{
    private readonly SpeciesCatalogRuntime _runtime;
    private readonly string _repositoryRoot;
    private readonly string _catalogPath;

    public SpeciesEditorService(
        SpeciesCatalogRuntime runtime,
        string repositoryRoot,
        string catalogPath)
    {
        ArgumentNullException.ThrowIfNull(runtime);
        ArgumentNullException.ThrowIfNull(repositoryRoot);
        ArgumentNullException.ThrowIfNull(catalogPath);
        _runtime = runtime;
        _repositoryRoot = repositoryRoot;
        _catalogPath = catalogPath;
    }

    public SpeciesCatalogDocument LoadDocument()
    {
        return File.Exists(_catalogPath)
            ? SpeciesCatalogStore.Load(_catalogPath)
            : new SpeciesCatalogDocument();
    }

    public IReadOnlyList<SpeciesCatalogValidationError> Add(SpeciesCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return SaveEntry(entry, replaceExisting: false);
    }

    public IReadOnlyList<SpeciesCatalogValidationError> Upsert(SpeciesCatalogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return SaveEntry(entry, replaceExisting: true);
    }

    private IReadOnlyList<SpeciesCatalogValidationError> SaveEntry(
        SpeciesCatalogEntry entry,
        bool replaceExisting)
    {
        SpeciesCatalogDocument document;
        try
        {
            document = LoadDocument();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or
            InvalidDataException or JsonException)
        {
            return
            [
                new SpeciesCatalogValidationError(-1, "Catalog", exception.Message)
            ];
        }

        var existingIndex = document.Species.FindIndex(candidate =>
            string.Equals(candidate.Key, entry.Key, StringComparison.Ordinal));
        if (existingIndex >= 0 && replaceExisting)
            document.Species[existingIndex] = entry;
        else
            document.Species.Add(entry);
        return _runtime.SaveAndApply(document, _catalogPath, _repositoryRoot);
    }
}
