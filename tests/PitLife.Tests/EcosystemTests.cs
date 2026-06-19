using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests;

public class EcosystemTests
{
    [Fact]
    public void Tick_AppliesSimulationSpeedExactlyOnce()
    {
        var ecosystem = new Ecosystem(32, 24, 7)
        {
            SimulationSpeed = 4f
        };
        var elapsed = TimeSpan.FromSeconds(0.1);

        ecosystem.Tick(new GameTime(elapsed, elapsed));

        Assert.Equal(0.4f, ecosystem.TotalTime, precision: 4);
    }
}
