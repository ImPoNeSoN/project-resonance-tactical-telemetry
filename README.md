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
| 17 | Magic Burst vs the chain step (pilot ambiguity 1) | A spell that qualifies as a Magic Burst deals that burst damage against the window open at resolution, then applies its property as the next chain step. A valid transition still detonates. Whether it opens the next window is decision 25 | 02 §2.10.2, §2.11.2 |
| 18 | Unpublished dice in the worked log (pilot ambiguity 2) | The §2.15 fight is PCG-XSH-RR seed `20261002`. Every roll that fight draws is printed. A Magic Burst draws no hit roll. A 0% interrupt draws no interrupt roll. `ScriptedRng` replays a recorded list only when a future excerpt omits a roll | 02 §2.15.1 |
| 19 | Which enmity table sheds (pilot ambiguity 3) | CE sheds from the table that selected the target. An empty part table falls back to the Core table, and the shed lands on Core | 02 §2.9.4 |
| 20 | Regen clock and Benediction Concentration (pilot ambiguity 4) | Each regen pulses every 500 ticks from its own start, not from `tick % 500`. Circuit Benediction's +20 Concentration lasts the full regen and expires on the same tick | 02 §2.2.3 |
| 21 | Shelter of the Chanters (pilot ambiguity 5) | Kept as implemented. Slot 0 is the Core-table argmax and at least two other heroes are casting: that anchor's DEF ×1.25 and that anchor's flat CE ×1.30, after Enmity+ and before Aethel ×0.60. Core table only | 02 §2.12.6 |
| 22 | Magma Core and Tempest Crown “+110% Burst” (pilot ambiguity 6) | Every L3 finisher bonus is true damage outside BurstBucket. Those two cells are +110% of the burst's step-9 damage as true damage, not a BurstBucket term | 02 §2.10.4, §2.11.4 |
| 23 | Integer rounding on odd inputs (pilot ambiguity 7) | Division truncates toward zero everywhere a formula does not say ceil. Chant time and Heat gain still ceil | 02 §2.16.1 |
| 24 | Damage product wider than `long` (pilot ambiguity 8) | The pipeline keeps `BigInteger` and divides by `10000^n` once. That width is a known performance risk for a later zero-alloc pass. It is not a float | 02 §2.16.1 |
| 25 | Burst closers were refreshing Apex forever | Accepted. A matching burst that closes a chain still detonates and still takes its burst bonuses, but it opens no new resonance window and no new Magic Burst window. The chain becomes Empty. The window it qualified in stays open, Kith-Lir extension included. A non-burst closer still opens both windows. The next Apex is rebuilt from L1 by actions that are not bursts | 02 §2.10.2, §2.11.2 |
| 26 | Gambit text is looser than the EBNF | Leading slot numbers (`1`, `1.`, `1)`) and `//` comments are ignored. `CAST` and `USE` are synonyms. `WAIT` is written `WAIT [Guard]` | 04 §4.12.1, §4.12.8 |
| 27 | `Boss` with no part | `Casting` reads the boss unit. Part stats read the Core | 04 §4.12.8 |
| 28 | Percent predicates | HP% and MP% truncate (`current * 100 / max`). `CountBelowHP%` counts every hero, including the actor, strictly below N% | 04 §4.12.8 |
| 29 | Shield Bash interrupt before chip rarity exists | The chip grant is decision 14, not 11. Compile and runtime both require the Bulwark Bash grant. The flat interrupt is the common-rarity 35% until sockets and rarity exist, and only when the hit lands on the part that is casting | 05 §5.6.4, 04 §4.12.8 |
| 30 | Conditions whose world systems are not in the sim | `Aquifer` is false until Tollen's resource exists. Global burst and Aether Density are integer fields. Raids and dungeons are not simulated | 04 §4.12.2, §4.12.8 |
| 31 | `ChainParticipants` | Population count of the part's contributor bitmask. L1 replaces it with the opener's slot bit. L2 ORs the closer. Clear and expiry zero it | 04 §4.12.8 |
| 32 | DEFER timing | The matching tick does not act. Later ticks re-evaluate while `tick < DeferUntil`. At expiry, DEFER slots are skipped. Matching DEFER again does not reset N. AP while deferring is capped at 12,000. No recovery is paid | 04 §4.12.3, §4.12.8 |
| 33 | `SpendHeat` and `WAIT` | `SpendHeat` spends 20 Heat for any hero with the flag and at least 20 Heat. `WAIT` pays Stance recovery and grants −20% damage taken until the next action. That reduction blocks CE shed | 04 §4.12.3, §4.12.8 |
| 34 | Manual play versus gambits, and pause | A queued command beats the deck. The command stream is the replay: stamp 0 before any tick, otherwise T+1 after tick T. Drain applies due commands at the start of the tick. Pause stops between ticks. Step advances one tick and leaves pause set | 04 §4.11.1, §4.12.4, §4.12.8 |
| 35 | Extra-hit dice | A failed multi-attack check draws no extra hit or crit roll. An extra hit rolls crit only if that extra hit lands. Damage is 50% of the primary's pre-crit dealt damage, then that extra's crit multiplier | 02 §2.5.4 |
| 36 | Where the passive amounts live | Saeli's 1,800 cadence and Thurga's Heat remainder of 30 are hero fields. Other Sylvari-Mor stay at 1,200 and other Ash-Dravan Heat resets to 0. Racial +75 EPEN is added when resistance is resolved and stacks with the EPEN field. Ash-Dravan Burn immunity is the hero pulse, not the boss Burn | 01 §1.4, §1.6.4, 02 §2.12 |
