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
        Assert.False(SpeciesRegistry.IsValidBiome("Clover", BiomeType.DeepOcean));
        Assert.False(SpeciesRegistry.IsValidBiome("Clover", BiomeType.ShallowWater));
        Assert.True(SpeciesRegistry.IsValidBiome("Clover", BiomeType.Grassland));
    }

    [Fact]
    public void All_ContainsAllRegisteredSpecies()
    {
        Assert.Contains("Shark", SpeciesRegistry.All);
        Assert.Contains("Clover", SpeciesRegistry.All);
        Assert.Contains("Bear", SpeciesRegistry.All);
        Assert.Contains("Wolf", SpeciesRegistry.All);
    }

    [Fact]
    public void NewSpecies_HaveExpectedKindsAndBiomes()
    {
        Assert.Equal(CreatureType.Herbivore, SpeciesRegistry.Get("Moose")!.Kind);
        Assert.Equal(CreatureType.Omnivore, SpeciesRegistry.Get("Badger")!.Kind);
        Assert.Equal(CreatureType.Carnivore, SpeciesRegistry.Get("Owl")!.Kind);
        Assert.True(SpeciesRegistry.IsValidBiome("Fern", BiomeType.Swamp));
        Assert.False(SpeciesRegistry.IsValidBiome("Fern", BiomeType.Desert));
        Assert.True(SpeciesRegistry.IsValidBiome("Sunflower", BiomeType.Grassland));
    }

    [Fact]
    public void Plants_DeclareConsistentReproductionModes()
    {
        foreach (var species in SpeciesRegistry.OfType(CreatureType.Plant))
        {
            SpeciesDefinition definition = SpeciesRegistry.Get(species)!;
            Assert.NotNull(definition.PlantReproduction);

            if (definition.PlantReproduction != PlantReproductionMode.Seeds)
                Assert.Equal(PollinationMode.None, definition.Pollination);
        }

        Assert.Equal(PlantReproductionMode.Spores, SpeciesRegistry.Get("Fern")!.PlantReproduction);
        Assert.Equal(PlantReproductionMode.Spores, SpeciesRegistry.Get("Chanterelle")!.PlantReproduction);
        Assert.Equal(PlantReproductionMode.Seeds, SpeciesRegistry.Get("Lavender")!.PlantReproduction);
        Assert.Equal(PollinationMode.Insects, SpeciesRegistry.Get("Lavender")!.Pollination);
        Assert.Equal(PlantReproductionMode.Fragmentation, SpeciesRegistry.Get("Algae")!.PlantReproduction);
        Assert.Equal(PlantReproductionMode.BroadcastSpawning, SpeciesRegistry.Get("Coral")!.PlantReproduction);
    }

    [Fact]
    public void Animals_DoNotExposePlantReproduction()
    {
        foreach (CreatureType kind in new[] { CreatureType.Herbivore, CreatureType.Carnivore, CreatureType.Omnivore })
        {
            foreach (var species in SpeciesRegistry.OfType(kind))
            {
                SpeciesDefinition definition = SpeciesRegistry.Get(species)!;
                Assert.Null(definition.PlantReproduction);
                Assert.Equal(PollinationMode.None, definition.Pollination);
            }
        }
    }
}
