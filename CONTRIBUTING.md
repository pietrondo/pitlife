# Contributing to PitLife

Grazie per voler contribuire! PitLife è un simulatore di ecosistema 2D data-driven scritto in C# con MonoGame.

## Setup ambiente

```bash
git clone https://github.com/pietrondo/pitlife.git
cd PitLife
dotnet build
dotnet run
```

### Requisiti
- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- Scheda video con supporto OpenGL

## Build & Test

```bash
dotnet build                    # Compila il progetto
dotnet test                     # Esegue 310+ test
dotnet run                      # Avvia il gioco
```

## Struttura del progetto

| Directory | Contenuto |
|-----------|-----------|
| `Simulation/` | Ecosystem, Creature, Genome, Behaviors, WorldGenerator, Sistemi (clima, malattie, etc.) |
| `Rendering/` | PixelWorldRenderer, CreatureRenderer, Minimap, DayNightCycle, Camera |
| `UI/` | MainMenu, SpawnPanel, InGameUi, CataclysmPanel, SpeciesEditor |
| `Core/` | Logger, AssetRegistry, SpeciesCatalog, Config loaders |
| `Localization/` | I18n EN/IT |
| `Content/` | Assets, sprites, font |
| `Content/config/` | **File JSON data-driven** per specie, bilanciamento, clima, etc. |
| `tests/` | Test xUnit (310+) |

## Data-driven design (REGOLA #1)

**Non mettere valori hardcoded in C#.** Tutti i parametri di gameplay (specie, bilanciamento, clima, malattie) vanno in `Content/config/*.json`.

I file config sono:
- `species.json` — definizione di tutte le specie
- `balance.json` — costanti di energia, fame, combattimento
- `diseases.json` — preset malattie
- `climate.json` — parametri orbitali e stagionali
- `atmosphere.json` — costanti O₂/CO₂
- `behaviors.json` — parametri comportamentali (flocking, feeding, social)

Ogni file JSON ha un corrispondente loader in `Core/` (es. `BalanceConfig.cs`) con fallback ai valori di default se il file non esiste.

### Aggiungere una nuova specie

Modifica `Content/config/species.json`:

```json
{ "name": "MySpecies", "kind": "Herbivore", "isAquatic": false, "socialBehavior": "Herd", "size": 0.8 }
```

- **Piante**: aggiungi `"kind": "Plant"`, `"reproduction": "Seeds"`, `"minTemperature"`, `"maxTemperature"`
- **Animali**: aggiungi `"kind": "Herbivore|Carnivore|Omnivore"`, `"socialBehavior": "Solitary|Herd|Pack|School|Swarm|Pair"`

### Modificare il bilanciamento

Modifica `Content/config/balance.json` — tutti i valori sono autodocumentati. Non serve ricompilare.

## Convenzioni codice

- **Indentazione**: 4 spazi
- **Brace style**: Allman (parentesi su nuova linea)
- **Naming**: `_camelCase` per campi privati, `PascalCase` per tutto il resto
- **File-scoped namespaces**: `namespace PitLife.Simulation;`
- **Usings**: fuori dal namespace, ordinati (System → MonoGame → PitLife)
- **Linguaggio**: C# 14, .NET 10, Nullable abilitato
- **Design system**: vedi `DESIGN.md` per palette colori, tipografia, layout

### .editorconfig

Il file `.editorconfig` nella root applica automaticamente queste convenzioni in VS Code / Visual Studio / Rider.

## Flusso PR

1. Fai il fork del repo
2. Crea un branch: `git checkout -b feature/nome-feature`
3. Fai le modifiche e testa: `dotnet test`
4. Committa con [Conventional Commits](https://www.conventionalcommits.org/): `feat:`, `fix:`, `chore:`, `refactor:`
5. Pusha e apri una Pull Request

## Licenza

MIT — vedi [LICENSE](LICENSE)
