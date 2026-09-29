# 03a — Hero Roster: Heroes 01–12

Anchor Tanks (01–04), Physical Linkers (05–08), Buffer/Aura Supports (09–12). Index and distribution: [03-hero-roster.md](03-hero-roster.md).

---

## Hero 01 — Korrith Vael-Dun, "The Unbroken Carapace"

| Field | Value |
|---|---|
| Name | Korrith Vael-Dun |
| Epithet | The Unbroken Carapace |
| Race | Veth-Kari |
| Archetype | Anchor Tank (Physical focus) |
| Racial passive | Chitinous Grounding: 15% of post-mitigation physical damage taken is added to this hero's VE on the attacker. |

A deep-strata warden whose keratin lattice has been layered by nine decades of cave-quake fractures and regrowth. Korrith stands at the front of every formation as a deliberate target, converting incoming blows into enmity his allies can shelter behind.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 8,000 | 300 | 260 | 520 | 120 | 300 | 220 | 120 | 10 |
| Race modifier | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Individual tuning | +5% | +0% | +0% | +5% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **9,240** | **300** | **273** | **710** | **108** | **240** | **220** | **108** | **9** |

Derived: Physical DR = 710/(710+500) = 58.7%. Magical DR = 240/(240+600) = 28.6%. MACC = 0.5 × INT + 0.5 × ACC = 164. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 9⌉ = 1,112 ticks.

### Unique Situational Bonus

**Shelter of the Chanters.** While Korrith is the boss's current target (argmax VE+CE) AND at least 2 allies are in the Casting state at the same time, Korrith gains +25% DEF (multiplicative on his current DEF) and +30% CE generation from every ability. The condition is checked every tick; the bonus ends on the first tick where fewer than 2 allies are in the Casting state or Korrith is no longer the boss's target.

### Abilities

**1. Lattice Provoke**

- Damage Type: Enmity (no damage)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 2,200 / 300 — heavy-tagged
- Multiplier: 0.00 (no damage)
- Tactical Utility: Single-target taunt. Adds 2,200 VE and 300 CE on the chosen boss part. Stance recovery (4,000 AP) lets Korrith act again quickly, so he can re-taunt right after a healer's heavy heal spike.

**2. Seismic Maul**

- Damage Type: Physical
- Chain Property: Blunt
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 700
- Multiplier: 2.20 × ATK
- Tactical Utility: Main chain opener. Opens a 4,000-tick Blunt window (routes: Blunt→Fire Liquefaction, Blunt→Darkness or Blunt→Slashing Distortion). Damage-derived CE (8% of final damage) is added on top of the 700 flat CE.

**3. Keratin Bastion**

- Damage Type: Support (self buff)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 30
- Base VE / CE: 400 / 900
- Multiplier: 0.00 (no damage); −35% physical damage taken for 3,000 ticks
- Tactical Utility: Mitigation stance. While active, physical hits on Korrith count as mitigated, so they never trigger CE shed. Use it before boss Heavy strikes and multi-hit combos: every unmitigated hit would shed part of Korrith's CE lead, so Bastion protects his threat as well as his HP.

### Synergy notes

Pairs with Kith-Lir and Aethel-Born casters (Zeph Tri-Lumen, Aurel Nine-Vesper) whose long chants turn on his USB; his Seismic Maul opens Blunt windows for Liquefaction and Distortion.

---

## Hero 02 — Drusk Oma-Teth, "Deepwarden of the Hollow Choir"

| Field | Value |
|---|---|
| Name | Drusk Oma-Teth |
| Epithet | Deepwarden of the Hollow Choir |
| Race | Veth-Kari |
| Archetype | Anchor Tank (Physical focus) |
| Racial passive | Chitinous Grounding: 15% of post-mitigation physical damage taken is added to this hero's VE on the attacker. |

Drusk was trained in the resonance caverns beneath the Hollow Choir, where the Veth-Kari tune their chitin by striking it against singing stone. He fights by answering every blow with a counter-strike that rings through the target's frame.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 8,000 | 300 | 260 | 520 | 120 | 300 | 220 | 120 | 10 |
| Race modifier | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Individual tuning | +0% | +0% | +8% | -3% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **8,800** | **300** | **295** | **656** | **108** | **240** | **220** | **108** | **9** |

Derived: Physical DR = 656/(656+500) = 56.7%. Magical DR = 240/(240+600) = 28.6%. MACC = 0.5 × INT + 0.5 × ACC = 164. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 9⌉ = 1,112 ticks.

### Unique Situational Bonus

**Answering Toll.** If Drusk is struck by a physical attack within 500 ticks after he resolved any Blunt-property ability, he immediately counters for 0.80 × ATK Physical Blunt damage (no AP cost, no recovery). If no Resonance window is open on the attacker, the counter opens a Blunt window. Maximum one counter per 1,000 ticks.

### Abilities

**1. Choir Knell**

- Damage Type: Physical (all enemy parts)
- Chain Property: Blunt
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 600 / 500
- Multiplier: 1.40 × ATK per part
- Tactical Utility: AoE enmity builder that hits Core, Shield and Weapon Arm separately; each part gets its own 600 VE and 500 CE entry. Opens a Blunt window on the Core.

**2. Geode Reprisal**

- Damage Type: Support (counter stance)
- Chain Property: Earth (on counter)
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 40
- Base VE / CE: 300 / 800
- Multiplier: Counter 2.00 × ATK Elemental-Physical (Earth)
- Tactical Utility: Readies a counter for 3,000 ticks: the first physical hit Drusk takes triggers a 2.00 × ATK Earth counter that carries the Earth property (Earth→Fire = Liquefaction, Earth→Blunt = Tectonic Shear). Stacks with Answering Toll; both can fire from one hit.

**3. Hollow Resound**

- Damage Type: Physical
- Chain Property: Blunt
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 1,000 / 1,800 — heavy-tagged
- Multiplier: 3.60 × ATK
- Tactical Utility: Heavy finisher. On hit, pushes the boss AP back by 2,000 (delay). If the boss is in a Casting state, adds +20% to the damage-based interrupt roll (additive to the Interrupt% formula result).

### Synergy notes

Feeds Blunt openings to Gorrun Delve-Mace and Ishka Pyre-Lash; his Geode Reprisal counters carry Earth, enabling Earth→Fire Liquefaction for Varra Kesh-Ember or Thurga Ember-Maw.

---

## Hero 03 — Varra Kesh-Ember, "The Cinder Bulwark"

| Field | Value |
|---|---|
| Name | Varra Kesh-Ember |
| Epithet | The Cinder Bulwark |
| Race | Ash-Dravan |
| Archetype | Anchor Tank (Physical focus) |
| Racial passive | Thermal Battery: elemental damage taken builds Heat; at 100 Heat the next weapon skill forces a Level 2 Resonance Detonation. Burn immune; +75 Elemental Penetration. |

Varra led a reclaimer caravan across the Glass Barrens and learned to stand in front of storms of volatile aether. Her obsidian veins drink in elemental fire, and she spends the stored heat as tectonic slams that crack resonant armor.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 8,000 | 300 | 260 | 520 | 120 | 300 | 220 | 120 | 10 |
| Race modifier | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Individual tuning | +4% | +0% | +0% | +0% | +0% | +5% | +0% | +0% | +0% |
| **Final** | **8,740** | **285** | **299** | **546** | **120** | **284** | **220** | **108** | **10** |

Derived: Physical DR = 546/(546+500) = 52.2%. Magical DR = 284/(284+600) = 32.1%. MACC = 0.5 × INT + 0.5 × ACC = 170. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 10⌉ = 1,000 ticks.

### Unique Situational Bonus

**Kiln Discipline.** While Varra's Heat is ≥ 50, elemental damage she takes is reduced by 20% (applied after MEVA reduction and elemental resistance), and every Fire-property ability she uses adds +500 bonus CE. Heat is gained before this reduction is applied, so the reduction does not starve Thermal Battery.

### Abilities

**1. Slag Taunt**

- Damage Type: Elemental-Physical (Fire)
- Chain Property: Fire
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 20
- Base VE / CE: 1,800 / 400 — heavy-tagged
- Multiplier: 0.60 × ATK
- Tactical Utility: Taunt that deals minor Fire damage. Its Fire property can close a Blunt window into Liquefaction, letting the tank finish a chain. With Kiln Discipline active, total CE becomes 900.

**2. Magma Hammer**

- Damage Type: Elemental-Physical (Fire)
- Chain Property: Fire
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 750
- Multiplier: 2.00 × ATK
- Tactical Utility: Weapon skill; subject to Thermal Battery (at 100 Heat it forces a Level 2 detonation). Ash-Dravan Elemental Penetration (+75 EPEN) makes it effective into Fire-resistant targets.

**3. Obsidian Mantle**

- Damage Type: Support (party buff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 600 ticks
- MP Cost: 90
- Base VE / CE: 500 / 1,500 — heavy-tagged
- Multiplier: 0.00 (no damage); party −15% elemental damage taken for 4,000 ticks
- Tactical Utility: Party mitigation that also gives Varra +30 Heat on resolution. Counts as mitigation for CE-shed purposes against elemental hits only.

### Synergy notes

Builds Heat intentionally against elemental bosses; her forced Level 2 detonation at 100 Heat gives Spike DPS a guaranteed burst window without needing a linker.

---

## Hero 04 — Tessel Primewright, "The Hexfield Aegis"

| Field | Value |
|---|---|
| Name | Tessel Primewright |
| Epithet | The Hexfield Aegis |
| Race | Kith-Lir |
| Archetype | Anchor Tank (Arcane focus) |
| Racial passive | Aetheric Condenser: Magic Bursts refund 65% MP (replacing the standard 50%) and extend the burst window by 600 ticks (max +1,200 per window). |

Tessel stands barely over three feet tall and projects a lattice of interlocking sub-hex planes that fold incoming spells back into geometry. She is the roster's dedicated magic tank, meant for caster bosses that would dissolve a Veth-Kari's thin MEVA.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 8,000 | 300 | 180 | 520 | 300 | 300 | 220 | 120 | 10 |
| Race modifier | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Individual tuning | +5% | +0% | +0% | +0% | +5% | +0% | +0% | +0% | +0% |
| **Final** | **7,140** | **405** | **153** | **364** | **410** | **390** | **231** | **126** | **10** |

Derived: Physical DR = 364/(364+500) = 42.1%. Magical DR = 390/(390+600) = 39.4%. MACC = 0.5 × INT + 0.5 × ACC = 320. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 10⌉ = 1,000 ticks.

### Unique Situational Bonus

**Refutation Dividend.** When a boss Magical ability resolves on Tessel while her Sub-Hex Lattice shield is active, 30% of the damage absorbed by the shield is refunded as MP to the ally with the lowest current MP percentage (Tessel included). Refund is capped at 250 MP per hit.

### Abilities

**1. Sub-Hex Lattice**

- Damage Type: Support (self shield)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 300 ticks
- MP Cost: 80
- Base VE / CE: 800 / 600 — heavy-tagged
- Multiplier: Absorb shield 4.00 × INT for 4,000 ticks
- Tactical Utility: Absorbs damage of any type. Hits fully absorbed by it count as mitigated (no CE shed). Tessel's CE generation on this ability is doubled when the boss is casting a Magical spell.

**2. Geometric Refutation**

- Damage Type: Magical
- Chain Property: Light
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 400 ticks
- MP Cost: 60
- Base VE / CE: 200 / 900
- Multiplier: 1.80 × INT
- Tactical Utility: Light-element strike. Closes Fragmentation into Solar Apex (Level 3) and Fire/Wind windows into Radiance (Level 2). When cast on a boss that is chanting, adds +15% to the damage-based interrupt roll.

**3. Tessellate Provocation**

- Damage Type: Enmity (no damage)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 30
- Base VE / CE: 2,400 / 200 — heavy-tagged
- Multiplier: 0.00 (no damage)
- Tactical Utility: Hits every boss part (Core, Shield, Weapon Arm) for 2,400 VE and 200 CE each. The go-to response to a Solar Apex that has just moved all party VE onto a different tank.

### Synergy notes

Keeps Resource Batteries like Mox Relay-Pell free to spend MP on offense; her Light-property Geometric Refutation closes Fragmentation into Solar Apex.

---

## Hero 05 — Saeli Thorn-Vesper, "Cadence of the Seventh Second"

| Field | Value |
|---|---|
| Name | Saeli Thorn-Vesper |
| Epithet | Cadence of the Seventh Second |
| Race | Sylvari-Mor |
| Archetype | Physical Linker (Physical focus) |
| Racial passive | Cadence Surge: a critical hit or a physical chain link refunds 1,200 AP (once per action). |

Saeli sees the timeline as a braid of possible seconds and slips into the gaps between them. She is the roster's fastest linker, built to open Slashing windows and link Piercing into Fragmentation before the window's echo fades.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,600 | 250 | 380 | 300 | 110 | 180 | 280 | 180 | 14 |
| Race modifier | -20% | -10% | +5% | -10% | +0% | +0% | +10% | +30% | +25% |
| Individual tuning | +0% | +0% | +0% | +0% | +0% | +0% | +5% | +0% | +5% |
| **Final** | **4,480** | **225** | **399** | **270** | **110** | **180** | **323** | **234** | **18** |

Derived: Physical DR = 270/(270+500) = 35.1%. Magical DR = 180/(180+600) = 23.1%. MACC = 0.5 × INT + 0.5 × ACC = 216. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 18⌉ = 556 ticks.

### Unique Situational Bonus

**Seventh-Second Tempo.** If Saeli acts on the same target within 300 ticks after an ally's action resolved on that target, her next hit gains +20% crit chance (additive) and her Cadence Surge refund for that action becomes 1,800 AP instead of 1,200.

### Abilities

**1. Rend Pulse**

- Damage Type: Physical
- Chain Property: Slashing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 450
- Multiplier: 1.90 × ATK
- Tactical Utility: Opener for Fragmentation (Slashing→Wind or Slashing→Piercing) and Tectonic Shear (Slashing→Earth). Also closes Blunt into Distortion.

**2. Needle Flicker**

- Damage Type: Physical
- Chain Property: Piercing
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 250
- Multiplier: 1.10 × ATK
- Tactical Utility: Stance-cost attack (4,000 AP recovery). Lets Saeli link her own Slashing window into Fragmentation in two quick actions. Also opens Piercing for Induration (Piercing→Ice) and Conduction (Piercing→Lightning).

**3. Wind Talon Arc**

- Damage Type: Elemental-Physical (Wind)
- Chain Property: Wind
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 25
- Base VE / CE: 0 / 500
- Multiplier: 2.10 × ATK
- Tactical Utility: Wind-property weapon skill. Closes Slashing into Fragmentation and Conduction into Tempest Crown (Level 3); opens Wind→Light Radiance.

### Synergy notes

Core of the Solar Apex route: Rend Pulse (Slashing) → Needle Flicker (Piercing) = Fragmentation → Aurel Nine-Vesper's Solar Filament (Light) = Solar Apex.

---

## Hero 06 — Mirrim Ash-Pounce, "The Prowling Metronome"

| Field | Value |
|---|---|
| Name | Mirrim Ash-Pounce |
| Epithet | The Prowling Metronome |
| Race | Sylvari-Mor |
| Archetype | Physical Linker (Physical focus) |
| Racial passive | Cadence Surge: a critical hit or a physical chain link refunds 1,200 AP (once per action). |

Mirrim hunts the moment before a predator strikes. Her timeline perception is tuned to the instant a boss gathers itself to act, and she spends that instant pinning it in place with a lance of chilled bone.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,600 | 250 | 380 | 300 | 110 | 180 | 280 | 180 | 14 |
| Race modifier | -20% | -10% | +5% | -10% | +0% | +0% | +10% | +30% | +25% |
| Individual tuning | +0% | +0% | +4% | +0% | +0% | +0% | +0% | +4% | +0% |
| **Final** | **4,480** | **225** | **415** | **270** | **110** | **180** | **308** | **243** | **18** |

Derived: Physical DR = 270/(270+500) = 35.1%. Magical DR = 180/(180+600) = 23.1%. MACC = 0.5 × INT + 0.5 × ACC = 209. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 18⌉ = 556 ticks.

### Unique Situational Bonus

**Pounce on the Upbeat.** When the targeted boss's AP is between 8,000 and 9,999 at the moment Mirrim's ability resolves, her Piercing-property abilities also delay the boss by 1,500 AP. Maximum one delay per boss action cycle.

### Abilities

**1. Talon Lance**

- Damage Type: Physical
- Chain Property: Piercing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 500
- Multiplier: 2.00 × ATK
- Tactical Utility: Piercing opener for Induration (→Ice) and Conduction (→Lightning); closes Slashing into Fragmentation. Triggers Pounce on the Upbeat.

**2. Frostfang Pounce**

- Damage Type: Elemental-Physical (Ice)
- Chain Property: Ice
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 30
- Base VE / CE: 0 / 450
- Multiplier: 1.80 × ATK
- Tactical Utility: Closes Piercing/Water windows into Induration and Distortion windows into Umbral Zero (Level 3). As a physical chain link it triggers Cadence Surge (1,200 AP refund).

**3. Timeline Stalk**

- Damage Type: Support (self buff)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 20
- Base VE / CE: 0 / 200
- Multiplier: 0.00 (no damage); next ability recovery −4,000 AP
- Tactical Utility: Mirrim's next ability has its Recovery Cost reduced by 4,000 (Standard becomes 6,000; Heavy becomes 10,000). Buff lasts until used or 6,000 ticks.

### Synergy notes

Primary Induration linker: Talon Lance (Piercing) → Frostfang Pounce (Ice) is a self-contained Level 2 chain; feeds Ice bursts to Zeph Tri-Lumen and Nyx Sub-Vector.

---

## Hero 07 — Gorrun Delve-Mace, "The Faultline Drummer"

| Field | Value |
|---|---|
| Name | Gorrun Delve-Mace |
| Epithet | The Faultline Drummer |
| Race | Veth-Kari |
| Archetype | Physical Linker (Physical focus) |
| Racial passive | Chitinous Grounding: 15% of post-mitigation physical damage taken is added to this hero's VE on the attacker. |

Gorrun is a Veth-Kari quarry-drummer who learned that the right cadence of blows splits a mountain along its faults. He trades speed for crushing chain weight and a self-built momentum engine.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,600 | 250 | 380 | 300 | 110 | 180 | 280 | 180 | 14 |
| Race modifier | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Individual tuning | +0% | +0% | +6% | +0% | +0% | +0% | +0% | +0% | -5% |
| **Final** | **6,160** | **250** | **423** | **390** | **99** | **144** | **280** | **162** | **12** |

Derived: Physical DR = 390/(390+500) = 43.8%. Magical DR = 144/(144+600) = 19.4%. MACC = 0.5 × INT + 0.5 × ACC = 189. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 12⌉ = 834 ticks.

### Unique Situational Bonus

**Faultline Momentum.** Each time Gorrun closes a chain into Liquefaction, Distortion or Tectonic Shear, he gains 1 Faultline stack (max 3). Each stack grants +8% Blunt damage. All stacks are lost when Gorrun takes an unmitigated direct hit.

### Abilities

**1. Faultline Crush**

- Damage Type: Physical
- Chain Property: Blunt
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 600
- Multiplier: 2.30 × ATK
- Tactical Utility: Blunt opener; also closes Earth into Tectonic Shear (Earth→Blunt).

**2. Ore Cleaver**

- Damage Type: Physical
- Chain Property: Slashing
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 550
- Multiplier: 2.10 × ATK
- Tactical Utility: Closes Blunt into Distortion (Blunt→Slashing); opens Slashing for Fragmentation and Tectonic Shear; closes Radiance into Solar Apex.

**3. Tectonic Stomp**

- Damage Type: Elemental-Physical (Earth, all parts)
- Chain Property: Earth
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 40
- Base VE / CE: 300 / 1,400 — heavy-tagged
- Multiplier: 3.40 × ATK per part
- Tactical Utility: Heavy AoE across all boss parts. Closes Slashing into Tectonic Shear and Liquefaction into Magma Core (Level 3); opens Earth→Fire and Earth→Blunt.

### Synergy notes

Pairs with Veth-Kari Chitinous Grounding: his high DEF keeps him in the fight while stacking Faultline; with Drusk's counters he forms an all-Blunt chain engine.

---

## Hero 08 — Ishka Pyre-Lash, "The Whipcord Reclaimer"

| Field | Value |
|---|---|
| Name | Ishka Pyre-Lash |
| Epithet | The Whipcord Reclaimer |
| Race | Ash-Dravan |
| Archetype | Physical Linker (Physical focus) |
| Racial passive | Thermal Battery: elemental damage taken builds Heat; at 100 Heat the next weapon skill forces a Level 2 Resonance Detonation. Burn immune; +75 Elemental Penetration. |

Ishka reclaims salvage from resonance craters with a segmented obsidian whip that holds molten aether in its links. She reads open Fire windows the way other reclaimers read the wind.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,600 | 250 | 380 | 300 | 110 | 180 | 280 | 180 | 14 |
| Race modifier | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Individual tuning | +0% | +0% | +0% | +0% | +0% | +0% | +6% | +0% | +0% |
| **Final** | **5,880** | **240** | **437** | **315** | **110** | **162** | **297** | **162** | **14** |

Derived: Physical DR = 315/(315+500) = 38.7%. Magical DR = 162/(162+600) = 21.3%. MACC = 0.5 × INT + 0.5 × ACC = 203. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 14⌉ = 715 ticks.

### Unique Situational Bonus

**Draft Through the Breach.** If an ally opened a Fire-property window on the target within 1,000 ticks before Ishka acts, her first Slashing or Fire ability in that window ignores 30% of the target's DEF (applied before the DR formula).

### Abilities

**1. Ember Lash**

- Damage Type: Elemental-Physical (Fire)
- Chain Property: Fire
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 20
- Base VE / CE: 0 / 500
- Multiplier: 2.00 × ATK
- Tactical Utility: Closes Blunt/Earth into Liquefaction; opens Fire→Light Radiance. Benefits from Ash-Dravan +75 EPEN.

**2. Chainwhip Sever**

- Damage Type: Physical
- Chain Property: Slashing
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 250
- Multiplier: 1.00 × ATK
- Tactical Utility: Stance-cost Slashing link. Cheap way to close Blunt into Distortion or open Fragmentation.

**3. Obsidian Flay**

- Damage Type: Elemental-Physical (Darkness)
- Chain Property: Darkness
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 50
- Base VE / CE: 200 / 1,300 — heavy-tagged
- Multiplier: 3.20 × ATK
- Tactical Utility: Closes Blunt/Water into Distortion (+70% Umbral true damage, Buff Purge) and Induration into Umbral Zero (Level 3).

### Synergy notes

Links behind Varra Kesh-Ember and Thurga Ember-Maw's Fire openers; Obsidian Flay closes Blunt into Distortion for Umbral true damage.

---

## Hero 09 — Lyr Aurelis-7, "The Harmonic Conductor"

| Field | Value |
|---|---|
| Name | Lyr Aurelis-7 |
| Epithet | The Harmonic Conductor |
| Race | Aethel-Born |
| Archetype | Buffer/Aura Support (Arcane focus) |
| Racial passive | Phase Dampener: VE from heavy-tagged heals and spells decays at 20% per 500 ticks (2× speed). Innate +15% Fast Cast; CE generation ×0.60. |

Lyr was built as a concert-hall stabilizer, a synthetic whose circuit traces hum the counter-frequency that keeps resonant matter from flying apart. On the field it conducts the party's tempo.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,000 | 900 | 180 | 280 | 320 | 300 | 200 | 150 | 12 |
| Race modifier | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |
| Individual tuning | +0% | +5% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **4,750** | **1,040** | **162** | **252** | **352** | **330** | **200** | **150** | **13** |

Derived: Physical DR = 252/(252+500) = 33.5%. Magical DR = 330/(330+600) = 35.5%. MACC = 0.5 × INT + 0.5 × ACC = 276. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Ensemble Gain.** While at least 3 different allies have contributed a property to the currently open Resonance chain on the boss, Lyr's aura abilities have +50% potency (percentage values and absorb amounts multiplied by 1.5; durations unchanged).

### Abilities

**1. Tempo Aria**

- Damage Type: Support (party aura)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 800 ticks
- MP Cost: 120
- Base VE / CE: 500 / 150 (after Aethel-Born ×0.60 CE generation: 90)
- Multiplier: 0.00 (no damage); party +15% AGI for 6,000 ticks
- Tactical Utility: Party haste. Counts against the +50% Haste cap. With Ensemble Gain the value becomes +22.5% AGI.

**2. Resonant Lattice Hymn**

- Damage Type: Support (party shield)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,200 ticks
- MP Cost: 220
- Base VE / CE: 1,200 / 200 (after Aethel-Born ×0.60 CE generation: 120) — heavy-tagged
- Multiplier: Absorb 2.50 × INT on every ally for 5,000 ticks
- Tactical Utility: Party-wide absorb. Heavy-tagged (1,200 base VE), so Phase Dampener decays its VE at 20% per 500 ticks instead of 10%.

**3. Radiant Refrain**

- Damage Type: Magical
- Chain Property: Light
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 500 ticks
- MP Cost: 70
- Base VE / CE: 300 / 250 (after Aethel-Born ×0.60 CE generation: 150)
- Multiplier: 1.20 × INT damage; party heal equal to 15% of damage dealt
- Tactical Utility: Light-property spell. Closes Fragmentation into Solar Apex and Fire/Wind into Radiance. Low damage, but a support can finish a Level 3 chain without a Spike DPS.

### Synergy notes

Tempo Aria accelerates the whole chain engine; Radiant Refrain supplies a Light property that finishes Solar Apex off a Fragmentation window.

---

## Hero 10 — Seraphine Vol-Ivory, "The Porcelain Choir"

| Field | Value |
|---|---|
| Name | Seraphine Vol-Ivory |
| Epithet | The Porcelain Choir |
| Race | Aethel-Born |
| Archetype | Buffer/Aura Support (Arcane focus) |
| Racial passive | Phase Dampener: VE from heavy-tagged heals and spells decays at 20% per 500 ticks (2× speed). Innate +15% Fast Cast; CE generation ×0.60. |

Seraphine is a restoration synthetic whose porcelain shell carries a choir of seven subvocal emitters. Each heal she casts is layered in harmony so the enmity spike it produces collapses almost as fast as it rises.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,000 | 900 | 180 | 280 | 320 | 300 | 200 | 150 | 12 |
| Race modifier | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |
| Individual tuning | +0% | +0% | +0% | +0% | +6% | +0% | +0% | +0% | +0% |
| **Final** | **4,750** | **990** | **162** | **252** | **373** | **330** | **200** | **150** | **13** |

Derived: Physical DR = 252/(252+500) = 33.5%. Magical DR = 330/(330+600) = 35.5%. MACC = 0.5 × INT + 0.5 × ACC = 286. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Last-Note Mercy.** When an ally falls below 30% HP while a boss chant is targeting that ally, Seraphine's next Cure Cascade on that ally has Chant Time 0 and costs 0 MP. Triggers at most once per 6,000 ticks.

### Abilities

**1. Cure Cascade**

- Damage Type: Healing
- Chain Property: None
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 900 ticks
- MP Cost: 140
- Base VE / CE: 0 / 0
- Multiplier: Heal 3.20 × INT (single target)
- Tactical Utility: Primary heal. Generates heal-derived VE (40% of HP restored). Heals ≥ 1,500 HP are heavy-tagged, so Seraphine's Phase Dampener decays that VE twice as fast.

**2. Circuit Benediction**

- Damage Type: Support (buff)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 400 ticks
- MP Cost: 90
- Base VE / CE: 300 / 100 (after Aethel-Born ×0.60 CE generation: 60)
- Multiplier: Regen 0.15 × INT per 500 ticks for 4,000 ticks; +20 Concentration
- Tactical Utility: Single-target regen plus +20 Concentration, which reduces Interrupt% by 20 percentage points on the target's chants.

**3. Phase Sanctuary**

- Damage Type: Support (party buff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,500 ticks
- MP Cost: 260
- Base VE / CE: 1,400 / 200 (after Aethel-Born ×0.60 CE generation: 120) — heavy-tagged
- Multiplier: 0.00 (no damage); party −25% damage taken for 3,000 ticks
- Tactical Utility: Party damage reduction; hits taken while it is active count as mitigated (no CE shed). Heavy-tagged VE.

### Synergy notes

The safest healer against VE spikes thanks to Phase Dampener; Circuit Benediction feeds Concentration to Kith-Lir casters so their chants survive.

---

## Hero 11 — Pim Quadrant-Oss, "The Pocket Metronome"

| Field | Value |
|---|---|
| Name | Pim Quadrant-Oss |
| Epithet | The Pocket Metronome |
| Race | Kith-Lir |
| Archetype | Buffer/Aura Support (Arcane focus) |
| Racial passive | Aetheric Condenser: Magic Bursts refund 65% MP (replacing the standard 50%) and extend the burst window by 600 ticks (max +1,200 per window). |

Pim carries a brass pocket-orrery that computes optimal cast sequences in real time. She tunes allies like instruments, pushing haste and spell amplification onto whoever is about to act.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,000 | 900 | 180 | 280 | 320 | 300 | 200 | 150 | 12 |
| Race modifier | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Individual tuning | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +8% |
| **Final** | **4,250** | **1,215** | **153** | **196** | **416** | **390** | **210** | **158** | **13** |

Derived: Physical DR = 196/(196+500) = 28.2%. Magical DR = 390/(390+600) = 39.4%. MACC = 0.5 × INT + 0.5 × ACC = 313. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 13⌉ = 770 ticks.

### Unique Situational Bonus

**Proof by Timing.** If Pim acts within 300 ticks after a Kith-Lir ally detonated a Magic Burst, the durations of Pim's buffs on that action are increased by 50%.

### Abilities

**1. Haste Theorem**

- Damage Type: Support (buff)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 300 ticks
- MP Cost: 100
- Base VE / CE: 400 / 100
- Multiplier: 0.00 (no damage); target +25% AGI for 5,000 ticks
- Tactical Utility: Single-target haste. Counts against the +50% Haste cap.

**2. Barrier Proof**

- Damage Type: Support (party buff)
- Chain Property: None
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 600 ticks
- MP Cost: 160
- Base VE / CE: 600 / 150
- Multiplier: 0.00 (no damage); party +20% MEVA and +15% all elemental resistance for 5,000 ticks
- Tactical Utility: Answer to boss elemental nukes. Elemental resistance from buffs caps at +90% total.

**3. Prism Overclock**

- Damage Type: Support (buff)
- Chain Property: None
- Recovery Cost: Heavy / Tier-3 (14,000 AP)
- Chant Time: 1,000 ticks
- MP Cost: 200
- Base VE / CE: 900 / 100 — heavy-tagged
- Multiplier: 0.00 (no damage); target's next spell +40% damage and +20% Fast Cast
- Tactical Utility: The +20% Fast Cast applies within the total Fast Cast cap of 65%. The damage bonus is a separate multiplier (it does not add to Magic Burst bonuses). Heavy-tagged VE.

### Synergy notes

Prism Overclock on Zeph Tri-Lumen before a burst window produces the roster's single largest spell; Haste Theorem on a linker tightens chain timing.

---

## Hero 12 — Hald Brek-Sorrow, "The Chanting Bedrock"

| Field | Value |
|---|---|
| Name | Hald Brek-Sorrow |
| Epithet | The Chanting Bedrock |
| Race | Veth-Kari |
| Archetype | Buffer/Aura Support (Arcane focus) |
| Racial passive | Chitinous Grounding: 15% of post-mitigation physical damage taken is added to this hero's VE on the attacker. |

Hald is a Veth-Kari war-chanter whose hymns are struck, not sung: he beats rhythm into his own chitin with a flanged hammer, and the vibration hardens the lattice of everyone nearby.

### Baseline stats (Level 50 reference)

| Stat | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Archetype baseline | 5,000 | 900 | 180 | 280 | 320 | 300 | 200 | 150 | 12 |
| Race modifier | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Individual tuning | +5% | +0% | +0% | +0% | +0% | +0% | +0% | +0% | +0% |
| **Final** | **5,780** | **900** | **189** | **364** | **288** | **240** | **200** | **135** | **11** |

Derived: Physical DR = 364/(364+500) = 42.1%. Magical DR = 240/(240+600) = 28.6%. MACC = 0.5 × INT + 0.5 × ACC = 244. Ticks to fill 0 → 10,000 AP at base AGI = ⌈10,000 / 11⌉ = 910 ticks.

### Unique Situational Bonus

**Second Voice.** While Hald holds the second-highest total enmity (VE+CE) on the boss, his aura abilities also grant +10% DEF to the ally holding the highest enmity, for the same duration as the aura.

### Abilities

**1. Bedrock Chant**

- Damage Type: Support (party aura)
- Chain Property: None
- Recovery Cost: Stance (4,000 AP)
- Chant Time: 600 ticks
- MP Cost: 110
- Base VE / CE: 500 / 200
- Multiplier: 0.00 (no damage); party +20% DEF for 6,000 ticks
- Tactical Utility: Physical mitigation aura. Also triggers Second Voice.

**2. Warcry of the Deep**

- Damage Type: Support (party aura)
- Chain Property: None
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 90
- Base VE / CE: 700 / 400
- Multiplier: 0.00 (no damage); party +15% ATK and +10% crit chance for 4,000 ticks
- Tactical Utility: Instant offensive aura. Stacks with Pim's and Lyr's buffs because it touches different stats.

**3. Hammer of Hymns**

- Damage Type: Physical
- Chain Property: Blunt
- Recovery Cost: Standard (10,000 AP)
- Chant Time: 0 ticks (instant)
- MP Cost: 0
- Base VE / CE: 0 / 500
- Multiplier: 1.80 × ATK
- Tactical Utility: Blunt attack that extends the remaining duration of every active party buff by 1,000 ticks (once per buff instance). Opens Blunt windows.

### Synergy notes

A physical-party buffer; Warcry of the Deep supercharges linker crits (and so Cadence Surge refunds), while Hammer of Hymns extends every party buff.

---
