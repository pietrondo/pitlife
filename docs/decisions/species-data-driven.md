# Specie data-driven: eliminare la duplicazione BuiltinSpecies ↔ species.json

**Issue:** pietr-bpa

## Problema
`Simulation/Entities/BuiltinSpecies.cs` (396 righe) registra 79 specie hardcoded in C#
tramite `RegisterPlant`/`RegisterAnimal`. `Content/config/species.json` definisce 120 specie.
Le 77 specie hardcoded sono TUTTE presenti anche nel JSON (overlap 77/77): non esiste
alcuna specie esclusivamente builtin. Violazione della regola 6 ("species ... in external
config files, not in source code") e doppia fonte di verità che può andare in drift.

## Semantica attuale
`SpeciesRegistry.Register` sovrascrive per nome (`_bySpecies[def.Species] = def`).
Il costruttore statico esegue `BuiltinSpecies.RegisterAll()` e poi `TryLoadJsonOverrides()`,
che deserializza species.json e chiama `Register` per ogni entry → **il JSON vince sempre**.
Il registro effettivo contiene 120 specie (77 override + 43 solo-JSON); le builtin sono
sempre sovrascritte e quindi già di fatto morte quando species.json è presente (sempre:
è copiato in output come Content item, anche nei bin dei test).

## Alternative considerate
1. **Lasciare i builtin come fallback** — scartato: fallback difende solo lo scenario
   "species.json cancellato/corrotto" (installazione già rotta); costa 396 righe di dati
   duplicati + rischio drift.
2. **Eliminare i builtin, species.json unica fonte** — scelto: conforme a regola 6,
   rimuove la duplicazione. Il caso JSON mancante resta non-fatal (try/catch esistente).

## Approccio scelto
- Eliminare `BuiltinSpecies.cs` e la chiamata `BuiltinSpecies.RegisterAll()`.
- `SpeciesRegistry` carica solo da `species.json` (la `TryLoadJsonOverrides` diventa il
  loader primario; aggiornare il commento, non più "keep built-in species").
- Nessuna modifica a `SpeciesJsonLoader` (già completo: campi size/maturity/temperature/etc.).

## Verifica di sicurezza
- 0 specie esclusive builtin (overlap 77/77) → rimuovere i builtin non toglie alcuna specie
  al registro effettivo.
- I test girano con species.json presente nel bin (Content item) → nessun test dipende da
  specie builtin-only.
- Gate: `dotnet build` + suite completa; il config-doctor deve riportare 0 duplicati.

## File impattati
- `Simulation/Entities/BuiltinSpecies.cs` (eliminato)
- `Simulation/Entities/SpeciesRegistry.cs` (rimossa chiamata, commenti aggiornati)
