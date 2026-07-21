using System;
using System.Collections.Generic;

namespace PitLife.Core;

public static class MaturationConfig
{
    public static MaturationConfigData Data { get; private set; } = 
        ConfigLoader.Load<MaturationConfigData>("maturation.json");
}

public class MaturationConfigData
{
    public int Version { get; set; } = 1;
    public float DefaultAge { get; set; } = 30f;
    public Dictionary<string, float> Ages { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
