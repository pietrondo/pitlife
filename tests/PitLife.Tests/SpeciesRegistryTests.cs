using PitLife.Simulation;

namespace PitLife.Tests;

public class SpeciesRegistryTests
{
    [Fact]
    public void Get_ReturnsDefinition_ForKnownSpecies()
    {
        var def = SpeciesRegistry.Get("Shark");
        Assert.NotNull(def);
        Assert.Equal(CreatureType.Carnivore, def!.Kind);
        Assert.True(def.IsAquatic);
    }

    [Fact]
    public void Get_ReturnsNull_ForUnknownSpecies()
    {
        Assert.Null(SpeciesRegistry.Get("Unicorn"));
    }

    [Fact]
    public void OfType_ReturnsOnlyPlants_ForPlantKind()
    {
        var plants = SpeciesRegistry.OfType(CreatureType.Plant).ToList();
        Assert.NotEmpty(plants);
        Assert.All(plants, p => Assert.Equal(CreatureType.Plant, SpeciesRegistry.Get(p)!.Kind));
    }

    [Fact]
    public void IsPackAnimal_TrueForDeer()
    {
        Assert.True(SpeciesRegistry.IsPackAnimal("Deer"));
    }

    [Fact]
    public void IsSolitary_TrueForTiger()
    {
        Assert.True(SpeciesRegistry.IsSolitary("Tiger"));
    }

    [Fact]
    public void IsValidBiome_SharkOnlyInDeepOcean()
    {
        Assert.True(SpeciesRegistry.IsValidBiome("Shark", BiomeType.DeepOcean));
        Assert.False(SpeciesRegistry.IsValidBiome("Shark", BiomeType.ShallowWater));
        Assert.False(SpeciesRegistry.IsValidBiome("Shark", BiomeType.Grassland));
    }

    [Fact]
    public void IsValidBiome_PlantRejectedInOcean()
    {
        Assert.False(SpeciesRegistry.IsValidBiome("Plant", BiomeType.DeepOcean));
        Assert.False(SpeciesRegistry.IsValidBiome("Plant", BiomeType.ShallowWater));
        Assert.True(SpeciesRegistry.IsValidBiome("Plant", BiomeType.Grassland));
    }

    [Fact]
    public void All_ContainsAllRegisteredSpecies()
    {
        Assert.Contains("Shark", SpeciesRegistry.All);
        Assert.Contains("Plant", SpeciesRegistry.All);
        Assert.Contains("Bear", SpeciesRegistry.All);
        Assert.Contains("Wolf", SpeciesRegistry.All);
    }
}
