using System;
using System.IO;
using System.Text.Json;

namespace PitLife.Core;

public static class EvolutionConfig
{
    public static EvolutionConfigData Data { get; private set; } = new();

    static EvolutionConfig()
    {
        try
        {
            string path = Path.Combine("Content", "config", "evolution.json");
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                var parsed = JsonSerializer.Deserialize<EvolutionConfigData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    AllowTrailingCommas = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
                if (parsed != null) Data = parsed;
            }
        }
        catch { /* use defaults */ }
    }
}

public class EvolutionConfigData
{
    public int Version { get; set; } = 1;
    public HerbivoreEvolutionConfig Herbivore { get; set; } = new();
    public CarnivoreEvolutionConfig Carnivore { get; set; } = new();
    public OmnivoreEvolutionConfig Omnivore { get; set; } = new();
}

public class HerbivoreEvolutionConfig
{
    public float WaterAdaptation { get; set; } = 0.65f;
    public float DolphinSpeed { get; set; } = 1.2f;
    public float WhaleSize { get; set; } = 1.2f;
    public float KangarooDesertAdaptation { get; set; } = 0.45f;
    public float KangarooSpeed { get; set; } = 1.2f;
    public float KangarooSize { get; set; } = 1.0f;
    public float LizardDesertAdaptation { get; set; } = 0.65f;
    public float LizardSize { get; set; } = 0.8f;
    public float GazelleDesertAdaptation { get; set; } = 0.55f;
    public float GazelleSpeed { get; set; } = 1.1f;
    public float RabbitSize { get; set; } = 0.75f;
    public float RabbitSpeed { get; set; } = 1.1f;
    public float GoatColdAdaptation { get; set; } = 0.55f;
    public float GoatSize { get; set; } = 1.1f;
    public float HorseSize { get; set; } = 1.25f;
    public float HorseSpeed { get; set; } = 1.1f;
    public float DeerSize { get; set; } = 1.0f;
    public float DeerForestAdaptation { get; set; } = 0.5f;
    public float SheepSizeMin { get; set; } = 0.8f;
    public float SheepSizeMax { get; set; } = 1.2f;
    public float SheepSpeed { get; set; } = 0.9f;
}

public class CarnivoreEvolutionConfig
{
    public float WaterAdaptation { get; set; } = 0.65f;
    public float OrcaSize { get; set; } = 1.2f;
    public float SealColdAdaptation { get; set; } = 0.5f;
    public float CheetahSpeed { get; set; } = 1.4f;
    public float CheetahDesertAdaptation { get; set; } = 0.4f;
    public float CheetahForestAdaptation { get; set; } = 0.4f;
    public float CrocodileWaterAdaptation { get; set; } = 0.45f;
    public float CrocodileDesertAdaptation { get; set; } = 0.45f;
    public float LionDesertAdaptation { get; set; } = 0.45f;
    public float LionSpeed { get; set; } = 1.1f;
    public float WolfColdAdaptation { get; set; } = 0.45f;
    public float WolfSize { get; set; } = 0.9f;
    public float LynxColdAdaptation { get; set; } = 0.55f;
    public float LynxSize { get; set; } = 1.0f;
    public float TigerForestAdaptation { get; set; } = 0.65f;
    public float TigerSize { get; set; } = 1.2f;
    public float LeopardForestAdaptation { get; set; } = 0.5f;
    public float LeopardSize { get; set; } = 1.2f;
    public float FoxSize { get; set; } = 0.8f;
}

public class OmnivoreEvolutionConfig
{
    public float WaterAdaptation { get; set; } = 0.65f;
    public float HippopotamusSize { get; set; } = 1.3f;
    public float WalrusColdAdaptation { get; set; } = 0.5f;
    public float FrogWaterAdaptation { get; set; } = 0.4f;
    public float FrogForestAdaptation { get; set; } = 0.4f;
    public float BearSize { get; set; } = 1.3f;
    public float BoarSizeMin { get; set; } = 0.9f;
    public float BoarSizeMax { get; set; } = 1.3f;
    public float RaccoonSize { get; set; } = 0.9f;
}
