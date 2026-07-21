namespace PitLife.Core;

public static class GeneticsConfig
{
    public static GeneticsConfigDoc Data { get; } = ConfigLoader.Load("genetics.json", Fallback);

    private static readonly GeneticsConfigDoc Fallback = new GeneticsConfigDoc(
        Version: 1,
        Traits: new TraitConfigDoc(
            Speed: new TraitBounds { Min = 0.5f, Max = 2f, Step = 0.3f },
            Size: new TraitBounds { Min = 0.5f, Max = 2f, Step = 0.3f },
            Metabolism: new TraitBounds { Min = 0.5f, Max = 2f, Step = 0.3f },
            VisionRange: new TraitBounds { Min = 1f, Max = 10f, Step = 1f },
            MutationRate: new TraitBounds { Min = 0.01f, Max = 0.2f, Step = 0.03f, Default = 0.05f },
            Adaptation: new TraitBounds { Min = 0f, Max = 1f, Step = 0.2f },
            Color: new TraitBounds { Min = 40f, Max = 255f, Step = 30f }
        ),
        Alleles: new AlleleConfigDoc(
            MutationChanceMultiplier: 0.25f,
            DominanceMutationStep: 0.2f,
            DominanceMin: 0.05f,
            DominanceMax: 1f,
            RandomDominanceMin: 0.2f,
            RandomDominanceSpread: 0.8f
        )
    );

    public sealed record GeneticsConfigDoc(
        int Version,
        TraitConfigDoc Traits,
        AlleleConfigDoc Alleles
    );

    public sealed record TraitConfigDoc(
        TraitBounds Speed,
        TraitBounds Size,
        TraitBounds Metabolism,
        TraitBounds VisionRange,
        TraitBounds MutationRate,
        TraitBounds Adaptation,
        TraitBounds Color
    );

    public sealed record TraitBounds
    {
        public float Min { get; init; }
        public float Max { get; init; }
        public float Step { get; init; }
        public float Default { get; init; }
    }

    public sealed record AlleleConfigDoc(
        float MutationChanceMultiplier,
        float DominanceMutationStep,
        float DominanceMin,
        float DominanceMax,
        float RandomDominanceMin,
        float RandomDominanceSpread
    );
}
