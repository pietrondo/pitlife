using Microsoft.Xna.Framework.Input;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class SpawnPanelTests
{
    [Fact]
    public void Categories_ContainEveryRegisteredSpecies()
    {
        string[] categories = ["Plants", "AquaticPlants", "LandHerbivores", "LandCarnivores", "LandOmnivores", "Birds", "Fish", "MarineMammals"];
        var panelSpecies = categories.SelectMany(SpawnPanel.SpeciesForCategory).ToHashSet();

        Assert.Equal(SpeciesRegistry.All.ToHashSet(), panelSpecies);
    }

    [Fact]
    public void ClickingButtons_UsesPanelRelativeCoordinates()
    {
        var panel = new SpawnPanel();
        panel.SetViewportHeight(720);
        panel.Open();
        var released = new MouseState(0, 0, 0, ButtonState.Released, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released);
        var emptyKbd = new KeyboardState();

        panel.Update(released, released, emptyKbd, emptyKbd);
        Click(panel, released, emptyKbd, 110, 118);
        Click(panel, released, emptyKbd, 110, 370);

        Assert.Equal("Plants", panel.SelectedCategory);
        Assert.Equal("Plant", panel.SelectedSpeciesKey);
    }

    private static void Click(SpawnPanel panel, MouseState released, KeyboardState emptyKbd, int x, int y)
    {
        var pressed = new MouseState(x, y, 0, ButtonState.Pressed, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released);
        Assert.True(panel.Update(pressed, released, emptyKbd, emptyKbd));
    }
}
