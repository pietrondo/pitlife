<p align="center">
  <img src="Content/assets/logo.png" alt="PitLife logo" width="180">
</p>

<h1 align="center">PitLife</h1>

Clone di SimLife (Maxis 1992) — simulazione di ecosistema con genetica, mutazioni e catena alimentare.

- **Engine**: MonoGame 3.8 (DesktopGL)
- **Grafica**: PixelLab pixel art
- **Target**: .NET 9

## Eseguire

```bash
dotnet run
```

Il menu principale supporta mouse, frecce e Invio. La finestra Opzioni permette di attivare o disattivare lo schermo intero.

Durante la simulazione: WASD/frecce per muovere la camera, scroll per zoom, spazio per la pausa ed ESC per tornare al menu.

## Creature

| Classe      | Specie                                  |
|-------------|-----------------------------------------|
| Piante      | Bush, Flowers, Mushroom, GrassTuft, Cactus |
| Erbivori    | Gazelle, Rabbit, Deer, Sheep            |
| Carnivori   | Wolf, Fox, Lynx, Tiger                  |
| Onnivori    | Bear, Boar, Raccoon                     |

Ogni creatura ha un genoma unico con 6 geni che mutano alla riproduzione.
