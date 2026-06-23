# PitLife

Simulatore di ecosistema 2D data-driven con creature che vivono, si nutrono, si riproducono, evolvono e interagiscono in un mondo dinamico con 15 biomi, 86+ specie, stagioni, clima e cataclismi.

## Come Giocare

PitLife è una simulazione sandbox: osserva l'ecosistema evolvere, spawna creature, scatena cataclismi e bilancia la vita.

### Toolbar (in basso)

| Bottone | Azione |
|---------|--------|
| **Statistiche** | Popolazioni, specie, gas |
| **Creatura** | Dettagli creatura selezionata |
| **Allinea** | Riordina finestre |
| **< >** | Velocità simulazione (0/1x/2x/4x) |
| **Cataclismi** | Pannello cataclismi |
| **Menu** | Torna al menu principale |

### Controlli

| Tasto | Azione |
|-------|--------|
| **WASD / Frecce** | Muovi camera |
| **Rotella mouse** | Zoom |
| **Click su creatura** | Seleziona e vedi dettagli |
| **Click su terreno** | Vedi info bioma |
| **Spazio** | Pausa/Riprendi |
| **↑ / ↓** | Aumenta/Riduci velocità |
| **1 / 2 / 3** | Velocità 1x / 2x / 4x |
| **F1** | Mostra/Nascondi debug overlay |
| **F2** | Finestra Statistiche |
| **F3** | Finestra Dettagli Creatura |
| **F4** | Pannello Spawn creature |
| **F6** | Editor Specie |
| **F7** | Cataclisma casuale globale |
| **F8** | Pannello Cataclismi (sidebar) |
| **ESC** | Annulla selezione / Menu principale |

### Spawn Creature
1. Premi **F4** per aprire il pannello spawn
2. Scegli la categoria (Piante, Erbivori, Carnivori, Onnivori)
3. Clicca una specie (usa la barra di ricerca per filtrare)
4. Clicca sulla mappa per spawnare (3 individui in gruppo)
5. Icone: **~** = acquatico, **^** = volatile

### Cataclismi (Piazza sulla mappa)
1. Apri il pannello con **F8** (sidebar sinistra) o **Cataclismi** nella toolbar
2. Seleziona il tipo: Asteroide, Era Glaciale, Supervulcano, Terremoto, Siccità, Inondazione
3. Clicca sulla mappa per piazzarlo
4. Il terreno cambia visibilmente (centro + anello)
5. **F7** per cataclisma casuale globale

**Animazioni:**
- **Asteroide**: meteora che cade dal cielo, scia di fumo, esplosione con anelli d'urto
- **Supervulcano**: colonna di magma che sale, lava a grappolo, pozza di lava alla base
- **Era Glaciale**: anelli di gelo concentrici, fiocchi di neve
- **Terremoto**: scuotimento schermo, linee di crepa
- **Siccità / Inondazione / Firestorm**: effetti visivi specifici

### World Generation (Menu principale)
- **Preset**: Pangea, Continenti, Arcipelago, WetWorld, DryWorld
- **Continenti**: 1-6 masse continentali distinte con centri reali
- **Livello mare**: da 0 a 100, influenza rapporto terra/acqua
- **Dimensione isole**: Piccole, Medie, Grandi
- **Mappa**: 96×72, 200×150, 400×300, 800×600

## Caratteristiche

### Simulazione
- **15 biomi**: DeepOcean, ShallowWater, Beach, Desert, Savanna, Grassland, Forest, DenseForest, Swamp, Tundra, Mountain, Snow, CoralReef, Cave, Volcano
- **86+ specie** tra piante, erbivori, carnivori, onnivori, insetti e preistorici
- **Ciclo giorno/notte** con 4 fasi e overlay visivo
- **4 stagioni** (480s = 1 anno) con effetti su crescita e metabolismo
- **Clima**: temperatura per-tile, stress termico, eventi estremi
- **Cataclismi**: modificano visibilmente il terreno (biomi, erba, nutrienti)

### Genetica ed Evoluzione
- **Genoma diploide**: 11 loci con alleli, dominanza e ricombinazione
- **Tratti ereditabili**: Speed, Size, Metabolism, Vision, adattamenti climatici
- **Personalità**: Aggression, Sociability, Intelligence, MemorySpan, PlantRecognition
- **Deriva genetica**: fluttuazioni casuali in popolazioni piccole
- **Inbreeding**: coefficiente di parentela e depressione genetica
- **Evoluzione visibile**: colore genome riflesso nello sprite

### Ecologia
- **Rete trofica**: 5 livelli trofici, efficienza energetica 10%
- **Grazing**: erba sui tile, rigenerazione stagionale
- **Nutrienti suolo**: ciclo NPK, decomposizione carogne fertilizza
- **Atmosfera**: O₂/CO₂ globali, fotosintesi e respirazione
- **Sete**: animali bevono da fiumi e oceani
- **Tossicità**: piante e animali velenosi, apprendimento erbivori
- **Malattie**: epidemie con trasmissione, immunità e recupero
- **Simbiosi**: mutualismo implicito (api-fiori, pesci pulitori)
- **Migrazioni**: home range, movimento stagionale

### Comportamento
- **Ciclo attività**: animali diurni/notturni/crepuscolari
- **Memoria spaziale**: ricordo di cibo e pericoli
- **Cuccioli**: infanti protetti dai genitori
- **Corteggiamento**: combattimento tra maschi, selezione sessuale
- **Territorialità**: difesa del branco, home range
- **Socialità**: branchi, stormi, banchi con flocking
- **Difese**: statistiche di attacco/difesa basate sul genoma

### UI
- **Tema foresta**: palette verde/marrone con finestre draggable
- **Toolbar in basso**: statistiche, creature, velocità, cataclismi, menu
- **Animazioni cataclismi**: meteora, magma, gelo, crepe, onde
- **Minimap**: angolo in basso a destra con biomes e creature
- **I18n**: Italiano/Inglese con toggle nel menu
- **Persistenza**: salvataggio/caricamento mondo, preferenze lingua
- **Log rotation**: max 5 file di log

## Requisiti

- Windows, macOS o Linux
- .NET 9.0+
- Scheda video con supporto OpenGL

## Installazione

```bash
git clone https://github.com/pietrondo/pitlife.git
cd PitLife
dotnet build
dotnet run
```

Oppure esegui `bin/Debug/net9.0/PitLife.exe` direttamente.

## Struttura

| Directory | Contenuto |
|-----------|-----------|
| `Simulation/` | Ecosystem, Creature, Genome, Behaviors, Climate, Disease, Cataclysms, WorldGenerator |
| `Rendering/` | PixelWorldRenderer, CreatureRenderer, Minimap, DayNightCycle, Camera |
| `UI/` | MainMenu, SpawnPanel, InGameUi, CataclysmPanel, SpeciesEditor, UiWindow |
| `Core/` | Logger, AssetRegistry, SpeciesCatalog |
| `Localization/` | I18n EN/IT |
| `Content/` | Assets, sprites, font |
| `tests/` | 245 test (stabilità, performance, property-based) |

## Sviluppo

PitLife usa C# con MonoGame. Per aggiungere specie o comportamenti:
- **Specie**: modifica `Simulation/SpeciesRegistry.cs` o crea `Content/species.json`
- **Comportamenti**: implementa `ICreatureBehavior` in `Simulation/Behaviors/`
- **Biomi**: aggiungi a `BiomeType.cs`, `Tile.cs`, `WorldGenerator.cs`, renderer

## Licenza

MIT - vedi [LICENSE](LICENSE)
