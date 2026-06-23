using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.UI;

public sealed class SpawnPanelState
{
    public bool IsOpen { get; private set; }
    public string? SelectedSpeciesKey { get; private set; }
    public string? SelectedCategory { get; private set; }
    public string? SelectedCataclysm { get; set; }
    public bool ShowCataclysms { get; set; }
    public int ScrollOffset { get; set; }
    public int MaxScroll { get; set; }

    public static IReadOnlyList<string> SpeciesForCategory(string category) =>
        SpeciesCatalogModel.Build().TryGetValue(category, out var species) ? species : Array.Empty<string>();

    public static string[] CategoryOrder { get; } = ["Plants", "AquaticPlants", "Herbivores", "Carnivores", "Omnivores"];

    public void Open() => IsOpen = true;
    public void Close()
    {
        IsOpen = false;
        SelectedSpeciesKey = null;
        SelectedCategory = null;
        ScrollOffset = 0;
    }
    public void Toggle() => IsOpen = !IsOpen;
    public void DeselectSpecies() => SelectedSpeciesKey = null;

    public bool SelectCategory(string? clickedKey)
    {
        var newCategory = clickedKey == SelectedCategory ? null : clickedKey;
        if (newCategory == SelectedCategory) return false;
        SelectedCategory = newCategory;
        SelectedSpeciesKey = null;
        ScrollOffset = 0;
        return true;
    }

    public void SelectSpecies(string? key)
    {
        SelectedSpeciesKey = key;
    }
}
