using System;

namespace PitLife.Tool;

internal static class Program
{
    private static int Main(string[] args)
    {
        try
        {
            ToolRoot.EnsureWorkingDirectory();

            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            return args[0] switch
            {
                "config-doctor" => ConfigDoctor.Run(),
                "sim" => SimRunner.Run(args[1..]),
                "help" or "--help" or "-h" => PrintUsage(),
                _ => Unknown(args[0])
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ERRORE: {ex.Message}");
            return 1;
        }
    }

    private static int Unknown(string cmd)
    {
        Console.Error.WriteLine($"Comando sconosciuto: '{cmd}'");
        PrintUsage();
        return 1;
    }

    private static int PrintUsage()
    {
        Console.WriteLine("""
            PitLife.Tool — tool locale di debug

            Uso:
              PitLife.Tool config-doctor
                  Valida i JSON config, rileva specie duplicate (C#/JSON) e drift default-vs-JSON.

              PitLife.Tool sim [opzioni]
                  Runner headless della simulazione.
                  --ticks N        numero di tick (default 1000)
                  --seed S         seed (default 42)
                  --width W        larghezza mappa (default 64)
                  --height H       altezza mappa (default 48)
                  --herbivores H   erbivori iniziali (default 30)
                  --carnivores C   carnivori iniziali (default 10)
                  --omnivores O    onnivori iniziali (default 8)
                  --plants P       piante iniziali (default 80)
                  --interval R     ogni quanti tick stampare (default 100)
            """);
        return 0;
    }
}
