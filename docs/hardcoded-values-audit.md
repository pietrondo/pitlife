# Hardcoded Values Audit — PitLife-n8r

Date: 2026-06-23 | 150+ hardcoded values across 16 files

## Priority Summary

| Priority | File | Action |
|----------|------|--------|
| **P0** | `Content/config/biomes.json` | **CREATED** — biome vegetation/grass/temperature moved from Tile.cs |
| **P1** | `Content/config/species.json` | Species from BuiltinSpecies.cs (~80 species) |
| **P1** | `Content/config/balance.json` | Creature energy formulas, thresholds, scales |
| **P1** | `Content/config/diseases.json` | Disease presets from DiseaseSystem.cs |
| **P2** | `Content/config/climate.json` | Seasons, extreme events from ClimateSystem.cs |
| **P2** | `Content/config/atmosphere.json` | O2/CO2 constants from AtmosphereSystem.cs |
| **P2** | `Content/config/behaviors.json` | Flocking params, feeding rates from Behaviors/ |
| **P3** | `Content/config/cataclysms.json` | Cataclysm event definitions |
| **P3** | `Content/config/genetics.json` | Mutation rates, gene ranges from Genetics.cs |
| **P3** | `Content/config/evolution.json` | Evolution thresholds from EvolutionRules.cs |

## Top 10 Highest-Impact Hardcoded Values

1. **BuiltinSpecies.cs** — 80 species defined in C# (should be `species.json`)
2. **Tile.cs:57-102** — 15 biome vegetation/grass/temp maps (**DONE** — `biomes.json`)
3. **Creature.cs:25-100** — 20+ energy/formula constants
4. **Creature.cs:160-303** — Climate/pressure/O2/altitude energy modifiers
5. **FeedingModule.cs** — 15+ feeding rates and damage formulas
6. **ClimateSystem.cs:42-49** — Seasonal modifier tuples
7. **DiseaseSystem.cs:21-25** — 3 disease preset definitions
8. **CataclysmSystem.cs:74-256** — 20+ event duration/multiplier/radius values
9. **SocialModule.cs:44-182** — Flocking weights and combat parameters
10. **AtmosphereSystem.cs:9-18** — O2/CO2 balance constants

## Next Steps (future issues)
- [ ] Create `species.json` from BuiltinSpecies.cs
- [ ] Create `balance.json` from Creature.cs constants
- [ ] Create `diseases.json` from DiseaseSystem presets
- [ ] Create `climate.json` from ClimateSystem season modifiers
- [ ] Write E2E test that loads config from JSON
