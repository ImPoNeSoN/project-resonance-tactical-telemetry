# PROJECT RESONANCE: TACTICAL TELEMETRY

**Game Design & Technical Specification Bible**, version 1.0 (September 29, 2026)

Genre: tactical turn-based party RPG (Conditional Time Battle) · Engine: **Godot 4.x, C# only** · Platforms: Windows, macOS, Linux, Steam Deck · Modes: single-player dungeon delves, asynchronous Ghost Protocol arena, co-op Alliance Raids

---

## 1. Pitch

> In a world where magic is physics, every strike is a frequency. Hit a monster with the right two frequencies in the right four seconds and its matter tears itself apart.

*Project Resonance: Tactical Telemetry* is a tactical party RPG built around a readable, deterministic combat simulation. Squads of 4–6 heroes drawn from five biologically distinct races fight on a 10,000-AP Conditional Time Battle timeline. Each ability imprints a **chain property** on its target; the right sequence of properties produces **Resonance Detonations** (Level 2) and **Apex Detonations** (Level 3), which open **Magic Burst** windows for casters whose chants are timed to resolve inside them. Tanks juggle **dual enmity** (spiking Volatile and permanent Cumulative), heroes swap between **four gear loadouts inside a single action**, and every calculation streams live to a **Combat Math Console**.

The game is played three ways:

1. **Resonance Shafts**: roguelite dungeon delves where rising **Aether Density** makes enemies frenzied and loot richer, with an **Extract or Delve Deeper** decision every third floor.
2. **Ghost Protocol**: an asynchronous arena where players upload squads with their **Gambit Decks** (visual IF → THEN logic) and are ranked by the fewest ticks needed to clear opponents with zero casualties. Every result is re-simulated on the server from a seed.
3. **Alliance Raids**: 2–3 squads fight decoupled phases of a World Boss at the same time. One squad's Apex chain triggers a **Global Magic Burst** for everyone.

### 1.1 Design pillars

| Pillar | Meaning | Where it lives |
|---|---|---|
| Legible math | Every number is inspectable in under a second | 02, 04 §4.10 |
| Timing is skill | Chants are committed before windows open; the skill is prediction | 02 §2.3, §2.11 |
| Roles with teeth | Tanks manage two enmity types; linkers build chains; casters cash them in | 02 §2.9, 03 |
| Programmable squads | Gambits turn tactics into shareable, rankable logic | 04 §4.12, 06 |
| Deterministic core | One integer-only simulation for client, validator and raid host | 02 §2.16, 06 §6.3 |

### 1.2 Core loop

```
Build squad (24 heroes, 5 races, 6 archetypes)
  → Tune 4 loadouts per hero + Schematic Chips + Gambit Deck
    → Delve a Resonance Shaft (Aether Density rises; Extract or Delve every 3rd floor)
      → Earn gear, chips, Legendary Prototypes
        → Refine chips, upload a Ghost, join Alliance Raids
          → Rebuild squad
```

---

## 2. Module index

| # | File | Contents |
|---|---|---|
| 1 | [docs/01-world-and-races.md](docs/01-world-and-races.md) | World "Aetheric Harmonics" (magic as physics), history, factions, geography; the 5 races with biology, silhouette, stat modifiers, signature passives with exact formulas |
| 2 | [docs/02-combat-engine.md](docs/02-combat-engine.md) | CTB timeline, chanting and interrupts, hit, physical, magical, crit, elemental and true damage, dual enmity, Resonance matrix L1–L3, Magic Burst, racial passive rules, 4-loadout gear swapping, status table, **full worked combat log**, sim implementation notes |
| 3 | [docs/03-hero-roster.md](docs/03-hero-roster.md) | Roster index: stat derivation, race × archetype distribution table, master stat table, 72-ability index, chain property coverage |
| 3a | [docs/03a-hero-roster-01-12.md](docs/03a-hero-roster-01-12.md) | Full sheets for Heroes 01–12 (Anchor Tanks, Physical Linkers, Buffer/Aura Supports) |
| 3b | [docs/03b-hero-roster-13-24.md](docs/03b-hero-roster-13-24.md) | Full sheets for Heroes 13–24 (Debuffer/Disrupters, Spike DPS, Resource Batteries) |
| 4 | [docs/04-hud-and-visual-engine.md](docs/04-hud-and-visual-engine.md) | 35° isometric camera, PBR material library, WorldEnvironment, chain shakes, directional particles, HUD (timeline, boss anatomy, oscilloscope, math console), controls, **Gambit Deck grammar and engine**, Godot scene trees, performance budget |
| 5 | [docs/05-dungeon-progression.md](docs/05-dungeon-progression.md) | Run structure, Aether Density Meter (rise rates, fallout, frenzy scaling), Extract vs Delve, wipe rules, Legendary Prototype drop rates, Schematic Chips and refinement tables |
| 6 | [docs/06-multiplayer-raids-arena.md](docs/06-multiplayer-raids-arena.md) | Ghost Protocol scoring, Glicko-2 rating, anti-cheese, deterministic replay/validation, Alliance Raids and Global Magic Burst, epoch netcode, backend recommendation |
| 7 | [docs/07-production-plan.md](docs/07-production-plan.md) | Milestones Concept → Launch, 55 two-week sprints with owners, effort estimates, risk register, quality gates, C# solution structure, scope-cut ladder, solo variant |

---

## 3. The roster at a glance

| # | Hero | Race | Archetype |
|---|---|---|---|
| 01 | Korrith Vael-Dun | Veth-Kari | Anchor Tank |
| 02 | Drusk Oma-Teth | Veth-Kari | Anchor Tank |
| 03 | Varra Kesh-Ember | Ash-Dravan | Anchor Tank |
| 04 | Tessel Primewright | Kith-Lir | Anchor Tank |
| 05 | Saeli Thorn-Vesper | Sylvari-Mor | Physical Linker |
| 06 | Mirrim Ash-Pounce | Sylvari-Mor | Physical Linker |
| 07 | Gorrun Delve-Mace | Veth-Kari | Physical Linker |
| 08 | Ishka Pyre-Lash | Ash-Dravan | Physical Linker |
| 09 | Lyr Aurelis-7 | Aethel-Born | Buffer/Aura Support |
| 10 | Seraphine Vol-Ivory | Aethel-Born | Buffer/Aura Support |
| 11 | Pim Quadrant-Oss | Kith-Lir | Buffer/Aura Support |
| 12 | Hald Brek-Sorrow | Veth-Kari | Buffer/Aura Support |
| 13 | Nyx Sub-Vector | Kith-Lir | Debuffer/Disrupter |
| 14 | Vessa Quiet-Claw | Sylvari-Mor | Debuffer/Disrupter |
| 15 | Ondrel Vey-Static | Aethel-Born | Debuffer/Disrupter |
| 16 | Crag Moor-Ash | Ash-Dravan | Debuffer/Disrupter |
| 17 | Zeph Tri-Lumen | Kith-Lir | Spike DPS |
| 18 | Kaelis Moon-Ravel | Sylvari-Mor | Spike DPS |
| 19 | Thurga Ember-Maw | Ash-Dravan | Spike DPS |
| 20 | Aurel Nine-Vesper | Aethel-Born | Spike DPS |
| 21 | Ilune Cache-Aria | Aethel-Born | Resource Battery |
| 22 | Mox Relay-Pell | Kith-Lir | Resource Battery |
| 23 | Brannoch Ash-Well | Ash-Dravan | Resource Battery |
| 24 | Tollen Geode-Vast | Veth-Kari | Resource Battery |

---

## 4. Godot C# stack notes

### 4.1 Engine and runtime

- **Godot 4.x, .NET build** (the C#-enabled editor). The project pins one Godot 4.x release per milestone and upgrades only at milestone boundaries after a one-sprint spike (07 §7.5 R5).
- **.NET 8 SDK**; all projects target `net8.0`. `Nullable` enabled, warnings as errors.
- **No GDScript** in the shipping project. Shaders are Godot shading language (`.gdshader`), which is not a scripting language and is permitted.
- Export targets: Windows x64, Linux x64 (Steam Deck), macOS universal. Godot 4.x does not support web export for C# projects, so there is no browser build.

### 4.2 Architecture in one paragraph

All game rules live in **`Resonance.Sim`**, a plain C# class library with **no Godot references**, using only integer and basis-point math and seeded PCG32 RNG. The Godot project (`Resonance.Game`) wraps it: designers author **Resources** (`[GlobalClass]` `HeroDefinition`, `AbilityDefinition`, `ChipDefinition`, and the other data types) in the editor, a `DataCompiler` converts them into immutable sim records, an **Autoload** `SimBridge` advances the simulation and drains a ring buffer of `CombatEvent` structs once per frame, and presentation **scenes** (battle, HUD, dungeon, arena, raid) react to batched events. The same `Resonance.Sim.dll` runs in the headless arena validator and the raid host (ASP.NET Core 8), which is what makes async arena results verifiable.

### 4.3 Godot-native patterns used

| Pattern | Use in this project |
|---|---|
| Scenes | `BattleScene`, `HeroActor`, `BossActor`, `Hud`, `TimelineBar`, `PartRow`, `GambitSlotRow`, dungeon rooms, arena and raid lobbies (scene trees in 04 §4.14) |
| Nodes | `Camera3D` (orthographic, 35°), `GPUParticles3D` pools, `Label3D` damage numbers, `Control` HUD widgets, `CanvasLayer` HUD root |
| Signals | Designer-facing hookups (`[Signal]` delegates on UI scenes); high-frequency sim traffic uses C# `event`s to avoid Variant marshaling |
| Resources | All static data (heroes, abilities, races, bosses, chips, legendaries, floors), themes, materials, environments |
| Autoloads | `Settings`, `GameState`, `SaveService`, `SimBridge`, `VfxPool`, `AudioDirector`, `NetClient`, `ArenaService`, `RaidSession` |
| Shaders | Four PBR master shaders (carbon, brass, circuit, crystal), oscilloscope `canvas_item` shader, pause desaturation |
| WorldEnvironment | `env_resonance_combat.tres` (Forward+, glow, SSAO, LightmapGI, filmic tonemap) |

### 4.4 Solution layout (summary)

```
src/Resonance.Sim         pure C# rules library (net8.0)
src/Resonance.Game        Godot 4.x .NET project
src/Resonance.Api         ASP.NET Core 8 REST + SignalR
src/Resonance.Validator   headless arena re-simulation worker
src/Resonance.RaidHost    raid room host
tests/*                   xUnit, determinism corpus, BenchmarkDotNet
tools/BalanceRunner       batch ghost-vs-ghost simulation
```

Full tree: 07 §7.7.

### 4.5 Build and CI

- CI builds the solution with `dotnet build`, runs `Resonance.Sim.Tests` (including the golden reproduction of the 02 §2.15 combat log) and the determinism corpus on Windows x64, Linux x64 and macOS ARM64, then exports the Godot project headlessly with the .NET export templates.
- A banned-API analyzer prevents floating-point types in `Resonance.Sim`.
- BenchmarkDotNet with `MemoryDiagnoser` fails CI if a sim event allocates.

---

## 5. Conventions used in these documents

- **Ticks** are the simulation's time unit. At AGI 10, a unit acts once per 1,000 ticks. Presentation default: 1,000 ticks per second at 1× speed.
- **AP** always means Action Points on the CTB gauge (0–10,000). Arena score is called **Protocol Score** to avoid confusion.
- **VE / CE** are Volatile Enmity and Cumulative Enmity.
- **L1 / L2 / L3** are chain levels: a single property, a Resonance Detonation, an Apex Detonation.
- Percentages written as "+15%" on stats are multiplicative on the base stat unless the rule says "percentage points."
- Section references are written like "02 §2.9.4" (document 02, section 2.9.4).

---

## 6. Design decisions that reconcile gaps in the brief

| # | Gap or tension | Resolution | Reference |
|---|---|---|---|
| 1 | STR/DEX appear in the Weapon Skill loadout but the stat array uses ATK | STR/DEX are gear-only sub-stats added to ATK by damage property (Blunt/Slashing: +STR +0.5 DEX; Piercing: +DEX +0.5 STR; Elemental-Physical: +0.5 each); DEX also gives ACC and crit | 02 §2.1.3 |
| 2 | Kith-Lir 65% refund vs standard 50% burst refund | Not additive: 65% replaces 50%; chip refunds add on top; total cap 80% | 02 §2.11.5 |
| 3 | Kith-Lir window extension could chain forever | +600 per Kith-Lir burst, cap +1,200 per window (max 2,700 ticks) | 02 §2.11.5 |
| 4 | Thermal Battery Heat gain undefined | `max(3, ceil(300 × ElementalDamageTaken / MaxHP))`, ×1.5 for Fire, −5 per 1,000 idle ticks; Primed forces Liquefaction when the weapon skill's own property would not detonate | 02 §2.12.4 |
| 5 | CE shed formula undefined | `CE × min(0.25, 0.5 × Damage / MaxHP)` on direct unmitigated hits; "unmitigated" means no evade, no shield absorption, no typed damage-reduction buff (DEF, MEVA and gear DT− do not count) | 02 §2.9.4 |
| 6 | Resonance matrix incomplete for a 3-step system | Kept the 5 user resonances exactly; added routes (Earth → Fire, Water → Ice, Water → Darkness), 3 new L2 (Conduction, Tectonic Shear, Radiance) and 3 new L3 (Umbral Zero, Magma Core, Tempest Crown) alongside Solar Apex; unlisted transitions restart the chain | 02 §2.10 |
| 7 | "Slashing → Wind/Piercing" and "Blunt → Darkness/Slashing" | Read as two routes each (Slashing → Wind, Slashing → Piercing; Blunt → Darkness, Blunt → Slashing) | 02 §2.10.3 |
| 8 | "+X% Burst" meaning | The listed percentage is the Magic Burst bonus for matching spells in the window; detonations separately deal 50% (L2) or 100% true (L3) of the closing hit | 02 §2.10.5, §2.11.4 |
| 9 | AGI scale vs 4,000-tick windows | AGI 9–18 for heroes, so a Standard action takes ~550–1,110 ticks; windows span 4–7 actions and bursts 1.5–2.7 | 02 §2.2.4 |
| 10 | Heavy recovery can push AP below 0 | AP debt allowed down to −4,000; refunds capped at 10,000 | 02 §2.2.2 |
| 11 | Archetypes that mix weapon users and casters | Focus profiles (Physical / Arcane / Hybrid) swap only the ATK and INT baselines | 03 §3.1.2 |
| 12 | 24 heroes over 5 races | 5/5/5/5/4 split with Sylvari-Mor at 4 because Cadence Surge is the strongest tempo passive | 03 §3.2 |
| 13 | Boss sub-targets vs a single argmax | Per-part enmity tables; each boss ability targets from its own part's table, falling back to the Core table | 02 §2.9.1 |
| 14 | "Shield Bash" in the gambit example is not a hero ability | Granted by the Bulwark Bash Schematic Chip, usable by any hero | 05 §5.6.4 |
| 15 | Wipe loses 50% of yields: rounding for items | Currencies floor(50%); items keep ceil(n/2) chosen by the player | 05 §5.3.3 |
| 16 | Tactical Pause in real-time raids | Per-squad pause budget of 90 s per phase, then slow-motion; bounded clock skew between squads | 06 §6.5.4 |
