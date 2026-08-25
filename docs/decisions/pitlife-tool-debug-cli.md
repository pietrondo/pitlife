# PitLife.Tool — CLI locale di debug

**Issue:** pietr-fpr

## Problema
PitLife è un'app MonoGame (WinExe) senza modalità headless né strumenti di diagnosi.
I problemi data-driven (specie duplicate C#/JSON, drift default-vs-JSON, JSON malformati)
oggi emergono solo via grep manuale o test. Serve un tool locale per ispezionare config
e debuggare la simulazione senza lanciare il client grafico.

## Alternative considerate
1. **Estendere solo i test xUnit** — scartato: non dà un output diagnostico rapido e
   leggibile; i test usano i default C# (CWD diversa), mascherando il drift.
2. **Script bash one-off** — scartato: fragili su Windows, non riusabili, niente reflection.
3. **Progetto console dedicato `PitLife.Tool`** — scelto: stesso pattern già usato da
   `PitLife.Benchmarks` (OutputType Exe + ProjectReference a PitLife.csproj), riusabile,
   reflection e types C# disponibili.

## Approccio scelto
Un unico progetto console `tools/PitLife.Tool` con due comandi, zero dipendenze esterne
(arg parsing manuale, System.Text.Json già presente):

- **`config-doctor`** — diagnostica data-driven:
  1. validità JSON di ogni file in `Content/config/` (parse con System.Text.Json);
  2. duplicazione specie: `species.json` vs nomi hardcoded in `BuiltinSpecies.cs`
     (regex sul sorgente), riporta l'overlap;
  3. drift default-vs-JSON: per ciascuna delle 16 classi `*Config` riflette la proprietà
     statica `Data`, confronta `new()` (default C#) con il valore deserializzato dal JSON,
     segnalando i campi divergenti (es. i vecchi "// Fix tests").

- **`sim --ticks N --seed S --width W --height H [--herbivores H --carnivores C --omnivores O --plants P --interval R]`** —
  runner headless: crea `Ecosystem`, `Initialize`, itera `Tick(new GameTime(...))` per N
  tick, stampa a intervalli R: tick, piante, erbivori, carnivori, onnivori, totale creature.

La root del repo (per risolvere `Content/config`) viene individuata risalendo da
`AppContext.BaseDirectory` finché non si trova `Content/config`, poi `SetCurrentDirectory`.

## File impattati
- `tools/PitLife.Tool/PitLife.Tool.csproj` (nuovo)
- `tools/PitLife.Tool/Program.cs`, `ConfigDoctor.cs`, `SimRunner.cs` (nuovi)
- `PitLife.sln` (aggiunta progetto)

## Non-funzionale
Nessun cambio al codice di gioco. Nessuna dipendenza nuova. Nessuna InternalsVisibleTo
necessaria (reflection su tipi public; BuiltinSpecies letto dal sorgente).
