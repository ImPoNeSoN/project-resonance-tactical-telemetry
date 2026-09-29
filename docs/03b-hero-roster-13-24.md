# 03b — Hero Roster: Heroes 13–24

Debuffer/Disrupters (13–16), Spike DPS (17–20), Resource Batteries (21–24). Index and distribution: [03-hero-roster.md](03-hero-roster.md).

---

## Hero 13 — Nyx Sub-Vector, "The Null Theorem"

| Field | Value |
|---|---|
| Name | Nyx Sub-Vector |
| Epithet | The Null Theorem |
| Race | Kith-Lir |
| Archetype | Debuffer/Disrupter (Arcane focus) |
| Racial passive | Aetheric Condenser: Magic Bursts refund 65% MP (replacing the standard 50%) and extend the burst window by 600 ticks (max +1,200 per window). |

Nyx was a Kith-Lir proof-auditor who found that any chant can be disproven if you insert the right counter-geometry at the right tick. Nyx disassembles boss spellcraft mid-sentence.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,800 | 800 | 200 | 240 | 380 | 320 | 260 | 170 | 13 |
| Race modifier | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Individual tuning | +0% | +0% | +0% | +0% | +4% | +0% | +5% | +0% | +0% |
| **Final** | **4,080** | **1,080** | **170** | **168** | **514** | **416** | **287** | **179** | **13** |

Derived: Physical DR = 168/(168+500) = 25.1%. Magical DR = 416/(416+600) = 40.9%. MACC = 0.5 × INT + 0.5 × ACC = 400. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Counterexample.** Against a boss in the Casting state, Nyx's interrupt-capable spells gain +35% Interrupt chance (additive, after the formula) and cost 50% less MP.

### Abilities

**1. Vector Stun**

- Damage Type: Magical
- Chain Property: Lightning
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 200 ticks
- MP Cost: 90
- Base VE / CE: 500 / 300
- Multiplier: 0.80 × INT
- Tactical Utility: Fast interrupt: flat 60% base interrupt chance against a chanting target (plus the damage-based roll; the higher of the two is used), and 1,500 AP delay on hit. Closes Water/Piercing into Conduction.

**2. Entropic Sigil**

- Damage Type: Magical (debuff)
- Chain Property: Darkness
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 800 ticks
- MP Cost: 120
- Base VE / CE: 300 / 500
- Multiplier: 0.60 × INT; target −20% INT and −15% MEVA for 6,000 ticks
- Tactical Utility: Caster-boss suppression. The MEVA reduction raises party magical hit rate. Closes Blunt/Water into Distortion and Induration into Umbral Zero.

**3. Hexfold Paralysis**

- Damage Type: Magical (debuff)
- Chain Property: Ice
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,200 ticks
- MP Cost: 180
- Base VE / CE: 900 / 600 — heavy-tagged
- Multiplier: 1.20 × INT; target −25% AGI for 8,000 ticks
- Tactical Utility: Long slow. Stacks with Induration's 30% slow up to the −50% Slow cap. Closes Piercing/Water into Induration and Distortion into Umbral Zero.

### Synergy notes

The roster's dedicated interrupter. Hexfold Paralysis supplies an Ice property that closes Distortion into Umbral Zero.

---

## Hero 14 — Vessa Quiet-Claw, "The Hush Between Heartbeats"

| Field | Value |
|---|---|
| Name | Vessa Quiet-Claw |
| Epithet | The Hush Between Heartbeats |
| Race | Sylvari-Mor |
| Archetype | Debuffer/Disrupter (Physical focus) |
| Racial passive | Cadence Surge: a critical hit or a physical chain link refunds 1,200 AP (once per action). |

Vessa is a Sylvari-Mor silencer who moves in the half-tick between a target's intent and its action. She blinds, hobbles and mutes rather than kills.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,800 | 800 | 360 | 240 | 200 | 320 | 260 | 170 | 13 |
| Race modifier | -20% | -10% | +5% | -10% | +0% | +0% | +10% | +30% | +25% |
| Individual tuning | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +5% | +0% |
| **Final** | **3,840** | **720** | **378** | **216** | **200** | **320** | **286** | **232** | **16** |

Derived: Physical DR = 216/(216+500) = 30.2%. Magical DR = 320/(320+600) = 34.8%. MACC = 0.5 × INT + 0.5 × ACC = 243. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 16⌉ = 625 ticks.

### Unique Situational Bonus

**First in the Silence.** On any tick where Vessa and the boss both reach AP ≥ 10,000, Vessa always acts first regardless of tie-break order, and the debuffs she applies on that action last 30% longer.

### Abilities

**1. Tendon Snip**

- Damage Type: Physical (debuff)
- Chain Property: Piercing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 450
- Multiplier: 1.40 × ATK; target −20% ATK for 5,000 ticks
- Tactical Utility: Physical mitigation debuff. Opens Piercing for Induration and Conduction.

**2. Shadow Pounce**

- Damage Type: Elemental-Physical (Darkness, debuff)
- Chain Property: Darkness
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 30
- Base VE / CE: 0 / 500
- Multiplier: 1.60 × ATK; target −40 ACC for 4,000 ticks
- Tactical Utility: Blind. Closes Blunt/Water into Distortion and Induration into Umbral Zero.

**3. Silence Mark**

- Damage Type: Support (debuff)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 40
- Base VE / CE: 300 / 300
- Multiplier: 0.00 (no damage); Silence for 2,000 ticks
- Tactical Utility: Silenced targets cannot begin chants. Proc chance = 50% base + (Vessa ACC − target MEVA) / 400, clamped to 10–90%. Does not affect chants already in progress.

### Synergy notes

Silence Mark shuts down boss chants before they begin, which is stronger than interrupting; Shadow Pounce provides a Darkness closer for physical parties.

---

## Hero 15 — Ondrel Vey-Static, "The Circuit Heretic"

| Field | Value |
|---|---|
| Name | Ondrel Vey-Static |
| Epithet | The Circuit Heretic |
| Race | Aethel-Born |
| Archetype | Debuffer/Disrupter (Arcane focus) |
| Racial passive | Phase Dampener: VE from heavy-tagged heals and spells decays at 20% per 500 ticks (2× speed). Innate +15% Fast Cast; CE generation ×0.60. |

Ondrel rewrote his own circuit traces to pass current backward, and now feeds that reversed flow into enemy ward-structures. Every buff he strips becomes a shield for an ally.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,800 | 800 | 200 | 240 | 380 | 320 | 260 | 170 | 13 |
| Race modifier | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |
| Individual tuning | +0% | +0% | +0% | +0% | +0% | +5% | +0% | +0% | +0% |
| **Final** | **4,560** | **880** | **180** | **216** | **418** | **370** | **260** | **170** | **14** |

Derived: Physical DR = 216/(216+500) = 30.2%. Magical DR = 370/(370+600) = 38.1%. MACC = 0.5 × INT + 0.5 × ACC = 339. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 14⌉ = 715 ticks.

### Unique Situational Bonus

**Heretic's Tithe.** Each beneficial effect Ondrel removes from a boss grants the lowest-HP-percentage ally an absorb shield equal to 10% of Ondrel's Max HP for 4,000 ticks.

### Abilities

**1. Dispel Cascade**

- Damage Type: Magical (dispel)
- Chain Property: Wind
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 600 ticks
- MP Cost: 110
- Base VE / CE: 400 / 400 (after Aethel-Born ×0.60 CE generation: 240)
- Multiplier: 0.90 × INT; removes 1 beneficial effect (2 if a Distortion window is open)
- Tactical Utility: Buff removal. Closes Slashing into Fragmentation and Conduction into Tempest Crown; opens Wind→Light Radiance.

**2. Static Lattice**

- Damage Type: Magical
- Chain Property: Lightning
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 900 ticks
- MP Cost: 130
- Base VE / CE: 300 / 500 (after Aethel-Born ×0.60 CE generation: 300)
- Multiplier: 1.50 × INT; Shock: −10% AGI and 0.20 × INT Lightning damage per 500 ticks for 3,000 ticks
- Tactical Utility: Damage-over-time plus minor slow. Closes Water/Piercing into Conduction and Fragmentation into Tempest Crown.

**3. Aetheric Seizure**

- Damage Type: Support (debuff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,400 ticks
- MP Cost: 240
- Base VE / CE: 1,000 / 700 (after Aethel-Born ×0.60 CE generation: 420) — heavy-tagged
- Multiplier: 0.00 (no damage); target Shield sub-target disabled for 3,000 ticks
- Tactical Utility: A disabled Shield sub-target stops absorbing damage for the Core and cannot use shield abilities. Against bosses without a Shield part it instead removes 1 beneficial effect. Heavy-tagged VE.

### Synergy notes

Aetheric Seizure disables Shield sub-targets, the key enabler in raids where Squad 2 is assigned to shield nodes.

---

## Hero 16 — Crag Moor-Ash, "The Smog Warden"

| Field | Value |
|---|---|
| Name | Crag Moor-Ash |
| Epithet | The Smog Warden |
| Race | Ash-Dravan |
| Archetype | Debuffer/Disrupter (Hybrid focus) |
| Racial passive | Thermal Battery: elemental damage taken builds Heat; at 100 Heat the next weapon skill forces a Level 2 Resonance Detonation. Burn immune; +75 Elemental Penetration. |

Crag walks deep resonance shafts where the air is thick with volatile ash. He learned to bottle it, and his debuffs grow nastier as the dungeon's aether density rises.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,800 | 800 | 300 | 240 | 320 | 320 | 260 | 170 | 13 |
| Race modifier | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Individual tuning | +6% | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **5,340** | **760** | **345** | **252** | **320** | **288** | **260** | **153** | **13** |

Derived: Physical DR = 252/(252+500) = 33.5%. Magical DR = 288/(288+600) = 32.4%. MACC = 0.5 × INT + 0.5 × ACC = 290. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Thick Air.** While the Aether Density Meter is ≥ 50, every debuff Crag applies has +50% effect (percentage and flat values multiplied by 1.5; durations unchanged).

### Abilities

**1. Choking Smoke**

- Damage Type: Magical (debuff)
- Chain Property: Fire
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 500 ticks
- MP Cost: 100
- Base VE / CE: 300 / 450
- Multiplier: 0.70 × INT; Burn 0.30 × INT per 500 ticks for 4,000 ticks
- Tactical Utility: Fire DoT. Closes Blunt/Earth into Liquefaction and Tectonic Shear into Magma Core.

**2. Ash Veil**

- Damage Type: Magical (debuff)
- Chain Property: Earth
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 600 ticks
- MP Cost: 110
- Base VE / CE: 300 / 450
- Multiplier: 0.80 × INT; target −60 ACC for 4,000 ticks
- Tactical Utility: Accuracy debuff that makes high-EVA Sylvari-Mor almost untouchable. Closes Slashing into Tectonic Shear and Liquefaction into Magma Core.

**3. Slag Shackles**

- Damage Type: Elemental-Physical (Earth, debuff)
- Chain Property: Earth
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 60
- Base VE / CE: 200 / 1,100 — heavy-tagged
- Multiplier: 2.40 × ATK; target −30% AGI for 4,000 ticks
- Tactical Utility: Heavy weapon-skill slow; subject to Thermal Battery forcing.

### Synergy notes

Best in deep delves; Slag Shackles plus Nyx's Hexfold Paralysis reaches the −50% Slow cap on its own.

---

## Hero 17 — Zeph Tri-Lumen, "The Burst Cartographer"

| Field | Value |
|---|---|
| Name | Zeph Tri-Lumen |
| Epithet | The Burst Cartographer |
| Race | Kith-Lir |
| Archetype | Spike DPS (Arcane focus) |
| Racial passive | Aetheric Condenser: Magic Bursts refund 65% MP (replacing the standard 50%) and extend the burst window by 600 ticks (max +1,200 per window). |

Zeph maps the harmonic fault-lines that open after a Resonance Detonation and drops precision spells into them. Zeph's ocular array counts ticks faster than any other hero on the roster.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,600 | 400 | 200 | 230 | 440 | 200 | 290 | 190 | 13 |
| Race modifier | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Individual tuning | -5% | +0% | +0% | +0% | +6% | +0% | +0% | +0% | +0% |
| **Final** | **3,710** | **540** | **170** | **161** | **606** | **260** | **305** | **200** | **13** |

Derived: Physical DR = 161/(161+500) = 24.4%. Magical DR = 260/(260+600) = 30.2%. MACC = 0.5 × INT + 0.5 × ACC = 455. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Early-Window Cartography.** If Zeph lands a Magic Burst within the first 300 ticks of a burst window, that spell deals +25% damage and the Aetheric Condenser window extension from that burst is 900 ticks instead of 600.

### Abilities

**1. Blizzard II**

- Damage Type: Magical
- Chain Property: Ice
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 1,000 ticks
- MP Cost: 180
- Base VE / CE: 300 / 600
- Multiplier: 3.00 × INT
- Tactical Utility: Core Ice nuke. Bursts Induration and Umbral Zero windows; closes Piercing/Water into Induration.

**2. Pyre Lattice**

- Damage Type: Magical
- Chain Property: Fire
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,400 ticks
- MP Cost: 320
- Base VE / CE: 700 / 1,000 — heavy-tagged
- Multiplier: 4.40 × INT
- Tactical Utility: Tier-3 Fire nuke. Bursts Liquefaction and Magma Core windows; closes Blunt/Earth into Liquefaction.

**3. Hex Lance**

- Damage Type: Magical
- Chain Property: Lightning
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 600 ticks
- MP Cost: 140
- Base VE / CE: 200 / 500
- Multiplier: 2.40 × INT
- Tactical Utility: Fast Lightning nuke. Bursts Conduction and Tempest Crown; closes Fragmentation into Tempest Crown.

### Synergy notes

Blizzard II is the canonical Induration burst (Gambit: IF Target [Resonance: Ice] → CAST [Blizzard II]). Pyre Lattice bursts Liquefaction and Magma Core.

---

## Hero 18 — Kaelis Moon-Ravel, "The Split-Second Execution"

| Field | Value |
|---|---|
| Name | Kaelis Moon-Ravel |
| Epithet | The Split-Second Execution |
| Race | Sylvari-Mor |
| Archetype | Spike DPS (Physical focus) |
| Racial passive | Cadence Surge: a critical hit or a physical chain link refunds 1,200 AP (once per action). |

Kaelis is a Sylvari-Mor executioner who waits for one perfect frame in the timeline. When a boss part cracks, Kaelis is already there.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,600 | 400 | 440 | 230 | 180 | 200 | 290 | 190 | 13 |
| Race modifier | -20% | -10% | +5% | -10% | +0% | +0% | +10% | +30% | +25% |
| Individual tuning | +0% | +0% | +5% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **3,680** | **360** | **485** | **207** | **180** | **200** | **319** | **247** | **16** |

Derived: Physical DR = 207/(207+500) = 29.3%. Magical DR = 200/(200+600) = 25.0%. MACC = 0.5 × INT + 0.5 × ACC = 249. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 16⌉ = 625 ticks.

### Unique Situational Bonus

**Execution Frame.** Against a boss sub-target (Core, Shield or Weapon Arm) below 35% of its HP, Kaelis's crit damage is increased by +40% (additive to the crit damage multiplier). While Execution Frame is active, Kaelis's crit damage cap is raised from 2.50× to 2.90×.

### Abilities

**1. Crescent Sever**

- Damage Type: Physical
- Chain Property: Slashing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 600
- Multiplier: 2.60 × ATK
- Tactical Utility: Slashing opener for Fragmentation and Tectonic Shear; closes Blunt into Distortion and Radiance into Solar Apex.

**2. Umbral Pierce**

- Damage Type: Elemental-Physical (Darkness)
- Chain Property: Darkness
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 30
- Base VE / CE: 0 / 600
- Multiplier: 2.40 × ATK
- Tactical Utility: Closes Blunt/Water into Distortion and Induration into Umbral Zero.

**3. Ravel Execution**

- Damage Type: Physical
- Chain Property: Piercing
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 1,500 — heavy-tagged
- Multiplier: 5.20 × ATK
- Tactical Utility: Tier-3 finisher. Closes Slashing into Fragmentation. Inside a Fragmentation or Solar Apex burst window it receives the physical burst bonus.

### Synergy notes

Physical burst specialist: every Fragmentation window gives +60% Physical Burst to Ravel Execution.

---

## Hero 19 — Thurga Ember-Maw, "The Furnace Headsman"

| Field | Value |
|---|---|
| Name | Thurga Ember-Maw |
| Epithet | The Furnace Headsman |
| Race | Ash-Dravan |
| Archetype | Spike DPS (Physical focus) |
| Racial passive | Thermal Battery: elemental damage taken builds Heat; at 100 Heat the next weapon skill forces a Level 2 Resonance Detonation. Burn immune; +75 Elemental Penetration. |

Thurga was the executioner of a reclaimer clan, and the molten circulation in her arms runs hot enough to glow through her skin. She stores Heat deliberately and spends it in single, catastrophic blows.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,600 | 400 | 440 | 230 | 180 | 200 | 290 | 190 | 13 |
| Race modifier | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Individual tuning | +0% | +0% | +6% | +0% | +0% | +0% | +0% | -5% | +0% |
| **Final** | **4,830** | **380** | **536** | **242** | **180** | **180** | **290** | **162** | **13** |

Derived: Physical DR = 242/(242+500) = 32.6%. Magical DR = 180/(180+600) = 23.1%. MACC = 0.5 × INT + 0.5 × ACC = 235. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Banked Furnace.** When Thermal Battery forces a Level 2 detonation through one of Thurga's weapon skills, that weapon skill deals +35% damage and Thurga's Heat is set to 30 instead of 0 afterward.

### Abilities

**1. Headsman's Cleave**

- Damage Type: Physical
- Chain Property: Slashing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 650
- Multiplier: 2.80 × ATK
- Tactical Utility: Slashing opener; closes Blunt into Distortion and Radiance into Solar Apex.

**2. Molten Verdict**

- Damage Type: Elemental-Physical (Fire)
- Chain Property: Fire
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 50
- Base VE / CE: 0 / 1,500 — heavy-tagged
- Multiplier: 4.80 × ATK
- Tactical Utility: Tier-3 Fire weapon skill. Closes Blunt/Earth into Liquefaction and Tectonic Shear into Magma Core. The preferred skill to spend 100 Heat on.

**3. Ash Brand**

- Damage Type: Physical
- Chain Property: Blunt
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 300
- Multiplier: 1.20 × ATK
- Tactical Utility: Stance-cost Blunt opener that sets up Molten Verdict for Liquefaction.

### Synergy notes

Needs elemental incoming damage: fits against Fire and Lightning bosses, or with deliberate Heat feeding from Brannoch Ash-Well.

---

## Hero 20 — Aurel Nine-Vesper, "The Luminous Arithmetic"

| Field | Value |
|---|---|
| Name | Aurel Nine-Vesper |
| Epithet | The Luminous Arithmetic |
| Race | Aethel-Born |
| Archetype | Spike DPS (Arcane focus) |
| Racial passive | Phase Dampener: VE from heavy-tagged heals and spells decays at 20% per 500 ticks (2× speed). Innate +15% Fast Cast; CE generation ×0.60. |

Aurel is an Aethel-Born lightwright whose circuits render photons as integer arithmetic. Its minimal enmity generation lets it deliver Solar Apex finishers without pulling the boss.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 4,600 | 400 | 200 | 230 | 440 | 200 | 290 | 190 | 13 |
| Race modifier | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |
| Individual tuning | +0% | +0% | +0% | +0% | +5% | +0% | +0% | +0% | +0% |
| **Final** | **4,370** | **440** | **180** | **207** | **508** | **220** | **290** | **190** | **14** |

Derived: Physical DR = 207/(207+500) = 29.3%. Magical DR = 220/(220+600) = 26.8%. MACC = 0.5 × INT + 0.5 × ACC = 399. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 14⌉ = 715 ticks.

### Unique Situational Bonus

**Quiet Radiance.** While Aurel's CE on the targeted boss part is below 1,000, Aurel's Light-property spells gain +20% crit chance (additive).

### Abilities

**1. Photon Sermon**

- Damage Type: Magical
- Chain Property: Light
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 900 ticks
- MP Cost: 160
- Base VE / CE: 300 / 500 (after Aethel-Born ×0.60 CE generation: 300)
- Multiplier: 3.10 × INT
- Tactical Utility: Standard Light nuke. Bursts Radiance and Solar Apex; closes Fire/Wind into Radiance and Fragmentation into Solar Apex.

**2. Solar Filament**

- Damage Type: Magical
- Chain Property: Light
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,600 ticks
- MP Cost: 300
- Base VE / CE: 800 / 900 (after Aethel-Born ×0.60 CE generation: 540) — heavy-tagged
- Multiplier: 4.60 × INT
- Tactical Utility: Tier-3 Light nuke; the intended Solar Apex closer. Heavy-tagged VE, decays twice as fast under Phase Dampener.

**3. Gale Quanta**

- Damage Type: Magical
- Chain Property: Wind
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 500 ticks
- MP Cost: 120
- Base VE / CE: 200 / 400 (after Aethel-Born ×0.60 CE generation: 240)
- Multiplier: 2.20 × INT
- Tactical Utility: Fast Wind nuke. Closes Slashing into Fragmentation and Conduction into Tempest Crown; bursts Fragmentation and Tempest Crown windows.

### Synergy notes

Designated Solar Apex closer: Fragmentation → Solar Filament. Low CE keeps Quiet Radiance active long into a fight.

---

## Hero 21 — Ilune Cache-Aria, "The Wellspring Protocol"

| Field | Value |
|---|---|
| Name | Ilune Cache-Aria |
| Epithet | The Wellspring Protocol |
| Race | Aethel-Born |
| Archetype | Resource Battery (Arcane focus) |
| Racial passive | Phase Dampener: VE from heavy-tagged heals and spells decays at 20% per 500 ticks (2× speed). Innate +15% Fast Cast; CE generation ×0.60. |

Ilune was a reservoir-synthetic, an Aethel-Born built to store aether for an entire city district. It now hands that reserve to casters in measured pulses.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,200 | 1,100 | 160 | 260 | 300 | 280 | 200 | 150 | 11 |
| Race modifier | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |
| Individual tuning | +0% | +6% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **4,940** | **1,285** | **144** | **234** | **330** | **308** | **200** | **150** | **12** |

Derived: Physical DR = 234/(234+500) = 31.9%. Magical DR = 308/(308+600) = 33.9%. MACC = 0.5 × INT + 0.5 × ACC = 265. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 12⌉ = 834 ticks.

### Unique Situational Bonus

**Returning Current.** When an ally under Ilune's Refresh Current lands a Magic Burst, Ilune regains 5% of its Max MP. Triggers once per burst spell.

### Abilities

**1. Refresh Current**

- Damage Type: Support (party aura)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 600 ticks
- MP Cost: 150
- Base VE / CE: 500 / 100 (after Aethel-Born ×0.60 CE generation: 60)
- Multiplier: 0.00 (no damage); party +20 MP per 500 ticks for 6,000 ticks
- Tactical Utility: Party MP regeneration (12 pulses, 240 MP per ally over the full duration).

**2. Aether Transfusion**

- Damage Type: Support (resource)
- Chain Property: None
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 400 ticks
- MP Cost: 0
- Base VE / CE: 400 / 100 (after Aethel-Born ×0.60 CE generation: 60)
- Multiplier: 0.00 (no damage); transfers 30% of Ilune's current MP to the target
- Tactical Utility: Costs no MP itself, since the transferred MP is the cost. Target cannot exceed its Max MP; excess is returned to Ilune.

**3. Tempo Gift**

- Damage Type: Support (resource)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 800 ticks
- MP Cost: 200
- Base VE / CE: 900 / 100 (after Aethel-Born ×0.60 CE generation: 60) — heavy-tagged
- Multiplier: 0.00 (no damage); target +3,000 AP
- Tactical Utility: AP grant, subject to the 10,000 AP refund ceiling. Cannot target Ilune. Heavy-tagged VE.

### Synergy notes

Sustains Zeph and Aurel through long fights; Tempo Gift can hand a Spike DPS the AP to act inside a burst window it would otherwise miss.

---

## Hero 22 — Mox Relay-Pell, "The Pocket Reactor"

| Field | Value |
|---|---|
| Name | Mox Relay-Pell |
| Epithet | The Pocket Reactor |
| Race | Kith-Lir |
| Archetype | Resource Battery (Arcane focus) |
| Racial passive | Aetheric Condenser: Magic Bursts refund 65% MP (replacing the standard 50%) and extend the burst window by 600 ticks (max +1,200 per window). |

Mox wears a sub-hex reactor on his back that siphons aether from hostile resonance fields. He converts enemy structure into party mana.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,200 | 1,100 | 160 | 260 | 300 | 280 | 200 | 150 | 11 |
| Race modifier | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Individual tuning | +0% | +5% | +0% | +0% | +0% | +0% | +0% | +0% | +5% |
| **Final** | **4,420** | **1,560** | **136** | **182** | **390** | **364** | **210** | **158** | **12** |

Derived: Physical DR = 182/(182+500) = 26.7%. Magical DR = 364/(364+600) = 37.8%. MACC = 0.5 × INT + 0.5 × ACC = 300. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 12⌉ = 834 ticks.

### Unique Situational Bonus

**Overflow Coupling.** While Mox's MP is above 80% of Max MP, his Sub-Hex Ration also grants the target +10% Fast Cast on its next spell (within the 65% total cap).

### Abilities

**1. Condenser Tap**

- Damage Type: Magical
- Chain Property: Lightning
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 500 ticks
- MP Cost: 60
- Base VE / CE: 200 / 300
- Multiplier: 1.00 × INT; party gains MP equal to 25% of the damage dealt, split evenly
- Tactical Utility: Offensive battery. Closes Water/Piercing into Conduction and Fragmentation into Tempest Crown.

**2. Sub-Hex Ration**

- Damage Type: Support (resource)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 300 ticks
- MP Cost: 40
- Base VE / CE: 300 / 100
- Multiplier: 0.00 (no damage); target +1.50 × INT MP
- Tactical Utility: Fast single-target MP restore; Overflow Coupling adds Fast Cast.

**3. Overclock Relay**

- Damage Type: Support (buff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 900 ticks
- MP Cost: 180
- Base VE / CE: 800 / 100 — heavy-tagged
- Multiplier: 0.00 (no damage); target's next ability Recovery Cost −40%
- Tactical Utility: A Heavy (14,000) ability becomes 8,400; a Standard becomes 6,000. Does not stack with Timeline Stalk; the larger reduction applies. Heavy-tagged VE.

### Synergy notes

Condenser Tap is a Lightning closer for Conduction, making Mox a battery who also links chains.

---

## Hero 23 — Brannoch Ash-Well, "The Ember Tithe"

| Field | Value |
|---|---|
| Name | Brannoch Ash-Well |
| Epithet | The Ember Tithe |
| Race | Ash-Dravan |
| Archetype | Resource Battery (Arcane focus) |
| Racial passive | Thermal Battery: elemental damage taken builds Heat; at 100 Heat the next weapon skill forces a Level 2 Resonance Detonation. Burn immune; +75 Elemental Penetration. |

Brannoch is an Ash-Dravan well-keeper who pays the caravan's aether debts with his own blood-heat. He turns HP and stored Heat into resources for allies.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,200 | 1,100 | 160 | 260 | 300 | 280 | 200 | 150 | 11 |
| Race modifier | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Individual tuning | +6% | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **5,790** | **1,045** | **184** | **273** | **300** | **252** | **200** | **135** | **11** |

Derived: Physical DR = 273/(273+500) = 35.3%. Magical DR = 252/(252+600) = 29.6%. MACC = 0.5 × INT + 0.5 × ACC = 250. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 11⌉ = 910 ticks.

### Unique Situational Bonus

**Tithe of Coals.** When Brannoch uses any ability while at ≥ 20 Heat, he may spend 20 Heat to give that ability +50% potency (damage, MP granted, AP granted or buff values multiplied by 1.5). Spending is chosen by the player or by a gambit flag.

### Abilities

**1. Ember Tithe**

- Damage Type: Support (resource)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 300 / 100
- Multiplier: 0.00 (no damage); Brannoch loses 10% Max HP, every ally gains +150 MP
- Tactical Utility: HP-to-MP conversion. The HP loss is self-inflicted true damage that cannot kill (leaves Brannoch at minimum 1 HP).

**2. Cinder Pulse**

- Damage Type: Elemental-Physical (Fire)
- Chain Property: Fire
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 20
- Base VE / CE: 0 / 400
- Multiplier: 1.60 × ATK; on hit every ally gains +300 AP
- Tactical Utility: Party AP grant attached to a Fire link; the AP is subject to the 10,000 AP refund ceiling.

**3. Forge Surge**

- Damage Type: Support (buff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 80
- Base VE / CE: 600 / 200 — heavy-tagged
- Multiplier: 0.00 (no damage); target's next weapon skill +0.80 multiplier and its property becomes Fire; if target is Ash-Dravan, +25 Heat
- Tactical Utility: Converts any weapon skill into a Fire link (for Blunt→Fire and Earth→Fire) and feeds Heat for Thermal Battery.

### Synergy notes

Heat feeding for Thurga Ember-Maw and Varra Kesh-Ember via Forge Surge; Ember Tithe keeps a caster party solvent at a health cost that healers can offset.

---

## Hero 24 — Tollen Geode-Vast, "The Slow Aquifer"

| Field | Value |
|---|---|
| Name | Tollen Geode-Vast |
| Epithet | The Slow Aquifer |
| Race | Veth-Kari |
| Archetype | Resource Battery (Arcane focus) |
| Racial passive | Chitinous Grounding: 15% of post-mitigation physical damage taken is added to this hero's VE on the attacker. |

Tollen is a Veth-Kari aquifer-tender who draws aether-rich water up from the deep strata. He is patient to a fault, and rewarded for it.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,200 | 1,100 | 160 | 260 | 300 | 280 | 200 | 150 | 11 |
| Race modifier | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Individual tuning | +0% | +8% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **5,720** | **1,190** | **168** | **338** | **270** | **224** | **200** | **135** | **10** |

Derived: Physical DR = 338/(338+500) = 40.3%. Magical DR = 224/(224+600) = 27.2%. MACC = 0.5 × INT + 0.5 × ACC = 235. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 10⌉ = 1,000 ticks.

### Unique Situational Bonus

**Aquifer Patience.** For every continuous 3,000 ticks in which Tollen takes no damage, he banks 1 Aquifer charge (max 3). Each charge doubles the MP restored by his next Wellspring Tap (1 charge = ×2, 2 charges = ×4, 3 charges = ×8). All charges are consumed on use.

### Abilities

**1. Wellspring Tap**

- Damage Type: Support (party resource)
- Chain Property: None
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 700 ticks
- MP Cost: 0
- Base VE / CE: 500 / 100
- Multiplier: 0.00 (no damage); every ally +120 MP + 8% of their Max MP
- Tactical Utility: Party MP restore; scales with Aquifer charges.

**2. Patient Stone**

- Damage Type: Support (resource)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 200 / 100
- Multiplier: 0.00 (no damage); the ally with the lowest AP gains +2,000 AP; Tollen loses 2,000 AP
- Tactical Utility: AP transfer. Tollen's AP may go down to the −4,000 AP debt floor.

**3. Aquifer Surge**

- Damage Type: Magical
- Chain Property: Water
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,200 ticks
- MP Cost: 150
- Base VE / CE: 700 / 600 — heavy-tagged
- Multiplier: 2.00 × INT; party +10% Max MP restored
- Tactical Utility: Water opener for Induration, Conduction and Distortion.

### Synergy notes

The only Water-property source on the roster outside gear: Aquifer Surge opens Water→Ice Induration, Water→Lightning Conduction and Water→Darkness Distortion.

---
