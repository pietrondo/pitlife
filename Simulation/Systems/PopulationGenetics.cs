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
        int popSize = 0;
        float sumHetero = 0f;
        float sumInbreeding = 0f;

        Span<int> onesCounts = stackalloc int[64];

        if (creatures is List<Creature> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var creature = list[i];
                if (creature == null || !creature.IsAlive) continue;
                if (species != null && !string.Equals(creature.Species, species, StringComparison.Ordinal)) continue;

                popSize++;
                var profile = creature.Genome.EnsureGeneticProfile();
                sumHetero += profile.Heterozygosity;
                sumInbreeding += creature.InbreedingCoefficient;

                for (int marker = 0; marker < 64; marker++)
                {
                    ulong bit = 1UL << marker;
                    if ((profile.MarkerHaplotypeA & bit) != 0) onesCounts[marker]++;
                    if ((profile.MarkerHaplotypeB & bit) != 0) onesCounts[marker]++;
                }
            }
        }
        else
        {
            foreach (var creature in creatures)
            {
                if (creature == null || !creature.IsAlive) continue;
                if (species != null && !string.Equals(creature.Species, species, StringComparison.Ordinal)) continue;

                popSize++;
                var profile = creature.Genome.EnsureGeneticProfile();
                sumHetero += profile.Heterozygosity;
                sumInbreeding += creature.InbreedingCoefficient;

                for (int marker = 0; marker < 64; marker++)
                {
                    ulong bit = 1UL << marker;
                    if ((profile.MarkerHaplotypeA & bit) != 0) onesCounts[marker]++;
                    if ((profile.MarkerHaplotypeB & bit) != 0) onesCounts[marker]++;
                }
            }
        }

        if (popSize == 0)
            return new PopulationGeneticMetrics(0, 0f, 0f, 0, 0f);

        float expectedHeterozygosity = 0f;
        int polymorphicMarkers = 0;
        int alleleCount = popSize * 2;

        for (int marker = 0; marker < 64; marker++)
        {
            int ones = onesCounts[marker];
            float frequency = ones / (float)alleleCount;
            expectedHeterozygosity += 2f * frequency * (1f - frequency);
            if (ones > 0 && ones < alleleCount)
                polymorphicMarkers++;
        }

        return new PopulationGeneticMetrics(
            popSize,
            sumHetero / popSize,
            expectedHeterozygosity / 64f,
            polymorphicMarkers,
            sumInbreeding / popSize);
    }
}
