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
| 37 | DoTs versus the golden excerpt | A scored encounter treats a pending part burn, shock, or hero burn as work, so a DoT can finish the Core or keep the fight alive until it pulses. The golden factory does not score an outcome, so Benediction regen still scheduled past tick 3,087 does not extend that log. Radiance is its own HoT and does keep the timeline alive; the golden party never has it | 02 §2.10.6, §2.15 |
| 38 | What ends the Carapace fight | Victory is Core HP 0. Shield and Weapon Arm are destroyable side targets and are skipped once down. No living hero, or the hard-enrage tick, is a defeat | 02 §2.9 |
| 39 | Frenzy and hard enrage numbers | Frenzy is +2,200 bp ATK and INT when Core HP is at or under 35%, or at tick 80,000. Hard enrage at tick 120,000 is a loss if the Core still stands. The 12-seed harness produced these knobs | Slice 4 |
| 40 | Tank filler and when Shield Bash fires | Korrith's filler is Attack, so he does not restart an open chain with Blunt. Shield Bash fires when the boss cast has under 700 ticks left, not for the whole chant | 04 §4.12.6 |
| 41 | Encounter loadouts versus the golden set | Golden heroes keep one combined GearMods and do not swap. Encounter heroes swap Idle, Fast Cast, Mid-Cast, and Weapon at the phase §2.13 names. Seraphine's encounter Mid-Cast set is +40 INT and +50% healing potency; the golden set stays at potency 0 | 02 §2.13, §2.15 |
| 42 | Tempest Crown haste and interrupt | +2,000 AP skips allies who are casting. The grant uses the refund ceiling and happens when the apex resolves, before that closer pays recovery, so a closer already at 10,000 AP loses it. The interrupt is 100% when that part is casting, draws no roll, and pays Stance recovery | 02 §2.10.6 |
| 43 | Raid shield coverage | The raid rule of −20% Core damage per shield node is not this fight. Threat stays on the per-part tables | 06, 02 §2.9 |
| 44 | Boss Burn and the earth cantrip | Kiln Vent (id 40) is a boss-only magical Fire chant that applies hero Burn. Earth Bolt (id 42) exists so tests can close Tectonic Shear. Neither is on a hero sheet | 02 §2.8.4, §2.10.6 |
| 45 | Default deck MP | Zeph casts only into a matching burst element, and opens with Blizzard II only at MP% ≥ 85. Mirrim pounces a piercing window and stalks only at MP% ≥ 50. Seraphine heals under 55% and 75%, refreshes Benediction while her MP% is at least 40, and casts Phase Sanctuary while the boss is casting and her MP% is at least 50 | 04 §4.12 |
| 46 | Per-part resistance versus the golden boss | A part with a null resist array uses the boss array, so §2.15 still sees Ice +10% and Fire +30%. Encounter parts set their own arrays. Burn and shock use the afflicted part's resist | 02 §2.15, Slice 5 |
| 47 | Kiln Guard | A boss beneficial. While it is present, boss physical damage is +1,000 bp, applied after the pipeline and before absorb. It reforms every 8,000 ticks if missing. Distortion's purge can remove it. The golden boss never has it | Slice 5 |
| 48 | Frenzy Plating | When frenzy starts, the boss gains Frenzy Plating: +800 bp DEF and MEVA while the beneficial remains. It does not reform. Distortion purges the newest beneficial first, so plating goes before an older Kiln Guard. The golden boss never has it | Slice 5 |
| 49 | Where a Level 3 is allowed to fire | Finishers stay inside decision 25. Shatter Choir casts Photon Sermon on an open Fragmentation only while Core HP is under 60%. Guardbreak casts Blizzard II on an open Distortion only while Core HP is under 34% and at least 28%. Both are non-burst closers, so the apex still opens its own burst window. Across the 12-seed harness that is 0–2 finishers per fight | 02 §2.10.2, Slice 5 |
| 50 | How chain variety is counted | A detonation is one call that applies an L2 or an L3 effect, not a combat-log substring. The harness cap is on L2 counts: in the best-performing preset, no single L2 is more than 60% of that preset's L2 detonations. Apex counts are reported separately | Slice 5 |
| 51 | Kits that the default decks do not spend a slot on | Needle Flicker, Solar Filament, and Ravel Execution are on the hero kits for a manual order. The default decks leave those slots for the route the preset is built to walk | 03, 04 §4.12 |
| 52 | Which seed the party select uses | Every preset starts on showcase seed 20261004, so the choice is the party and not a new roll. The harness still runs each preset on 20261004 through 20261015 | Slice 5 |
| 53 | All-parts weapon skills | Tectonic Stomp's all-parts hit is not implemented. No default preset fields Gorrun. AoE waits for a later slice | 03 |
| 54 | Ally MP% still reads the actor | Unchanged from Slice 3. A deck that needs another hero to spend a window uses a Core HP% gate, which the gambit VM can read without drawing RNG | 04 §4.12.8 |
| 55 | Shield Bash stun and diminishing returns | A landed Shield Bash stuns that part for 1,500 ticks. The part cannot start an action and cannot chant: the script skips it, a queued action pays Stance recovery and does not strike, and a chant already running on that part is cancelled the same way as a flat interrupt. A chant that would resolve while the part is still stunned fizzles. Repeats diminish: while the part is stunned, or inside the 4,000-tick window after a stun once immunity has ended, the next duration is half the previous one (integer). A result below 1 tick does not stun, clears that window, and grants 1,000 ticks of immunity. Immunity also follows every real stun (it starts when the stun ends) and lasts 1,000 ticks. A bash during immunity does not stun and does not refresh the timers. The 35% flat interrupt roll is unchanged | Owner ruling, 2026-10-03 |
| 56 | Execution Frame is crit damage, not crit chance | The approval said "bonus crit chance." The roster (03b Hero 18) and the combat engine (02 §2.7) say that below 35% HP on that part, crit damage is +40% additive and the crit-damage cap rises from 2.50× to 2.90×. Recommended answer: follow the bible. The threshold is strict (`hp * 100 < maxHp * 35`), so 35% itself does not qualify. Non-crits are unchanged | 03b, 02 §2.7 |
| 57 | Execution Frame scope | The bible describes the frame as a Kaelis passive on his attacks. The approved ruling names Ravel Execution. Recommended answer: apply it only to Ravel Execution. Crescent Sever and Umbral Pierce stay on the normal 2.50× cap until the owner asks for the full passive. Default decks still do not spend a slot on Ravel Execution (decision 51), so the harness does not see the frame | 03b, owner ruling |
| 58 | Which hero gets the absorb shield | Seraphine, not Korrith. Korrith already has Keratin Bastion. Seraphine is the Porcelain Choir buffer, and her sheet has no absorb. Choir Aegis (id 59) is a single-ally shield: 2.00× (INT + mid INT) absorb for 3,000 ticks, MP 80, chant 300, recovery 4,000, VE 200 / CE 100. A refresh raises the pool only when the new grant is larger, and it always restarts the timer. It does not stack. It is on her encounter kit (first slot, so selecting her shows it) and not on the default gambit or the golden queue. Absorb expiry keeps the timeline alive only while a pool is actually set; golden heroes never have one | Owner ruling, 03a Hero 10 |
| 59 | When the default decks bash and shield | Ice Lattice and Guardbreak bash the current target when that chant has under 200 ticks left, or when the boss is not chanting and its gauge is at least 9,000 AP. The part must not be stun-immune, and the chant line also requires that it is not already stunned. Shatter Choir uses the same shape with a shorter tail (under 80 ticks) and a later gauge (at least 9,300 AP), because the wider window wins every seed. Seraphine casts Choir Aegis on the lowest-HP ally under 70% when that ally has no aegis, then on the other ally with the most threat while her MP% is at least 35. The 75% cure stays in the 6-slot budget, so Always Attack is the line that moved; an unmatched turn still Attacks. Frenzy for this fight is +4,000 bp ATK and INT at 50% Core HP (decision 39 was +2,200 bp at 35%). A landed stun shorter than 1,500 ticks counts as lost to diminishing returns as well as landed. An immune or zero-duration bash counts only as lost. The golden queues are unchanged. Recommended answer: accept these gates | Owner request, 04 §4.12 |
| 60 | `CastResolvesIn` and which preset the 60% cap covers | `CastResolvesIn` compares the cast that is actually running. It was comparing the ability id to 0, so decision 40's "under 700 ticks" line never matched a real chant. The 60% cap stays decision 50: the best preset only. Ice Lattice and Guardbreak are one-route parties. Moving Ice's conduction gate up, or Guardbreak's burn gate up, either left Induration or Distortion above 60% or dropped that preset out of 7–9 wins. Liquefaction stays about one detonation per Guardbreak fight because Pyre Lattice costs 320 of Zeph's 540 MP. Recommended answer: keep the best-preset cap and require at least 5 distinct L2s across the 12 seeds | 04 §4.12, decision 50 |
| 61 | What VS-1 is, since the bible has no bestiary and rest is a full restore | One linear floor of Cinder Throat. Not the 5–7 node generator, and not ADM or loot. Rooms, in order: Cinder Mite (trash), Slag Skitter (trash), Kiln Warden (elite: Core plus Brand), Kiln Cool (rest), Carapace Engine Mk. II. Grey-box stand-ins, because 05 names room types and no creatures. Cinder Mite: ATK 140, DEF 80, INT 120, MEVA 60, ACC 180, EVA 40, AGI 10, concentration 0, Core 7,000 HP, Mite Spit then Attack. Slag Skitter: ATK 180, DEF 120, INT 40, MEVA 70, ACC 180, EVA 50, AGI 10, concentration 0, Core 9,000 HP, Skitter Lash. Kiln Warden: ATK 240, DEF 180, INT 160, MEVA 100, ACC 200, EVA 60, AGI 10, concentration 20, Core 22,000 HP, Brand 8,000 HP, Piston Sweep / Overpressure Lance / Piston Sweep. All three are weak to Fire (−10%) and resist Ice (+10%). Rest puts back 70% of missing HP and 99% of missing MP. A hero at 0 HP is raised to 70% HP and 99% MP, so one death is not an automatic floor loss. Only HP and MP carry. AP, buffs, absorb, and enmity reset, because each room builds a fresh party and then copies vitals. Fight RNG is the run seed plus the room index. The playable floor uses showcase seed 20261004. Boss only skips the floor and opens the existing party picker on that seed. Restart during an ongoing floor fight reloads the room from the vitals you entered with. Recommended answer: accept this slice | 05, 02 §2.3.5, VS-1 |
| 62 | Floor clear band versus the boss-only band | The floor harness is separate from the boss-only 7–9 wins. Each preset must clear 6–9 of seeds 20261004–20261015. This pass is Ice Lattice 6/12 (wipes 20261005, 20261007, 20261010, 20261011, 20261014, 20261015), Shatter Choir 8/12 (wipes 20261010, 20261012, 20261013, 20261014), Guardbreak 9/12 (wipes 20261005, 20261006, 20261015). Every wipe is on the Carapace Engine. Trash, the elite, and the rest do not end a run. The Carapace numbers, the golden log, and the stun rules are unchanged. The pre-boss bodies are short enough that the party arrives a little short, and Kiln Cool puts back 70% of missing HP and 99% of missing MP. Recommended answer: accept this band and these counts | VS-1, decision 50 |
| 63 | Which six heroes are the playable roster | The seven implemented kits are Korrith Vael-Dun (Veth-Kari, tank), Saeli Thorn-Vesper (Sylvari-Mor, linker), Mirrim Ash-Pounce (Sylvari-Mor, linker), Kaelis Moon-Ravel (Sylvari-Mor, spike), Zeph Tri-Lumen (Kith-Lir, spike), Aurel Nine-Vesper (Aethel-Born, spike), and Seraphine Vol-Ivory (Aethel-Born, healer). The playable six drop Mirrim. Korrith is the only tank and the only Veth-Kari. Seraphine is the only healer. Zeph is the only Kith-Lir. Saeli is the Wind closer Shatter Choir is built on; Kaelis has no Wind, and putting him in her slot collapsed that preset. The seven kits span four races, not three. Ash-Dravan has no hero among them, so all four implemented races stay: Veth-Kari, Sylvari-Mor (Saeli and Kaelis), Kith-Lir, and Aethel-Born (Aurel and Seraphine). Shatter Choir and Guardbreak keep their decks. Ice Lattice is reworked onto Saeli: she Needle Flickers while Core HP is at least 28%, then Rend Pulses. Zeph casts Blizzard II onto an open Piercing window only while Core HP is at least 80% and his MP% is at least 90 (one Induration), and Hex Lances while Core HP is under 40%, his MP% is at least 60, and no lightning burst is open (one Conduction, so the burst refund does not keep the cast going). Recommended answer: accept this six and this Ice Lattice rework | 03, 03a, 03b, VS-2 |
| 64 | How a party of four is chosen | Party select is the front door, before the floor and before Boss only. Six cards show name, role, race, and kit summary. The three preset buttons fill those four. Start requires exactly four distinct heroes. No tank, or no healer, is a warning and does not block. A four that matches a preset uses that preset's tuned decks. Any other four uses each hero's default gambit plan. Seraphine's default Benediction targets the ally with the lowest MP%, so it does not name a hero who is absent. Parties sort by roster id, so Korrith is slot 0 when he is present. Boss only starts that party on showcase seed 20261004 and does not open the old three-preset picker. Change party returns to this screen. Recommended answer: accept these gates | VS-2, decision 52 |
| 65 | What the 15-combo floor sweep is allowed to do | Presets stay in both bands. Boss only, seeds 20261004–20261015: Ice Lattice 9/12 (average 48,929, Induration 12, Distortion 19, Conduction 12), Shatter Choir 9/12 (average 42,158, Fragmentation 102, Distortion 152, Solar Apex 13), Guardbreak 7/12 (average 48,990, Liquefaction 12, Distortion 174, Umbral Zero 11). Shatter Choir is the best preset, so the 60% cap is its 59% Distortion share. Five distinct L2s. Floor: Ice Lattice 6/12 (wipes 20261005, 20261007, 20261008, 20261009, 20261013, 20261014), Shatter Choir 8/12 (wipes 20261010, 20261012, 20261013, 20261014), Guardbreak 9/12 (wipes 20261005, 20261006, 20261015). Every wipe is the Carapace. The other twelves, same seeds: Korrith/Saeli/Kaelis/Zeph 12/12, Korrith/Saeli/Kaelis/Aurel 12/12, Korrith/Saeli/Kaelis/Seraphine 12/12, Korrith/Saeli/Zeph/Aurel 0/12, Korrith/Kaelis/Zeph/Aurel 0/12, Korrith/Kaelis/Aurel/Seraphine 8/12, Korrith/Zeph/Aurel/Seraphine 0/12, Saeli/Kaelis/Zeph/Aurel 0/12, Saeli/Kaelis/Zeph/Seraphine 8/12, Saeli/Kaelis/Aurel/Seraphine 12/12, Saeli/Zeph/Aurel/Seraphine 0/12, Kaelis/Zeph/Aurel/Seraphine 0/12. A 0/12 or 12/12 is flagged and is not a failure. Presets stayed inside 7–9 boss wins and 6–9 floor clears, so the Carapace, the trash, the rest, and the golden log were not retuned. Recommended answer: accept the flags and leave the non-preset twelves as they are | VS-2, decision 50, decision 62 |
| 66 | Hunyuan3D-2 proof meshes must not ship | The Korrith and Carapace Engine glbs in `src/Resonance.Game/assets/models/proof` are Hunyuan3D-2 outputs under the Tencent Hunyuan 3D 2.0 Community License. They are a proof only and must not ship. The license does not cover the European Union, the United Kingdom, or South Korea. Outputs must not be used to improve other AI models. The MANIFEST in that folder keeps this note. Recommended answer: accept the proof and do not ship the glbs | assets/models/proof/MANIFEST.md |
| 67 | Clear-rate band for every pick-4 party | Decision 65 recorded the 15-combo floor sweep and left 0/12 and 12/12 as flags. This pass gives every four a target, because that decision did not write a band for the non-preset parties. The band is 25–75% clears on seeds 20261004–20261027 (24 seeds, so 6–18). A 0/24 or 24/24 is outside the band. The same parties on the original 12 seeds land in 3–9, which is that band on the shorter window. Preset boss wins stay 7–9 of 20261004–20261015 and preset floor clears stay 6–9 of those seeds (decisions 50, 60, and 62). Measured boss wins are still Ice Lattice 9/12, Shatter Choir 9/12, Guardbreak 7/12. Measured preset floor clears are still Ice Lattice 6/12, Shatter Choir 8/12, Guardbreak 9/12. The Carapace, trash, rest, golden log, and the accepted spec rulings are unchanged. Tuning is data: default gambits, plus physical and magical coefficients that the three preset decks do not cast. Hero ATK, the shared preset coefficients, and Solar Filament are unchanged, so decision 51 still holds for Solar Filament. The before/after table is in [docs/cinder-throat-balance.md](docs/cinder-throat-balance.md). Korrith is still the largest swing: parties with him average 13.0/24 and parties without him average 8.6/24. Zeph is next, 12.7/24 against 9.2/24. The coldest four is Saeli/Kaelis/Aurel/Seraphine at 6/24. The hottest non-preset fours are Korrith/Saeli/Kaelis/Zeph and Korrith/Saeli/Zeph/Aurel at 17/24. Guardbreak is 18/24, on the 75% line, and still 9/12 inside decision 62. Recommended answer: accept 25–75% on this 24-seed window | VS-2, decision 65 |
| 68 | Playtest build 0.1 | Outside players get a Windows x86_64 zip, `ProjectResonance-<version>-win64.zip`, built by Godot 4.7.2 .NET with a self-contained runtime embedded in the export. They unzip and run `ProjectResonance.exe`. No Godot install and no .NET install. Product name Project Resonance, version 0.1.0. The GLB models, HUD, and icons are in the export. Docs, tests, editor addons, and the debug battle scene are not. A Linux x86_64 preset is in the same export file. CI runs `dotnet test` on tag `v*` and on manual dispatch, exports Windows, and attaches the zip to that tag's GitHub Release. The game opens on a title screen (Play, Settings, Quit), then party select, the Cinder Throat floor, and a victory or defeat screen that returns to the title. Settings (window mode, four resolutions, 3D or grey-box, battle speed 1×/2×/4×) save to `user://settings.cfg`. Each finished or abandoned run writes JSON under `user://runs/` with version, seed, party, rooms cleared, result, duration, and the combat log. Unhandled exceptions write `user://crash/`. Boss only, Step, and To end stay on editor and debug builds and are absent from the release export. Ability buttons use the full name at a fixed 10px font with a flat style, so Blizzard II, Tri Spark, Pyre Lattice, and Hex Lance are not ellipsized, and the HUD rects stay fixed. The Hunyuan proof meshes for Korrith and the Carapace ship in this private zip only. Decision 66 still bars them from a public release. How to play, what to test, and the known leg notes are in [docs/playtest-0.1.md](docs/playtest-0.1.md). Recommended answer: accept this as the first external build | docs/playtest-0.1.md |

Decisions 26–36 are the Slice 3 rulings. The owner accepted them on 2026-10-03 exactly as built.

Decisions 37–45 are the Slice 4 rulings. The owner accepted them on 2026-10-03 exactly as built.

Decisions 55–58 are the ability-gap rulings. The owner accepted them on 2026-10-03 exactly as recommended. Decision 59 is the follow-up that puts those tools on the auto-run decks. The owner accepted decisions 59–60 on 2026-10-04 exactly as recommended.

Decisions 61–62 are the floor rulings. The owner accepted them on 2026-10-04 exactly as recommended.

Decisions 63–65 are the roster rulings. The owner accepted them on 2026-10-04 exactly as recommended.

Decision 67 is the pick-4 clear-rate band. It does not rewrite decision 65.

Decision 68 is Playtest build 0.1. It does not rewrite decision 66 for a public release.
