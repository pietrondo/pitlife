using System.Collections.Generic;

namespace PitLife.Core;

public static class DiseaseConfig
{
    private static readonly DiseaseConfigDoc Fallback = new(
        Version: 1,
        Diseases: new List<DiseaseDefEntry>
        {
            new() { Name = "Fever", TransmissionRate = 0.15f, Lethality = 0.1f, RecoveryTime = 30f, EnergyDrain = 2f },
            new() { Name = "Plague", TransmissionRate = 0.3f, Lethality = 0.3f, RecoveryTime = 45f, EnergyDrain = 4f },
            new() { Name = "Parasite", TransmissionRate = 0.1f, Lethality = 0.05f, RecoveryTime = 60f, EnergyDrain = 1f }
        },
        Outbreak: new OutbreakDefaults()
    );

    public static DiseaseConfigDoc Data { get; } = ConfigLoader.Load("diseases.json", Fallback);

    public static IReadOnlyList<DiseaseDefEntry> Diseases => Data.Diseases ?? Fallback.Diseases!;
    public static OutbreakDefaults Outbreak => Data.Outbreak ?? Fallback.Outbreak!;

    public sealed record DiseaseConfigDoc(int Version, List<DiseaseDefEntry>? Diseases, OutbreakDefaults? Outbreak);

    public sealed record DiseaseDefEntry
    {
        public string Name { get; init; } = "";
        public float TransmissionRate { get; init; }
        public float Lethality { get; init; }
        public float RecoveryTime { get; init; }
        public float EnergyDrain { get; init; }
    }

    public sealed record OutbreakDefaults
    {
        public int MinCreatures { get; init; } = 10;
        public float InitialTimerSeconds { get; init; } = 60f;
    }
}
