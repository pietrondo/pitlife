using System;

namespace PitLife.Simulation;

public struct ReproductionData
{
    public float LastReproductionTime;

    public void Reset()
    {
        LastReproductionTime = -60f;
    }

    public int GetLitterSize(Genome genome)
    {
        return Math.Max(1, (int)(genome.Size * Core.BalanceConfig.Data.Creature.LitterSizeMultiplier));
    }

    public float GetReproductionCooldown(Genome genome)
    {
        return Core.BalanceConfig.Data.Creature.ReproductionCooldownBase + (1f - genome.Metabolism) * Core.BalanceConfig.Data.Creature.ReproductionCooldownMetabolismFactor;
    }

    public float GetReproductionThreshold(float maxEnergy)
    {
        return maxEnergy * Core.BalanceConfig.Data.Creature.ReproductionThresholdRatio;
    }
}
