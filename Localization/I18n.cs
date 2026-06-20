using System;
using System.Collections.Generic;
using System.Globalization;
using PitLife.Simulation;

namespace PitLife.Localization;

public static class I18n
{
    private static readonly Dictionary<string, string> English = CreateEnglishCatalog();
    private static readonly Dictionary<string, string> Italian = CreateItalianCatalog();
    private static IReadOnlyDictionary<string, string> _current = Italian;

    public static string CurrentLanguage { get; private set; } = "it";
    public static IReadOnlyCollection<string> SupportedLanguages { get; } = ["it", "en"];

    public static void SetLanguage(string language)
    {
        string normalized = language.Trim().ToLowerInvariant();
        if (normalized.StartsWith("it"))
        {
            CurrentLanguage = "it";
            _current = Italian;
            return;
        }

        if (normalized.StartsWith("en"))
        {
            CurrentLanguage = "en";
            _current = English;
            return;
        }

        throw new ArgumentException($"Unsupported language: {language}", nameof(language));
    }

    public static string T(string key) =>
        _current.TryGetValue(key, out string? value) ? value :
        Italian.TryGetValue(key, out value) ? value : key;

    public static string Format(string key, params object[] args) =>
        string.Format(CultureInfo.GetCultureInfo(CurrentLanguage), T(key), args);

    public static string Species(string species)
    {
        string key = $"species.{species}";
        string value = T(key);
        return value == key ? species : value;
    }

    public static string CreatureTypeName(CreatureType type) => T($"creatureType.{type}");

    public static IReadOnlyCollection<string> Keys(string language) =>
        language.StartsWith("it", StringComparison.OrdinalIgnoreCase) ? Italian.Keys : English.Keys;

    private static Dictionary<string, string> CreateEnglishCatalog() => new(StringComparer.Ordinal)
    {
        ["common.yes"] = "YES",
        ["common.no"] = "NO",
        ["common.back"] = "BACK",
        ["menu.start"] = "START",
        ["menu.newWorld"] = "NEW WORLD",
        ["menu.options"] = "OPTIONS",
        ["menu.exit"] = "EXIT",
        ["menu.fullscreen"] = "FULLSCREEN: {0}",
        ["menu.mainTitle"] = "MAIN MENU",
        ["menu.optionsTitle"] = "OPTIONS",
        ["menu.hint"] = "ARROWS: navigate   ENTER: select",
        ["menu.optionsHint"] = "ENTER: select   ESC: back",
        ["toolbar.statistics"] = "STATISTICS",
        ["toolbar.creature"] = "CREATURE",
        ["window.statistics"] = "STATISTICS",
        ["window.creature"] = "CREATURE DETAILS",
        ["stats.time"] = "Simulation time: {0:F1}s",
        ["stats.paused"] = "Status: PAUSED",
        ["stats.speed"] = "Status: {0:0.#}x",
        ["stats.total"] = "Total population: {0}",
        ["stats.plants"] = "Plants: {0}",
        ["stats.herbivores"] = "Herbivores: {0}",
        ["stats.carnivores"] = "Carnivores: {0}",
        ["stats.omnivores"] = "Omnivores: {0}",
        ["creature.none"] = "No creature selected.",
        ["creature.selectHint"] = "Click a creature in the world.",
        ["creature.heading"] = "{0} - {1}",
        ["creature.energy"] = "Energy: {0:F1} / {1:F1}",
        ["creature.age"] = "Age: {0:F1}s",
        ["creature.speed"] = "Speed: {0:F2}",
        ["creature.size"] = "Size: {0:F2}",
        ["creature.metabolism"] = "Metabolism: {0:F2}",
        ["creature.vision"] = "Vision: {0:F1}",
        ["creature.genome"] = "Genome: #{0:X2}{1:X2}{2:X2}",
        ["hud.paused"] = "PAUSED",
        ["hud.summary"] = "Time: {0:F1}s | Plants: {1}  Herbivores: {2}  Carnivores: {3}  Omnivores: {4} | Speed: {5}",
        ["hud.controls"] = "WASD:move  Scroll:zoom  1(1x) 2(2x) 3(4x) Space:pause  F2/F3:windows  ESC:menu",
        ["creatureType.Plant"] = "Plant",
        ["creatureType.Herbivore"] = "Herbivore",
        ["creatureType.Carnivore"] = "Carnivore",
        ["creatureType.Omnivore"] = "Omnivore",
        ["species.Plant"] = "Plant",
        ["species.Flowers"] = "Flowers",
        ["species.Mushroom"] = "Mushroom",
        ["species.GrassTuft"] = "Grass Tuft",
        ["species.Cactus"] = "Cactus",
        ["species.Moss"] = "Moss",
        ["species.BerryBush"] = "Berry Bush",
        ["species.Pine"] = "Pine",
        ["species.Toadstool"] = "Toadstool",
        ["species.Rabbit"] = "Rabbit",
        ["species.Deer"] = "Deer",
        ["species.Sheep"] = "Sheep",
        ["species.Horse"] = "Horse",
        ["species.Goat"] = "Goat",
        ["species.Fish"] = "Fish",
        ["species.Lizard"] = "Lizard",
        ["species.Turtle"] = "Turtle",
        ["species.Salmon"] = "Salmon",
        ["species.Fox"] = "Fox",
        ["species.Lynx"] = "Lynx",
        ["species.Tiger"] = "Tiger",
        ["species.Lion"] = "Lion",
        ["species.Leopard"] = "Leopard",
        ["species.Crocodile"] = "Crocodile",
        ["species.Snake"] = "Snake",
        ["species.Eagle"] = "Eagle",
        ["species.Wolf"] = "Wolf",
        ["species.Shark"] = "Shark",
        ["species.Piranha"] = "Piranha",
        ["species.Boar"] = "Boar",
        ["species.Raccoon"] = "Raccoon",
        ["species.Frog"] = "Frog",
        ["species.Beetle"] = "Beetle",
        ["species.Butterfly"] = "Butterfly",
        ["species.Bear"] = "Bear",
        ["species.Jellyfish"] = "Jellyfish",
        ["species.Gazelle"] = "Gazelle",
        ["species.Seaweed"] = "Seaweed",
        ["species.Algae"] = "Algae",
        ["species.Kelp"] = "Kelp",
        ["species.WaterLily"] = "Water Lily",
        ["species.Coral"] = "Coral",
        ["ui.gender.male"] = "Male",
        ["ui.gender.female"] = "Female",
        ["ui.age"] = "Age",
        ["ui.age.seconds"] = "{0:F1}s",
        ["ui.status.adult"] = "Adult",
        ["ui.status.baby"] = "Baby",
        ["dayphase.dawn"] = "Dawn",
        ["dayphase.day"] = "Day",
        ["dayphase.dusk"] = "Dusk",
        ["dayphase.night"] = "Night",
        ["minimap.title"] = "Map",
        ["minimap.plants"] = "Plants",
        ["minimap.herbivores"] = "Herbivores",
        ["minimap.carnivores"] = "Carnivores",
        ["minimap.omnivores"] = "Omnivores",
        ["spawn.title"] = "Spawn",
        ["spawn.plants"] = "Plants",
        ["spawn.aquaticplants"] = "Aquatic Plants",
        ["spawn.herbivores"] = "Herbivores",
        ["spawn.carnivores"] = "Carnivores",
        ["spawn.omnivores"] = "Omnivores",
        ["spawn.hint"] = "Click map to spawn",
        ["spawn.none"] = "Select species",
        ["spawn.selected"] = "Selected",
        ["menu.seedPlaceholder"] = "Enter seed (optional)..."
    };

    private static Dictionary<string, string> CreateItalianCatalog()
    {
        var catalog = new Dictionary<string, string>(English, StringComparer.Ordinal)
        {
            ["common.yes"] = "SÌ",
            ["common.no"] = "NO",
            ["common.back"] = "INDIETRO",
            ["menu.start"] = "INIZIA",
            ["menu.options"] = "OPZIONI",
            ["menu.exit"] = "ESCI",
            ["menu.fullscreen"] = "SCHERMO INTERO: {0}",
            ["menu.mainTitle"] = "MENU PRINCIPALE",
            ["menu.optionsTitle"] = "OPZIONI",
            ["menu.hint"] = "FRECCE: naviga   INVIO: seleziona",
            ["menu.optionsHint"] = "INVIO: seleziona   ESC: indietro",
            ["toolbar.statistics"] = "STATISTICHE",
            ["toolbar.creature"] = "CREATURA",
            ["window.statistics"] = "STATISTICHE",
            ["window.creature"] = "DETTAGLI CREATURA",
            ["stats.time"] = "Tempo simulato: {0:F1}s",
            ["stats.paused"] = "Stato: PAUSA",
            ["stats.speed"] = "Stato: {0:0.#}x",
            ["stats.total"] = "Popolazione totale: {0}",
            ["stats.plants"] = "Piante: {0}",
            ["stats.herbivores"] = "Erbivori: {0}",
            ["stats.carnivores"] = "Carnivori: {0}",
            ["stats.omnivores"] = "Onnivori: {0}",
            ["creature.none"] = "Nessuna creatura selezionata.",
            ["creature.selectHint"] = "Clicca una creatura nel mondo.",
            ["creature.heading"] = "{0} - {1}",
            ["creature.energy"] = "Energia: {0:F1} / {1:F1}",
            ["creature.age"] = "Età: {0:F1}s",
            ["creature.speed"] = "Velocità: {0:F2}",
            ["creature.size"] = "Dimensione: {0:F2}",
            ["creature.metabolism"] = "Metabolismo: {0:F2}",
            ["creature.vision"] = "Visione: {0:F1}",
            ["creature.genome"] = "Genoma: #{0:X2}{1:X2}{2:X2}",
            ["hud.paused"] = "PAUSA",
            ["hud.summary"] = "Tempo: {0:F1}s | Piante: {1}  Erbivori: {2}  Carnivori: {3}  Onnivori: {4} | Velocità: {5}",
            ["hud.controls"] = "WASD:muovi  Rotella:zoom  1(1x) 2(2x) 3(4x) Spazio:pausa  F2/F3:finestre  ESC:menu",
            ["creatureType.Plant"] = "Pianta",
            ["creatureType.Herbivore"] = "Erbivoro",
            ["creatureType.Carnivore"] = "Carnivoro",
            ["creatureType.Omnivore"] = "Onnivoro",
            ["species.Plant"] = "Pianta",
            ["species.Flowers"] = "Fiori",
            ["species.Mushroom"] = "Fungo",
            ["species.GrassTuft"] = "Ciuffo d'erba",
            ["species.Moss"] = "Muschio",
            ["species.BerryBush"] = "Cespuglio di bacche",
            ["species.Pine"] = "Pino",
            ["species.Toadstool"] = "Amanita",
            ["species.Rabbit"] = "Coniglio",
            ["species.Deer"] = "Cervo",
            ["species.Sheep"] = "Pecora",
            ["species.Horse"] = "Cavallo",
            ["species.Goat"] = "Capra",
            ["species.Fish"] = "Pesce",
            ["species.Lizard"] = "Lucertola",
            ["species.Turtle"] = "Tartaruga",
            ["species.Salmon"] = "Salmone",
            ["species.Fox"] = "Volpe",
            ["species.Lynx"] = "Lince",
            ["species.Tiger"] = "Tigre",
            ["species.Lion"] = "Leone",
            ["species.Leopard"] = "Leopardo",
            ["species.Crocodile"] = "Coccodrillo",
            ["species.Snake"] = "Serpente",
            ["species.Eagle"] = "Aquila",
            ["species.Wolf"] = "Lupo",
            ["species.Shark"] = "Squalo",
            ["species.Boar"] = "Cinghiale",
            ["species.Raccoon"] = "Procione",
            ["species.Frog"] = "Rana",
            ["species.Beetle"] = "Coleottero",
            ["species.Butterfly"] = "Farfalla",
            ["species.Bear"] = "Orso",
            ["species.Jellyfish"] = "Medusa",
        ["species.Gazelle"] = "Gazzella",
        ["species.Seaweed"] = "Alga",
        ["species.Algae"] = "Alghe",
        ["species.Kelp"] = "Kelp",
        ["species.WaterLily"] = "Ninfea",
        ["species.Coral"] = "Corallo",
        ["ui.gender.male"] = "Maschio",
            ["ui.gender.female"] = "Femmina",
            ["ui.age"] = "Età",
            ["ui.age.seconds"] = "{0:F1}s",
            ["ui.status.adult"] = "Adulto",
            ["ui.status.baby"] = "Cucciolo",
            ["dayphase.dawn"] = "Alba",
            ["dayphase.day"] = "Giorno",
            ["dayphase.dusk"] = "Tramonto",
            ["dayphase.night"] = "Notte",
            ["minimap.title"] = "Mappa",
            ["minimap.plants"] = "Piante",
            ["minimap.herbivores"] = "Erbivori",
            ["minimap.carnivores"] = "Carnivori",
            ["minimap.omnivores"] = "Onnivori",
            ["spawn.title"] = "Spawn",
        ["spawn.plants"] = "Piante",
        ["spawn.aquaticplants"] = "Piante Acquatiche",
        ["spawn.herbivores"] = "Erbivori",
            ["spawn.carnivores"] = "Carnivori",
            ["spawn.omnivores"] = "Onnivori",
            ["spawn.hint"] = "Clicca sulla mappa per spawnare",
            ["spawn.none"] = "Seleziona specie",
            ["spawn.selected"] = "Selezionato",
            ["menu.seedPlaceholder"] = "Inserisci seed (opzionale)..."
        };

        return catalog;
    }
}
