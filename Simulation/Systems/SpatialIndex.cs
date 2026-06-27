using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PitLife.Simulation;

public class SpatialIndex
{
    private readonly SpatialGrid _grid;

    public SpatialIndex(int width, int height, int cellSize)
    {
        _grid = new SpatialGrid(width, height, cellSize);
    }

    public void Rebuild(IEnumerable<Creature> creatures) => _grid.Rebuild(creatures);
    public void Update(Creature creature) => _grid.Update(creature);
    public void Remove(Creature creature) => _grid.Remove(creature);

    public Plant? FindNearestPlant(Herbivore seeker) => FindNearest<Plant>(seeker);
    public Plant? FindNearestPlantFor(Creature seeker) => FindNearest<Plant>(seeker);

    public Creature? FindNearestPrey(Creature seeker)
    {
        return _grid.FindNearest(seeker, c =>
            c.CreatureType != CreatureType.Carnivore &&
            c.CreatureType != seeker.CreatureType &&
            (c.CreatureType != CreatureType.Plant || seeker is not Herbivore and not Omnivore));
    }

    public Creature? FindNearestSameSpecies(Creature seeker)
    {
        return _grid.FindNearest(seeker, c => c != seeker && c.Species == seeker.Species);
    }

    public Creature? FindNearestMate(Creature seeker)
    {
        if (!seeker.IsAdult || seeker.Gender is not (Gender.Male or Gender.Female))
            return null;
        return _grid.FindNearest(seeker, seeker.CanMateWith);
    }

    public Creature? FindNearestPredator(Creature seeker)
    {
        return _grid.FindNearest(seeker, c => c.CreatureType == CreatureType.Carnivore);
    }

    private T? FindNearest<T>(Creature seeker) where T : Creature
    {
        return _grid.FindNearest(seeker, c => c is T) as T;
    }

    public List<Creature> FindNeighbors(Creature seeker, float radius, Func<Creature, bool> predicate)
    {
        return _grid.GetNeighbors(seeker, radius, predicate);
    }
}
