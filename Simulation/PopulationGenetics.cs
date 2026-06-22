using System;
using System.Collections.Generic;
using System.Linq;

namespace PitLife.Simulation;

public readonly record struct PopulationGeneticMetrics(
    int PopulationSize,
    float MeanIndividualHeterozygosity,
    float ExpectedMarkerHeterozygosity,
    int PolymorphicMarkerCount,
    float MeanInbreedingCoefficient);

public static class PopulationGenetics
{
    public static PopulationGeneticMetrics Calculate(
        IEnumerable<Creature> creatures,
        string? species = null)
    {
        Creature[] population = creatures
            .Where(creature => creature.IsAlive &&
                (species is null || string.Equals(creature.Species, species, StringComparison.Ordinal)))
            .ToArray();
        if (population.Length == 0)
            return new PopulationGeneticMetrics(0, 0f, 0f, 0, 0f);

        GeneticProfile[] profiles = population
            .Select(creature => creature.Genome.EnsureGeneticProfile())
            .ToArray();
        float individualHeterozygosity = profiles.Average(profile => profile.Heterozygosity);
        float expectedHeterozygosity = 0f;
        int polymorphicMarkers = 0;
        int alleleCount = profiles.Length * 2;
        for (int marker = 0; marker < 64; marker++)
        {
            ulong bit = 1UL << marker;
            int ones = 0;
            foreach (GeneticProfile profile in profiles)
            {
                if ((profile.MarkerHaplotypeA & bit) != 0) ones++;
                if ((profile.MarkerHaplotypeB & bit) != 0) ones++;
            }
            float frequency = ones / (float)alleleCount;
            expectedHeterozygosity += 2f * frequency * (1f - frequency);
            if (ones > 0 && ones < alleleCount)
                polymorphicMarkers++;
        }

        return new PopulationGeneticMetrics(
            population.Length,
            individualHeterozygosity,
            expectedHeterozygosity / 64f,
            polymorphicMarkers,
            population.Average(creature => creature.InbreedingCoefficient));
    }
}
