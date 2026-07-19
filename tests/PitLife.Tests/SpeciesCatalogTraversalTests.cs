using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesCatalogTraversalTests
{
    [Fact]
    public void Load_ThrowsOnPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SpeciesCatalogStore.Load("../secret.json", "some_directory"));
    }

    [Fact]
    public void Save_ThrowsOnPathTraversal()
    {
        Assert.Throws<ArgumentException>(() => SpeciesCatalogStore.Save("../secret.json", new SpeciesCatalogDocument(), "some_directory"));
    }


    [Fact]
    public void Load_ThrowsOnAbsolutePathTraversal()
    {
        var absPath = Path.GetFullPath("/etc/passwd");
        if (Environment.OSVersion.Platform == PlatformID.Win32NT) absPath = @"C:\Windows\System32\config\SAM";
        Assert.Throws<UnauthorizedAccessException>(() => SpeciesCatalogStore.Load(absPath, "some_directory"));
    }

}
