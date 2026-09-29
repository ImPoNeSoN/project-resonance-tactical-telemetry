# 07 — Production Plan (Project Management View)

Planning date: Tuesday, September 29, 2026 (America/Chicago). All dates are local calendar dates; sprints run Monday to Sunday, two weeks each.

---

## 7.1 Team model and owner codes

The plan assumes a **small team of 3.2 FTE on average**: one full-time lead who owns design and engineering, one full-time 3D/technical artist from the Prototype milestone, plus part-time contractors. A **solo** variant is in 7.9.

| Code | Role | Commitment | Joins | Owns |
|---|---|---|---|---|
| LD | Lead Designer-Engineer (project owner) | 1.0 FTE | Concept | Game design, `Resonance.Sim`, gambit interpreter, gameplay code, production, balance |
| TA | 3D / Technical Artist | 1.0 FTE | Sprint 5 | Characters, bosses, arenas, shaders, VFX, render performance |
| UX | UI/UX Designer (contract) | 0.5 FTE | Sprint 9 | HUD, Gambit Deck editor, menus, accessibility |
| BE | Backend Engineer (contract) | 0.5 FTE | Sprint 19 | ASP.NET Core API, validator, raid host, ops |
| AU | Audio Designer / Composer (contract) | 0.2 FTE | Sprint 13 | SFX, music, bark recording direction |
| QA | QA Tester (contract) | 0.5 FTE | Sprint 25 | Test plans, regression, determinism audits, perf capture |
| CM | Community / Marketing (contract) | 0.2 FTE | Sprint 31 | Steam page, playtests, Discord, beta ops |

FTE ramp: 1.0 (S1–S4), 2.0 (S5–S8), 2.5 (S9–S12), 2.7 (S13–S18), 3.2 (S19–S24), 3.7 (S25–S30), 3.9 (S31–S54).

---

## 7.2 Milestone overview

| # | Milestone | Sprints | Dates | Duration | Primary owner | Exit criteria (all must pass) |
|---|---|---|---|---|---|---|
| M1 | Concept | S1–S2 | Oct 5 – Nov 1, 2026 | 4 weeks | LD | This design bible approved; one-page pitch; combat spreadsheet model of 02 reproduces the 02 §2.15 log numbers exactly; C# solution skeleton builds; risk register baselined |
| M2 | Godot C# Prototype | S3–S8 | Nov 2, 2026 – Jan 24, 2027 | 12 weeks | LD | Grey-box battle with 4 heroes vs 1 boss (3 parts) fully playable; `Resonance.Sim` passes golden test of the 02 §2.15 log; event-driven loop ≤ 0.3 ms per frame; Tactical Pause; gambit interpreter with 10 conditions and 3 verbs; debug Math Console |
| M3 | Vertical Slice | S9–S18 | Jan 25 – Jun 13, 2027 | 20 weeks | LD, TA | One shaft (6 floors, 2 bosses) at final quality; 8 heroes at final art; full HUD (timeline, anatomy, oscilloscope, console); 4K/60 on High reference hardware; Gambit Deck editor usable by external playtesters (5 of 8 testers build a working deck unaided) |
| M4 | Alpha | S19–S30 | Jun 14 – Nov 28, 2027 | 24 weeks | LD, BE | Feature complete: all 24 heroes playable (at least grey-box art), all systems in 02/04/05 implemented, Ghost Protocol live on staging with validator, raid prototype with 2 squads, save system, settings, accessibility baseline |
| M5 | Content Complete | S31–S42 | Nov 29, 2027 – May 14, 2028 | 24 weeks | TA, LD | All 24 heroes at final art and VFX; 5 shafts; World Boss; 20 chips; 12 Legendaries; all barks recorded; localization strings frozen (EN, DE, FR, ES, PT-BR, JA, ZH-Hans) |
| M6 | Beta | S43–S50 | May 15 – Sep 3, 2028 | 16 weeks | QA, LD, BE | Closed beta (1,000 players) then open beta (Steam Playtest); crash-free sessions ≥ 99.5%; zero known determinism desyncs across Windows x64, Linux x64, macOS ARM64; performance gates in 7.6 met; backend load test at 3× target concurrency |
| M7 | Gold | S51–S53 | Sep 4 – Oct 15, 2028 | 6 weeks | LD | Release candidate; Steam review passed; Steam Deck Verified submission; no A/B bugs open; day-1 patch pipeline tested |
| M8 | Launch | S54 | Launch Tuesday, Oct 24, 2028 | 2 weeks | LD, CM, BE | Store live; arena Season 1 opens; live-ops on-call rota; post-launch sprint S55 reserved for hotfixes |

---

## 7.3 Sprint plan (all 55 sprints)

Holiday capacity: S6 and S7 (late Dec 2026 – early Jan 2027), S32 and S33 (late Dec 2027 – early Jan 2028) are planned at 50% capacity.

| Sprint | Dates | Milestone | Sprint goal | Owner(s) |
|---|---|---|---|---|
| S1 | Oct 5 – Oct 18, 2026 | M1 Concept | Finalize design bible; build combat math spreadsheet from 02 | LD |
| S2 | Oct 19 – Nov 1, 2026 | M1 Concept | C# solution skeleton (Sim, Game, Tests), CI build, risk register, art style frames (contract concept artist, 2 weeks) | LD |
| S3 | Nov 2 – Nov 15, 2026 | M2 Prototype | `Resonance.Sim` core: centi-AP gauge, tick order, event scheduler, PCG32 | LD |
| S4 | Nov 16 – Nov 29, 2026 | M2 Prototype | Damage pipelines (physical, elemental-physical, magical, true), hit/crit, xUnit tests | LD |
| S5 | Nov 30 – Dec 13, 2026 | M2 Prototype | Enmity system (per-part tables, VE buckets, shed); TA onboarding and hero base mesh | LD, TA |
| S6 | Dec 14 – Dec 27, 2026 | M2 Prototype | Resonance matrix L1–L3, detonations, burst windows (50% capacity) | LD, TA |
| S7 | Dec 28, 2026 – Jan 10, 2027 | M2 Prototype | Chants, interrupts, loadout swaps; SimBridge and grey-box BattleScene (50% capacity) | LD, TA |
| S8 | Jan 11 – Jan 24, 2027 | M2 Prototype | Gambit parser + interpreter v1; Tactical Pause; golden test of 02 §2.15; M2 review | LD, TA |
| S9 | Jan 25 – Feb 7, 2027 | M3 Slice | Timeline Bar v1; material masters (carbon, brass, circuit, crystal) | LD, TA, UX |
| S10 | Feb 8 – Feb 21, 2027 | M3 Slice | Boss anatomy panel; hero 1 (Korrith) final art | LD, TA, UX |
| S11 | Feb 22 – Mar 7, 2027 | M3 Slice | Resonance Oscilloscope shader; hero 6 (Mirrim) final | LD, TA, UX |
| S12 | Mar 8 – Mar 21, 2027 | M3 Slice | Math Console virtualized renderer; hero 17 (Zeph) final | LD, TA, UX |
| S13 | Mar 22 – Apr 4, 2027 | M3 Slice | Chain shakes and directional burst particles; audio onboarding | LD, TA, AU |
| S14 | Apr 5 – Apr 18, 2027 | M3 Slice | Dungeon run generator, rooms, ADM system; hero 10 (Seraphine) final | LD, TA |
| S15 | Apr 19 – May 2, 2027 | M3 Slice | Extract/Delve, loot, chips v1; boss 1 final art | LD, TA, UX |
| S16 | May 3 – May 16, 2027 | M3 Slice | Gambit Deck editor UI; heroes 5 and 20 final | LD, TA, UX |
| S17 | May 17 – May 30, 2027 | M3 Slice | 4K performance pass (FSR 2, LOD, pooling); heroes 3 and 13 final | LD, TA |
| S18 | May 31 – Jun 13, 2027 | M3 Slice | Slice polish; external playtest (8 testers); M3 review | LD, TA, UX, AU |
| S19 | Jun 14 – Jun 27, 2027 | M4 Alpha | Backend skeleton (ASP.NET Core, Postgres, Redis, Steam auth); heroes 1–12 abilities in data | LD, BE |
| S20 | Jun 28 – Jul 11, 2027 | M4 Alpha | Heroes 13–24 abilities in data; ability automated tests for all 72 | LD, BE, TA |
| S21 | Jul 12 – Jul 25, 2027 | M4 Alpha | Ghost snapshot upload, canonical hashing; racial passives complete | LD, BE, TA |
| S22 | Jul 26 – Aug 8, 2027 | M4 Alpha | Validator worker, replay format, checkpoint hashing | LD, BE, TA |
| S23 | Aug 9 – Aug 22, 2027 | M4 Alpha | Glicko-2 rating service, leaderboards; gambit catalog complete (34 conditions) | LD, BE, TA |
| S24 | Aug 23 – Sep 5, 2027 | M4 Alpha | Live Assault input logging and validation; all 20 chips in data | LD, BE, TA, UX |
| S25 | Sep 6 – Sep 19, 2027 | M4 Alpha | Raid host: epoch coupling, 2-squad prototype; QA onboarding and determinism test matrix | LD, BE, QA |
| S26 | Sep 20 – Oct 3, 2027 | M4 Alpha | Global Magic Burst; raid pause budget; SignalR client | LD, BE, QA |
| S27 | Oct 4 – Oct 17, 2027 | M4 Alpha | Save/load, settings, input remapping, accessibility baseline | LD, UX, QA |
| S28 | Oct 18 – Oct 31, 2027 | M4 Alpha | Shafts 2–5 layouts (grey-box), 10 floor bosses grey-box | LD, TA, QA |
| S29 | Nov 1 – Nov 14, 2027 | M4 Alpha | Full playthrough bug burn-down; cross-platform determinism audit | LD, QA, BE |
| S30 | Nov 15 – Nov 28, 2027 | M4 Alpha | Alpha lock; internal alpha (30 friends-and-family); M4 review | All |
| S31 | Nov 29 – Dec 12, 2027 | M5 Content | Heroes 2, 4 final art; Steam page live; CM onboarding | TA, CM, LD |
| S32 | Dec 13 – Dec 26, 2027 | M5 Content | Heroes 7, 8 final (50% capacity) | TA, LD |
| S33 | Dec 27, 2027 – Jan 9, 2028 | M5 Content | Heroes 9, 11 final (50% capacity) | TA, LD |
| S34 | Jan 10 – Jan 23, 2028 | M5 Content | Heroes 12, 14 final; shaft 2 art | TA, LD |
| S35 | Jan 24 – Feb 6, 2028 | M5 Content | Heroes 15, 16 final; shaft 3 art | TA, LD, AU |
| S36 | Feb 7 – Feb 20, 2028 | M5 Content | Heroes 18, 19 final; bark recording batch 1 | TA, LD, AU |
| S37 | Feb 21 – Mar 5, 2028 | M5 Content | Heroes 21, 22 final; shaft 4 art | TA, LD |
| S38 | Mar 6 – Mar 19, 2028 | M5 Content | Heroes 23, 24 final; shaft 5 art | TA, LD |
| S39 | Mar 20 – Apr 2, 2028 | M5 Content | World Boss Hollow-Sun Colossus final; bark batch 2 | TA, LD, AU |
| S40 | Apr 3 – Apr 16, 2028 | M5 Content | 12 Legendary Prototype visuals; music final | TA, AU, LD |
| S41 | Apr 17 – Apr 30, 2028 | M5 Content | Localization string freeze and send-out; tutorial | LD, UX, CM |
| S42 | May 1 – May 14, 2028 | M5 Content | Content lock; M5 review | All |
| S43 | May 15 – May 28, 2028 | M6 Beta | Closed beta wave 1 (250 players); telemetry dashboards | QA, BE, CM, LD |
| S44 | May 29 – Jun 11, 2028 | M6 Beta | Balance pass 1 (hero win rates, arena meta) | LD, QA |
| S45 | Jun 12 – Jun 25, 2028 | M6 Beta | Closed beta wave 2 (1,000 players); raid beta | QA, BE, CM |
| S46 | Jun 26 – Jul 9, 2028 | M6 Beta | Performance gates (7.6), Steam Deck pass | TA, LD, QA |
| S47 | Jul 10 – Jul 23, 2028 | M6 Beta | Localization integration and LQA | LD, QA, UX |
| S48 | Jul 24 – Aug 6, 2028 | M6 Beta | Open beta (Steam Playtest); backend load test at 3× | BE, QA, CM |
| S49 | Aug 7 – Aug 20, 2028 | M6 Beta | Balance pass 2; anti-cheese tuning | LD, BE |
| S50 | Aug 21 – Sep 3, 2028 | M6 Beta | Beta close; bug triage; M6 review | All |
| S51 | Sep 4 – Sep 17, 2028 | M7 Gold | Release candidate 1; certification checklist | LD, QA |
| S52 | Sep 18 – Oct 1, 2028 | M7 Gold | RC2; Steam review submission; Deck Verified submission | LD, QA, CM |
| S53 | Oct 2 – Oct 15, 2028 | M7 Gold | Gold master; day-1 patch pipeline dry run; launch comms | LD, BE, CM |
| S54 | Oct 16 – Oct 29, 2028 | M8 Launch | Launch Tuesday Oct 24, 2028; Season 1 opens; on-call | LD, BE, CM, QA |
| S55 | Oct 30 – Nov 12, 2028 | Post-launch | Hotfixes, first balance patch, retrospective | All |

---

## 7.4 Workstream effort estimates

Estimates are in person-weeks (pw) of focused work, including tests, for the small-team plan. The estimates carry no padding; schedule contingency comes from the scope-cut ladder in 7.8.

| Workstream | Scope reference | Estimate (pw) | Owner |
|---|---|---|---|
| `Resonance.Sim` core (timeline, pipelines, enmity, resonance, bursts, statuses, loadouts) | 02 | 16 | LD |
| Racial passives and 72 abilities in data + tests | 01, 03 | 10 | LD |
| Gambit parser, interpreter, validator | 04 §4.12 | 7 | LD |
| Gambit Deck editor UI | 04 §4.12.7 | 5 | UX, LD |
| HUD: timeline, anatomy, oscilloscope, math console, party panel | 04 §4.7–4.10 | 12 | UX, LD |
| Battle presentation (SimBridge, presenter, animation state machines) | 02 §2.16.4, 04 §4.14 | 8 | LD |
| Rendering: master materials, WorldEnvironment, presets, performance | 04 §4.3–4.4 | 8 | TA |
| VFX: shakes, 11 property sparks, 7 L2, 4 L3, bursts, global burst | 04 §4.5 | 12 | TA |
| Hero art: 24 heroes (model, rig, 14 animations each) | 03 | 60 (2.5 pw each) | TA (+ outsourced animation for 12 heroes) |
| Bosses: 11 floor bosses + World Boss | 05, 06 | 30 | TA |
| Arenas: 5 shafts × 3 arena sets + World Boss arena | 01 §1.9 | 16 | TA |
| Dungeon systems (generator, ADM, extraction, loot, chips, refinement) | 05 | 9 | LD |
| Backend API, auth, storage | 06 §6.6 | 8 | BE |
| Arena: validator, rating, leaderboards, anti-cheese | 06 §6.2–6.3 | 9 | BE, LD |
| Raids: raid host, epochs, client session, rollback | 06 §6.4–6.5 | 10 | BE, LD |
| Audio: SFX (≈ 600), music (12 tracks), barks (480 lines × 7 languages) | 01 §1.8 | 14 | AU |
| UI shell: menus, roster, loadouts, settings, save | — | 8 | UX, LD |
| QA: test plans, regression, determinism matrix, perf captures | — | 30 | QA |
| Localization (7 languages) | — | Vendor | CM, LD |
| Marketing, community, beta ops | — | 12 | CM |
| **Total** | | **~284 pw** (+ vendor localization, + outsourced animation) | |

Against capacity: the FTE ramp in 7.1 sums to 346 pw over the 108 weeks from S1 to S54 (S1–S4: 8, S5–S8: 16, S9–S12: 20, S13–S18: 32.4, S19–S24: 38.4, S25–S30: 44.4, S31–S54: 187.2). Holiday sprints remove about 8 pw, and about 15% of the remainder goes to meetings, reviews and admin, which leaves about 287 pw productive against 284 pw estimated. **The plan has almost no slack.** Contingency is the scope-cut ladder (7.8): rungs 1–5 together free 40 pw (14% of the estimate), and the plan assumes at least rungs 1 and 4 will be taken unless velocity beats estimates through Alpha.

---

## 7.5 Top risks (risk register)

Scores: Likelihood (L) and Impact (I) from 1 to 5; Exposure = L × I.

| # | Risk | L | I | Exp. | Mitigation | Early warning trigger | Owner |
|---|---|---|---|---|---|---|---|
| R1 | **CTB tick sim performance in C# hot paths**: per-tick loops, LINQ, boxing, allocations and GC pauses cause frame hitches at 4× speed and slow validators | 4 | 5 | 20 | Event-driven time skipping (02 §2.16.2); struct-of-arrays `BattleState`; zero-allocation rule enforced by a BenchmarkDotNet suite with `MemoryDiagnoser` in CI (fail if > 0 B allocated per event); no Godot types in Sim; batched event drain to Godot (one boundary crossing per frame) | Sim advance > 0.3 ms/frame in the benchmark scene, or any Gen2 GC during a 30-minute soak | LD |
| R2 | **Gambit interpreter**: correctness bugs, non-determinism, evaluation cost, and a UX that players cannot understand | 4 | 4 | 16 | Grammar frozen early (04 §4.12.1); parser → compiled closure-free bytecode (array of opcodes) evaluated by a switch loop; property-based tests (FsCheck) comparing interpreter vs reference evaluator; "dry-run" editor button; playtest target: 5 of 8 new players build a working deck unaided at M3 | Any gambit desync in arena validation; playtest target missed | LD, UX |
| R3 | **Async arena backend**: validation mismatches across OS/CPU, operating cost, cheating, and the small team's ops burden | 3 | 5 | 15 | Integer-only sim with banned-API analyzer; CI runs the golden-replay corpus (500 replays) on Windows x64, Linux x64 and macOS ARM64 every commit; single-stack C# backend (06 §6.6); managed Postgres/Redis; staging environment from S22; load test at 3× in S48 | Any cross-platform checkpoint mismatch in CI; validator p95 > 20 ms | BE, LD |
| R4 | **4K / isometric rendering performance**: Forward+ at 4K with SSR, glow, SSAO, volumetric fog and up to 60k particles misses 60 fps (and 120 fps in Performance mode) | 4 | 4 | 16 | FSR 2 presets (04 §4.3.3); LightmapGI instead of SDFGI; particle budgets with priority culling; mesh LOD and visibility ranges; monthly GPU captures (RenderDoc) against the frame budget table (04 §4.14.4); TA owns a perf dashboard | High preset below 60 fps on RTX 3070 in the benchmark arena for 2 consecutive sprints | TA |
| R5 | **Godot C# platform limits and churn**: Godot 4.x minor versions change APIs, .NET target versions move with Godot releases, C# has no web export, editor hot-reload of C# can be flaky | 3 | 3 | 9 | Pin Godot version per milestone (upgrade only at milestone boundaries after a 1-sprint spike); keep game code thin over `Resonance.Sim`; desktop-only target declared (06 §6.7); CI uses the Godot .NET headless build | A required fix exists only in a newer Godot version mid-milestone | LD |
| R6 | **Cross-platform determinism** (a specific case of R3): ARM64 vs x64, .NET runtime differences, unstable sorts | 2 | 5 | 10 | Stable sorts only; no floating point; checkpoint hashes every 1,000 ticks; determinism matrix owned by QA | Any mismatch | QA, LD |
| R7 | **Content scope**: 24 heroes × 3 abilities × VFX and animation, 12 bosses, 5 shafts overwhelm one artist | 5 | 4 | 20 | Outsource animation for 12 heroes; modular kit for arenas; shared VFX grammar per property (11 property sparks reused); scope-cut ladder (7.8) | TA velocity below 1 hero per sprint by S34 | TA, LD |
| R8 | **Balance combinatorics**: chain routes × heroes × chips produce degenerate arena metas | 4 | 3 | 12 | Headless batch simulator (uses Validator) runs 100,000 ghost-vs-ghost matches nightly in Beta; win-rate dashboards per hero; Normalized ladder | Any hero > 58% or < 42% win rate over 10,000 matches | LD |
| R9 | **Raid netcode complexity** | 3 | 4 | 12 | Epoch coupling (06 §6.5.3) keeps squads independent; ship 2-squad raids first; 3-squad raids can slip to post-launch | Raid prototype not stable by S26 | BE, LD |
| R10 | **HUD information overload** | 3 | 3 | 9 | Verbosity levels (04 §4.10.2); progressive tutorial unlocking widgets; UX-led playtests each milestone | Playtest SUS score < 68 | UX |
| R11 | **Key-person risk / burnout** (LD owns design and engineering) | 3 | 5 | 15 | 40-hour weeks planned (no crunch in schedule); documentation-first (this bible); BE contractor cross-trained on Sim; scope-cut ladder (7.8) used as contingency instead of overtime | LD overtime > 10% for 3 sprints | LD |
| R12 | **Steam Deck / platform compliance** | 2 | 3 | 6 | Deck testing from S29; controller-first UI paths in 04 §4.11.2 | Deck frame rate < 30 fps at 800p | TA, QA |

---

## 7.6 Quality and performance gates

| Gate | M2 Prototype | M3 Slice | M4 Alpha | M6 Beta | M7 Gold |
|---|---|---|---|---|---|
| Sim advance per frame (benchmark fight, 4× speed) | ≤ 0.5 ms | ≤ 0.3 ms | ≤ 0.3 ms | ≤ 0.3 ms | ≤ 0.3 ms |
| Allocations per sim event | ≤ 64 B | 0 B | 0 B | 0 B | 0 B |
| High preset 4K fps (RTX 3070 class) | n/a | ≥ 60 (slice arena) | ≥ 55 all arenas | ≥ 60 all arenas | ≥ 60 |
| Performance preset 4K fps (RTX 4070 class) | n/a | n/a | ≥ 100 | ≥ 120 | ≥ 120 |
| Validator p95 per match | n/a | n/a | ≤ 20 ms | ≤ 10 ms | ≤ 10 ms |
| Golden-replay determinism (3 platforms) | 1 replay | 50 replays | 300 replays | 500 replays | 500 replays |
| Crash-free sessions | n/a | 97% | 98.5% | 99.5% | 99.8% |
| Open A-severity bugs | any | ≤ 10 | ≤ 5 | 0 | 0 |

---

## 7.7 Recommended C# solution structure

```
ProjectResonance/
├─ ProjectResonance.sln
├─ Directory.Build.props             // LangVersion, Nullable enable, TreatWarningsAsErrors, analyzers
├─ BannedSymbols.txt                 // float/double/Math floating APIs banned in Resonance.Sim
├─ src/
│  ├─ Resonance.Sim/                 // net8.0 class library, NO Godot references
│  │  ├─ Core/        (SimClock, ApGauge, BattleState, UnitState, Pcg32, FixedMath)
│  │  ├─ Combat/      (TimelineSystem, ChantSystem, DamagePipeline, HitResolver, EnmitySystem,
│  │  │                ResonanceSystem, BurstSystem, StatusSystem, LoadoutSystem, RacialPassives)
│  │  ├─ Gambits/     (Lexer, Parser, Compiler → GambitProgram opcodes, GambitVM, DeckValidator)
│  │  ├─ Dungeon/     (RunGenerator, AetherDensitySystem, LootSystem, ChipRefinement)
│  │  ├─ Arena/       (GhostSnapshot, ReplayWriter/Reader, Glicko2, ProtocolScore)
│  │  ├─ Raid/        (WorldBossState, EpochCoupler, CouplingEvent)
│  │  └─ Data/        (HeroDef, AbilityDef, RaceDef, BossDef, ChipDef, ResonanceTable)
│  ├─ Resonance.Game/                // Godot project (Godot.NET.Sdk), references Resonance.Sim
│  │  ├─ project.godot
│  │  ├─ Resonance.Game.csproj
│  │  ├─ autoloads/   (SimBridge, GameState, SaveService, VfxPool, AudioDirector, NetClient,
│  │  │                ArenaService, RaidSession, Settings)
│  │  ├─ data/        (*.tres: HeroDefinition, AbilityDefinition, RaceDefinition, BossDefinition,
│  │  │                ChipDefinition, LegendaryDefinition, FloorDefinition resources)
│  │  ├─ scripts/     (Resources/*.cs [GlobalClass], DataCompiler.cs, presenters, UI controllers)
│  │  ├─ scenes/      (battle/, hud/, dungeon/, arena/, raid/, menus/)
│  │  ├─ render/      (shaders/, materials/, environments/)
│  │  ├─ vfx/         (particle scenes, flipbooks)
│  │  └─ ui/          (theme/, fonts/, icons/)
│  ├─ Resonance.Api/                 // ASP.NET Core 8 minimal APIs + SignalR hubs
│  ├─ Resonance.Validator/           // .NET 8 worker (Redis streams consumer)
│  └─ Resonance.RaidHost/            // .NET 8 SignalR raid room host
├─ tests/
│  ├─ Resonance.Sim.Tests/           // xUnit: formulas, golden log 02 §2.15, 72 ability tests
│  ├─ Resonance.Sim.Determinism/     // golden-replay corpus runner (CI on 3 platforms)
│  ├─ Resonance.Sim.Benchmarks/      // BenchmarkDotNet (MemoryDiagnoser)
│  └─ Resonance.Api.Tests/
├─ tools/
│  └─ BalanceRunner/                 // headless batch ghost-vs-ghost simulator
└─ docs/                             // this design bible
```

Autoload registration order in `project.godot`: `Settings`, `GameState`, `SaveService`, `SimBridge`, `VfxPool`, `AudioDirector`, `NetClient`, `ArenaService`, `RaidSession`.

---

## 7.8 Scope-cut recommendations (ordered ladder)

Cut from the top when the early warning triggers in 7.5 fire. Each rung lists the savings and what the game keeps.

| Rung | Cut | Saves (pw) | Keeps | Decision point | Owner |
|---|---|---|---|---|---|
| 1 | 3-squad raids → ship 2-squad raids only; 3-squad in a post-launch update | 4 | Alliance raids with core/shield split and Global Magic Burst | S26 | LD, BE |
| 2 | Live Assault mode → post-launch (keep Ghost Duel and Weekly Gauntlet) | 3 | Full async arena | S24 | LD, BE |
| 3 | Shafts 5 → 3 at launch (Hollow Choir, Sub-Hex Well, Cinder Throat); 2 as free updates | 10 | 45 floors of content, all bosses reused as elites | S34 | LD, TA |
| 4 | Barks 480 → 120 lines (1 per race pairing per hero) | 5 | Personality and chain callouts | S35 | AU, LD |
| 5 | Hero art: 24 unique rigs → 5 race base rigs with per-hero armor kits (all 24 heroes still ship) | 18 | All 24 heroes, 72 abilities | S31 | TA |
| 6 | Legendary Prototypes 12 → 8 at launch | 2 | Legendary chase with pity | S40 | LD |
| 7 | Volumetric fog, SSR and Ultra preset removed | 2 | High and Performance presets | S46 | TA |
| 8 | Localization 7 → 3 languages (EN, DE, ZH-Hans) at launch | Vendor cost | Global reach core markets | S41 | CM |
| — | **Never cut:** CTB sim, Resonance matrix, Magic Burst, dual enmity, 4-loadout swaps, Gambit Deck, Tactical Pause, Math Console, deterministic replays | — | The game's identity | — | LD |

---

## 7.9 Solo-developer variant

If the project runs with only the Lead (1.0 FTE) plus occasional contractors, the full scope is not realistic: 284 pw at 0.8 productivity is 355 weeks (6.8 years) before contingency, about 8.5 years with 25% contingency. The recommended solo scope:

| Area | Solo scope |
|---|---|
| Heroes | 24 in data and balance; 5 race base rigs + armor kits (rung 5) |
| Content | 3 shafts (rung 3), 6 floor bosses, no World Boss at launch |
| Multiplayer | Ghost Duel + Weekly Gauntlet only; raids post-launch |
| Art | Asset-store base meshes plus custom materials; outsourced animation packages |
| Audio | Licensed music library + contracted SFX pack; no barks |
| Estimate | About 120 pw productive: 120 / 0.8 = 150 weeks, × 1.25 contingency = 187.5 weeks, about 3.6 years |

Solo milestone dates (small-team milestone weeks × 1.736, the ratio 187.5 / 108):

| Milestone | Solo date |
|---|---|
| Concept | Nov 22, 2026 |
| Godot C# Prototype | Apr 17, 2027 |
| Vertical Slice | Dec 16, 2027 |
| Alpha | Oct 3, 2028 |
| Content Complete | Jul 21, 2029 |
| Beta | Feb 1, 2030 |
| Gold | Apr 15, 2030 |
| Launch | Tuesday, May 14, 2030 (first Tuesday after the computed May 9 date) |

---

## 7.10 Ceremonies, tooling and reporting

| Item | Cadence | Owner |
|---|---|---|
| Sprint planning | Monday of week 1, 90 min | LD |
| Async stand-up (text) | Daily | All |
| Sprint review + playable build | Friday of week 2 | LD |
| Retrospective | Friday of week 2, 30 min | LD |
| Milestone review (go/no-go against exit criteria) | End of each milestone | LD + all contractors |
| Risk register review | Every sprint review | LD |
| Perf capture report | Every 2 sprints from S9 | TA |
| Determinism report | Every sprint from S25 | QA |
| Tooling | Git + Git LFS for art; CI builds Godot .NET headless exports and runs Sim tests on 3 platforms; issue tracker with milestones mirroring M1–M8 | LD |
