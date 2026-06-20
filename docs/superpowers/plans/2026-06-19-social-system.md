# Piano d'implementazione — Sistema sociale (Genere, Branco/Solitario, Cucciolo/Adulto)

**Data**: 2026-06-19
**Spec di riferimento**: `docs/superpowers/specs/2026-06-19-social-system-design.md`
**Approccio**: TDD-friendly, bite-sized task, un branch per feature, commit atomici.

---

## Scope check

- **Subsistema singolo**: comportamento sociale creature (genere, branco/solitario, ciclo vitale).
- Un solo file di piano.
- Commit frequenti (uno per task completato).
- Build + test devono passare ad ogni commit.

---

## File structure map

### Nuovi file

| File | Scopo |
|------|-------|
| `Simulation/Gender.cs` | `enum Gender { Male, Female }` |
| `tests/PitLife.Tests/SocialSystemTests.cs` | Test per genere, branco/solitario, cucciolo/adulto, riproduzione |

### File da modificare

| File | Modifiche |
|------|-----------|
| `Simulation/Creature.cs` | Aggiungere `Gender`, `IsAdult`, `IsBaby`; `ReproduceWith` richiede genere opposto + IsAdult |
| `Simulation/Ecosystem.cs` | Aggiungere `PackSpecies`/`SolitarySpecies` HashSet, helper `IsPackAnimal`/`IsSolitary`; `SpawnSpecies` assegna genere casuale; nuovo metodo `TryReproduce(creature)` che gestisce accoppiamento M+F con M-F pair detection |
| `Simulation/Herbivore.cs` | Comportamento branco/solitario nel `Update` (dopo anti-predatore, prima di ricerca cibo) |
| `Simulation/Carnivore.cs` | Comportamento branco/solitario nel `Update` |
| `Simulation/Omnivore.cs` | Comportamento branco/solitario nel `Update` |
| `Rendering/CreatureRenderer.cs` | Scala ×0.6 se `!IsAdult`; icona ♂/♀ sotto la creatura |
| `UI/InGameUi.cs` | Aggiungere riga Genere + Età + Adulto/Cucciolo nel pannello dettagli creatura |
| `Localization/I18n.cs` | Aggiungere `ui.gender.male`, `ui.gender.female`, `ui.age`, `ui.age.seconds`, `ui.status.baby`, `ui.status.adult` (EN + IT) |

### File NON toccati

- `Simulation/Plant.cs` (no genere, no branco, no riproduzione sessuale)
- `Simulation/Genome.cs` (nessuna modifica al genoma)
- `Simulation/SpatialGrid.cs` (riusato per query prossimità stessa specie)

---

## Convenzioni

- Test xUnit `[Fact]`/`[Theory]`, stile `EcosystemTests.cs` esistente
- Coverage minimo per ogni task: almeno 1 test che esercita il codice nuovo
- Branchi/solitari definiti come `HashSet<string>` static in `Ecosystem` (analogo a `AquaticSpecies`)
- Genere assegnato random 50/50 al `SpawnSpecies` (per `Creature` non-Plant)
- Soglia età adulta: `Age >= 30f` secondi simulati

---

## Task

### Task 1 — Enum Gender

**Obiettivo**: introdurre tipo `Gender`.

**File**: `Simulation/Gender.cs` (nuovo).

**Codice**:
```csharp
namespace PitLife.Simulation;

public enum Gender
{
    Male,
    Female
}
```

**Test**: nessuno (enum puro). Verifica solo build: `dotnet build`.

**Commit**: `feat(social): add Gender enum`.

---

### Task 2 — Proprietà Gender/IsAdult/IsBaby in Creature

**Obiettivo**: aggiungere a `Creature` il genere e i flag derivati età.

**File**: `Simulation/Creature.cs`.

**Modifiche minime**:
- Aggiungere `public Gender Gender { get; set; }` (init da costruttore, default `Male`).
- Aggiungere `public bool IsAdult => Age >= 30f;`
- Aggiungere `public bool IsBaby => !IsAdult;`
- Modificare costruttore per accettare `Gender gender` come parametro opzionale (default `Male`).
- Aggiungere `Gender` al calcolo di `ReproduceWith`: due creature stesso genere non possono riprodursi (return `null`); baby non può riprodursi (return `null`).

**Test** (in `tests/PitLife.Tests/SocialSystemTests.cs`):
```csharp
[Fact]
public void Creature_IsAdult_False_Below30Seconds()
{
    var c = new Herbivore(new Vector2(10, 10), Genome.Random(new Random(1)));
    c.GrowFor(29f);
    Assert.False(c.IsAdult);
    Assert.True(c.IsBaby);
}

[Fact]
public void Creature_IsAdult_True_At30Seconds()
{
    var c = new Herbivore(new Vector2(10, 10), Genome.Random(new Random(1)));
    c.GrowFor(30f);
    Assert.True(c.IsAdult);
    Assert.False(c.IsBaby);
}

[Fact]
public void ReproduceWith_SameGender_ReturnsNull()
{
    var g = Genome.Random(new Random(1));
    var a = new Herbivore(new Vector2(10, 10), g) { Energy = 1000f, Gender = Gender.Male };
    var b = new Herbivore(new Vector2(20, 20), g) { Energy = 1000f, Gender = Gender.Male };
    Assert.Null(a.ReproduceWith(b, new Random(1)));
}

[Fact]
public void ReproduceWith_Baby_ReturnsNull()
{
    var g = Genome.Random(new Random(1));
    var a = new Herbivore(new Vector2(10, 10), g) { Energy = 1000f, Gender = Gender.Male };
    var b = new Herbivore(new Vector2(20, 20), g) { Energy = 1000f, Gender = Gender.Female };
    a.GrowFor(10f); // baby
    b.GrowFor(60f);
    Assert.Null(a.ReproduceWith(b, new Random(1)));
}
```

**Nota**: serve helper `c.GrowFor(seconds)` per testare l'età senza `Update`. Aggiungerlo a `Creature` come `internal void GrowFor(float seconds) => Age += seconds;` (per non rompere API pubblica).

**Verifica**: `dotnet test tests/PitLife.Tests --filter "FullyQualifiedName~SocialSystemTests"`.

**Commit**: `feat(social): add Gender, IsAdult, IsBaby to Creature`.

---

### Task 3 — PackSpecies e SolitarySpecies in Ecosystem

**Obiettivo**: classificare le specie in base al comportamento sociale.

**File**: `Simulation/Ecosystem.cs`.

**Modifiche**:
- Aggiungere HashSet statici accanto a `AquaticSpecies`:
  ```csharp
  private static readonly HashSet<string> PackSpecies = [
      "Deer", "Sheep", "Horse", "Goat", "Rabbit",
      "Wolf", "Lion",
      "Fish", "Salmon",
      "Gazelle"
  ];

  private static readonly HashSet<string> SolitarySpecies = [
      "Tiger", "Leopard", "Bear",
      "Crocodile", "Snake",
      "Frog", "Lizard", "Beetle", "Butterfly",
      "Eagle", "Fox", "Lynx",
      "Shark", "Piranha", "Jellyfish",
      "Turtle", "Tortoise"
  ];
  ```
- Aggiungere metodi pubblici `static bool IsPackAnimal(string species) => PackSpecies.Contains(species);` e idem `IsSolitary`.
- In `SpawnSpecies<T>`, dopo la creazione della creatura, assegnare `c.Gender = (Random.Next(2) == 0) ? Gender.Male : Gender.Female;` (solo per `CreatureType` != Plant). Aggiungere una guardia `if (c is not Plant)`.

**Test**:
```csharp
[Fact]
public void IsPackAnimal_Deer_ReturnsTrue()
{
    Assert.True(Ecosystem.IsPackAnimal("Deer"));
}

[Fact]
public void IsSolitary_Tiger_ReturnsTrue()
{
    Assert.True(Ecosystem.IsSolitary("Tiger"));
}

[Fact]
public void IsPackAnimal_Tortoise_ReturnsFalse()
{
    Assert.False(Ecosystem.IsPackAnimal("Tortoise"));
    Assert.True(Ecosystem.IsSolitary("Tortoise"));
}

[Fact]
public void Spawn_AssignsGender_ForNonPlantCreatures()
{
    var eco = new Ecosystem(32, 24, 7);
    eco.Initialize(h: 50, c: 0, o: 0, p: 0);
    // Initialize ends by calling FlushPending, so creatures are in eco.Creatures.
    Assert.All(eco.Creatures.Where(c => c.CreatureType != CreatureType.Plant),
               c => Assert.True(c.Gender == Gender.Male || c.Gender == Gender.Female));
}
```

**Verifica**: `dotnet test --filter "FullyQualifiedName~SocialSystemTests"`.

**Commit**: `feat(social): classify species as pack or solitary`.

---

### Task 4 — Comportamento branco/solitario nei sottotipi Creature

**Obiettivo**: applicare movimento sociale a Herbivore/Carnivore/Omnivore.

**File**: `Simulation/Herbivore.cs`, `Simulation/Carnivore.cs`, `Simulation/Omnivore.cs`.

**Modifica** (pattern comune, applicato in ciascun `Update` tra anti-predatore e ricerca cibo/prede):
```csharp
// Social behavior
if (Ecosystem.IsPackAnimal(Species))
{
    var neighbor = FindNearestSameSpecies(ecosystem);
    if (neighbor != null && DistanceTo(neighbor) < VisionPixels)
    {
        MoveToward(neighbor.Position, dt * 0.3f, world);
        if (DistanceTo(neighbor) < VisionPixels * 0.3f)
            Energy += 5f * dt; // bonus branco
    }
}
else if (Ecosystem.IsSolitary(Species))
{
    var neighbor = FindNearestSameSpecies(ecosystem);
    if (neighbor != null && DistanceTo(neighbor) < VisionPixels * 0.5f)
    {
        MoveAwayFrom(neighbor.Position, dt, world);
        Energy -= 3f * dt; // penalty affollamento
    }
}
```

**Helper da aggiungere a `Creature`**:
```csharp
public Creature? FindNearestSameSpecies(Ecosystem ecosystem)
{
    return ecosystem.FindNearestSameSpecies(this);
}
```

**Metodo da aggiungere a `Ecosystem`**:
```csharp
public Creature? FindNearestSameSpecies(Creature seeker)
{
    return _spatialGrid.FindNearest(seeker, c => c != seeker && c.Species == seeker.Species);
}
```

**Test** (uno per pattern):
```csharp
[Fact]
public void PackAnimal_MovesToward_SameSpeciesNeighbor()
{
    // Spawn 2 Deer vicini, verifica che dopo alcuni tick si avvicinino
}

[Fact]
public void SolitaryAnimal_MovesAway_FromSameSpeciesNeighbor()
{
    // Spawn 2 Tiger vicini, verifica che dopo alcuni tick si allontanino
}
```

**Verifica**: `dotnet test`.

**Commit**: `feat(social): add pack and solitary behavior to creatures`.

---

### Task 5 — Riproduzione M+F e baby non si riproduce

**Obiettivo**: completare la logica riproduttiva richiedendo M+F adulti.

**File**: `Simulation/Ecosystem.cs`.

**Modifica**: aggiungere metodo `TryFindMate(Creature seeker)` e logica di riproduzione nel `Tick` o nell'`Update` delle creature.

**Approccio minimal**:
- Aggiungere in `Creature.Update` (hook, opzionale solo se `IsAdult` e `Energy > ReproductionThreshold`):
  ```csharp
  if (IsAdult && Energy > ReproductionThreshold && Ecosystem.IsPackAnimal(Species))
  {
      var mate = ecosystem.FindNearestMate(this);
      if (mate != null && DistanceTo(mate) < VisionPixels * 0.5f)
      {
          var child = ReproduceWith(mate, ecosystem.Random);
          if (child != null) ecosystem.AddCreature(child);
      }
  }
  ```
- Aggiungere `FindNearestMate` a `Ecosystem`:
  ```csharp
  public Creature? FindNearestMate(Creature seeker)
  {
      return _spatialGrid.FindNearest(seeker,
          c => c != seeker && c.Species == seeker.Species
            && c.Gender != seeker.Gender && c.IsAdult);
  }
  ```

**Test**:
```csharp
[Fact]
public void AdultMale_FindsAdultFemale_SameSpecies()
{
    var eco = new Ecosystem(64, 48, 7);
    var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Male, Energy = 1000f };
    var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Female, Energy = 1000f };
    m.GrowFor(60f); f.GrowFor(60f);
    eco.AddCreature(m);
    eco.AddCreature(f);
    eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1))); // flush _pendingAdd into _spatialGrid
    var mate = eco.FindNearestMate(m);
    Assert.Same(f, mate);
}

[Fact]
public void BabyMale_FindsNoMate()
{
    var eco = new Ecosystem(64, 48, 7);
    var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Male, Energy = 1000f };
    var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Female, Energy = 1000f };
    m.GrowFor(10f); // baby
    f.GrowFor(60f);
    eco.AddCreature(m);
    eco.AddCreature(f);
    eco.Tick(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1))); // flush _pendingAdd into _spatialGrid
    Assert.Null(eco.FindNearestMate(m));
}
```

**Verifica**: `dotnet test`.

**Commit**: `feat(social): reproduction requires adult M+F pair`.

---

### Task 6 — Visualizzazione cucciolo + icona genere

**Obiettivo**: differenziare visivamente baby e genere.

**File**: `Rendering/CreatureRenderer.cs`.

**Modifica in `Draw`**:
- Dopo aver calcolato `size`:
  ```csharp
  float renderSize = c.IsBaby ? size * 0.6f : size;
  int s = Math.Max(4, (int)renderSize);
  Rectangle dest = new((int)(px - s / 2), (int)(py - s / 2), s, s);
  ```
- Dopo aver disegnato la creatura, se NON è pianta, disegnare icona genere:
  ```csharp
  if (c.CreatureType != CreatureType.Plant && _pixelTexture != null)
  {
      Color genderColor = c.Gender == Gender.Male ? Color.Red : new Color(80, 120, 255);
      int dot = Math.Max(2, s / 6);
      int yOff = s / 2 + 2;
      sb.Draw(_pixelTexture, new Rectangle((int)px - dot / 2, (int)py + yOff, dot, dot), genderColor);
  }
  ```

**Test**: minimo, solo build. Il rendering è difficile da testare senza mock grafico; verifica manuale eseguendo il gioco.

**Verifica**: `dotnet build`.

**Commit**: `feat(social): render baby size and gender icon`.

---

### Task 7 — UI: mostrare Genere ed Età nel pannello creatura

**Obiettivo**: aggiungere informazioni sociali al pannello UI.

**File**: `UI/InGameUi.cs`.

**Modifica**: aggiungere 2 righe al pannello dettagli creatura (sotto la riga "Specie"):
```csharp
sb.DrawString(font, $"Gender: {creature.Gender}", pos, color);
sb.DrawString(font, $"Age: {creature.Age:F1}s ({creature.IsAdult ? "Adult" : "Baby"})", pos, color);
```
- Usare `Localization.I18n.T("ui.gender.male")` / `T("ui.gender.female")` per il genere.
- Per adult/baby usare `T("ui.status.adult")` / `T("ui.status.baby")`.

**File**: `Localization/I18n.cs`.

**Aggiunte EN**:
```json
"ui.gender.male": "Male",
"ui.gender.female": "Female",
"ui.age": "Age",
"ui.age.seconds": "{0:F1}s",
"ui.status.adult": "Adult",
"ui.status.baby": "Baby"
```

**Aggiunte IT**:
```json
"ui.gender.male": "Maschio",
"ui.gender.female": "Femmina",
"ui.age": "Età",
"ui.age.seconds": "{0:F1}s",
"ui.status.adult": "Adulto",
"ui.status.baby": "Cucciolo"
```

**Test**: nessuno (stringhe localization, richiederebbe mock del dizionario).

**Verifica**: `dotnet build`, manuale in gioco.

**Commit**: `feat(social): add gender and age to creature info panel`.

---

### Task 8 — Test integrazione: simulazione M+F produce cucciolo

**Obiettivo**: smoke test che il sistema end-to-end funzioni.

**File**: `tests/PitLife.Tests/SocialSystemTests.cs`.

**Test**:
```csharp
[Fact]
public void Integration_AdultPairReproduces_BabyHasNoGenderReproduction()
{
    var eco = new Ecosystem(64, 48, 42);
    var m = new Herbivore(new Vector2(100, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Male, Energy = 1000f };
    var f = new Herbivore(new Vector2(110, 100), Genome.Random(new Random(1)))
            { Gender = Gender.Female, Energy = 1000f };
    m.GrowFor(60f); f.GrowFor(60f);
    eco.AddCreature(m);
    eco.AddCreature(f);

    var child = m.ReproduceWith(f, new Random(1));
    Assert.NotNull(child);
    Assert.True(child.IsBaby);
    Assert.True(child.Gender == Gender.Male || child.Gender == Gender.Female);

    // Il cucciolo non si riproduce anche se ha energia
    child.Energy = 1000f;
    var other = new Herbivore(new Vector2(120, 100), Genome.Random(new Random(2)))
                { Gender = child.Gender == Gender.Male ? Gender.Female : Gender.Male, Energy = 1000f };
    other.GrowFor(60f);
    Assert.Null(child.ReproduceWith(other, new Random(1)));
}
```

**Verifica**: `dotnet test PitLife.sln` (suite completa).

**Commit**: `test(social): integration test for M+F reproduction and baby sterility`.

---

## Sequenza di esecuzione

Eseguire i task nell'ordine 1 → 8. Ogni task è autonomo e produce un commit atomico. Dopo ogni task:

1. `dotnet build` (deve essere 0 errori, 0 warning)
2. `dotnet test tests/PitLife.Tests --filter "FullyQualifiedName~SocialSystemTests"` (solo per task 2-5, 8)
3. Git add + commit con messaggio descritto

Dopo il task 8 finale:

1. `dotnet test PitLife.sln` (suite completa)
2. Esecuzione manuale del gioco (verifica rendering baby + icona genere + pannello UI)
3. Commit di chiusura se necessario

## Rischio e mitigazione

| Rischio | Mitigazione |
|---------|-------------|
| Render baby crea texture troppo piccola per essere visibile | Minimo 4 px (controllo `Math.Max(4, ...)`) |
| Specie non in Pack né Solitary (es. nuove aggiunte) | Default: comportamento attuale (wander) — nessuna regressione |
| Riproduzione troppo frequente satura `MaxCreatures` | Già gestito da `Ecosystem.MaxCreatures`; `AddCreature` scarta se pieno |
| `Gender` rompe serializzazione save/load futura | Fuori scope; aggiungere ignore attribute più tardi se serve |
