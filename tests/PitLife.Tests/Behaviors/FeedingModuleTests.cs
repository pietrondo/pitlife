using Microsoft.Xna.Framework;
using PitLife.Simulation;

namespace PitLife.Tests.Behaviors;

public class FeedingModuleTests
{
    private Ecosystem CreateEcosystem()
    {
        var eco = new Ecosystem(64, 48, 42);
        eco.Initialize(0, 0, 0, 0); // Start empty
        return eco;
    }

    private Herbivore CreateHerbivore(Vector2 pos, Ecosystem eco)
    {
        var herb = new Herbivore(pos, Genome.Random(new Random(1)), "Deer");
        eco.AddCreature(herb);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        herb.Energy = herb.MaxEnergy * 0.9f;

        // Setup tile so creature can move
        var tx = (int)(pos.X / eco.World.TileSize);
        var ty = (int)(pos.Y / eco.World.TileSize);
        eco.World.Tiles[tx, ty] = new Tile(BiomeType.Grassland);
        return herb;
    }

    private Carnivore CreateCarnivore(Vector2 pos, Ecosystem eco)
    {
        var carn = new Carnivore(pos, Genome.Random(new Random(1)), "Wolf");
        eco.AddCreature(carn);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        carn.Energy = carn.MaxEnergy * 0.9f;
        var tx = (int)(pos.X / eco.World.TileSize);
        var ty = (int)(pos.Y / eco.World.TileSize);
        eco.World.Tiles[tx, ty] = new Tile(BiomeType.Grassland);
        return carn;
    }

    private Omnivore CreateOmnivore(Vector2 pos, Ecosystem eco)
    {
        var omn = new Omnivore(pos, Genome.Random(new Random(1)), "Bear");
        eco.AddCreature(omn);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        omn.Energy = omn.MaxEnergy * 0.9f;
        var tx = (int)(pos.X / eco.World.TileSize);
        var ty = (int)(pos.Y / eco.World.TileSize);
        eco.World.Tiles[tx, ty] = new Tile(BiomeType.Grassland);
        return omn;
    }

    private Plant CreatePlant(Vector2 pos, Ecosystem eco, float energy = 100f, bool poisonous = false)
    {
        var genome = Genome.Random(new Random(1));
        var plant = new Plant(pos, genome, "Clover")
        {
            Energy = energy,
            Toxicity = poisonous ? 1f : 0f
        };
        eco.AddCreature(plant);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        plant.Energy = energy;
        var tx = (int)(pos.X / eco.World.TileSize);
        var ty = (int)(pos.Y / eco.World.TileSize);
        eco.World.Tiles[tx, ty] = new Tile(BiomeType.Grassland);
        return plant;
    }

    [Fact]
    public void FeedingModule_Herbivore_NotHungry_DoesNotFeed()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);
        herb.Energy = herb.MaxEnergy * 0.7f; // Threshold is 0.6f for herbivores

        // Disable grass so TryFeedNearby doesn't graze
        var tx = (int)(herb.Position.X / eco.World.TileSize);
        var ty = (int)(herb.Position.Y / eco.World.TileSize);
        var tile = eco.World.Tiles[tx, ty];
        tile.GrassAmount = 0f;
        eco.World.Tiles[tx, ty] = tile;

        var module = new FeedingModule();
        var acted = module.Update(herb, eco.World, eco, 0.1f);

        Assert.False(acted);
    }

    [Fact]
    public void FeedingModule_Carnivore_NotHungry_DoesNotFeed()
    {
        var eco = CreateEcosystem();
        var carn = CreateCarnivore(new Vector2(10, 10), eco);
        carn.Energy = carn.MaxEnergy * 0.85f; // Threshold is 0.8f for carnivores

        var module = new FeedingModule();
        var acted = module.Update(carn, eco.World, eco, 0.1f);

        Assert.False(acted);
    }

    [Fact]
    public void FeedingModule_Herbivore_Hungry_EatsFruit()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);
        herb.Energy = herb.MaxEnergy * 0.5f;

        // Spawn fruit by accessing the _fruits array via reflection
        var field = eco.Fruits.GetType().GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fruitsArray = (Fruit[])field?.GetValue(eco.Fruits)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 50f, 100f, "Apple", poisonous: false, toxicity: 0f);
        var countField = eco.Fruits.GetType().GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        countField?.SetValue(eco.Fruits, 1);

        var module = new FeedingModule();
        var energyBefore = herb.Energy;
        var acted = module.Update(herb, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(herb.Energy > energyBefore);
    }

    [Fact]
    public void FeedingModule_Herbivore_Hungry_EatsToxicFruit()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);

        var genome = herb.Genome;
        genome.PlantRecognition = 0.1f; // Low recognition

        // Herbivore property doesn't allow setting genome directly easily so we create a new one
        var toxicHerb = new Herbivore(new Vector2(10, 10), genome, "Deer");
        eco.AddCreature(toxicHerb);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        toxicHerb.Energy = toxicHerb.MaxEnergy * 0.5f;

        var field = eco.Fruits.GetType().GetField("_fruits", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var fruitsArray = (Fruit[])field?.GetValue(eco.Fruits)!;
        fruitsArray[0] = new Fruit(new Vector2(10, 10), 50f, 100f, "ToxicApple", poisonous: true, toxicity: 1f);
        var countField = eco.Fruits.GetType().GetField("_fruitCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        countField?.SetValue(eco.Fruits, 1);

        var module = new FeedingModule();
        var energyBefore = toxicHerb.Energy;
        var acted = module.Update(toxicHerb, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(toxicHerb.Energy < energyBefore); // Toxic damage
    }

    [Fact]
    public void FeedingModule_Herbivore_Hungry_ConsumesPlant()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);
        herb.Energy = herb.MaxEnergy * 0.5f;

        var plant = CreatePlant(new Vector2(10, 10), eco, 100f);

        var module = new FeedingModule();
        var herbEnergyBefore = herb.Energy;
        var plantEnergyBefore = plant.Energy;

        var acted = module.Update(herb, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(herb.Energy > herbEnergyBefore);
        Assert.True(plant.Energy < plantEnergyBefore);
    }

    [Fact]
    public void FeedingModule_Herbivore_Hungry_MovesToPlant()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);
        herb.Energy = herb.MaxEnergy * 0.5f;

        var plant = CreatePlant(new Vector2(30, 30), eco, 100f);

        var module = new FeedingModule();
        var posBefore = herb.Position;

        var acted = module.Update(herb, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.NotEqual(posBefore, herb.Position);
    }

    [Fact]
    public void FeedingModule_Carnivore_Hungry_AttacksPrey()
    {
        var eco = CreateEcosystem();
        var carn = CreateCarnivore(new Vector2(10, 10), eco);
        carn.Energy = carn.MaxEnergy * 0.5f;

        var prey = CreateHerbivore(new Vector2(11, 10), eco);
        prey.Energy = 100f; // Ensure prey doesn't instantly die from low energy

        var module = new FeedingModule();
        var preyEnergyBefore = prey.Energy;
        var carnEnergyBefore = carn.Energy;

        var acted = module.Update(carn, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(prey.Energy < preyEnergyBefore);
        Assert.True(carn.Energy > carnEnergyBefore);
    }

    [Fact]
    public void FeedingModule_Carnivore_Hungry_AttacksPrey_DefenseMitigation()
    {
        var eco = CreateEcosystem();
        var carn = CreateCarnivore(new Vector2(10, 10), eco);
        carn.Energy = carn.MaxEnergy * 0.5f;

        // Weak prey
        var weakPreyGenome = Genome.Random(new Random(1));
        weakPreyGenome.Size = 0.1f;
        weakPreyGenome.Metabolism = 0.1f;
        var weakPrey = new Herbivore(new Vector2(11, 10), weakPreyGenome, "Deer");
        weakPrey.Energy = 100f;

        // Strong prey
        var strongPreyGenome = Genome.Random(new Random(2));
        strongPreyGenome.Size = 1.0f;
        strongPreyGenome.Metabolism = 1.0f;
        var strongPrey = new Herbivore(new Vector2(11, 10), strongPreyGenome, "Elk");
        strongPrey.Energy = 100f;

        // Ensure StrongPrey has higher defense than WeakPrey
        Assert.True(strongPrey.Defense > weakPrey.Defense);

        var module = new FeedingModule();

        // Attack weak prey
        var weakPreyEnergyBefore = weakPrey.Energy;

        // Set attack damage manually by extracting attack preys and calculating difference,
        // Or we can just run the module and use reflection if necessary, but module.Update does the attack
        // To isolate, we just need to measure energy difference.

        // Note: module.Update modifies carnivore energy and attacks the nearest prey
        eco.AddCreature(weakPrey);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        module.Update(carn, eco.World, eco, 0.1f);
        var damageToWeak = weakPreyEnergyBefore - weakPrey.Energy;

        // Clear grid and set up new state to attack strong prey
        eco.Creatures.Remove(weakPrey);
        var field = eco.GetType().GetField("_spatialGrid", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var grid = (SpatialGrid)field?.GetValue(eco)!;

        var bucketsField = grid.GetType().GetField("_buckets", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var locationsField = grid.GetType().GetField("_locations", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        var buckets = bucketsField?.GetValue(grid);
        buckets?.GetType().GetMethod("Clear")?.Invoke(buckets, null);

        var locations = locationsField?.GetValue(grid);
        locations?.GetType().GetMethod("Clear")?.Invoke(locations, null);
        grid.Update(carn);

        eco.AddCreature(strongPrey);
        eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        var strongPreyEnergyBefore = strongPrey.Energy;
        module.Update(carn, eco.World, eco, 0.1f);
        var damageToStrong = strongPreyEnergyBefore - strongPrey.Energy;

        Assert.True(damageToWeak > damageToStrong);
    }

    [Fact]
    public void FeedingModule_Herbivore_Starving_EdgeCase()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);

        // Starving creature (energy near 0)
        herb.Energy = 0.01f;

        var plant = CreatePlant(new Vector2(10, 10), eco, 100f);

        var module = new FeedingModule();
        var herbEnergyBefore = herb.Energy;

        var acted = module.Update(herb, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(herb.Energy > herbEnergyBefore); // Still feeds successfully
    }

    [Fact]
    public void FeedingModule_Carnivore_Hungry_ToxicPrey()
    {
        var eco = CreateEcosystem();
        var carn = CreateCarnivore(new Vector2(10, 10), eco);
        carn.Energy = carn.MaxEnergy * 0.5f;

        var prey = CreateHerbivore(new Vector2(11, 10), eco);
        prey.Energy = 100f;
        prey.Toxicity = 1.0f; // Toxic prey

        var module = new FeedingModule();
        var carnEnergyBefore = carn.Energy;

        var acted = module.Update(carn, eco.World, eco, 0.1f);

        Assert.True(acted);
        // Energy gain is reduced due to toxicity
        Assert.True(carn.Energy < carnEnergyBefore + carn.AttackDamage * 0.1f * 1.5f);
    }

    [Fact]
    public void FeedingModule_Omnivore_Hungry_ConsumesPlant()
    {
        var eco = CreateEcosystem();
        var omn = CreateOmnivore(new Vector2(10, 10), eco);
        omn.Energy = omn.MaxEnergy * 0.5f; // Needs to be > 0.4f to not seek prey based on HuntAsOmnivore check

        var plant = CreatePlant(new Vector2(10, 10), eco, 100f);

        var module = new FeedingModule();
        var omnEnergyBefore = omn.Energy;
        var plantEnergyBefore = plant.Energy;

        var acted = module.Update(omn, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(omn.Energy > omnEnergyBefore);
        Assert.True(plant.Energy < plantEnergyBefore);
    }

    [Fact]
    public void FeedingModule_ScavengeCarcass_Works()
    {
        var eco = CreateEcosystem();
        var carn = CreateCarnivore(new Vector2(10, 10), eco);
        carn.Energy = carn.MaxEnergy * 0.5f;

        var carcass = CreateHerbivore(new Vector2(11, 10), eco);
        carcass.Energy = 100f; // Some energy left in carcass
        // To make it a carcass, it must be dead
        typeof(Creature).GetProperty("IsAlive")?.SetValue(carcass, false);

        var module = new FeedingModule();
        var carnEnergyBefore = carn.Energy;

        var acted = module.Update(carn, eco.World, eco, 0.1f);

        Assert.True(acted);
        Assert.True(carn.Energy > carnEnergyBefore);
    }

    [Fact]
    public void FeedingModule_TryGraze_EatsGrass()
    {
        var eco = CreateEcosystem();
        var herb = CreateHerbivore(new Vector2(10, 10), eco);
        herb.Energy = herb.MaxEnergy * 0.5f;

        var tile = eco.World.GetTileAtPosition(10, 10);
        tile.GrassAmount = 1.0f; // Set grass amount

        // Use TryGraze directly (it's internal static, we need to access via reflection or Update)
        var module = new FeedingModule();
        var herbEnergyBefore = herb.Energy;

        var acted = module.Update(herb, eco.World, eco, 0.1f);

        // Herbivore feeds from grass if no plant/fruit found
        Assert.True(acted);
        Assert.True(herb.Energy > herbEnergyBefore);
    }
}
