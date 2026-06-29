using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using PitLife.UI;
using PitLife.Simulation;
using Xunit;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

namespace PitLife.Tests;

public class SpawnPanelFilterTests
{
    [Fact]
    public void RefreshSpeciesCatalog_PopulatesCategories()
    {
        var panel = new SpawnPanel();
        panel.SetViewportHeight(720);
        panel.Open();

        // Simulate click on "Herbivores" category. Plant is at 118, AquaticPlants is 118+24+2+6 = 150, Herbivores is 182
        var emptyKbd = new KeyboardState();
        var released = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var pressed = new MouseState(110, 118 + (32) * 2, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);

        panel.Update(pressed, released, emptyKbd, emptyKbd);

        Assert.Equal("Herbivores", panel.SelectedCategory);

        var buttonsField = typeof(SpawnPanel).GetField("_speciesButtons", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var buttons = (List<UiButton>)buttonsField.GetValue(panel)!;
        Assert.NotEmpty(buttons);
    }
    
    [Fact]
    public void SpawnPanel_Filter_UpdatesSpeciesList()
    {
        var panel = new SpawnPanel();
        panel.SetViewportHeight(720);
        panel.Open();

        var emptyKbd = new KeyboardState();
        var released = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var pressed = new MouseState(110, 118 + (32) * 2, 0, ButtonState.Pressed, ButtonState.Released, ButtonState.Released, ButtonState.Released, ButtonState.Released);
        panel.Update(pressed, released, emptyKbd, emptyKbd);
        
        var buttonsField = typeof(SpawnPanel).GetField("_speciesButtons", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var buttonsBefore = (List<UiButton>)buttonsField.GetValue(panel)!;
        var countBefore = buttonsBefore.Count;
        
        var searchInputField = typeof(SpawnPanel).GetField("_searchInput", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var searchInput = (UiTextInput)searchInputField.GetValue(panel)!;
        
        searchInput.SetText("Deer");
        
        panel.Update(released, pressed, emptyKbd, emptyKbd); // Need to trigger RebuildSpeciesButtons
        
        var buttonsAfter = (List<UiButton>)buttonsField.GetValue(panel)!;
        Assert.True(buttonsAfter.Count < countBefore);
        Assert.Contains(buttonsAfter, b => (string)b.Tag! == "Deer");
    }
}
