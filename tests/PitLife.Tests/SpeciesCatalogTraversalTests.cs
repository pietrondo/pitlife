using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesCatalogTraversalTests
{
    [Fact]
    public void Load_ThrowsOnPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SpeciesCatalogStore.Load("../secret.json"));
    }

    [Fact]
    public void Save_ThrowsOnPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SpeciesCatalogStore.Save("../secret.json", new SpeciesCatalogDocument(), "some_directory"));
    }
}
