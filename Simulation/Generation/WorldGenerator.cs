using System;

namespace PitLife.Simulation;

public sealed class WorldGenerator
{
    private readonly World _world;
    private readonly Random _rng;

    public WorldGenerator(World world, int seed)
    {
        _world = world;
        _rng = new Random(seed);
    }

    public void Generate() => Generate(WorldGenOptions.Pangea());

    public void Generate(WorldGenOptions options)
    {
        new ContinentGenerator(_world, _rng).Generate(options);

        var features = new FeaturePlacer(_world, _rng);
        features.PlaceCoralReefs();
        features.PlaceCaves();
        features.PlaceVolcanoes();

        new RiverSystem(_world).CarveRivers(_rng.Next());

        var refiner = new TerrainRefiner(_world);
        refiner.SmoothTerrain();
        refiner.CopyEdgesForWrap();
        refiner.EnsureAllBiomesPresent();
    }
}
