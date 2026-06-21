# PitLife

Una simulazione di ecosistema 2D con creature che vivono, si nutrono, si riproducono e interagiscono in un mondo dinamico.

## Come Giocare

### Obiettivo
PitLife è una simulazione sandbox: non c'è un obiettivo preciso, ma puoi osservare come l'ecosistema evolve nel tempo. Prova a mantenere l'equilibrio tra piante, erbivori, carnivori e onnivori!

### Controlli

#### Movimento e Camera
- **WASD** o **Frecce direzionali**: Muovi la camera nel mondo
- **Rotella del mouse**: Zoom in/out
- **Click sinistro su una creatura**: Seleziona la creatura per vedere i dettagli

#### Velocità di Simulazione
- **1**: Velocità normale (1x)
- **2**: Velocità doppia (2x)
- **3**: Velocità quadrupla (4x)
- **Spazio**: Pausa/Riprendi

#### Interfaccia
- **F2**: Apri/Chiudi finestra Statistiche
- **F3**: Apri/Chiudi finestra Dettagli Creatura
- **ESC**: Apri menu principale

#### Spawn Creature
1. Seleziona una specie dal pannello "Spawn" a destra
2. Clicca sulla mappa dove vuoi far apparire la creatura
3. Le creature si evolvono e si riproducono automaticamente!

### Tipi di Creature

| Tipo | Comportamento | Esempi |
|------|--------------|--------|
| **Piante** | Crescono automaticamente, fonte di cibo | Erba, Fiori, Funghi, Cactus |
| **Erbivori** | Mangiano piante, fuggono dai predatori | Conigli, Cervi, Pecore |
| **Carnivori** | Cacciano altre creature | Lupi, Tigri, Aquile |
| **Onnivori** | Mangiano piante e creature | Orsi, Procioni, Maiali |

### Sistema di Ecosistema

- **Ciclo giorno/notte**: Le creature sono più attive durante il giorno
- **Energia**: Ogni creatura ha bisogno di energia per sopravvivere
- **Età**: Le creature invecchiano e muoiono di vecchiaia
- **Riproduzione**: Le creature adulte si riproducono quando hanno abbastanza energia
- **DNA**: Ogni creatura ha un genoma unico che influenza le sue caratteristiche

### Consigli

- **Equilibrio**: Troppi erbivori esauriranno le piante; troppi carnivori decimeranno gli erbivori
- **Seed**: Usa lo stesso seed per rigenerare un mondo identico
- **Osserva**: Clicca su una creatura per vedere le sue statistiche dettagliate

## Requisiti di Sistema

- Windows, macOS o Linux
- .NET 9.0 o superiore
- Scheda video con supporto DirectX/OpenGL

## Installazione

### Da Codice Sorgente
```bash
git clone <repository-url>
cd PitLife
dotnet build
dotnet run
```

## Sviluppo

PitLife è sviluppato in C# con MonoGame framework.

### Struttura del Progetto
- `Simulation/`: Logica di simulazione (Ecosystem, Creature, World)
- `UI/`: Interfaccia utente (Menu, Pannelli, Renderer)
- `Rendering/`: Rendering grafico (Creature, World, Minimap)
- `Localization/`: Supporto multilingua (Italiano/Inglese)

## Licenza

PitLife è distribuito con licenza [MIT](LICENSE).

## Crediti

Sviluppato con ❤️ usando MonoGame e .NET
