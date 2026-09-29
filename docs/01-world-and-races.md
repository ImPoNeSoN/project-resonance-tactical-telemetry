# 01 — World and Races: "Aetheric Harmonics"

## 1.1 Core premise: magic is physics

In the world of Resonance, magic is not a gift or a prayer. It is a measurable field quantity called **aether**, and it obeys conservation laws like heat or charge. Every piece of matter has a **natural resonant frequency**. When aether is driven into matter at that frequency, the matter's internal bonds absorb energy faster than they can shed it, and the matter **destabilizes**: stone liquefies, metal crystallizes, flesh fragments, light condenses into mass.

The founding observation of the setting is the **Harmonic Principle**:

> Matter struck twice at complementary frequencies within a short interval does not add the two impacts. It multiplies them.

This principle is the in-world explanation for every combat mechanic:

| World physics | Game mechanic | Rule reference |
|---|---|---|
| A strike imprints a frequency (Blunt, Piercing, Slashing, or one of eight elemental harmonics) on the target's structure | Chain Property / Level 1 window | 02 §2.10.2 |
| The imprinted frequency rings out and fades in about four seconds of combat time | 4,000-tick Resonance window | 02 §2.10.2 |
| A complementary second frequency causes constructive interference: a Resonance Detonation | Level 2 Resonance | 02 §2.10.3 |
| A third frequency tuned to the interference pattern causes cascade failure (an "Apex") | Level 3 Resonance | 02 §2.10.4 |
| After a detonation, the target's structure is briefly "open": spells of the matching harmonic pass through its defenses | Magic Burst window (1,500 ticks) | 02 §2.11 |
| Time itself is quantized into aetheric ticks; living beings act when their internal potential crests | CTB timeline, 10,000-AP gauge | 02 §2.2 |
| Chanting is the act of tuning one's body into a transmitter; a sharp shock detunes it | Chant Time and Interrupt% | 02 §2.3 |
| Enmity is literal: creatures perceive aetheric "noise" and turn toward the loudest source | Volatile / Cumulative Enmity | 02 §2.9 |

### 1.1.1 The two kinds of noise

Scholars of the Kith-Lir Sub-Hex Academies classify aetheric noise in two bands:

- **Volatile noise** (VE): sharp spikes from sudden energy events such as heals, taunts, and large spells. It rings loudly and fades by about a tenth every half-second (every 500 ticks).
- **Cumulative noise** (CE): the low hum of sustained violence. Every blow leaves a permanent trace in the target's perception. It fades only when the target physically strikes the source and "grounds" part of that trace (the CE shed rule).

### 1.1.2 Aether Density

Aether pools in deep places. The deeper one descends into a **Resonance Shaft** (the game's dungeons), the denser the ambient aether becomes. Dense aether makes every detonation more violent and pushes creatures into frenzy, and eventually the ambient field begins to destabilize the explorers themselves. This is the **Aether Density Meter** (05 §5.2).

### 1.1.3 Timeline and history

| Era | Years (Aetheric Reckoning, A.R.) | Event |
|---|---|---|
| The Quiet Crust | before 0 A.R. | Surface aether is thin; the races live apart in their ecological niches. |
| The First Chord | 0 A.R. | A Kith-Lir geometrician proves the Harmonic Principle by collapsing a mountain spur with two tuning forks. |
| The Shaft Rush | 112–240 A.R. | Every nation digs for dense aether. Ash-Dravan reclaimer caravans form to scavenge collapsed shafts. |
| The Choir Collapse | 241 A.R. | A Veth-Kari city tuned too many resonance columns at once and fell into the earth. The Hollow Choir cavern is its grave. |
| Synthesis | 300–380 A.R. | Aethel-Born synthetics are built as city-scale aether stabilizers. |
| The Unsealing | 412 A.R. (game start) | World Bosses, aether-saturated constructs and beasts of impossible scale, begin rising from the deepest shafts. The **Resonance Accord** is formed: five races field joint tactical squads. |

### 1.1.4 Factions

| Faction | Role in the game | Player interaction |
|---|---|---|
| The Resonance Accord | The player's organization; runs squads, hosts the Ghost Protocol arena | Hub, roster management, arena and raid matchmaking |
| The Sub-Hex Academies (Kith-Lir) | Research; sells schematic refinement | Schematic Chip refinement vendor (05 §5.6) |
| Reclaimer Caravans (Ash-Dravan) | Salvage economy | Extraction broker; buys run yields at Extract nodes |
| The Deepward Chitin-Holds (Veth-Kari) | Maintain the upper shafts | Dungeon entrance and first three floors |
| The Stalker Lodges (Sylvari-Mor) | Scouts and timeline-seers | Provide floor intel (reveal elite spawn positions) |
| The Stabilizer Consortium (Aethel-Born) | Maintain cities' counter-frequencies | Global event source for World Boss raids |

---

## 1.2 Race overview

| # | Race | Epithet | Height | Stat identity | Signature passive |
|---|---|---|---|---|---|
| 1 | Veth-Kari | Chitin-Fused Vanguard | 6 ft 4 in – 7 ft 2 in | High DEF, low MEVA | Chitinous Grounding |
| 2 | Sylvari-Mor | Aether-Attuned Feline Stalkers | 5 ft 6 in – 6 ft 2 in | High AGI, high EVA, low Max HP | Cadence Surge |
| 3 | Kith-Lir | Micro-Stature Sub-Hex Geometricians | 3 ft 0 in – 3 ft 6 in | Highest INT and MP, exceptional MEVA, minimal DEF | Aetheric Condenser |
| 4 | Ash-Dravan | Obsidian-Veined Ashen Reclaimers | 6 ft 0 in – 6 ft 8 in | High ATK, Burn immunity, high Elemental Penetration | Thermal Battery |
| 5 | Aethel-Born | Resonant Synth-Elves | 5 ft 10 in – 6 ft 4 in | High Fast Cast, minimal CE generation | Phase Dampener |

### 1.2.1 Race stat modifiers (applied multiplicatively to archetype baselines)

| Race | HP | MP | ATK | DEF | INT | MEVA | ACC | EVA | AGI | Innate extras |
|---|---|---|---|---|---|---|---|---|---|---|
| Veth-Kari | +10% | +0% | +5% | +30% | −10% | −20% | +0% | −10% | −10% | none |
| Sylvari-Mor | −20% | −10% | +5% | −10% | +0% | +0% | +10% | +30% | +25% | none |
| Kith-Lir | −15% | +35% | −15% | −30% | +30% | +30% | +5% | +5% | +0% | none |
| Ash-Dravan | +5% | −5% | +15% | +5% | +0% | −10% | +0% | −10% | +0% | +75 EPEN; Burn immunity |
| Aethel-Born | −5% | +10% | −10% | −10% | +10% | +10% | +0% | +0% | +5% | +15% Fast Cast; CE generation ×0.60 |

How these hit the user-specified identities:

- **Veth-Kari** "High base Phys DEF, low MEVA": +30% DEF and −20% MEVA, the largest DEF and the lowest MEVA modifier in the game.
- **Sylvari-Mor** "High AGI, high EVA, low Max HP": +25% AGI, +30% EVA, and −20% HP, the lowest HP modifier.
- **Kith-Lir** "Highest INT and MP, exceptional MEVA, minimal Phys DEF": +30% INT and +35% MP (both highest), +30% MEVA (highest), −30% DEF (lowest).
- **Ash-Dravan** "Burn immunity, high Elemental Penetration": Burn immunity plus +75 EPEN (−15 percentage points of target resistance), the only innate EPEN.
- **Aethel-Born** "High Fast Cast, minimal Cumulative Enmity generation": +15% innate Fast Cast (the only race that can exceed the 50% gear cap) and ×0.60 CE generation.

Hero stat blocks are derived in [03-hero-roster.md](03-hero-roster.md) §3.1.

---

## 1.3 Veth-Kari — Chitin-Fused Vanguard

### 1.3.1 Biology

Veth-Kari are subterranean apex humanoids. Their skeleton is not bone but a **keratin lattice**: a honeycomb of layered keratin plates fused to an exoskeletal shell that covers the shoulders, forearms, spine and shins. The lattice regrows after fracture in stronger patterns, so an old Veth-Kari is visibly more armored than a young one; elders count their years in lattice rings.

- **Senses:** vestigial eyes, but a highly developed seismic sense through the lattice. They "hear" footsteps through stone at 200 meters.
- **Metabolism:** slow, mineral-heavy diet. They can go nine days without food.
- **Aetheric trait:** the lattice is extremely good at absorbing and dispersing **physical** frequencies (hence high DEF) but it is a poor insulator against **arcane** harmonics, which pass through the hollow honeycomb cells (hence low MEVA).
- **Lifespan:** 180–220 years.
- **Reproduction:** clutch-laying; young are raised communally in the Chitin-Holds.

### 1.3.2 Visual silhouette

- Broad trapezoidal torso with a dorsal ridge of overlapping plates that rises above the head line: the silhouette reads as "a walking rampart" from the 35° isometric camera.
- Forearm plates flare into natural bucklers.
- Palette: slate grey and umber chitin, with bioluminescent amber seams at lattice joints that brighten when Chitinous Grounding triggers.
- Idle animation: slow side-to-side weight shift; plates clack in a 4-beat rhythm.
- Material notes (PBR): matte carbon-like chitin (roughness 0.85), with polished brass inlays on ceremonial plates (metallic 1.0, roughness 0.25).

### 1.3.3 Culture

Veth-Kari live in the **Deepward Chitin-Holds**, stratified cities carved into shaft walls. Their society values endurance and the "held line." Their music is percussive (struck chitin). The Choir Collapse of 241 A.R. left a cultural wound: Veth-Kari distrust anyone who tunes resonance recklessly, and they volunteer as tanks for the Accord because they believe someone should stand between the reckless and the ground.

### 1.3.4 Signature passive: Chitinous Grounding

> 15% of physical damage taken converts to Volatile Enmity retention.

Exact rule: on every physical hit taken, the Veth-Kari gains `floor(0.15 × post-mitigation physical damage)` VE on the attacking sub-target's enmity table (02 §2.12.1). Worked value: Korrith Vael-Dun taking a 1,200-damage Piston Sweep gains 180 VE.

Design purpose: tanks naturally generate the most VE exactly when they are doing their job (getting hit), which protects their lead while CE sheds from unmitigated hits.

### 1.3.5 Veth-Kari heroes

| # | Hero | Archetype |
|---|---|---|
| 01 | Korrith Vael-Dun | Anchor Tank |
| 02 | Drusk Oma-Teth | Anchor Tank |
| 07 | Gorrun Delve-Mace | Physical Linker |
| 12 | Hald Brek-Sorrow | Buffer/Aura Support |
| 24 | Tollen Geode-Vast | Resource Battery |

---

## 1.4 Sylvari-Mor — Aether-Attuned Feline Stalkers

### 1.4.1 Biology

Sylvari-Mor are digitigrade hunters with feline morphology: long hind limbs with an extra ankle joint, retractable claws, tufted ears, and a long counterbalancing tail. Their defining trait is **timeline perception**: their nervous system samples the aetheric tick rate directly, so they perceive combat as a sequence of discrete moments rather than a continuous flow.

- **Senses:** exceptional low-light vision; whisker-like vibrissae detect aetheric turbulence (they can feel an open Resonance window on a target).
- **Metabolism:** fast, carnivorous; they burn energy quickly and have low fat reserves, which is the in-world reason for low Max HP.
- **Aetheric trait:** a Sylvari-Mor that lands a perfect strike (a crit) or links a chain experiences a "cadence lock," a brief acceleration of their personal timeline. This is Cadence Surge.
- **Lifespan:** 60–80 years.

### 1.4.2 Visual silhouette

- Lean, forward-leaning posture; the digitigrade legs and tail make a distinctive "Z" silhouette.
- Ears always visible above the head: the most readable race from the isometric camera at 4K and at 1080p.
- Palette: dusk violets, sand, and silver fur patterns; eyes emit faint cyan light (emissive, 2.0 intensity) that pulses on Cadence Surge.
- Idle animation: tail flicks every 1.5 seconds; weight on the balls of the feet.
- Material notes: fur uses a shell/fin-free anisotropic shader for performance; leather harness with polished brass buckles.

### 1.4.3 Culture

Organized into **Stalker Lodges**, meritocratic hunting bands. They keep no written history; instead each lodge memorizes "the Seventh Second," a sequence of perfect moments passed down orally. They are the Accord's scouts.

### 1.4.4 Signature passive: Cadence Surge

> Crits and physical chain links refund 1,200 AP.

Exact rule: when an action contains at least one critical hit OR is a Physical/Elemental-Physical ability that produces a valid L2 or L3 transition, refund 1,200 AP after recovery (maximum once per action; refund cannot raise AP above 10,000). 02 §2.12.2.

Worked value: Mirrim Ash-Pounce at AGI 18 needs 556 ticks for a full 10,000 AP; a 1,200 refund saves 67 ticks, about 12% of her turn.

### 1.4.5 Sylvari-Mor heroes

| # | Hero | Archetype |
|---|---|---|
| 05 | Saeli Thorn-Vesper | Physical Linker |
| 06 | Mirrim Ash-Pounce | Physical Linker |
| 14 | Vessa Quiet-Claw | Debuffer/Disrupter |
| 18 | Kaelis Moon-Ravel | Spike DPS |

---

## 1.5 Kith-Lir — Micro-Stature Sub-Hex Geometricians

### 1.5.1 Biology

Kith-Lir stand 3 to 3.5 feet tall but are extremely dense: an adult weighs about 70 kg (the density of a person twice their height). Their bones are compact mineral lattices arranged in sub-hexagonal (hexagons subdivided into triangles) patterns, which is where the Academies' geometry comes from. They are born with ordinary eyes, but at adolescence every Kith-Lir is fitted with **synthetic ocular arrays**: brass-and-crystal lens clusters that read aetheric field lines directly.

- **Senses:** ocular arrays give them visible perception of frequencies, including Resonance windows, burst windows and enemy chant progress.
- **Metabolism:** low; they eat mineral-rich broths.
- **Aetheric trait:** the sub-hex skeleton acts as a **condenser**. When a burst spell passes through a Resonance-opened target, the backwash is caught by the Kith-Lir's own skeleton and returned as MP; the ringing lattice also holds the target's structure open longer. This is Aetheric Condenser.
- **Physical weakness:** dense but brittle under shear; minimal physical DEF.
- **Lifespan:** 140–160 years.

### 1.5.2 Visual silhouette

- Short, compact, wide-stanced; oversized coats and mantles add visual mass so they read clearly at the isometric camera.
- The ocular array is a large emissive feature (a cluster of 3–7 lenses) that glows in the color of the element they are currently chanting.
- Palette: deep teal and cream robes, brass instruments, crystalline ice-blue lenses.
- Idle animation: head-tilt scanning; lenses rotate.
- Material notes: crystalline lenses use a refractive shader with a screen-space refraction fallback to a cubemap on low settings; brass is polished (roughness 0.2).

### 1.5.3 Culture

The **Sub-Hex Academies** are their institutions: debate-halls where proofs are demonstrated by collapsing small objects. Social rank is determined by published proofs. They are the Accord's scientists and are responsible for Schematic Chip refinement.

### 1.5.4 Signature passive: Aetheric Condenser

> Magic Bursts detonated refund 65% MP and extend burst window by 600 ticks.

Exact rule (02 §2.11.5):

- A Magic Burst cast by a Kith-Lir refunds 65% of its MP cost. This **replaces** the standard 50% burst refund; it does not add to it (a 115% refund would make bursting free and remove MP as a constraint).
- Each Kith-Lir Magic Burst extends the current burst window by 600 ticks; total extension per window is capped at +1,200 ticks (maximum window length 2,700 ticks).
- Applies to Magic Bursts only, not Physical Bursts.

Worked value: Zeph Tri-Lumen bursting Blizzard II (180 MP) regains 117 MP instead of 90.

### 1.5.5 Kith-Lir heroes

| # | Hero | Archetype |
|---|---|---|
| 04 | Tessel Primewright | Anchor Tank |
| 11 | Pim Quadrant-Oss | Buffer/Aura Support |
| 13 | Nyx Sub-Vector | Debuffer/Disrupter |
| 17 | Zeph Tri-Lumen | Spike DPS |
| 22 | Mox Relay-Pell | Resource Battery |

---

## 1.6 Ash-Dravan — Obsidian-Veined Ashen Reclaimers

### 1.6.1 Biology

Ash-Dravan are broad-shouldered nomads whose circulatory system carries **molten elemental plasma** instead of blood: a slurry of mineral salts held liquid by constant aetheric agitation. It shows through their skin as glowing obsidian veins. Their body temperature is 70°C at rest.

- **Senses:** heat-sensing pits beneath the eyes.
- **Metabolism:** they eat ash and mineral slag from collapsed shafts; food is scarce, so they are nomadic.
- **Aetheric trait 1 (Burn immunity):** fire cannot burn what is already molten. They are immune to Burn DoT.
- **Aetheric trait 2 (Elemental Penetration):** their strikes carry heat that pre-destabilizes a target's elemental wards (+75 EPEN).
- **Aetheric trait 3 (Thermal Battery):** elemental energy absorbed from attacks heats their plasma. When it reaches saturation, their next strike vents it as a forced detonation.
- **Lifespan:** 90–110 years.

### 1.6.2 Visual silhouette

- Widest shoulders in the game; heavy head-down posture; the silhouette reads as an inverted triangle.
- Veins glow and brighten with Heat: emissive intensity scales linearly from 0.5 at 0 Heat to 6.0 at 100 Heat, so players can read Heat without the HUD.
- Palette: charcoal skin, orange-to-white vein emissive, reclaimed metal armor with patina.
- Idle animation: steam vents from shoulder plates every 3 seconds (GPU particles, 24 particles per vent).
- Material notes: skin uses a subsurface-free cheap "emissive crack" mask to stay within the performance budget.

### 1.6.3 Culture

**Reclaimer Caravans** travel between collapsed shafts salvaging aether-crystal and artifacts. They are traders and brokers; in-game, they run the Extraction economy. They value debts repaid; "the Tithe" is a sacred concept.

### 1.6.4 Signature passive: Thermal Battery

> Elemental damage taken builds Heat; at 100 Heat next weapon skill forces a Level 2 Resonance Detonation.

Exact Heat gain formula (02 §2.12.4):

```
HeatGain = max(3, ceil(300 × ElementalDamageTaken / MaxHP))
If the hit is Fire: HeatGain = ceil(HeatGain × 1.5)
Heat = min(100, Heat + HeatGain)
Decay: −5 Heat per 1,000 ticks with no elemental damage taken
```

Worked values for Varra Kesh-Ember (8,740 Max HP):

| Elemental hit | Calculation | Heat gained |
|---|---|---|
| 200 Lightning | max(3, ceil(300 × 200 / 8,740)) = max(3, ceil(6.86)) | 7 |
| 874 Ice | ceil(300 × 874 / 8,740) = 30 | 30 |
| 874 Fire | 30 × 1.5 | 45 |
| 2,000 Fire | ceil(300 × 2,000 / 8,740) = 69; × 1.5 = 103.5 → 104 | 100 (capped) |
| 40 Water (chip) | max(3, 2) | 3 |

At 100 Heat (Primed), the next weapon skill either produces its natural L2/L3 transition with +25% detonation damage, or forces a Liquefaction L2 detonation. Heat then resets to 0.

### 1.6.5 Ash-Dravan heroes

| # | Hero | Archetype |
|---|---|---|
| 03 | Varra Kesh-Ember | Anchor Tank |
| 08 | Ishka Pyre-Lash | Physical Linker |
| 16 | Crag Moor-Ash | Debuffer/Disrupter |
| 19 | Thurga Ember-Maw | Spike DPS |
| 23 | Brannoch Ash-Well | Resource Battery |

---

## 1.7 Aethel-Born — Resonant Synth-Elves

### 1.7.1 Biology (engineering)

Aethel-Born are synthetic beings built by the Stabilizer Consortium between 300 and 380 A.R. They have elven proportions, a **porcelain ceramic shell**, and **subcutaneous circuit traces**: fine channels of conductive crystal visible beneath the translucent glaze. They were designed as counter-frequency emitters to keep cities from shaking apart, and each one contains a harmonic damper core.

- **Senses:** full-spectrum optical sensors and aetheric field sampling.
- **Sustenance:** aether intake through the circuit traces; they "eat" ambient field.
- **Aetheric trait 1 (Fast Cast):** they are built to modulate frequencies, so chanting is native to them (+15% innate Fast Cast).
- **Aetheric trait 2 (minimal CE):** their damper core suppresses the lasting noise of their actions (CE × 0.60).
- **Aetheric trait 3 (Phase Dampener):** heavy spikes of volatile noise are damped actively, fading at double speed.
- **Lifespan:** indefinite with maintenance; the oldest active unit is 112 years.
- **Sapience:** full; Aethel-Born have legal personhood under the Accord since 391 A.R.

### 1.7.2 Visual silhouette

- Tall, slender, upright; elongated ears and a halo-like dorsal emitter ring behind the head (the most distinctive element from the isometric camera).
- Circuit traces glow along the limbs; while chanting, the traces animate as flowing light toward the hands (UV-scrolled emissive mask).
- Palette: white and ivory porcelain, gold circuit traces, occasional cyan or rose emitters by model line.
- Idle animation: perfectly still apart from the rotating dorsal ring (uncanny by design).
- Material notes: porcelain uses clearcoat (clearcoat 1.0, clearcoat roughness 0.1) over a low-roughness base; circuits are emissive masks.

### 1.7.3 Culture

Aethel-Born identify by **model line** and **tuning** (the frequency they were built to damp). Many seek meaning beyond their original function; joining the Accord is a common path. They make natural healers and supports because of how cleanly their heavy spells fade from enemy attention.

### 1.7.4 Signature passive: Phase Dampener

> VE from heavy heals/spells decays 2x speed.

Exact rule (02 §2.9.2): Aethel-Born VE is split into a normal bucket (decays 10% per 500 ticks) and a heavy bucket (decays 20% per 500 ticks). Heavy-tagged sources: single heals restoring ≥ 1,500 HP, abilities with Base VE ≥ 800, and abilities with Heavy (14,000 AP) recovery.

Worked value: Seraphine Vol-Ivory's Phase Sanctuary (1,400 heavy VE) decays 1,400 → 1,120 → 896 → 716 → 572 over 2,000 ticks. A non-Aethel-Born with the same 1,400 VE would still hold 918 after the same time.

### 1.7.5 Aethel-Born heroes

| # | Hero | Archetype |
|---|---|---|
| 09 | Lyr Aurelis-7 | Buffer/Aura Support |
| 10 | Seraphine Vol-Ivory | Buffer/Aura Support |
| 15 | Ondrel Vey-Static | Debuffer/Disrupter |
| 20 | Aurel Nine-Vesper | Spike DPS |
| 21 | Ilune Cache-Aria | Resource Battery |

---

## 1.8 Inter-race relations matrix

| | Veth-Kari | Sylvari-Mor | Kith-Lir | Ash-Dravan | Aethel-Born |
|---|---|---|---|---|---|
| **Veth-Kari** | — | Respect (scouts keep holds safe) | Wary (reckless tuners) | Trade partners (slag for salvage) | Protective (Aethel-Born stabilize holds) |
| **Sylvari-Mor** | Admire their patience | — | Amused by them | Competitive (salvage rights) | Curious (they cannot read Aethel-Born timelines) |
| **Kith-Lir** | Consider them slow | Study their timeline sense | — | Buy their crystal | Collaborators (co-designed damper cores) |
| **Ash-Dravan** | Owe them old debts | Rivals | Customers | — | Suspicious (built by the Consortium that sealed shafts) |
| **Aethel-Born** | Grateful | Fond | Colleagues | Seeking reconciliation | — |

Gameplay effect: none mechanically (no bonuses for race composition), but the relations drive the bark system: pre-fight lines and chain callouts differ by pairing. Each hero has 4 pairing barks per other race (5 races × 4 = 20 barks per hero; 480 total, localized).

---

## 1.9 Geography

| Region | Surface climate | Shafts (dungeons) | Dominant race | Boss element bias |
|---|---|---|---|---|
| The Glass Barrens | Volcanic desert | Cinder Throat (12 floors) | Ash-Dravan | Fire, Earth |
| Deepward Escarpment | Temperate highlands | The Hollow Choir (15 floors) | Veth-Kari | Earth, Darkness |
| Veiled Canopy | Twilight forest | Stalker's Descent (12 floors) | Sylvari-Mor | Wind, Lightning |
| Tessellate Reach | Crystalline tundra | Sub-Hex Well (15 floors) | Kith-Lir | Ice, Light |
| Consortium Spires | Megacity | The Null Foundry (18 floors) | Aethel-Born | Lightning, Water |
| The Unsealed Rift | Aetheric storm | World Boss arena only | none | All |

Floor counts and element biases feed the dungeon generator in 05 §5.1.
