# 05 — Dungeon Progression: Resonance Shafts, Aether Density, Extraction, Loot and Schematic Chips

---

## 5.1 Run structure

A **run** is one descent into a Resonance Shaft (region list in 01 §1.9) with a squad of 4–6 heroes.

### 5.1.1 Floors and blocks

- Shafts have 12, 15 or 18 floors.
- Floors are grouped in **blocks of three**. Floor 3 of every block (floors 3, 6, 9, 12, 15, 18) ends with a **Floor Boss** and then a **Decision Node: Extract or Delve Deeper**.
- Each floor is a node graph of 5–7 rooms generated from the run seed.

### 5.1.2 Room types

| Room | Frequency per floor | Content | Aether Density effect |
|---|---|---|---|
| Combat | 2–3 | 3–5 trash enemies, 6,000–10,000 ticks typical | Combat-time rise only |
| Elite | 0–1 (1 guaranteed on floors 2, 5, 8, 11, 14, 17) | One elite with 2 sub-targets | +1.0 on entry |
| Event | 1 | Narrative choice with a mechanical trade (examples in 5.1.3) | Varies |
| Rest | 1 per block (on floor 2 of each block) | Full HP/MP restore; −5.0 Aether Density | −5.0 |
| Vault | 0–1 | Free loot chest; guaranteed Schematic Chip | none |
| Floor Boss | 1 on floors 3, 6, 9, 12, 15, 18 | Boss with 3 sub-targets (Core, Shield, Weapon Arm) | +1.0 on kill |

### 5.1.3 Event room examples (all numbers binding)

| Event | Option A | Option B |
|---|---|---|
| Unstable Reliquary | Take a Rare chip, +6.0 Aether Density | Leave it |
| Reclaimer Toll | Pay 20% of current run Aether Shards: −8.0 Aether Density | Refuse |
| Stalker Scout | Reveal the next floor's elite position and element | +150 Aether Shards |
| Choir Resonance Column | Every hero +10% ATK and INT for the rest of the floor, +4.0 Aether Density | Every hero restores 30% MP |
| Stabilizer Cache | Gain 1 Stabilizer Beacon (−15.0 Aether Density on use) | Gain 1 Refinement Stabilizer (5.6.6) |

---

## 5.2 Aether Density Meter (ADM)

### 5.2.1 Scale

- Range 0.0 to 100.0, stored in the sim as an integer in tenths (0–1,000).
- Persists across all floors of a run; resets to 0 at run start.
- **Floor minimum:** ADM cannot be reduced below `2.0 × current floor number` (floor 9 minimum 18.0).

### 5.2.2 Exact rise rates

| Source | Rise |
|---|---|
| Combat time | +0.05 per 1,000 combat ticks (applied at each 1,000-tick boundary of the fight clock) |
| Party Level 2 Resonance Detonation | +0.1 each |
| Party Level 3 Apex Detonation | +0.5 each |
| Hero KO | +2.0 each |
| Descend to a new floor | +1.5 |
| Enter Elite room | +1.0 |
| Floor Boss defeated | +1.0 |
| Event choices | As listed in 5.1.3 |

| Source | Reduction |
|---|---|
| Rest room | −5.0 |
| Stabilizer Beacon (consumable) | −15.0 |
| Reclaimer Toll event | −8.0 |

### 5.2.3 Expected curve (reference party, no Beacons)

Assumptions per floor: 60,000 combat ticks (+3.0), 20 L2 detonations (+2.0), 3 L3 detonations (+1.5), descend (+1.5), one elite (+1.0): about +9.0 per floor, minus 5.0 from each block's Rest room, plus 1.0 per boss.

| After floor | Expected ADM | Band |
|---|---|---|
| 3 | 23.0 | Stable |
| 6 | 46.0 | Charged |
| 9 | 69.0 | Volatile |
| 12 | 92.0 | Critical |
| 15 | 100.0 (capped) | Meltdown |
| 18 | 100.0 | Meltdown |

### 5.2.4 Bands and fallout tick penalties

A **Fallout Tick** is a periodic hazard pulse during combat, scheduled on the fight clock (first pulse at the interval, not at tick 0). Fallout damage is true damage and cannot kill (leaves 1 HP); it does not cause CE shed (not a direct hit) and does not interrupt chants.

| Band | ADM range | Fallout interval | AP penalty per pulse (all heroes) | HP penalty per pulse | Other effects |
|---|---|---|---|---|---|
| Stable | 0.0 – 24.9 | none | — | — | — |
| Charged | 25.0 – 49.9 | every 3,000 ticks | −200 AP | none | Detonation damage +10% (both sides) |
| Volatile | 50.0 – 74.9 | every 2,000 ticks | −400 AP | 1% Max HP | Detonation damage +20%; Crag Moor-Ash's Thick Air active |
| Critical | 75.0 – 99.9 | every 1,500 ticks | −600 AP | 2% Max HP | Detonation damage +30%; hero Interrupt% +10 percentage points |
| Meltdown | 100.0 | every 1,000 ticks | −1,000 AP | 4% Max HP | Detonation damage +40%; healing received −25%; bosses start fights already Frenzied |

AP penalties respect the −4,000 AP debt floor. Fallout pulses land in step 2 of the tick order (02 §2.2.3).

### 5.2.5 Boss frenzy scaling

Applied to every elite and Floor Boss at spawn, using the ADM value at spawn:

```
ATK_boss   = ATK_base   × (1 + ADM / 200)
INT_boss   = INT_base   × (1 + ADM / 200)
AGI_boss   = AGI_base   × (1 + ADM / 400)
HP_part    = HP_base    × (1 + ADM / 250)
Concentration = min(90, Concentration_base + floor(ADM / 4))
FrenzyThreshold (Core HP%) = 25 + ADM / 4
```

**Frenzy state** (entered when Core HP% ≤ FrenzyThreshold, or immediately at Meltdown):

- ATK and INT +15% (multiplicative on the scaled values).
- Every boss action's Recovery Cost is reduced by `1,000 + 20 × ADM` AP (minimum recovery 4,000).
- Every 3rd boss action in Frenzy is **Density Rupture**: Magical, element = the shaft's first bias element, 0.80 × INT to every hero, Chant Time 1,500, Concentration 90.
- **Hard enrage** at 90,000 total fight ticks: ATK ×3 and INT ×3.

Worked table for the Carapace Engine Mk. II (base ATK 520, AGI 12, Core HP 120,000, Concentration 40):

| ADM | ATK | AGI | Core HP | Concentration | Frenzy threshold | Frenzy recovery reduction |
|---|---|---|---|---|---|---|
| 0 | 520 | 12.00 | 120,000 | 40 | 25.0% | −1,000 |
| 25 | 585 | 12.75 | 132,000 | 46 | 31.25% | −1,500 |
| 50 | 650 | 13.50 | 144,000 | 52 | 37.5% | −2,000 |
| 75 | 715 | 14.25 | 156,000 | 58 | 43.75% | −2,500 |
| 100 | 780 | 15.00 | 168,000 | 65 | 50.0% | −3,000 |

(Fractional AGI is exact in the sim via centi-AP, 02 §2.16.1.)

### 5.2.6 Upside of density

- **Loot multiplier** on all run yields: `1 + ADM / 100` (×2.0 at 100), evaluated at the moment each yield drops.
- **Legendary Prototype chance** multiplier: `1 + ADM / 50` (×3.0 at 100), see 5.4.
- Detonation damage bonus from the band table applies to the party as well as enemies.

---

## 5.3 Extract vs Delve Deeper

### 5.3.1 Decision Node

After every Floor Boss (floors 3, 6, 9, 12, 15, 18) the squad reaches a Decision Node:

| Option | Effect |
|---|---|
| **Extract** | Run ends successfully. 100% of unbanked run yields are banked to the account. |
| **Delve Deeper** | Continue to the next block. ADM carries over. The next block's yield multiplier increases (5.3.2). HP/MP are not restored (the Rest room of the next block restores them). |

On the final floor of a shaft, Extract is automatic.

### 5.3.2 Delve multiplier

Yields dropped during block `b` (block 1 = floors 1–3) are multiplied by:

```
DelveMultiplier(b) = 1 + 0.25 × (b − 1)
TotalYieldMultiplier = DelveMultiplier(b) × (1 + ADM / 100)
```

| Block | Floors | Delve multiplier | Average ADM loot multiplier on the expected curve | Total |
|---|---|---|---|---|
| 1 | 1–3 | 1.00 | 1.12 (average ADM 11.5) | 1.12 |
| 2 | 4–6 | 1.25 | 1.35 (average ADM 34.5) | 1.69 |
| 3 | 7–9 | 1.50 | 1.58 (average ADM 57.5) | 2.37 |
| 4 | 10–12 | 1.75 | 1.81 (average ADM 80.5) | 3.17 |
| 5 | 13–15 | 2.00 | 1.98 (average ADM 98, capped at 100) | 3.96 |
| 6 | 16–18 | 2.25 | 2.00 (ADM 100) | 4.50 |

### 5.3.3 Wipe penalty

A wipe (all heroes KO'd) ends the run:

- **Stackable currencies** (Aether Shards, Schematic Fragments, Prototype Cores): the unbanked amount is halved, rounded down: `kept = floor(unbanked × 0.50)`.
- **Items** (gear, chips, Legendary Prototypes): the player keeps `ceil(n × 0.50)` of the `n` unbanked items, choosing which ones on the **Salvage Screen**. The remaining items are converted to Schematic Fragments at 25% of their salvage value.
- Hero XP earned during the run is always kept (XP is never at risk).

### 5.3.4 Emergency extraction

A **Beacon Extract** consumable (crafted from 3 Prototype Cores) can be used at any Rest room to extract mid-block, banking 70% of unbanked yields (30% cost). Maximum one per run.

### 5.3.5 Run yields per source

| Source | Aether Shards | Schematic Fragments | Prototype Cores | Items |
|---|---|---|---|---|
| Combat room | 40 × floor | 1 | 0 | 30% chance: 1 gear piece |
| Elite room | 120 × floor | 3 | 0 | 1 gear piece + 40% chance: 1 chip |
| Vault | 200 × floor | 2 | 0 | 1 guaranteed chip |
| Floor Boss | 400 × floor | 8 + floor(floor / 3) | 1 (floors ≥ 6) | 2 gear pieces + 1 chip + Legendary roll |

All values are multiplied by TotalYieldMultiplier (rounded down).

### 5.3.6 Item rarity weights by block

| Block | Common | Uncommon | Rare | Epic | Legendary (chips only) |
|---|---|---|---|---|---|
| 1 | 60% | 30% | 9% | 1% | 0% |
| 2 | 45% | 35% | 16% | 4% | 0% |
| 3 | 30% | 38% | 24% | 7.5% | 0.5% |
| 4 | 20% | 35% | 32% | 12% | 1% |
| 5 | 10% | 30% | 38% | 19% | 3% |
| 6 | 5% | 22% | 40% | 28% | 5% |

---

## 5.4 Legendary Prototype drops

### 5.4.1 Drop rate formula

```
P(drop) = min(30%, (BaseRate(source) × (1 + ADM / 50)) + Pity)
Pity = 0.25 percentage points × (eligible kills since last Legendary Prototype drop), account-wide, reset to 0 on drop
```

Eligible sources are Floor Bosses and Elites only. Elite base rate = 20% of the Floor Boss base of the same block.

### 5.4.2 Floor Boss rates (before pity)

| Floor boss | Base | ADM 0 | ADM 25 | ADM 50 | ADM 75 | ADM 100 |
|---|---|---|---|---|---|---|
| Floor 3 | 0.50% | 0.50% | 0.75% | 1.00% | 1.25% | 1.50% |
| Floor 6 | 1.25% | 1.25% | 1.88% | 2.50% | 3.13% | 3.75% |
| Floor 9 | 2.50% | 2.50% | 3.75% | 5.00% | 6.25% | 7.50% |
| Floor 12 | 4.00% | 4.00% | 6.00% | 8.00% | 10.00% | 12.00% |
| Floor 15 | 6.00% | 6.00% | 9.00% | 12.00% | 15.00% | 18.00% |
| Floor 18 | 8.00% | 8.00% | 12.00% | 16.00% | 20.00% | 24.00% |

### 5.4.3 Elite rates (before pity)

| Elite in block ending on | Base | ADM 0 | ADM 25 | ADM 50 | ADM 75 | ADM 100 |
|---|---|---|---|---|---|---|
| Floor 3 | 0.10% | 0.10% | 0.15% | 0.20% | 0.25% | 0.30% |
| Floor 6 | 0.25% | 0.25% | 0.38% | 0.50% | 0.63% | 0.75% |
| Floor 9 | 0.50% | 0.50% | 0.75% | 1.00% | 1.25% | 1.50% |
| Floor 12 | 0.80% | 0.80% | 1.20% | 1.60% | 2.00% | 2.40% |
| Floor 15 | 1.20% | 1.20% | 1.80% | 2.40% | 3.00% | 3.60% |
| Floor 18 | 1.60% | 1.60% | 2.40% | 3.20% | 4.00% | 4.80% |

Pity example: after 40 eligible kills without a drop, pity is +10.0 points, so a Floor 12 boss at ADM 75 rolls at 10.00% + 10.00% = 20.00%.

### 5.4.4 Legendary Prototype catalog (12 items)

Legendary Prototypes are gear pieces with fixed stats and a unique effect. Duplicates convert to 5 Prototype Cores.

| # | Name | Slot | Loadout affinity | Stats | Unique effect |
|---|---|---|---|---|---|
| 1 | Chronometric Carapace | Body | Idle/Engaged | DEF +120, HP +600 | The first unmitigated hit taken every 6,000 ticks is treated as mitigated (no CE shed). |
| 2 | Tri-Lumen Occulus | Head | Mid-Cast/Burst | INT +70, MBD +10% | Magic Bursts that resolve within the first 200 ticks of a burst window deal +15% damage. |
| 3 | Seventh Second Greaves | Legs | Weapon Skill | DEX +40, Crit rate +5% | Sylvari-Mor: Cadence Surge refunds +300 AP more. Other races: a crit refunds 600 AP once per 4,000 ticks. |
| 4 | Kiln-Heart Gauntlets | Hands | Weapon Skill | STR +45 | Ash-Dravan: Heat gain ×1.5 and Primed weapon skills +20% damage. Other races: weapon skills into an open Fire window +15% damage. |
| 5 | Choir-Bell Focus | Focus | Idle/Engaged | Concentration +15, Enmity+ +10% | 5% of physical damage taken is added as VE (stacks additively with Chitinous Grounding to 20%). |
| 6 | Null Theorem Codex | Focus | Mid-Cast/Burst | MACC +40 | Flat-interrupt abilities (Vector Stun, Shield Bash) gain +10 percentage points of interrupt chance. |
| 7 | Porcelain Requiem | Body | Mid-Cast/Burst | Healing Potency +15%, INT +30 | Heals restoring ≥ 1,500 HP also grant the target an absorb shield of 10% of the amount healed for 3,000 ticks. |
| 8 | Apex Resonator | Focus | Any | none | L3 detonations you close deal +30% detonation damage and extend the resulting burst window by 300 ticks (this extension counts toward the +1,200 cap). |
| 9 | Fractal Spur | Hands | Fast Cast | Fast Cast +8% | Chants whose CT_eff is ≤ 500 ticks cost 15% less MP. |
| 10 | Aquifer Mantle | Body | Idle/Engaged | MP +200, MP Regen +10 per 500 ticks | Water-property abilities you use deal +20% damage. |
| 11 | Tempest Crown Sigil | Head | Any | ACC +20, MACC +20 | When a Tempest Crown you participated in detonates, you gain 2,500 AP instead of 2,000. |
| 12 | Umbral Keystone | Focus | Weapon Skill | WS Damage +8% | Darkness-property weapon skills deal +25% damage against a target whose chain state is L2(Distortion) or L2(Induration). |

---

## 5.5 Gear (non-legendary)

Gear pieces drop with a slot, a loadout affinity and 1–4 stat lines by rarity:

| Rarity | Stat lines | Line value range (percent of the Legendary benchmark for that stat) |
|---|---|---|
| Common | 1 | 25–40% |
| Uncommon | 2 | 35–55% |
| Rare | 3 | 50–70% |
| Epic | 4 | 65–90% |

Legendary benchmark per stat (the 100% value): DEF 120, HP 600, INT 70, STR 45, DEX 40, MACC 40, ACC 40, EVA 40, MEVA 60, MP 200, Fast Cast 10%, MBD 10%, Crit rate 5%, Crit DMG 0.20, WS Damage 8%, DA 10%, TA 5%, Concentration 15, Enmity+ 10%, DT− 6%, Healing Potency 15%.

---

## 5.6 Schematic Chips

### 5.6.1 What chips are

Schematic Chips are socketable modules with a **trigger** and an **effect**. Each loadout (02 §2.13) has 2 chip sockets, so every hero has 8 sockets across the four loadouts.

### 5.6.2 Socket rules

1. A chip's **affinity** must match the loadout (Idle/Engaged, Fast Cast, Mid-Cast/Burst, Weapon Skill) or be **Any**.
2. The same chip name cannot be socketed twice in one loadout; it can appear in different loadouts.
3. **A chip only works while its loadout is worn.** An "On Magic Burst" chip works in Mid-Cast/Burst because bursts resolve in that set; placing it in Idle/Engaged makes it inert, and the socket UI warns about this.
4. Passive chip stats count toward the gear caps (Fast Cast 50%, MBD 40%, DT− 30%, and the rest in 02 §2.13.1).
5. Internal cooldowns (ICD) are per chip instance, measured in ticks on the fight clock.
6. Chips are swapped freely outside combat; in-dungeon swapping is allowed only in Rest rooms.

### 5.6.3 Trigger stat catalog

| Trigger id | Trigger | Fires when |
|---|---|---|
| T-MB | On Magic Burst | A Magic Burst you cast resolves |
| T-L2C | On L2 Closed | Your ability produces a Level 2 detonation |
| T-L3P | On L3 Party | Any party Level 3 detonation |
| T-LINK | On Physical Link | Your Physical/Elemental-Physical ability produces a valid L2/L3 transition |
| T-CRIT | On Crit | You land a critical hit |
| T-OPEN | On Window Open | Your ability opens a Level 1 window |
| T-UNMIT | On Unmitigated Hit | You take a direct unmitigated hit |
| T-INT | On Interrupt | Your ability interrupts an enemy chant |
| T-LOWHP | While Low HP | While your HP < 30% |
| T-PASS | Passive | Always, while the loadout is worn |
| T-GRANT | Grants Ability | Adds an ability to your ability list while socketed |
| T-CONV | Conversion | Changes a property or damage type of one of your abilities |

### 5.6.4 Chip catalog with values by rarity (+0 refinement)

| # | Chip | Affinity | Trigger | Effect | Common | Uncommon | Rare | Epic | Legendary | ICD |
|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Bulwark Bash | Idle/Engaged | T-GRANT | Grants **Shield Bash**: Physical, Blunt, Stance recovery, CT 0, MP 0, 0.80 × ATK, VE 0 / CE 400, flat interrupt chance: | 35% | 40% | 45% | 50% | 60% | none |
| 2 | Tidal Edge | Weapon Skill | T-CONV | `USE [Attack]` becomes Elemental-Physical Water with multiplier 1.00 + | 0.10 | 0.20 | 0.30 | 0.40 | 0.60 | none |
| 3 | Burst Siphon | Mid-Cast/Burst | T-MB | Additional MP refund (total cap 80%) | +5% | +7% | +9% | +11% | +15% | none |
| 4 | Resonant Lens | Mid-Cast/Burst | T-MB | INT for 2,000 ticks (refreshes) | +4% | +6% | +8% | +10% | +14% | none |
| 5 | Condenser Coil | Mid-Cast/Burst | T-PASS | Magic Burst Damage | +3% | +4% | +5% | +6% | +8% | none |
| 6 | Swiftchant Filament | Fast Cast | T-PASS | Fast Cast | +3% | +4% | +5% | +6% | +8% | none |
| 7 | Steady Hand | Mid-Cast/Burst | T-PASS | Concentration | +4 | +6 | +8 | +10 | +14 | none |
| 8 | Linkbreaker Edge | Weapon Skill | T-LINK | ATK for 3,000 ticks (refreshes) | +5% | +6% | +8% | +10% | +14% | none |
| 9 | Keen Cadence | Weapon Skill | T-PASS | Crit rate | +2% | +3% | +4% | +5% | +7% | none |
| 10 | Fracture Driver | Weapon Skill | T-L2C | That detonation's damage | +8% | +10% | +12% | +15% | +20% | none |
| 11 | Apex Conductor | Any | T-L3P | You gain AP (refund ceiling applies) | +300 | +400 | +500 | +600 | +800 | 6,000 |
| 12 | Grounding Plate | Idle/Engaged | T-UNMIT | Absorb shield (% of Max HP) for 3,000 ticks | 3% | 4% | 5% | 6% | 8% | 2,000 |
| 13 | Threat Anchor | Idle/Engaged | T-PASS | Enmity+ | +4% | +5% | +6% | +8% | +10% | none |
| 14 | Hush Veil | Idle/Engaged | T-PASS | Enmity+ (negative) | −4% | −5% | −6% | −8% | −10% | none |
| 15 | Heat Sink | Any (Ash-Dravan only) | T-PASS | Heat gain | +10% | +12% | +15% | +18% | +25% | none |
| 16 | Dampener Mesh | Idle/Engaged | T-PASS | Your VE decays faster per 500 ticks (percentage points added to the 10% step) | +1 | +1 | +2 | +2 | +3 | none |
| 17 | Interrupt Coil | Any | T-INT | Interrupted target AP | −500 | −600 | −700 | −850 | −1,100 | none |
| 18 | Window Keeper | Any | T-OPEN | That L1 window lasts longer (ticks) | +200 | +250 | +300 | +400 | +500 | none |
| 19 | Last Stand Lattice | Idle/Engaged | T-LOWHP | DT− (counts toward gear cap 30%) | −5% | −6% | −8% | −10% | −12% | none |
| 20 | Momentum Relay | Weapon Skill | T-CRIT | Your next ability's recovery cost reduced by (AP) | 300 | 400 | 500 | 650 | 800 | once per action |

Notes:

- **Bulwark Bash** is the source of the `USE [Shield Bash]` gambit example in 04 §4.12.1.
- **Tidal Edge** is the second Water-property source in the game (the first is Tollen Geode-Vast's Aquifer Surge), enabling Water → Ice / Lightning / Darkness routes for any hero.
- **Window Keeper** extends chain windows only; burst windows are extended only by Kith-Lir and the Apex Resonator.

### 5.6.5 Refinement

Chips are refined from +0 to their rarity's maximum. Each level multiplies the chip's numeric effect.

| Level | Effect multiplier | Aether Shards | Schematic Fragments | Prototype Cores | Base success |
|---|---|---|---|---|---|
| +1 | ×1.08 | 200 | 5 | 0 | 100% |
| +2 | ×1.16 | 350 | 8 | 0 | 100% |
| +3 | ×1.24 | 500 | 12 | 0 | 95% |
| +4 | ×1.32 | 800 | 16 | 0 | 90% |
| +5 | ×1.40 | 1,200 | 22 | 1 | 80% |
| +6 | ×1.48 | 1,800 | 30 | 1 | 70% |
| +7 | ×1.56 | 2,600 | 40 | 2 | 60% |
| +8 | ×1.64 | 3,600 | 52 | 2 | 50% |
| +9 | ×1.72 | 5,000 | 68 | 3 | 40% |
| +10 | ×1.80 | 7,000 | 90 | 4 | 30% |

| Rarity | Maximum refinement |
|---|---|
| Common | +4 |
| Uncommon | +6 |
| Rare | +8 |
| Epic | +10 |
| Legendary | +10 |

Rules:

- **Failure:** materials are consumed and the level is unchanged. At +6 and above, a failure also has a 50% chance to drop the chip 1 level, unless a Refinement Stabilizer is consumed.
- **Failure pity:** each consecutive failure at the same target level adds +5 percentage points to the next attempt at that level (resets on success).
- **Rounding:** percentage effects round to 0.1 point; flat effects (AP, ticks, Concentration) round down to an integer. ICDs are never refined.
- **Caps still apply:** a refined Swiftchant Filament cannot push gear Fast Cast above 50%.
- **Fusion:** 3 copies of the same chip at the same rarity, all at that rarity's maximum refinement, fuse into 1 copy of the next rarity at +0 (Epic → Legendary is the final step).
- **Refinement RNG** uses the account's refinement stream (seeded server-side for online accounts) so refinement results cannot be re-rolled by reloading.

Worked examples:

| Chip | Rarity | Level | Base | Calculation | Final |
|---|---|---|---|---|---|
| Resonant Lens | Epic | +7 | +10% INT | 10 × 1.56 | +15.6% INT |
| Apex Conductor | Rare | +8 | +500 AP | 500 × 1.64 = 820 | +820 AP |
| Bulwark Bash | Legendary | +10 | 60% | 60 × 1.80 = 108 | 95% (clamped by interrupt cap) |
| Steady Hand | Uncommon | +6 | +6 | 6 × 1.48 = 8.88 | +8 |
| Window Keeper | Common | +4 | +200 ticks | 200 × 1.32 | +264 ticks |

### 5.6.6 Refinement consumables

| Item | Source | Effect |
|---|---|---|
| Refinement Stabilizer | Stabilizer Cache event; crafted from 2 Prototype Cores | Prevents level loss on one failed refinement at +6 or above |
| Stabilizer Beacon | Event; crafted from 1 Prototype Core + 500 Aether Shards | −15.0 Aether Density (in-run consumable) |
| Beacon Extract | Crafted from 3 Prototype Cores | Emergency extraction at 70% yields (5.3.4) |

---

## 5.7 Economy summary

| Currency | Earned from | Spent on |
|---|---|---|
| Aether Shards | All rooms | Refinement, crafting, hero rank-ups |
| Schematic Fragments | All rooms, salvage | Refinement, chip crafting (40 Fragments = 1 random Common chip) |
| Prototype Cores | Floor Bosses on floor 6+, Legendary duplicates | High-level refinement, consumables |
| Arena Marks | Ghost Protocol (06) | Cosmetic and Legendary Prototype selection tokens (one token per season: pick any Legendary) |

---

## 5.8 Godot C# implementation notes

- `FloorDefinition`, `RoomDefinition`, `EnemyGroupDefinition`, `ChipDefinition`, `LegendaryDefinition` are `[GlobalClass]` Resources under `res://data/dungeon/`, compiled by `DataCompiler` into `Resonance.Sim.Data` records (same pipeline as combat data).
- `RunGenerator` (pure C#, in `Resonance.Sim`) builds a floor graph from `(RunSeed, ShaftId, FloorIndex)` using a dedicated PCG32 stream; identical seeds produce identical shafts on every platform.
- `AetherDensitySystem` runs inside the sim tick loop (step 2 of the tick order), so fallout pulses are deterministic and replayable.
- `RunState` (a serializable POCO: floor, ADM in tenths, yields, unbanked items, hero HP/MP, consumables, RNG stream positions) is saved to `user://runs/current.json` after every room, with a SHA-256 checksum to discourage save editing. Online accounts mirror `RunState` to the backend at each Decision Node.
- `LootSystem.RollLegendary(source, adm, pity, rng)` is a pure function; the pity counter lives in the account profile, not in the run.
- The Salvage Screen (wipe) is a `Control` scene `salvage_screen.tscn` that receives the unbanked item list and enforces `ceil(n / 2)` selections.
