# 03 — Hero Roster: Index, Distribution and Stat Summary

This file indexes all 24 heroes. Full hero sheets are in [03a-hero-roster-01-12.md](03a-hero-roster-01-12.md) (Heroes 01–12) and [03b-hero-roster-13-24.md](03b-hero-roster-13-24.md) (Heroes 13–24). Formulas referenced here are defined in [02-combat-engine.md](02-combat-engine.md); race modifiers are defined in [01-world-and-races.md](01-world-and-races.md).

## 3.1 Stat derivation rule

Every hero's baseline stats are computed as:

```
Final(stat) = round( ArchetypeFocusBaseline(stat) × (1 + RaceModifier(stat)) × (1 + IndividualTuning(stat)) )
```

HP rounds to the nearest 10, MP to the nearest 5, every other stat to the nearest integer (half rounds up). Individual tuning is capped at ±8% per stat and at most two stats per hero, so race identity always dominates.

### 3.1.1 Archetype baselines (Level 50 reference)

| Archetype | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Anchor Tank | 8,000 | 300 | 260 | 520 | 120 | 300 | 220 | 120 | 10 |
| Physical Linker | 5,600 | 250 | 380 | 300 | 110 | 180 | 280 | 180 | 14 |
| Buffer/Aura Support | 5,000 | 900 | 180 | 280 | 320 | 300 | 200 | 150 | 12 |
| Debuffer/Disrupter | 4,800 | 800 | 200 | 240 | 380 | 320 | 260 | 170 | 13 |
| Spike DPS | 4,600 | 400 | 440 | 230 | 260 | 200 | 290 | 190 | 13 |
| Resource Battery | 5,200 | 1,100 | 160 | 260 | 300 | 280 | 200 | 150 | 11 |

### 3.1.2 Focus profiles (override ATK and INT of the archetype baseline)

Some archetypes contain both weapon users and casters. A focus profile replaces only the ATK and INT columns.

| Archetype | Focus | ATK | INT | Used by |
|---|---|---|---|---|
| Anchor Tank | Physical | 260 | 120 | Korrith Vael-Dun, Drusk Oma-Teth, Varra Kesh-Ember |
| Anchor Tank | Arcane | 180 | 300 | Tessel Primewright |
| Physical Linker | Physical | 380 | 110 | Saeli Thorn-Vesper, Mirrim Ash-Pounce, Gorrun Delve-Mace, Ishka Pyre-Lash |
| Buffer/Aura Support | Arcane | 180 | 320 | Lyr Aurelis-7, Seraphine Vol-Ivory, Pim Quadrant-Oss, Hald Brek-Sorrow |
| Debuffer/Disrupter | Arcane | 200 | 380 | Nyx Sub-Vector, Ondrel Vey-Static |
| Debuffer/Disrupter | Physical | 360 | 200 | Vessa Quiet-Claw |
| Debuffer/Disrupter | Hybrid | 300 | 320 | Crag Moor-Ash |
| Spike DPS | Physical | 440 | 180 | Kaelis Moon-Ravel, Thurga Ember-Maw |
| Spike DPS | Arcane | 200 | 440 | Zeph Tri-Lumen, Aurel Nine-Vesper |
| Resource Battery | Arcane | 160 | 300 | Ilune Cache-Aria, Mox Relay-Pell, Brannoch Ash-Well, Tollen Geode-Vast |

### 3.1.3 Race modifiers (multiplicative)

| Race | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|
| Veth-Kari | +10% | +0% | +5% | +30% | -10% | -20% | +0% | -10% | -10% |
| Sylvari-Mor | -20% | -10% | +5% | -10% | +0% | +0% | +10% | +30% | +25% |
| Kith-Lir | -15% | +35% | -15% | -30% | +30% | +30% | +5% | +5% | +0% |
| Ash-Dravan | +5% | -5% | +15% | +5% | +0% | -10% | +0% | -10% | +0% |
| Aethel-Born | -5% | +10% | -10% | -10% | +10% | +10% | +0% | +0% | +5% |

## 3.2 Distribution table (race × archetype)

| Archetype | Veth-Kari | Sylvari-Mor | Kith-Lir | Ash-Dravan | Aethel-Born | Total |
|---|---|---|---|---|---|---|
| Anchor Tank | #01 Korrith Vael-Dun<br>#02 Drusk Oma-Teth | — | #04 Tessel Primewright | #03 Varra Kesh-Ember | — | 4 |
| Physical Linker | #07 Gorrun Delve-Mace | #05 Saeli Thorn-Vesper<br>#06 Mirrim Ash-Pounce | — | #08 Ishka Pyre-Lash | — | 4 |
| Buffer/Aura Support | #12 Hald Brek-Sorrow | — | #11 Pim Quadrant-Oss | — | #09 Lyr Aurelis-7<br>#10 Seraphine Vol-Ivory | 4 |
| Debuffer/Disrupter | — | #14 Vessa Quiet-Claw | #13 Nyx Sub-Vector | #16 Crag Moor-Ash | #15 Ondrel Vey-Static | 4 |
| Spike DPS | — | #18 Kaelis Moon-Ravel | #17 Zeph Tri-Lumen | #19 Thurga Ember-Maw | #20 Aurel Nine-Vesper | 4 |
| Resource Battery | #24 Tollen Geode-Vast | — | #22 Mox Relay-Pell | #23 Brannoch Ash-Well | #21 Ilune Cache-Aria | 4 |
| **Total** | 5 | 4 | 5 | 5 | 5 | 24 |

Design reasoning: 24 heroes do not divide evenly across 5 races, so four races field 5 heroes and Sylvari-Mor fields 4. Sylvari-Mor gets the smaller count because its racial passive (Cadence Surge) is the strongest tempo tool in the game; fewer carriers keeps it from dominating arena meta. Each archetype has 4 heroes spread over at least 3 races, so every role has more than one racial flavour.

## 3.3 Master roster table

| # | Name | Epithet | Race | Archetype | Focus | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 01 | Korrith Vael-Dun | The Unbroken Carapace | Veth-Kari | Anchor Tank | Physical | 9,240 | 300 | 273 | 710 | 108 | 240 | 220 | 108 | 9 |
| 02 | Drusk Oma-Teth | Deepwarden of the Hollow Choir | Veth-Kari | Anchor Tank | Physical | 8,800 | 300 | 295 | 656 | 108 | 240 | 220 | 108 | 9 |
| 03 | Varra Kesh-Ember | The Cinder Bulwark | Ash-Dravan | Anchor Tank | Physical | 8,740 | 285 | 299 | 546 | 120 | 284 | 220 | 108 | 10 |
| 04 | Tessel Primewright | The Hexfield Aegis | Kith-Lir | Anchor Tank | Arcane | 7,140 | 405 | 153 | 364 | 410 | 390 | 231 | 126 | 10 |
| 05 | Saeli Thorn-Vesper | Cadence of the Seventh Second | Sylvari-Mor | Physical Linker | Physical | 4,480 | 225 | 399 | 270 | 110 | 180 | 323 | 234 | 18 |
| 06 | Mirrim Ash-Pounce | The Prowling Metronome | Sylvari-Mor | Physical Linker | Physical | 4,480 | 225 | 415 | 270 | 110 | 180 | 308 | 243 | 18 |
| 07 | Gorrun Delve-Mace | The Faultline Drummer | Veth-Kari | Physical Linker | Physical | 6,160 | 250 | 423 | 390 | 99 | 144 | 280 | 162 | 12 |
| 08 | Ishka Pyre-Lash | The Whipcord Reclaimer | Ash-Dravan | Physical Linker | Physical | 5,880 | 240 | 437 | 315 | 110 | 162 | 297 | 162 | 14 |
| 09 | Lyr Aurelis-7 | The Harmonic Conductor | Aethel-Born | Buffer/Aura Support | Arcane | 4,750 | 1,040 | 162 | 252 | 352 | 330 | 200 | 150 | 13 |
| 10 | Seraphine Vol-Ivory | The Porcelain Choir | Aethel-Born | Buffer/Aura Support | Arcane | 4,750 | 990 | 162 | 252 | 373 | 330 | 200 | 150 | 13 |
| 11 | Pim Quadrant-Oss | The Pocket Metronome | Kith-Lir | Buffer/Aura Support | Arcane | 4,250 | 1,215 | 153 | 196 | 416 | 390 | 210 | 158 | 13 |
| 12 | Hald Brek-Sorrow | The Chanting Bedrock | Veth-Kari | Buffer/Aura Support | Arcane | 5,780 | 900 | 189 | 364 | 288 | 240 | 200 | 135 | 11 |
| 13 | Nyx Sub-Vector | The Null Theorem | Kith-Lir | Debuffer/Disrupter | Arcane | 4,080 | 1,080 | 170 | 168 | 514 | 416 | 287 | 179 | 13 |
| 14 | Vessa Quiet-Claw | The Hush Between Heartbeats | Sylvari-Mor | Debuffer/Disrupter | Physical | 3,840 | 720 | 378 | 216 | 200 | 320 | 286 | 232 | 16 |
| 15 | Ondrel Vey-Static | The Circuit Heretic | Aethel-Born | Debuffer/Disrupter | Arcane | 4,560 | 880 | 180 | 216 | 418 | 370 | 260 | 170 | 14 |
| 16 | Crag Moor-Ash | The Smog Warden | Ash-Dravan | Debuffer/Disrupter | Hybrid | 5,340 | 760 | 345 | 252 | 320 | 288 | 260 | 153 | 13 |
| 17 | Zeph Tri-Lumen | The Burst Cartographer | Kith-Lir | Spike DPS | Arcane | 3,710 | 540 | 170 | 161 | 606 | 260 | 305 | 200 | 13 |
| 18 | Kaelis Moon-Ravel | The Split-Second Execution | Sylvari-Mor | Spike DPS | Physical | 3,680 | 360 | 485 | 207 | 180 | 200 | 319 | 247 | 16 |
| 19 | Thurga Ember-Maw | The Furnace Headsman | Ash-Dravan | Spike DPS | Physical | 4,830 | 380 | 536 | 242 | 180 | 180 | 290 | 162 | 13 |
| 20 | Aurel Nine-Vesper | The Luminous Arithmetic | Aethel-Born | Spike DPS | Arcane | 4,370 | 440 | 180 | 207 | 508 | 220 | 290 | 190 | 14 |
| 21 | Ilune Cache-Aria | The Wellspring Protocol | Aethel-Born | Resource Battery | Arcane | 4,940 | 1,285 | 144 | 234 | 330 | 308 | 200 | 150 | 12 |
| 22 | Mox Relay-Pell | The Pocket Reactor | Kith-Lir | Resource Battery | Arcane | 4,420 | 1,560 | 136 | 182 | 390 | 364 | 210 | 158 | 12 |
| 23 | Brannoch Ash-Well | The Ember Tithe | Ash-Dravan | Resource Battery | Arcane | 5,790 | 1,045 | 184 | 273 | 300 | 252 | 200 | 135 | 11 |
| 24 | Tollen Geode-Vast | The Slow Aquifer | Veth-Kari | Resource Battery | Arcane | 5,720 | 1,190 | 168 | 338 | 270 | 224 | 200 | 135 | 10 |

## 3.4 Ability index (72 abilities)

| # | Hero | Ability | Damage Type | Chain Property | Recovery | Chant (ticks) | MP | VE / CE | Multiplier |
|---|---|---|---|---|---|---|---|---|---|
| 01 | Korrith Vael-Dun | Lattice Provoke | Enmity (no damage) | None | 4,000 | 0 | 0 | 2,200 / 300 | 0.00 (no damage) |
| 01 | Korrith Vael-Dun | Seismic Maul | Physical | Blunt | 10,000 | 0 | 0 | 0 / 700 | 2.20 × ATK |
| 01 | Korrith Vael-Dun | Keratin Bastion | Support (self buff) | None | 4,000 | 0 | 30 | 400 / 900 | 0.00 (no damage); −35% physical damage taken for 3,000 ticks |
| 02 | Drusk Oma-Teth | Choir Knell | Physical (all enemy parts) | Blunt | 10,000 | 0 | 0 | 600 / 500 | 1.40 × ATK per part |
| 02 | Drusk Oma-Teth | Geode Reprisal | Support (counter stance) | Earth (on counter) | 4,000 | 0 | 40 | 300 / 800 | Counter 2.00 × ATK Elemental-Physical (Earth) |
| 02 | Drusk Oma-Teth | Hollow Resound | Physical | Blunt | 14,000 | 0 | 0 | 1,000 / 1,800 | 3.60 × ATK |
| 03 | Varra Kesh-Ember | Slag Taunt | Elemental-Physical (Fire) | Fire | 4,000 | 0 | 20 | 1,800 / 400 | 0.60 × ATK |
| 03 | Varra Kesh-Ember | Magma Hammer | Elemental-Physical (Fire) | Fire | 10,000 | 0 | 0 | 0 / 750 | 2.00 × ATK |
| 03 | Varra Kesh-Ember | Obsidian Mantle | Support (party buff) | None | 14,000 | 600 | 90 | 500 / 1,500 | 0.00 (no damage); party −15% elemental damage taken for 4,000 ticks |
| 04 | Tessel Primewright | Sub-Hex Lattice | Support (self shield) | None | 4,000 | 300 | 80 | 800 / 600 | Absorb shield 4.00 × INT for 4,000 ticks |
| 04 | Tessel Primewright | Geometric Refutation | Magical | Light | 10,000 | 400 | 60 | 200 / 900 | 1.80 × INT |
| 04 | Tessel Primewright | Tessellate Provocation | Enmity (no damage) | None | 4,000 | 0 | 30 | 2,400 / 200 | 0.00 (no damage) |
| 05 | Saeli Thorn-Vesper | Rend Pulse | Physical | Slashing | 10,000 | 0 | 0 | 0 / 450 | 1.90 × ATK |
| 05 | Saeli Thorn-Vesper | Needle Flicker | Physical | Piercing | 4,000 | 0 | 0 | 0 / 250 | 1.10 × ATK |
| 05 | Saeli Thorn-Vesper | Wind Talon Arc | Elemental-Physical (Wind) | Wind | 10,000 | 0 | 25 | 0 / 500 | 2.10 × ATK |
| 06 | Mirrim Ash-Pounce | Talon Lance | Physical | Piercing | 10,000 | 0 | 0 | 0 / 500 | 2.00 × ATK |
| 06 | Mirrim Ash-Pounce | Frostfang Pounce | Elemental-Physical (Ice) | Ice | 10,000 | 0 | 30 | 0 / 450 | 1.80 × ATK |
| 06 | Mirrim Ash-Pounce | Timeline Stalk | Support (self buff) | None | 4,000 | 0 | 20 | 0 / 200 | 0.00 (no damage); next ability recovery −4,000 AP |
| 07 | Gorrun Delve-Mace | Faultline Crush | Physical | Blunt | 10,000 | 0 | 0 | 0 / 600 | 2.30 × ATK |
| 07 | Gorrun Delve-Mace | Ore Cleaver | Physical | Slashing | 10,000 | 0 | 0 | 0 / 550 | 2.10 × ATK |
| 07 | Gorrun Delve-Mace | Tectonic Stomp | Elemental-Physical (Earth, all parts) | Earth | 14,000 | 0 | 40 | 300 / 1,400 | 3.40 × ATK per part |
| 08 | Ishka Pyre-Lash | Ember Lash | Elemental-Physical (Fire) | Fire | 10,000 | 0 | 20 | 0 / 500 | 2.00 × ATK |
| 08 | Ishka Pyre-Lash | Chainwhip Sever | Physical | Slashing | 4,000 | 0 | 0 | 0 / 250 | 1.00 × ATK |
| 08 | Ishka Pyre-Lash | Obsidian Flay | Elemental-Physical (Darkness) | Darkness | 14,000 | 0 | 50 | 200 / 1,300 | 3.20 × ATK |
| 09 | Lyr Aurelis-7 | Tempo Aria | Support (party aura) | None | 4,000 | 800 | 120 | 500 / 150 | 0.00 (no damage); party +15% AGI for 6,000 ticks |
| 09 | Lyr Aurelis-7 | Resonant Lattice Hymn | Support (party shield) | None | 14,000 | 1,200 | 220 | 1,200 / 200 | Absorb 2.50 × INT on every ally for 5,000 ticks |
| 09 | Lyr Aurelis-7 | Radiant Refrain | Magical | Light | 10,000 | 500 | 70 | 300 / 250 | 1.20 × INT damage; party heal equal to 15% of damage dealt |
| 10 | Seraphine Vol-Ivory | Cure Cascade | Healing | None | 10,000 | 900 | 140 | 0 / 0 | Heal 3.20 × INT (single target) |
| 10 | Seraphine Vol-Ivory | Circuit Benediction | Support (buff) | None | 4,000 | 400 | 90 | 300 / 100 | Regen 0.15 × INT per 500 ticks for 4,000 ticks; +20 Concentration |
| 10 | Seraphine Vol-Ivory | Phase Sanctuary | Support (party buff) | None | 14,000 | 1,500 | 260 | 1,400 / 200 | 0.00 (no damage); party −25% damage taken for 3,000 ticks |
| 11 | Pim Quadrant-Oss | Haste Theorem | Support (buff) | None | 4,000 | 300 | 100 | 400 / 100 | 0.00 (no damage); target +25% AGI for 5,000 ticks |
| 11 | Pim Quadrant-Oss | Barrier Proof | Support (party buff) | None | 10,000 | 600 | 160 | 600 / 150 | 0.00 (no damage); party +20% MEVA and +15% all elemental resistance for 5,000 ticks |
| 11 | Pim Quadrant-Oss | Prism Overclock | Support (buff) | None | 14,000 | 1,000 | 200 | 900 / 100 | 0.00 (no damage); target's next spell +40% damage and +20% Fast Cast |
| 12 | Hald Brek-Sorrow | Bedrock Chant | Support (party aura) | None | 4,000 | 600 | 110 | 500 / 200 | 0.00 (no damage); party +20% DEF for 6,000 ticks |
| 12 | Hald Brek-Sorrow | Warcry of the Deep | Support (party aura) | None | 10,000 | 0 | 90 | 700 / 400 | 0.00 (no damage); party +15% ATK and +10% crit chance for 4,000 ticks |
| 12 | Hald Brek-Sorrow | Hammer of Hymns | Physical | Blunt | 10,000 | 0 | 0 | 0 / 500 | 1.80 × ATK |
| 13 | Nyx Sub-Vector | Vector Stun | Magical | Lightning | 4,000 | 200 | 90 | 500 / 300 | 0.80 × INT |
| 13 | Nyx Sub-Vector | Entropic Sigil | Magical (debuff) | Darkness | 10,000 | 800 | 120 | 300 / 500 | 0.60 × INT; target −20% INT and −15% MEVA for 6,000 ticks |
| 13 | Nyx Sub-Vector | Hexfold Paralysis | Magical (debuff) | Ice | 14,000 | 1,200 | 180 | 900 / 600 | 1.20 × INT; target −25% AGI for 8,000 ticks |
| 14 | Vessa Quiet-Claw | Tendon Snip | Physical (debuff) | Piercing | 10,000 | 0 | 0 | 0 / 450 | 1.40 × ATK; target −20% ATK for 5,000 ticks |
| 14 | Vessa Quiet-Claw | Shadow Pounce | Elemental-Physical (Darkness, debuff) | Darkness | 10,000 | 0 | 30 | 0 / 500 | 1.60 × ATK; target −40 ACC for 4,000 ticks |
| 14 | Vessa Quiet-Claw | Silence Mark | Support (debuff) | None | 4,000 | 0 | 40 | 300 / 300 | 0.00 (no damage); Silence for 2,000 ticks |
| 15 | Ondrel Vey-Static | Dispel Cascade | Magical (dispel) | Wind | 10,000 | 600 | 110 | 400 / 400 | 0.90 × INT; removes 1 beneficial effect (2 if a Distortion window is open) |
| 15 | Ondrel Vey-Static | Static Lattice | Magical | Lightning | 10,000 | 900 | 130 | 300 / 500 | 1.50 × INT; Shock: −10% AGI and 0.20 × INT Lightning damage per 500 ticks for 3,000 ticks |
| 15 | Ondrel Vey-Static | Aetheric Seizure | Support (debuff) | None | 14,000 | 1,400 | 240 | 1,000 / 700 | 0.00 (no damage); target Shield sub-target disabled for 3,000 ticks |
| 16 | Crag Moor-Ash | Choking Smoke | Magical (debuff) | Fire | 10,000 | 500 | 100 | 300 / 450 | 0.70 × INT; Burn 0.30 × INT per 500 ticks for 4,000 ticks |
| 16 | Crag Moor-Ash | Ash Veil | Magical (debuff) | Earth | 10,000 | 600 | 110 | 300 / 450 | 0.80 × INT; target −60 ACC for 4,000 ticks |
| 16 | Crag Moor-Ash | Slag Shackles | Elemental-Physical (Earth, debuff) | Earth | 14,000 | 0 | 60 | 200 / 1,100 | 2.40 × ATK; target −30% AGI for 4,000 ticks |
| 17 | Zeph Tri-Lumen | Blizzard II | Magical | Ice | 10,000 | 1,000 | 180 | 300 / 600 | 3.00 × INT |
| 17 | Zeph Tri-Lumen | Pyre Lattice | Magical | Fire | 14,000 | 1,400 | 320 | 700 / 1,000 | 4.40 × INT |
| 17 | Zeph Tri-Lumen | Hex Lance | Magical | Lightning | 10,000 | 600 | 140 | 200 / 500 | 2.40 × INT |
| 18 | Kaelis Moon-Ravel | Crescent Sever | Physical | Slashing | 10,000 | 0 | 0 | 0 / 600 | 2.60 × ATK |
| 18 | Kaelis Moon-Ravel | Umbral Pierce | Elemental-Physical (Darkness) | Darkness | 10,000 | 0 | 30 | 0 / 600 | 2.40 × ATK |
| 18 | Kaelis Moon-Ravel | Ravel Execution | Physical | Piercing | 14,000 | 0 | 0 | 0 / 1,500 | 5.20 × ATK |
| 19 | Thurga Ember-Maw | Headsman's Cleave | Physical | Slashing | 10,000 | 0 | 0 | 0 / 650 | 2.80 × ATK |
| 19 | Thurga Ember-Maw | Molten Verdict | Elemental-Physical (Fire) | Fire | 14,000 | 0 | 50 | 0 / 1,500 | 4.80 × ATK |
| 19 | Thurga Ember-Maw | Ash Brand | Physical | Blunt | 4,000 | 0 | 0 | 0 / 300 | 1.20 × ATK |
| 20 | Aurel Nine-Vesper | Photon Sermon | Magical | Light | 10,000 | 900 | 160 | 300 / 500 | 3.10 × INT |
| 20 | Aurel Nine-Vesper | Solar Filament | Magical | Light | 14,000 | 1,600 | 300 | 800 / 900 | 4.60 × INT |
| 20 | Aurel Nine-Vesper | Gale Quanta | Magical | Wind | 10,000 | 500 | 120 | 200 / 400 | 2.20 × INT |
| 21 | Ilune Cache-Aria | Refresh Current | Support (party aura) | None | 4,000 | 600 | 150 | 500 / 100 | 0.00 (no damage); party +20 MP per 500 ticks for 6,000 ticks |
| 21 | Ilune Cache-Aria | Aether Transfusion | Support (resource) | None | 10,000 | 400 | 0 | 400 / 100 | 0.00 (no damage); transfers 30% of Ilune's current MP to the target |
| 21 | Ilune Cache-Aria | Tempo Gift | Support (resource) | None | 14,000 | 800 | 200 | 900 / 100 | 0.00 (no damage); target +3,000 AP |
| 22 | Mox Relay-Pell | Condenser Tap | Magical | Lightning | 10,000 | 500 | 60 | 200 / 300 | 1.00 × INT; party gains MP equal to 25% of the damage dealt, split evenly |
| 22 | Mox Relay-Pell | Sub-Hex Ration | Support (resource) | None | 4,000 | 300 | 40 | 300 / 100 | 0.00 (no damage); target +1.50 × INT MP |
| 22 | Mox Relay-Pell | Overclock Relay | Support (buff) | None | 14,000 | 900 | 180 | 800 / 100 | 0.00 (no damage); target's next ability Recovery Cost −40% |
| 23 | Brannoch Ash-Well | Ember Tithe | Support (resource) | None | 4,000 | 0 | 0 | 300 / 100 | 0.00 (no damage); Brannoch loses 10% Max HP, every ally gains +150 MP |
| 23 | Brannoch Ash-Well | Cinder Pulse | Elemental-Physical (Fire) | Fire | 10,000 | 0 | 20 | 0 / 400 | 1.60 × ATK; on hit every ally gains +300 AP |
| 23 | Brannoch Ash-Well | Forge Surge | Support (buff) | None | 14,000 | 0 | 80 | 600 / 200 | 0.00 (no damage); target's next weapon skill +0.80 multiplier and its property becomes Fire; if target is Ash-Dravan, +25 Heat |
| 24 | Tollen Geode-Vast | Wellspring Tap | Support (party resource) | None | 10,000 | 700 | 0 | 500 / 100 | 0.00 (no damage); every ally +120 MP + 8% of their Max MP |
| 24 | Tollen Geode-Vast | Patient Stone | Support (resource) | None | 4,000 | 0 | 0 | 200 / 100 | 0.00 (no damage); the ally with the lowest AP gains +2,000 AP; Tollen loses 2,000 AP |
| 24 | Tollen Geode-Vast | Aquifer Surge | Magical | Water | 14,000 | 1,200 | 150 | 700 / 600 | 2.00 × INT; party +10% Max MP restored |

## 3.5 Chain property coverage

| Property | Count | Sources |
|---|---|---|
| Blunt | 6 | Seismic Maul (#01), Choir Knell (#02), Hollow Resound (#02), Faultline Crush (#07), Hammer of Hymns (#12), Ash Brand (#19) |
| Piercing | 4 | Needle Flicker (#05), Talon Lance (#06), Tendon Snip (#14), Ravel Execution (#18) |
| Slashing | 5 | Rend Pulse (#05), Ore Cleaver (#07), Chainwhip Sever (#08), Crescent Sever (#18), Headsman's Cleave (#19) |
| Fire | 7 | Slag Taunt (#03), Magma Hammer (#03), Ember Lash (#08), Choking Smoke (#16), Pyre Lattice (#17), Molten Verdict (#19), Cinder Pulse (#23) |
| Ice | 3 | Frostfang Pounce (#06), Hexfold Paralysis (#13), Blizzard II (#17) |
| Wind | 3 | Wind Talon Arc (#05), Dispel Cascade (#15), Gale Quanta (#20) |
| Earth | 4 | Geode Reprisal (#02), Tectonic Stomp (#07), Ash Veil (#16), Slag Shackles (#16) |
| Lightning | 4 | Vector Stun (#13), Static Lattice (#15), Hex Lance (#17), Condenser Tap (#22) |
| Water | 1 | Aquifer Surge (#24) |
| Light | 4 | Geometric Refutation (#04), Radiant Refrain (#09), Photon Sermon (#20), Solar Filament (#20) |
| Darkness | 4 | Obsidian Flay (#08), Entropic Sigil (#13), Shadow Pounce (#14), Umbral Pierce (#18) |

Water has a single hero source (Aquifer Surge, #24) by design; additional Water sources come from Schematic Chips with the Tidal Edge trigger (see [05-dungeon-progression.md](05-dungeon-progression.md)), which makes Water routes a build decision rather than a default.

## 3.6 Reading a hero sheet

- **Recovery Cost** is subtracted from AP after the action resolves: Standard 10,000, Stance 4,000, Heavy / Tier-3 14,000.
- **Chant Time** is in ticks before Fast Cast. Effective chant = Chant × (1 − FastCast), FastCast total cap 65%.
- **Base VE / CE** is the flat enmity on resolution. Damage-derived CE (8% of final damage) and heal-derived VE (40% of HP restored) are added on top, as defined in 02-combat-engine.md section 2.8.
- **Heavy-tagged** abilities (base VE ≥ 800 or Heavy recovery) have their VE decay at double speed for Aethel-Born casters (Phase Dampener).
- **Multiplier** is applied to ATK for Physical and Elemental-Physical, to INT for Magical and Healing.
