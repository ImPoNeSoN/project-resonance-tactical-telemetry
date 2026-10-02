# Pilot sprint report — combat-math core

Date: 2026-10-01 for the first pilot, 2026-10-02 for the resonance slice below. Scope of the first section: the 2-week pilot described in the task brief, not milestones M1–M8 in `docs/07-production-plan.md`.

Source of truth used: `README.md` §6, `docs/02-combat-engine.md` (formulas and the §2.15 worked log), `docs/01-world-and-races.md` and `docs/03-hero-roster.md` (plus 03a/03b) for the four heroes in that log. The first pilot treated the worked log as the numeric acceptance test where it disagreed with the prose. The owner has since settled those disagreements. The rulings are in README decisions 17–24, in `docs/02`, and in the resolved list below. The resonance slice regenerated §2.15 from the sim.

## What was built

A Godot 4.7 C# solution at the repo root, laid out like `docs/07-production-plan.md` §7.7:

| Project | Role |
|---|---|
| `src/Resonance.Sim` | `net8.0` class library. No Godot reference. Integer-only: `BannedApiAnalyzers` rejects `float` and `double`. |
| `tests/Resonance.Sim.Tests` | xUnit. Formula clamps plus a golden replay of §2.15. |
| `src/Resonance.Game` | `Godot.NET.Sdk` 4.7.0. `project.godot`, autoload `SimBridge`, scene `scenes/battle/DebugBattle.tscn`. |
| `ProjectResonance.sln` | Builds all three. |

`Resonance.Sim` contains:

- CTB gauge in centi-AP (100 centi = 1 AP, ready at 1,000,000). Per tick, AP gains `AGI × 100` scaled by haste/slow (±50% cap). Act at AP ≥ 10,000. Recovery: Standard 10,000 / Stance 4,000 / Heavy 14,000. Debt floor −4,000 AP. Refunds stop at 10,000. Opening AP is `min(6,000, AGI × 300)`.
- Chant time `ceil(CT × (1 − FC))` with FC gear cap 50% and total cap 65%. MP is spent when the chant starts. While casting, AP is frozen. Interrupt chance is `clamp((Damage / MaxHP) × 2.5 − Concentration × 0.01, 0, 0.95)`, in basis points.
- Hit% `clamp(0.75 + (ACC − EVA) / 200, 0.05, 0.95)`. Physical DR `DEF / (DEF + 500)`. Magical DR uses 600. Physical damage `ATK × Mult × (1 − DR)`, with the magic, crit, resistance, burst, and true-damage factors from §2. The product is divided by `10000^n` once, using `BigInteger`, so the floor happens once.
- Dual VE/CE tables, 30,000 cap, normal decay `floor(VE × 0.90)` every 500 ticks, Aethel heavy bucket `floor(VE × 0.80)`, CE shed `floor(CE × min(0.25, 0.5 × Damage / MaxHP))`. Boss target is argmax of `VE + CE` (tie: higher CE, then lower slot). Empty part tables fall back to Core.
- Seeded PCG-XSH-RR with separate streams (`Hit`, `Crit`, `MultiAttack`, `Interrupt`, `Proc`, `Ai`). The golden test uses a scripted stream because §2.15 does not publish a seed.
- Static L2 (15 routes) and L3 (8 routes) tables. L2 detonation is 50% of the closing hit. L3 and Distortion bonuses are true damage, matching §2.11.4 and README decision 8.
- The §2.15 party: Korrith, Mirrim, Zeph, Seraphine versus Carapace Engine Mk. II, with the published loadouts and scripted action queues.

The Godot debug scene is plain `Control` nodes: one timeline bar per hero, boss HP/chain/burst text, Step / Auto-run / Reset, and a log panel fed by `SimBridge` signals. The sim types never enter the scene script except as data.

## What was not built

This is a math skeleton for one worked encounter, not the M2 prototype.

- Gambit parser, Tactical Pause, and the other autoloads in §7.7 (`Settings`, `GameState`, `SaveService`, `VfxPool`, `AudioDirector`, `NetClient`, `ArenaService`, `RaidSession`).
- The other 60 abilities, the other 20 heroes, dungeon generation, ADM, loot, raids, and the API/validator.
- Zero-allocation hot path and the ring buffer. The sim uses `List`, strings, and a `bool[]` for alive flags.
- Full shield, DoT, and status simulation. Circuit Benediction schedules a regen pulse; Zeph is at full HP in the log, so it does not change the golden numbers.
- Re-deriving every hero in `docs/03` from gear. Only the four §2.15 actors are instantiated.
- Thermal Battery inside the battle loop. The first pilot left `HeatGain` and the Primed-vs-Liquefaction choice as pure functions. The resonance slice wires them into `BattleSimulator`.
- Double/triple attack extra hits. The rolls are consumed (or skipped when the chance is 0). Extra-hit damage is not applied. Every golden multi-attack roll fails, so the published HP is unchanged.
- Art, the HUD in `docs/04`, CI, and a BenchmarkDotNet gate.

## Test results

Command, from the repo root, with the .NET 8 SDK:

```
dotnet build ProjectResonance.sln
dotnet test ProjectResonance.sln
```

Build: 0 warnings, 0 errors.

Tests at the end of the first pilot: 36 passed, 0 failed (about 30 ms). That count was 33 formula facts/theory rows plus 3 golden tests against the pre-ruling log (Core 111683, Weapon Arm CE 1056, second Blizzard 1924). The resonance slice replaced that golden state. The current numbers and the current test count are in the slice section below.

Godot 4.7-stable mono, headless, `--path src/Resonance.Game`:

- `--import --quit` completed with no script errors.
- A scene run loaded the `SimBridge` autoload and `DebugBattle`. `_Ready` built four hero cards and the Carapace placeholder (Core 120000, Arm 45000, empty chain). One `Step()` advanced the sim to tick 256 and wrote the opening log lines. Temporary prints used for that check were removed afterward. There is no display on this machine, so the buttons were not clicked by hand; the same `Step()` path is what the Step button calls.

## Spec ambiguities — resolved 2026-10-02

The owner settled all eight. Each ruling is in `docs/02` and in README §6. None of them is still open.

1. **Magic Burst vs chain application. Resolved (decision 17).** A matching-element spell inside a Magic Burst window deals burst damage and then applies its property as the next chain step. The resonance slice already implemented that rule. Tick 1,170 restarts the chain at L1(Ice). Tick 2,639 closes Piercing → Induration and replaces the window.

2. **Rolls and the PCG seed. Resolved (decision 18).** The golden fight uses PCG-XSH-RR seed `20261002`. The regenerated §2.15 log prints every roll that fight draws and names the seed. A Magic Burst draws no hit roll (`no roll`). A 0% interrupt draws no interrupt roll. `ScriptedRng` stays as the replay tool: the golden test records the seed's rolls and replays that list to the same end state. It is not used to invent fillers for rolls the log omitted.

3. **Which enmity table sheds. Resolved (decision 19).** CE sheds from the table that selected the target, falling back to the Core table when the part's table is empty. The Weapon Arm piston at tick 534 still sheds 32 Core CE.

4. **Regen timing. Resolved (decision 20).** Pulses fire every 500 ticks from the effect's own start. Circuit Benediction's +20 Concentration lasts the full regen and expires on the same tick (4,690 in the golden fight; next pulse 1,190).

5. **Shelter of the Chanters. Resolved (decision 21).** Kept as implemented and written in §2.12.6 and in `docs/03a`: slot 0 is the Core-table argmax and at least two other heroes are casting, so that anchor's DEF ×1.25 and that anchor's flat CE ×1.30, after Enmity+ and before Aethel ×0.60.

6. **L3 wording. Resolved (decision 22).** Every L3 finisher bonus is true damage outside BurstBucket. The Magma Core and Tempest Crown cells in §2.10.4 now say “+110% of burst damage added as True Damage (not BurstBucket)”, matching §2.11.4. `Level3_bonus_is_true_damage_outside_the_burst_bucket` checks both profiles (bucket 0, true 11,000) and a power-1,000 hit with +20% MBD: dealt 1,200, true 1,320. Folding +110% into the bucket yields 2,300, which is not the same total.

7. **Integer rounding. Resolved (decision 23).** Division truncates toward zero everywhere a formula does not say ceil. Odd-input tests cover magic accuracy `(5+2)/2 = 3`, piercing STR 1 + DEX 51 → 51, blunt DEX 1 → 0, and elemental-physical `(1+2)/2 = 1`. Chant time and Heat gain still ceil.

8. **Damage accumulator width. Resolved (decision 24), with a known performance risk.** `BigInteger` stays. Ten basis-point factors overflow a fixed-width product before the single floor, so the pipeline multiplies in `BigInteger` and divides by `10000^n` once. That allocation is a performance risk to remove in a later zero-alloc pass. It is not a float, and this slice does not switch the pipeline back to `long`.

## Time

Wall clock for this agent, from task receipt at 2026-10-01 02:36 UTC through the headless scene check at about 03:05 UTC: **about 30 minutes**. That includes reading the combat chapter, writing the library, fixing the damage-product overflow, matching the golden log, and importing the Godot project. It does not include a human review pass.

A human C# developer who has already read docs 01–03, working only on this pilot (not gambits, not the 16 person-weeks in §7.4 for the full sim):

| Task | Human | This agent |
|---|---|---|
| Godot 4.7 C# solution and the §7.7 folders | 0.5–1 day | ~15 minutes, inside the same session |
| Formula library, fixed-point gauge, clamps, xUnit | 2–3 days | same session |
| CTB scheduler, chants, recovery, debt, scripted loadouts | 3–4 days | same session |
| Enmity, resonance, burst, and making §2.15 match exactly | 3–5 days | same session; most of the risk was here |
| Grey-box debug scene (`SimBridge` + Control nodes) | 1–1.5 days | ~10 minutes |
| This report | 0.5 day | ~15 minutes |

**Human total for this pilot: about 11–16 focused person-days (roughly two to three weeks).** The production plan’s S3–S8 window is longer because it also includes gambits, a zero-alloc hot path, and a playable grey-box battle, which this sprint did not attempt.

The agent was faster at transcription of formulas that are already numeric. The parts the first pilot left implicit — unpublished dice, which table sheds, and whether a burst closes a chain — are the rulings in the section above.

## Resonance chain and Magic Burst slice

Date: 2026-10-02. This slice starts from the merged combat-math core (PR #1, `0cb3b84`) and finishes docs/02 §§2.10–2.12 plus the race passives in docs/01 that touch that system. The worked log was regenerated from the sim under seed `20261002`, not edited by hand.

### What this slice added

- The full L2 and L3 matrix as data, 4,000-tick chain windows, and a restart when the incoming property has no route.
- Detonations: L2 is `floor(50% of the closing hit)`; L3 is 100% of the closing hit as true damage, then the chain returns to Empty and a new burst window opens.
- Side effects that this fight and the named rules require: Liquefaction Burn, Burn III, Induration −30% AGI, Fragmentation −25% DEF Shatter, Magma Core −30% DEF Shatter, Distortion buff purge (up to two most recent), Solar Apex VE reset onto the living anchor.
- Magic Burst: 100% hit with no hit roll, +50% crit, positive resistance bypassed, 50% MP refund. Kith-Lir 65% replaces 50%, chip refunds cap at 80%, +600 ticks per burst (Zeph's early window is 900), extension cap +1,200.
- The owner's chain-step rule: burst damage is computed against the window open at the start of the resolution, then the property is applied. A detonation replaces the window (the old Kith-Lir extension does not carry). A restart does not close the window.
- Ash-Dravan Thermal Battery in the battle loop: Heat, decay, and a primed weapon skill. A natural L2 or L3 gets +25% detonation. Anything else is a forced Liquefaction plus Burn plus a Fire window, and Heat returns to 0. A forced detonation does not grant Cadence Surge.
- Cadence Surge only for a Sylvari-Mor whose action crits or is a physical chain link. The 1,200 AP refund is paid after recovery.
- Godot debug readout: `SimBridge` emits `ResonanceChanged` with the per-part chain, the burst mask, the ticks left, and the next links.
- The eight settled ambiguities above, including the floor-everywhere tests and the L3 true-damage test.

### What this slice did not build

- Conduction Shock and the 1,500 AP delay, Tectonic Shear's MEVA down, Radiance's party regen, Umbral Zero's AP −3,000 and MEVA down, and Tempest Crown's 100% interrupt and +2,000 AP. The matrix routes exist. Those side effects do not run.
- Ensemble Gain's chain-participant list.
- Multi-attack extra-hit damage. The rolls are consumed. Extra hits are not applied. Every golden double-attack roll fails.
- A cap on burst-as-chain-step loops. Measured, not implemented. See below.
- Gambits, the other abilities and heroes, raids, shields beyond the golden fight, and the zero-alloc pass. `BigInteger` in the damage product is the performance risk called out in decision 24.

### Loop measurement (cap proposed, not implemented)

One hero can reach Solar Apex and keep detonating it by alternating a burst closer with the next matrix step: Shear (Slashing) → Talon (Piercing, Fragmentation) → Prism (Light, Solar Apex), then Gale (Wind burst on the Solar window, which also opens L1 Wind) → Prism (Light burst, and Wind → Light is Radiance, which opens a new window) → Shear (Radiance → Slashing is Solar Apex again). The test `Burst_chain_steps_reach_level_3_and_keep_detonating` records at least eight Solar Apex true detonations, and the span from the first to the last is longer than 2,700 ticks, which is the longest a single extended burst window can last.

The same refresh exists one tier down. Piercing into an open Induration chain has no L3 route, so it restarts at L1(Piercing) and leaves the Ice window up. The next Ice burst then detonates Induration and opens a fresh Ice window.

**Proposed cap, not in the sim:** a Magic Burst that also detonates may update the chain and deal the detonation, but it must not open the replacement burst window. Only a closer that was not already bursting on that sub-target opens the next window. That keeps the owner's "also a chain step" rule and stops both loops at one window. An alternative is a 4,000-tick detonation cooldown per sub-target. Neither is implemented.

### Tests for this slice

`dotnet build ProjectResonance.sln` and `dotnet test ProjectResonance.sln` from the repo root, .NET 8 SDK. Build: 0 warnings, 0 errors. Tests: 43 passed, 0 failed (about 40 ms). That is the first pilot's formula facts, three golden tests re-pinned to seed `20261002` (including a scripted replay of every recorded roll), the chain and burst facts, the odd-input truncation facts, and `Level3_bonus_is_true_damage_outside_the_burst_bucket`.

The golden end state at tick 3,087, seed `20261002`: Korrith HP 8,643, MP 270, AP 8,483, Core VE 3,164, CE 2,303; Mirrim HP 4,480, MP 95, AP 4,566, Core CE 2,440, Weapon Arm CE 1,036; Zeph HP 3,710, MP 414, AP 5,831, Core VE 466, CE 1,899; Seraphine HP 4,750, MP 500, AP −3,989, VE 1,717 (317 normal + 1,400 heavy), CE 180; Core HP 108,698, Weapon Arm 43,915, Shield 30,000, boss AP 874; Core chain L1(Ice) until 6,900, Core burst until 4,139.

Godot 4.7-stable mono, headless, `--path src/Resonance.Game --quit-after 8` exited 0 with no script errors. There is no display on this machine, so the Step button was not clicked by hand. The scene's `_Ready` path is what that run loads.

### Time for this slice

Wall clock for this agent, from the widened resonance task at about 2026-10-02 00:43 UTC through the build, tests, and headless scene check at 01:06 UTC: **about 25 minutes**. That includes the chain side effects, the burst-as-chain-step ordering, Thermal Battery and Cadence in the battle loop, the seeded log rewrite, the degeneracy measurement, and writing the eight rulings into the bible.

A human C# developer who already has the merged core, working only on this slice:

| Task | Human | This agent |
|---|---|---|
| Matrix side effects, window replacement, burst-as-chain-step ordering | 1.5–2 days | same session |
| Thermal Battery and Cadence Surge in the battle loop | 0.5–1 day | same session |
| Regenerating §2.15 from a named seed and re-pinning the golden test | 1–2 days | same session |
| Degeneracy measurement and the resonance readout | 0.5–1 day | same session |
| Writing the eight rulings into docs/02, README §6, and this report | 0.5 day | same session |

**Human total for this slice: about 4–7 focused person-days.** The first pilot's 11–16 day estimate above is unchanged; it covered the core this slice started from.
