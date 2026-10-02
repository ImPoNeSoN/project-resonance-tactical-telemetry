# 02 — Combat Engine: Conditional Time Battle, Damage Math, Enmity, Resonance

This is the authoritative rules reference for the combat simulation. Every number in the hero sheets ([03-hero-roster.md](03-hero-roster.md)), the HUD ([04-hud-and-visual-engine.md](04-hud-and-visual-engine.md)), the dungeon systems ([05-dungeon-progression.md](05-dungeon-progression.md)) and the arena validator ([06-multiplayer-raids-arena.md](06-multiplayer-raids-arena.md)) resolves through the formulas below. When a hero sheet and this file disagree, this file wins and the hero sheet is a bug.

All formulas are written in real-number notation for readability. Section 2.16 specifies how the simulation evaluates them in deterministic integer / fixed-point arithmetic.

---

## 2.1 Stat glossary

### 2.1.1 Primary stats (present on every unit)

| Stat | Meaning | Typical Level 50 hero range | Typical boss range |
|---|---|---|---|
| HP | Hit points. Unit is KO'd at 0. | 3,680 – 9,240 | Per part: 30,000 – 2,000,000 |
| MP | Mana for abilities. | 225 – 1,560 | Bosses do not spend MP |
| ATK | Weapon power. Base of all Physical and Elemental-Physical damage. | 136 – 536 | 380 – 900 |
| DEF | Physical defense. Feeds physical DR. | 161 – 710 | 300 – 900 |
| INT | Arcane power. Base of Magical damage and Healing. | 99 – 606 | 250 – 800 |
| MEVA | Magic evasion and magic defense. Feeds magical hit chance and magical DR. | 144 – 416 | 200 – 600 |
| ACC | Physical accuracy. | 200 – 323 | 250 – 450 |
| EVA | Physical evasion. | 108 – 247 | 100 – 260 |
| AGI | Timeline speed; AP gained per tick. | 9 – 18 | 8 – 20 |

### 2.1.2 Derived and gear-only stats

| Stat | Source | Definition / use | Cap |
|---|---|---|---|
| MACC (Magic Accuracy) | Derived + gear | MACC = 0.5 × INT + 0.5 × ACC + gear MACC | None |
| STR | Gear, chips, buffs only | Adds to effective ATK for Blunt/Slashing (full) and Piercing (half) | Gear STR ≤ 30% of base ATK |
| DEX | Gear, chips, buffs only | Adds to effective ATK for Piercing (full) and Blunt/Slashing (half); +1 ACC per point; +0.04% crit chance per point | Gear DEX ≤ 30% of base ATK |
| Concentration | Gear, buffs | Reduces Interrupt% by 1 percentage point per point | 60 (heroes); bosses have fixed values 0–90 |
| Fast Cast (FC) | Gear, race, buffs | Chant reduction | Gear 50%; total 65% |
| EPEN (Elemental Penetration) | Race, gear | Each point reduces target's positive elemental resistance by 0.2 percentage points | 150 (−30% resistance) |
| Crit Rate | Derived + gear | See 2.7 | 50% (Burst can exceed, hard cap 100%) |
| Crit DMG | Gear | Added to the 1.50× base crit multiplier | +1.00 (2.50× total) |
| Magic Burst Damage (MBD) | Mid-Cast gear | Added into the Burst bonus bucket | +40% |
| WS Damage | Weapon Skill gear | Multiplies weapon skill damage | +40% |
| Double Attack / Triple Attack | Weapon Skill gear | Extra hits on weapon skills, see 2.5.4 | DA 50%, TA 25% |
| Healing Potency | Mid-Cast gear | Multiplies heals | +50% |
| Enmity+ | Idle/Engaged gear | Multiplies CE generation from abilities | −50% to +50% |
| DT− (damage taken reduction) | Gear | Reduces damage taken; does NOT count as mitigation for CE shed | Gear −30%; gear + buffs −60% |
| Haste | Buffs | Multiplies AGI upward | +50% |
| Slow | Debuffs | Multiplies AGI downward | −50% |

### 2.1.3 Reconciling STR/DEX with the ATK-based stat array

The base stat array (the nine primary stats) deliberately contains only ATK. STR and DEX are **gear-only sub-stats**: no hero has base STR or DEX, and they appear exclusively on loadout gear, Schematic Chips and a small number of buffs. They exist so that the Weapon Skill loadout can specialize a hero toward a damage property without changing that hero's identity stats.

Effective ATK used by a weapon skill:

| Ability property / type | ATK_eff |
|---|---|
| Physical, Blunt | ATK + STR + 0.5 × DEX |
| Physical, Slashing | ATK + STR + 0.5 × DEX |
| Physical, Piercing | ATK + DEX + 0.5 × STR |
| Elemental-Physical (any element) | ATK + 0.5 × STR + 0.5 × DEX |
| Counter-attacks (Drusk Oma-Teth) | ATK + Idle-set STR/DEX by the same property rules |

DEX additionally adds +1 ACC per point and +0.04 percentage points of crit chance per point. Base ATK multiplied by buffs (Warcry of the Deep +15%) multiplies ATK before STR/DEX are added: `ATK_eff = ATK × (1 + ATK buffs) + STR/DEX terms`.

---

## 2.2 CTB timeline (the tick simulation)

### 2.2.1 Gauge

- Every unit has an AP gauge displayed from 0 to 10,000.
- Per tick: `AP += AGI_eff`, where `AGI_eff = AGI × (1 + Haste − Slow)`, Haste capped at +50%, Slow capped at −50%.
- A unit may act when `AP ≥ 10,000`.
- After the action resolves: `AP −= RecoveryCost`.

| Recovery class | Cost | Used by |
|---|---|---|
| Stance | 4,000 | Taunts, stances, quick links, fast buffs |
| Standard | 10,000 | Most attacks and spells |
| Heavy / Tier-3 | 14,000 | Finishers, tier-3 spells, party-wide heavy buffs |

### 2.2.2 Gauge boundary rules

- **Overshoot.** AP can exceed 10,000 on the tick a unit becomes ready (for example 9,995 + 18 = 10,013). The overshoot is kept and is subtracted from normally.
- **AP debt.** After a Heavy action, AP can go negative (10,013 − 14,000 = −3,987). The floor is −4,000 AP. The HUD draws debt as a red under-bar beneath the portrait.
- **Refund ceiling.** AP refunds and grants (Cadence Surge, Tempo Gift, Cinder Pulse, Tempest Crown, Patient Stone) cannot raise AP above 10,000. If AP is already ≥ 10,000, the grant is lost.
- **Delays.** Enemy AP delays (Hollow Resound, Vector Stun, Pounce on the Upbeat, Conduction, Umbral Zero) reduce AP down to the −4,000 floor.
- **Initial AP.** At combat start: `AP_initial = min(6,000, AGI × 300)`. Ambush encounters set the ambushed side to 0.
- **Casting freeze.** While a unit is chanting, its AP does not change. Recovery is paid when the chant resolves (or when it is interrupted, see 2.3.4).
- **Stun.** A stunned unit gains no AP for the stun duration.

### 2.2.3 Deterministic tick order

Within a single tick the simulation processes steps in exactly this order:

1. **AP gain** for every unit that is not casting and not stunned.
2. **Global timers:** VE decay if `tick % 500 == 0`; status expirations; Resonance window and burst window expirations; Aether Density Meter updates (dungeon only). DoT and regen pulses are **not** tied to that global phase. Each effect pulses every 500 ticks measured from its own start (`start + 500`, `start + 1,000`, …) until its duration ends. Circuit Benediction's +20 Concentration lasts for the whole regen, and expires on the same tick the regen expires.
3. **Chant resolutions** due this tick, ordered by chant start tick, then party slot, then enemies.
4. **Action selection:** every unit with AP ≥ 10,000 that is not casting acts, ordered by:
   1. higher AP first,
   2. then higher AGI_eff,
   3. then player units before enemy units (Vessa Quiet-Claw's First in the Silence overrides this for her),
   4. then lower slot index.
   Each action resolves completely (damage, enmity, chain state, refunds) before the next unit is evaluated, so the second unit in the same tick sees the updated state.
5. **Gambit evaluation** happens inside step 4 for units under gambit control (see 04-hud-and-visual-engine.md section 4.9).

### 2.2.4 Timing scale

At AGI 10 a unit needs 1,000 ticks to go from 0 to 10,000 AP, so one Standard action per 1,000 ticks. All windows in the game are measured against this:

| Duration | In Standard turns at AGI 10 | In Standard turns at AGI 18 |
|---|---|---|
| VE decay step (500 ticks) | 0.5 | 0.9 |
| Magic Burst window (1,500 ticks) | 1.5 | 2.7 |
| Resonance window (4,000 ticks) | 4.0 | 7.2 |
| Induration slow (8,000 ticks) | 8.0 | 14.4 |
| Cadence Surge refund (1,200 AP) | 12% of a turn at any AGI | 12% of a turn at any AGI |

---

## 2.3 Chanting, Fast Cast and interrupts

### 2.3.1 Chant placement

When a unit starts an ability with Chant Time (CT) > 0:

1. The Fast Cast loadout is evaluated and the effective chant time is locked: `CT_eff = ceil(CT × (1 − FC_total))`, with `FC_total = min(0.65, min(0.50, FC_gear) + FC_race + FC_buffs)`.
2. An **interrupt target node** is placed on the timeline at `Delay = CT_eff` ticks after the current tick. The HUD draws it on the 10,000-AP bar as a diamond node tethered to the caster's portrait.
3. The unit swaps to its Mid-Cast/Burst loadout for the whole chant duration.
4. On the resolution tick the ability resolves using the Mid-Cast loadout, then `AP −= RecoveryCost`.

Aethel-Born have +15% innate Fast Cast (race), which counts under the 65% total cap but not under the 50% gear cap. That is how Aethel-Born reach 65% while other races top out at 50% plus buffs.

### 2.3.2 Interrupt formula

Every damaging hit (not DoT pulses) a chanting unit takes rolls an interrupt check:

```
Interrupt% = (DamageTaken / MaxHP) × 2.5 − (Concentration × 0.01)
Interrupt% = clamp(Interrupt%, 0.00, 0.95)
```

- `DamageTaken` is the final post-mitigation damage of that hit (after absorb shields: a hit fully absorbed has DamageTaken 0).
- For bosses, `MaxHP` is the Max HP of the casting sub-target (Core, Shield or Weapon Arm), not the sum of all parts.
- Example: a 3,710 HP Kith-Lir caster with Concentration 20 hit for 900: 900 / 3,710 × 2.5 − 0.20 = 0.606 − 0.20 = 40.6%.

### 2.3.3 Ability-driven interrupts (Stuns)

Abilities marked as interrupt-capable (Vector Stun, Tempest Crown detonation, Hollow Resound bonus) use:

```
Interrupt% = max(FlatInterrupt, DamageInterrupt) + AdditiveBonuses
```

where `FlatInterrupt` is the ability's listed base (Vector Stun 60%), `DamageInterrupt` is the 2.3.2 formula, and additive bonuses (Nyx Sub-Vector's Counterexample +35%, Hollow Resound +20%, Geometric Refutation +15%) are added afterward. Tempest Crown is a flat 100% that ignores Concentration. All other results clamp to 0–95%.

### 2.3.4 Interrupt consequences

- The ability does not resolve and its interrupt node is removed from the timeline.
- 50% of the MP cost is consumed (rounded down).
- The caster pays a Stance recovery (4,000 AP) instead of the ability's normal recovery.
- The caster swaps back to Idle/Engaged.

### 2.3.5 Boss chant reference values

| Boss tier | Concentration | Notes |
|---|---|---|
| Floor trash | 0 | Easily interrupted by damage |
| Floor boss | 40 | Damage interrupts require about 16% of part HP in one hit |
| Deep boss (floor 10+) | 60 | Damage interrupts are impractical; use Stuns |
| World Boss parts | 90 | Only Tempest Crown and flat-chance Stuns work |

---

## 2.4 Hit and evasion

### 2.4.1 Physical

```
Hit% = clamp(0.75 + (ACC − EVA) / 200, 0.05, 0.95)
```

ACC includes DEX (+1 per point) and debuffs (Blind −40, Ash Veil −60).

### 2.4.2 Magical

```
MagicHit% = clamp(0.75 + (MACC − MEVA) / 200, 0.05, 0.95)
```

- A damaging spell that fails its hit roll is **Resisted**: it deals 50% damage and any debuff rider fails. It still applies its chain property.
- A non-damaging debuff that fails deals no effect and applies no chain property.
- Magic Bursts skip the roll (100% hit).
- Heals and buffs on allies never roll.

### 2.4.3 Chain properties on misses

A physical miss applies no chain property and does not change the target's chain state. A Resisted spell applies its property normally.

---

## 2.5 Physical damage

### 2.5.1 Core formula (user-specified, unchanged)

```
DR      = DEF / (DEF + 500)
PhysDmg = (ATK × Multiplier) × (1 − DR)
```

`ATK` here is ATK_eff from 2.1.3. `DEF` is the target's effective DEF:

```
DEF_eff = DEF × (1 + DEF buffs) × (1 − DEF Shatter) × (1 − ArmorIgnore)
```

- DEF Shatter comes from Fragmentation (25%), Magma Core (30%) and chips; Shatter effects do not stack with each other: the highest one applies.
- ArmorIgnore is per-hit (Ishka Pyre-Lash's Draft Through the Breach 30%).

### 2.5.2 Elemental-Physical

Weapon skills that carry an element use the physical formula and then elemental resistance:

```
ElemPhysDmg = (ATK_eff × Multiplier) × (1 − DR) × (1 − EffRes_element)
```

These count as weapon skills for Thermal Battery, the Weapon Skill loadout and physical Magic Burst rules. For Heat and for elemental damage-taken reductions (Obsidian Mantle, Kiln Discipline) they count as elemental damage.

### 2.5.3 Full physical pipeline (order matters)

1. `Base = ATK_eff × Multiplier` (Multiplier includes additive modifiers like Forge Surge +0.80).
2. `× (1 − DR)` using DEF_eff.
3. `× (1 − EffRes)` for Elemental-Physical only.
4. `× CritMultiplier` if the crit roll succeeds.
5. `× (1 + WS Damage)` for weapon skills (Weapon Skill loadout, cap +40%).
6. `× (1 + BurstBucket)` if the hit lands inside a matching physical burst window (Fragmentation or Solar Apex).
7. `× (1 + DamageDealtBuffs)` (Faultline Momentum, Thurga's Banked Furnace, Prism Overclock is spells only).
8. `× (1 − TargetDamageTakenReduction)` (bosses have none by default; some phases grant it).
9. Floor to an integer.
10. Absorb shields on the target subtract from the result.

### 2.5.4 Multi-Attack

Weapon skills roll Multi-Attack once after the primary hit:

- Roll Triple Attack first (chance = gear TA, cap 25%). On success, two extra hits.
- Otherwise roll Double Attack (chance = gear DA, cap 50%). On success, one extra hit.
- Each extra hit deals 50% of the primary hit's pre-crit damage, rolls its own crit and hit, carries no chain property, and does not count as a chain link. Extra hits generate CE normally and can trigger Cadence Surge only via a crit (still at most once per action).

---

## 2.6 Magical damage

### 2.6.1 Formula (INT/MEVA analog of the physical formula)

```
MDR    = MEVA / (MEVA + 600)
MagDmg = (INT × Multiplier) × (1 − MDR) × (1 − EffRes_element)
```

The constant 600 (vs 500 for DEF) exists because MEVA also drives magical hit chance; a lower DR slope keeps MEVA from being twice as valuable as DEF.

### 2.6.2 Full magical pipeline

1. `Base = INT_eff × Multiplier` (INT_eff includes Mid-Cast gear INT and INT buffs; Entropic Sigil −20% INT applies to bosses).
2. `× (1 − MDR)`.
3. `× (1 − EffRes)`; a Magic Burst replaces EffRes with `min(EffRes, 0)` (resistance is bypassed but weakness is kept).
4. `× 0.50` if Resisted (never on a burst).
5. `× CritMultiplier` if the crit roll succeeds.
6. `× (1 + BurstBucket)` if bursting (see 2.11).
7. `× (1 + SpellDamageBuffs)` (Prism Overclock +40%, Zeph's Early-Window Cartography +25%).
8. `× BurstDiminish` for the 2nd and later bursts in the same window.
9. `× (1 − TargetDamageTakenReduction)`.
10. Floor to an integer; then add any true-damage burst component (Distortion and Level 3 bonuses) computed from the value at step 9.
11. Absorb shields subtract.

### 2.6.3 Healing

```
Heal = INT_eff × Multiplier × (1 + HealingPotency)
```

Heals never crit unless a chip grants it. HP restored beyond Max HP is overheal; only HP actually restored generates VE.

---

## 2.7 Critical hits

| Component | Physical | Magical |
|---|---|---|
| Base | 5% | 5% |
| Accuracy margin | + max(0, ACC_eff − EVA) / 2,000 | none |
| DEX | + DEX × 0.04% | none |
| Gear / buffs | + listed values | + listed values |
| Normal cap | 50% | 50% |
| Magic/Physical Burst | + 50% applied after the cap | + 50% applied after the cap |
| Hard cap | 100% | 100% |

```
CritMultiplier = 1.50 + CritDMG_gear      (max 2.50; Kaelis Moon-Ravel's Execution Frame raises his to 2.90)
```

True damage never crits. DoTs never crit.

---

## 2.8 Elemental resistance, penetration and true damage

### 2.8.1 Elements

Eight elements exist: Fire, Ice, Wind, Earth, Lightning, Water, Light, Darkness. "Umbral" damage is Darkness-flavoured true damage produced only by Distortion and Umbral Zero.

### 2.8.2 Resistance

Each unit has `Res_e` per element in the range −50% (weak) to +90% (near-immune). Heroes start at 0% for all elements (race exceptions: Ash-Dravan Burn immunity, below). Buffs (Barrier Proof +15%) add; buff-derived resistance is capped at +90% total.

### 2.8.3 Penetration

```
EffRes = Res_e − EPEN × 0.002    if Res_e > 0, floored at 0
EffRes = Res_e                   if Res_e ≤ 0 (penetration never deepens a weakness)
```

Ash-Dravan have +75 EPEN innately (−15 percentage points). EPEN cap is 150 (−30 points).

### 2.8.4 Burn immunity (Ash-Dravan)

Ash-Dravan take 0 damage from Burn DoT pulses and cannot be afflicted with the Burn status. Direct Fire damage still applies and still generates Heat (×1.5, see 2.12.4).

### 2.8.5 True damage

True damage ignores DEF, MEVA, elemental resistance, damage-taken reductions and DT− gear. It is still absorbed by absorb shields and by an active boss Shield sub-target (see 06). It never crits, is never Resisted, and is not affected by burst multipliers except where a rule explicitly creates true damage from a burst.

Sources: Level 3 detonations, Distortion and Level 3 burst components, Umbral effects, Ember Tithe self-damage.

---

## 2.9 Dual enmity: Volatile Enmity (VE) and Cumulative Enmity (CE)

### 2.9.1 Tables

Every boss sub-target (Core, Shield, Weapon Arm) keeps its own enmity table with one VE and one CE entry per hero. Each boss ability belongs to a sub-target; that ability selects its target from its own sub-target's table. If that table has no entries above 0, it falls back to the Core table.

Enmity from an ability goes to the table of the sub-target that ability targeted. Self-buffs, party buffs and heals go to the Core table (and to every table if the ability says "all parts").

### 2.9.2 Volatile Enmity (VE)

| Source | VE gained |
|---|---|
| Ability flat VE | Listed Base VE |
| Healing | 40% of HP actually restored |
| Absorb shields applied | 25% of shield amount |
| Veth-Kari Chitinous Grounding | 15% of post-mitigation physical damage taken, added to the attacking sub-target's table |

- **Decay:** on every global tick divisible by 500, `VE = floor(VE × 0.90)` (10% per 500 ticks).
- **Aethel-Born Phase Dampener:** Aethel-Born keep a second VE bucket for heavy-tagged VE. Heavy-tagged sources are: a single heal restoring ≥ 1,500 HP, any ability with Base VE ≥ 800, and any ability with Heavy (14,000) recovery. The heavy bucket decays `floor(VE_heavy × 0.80)` per 500 ticks (2× speed). Displayed VE is the sum of both buckets.
- **Cap:** 30,000 VE per hero per table.

### 2.9.3 Cumulative Enmity (CE)

| Source | CE gained |
|---|---|
| Ability flat CE | Listed Base CE × (1 + Enmity+) |
| Damage | 8% of final damage dealt (including detonation damage credited to the chain closer) |
| Stances | Listed Base CE |
| Aethel-Born | All CE generation × 0.60 |

- CE does not decay.
- **Cap:** 30,000 CE per hero per table.

### 2.9.4 CE shed (the only way CE goes down)

CE is shed only when a hero takes a **direct unmitigated hit** from the sub-target that owns the table.

A hit is **unmitigated** when all of these are true:

- it was not evaded (it landed),
- no absorb shield absorbed any part of it,
- the hero had no active damage-taken reduction buff that applies to that damage category (Keratin Bastion for physical, Obsidian Mantle for elemental, Phase Sanctuary for all).

DEF, MEVA, elemental resistance and gear DT− do **not** count as mitigation. DoT pulses are never "direct."

```
CE_shed = floor(CE × min(0.25, 0.5 × DamageTaken / MaxHP))
CE      = CE − CE_shed
```

So a hit for 10% of Max HP sheds 5% of that hero's CE on that table, and no single hit can shed more than 25%. Tanks protect their CE lead by staying mitigated; damage dealers who get hit shed CE and naturally drop down the table.

**Which table.** CE is shed from the table that **selected** the target. That is the acting sub-target's own table when it has any threat, and the **Core** table when that part's table is empty (the same fallback as 2.9.1). A Weapon Arm hit that fell back to Core sheds Core CE, not an empty Arm row.

### 2.9.5 Target selection

```
BossTarget = argmax over heroes (VE + CE) on the acting sub-target's table
```

Ties: higher CE, then lower party slot. KO'd heroes are skipped. There is no hysteresis: if the argmax changes between two boss actions, the next action retargets.

### 2.9.6 Solar Apex VE reset

When a Solar Apex detonates on a sub-target, every hero's VE on that table is set to 0 and the sum of all removed VE is added to the **active tank's** VE on that table. Active tank = the hero in the formation's Anchor slot; if KO'd, the living Anchor Tank archetype hero with the highest CE; if none, the living hero with the highest CE. The added VE is not heavy-tagged even if some of it came from Aethel-Born heavy buckets.

---

## 2.10 Resonance (chain system)

### 2.10.1 Properties

Eleven Level 1 properties can be applied by abilities:

| Group | Properties |
|---|---|
| Physical | Blunt, Piercing, Slashing |
| Elemental | Fire, Ice, Wind, Earth, Lightning, Water, Light, Darkness |

### 2.10.2 Chain state per sub-target

Each boss sub-target has one chain state: `Empty`, `L1(property)`, `L2(resonance)`, and each non-empty state has a window timer.

- Applying a property to an `Empty` target opens a **4,000-tick window** at `L1(property)`.
- Applying a property to an open window looks up the transition table (2.10.3 and 2.10.4):
  - **Valid L1 → L2 transition:** Level 2 Resonance Detonation. The state becomes `L2(resonance)` with a fresh 4,000-tick window, and a 1,500-tick Magic Burst window opens.
  - **Valid L2 → L3 transition:** Level 3 (Apex) Detonation. The state becomes `Empty` (L3 is terminal), and a 1,500-tick Magic Burst window opens.
  - **No valid transition:** the chain restarts: the state becomes `L1(new property)` with a fresh 4,000-tick window. Nothing detonates.
- When a window expires, the state becomes `Empty`.
- Chain participants (distinct heroes who applied a property that is still part of the current chain) are tracked for Lyr Aurelis-7's Ensemble Gain.
- A **physical chain link** (for Cadence Surge) is a Physical or Elemental-Physical ability whose property produces a valid L2 or L3 transition. Opening a window is not a link.
- A **Magic Burst also takes the chain step.** A magical ability whose element is in the open burst window deals its burst damage against that window, then applies its property as the next chain step (2.11.2). A valid transition detonates and opens the next resonance level. Any other combination restarts the chain at `L1(that property)`. A restart does not close the burst window. A detonation still replaces it (extensions on the old window do not carry over).

### 2.10.3 Level 2 matrix (L1 window → incoming property)

The five user-specified resonances are kept exactly; three are extended with extra routes, and three new resonances (Conduction, Tectonic Shear, Radiance) complete the element set so every element has at least one L2 entry.

| Resonance (L2) | Routes (open L1 → incoming) | Burst elements | Burst bonus | Detonation effect |
|---|---|---|---|---|
| Liquefaction | Blunt → Fire; Earth → Fire | Fire | +40% Fire Burst | Burn DoT (2.10.6) |
| Induration | Piercing → Ice; Water → Ice | Ice | +50% Ice Burst | −30% AGI Slow for 8,000 ticks |
| Fragmentation | Slashing → Wind; Slashing → Piercing | Wind (spells) and all Physical / Elemental-Physical weapon skills | +60% Phys Burst | −25% DEF Shatter for 4,000 ticks |
| Distortion | Blunt → Darkness; Blunt → Slashing; Water → Darkness | Darkness | +70% of burst damage added as Umbral True Damage | Buff Purge: removes up to 2 beneficial effects |
| Conduction | Water → Lightning; Piercing → Lightning | Lightning | +45% Lightning Burst | Shock DoT and 1,500 AP delay |
| Tectonic Shear | Slashing → Earth; Earth → Blunt | Earth | +45% Earth Burst | −20% MEVA for 6,000 ticks |
| Radiance | Fire → Light; Wind → Light | Light | +40% Light Burst | Party Regen 2% Max HP per 500 ticks for 3,000 ticks |

Full L1 × incoming lookup (rows = open window, columns = incoming property; blank = chain restarts at the incoming property):

| Open \ Incoming | Blunt | Piercing | Slashing | Fire | Ice | Wind | Earth | Lightning | Water | Light | Darkness |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Blunt | | | Distortion | Liquefaction | | | | | | | Distortion |
| Piercing | | | | | Induration | | | Conduction | | | |
| Slashing | | Fragmentation | | | | Fragmentation | Tectonic Shear | | | | |
| Fire | | | | | | | | | | Radiance | |
| Ice | | | | | | | | | | | |
| Wind | | | | | | | | | | Radiance | |
| Earth | Tectonic Shear | | | Liquefaction | | | | | | | |
| Lightning | | | | | | | | | | | |
| Water | | | | | Induration | | | Conduction | | | Distortion |
| Light | | | | | | | | | | | |
| Darkness | | | | | | | | | | | |

Ice, Lightning, Light and Darkness are **closer-only** properties at Level 1: opening a window with them is legal but no L2 route starts from them. This is intentional: the elements Spike DPS casters burst with are the elements that finish chains, so casters cannot self-start chains.

### 2.10.4 Level 3 (Apex) matrix (L2 window → incoming property)

| Apex (L3) | Routes (open L2 → incoming) | Burst elements | Burst bonus | Detonation effect |
|---|---|---|---|---|
| Solar Apex | Fragmentation → Light (user-specified); Radiance → Slashing | Light, Wind, all Physical / Elemental-Physical | +120% of burst damage added as True Damage | Boss VE reset onto active tank (2.9.6) |
| Umbral Zero | Distortion → Ice; Induration → Darkness | Darkness, Ice | +110% of burst damage added as True Damage | Boss AP −3,000; −30% MEVA for 6,000 ticks |
| Magma Core | Liquefaction → Earth; Tectonic Shear → Fire | Fire, Earth | +110% of burst damage added as True Damage (not BurstBucket) | Burn III DoT; −30% DEF for 6,000 ticks |
| Tempest Crown | Fragmentation → Lightning; Conduction → Wind | Wind, Lightning | +110% of burst damage added as True Damage (not BurstBucket) | 100% interrupt of any chant on that sub-target; every living ally +2,000 AP |

Full L2 × incoming lookup (blank = chain restarts at the incoming property as L1):

| Open L2 \ Incoming | Blunt | Piercing | Slashing | Fire | Ice | Wind | Earth | Lightning | Water | Light | Darkness |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Liquefaction | | | | | | | Magma Core | | | | |
| Induration | | | | | | | | | | | Umbral Zero |
| Fragmentation | | | | | | | | Tempest Crown | | Solar Apex | |
| Distortion | | | | | Umbral Zero | | | | | | |
| Conduction | | | | | | Tempest Crown | | | | | |
| Tectonic Shear | | | | Magma Core | | | | | | | |
| Radiance | | | Solar Apex | | | | | | | | |

### 2.10.5 Detonation damage

```
L2 DetonationDmg = floor(0.50 × ClosingHitFinalDamage)   typed as the resonance's element, not re-mitigated
L3 DetonationDmg = floor(1.00 × ClosingHitFinalDamage)   True Damage
```

- ClosingHitFinalDamage is the closing ability's final damage after step 9 of its pipeline (before shields). A 0-damage closer (no damage multiplier) produces 0 detonation damage but still produces all other effects.
- Detonation damage credits CE to the closer at the standard 8%.
- A Thermal Battery forced detonation adds +25% to the L2 detonation damage (2.12.4).

### 2.10.6 Resonance status effects

| Effect | Source | Numbers | Stacking |
|---|---|---|---|
| Burn | Liquefaction | 0.35 × closer's max(ATK_eff, INT_eff) Fire damage per 500 ticks for 4,000 ticks (8 pulses); reduced only by elemental resistance after EPEN | Refreshes, never stacks; Burn III overrides |
| Burn III | Magma Core | 0.70 × closer's max(ATK_eff, INT_eff) per 500 ticks for 4,000 ticks (8 pulses) | Overrides Burn |
| Slow (Induration) | Induration | −30% AGI for 8,000 ticks | Adds with other Slows up to −50% cap; re-application refreshes |
| DEF Shatter | Fragmentation | −25% DEF for 4,000 ticks | Highest Shatter applies |
| Umbral burst | Distortion | +70% of burst spell damage as true damage | Per spell |
| Buff Purge | Distortion | Removes the 2 most recently applied beneficial effects | Instant |
| Shock | Conduction | 0.25 × closer's max(ATK_eff, INT_eff) Lightning damage per 500 ticks for 3,000 ticks (6 pulses) | Refreshes |
| AP delay | Conduction | Target AP −1,500 | Instant |
| MEVA Down | Tectonic Shear | −20% MEVA for 6,000 ticks | Highest MEVA Down applies |
| Regen (Radiance) | Radiance | Every living ally heals 2% Max HP per 500 ticks for 3,000 ticks (6 pulses); no VE generated | Refreshes |
| VE reset | Solar Apex | See 2.9.6 | Instant |
| AP setback | Umbral Zero | Target AP −3,000 | Instant |
| MEVA Down (Umbral) | Umbral Zero | −30% MEVA for 6,000 ticks | Highest applies |
| DEF Break (Magma) | Magma Core | −30% DEF for 6,000 ticks | Highest Shatter applies |
| Tempest interrupt | Tempest Crown | 100% interrupt, ignores Concentration | Instant |
| Tempest haste | Tempest Crown | Every living ally +2,000 AP (refund ceiling applies) | Instant |

---

## 2.11 Magic Burst

### 2.11.1 Window

- A 1,500-tick burst window opens on the detonated sub-target on the detonation tick.
- The window has an element set (column "Burst elements" in 2.10.3 / 2.10.4).
- Burst windows are independent of chain windows: a chain restart does not close a burst window. A new detonation on the same sub-target closes the old burst window and opens a new one (extensions do not carry over).

### 2.11.2 Qualifying

A **Magic Burst** happens when a Magical ability whose element is in the window's set **resolves** on that sub-target while the window is open. The start tick of the chant does not matter; resolution tick does. This is what makes chant timing the core skill of Spike DPS play.

That same resolution **also applies the spell's chain property**. Burst damage is computed against the window that was open at the start of the resolution (100% hit, burst crit, BurstBucket or true component, diminish index, MP refund). The property is applied after that, using the spell's step-9 damage as the closing hit. If the matrix detonates, the new detonation closes the window this spell just burst (a Kith-Lir extension applied to that old window does not carry onto the new one) and opens a fresh 1,500-tick window. If the matrix does not detonate, the chain restarts at `L1(property)` and the current burst window stays open, extension included. The spell does not also count as a burst of a window it just opened.

A **Physical Burst** happens when a Physical or Elemental-Physical ability resolves inside a window whose set includes Physical (Fragmentation and Solar Apex only). Elemental-Physical weapon skills do not burst in purely elemental windows (Frostfang Pounce into an Induration window is a normal hit); this keeps linkers from double-dipping on windows they just opened.

### 2.11.3 Burst effects (user-specified, unchanged)

| Effect | Magic Burst | Physical Burst |
|---|---|---|
| Hit | 100% (no Resisted) | 100% |
| Crit chance | +50% (after cap, hard cap 100%) | +50% (after cap, hard cap 100%) |
| Elemental resistance | Bypassed (`min(EffRes, 0)`) | Bypassed for Elemental-Physical |
| MP refund | 50% of MP cost | none |
| Resonance burst bonus | Added to BurstBucket | Added to BurstBucket |

### 2.11.4 BurstBucket

```
BurstBucket = ResonanceBurstBonus (non-true resonances only) + MBD_gear (cap +40%) + chip burst bonuses
BurstDiminish = max(0.50, 0.85^(n − 1)) for the n-th burst in the same window
TrueBurstComponent = floor(Step9Damage × TrueBonus)   for Distortion (+70%) and all L3 (+110% / +120%)
```

For L3 and Distortion windows, the resonance bonus is **not** added to BurstBucket; it is dealt as the separate true-damage component, and MBD gear still multiplies the normal portion.

### 2.11.5 Kith-Lir Aetheric Condenser stacking rules

- **MP refund does not stack additively.** A Kith-Lir burst refunds 65% of the MP cost **instead of** the standard 50% (they are alternatives; the higher applies). If they were additive (115%), bursts would be MP-positive, which collapses the resource game.
- Chip-based burst refunds add to whichever base applies; total refund cap is 80% of MP cost.
- **Window extension:** each Kith-Lir Magic Burst extends the current burst window by 600 ticks (Zeph Tri-Lumen's Early-Window Cartography makes it 900). Total extension is capped at +1,200 ticks per window, so a window can last at most 2,700 ticks.
- Extension happens on the resolution tick of the bursting spell and is visible on the Resonance Oscilloscope immediately.
- Global Magic Bursts from raids (06) can be extended by Kith-Lir only within the squad whose Kith-Lir burst; the extension does not propagate to other squads.

---

## 2.12 Racial passives: exact mechanics

### 2.12.1 Veth-Kari: Chitinous Grounding

On each physical hit taken that deals damage (after DEF and DT−, before or after shields does not matter; use post-shield damage), add `floor(0.15 × PhysicalDamageTaken)` VE to this hero on the attacking sub-target's table. This VE decays normally. Effect: Veth-Kari tanks gain threat from being hit, which keeps them on top while CE sheds from anything unmitigated.

### 2.12.2 Sylvari-Mor: Cadence Surge

When an action by a Sylvari-Mor includes at least one critical hit OR is a physical chain link (2.10.2), refund 1,200 AP after recovery is paid (refund ceiling applies). Maximum one refund per action. Saeli Thorn-Vesper's Seventh-Second Tempo raises it to 1,800 for the triggering action.

### 2.12.3 Kith-Lir: Aetheric Condenser

See 2.11.5. Applies only to Magic Bursts (not Physical Bursts) where the Kith-Lir is the caster.

### 2.12.4 Ash-Dravan: Thermal Battery

Heat is a 0–100 resource.

```
HeatGain per elemental hit = max(3, ceil(300 × ElementalDamageTaken / MaxHP))
HeatGain × 1.5 if the element is Fire (rounded up)
```

- Counts every direct elemental hit (Magical or Elemental-Physical) after all mitigation. DoTs do not grant Heat.
- Absorbed damage counts toward Heat (the hit still "arrived"); a fully evaded hit grants no Heat.
- Decay: −5 Heat every 1,000 ticks during which the hero took no elemental damage.
- Example: Varra Kesh-Ember (8,740 Max HP) takes 874 Fire damage: 300 × 874 / 8,740 = 30; ×1.5 = 45 Heat.

**At 100 Heat (Primed):** the hero's next weapon skill (Physical or Elemental-Physical ability) forces a Level 2 Resonance Detonation:

1. If the weapon skill's property forms a valid L2 or L3 transition with the target's current state, that transition happens normally and its detonation damage gets +25%.
2. Otherwise, a forced **Liquefaction** detonation happens (regardless of the current state): detonation damage = 0.50 × weapon skill final damage × 1.25, Burn is applied (Ash-Dravan Burn immunity is about the hero, not the target), the state becomes `L2(Liquefaction)` with a fresh window, and a Fire burst window opens.
3. Heat becomes 0 (Thurga Ember-Maw: 30).

The forced detonation is the Ash-Dravan hero's own action, so it never triggers another race's Cadence Surge; it does count the Ash-Dravan as a chain participant for Ensemble Gain.

### 2.12.5 Aethel-Born: Phase Dampener

See 2.9.2 for the two-bucket VE model. Additionally, all Aethel-Born have innate +15% Fast Cast and CE generation × 0.60.

### 2.12.6 Shelter of the Chanters (Korrith Vael-Dun)

Kept as implemented. While the formation anchor (party slot 0) is the **Core-table** argmax and at least two other heroes are casting, that anchor's DEF is multiplied by 1.25 and that anchor's flat CE generation is multiplied by 1.30. The CE multiplier is applied after Enmity+ and before the Aethel-Born ×0.60, and it does not apply to anyone else's CE. The check is the Core table only, not each part's table. The condition is re-checked when chants start and resolve; the bonus ends on the tick it stops being true.

---

## 2.13 Mid-action gear swapping (four loadouts)

### 2.13.1 Loadout structure

Each hero owns exactly four loadouts. Each loadout has 5 gear slots (Head, Body, Hands, Legs, Focus) and 2 Schematic Chip sockets (chips in 05-dungeon-progression.md). A gear item can be placed in multiple loadouts.

| Loadout | Worn when | Stats it can carry | Caps |
|---|---|---|---|
| Idle/Engaged | Default, whenever no action phase is in progress; also during instant non-weapon-skill abilities | DEF, MEVA, HP, EVA, DT−, Enmity+ (CE generation), Concentration, Regen | DT− gear −30%; Enmity+ −50% to +50%; Concentration 60 |
| Fast Cast | Only at the chant start tick, to compute CT_eff | Fast Cast, Concentration (ignored, not worn during chant), MP | FC gear 50%; total FC 65% |
| Mid-Cast/Burst | Whole chant duration and the resolution tick; also instant (CT 0) spells and heals | INT, MACC, Magic Burst Damage, Crit DMG, magic Crit rate, Healing Potency, Concentration | MBD +40%; Crit DMG +1.00; Healing Potency +50%; Concentration 60 |
| Weapon Skill | Execution tick of any Physical or Elemental-Physical ability with CT 0 | STR, DEX, Multi-Attack (DA/TA), WS Damage, Crit rate, Crit DMG, ACC | STR/DEX each 30% of base ATK; DA 50%; TA 25%; WS Damage +40%; Crit DMG +1.00 |

### 2.13.2 Swap timing rules

1. Swaps are instantaneous, cost no AP, and cannot fail.
2. Loadout contents cannot be edited during combat; Tactical Pause shows them read-only.
3. **Chant sequence:** start tick → Fast Cast set (CT locked) → Mid-Cast set for the entire chant (all damage taken, interrupt checks and Concentration use Mid-Cast defenses) → resolution with Mid-Cast → Recovery paid → Idle/Engaged.
4. **Weapon skill sequence:** execution tick → Weapon Skill set → hit, crit, multi-attack and enmity computed with WS set (so Idle-set Enmity+ does NOT apply) → Idle/Engaged.
5. **Instant non-weapon-skill abilities** (taunts, stances, instant buffs such as Warcry of the Deep) never leave Idle/Engaged, so Idle-set Enmity+ applies to them.
6. **Counters** (Answering Toll, Geode Reprisal) resolve in Idle/Engaged.
7. **Interrupted chant:** the hero returns to Idle/Engaged on the interrupt tick.
8. **Damage taken while not acting** always uses Idle/Engaged.

### 2.13.3 Design intent

The swap rules create three honest trade-offs: a caster in heavy Mid-Cast gear is squishier during its chant (interrupt risk); a tank weapon-skilling in its WS set loses Enmity+ for that action; Fast Cast gear only matters for one tick, so it competes for chip sockets with nothing else.

---

## 2.14 Status effect reference

| Status | Effect | Default duration | Cap / stacking |
|---|---|---|---|
| Haste | +X% AGI | Per ability | Sum capped at +50% |
| Slow | −X% AGI | Per ability | Sum capped at −50% |
| Stun | No AP gain; chants interrupted | 500–1,500 ticks | Bosses gain 50% stun resistance for 6,000 ticks after each stun |
| Silence | Cannot begin chants | 2,000 ticks | Refreshes |
| Blind | −ACC flat | 4,000 ticks | Highest applies |
| ATK Down | −X% ATK | 5,000 ticks | Highest applies |
| INT Down | −X% INT | 6,000 ticks | Highest applies |
| DEF Shatter / DEF Break | −X% DEF | 4,000 / 6,000 ticks | Highest applies |
| MEVA Down | −X% MEVA | 6,000 ticks | Highest applies |
| Burn / Burn III | Fire DoT | 4,000 ticks | Refreshes |
| Shock | Lightning DoT, −10% AGI (Static Lattice version) | 3,000 ticks | Refreshes |
| Regen | HP per 500 ticks | Per ability | Separate sources stack |
| Absorb shield | Absorbs damage | Per ability | Separate sources stack; consumed oldest first |
| DT reduction buffs | −X% damage taken | Per ability | Sum with gear DT− capped at −60% |

---

## 2.15 Worked example combat log

### 2.15.1 Setup

Training-arena fight (no Aether Density Meter). Party: Korrith Vael-Dun (slot 1, Anchor), Mirrim Ash-Pounce (slot 2), Zeph Tri-Lumen (slot 3), Seraphine Vol-Ivory (slot 4).

**Boss: Carapace Engine Mk. II** — AGI 12, ATK 520, DEF 420, INT 380, MEVA 300, ACC 300, EVA 140, Concentration 40. Resistances: Ice +10%, Fire +30%, all others 0%. Sub-targets: Core 120,000 HP (owns Overpressure Lance), Weapon Arm 45,000 HP (owns Piston Sweep), Shield 30,000 HP (inactive in this excerpt).

Boss abilities used:

- **Piston Sweep** (Weapon Arm): Physical, 1.60 × ATK, Standard recovery, CT 0.
- **Overpressure Lance** (Core): Magical Fire, 2.20 × INT, Standard recovery, CT 1,200.

Loadouts used:

| Hero | Idle/Engaged | Fast Cast | Mid-Cast/Burst | Weapon Skill |
|---|---|---|---|---|
| Korrith | DEF +80, Enmity+ 10% | none | none | STR +40 |
| Mirrim | EVA +20 | none | none | DEX +50, Double Attack 10% |
| Zeph | DEF +40 | Fast Cast 30% | INT +60, MBD +20%, Crit DMG +0.20 | none |
| Seraphine | MEVA +30 | Fast Cast 30% (+15% race = 45%) | none | none |

Initial AP (`min(6,000, AGI × 300)`): Korrith 2,700; Mirrim 5,400; Zeph 3,900; Seraphine 3,900; Boss 3,600.

Constants referenced: boss physical DR = 420 / 920 = 45.65%; boss magical DR = 300 / 900 = 33.33%.

**RNG.** PCG-XSH-RR encounter seed `20261002`, one stream each for hit, crit, multi-attack, interrupt, proc, and AI. Every roll this fight draws is printed below (`roll N`). A Magic Burst does not draw a hit roll (`no roll`). A 0% interrupt does not draw an interrupt roll. `ScriptedRng` remains the replay tool when some future excerpt omits a roll; this log does not omit any.

### 2.15.2 Log

| Tick | Actor | Event | Math | Result |
|---|---|---|---|---|
| 256 | Mirrim | Ready (AP 5,400 + 256 × 18 = 10,008). Talon Lance on Core. WS set on. | ATK_eff = 415 + 50 DEX = 465. ACC_eff = 308 + 50 = 358. Hit% = clamp(0.75 + (358 − 140)/200) = 0.95; roll 9,331 hit. Crit = 0.05 + 218/2,000 + 50 × 0.0004 = 17.9%; roll 3,082 no. DA 10%; roll 9,647 no. Dmg = 465 × 2.00 × (1 − 0.4565) = 505.43 | **505** Piercing to Core. Core chain: L1(Piercing), window 256 → 4,256. Mirrim Core CE = 500 + floor(505 × 0.08) = **540**. AP 10,008 − 10,000 = 8. Boss AP 6,672: Pounce on the Upbeat not triggered (needs 8,000–9,999). |
| 470 | Zeph | Ready (3,900 + 470 × 13 = 10,010). Starts Blizzard II on Core. | FC set: CT_eff = ceil(1,000 × (1 − 0.30)) = 700. Interrupt node placed at tick 1,170. Swap to Mid-Cast. MP 540 − 180 = 360. | Casting; AP frozen at 10,010. |
| 470 | Seraphine | Ready (10,010, same tick; slot 4 after slot 3). Circuit Benediction on Zeph. | CT_eff = ceil(400 × (1 − 0.45)) = 220 → node at 690. MP 990 − 90 = 900. | Casting. |
| 500 | — | VE decay step. | No hero holds VE yet. | No change. |
| 534 | Boss (Weapon Arm) | Ready (3,600 + 534 × 12 = 10,008). Piston Sweep. Arm table empty → falls back to Core table: argmax = Mirrim (540). | Hit% = 0.75 + (300 − 263)/200 = 0.935; roll 9,185 hit. Crit 6.85%; roll 3,338 no. Dmg = 520 × 1.60 × (1 − 270/770) = 540.26 | **540** to Mirrim (4,480 → 3,940). Unmitigated direct hit sheds the **selecting** table (Core): CE_shed = floor(540 × min(0.25, 0.5 × 540 / 4,480)) = floor(540 × 0.0603) = 32. Mirrim Core CE **508**. Boss AP 8. |
| 690 | Seraphine | Circuit Benediction resolves on Zeph. | Regen 0.15 × 373 = 55 HP per 500 ticks for 4,000 ticks, measured from this resolution (pulses at 1,190, 1,690, …). Zeph Concentration +20 lasts the same window and expires with the regen at 4,690. Seraphine Core VE +300; CE 100 × 0.60 = 60. AP 10,010 − 4,000 = 6,010. | Zeph Concentration 20 (his Blizzard II chant is now safer). |
| 812 | Mirrim | Ready (8 + 556 × 18 = 10,016; higher AP than Korrith's 10,008, so first). Frostfang Pounce on Core. | ATK_eff (Elemental-Physical) = 415 + 0.5 × 50 = 440. Hit roll 1,100 hit; crit roll 6,847 no. DA roll 8,244 no. Dmg = 440 × 1.80 × (1 − 0.4565) × (1 − 0.10 Ice res) = 387.39 | **387** Ice. Piercing → Ice = **Induration (L2)**. Detonation = floor(387 × 0.50) = **193** Ice. Boss −30% AGI for 8,000 ticks (AGI 12 → 8.4). Core burst window (Ice) 812 → 2,312. Core chain: L2(Induration), window 812 → 4,812. CE +450 + floor(580 × 0.08) = +496 → **1,004**. Physical chain link → Cadence Surge: AP 10,016 − 10,000 + 1,200 = **1,216**. |
| 812 | Korrith | Ready (2,700 + 812 × 9 = 10,008). Lattice Provoke on Core (Idle set, Enmity+ 10%). | VE +2,200; CE 300 × 1.10 = 330. | Korrith Core total 2,530 → new argmax. AP 10,008 − 4,000 = 6,008. |
| 997 | Seraphine | Ready (6,010 + 307 × 13 = 10,001). Starts Cure Cascade on Mirrim. | CT_eff = ceil(900 × 0.55) = 495 → node 1,492. MP 900 − 140 = 760. | Two allies casting (Zeph, Seraphine) and Korrith is target → **Shelter of the Chanters active** (Korrith DEF 790 → 987, CE gen +30%) from 997. |
| 1,000 | — | VE decay. | Korrith 2,200 → 1,980. Seraphine 300 → 270. | |
| 1,170 | Zeph | Blizzard II resolves on Core. Mid-Cast set: INT 606 + 60 = 666. Core has an open Ice burst window (812 → 2,312) → **Magic Burst**, and the spell also takes the chain step. | Elapsed in window 358 > 300 → Early-Window Cartography not triggered. No hit roll (Magic Burst). Base = 666 × 3.00 = 1,998. × (1 − 0.3333) = 1,332.0. Ice res bypassed. Crit = 5% + 50% = 55%; roll 1,300 **crit** × (1.50 + 0.20) = 1.70. BurstBucket = 0.50 Induration + 0.20 MBD = 0.70. 1,332.0 × 1.70 × 1.70 = 3,849.48. Chain was L2(Induration); Ice is not an Umbral Zero closer. | **3,849** Ice. Chain **restarts** L1(Ice), window 1,170 → 5,170. No detonation. Burst window stays open and extends +600 → ends **2,912**. MP refund 65% = 117 → MP 477. Zeph Core VE +300; CE 600 + floor(3,849 × 0.08) = 907. AP 10,010 − 10,000 = 10. Shelter of the Chanters ends (only Seraphine still casting). |
| 1,256 | Korrith | Ready (6,008 + 444 × 9 = 10,004). Keratin Bastion. | −35% physical damage taken for 3,000 ticks (until 4,256). VE +400; CE 900 × 1.10 = 990. MP 300 − 30 = 270. | Core: Korrith VE 2,380, CE 1,320. AP 6,004. |
| 1,300 | Mirrim | Ready (1,216 + 488 × 18 = 10,000). Timeline Stalk. | Next ability recovery −4,000. CE +200 (Core). MP 225 − 30 − 20 = 175. | Mirrim Core CE 1,204. AP 6,000. |
| 1,492 | Seraphine | Cure Cascade resolves on Mirrim. | Heal = 3.20 × 373 = 1,193; Mirrim missing 540 → restores 540. VE = floor(540 × 0.40) = 216 (under 1,500, so not heavy-tagged). | Mirrim 4,480 / 4,480. Seraphine Core VE 486. AP 10,001 − 10,000 = 1. |
| 1,500 | — | VE decay. | Korrith 2,380 → 2,142. Seraphine 486 → 437. Zeph 300 → 270. | |
| 1,523 | Mirrim | Ready (6,000 + 223 × 18 = 10,014). Talon Lance on **Weapon Arm** (separate chain state: Arm is Empty). | Boss AP = 3,344 + (1,523 − 812) × 8.4 = 9,316.4 → **Pounce on the Upbeat**: boss AP −1,500 → 7,816.4. Hit roll 1,013 hit; crit roll 4,309 no (17.9%). DA roll 9,947 no. Dmg = 505.43 | **505** Piercing to Arm. Arm chain: L1(Piercing), 1,523 → 5,523. Arm table: Mirrim CE 500 + floor(505 × 0.08) = **540**. No crit, no link → no Cadence Surge. Recovery 6,000 (Stalk): AP 10,014 − 6,000 = **4,014**. |
| 1,700 | Korrith | Ready (6,004 + 444 × 9 = 10,000). Lattice Provoke on Core. | VE +2,200; CE +330. | Korrith Core VE 4,342, CE 1,650. AP 6,000. |
| 1,783 | Boss (Core) | Ready (7,816.4 + 260 × 8.4 = 10,000.4). Starts Overpressure Lance. Core table argmax: Korrith 5,992 vs Mirrim 1,204, Zeph 1,177, Seraphine 497. | CT 1,200 → interrupt node at 2,983. | Boss casting; AP frozen. |
| 1,856 | Mirrim | Ready (4,014 + 333 × 18 = 10,008). Frostfang Pounce on Weapon Arm. | Hit roll 5,828 hit; crit roll 5,015 no. DA roll 4,493 no. Dmg = 387 (same math as tick 812). MP 175 − 30 = 145. | **387** Ice. Arm: Piercing → Ice = **Induration**. Detonation **193**. Slow refreshed until 9,856. Arm burst window (Ice) 1,856 → 3,356. Arm CE 540 + 496 = **1,036**. Physical link → Cadence Surge: AP 8 + 1,200 = **1,208**. |
| 1,939 | Zeph | Ready (10 + 769 × 13 = 10,007). Starts Blizzard II on Core. | CT_eff 700 → node 2,639 (inside the Core burst window ending 2,912). MP 477 − 180 = 297. | Casting. |
| 2,000 | — | VE decay. | Korrith 4,342 → 3,907. Seraphine 437 → 393. Zeph 270 → 243. | |
| 2,145 | Korrith | Ready (6,000 + 445 × 9 = 10,005). Seismic Maul on Core. **WS set on: Idle Enmity+ does not apply.** | ATK_eff = 273 + 40 STR = 313. Hit 0.95; roll 2,524 hit. Crit 9%; roll 2,740 no. Double Attack 0%: no roll. Dmg = 313 × 2.20 × (1 − 0.4565) = 374.24 | **374** Blunt. Core chain was L1(Ice) from the burst at 1,170; Blunt has no route from Ice → **chain restarts** at L1(Blunt), 2,145 → 6,145. The Core Ice burst window stays open to 2,912. CE 700 + 29 = 729 → Korrith Core CE 2,379. AP 5. Core HP 114,692. |
| 2,262 | Seraphine | Ready (1 + 770 × 13 = 10,011). Starts Phase Sanctuary. | CT_eff = ceil(1,500 × 0.55) = 825 → node 3,087. MP 760 − 260 = 500. | Zeph + Seraphine casting, Korrith is target → Shelter of the Chanters active again. |
| 2,345 | Mirrim | Ready (1,208 + 489 × 18 = 10,010). Timeline Stalk. | CE +200 (Core) → 1,404. MP 145 − 20 = 125. | AP 6,010. |
| 2,500 | — | VE decay (processed before actions on this tick). | Korrith 3,907 → 3,516. Seraphine 393 → 353. Zeph 243 → 218. | |
| 2,567 | Mirrim | Ready (6,010 + 222 × 18 = 10,006). Talon Lance on Core. | Boss is casting (AP frozen above 10,000) → no Pounce. Hit roll 9,012 hit; crit roll 7,738 no. DA roll 1,871 vs 1,000 no. Dmg 505. | **505** Piercing. Core chain L1(Blunt) → Piercing: no route → restarts at L1(Piercing), 2,567 → 6,567. CE +540 → 1,944. Recovery 6,000 (Stalk): AP 4,006. Core HP 114,187. |
| 2,639 | Zeph | Blizzard II resolves on Core. The Ice burst window is still open (ends 2,912) → **Magic Burst #2**, and the spell takes the chain step. | Same pre-burst base as tick 1,170: 1,332.0. No hit roll. Crit 55%; roll 2,109 **crit** ×1.70 → 2,264.4. BurstBucket 0.70 → 3,849.48. BurstDiminish 0.85 → floor **3,272**. Chain is L1(Piercing), so Ice closes **Induration**. Detonation = floor(3,272 × 0.50) = **1,636**. Interrupt% = (3,272 / 120,000) × 2.5 − 0.40 = 0.068 − 0.40 → clamp **0%** (no interrupt roll). | **3,272** Ice plus detonation **1,636**. The detonation **replaces** the burst window: the would-be +600 extension is discarded, and a fresh Ice window runs 2,639 → **4,139**. Chain L2(Induration) until 6,639. Slow refreshed until 10,639. MP refund 117 → 414. CE 600 + floor(4,908 × 0.08) = 992 → Zeph Core CE **1,899**; VE 218 + 300 = 518. AP 7. Shelter of the Chanters ends. Core HP 109,279. |
| 2,900 | Mirrim | Ready (4,006 + 333 × 18 = 10,000). Frostfang Pounce on Core. Boss still casting → no Pounce. | Hit roll 8,959 hit. Crit 17.9%; roll 24 **crit** ×1.50. DA roll 8,567 no. 387.39 × 1.50 = 581.08. The open window is Ice only, so this elemental-physical hit is not a Physical Burst. MP 125 − 30 = 95. | **581** Ice. Chain was L2(Induration); Ice is not an Umbral Zero closer → **restarts** L1(Ice), 2,900 → 6,900. Detonation 0. The burst window from 2,639 stays open until 4,139. CE 450 + floor(581 × 0.08) = 496 → **2,440**. Crit → Cadence Surge: AP 0 + 1,200 = **1,200**. Core HP 108,698. |
| 2,983 | Boss (Core) | Overpressure Lance resolves on Korrith. Korrith in Idle set. | MACC = 0.5 × 380 + 0.5 × 300 = 340; MagicHit = 0.75 + (340 − 240)/200 = 1.25 → 0.95; roll 6,667 hit. Crit 5%; roll 2,596 no. Dmg = 836 × (1 − 240/840) = 597.14; Fire res 0. | **597** to Korrith (9,240 → 8,643). Keratin Bastion covers physical only → **unmitigated**: CE_shed = floor(2,379 × min(0.25, 0.5 × 597 / 9,240)) = floor(2,379 × 0.0323) = **76** → Korrith Core CE 2,303. Magical, so no Chitinous Grounding VE. Boss AP 0.4. |
| 3,000 | — | VE decay. | Korrith 3,516 → 3,164. Seraphine 353 → 317. Zeph 518 → 466. | |
| 3,087 | Seraphine | Phase Sanctuary resolves. | Party −25% damage taken until 6,087. VE +1,400 into the **heavy bucket** (decays 20% per 500 ticks); CE 200 × 0.60 = 120. AP 10,011 − 14,000 = −3,989 (debt). | Seraphine Core VE 317 + 1,400 = 1,717. |

### 2.15.3 State at tick 3,087

| Unit | HP | MP | AP | Core VE | Core CE | Core total | Arm table |
|---|---|---|---|---|---|---|---|
| Korrith | 8,643 / 9,240 | 270 | 8,483 | 3,164 | 2,303 | **5,467** | 0 |
| Mirrim | 4,480 / 4,480 | 95 | 4,566 | 0 | 2,440 | 2,440 | **1,036** |
| Zeph | 3,710 / 3,710 | 414 | 5,831 | 466 | 1,899 | 2,365 | 0 |
| Seraphine | 4,750 / 4,750 | 500 | −3,989 | 1,717 | 180 | 1,897 | 0 |
| Boss | Core 108,698 / 120,000; Arm 43,915 / 45,000 | — | 874 (slowed, 8.4 AP per tick) | | | | |

Core chain is L1(Ice) until 6,900. The Core Ice burst window is open until 4,139. Induration slow lasts until 10,639. The Weapon Arm chain is L2(Induration) until 5,856, and its Ice burst window is still open until 3,356. Shield is untouched at 30,000.

### 2.15.4 What the log teaches (tutorial callouts)

1. **Chant timing beats reaction:** Zeph started Blizzard II at tick 470, before the chain even existed, so it would resolve inside the burst window Mirrim was about to open.
2. **Per-part enmity:** the Weapon Arm table is owned by Mirrim (1,036 vs 0). The next Piston Sweep hits Mirrim unless Korrith uses Tessellate-style all-parts provocation or hits the Arm. The HUD's per-part threat bars flag this in amber.
3. **Gear swap trade-off:** Korrith's Seismic Maul generated 729 CE instead of 799 because his WS set carries no Enmity+.
4. **Mitigation is typed:** Keratin Bastion did not stop the magical Lance from shedding CE.
5. **Boss interrupts need Stuns:** 3,272 damage produced 0% interrupt chance against Concentration 40. The detonation is not part of that interrupt check.

---

## 2.16 Godot C# implementation notes

### 2.16.1 Deterministic simulation library

The combat rules live in a pure C# class library, `Resonance.Sim` (net8.0, no `Godot` namespace references), so the same code runs in the Godot client, in the headless arena validator and in unit tests.

- All stats are `int`. Percentages are **basis points** (`int`, 10,000 = 100%).
- AP is stored in **centi-AP** (`int`, 1,000,000 = 10,000 AP) so fractional AGI from Haste/Slow (8.4 AGI → 840 centi-AP per tick) is exact.
- **Division truncates** (floor toward zero) everywhere a formula does not explicitly say ceil. Odd inputs of `(INT + ACC) / 2` and `0.5 × STR` or `0.5 × DEX` drop the fraction. Chant time and Heat gain still ceil, because those formulas say ceil.
- Ratios such as `DEF / (DEF + 500)` are computed as `DEF * 10000 / (DEF + 500)`, giving basis points. The damage product is floored **once** at pipeline end (step 9). Ten basis-point factors overflow `Int128` if multiplied before that division, so the pipeline currently multiplies in `BigInteger`. That is a known performance risk for the later zero-alloc pass; it is not a float.
- No `float`/`double` anywhere in `Resonance.Sim`. A Roslyn analyzer rule (banned API list) fails the build if `System.Single`, `System.Double` or `System.Math` floating overloads appear in the Sim assembly.
- RNG: `Pcg32` struct seeded per encounter; separate streams for hit, crit, multi-attack, interrupt, proc and AI so adding a new roll type never shifts other streams.

### 2.16.2 Event-driven time skipping (performance)

Iterating 10,000+ ticks one at a time is wasteful. The sim computes the next interesting tick and jumps:

```
nextTick = min(
    for each unit not casting: currentTick + ceil((1,000,000 − apCenti) / gainPerTick),
    next chant resolution,
    next multiple of 500 (VE decay / DoT phase),
    next status/window expiry,
    next Aether Density step (dungeon))
```

AP of all units advances by `gainPerTick × (nextTick − currentTick)` in one multiply. Typical fights resolve in 150–400 events instead of 30,000+ ticks. The arena validator uses the same loop, which is what makes server re-simulation cheap (06 §6.3).

### 2.16.3 Class outline

```
Resonance.Sim/
  Core/
    SimClock                  // current tick, next-event scheduler (binary heap of TimedEvent)
    ApGauge                   // centi-AP, debt floor, refund ceiling
    UnitState (struct)        // stats, AP, HP, MP, casting, statuses, heat, loadout index
    BattleState               // arrays of UnitState, boss parts, chain states, burst windows
    Pcg32 (struct)            // seeded RNG with stream ids
  Combat/
    TimelineSystem            // steps 1 and 4 of the tick order, tie-break comparer
    ChantSystem               // CT_eff, interrupt nodes, interrupt rolls
    DamagePipeline            // physical, elemental-physical, magical, true, heal pipelines
    HitResolver               // hit/crit/multi-attack rolls
    EnmitySystem              // per-part tables, VE buckets, decay, shed, argmax
    ResonanceSystem           // L1/L2/L3 transitions (static lookup table), detonations
    BurstSystem               // windows, qualification, BurstBucket, diminish, extensions
    StatusSystem              // durations, pulses, caps
    LoadoutSystem             // which of the 4 loadouts is active, derived stat cache per loadout
    RacialPassives            // Chitinous, Cadence, Condenser, Thermal, Phase Dampener hooks
  Data/
    AbilityDef, HeroDef, RaceDef, BossDef, LoadoutDef, ChipDef   // immutable records
    ResonanceTable            // generated from docs tables into a static readonly array [12,11]
  Events/
    CombatEvent (struct)      // tick, type, actor, target, values; written to a ring buffer
```

Key rule: systems are stateless static classes or sealed classes operating on `BattleState`; no LINQ, no allocations in the hot path (pre-sized arrays, `Span<T>`), no virtual dispatch in per-event loops.

### 2.16.4 Bridging to Godot

- An Autoload `SimBridge` (C# `Node`) owns the `BattleState` and advances it from `_Process` when not paused, converting sim ticks to presentation time (default 1,000 ticks per real second at 1× speed; 2× and 4× options).
- The sim writes `CombatEvent` structs into a ring buffer. Once per frame, `SimBridge` drains the buffer and emits **batched** C# events / Godot signals (`EventsDrained(CombatEvent[] batch)`), so there is one managed-to-engine boundary crossing per frame rather than per event.
- Presentation nodes (timeline bar, oscilloscope, math console) subscribe to `SimBridge` via C# `event` delegates (avoids Variant marshaling that `EmitSignal` incurs), while scene-level hookups that designers edit in the inspector use `[Signal]` delegates.
- Godot `Resource` classes (`[GlobalClass] public partial class AbilityDefinition : Resource`) are authored in the editor and compiled at load into the immutable `Resonance.Sim.Data` records by a `DataCompiler`, so the sim never touches Godot types.
