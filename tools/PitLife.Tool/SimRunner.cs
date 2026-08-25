using System;
using System.Buffers.Binary;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using PitLife.Rendering;
using PitLife.Simulation;

namespace PitLife.Tool;

internal static class SimRunner
{
    private sealed class Options
    {
        public int Ticks = 1000;
        public int Seed = 42;
        public int Width;
        public int Height;
        public int Herbivores;
        public int Carnivores;
        public int Omnivores;
        public int Plants;
        public int Interval = 100;

        // I default riflettono i valori reali del gioco (simulation.json).
        public Options()
        {
            var cfg = PitLife.Core.SimulationConfig.Data;
            Width = cfg.MapWidth;
            Height = cfg.MapHeight;
            Herbivores = cfg.InitialHerbivores;
            Carnivores = cfg.InitialCarnivores;
            Omnivores = cfg.InitialOmnivores;
            Plants = cfg.InitialPlants;
        }
    }

    public static int Run(string[] args)
    {
        var o = Parse(args);

        var eco = new Ecosystem(o.Width, o.Height, o.Seed) { MaxCreatures = 1000 };
        eco.Initialize(o.Herbivores, o.Carnivores, o.Omnivores, o.Plants);

        Console.WriteLine($"Simulazione: {o.Width}x{o.Height}, seed={o.Seed}, tick={o.Ticks}");
        Console.WriteLine($"Iniziali: H={o.Herbivores} C={o.Carnivores} O={o.Omnivores} P={o.Plants}\n");
        Console.WriteLine("tick   plants  herb   carn   omni   total");

        var dt = TimeSpan.FromSeconds(0.1f);
        for (int t = 0; t <= o.Ticks; t++)
        {
            if (t % o.Interval == 0)
            {
                eco.UpdateStats();
                Console.WriteLine(
                    $"{t,5}  {eco.PlantCount,6}  {eco.HerbivoreCount,5}  {eco.CarnivoreCount,4}  " +
                    $"{eco.OmnivoreCount,4}  {eco.Creatures.Count,6}");
            }
            if (t < o.Ticks)
                eco.Tick(new GameTime(dt, dt));
        }

        var m = eco.Metrics;
        Console.WriteLine($"\nDecessi: totale={m.TotalDeaths} fame={m.StarvationDeaths} " +
            $"predazione={m.PredationDeaths} vecchiaia={m.OldAgeDeaths} combattimento={m.CombatDeaths}");
        Console.WriteLine($"Nascite: {m.TotalBirths} · Specie presenti: {m.SpeciesCount}");
        ReportPixelAssets(m.SpeciesPopulations.Keys);
        return 0;
    }

    // Carica e decodifica (senza GPU) la texture pixel di ciascuna specie presente
    // nell'ecosistema, provando che gli animali simulati hanno uno sprite reale.
    private static void ReportPixelAssets(IEnumerable<string> species)
    {
        var bySpecies = AssetRegistry.SpeciesTextures.ToDictionary(a => a.Species, a => a.Path, StringComparer.Ordinal);
        var fallback = AssetRegistry.Fallbacks.FirstOrDefault()?.Path;
        var root = FindRoot();
        int loaded = 0, missing = 0;

        foreach (var name in species.OrderBy(x => x))
        {
            var rel = bySpecies.TryGetValue(name, out var p) ? p : fallback;
            if (rel == null || !File.Exists(Path.Combine(root, rel)))
            {
                Console.WriteLine($"  [PIXEL] {name,-18} -> (nessuna texture)");
                missing++;
                continue;
            }
            var path = Path.Combine(root, rel!);
            var (w, h, visible) = DecodeRgbaPng(path);
            Console.WriteLine($"  [PIXEL] {name,-18} -> {rel} ({w}x{h}, {visible}px visibili)");
            loaded++;
        }

        Console.WriteLine($"\nTexture pixel: {loaded} caricate e decodificate, {missing} mancanti su {species.Count()} specie presenti.");
    }

    private static string FindRoot()
    {
        var dir = new DirectoryInfo(Directory.GetCurrentDirectory());
        while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "Content")))
            dir = dir.Parent;
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    private static (int Width, int Height, int Visible) DecodeRgbaPng(string path)
    {
        var png = File.ReadAllBytes(path);
        int width = 0, height = 0;
        using var idat = new MemoryStream();
        var offset = 8;
        while (offset < png.Length)
        {
            var length = BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(offset, 4));
            var type = Encoding.ASCII.GetString(png, offset + 4, 4);
            var data = png.AsSpan(offset + 8, length);
            if (type == "IHDR")
            {
                width = BinaryPrimitives.ReadInt32BigEndian(data[..4]);
                height = BinaryPrimitives.ReadInt32BigEndian(data.Slice(4, 4));
            }
            else if (type == "IDAT")
            {
                idat.Write(data);
            }
            offset += length + 12;
        }

        idat.Position = 0;
        using var decompressed = new MemoryStream();
        using (var zlib = new ZLibStream(idat, CompressionMode.Decompress, leaveOpen: true))
            zlib.CopyTo(decompressed);
        var filtered = decompressed.ToArray();
        var stride = width * 4;
        var pixels = new byte[stride * height];
        for (var y = 0; y < height; y++)
        {
            var src = y * (stride + 1);
            var dst = y * stride;
            var filter = filtered[src];
            for (var x = 0; x < stride; x++)
            {
                var raw = filtered[src + 1 + x];
                var left = x >= 4 ? pixels[dst + x - 4] : (byte)0;
                var up = y > 0 ? pixels[dst - stride + x] : (byte)0;
                var upLeft = y > 0 && x >= 4 ? pixels[dst - stride + x - 4] : (byte)0;
                pixels[dst + x] = filter switch
                {
                    0 => raw,
                    1 => unchecked((byte)(raw + left)),
                    2 => unchecked((byte)(raw + up)),
                    3 => unchecked((byte)(raw + ((left + up) >> 1))),
                    4 => unchecked((byte)(raw + Paeth(left, up, upLeft))),
                    _ => raw
                };
            }
        }

        var visible = 0;
        for (var i = 0; i < pixels.Length; i += 4)
            if (pixels[i + 3] != 0)
                visible++;
        return (width, height, visible);
    }

    private static Options Parse(string[] args)
    {
        var o = new Options();
        for (int i = 0; i < args.Length; i++)
        {
            string? Value() => i + 1 < args.Length ? args[++i] : null;
            switch (args[i])
            {
                case "--ticks": o.Ticks = int.Parse(Value()!); break;
                case "--seed": o.Seed = int.Parse(Value()!); break;
                case "--width": o.Width = int.Parse(Value()!); break;
                case "--height": o.Height = int.Parse(Value()!); break;
                case "--herbivores": o.Herbivores = int.Parse(Value()!); break;
                case "--carnivores": o.Carnivores = int.Parse(Value()!); break;
                case "--omnivores": o.Omnivores = int.Parse(Value()!); break;
                case "--plants": o.Plants = int.Parse(Value()!); break;
                case "--interval": o.Interval = int.Parse(Value()!); break;
                default:
                    throw new ArgumentException($"Opzione sconosciuta: {args[i]}");
            }
        }
        return o;
    }

    private static byte Paeth(byte a, byte b, byte c)
    {
        int p = a + b - c;
        int pa = Math.Abs(p - a);
        int pb = Math.Abs(p - b);
        int pc = Math.Abs(p - c);
        return pa <= pb && pa <= pc ? a : pb <= pc ? b : c;
    }
}
