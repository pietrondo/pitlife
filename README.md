# PitLife

Clone di SimLife (Maxis 1992) — simulazione di ecosistema con genetica, mutazioni e catena alimentare.

- **Engine**: MonoGame 3.8 (DesktopGL)
- **Grafica**: PixelLab pixel art
- **Target**: .NET 9

## Eseguire

```bash
dotnet run
```

WASD/Arrows per muovere la camera, scroll per zoom, ESC per uscire.

## Creature

| Classe      | Specie                                  |
|-------------|-----------------------------------------|
| Piante      | Bush, Flowers, Mushroom, GrassTuft, Cactus |
| Erbivori    | Gazelle, Rabbit, Deer, Sheep            |
| Carnivori   | Wolf, Fox, Lynx, Tiger                  |
| Onnivori    | Bear, Boar, Raccoon                     |

Ogni creatura ha un genoma unico con 6 geni che mutano alla riproduzione.
