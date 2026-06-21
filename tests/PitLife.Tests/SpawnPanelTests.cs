using Microsoft.Xna.Framework.Input;
using PitLife.Simulation;
using PitLife.UI;

namespace PitLife.Tests;

public class SpawnPanelTests
{
    [Fact]
    public void Categories_ContainEveryRegisteredSpecies()
    {
        string[] categories = ["Plants", "AquaticPlants", "Herbivores", "Carnivores", "Omnivores"];
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

        panel.Update(released, released);
        Click(panel, released, 30, 110);
        Click(panel, released, 40, 270);

        Assert.Equal("Plants", panel.SelectedCategory);
        Assert.Equal("Plant", panel.SelectedSpeciesKey);
    }

    private static void Click(SpawnPanel panel, MouseState released, int x, int y)
    {
        var pressed = new MouseState(x, y, 0, ButtonState.Pressed, ButtonState.Released,
            ButtonState.Released, ButtonState.Released, ButtonState.Released);
        Assert.True(panel.Update(pressed, released));
    }
}
