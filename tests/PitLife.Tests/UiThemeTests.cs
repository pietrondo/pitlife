using Microsoft.Xna.Framework;
using PitLife.UI;
using Xunit;

namespace PitLife.Tests;

public class UiThemeTests
{
    [Fact]
    public void Colors_HaveExpectedValues()
    {
        Assert.Equal(new Color(20, 37, 29), UiTheme.ForestNight);
        Assert.Equal(new Color(11, 23, 18), UiTheme.DeepGrove);
        Assert.Equal(new Color(120, 200, 80), UiTheme.MossSignal);
        Assert.Equal(new Color(78, 156, 181), UiTheme.LakeBlue);
        Assert.Equal(new Color(242, 230, 201), UiTheme.WarmParchment);
        Assert.Equal(new Color(107, 81, 55), UiTheme.BarkEdge);
        Assert.Equal(new Color(169, 181, 167), UiTheme.MutedStone);
        Assert.Equal(new Color(200, 90, 74), UiTheme.DangerClay);
        Assert.Equal(new Color(5, 13, 10, 210), UiTheme.MenuScrim);
        Assert.Equal(new Color(3, 8, 6, 190), UiTheme.Shadow);
    }
}
