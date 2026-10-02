# Pilot sprint report — combat-math core

Date: 2026-10-01. Scope: the 2-week pilot described in the task brief, not milestones M1–M8 in `docs/07-production-plan.md`.

Source of truth used: `README.md` §6, `docs/02-combat-engine.md` (formulas and the §2.15 worked log), `docs/01-world-and-races.md` and `docs/03-hero-roster.md` (plus 03a/03b) for the four heroes in that log. Where the worked log and a prose rule disagree, the log was treated as the numeric acceptance test. Those disagreements are listed below. No new rule was written back into the design bible.

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
- Thermal Battery inside the battle loop. `HeatGain` and the Primed-vs-Liquefaction choice exist as pure functions and are unit-tested. They never run during `BattleSimulator`.
- Double/triple attack extra hits. The rolls are consumed (or skipped when the chance is 0). Extra-hit damage is not applied. Every golden multi-attack roll fails, so the published HP is unchanged.
- Art, the HUD in `docs/04`, CI, and a BenchmarkDotNet gate.

## Test results

Command, from the repo root, with the .NET 8 SDK:

```
dotnet build ProjectResonance.sln
dotnet test ProjectResonance.sln
```

Build: 0 warnings, 0 errors.

Tests: 36 passed, 0 failed (about 30 ms). That count is 33 formula facts/theory rows plus 3 golden tests. The golden test `Worked_log_ends_at_the_published_state` requires tick 3087 and the published table, including Korrith 8643/9240 HP, MP 270, AP 8483, Core VE 3164 / CE 2303; Mirrim MP 95 and Weapon Arm CE 1056; Zeph MP 414; Seraphine AP −3989 and VE 1717 (317 normal + 1400 heavy); Core 111683, Weapon Arm 43662, Shield 30000, boss AP 874. `Checkpoints_match_the_log_row_by_row` checks the intermediate rows (Talon 505, crit Talon 758, Frostfang 387 + detonation 193, Piston 540, Maul 374, Blizzard bursts 3849 and 1924, Lance 597, pounce to 7816.4, boss freeze at 10000.4).

Godot 4.7-stable mono, headless, `--path src/Resonance.Game`:

- `--import --quit` completed with no script errors.
- A scene run loaded the `SimBridge` autoload and `DebugBattle`. `_Ready` built four hero cards and the Carapace placeholder (Core 120000, Arm 45000, empty chain). One `Step()` advanced the sim to tick 256 and wrote the opening log lines. Temporary prints used for that check were removed afterward. There is no display on this machine, so the buttons were not clicked by hand; the same `Step()` path is what the Step button calls.

## Spec ambiguities

These were not resolved by README §6. The pilot followed the worked log so the golden test can pass, and left the prose in the design bible unchanged.

1. **Magic Burst vs chain application.** §2.10 says an applied property transitions the chain, and Blizzard II’s sheet closes Piercing → Induration. At ticks 1170 and 2639 the log deals burst damage and does not detonate or change the chain (tick 2145 is still L2 Induration; tick 2834 still closes L1 Piercing → Ice). The sim does not rewrite the chain on a Magic Burst resolution. A non-burst magical ability still applies its property. This is a local exception for the golden log, not a new design-bible rule.

2. **Missing rolls and no PCG seed.** The log prints some d10000 rolls and omits others (boss crits, later double-attack rolls, the last Frostfang hit and crit). The golden driver replays the printed rolls on separate streams and inserts failing fillers where a roll is required but not printed (boss piston crit 9999, boss lance crit 9999, later DA rolls 9999, last Frostfang hit 4200, crit 9000). A 0% interrupt chance does not consume RNG. Crit is rolled only on a landed hit. A future seed, or a full roll column in the log, would replace `ScriptedRng`.

3. **Which enmity table sheds.** §2.9.4 names the attacking sub-target’s table. The log sheds Core CE when the Weapon Arm table is empty (the selection fallback in §2.9.1 / README decision 13). The sim sheds the table that selected the target. README decision 5 defines the shed formula; it does not say which table.

4. **Regen timing.** Pulses are scheduled from the effect’s own start (`+500` ticks), not from `tick % 500 == 0`. Zeph is full, so golden HP does not depend on this. Circuit Benediction’s +20 Concentration is assumed to last the 4,000-tick regen. The excerpt never shows it expire.

5. **Shelter of the Chanters.** The +30% CE is applied to Korrith only, after Enmity+ and before the Aethel ×0.60, and the DEF ×1.25 is live while he is the Core argmax and at least two other heroes are casting. No Korrith action in the log generates CE while shelter is up, so that multiplier is unobserved.

6. **L3 wording.** The Magma/Tempest table text says “+110% Fire/Earth Burst”. §2.11.4 and README decision 8 say every L3 bonus is a true-damage component and is not part of the BurstBucket. The sim follows §2.11.4. The golden log never reaches an L3, so this is untested against a number.

7. **Integer rounding on odd inputs.** Magic accuracy is `(INT + ACC) / 2` (truncating). `0.5 × STR/DEX` truncates. Every value in the log is even, so half-up vs floor is untested.

8. **Damage accumulator width.** §2.16 says to accumulate in `long`. Ten basis-point factors overflow `Int128` if multiplied before the single division (Seismic Maul would floor to the wrong integer). The pipeline multiplies in `BigInteger` and divides by `10000^n` once. The golden integers match. This is a wider integer type, not a float.

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

The agent was faster at transcription of formulas that are already numeric. It was not faster at the parts the spec leaves implicit: unpublished dice, which table sheds, and whether a burst closes a chain. Those are still open questions for a designer, and a human would have had to stop and ask the same ones.
