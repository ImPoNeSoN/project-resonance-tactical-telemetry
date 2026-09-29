# 04 — HUD and Visual Engine

Scope: camera, rendering and materials, combat feedback (shakes, particles), every HUD widget, controls, the Gambit Deck logic engine, and Godot 4.x C# implementation with a performance budget.

---

## 4.1 Visual pillars

1. **Readable math.** Every number the combat engine computes can be surfaced on screen within one second: the timeline, oscilloscope and math console exist to make the CTB and Resonance systems legible.
2. **Industrial-arcane materiality.** Four hero materials define the look: matte carbon, polished brass, emissive circuits, crystalline ice.
3. **Impact proportional to chain level.** Level 1 is a tick, Level 2 is a thud, Level 3 is an event. Shake, hit-stop, particles and audio all scale by chain level.
4. **Tactical calm.** The camera never moves on its own except for shakes; Tactical Pause is always one key away.

---

## 4.2 Camera

| Parameter | Value |
|---|---|
| Projection | Orthographic (`Camera3D.Projection = Orthogonal`) |
| Pitch | 35° downward (camera rig rotation X = −35°) |
| Yaw | 45° default; player can rotate in 90° steps (Q / E) with a 250 ms eased tween |
| Orthographic size | 18 world units (default), zoom range 12–26 |
| Near / far | 0.1 / 200 |
| Arena footprint | 24 × 24 world units (1 unit ≈ 1 meter) |
| Focus | Rig centered on arena midpoint; pans with WASD / right-stick within arena bounds |

Rationale for 35° (instead of the classic 30° dimetric): it gives more vertical read on tall bosses (World Boss parts span 3 height tiers) while keeping the floor grid legible.

---

## 4.3 Rendering configuration

### 4.3.1 Renderer

- **Forward+** renderer (Godot 4.x default for desktop).
- Output resolution target: 3840×2160 (4K) at 60 fps (quality mode) or 120 fps (performance mode).
- Upscaling: FSR 2.x (Godot's built-in `Scaling3DMode.Fsr2`) at scale 0.67 for 4K/60 on mid-tier GPUs, 0.50 for 4K/120; native resolution on high-end GPUs. TAA disabled when FSR 2 is on (FSR 2 does its own temporal accumulation).
- MSAA off in 3D (temporal upscaler handles AA); 2D HUD is rendered at native output resolution (HUD never upscaled).

### 4.3.2 WorldEnvironment preset `env_resonance_combat.tres`

| Setting | Value | Why |
|---|---|---|
| Background | Custom color #0B0E14 (deep blue-black) | Makes emissive materials pop |
| Ambient light | Color #3A4660, energy 0.35, source: Color | Cheap, stable ambient |
| Tonemap | Filmic, exposure 1.0, white 6.0 (use AgX instead if the project's Godot 4.x version provides it) | Handles bright emissive bursts without clipping |
| SSAO | On, radius 1.2, intensity 1.6 (off on Low preset) | Grounds units on the arena |
| SSIL | Off | Cost vs benefit at isometric distance is poor |
| SDFGI | Off | Arenas are small and static; use LightmapGI |
| Global illumination | LightmapGI baked per arena, texel density 8 per unit | Static lighting at near-zero runtime cost |
| SSR | On (High and Ultra only), max steps 32, fade in 0.15 | Polished brass and ice reflections |
| Glow | On, intensity 0.8, strength 1.0, bloom 0.05, HDR threshold 1.2, levels 2–5 | Emissive circuits, bursts |
| Fog | Depth fog, density 0.008, color #1A2233; volumetric fog off (Ultra: on, density 0.01) | Depth separation of boss tiers |
| Adjustments | Saturation 1.05; Tactical Pause lowers to 0.35 via tween | Pause readability |

### 4.3.3 Quality presets

| Preset | Target | FSR 2 scale | SSAO | SSR | Shadows (directional) | Volumetric fog | Particle budget |
|---|---|---|---|---|---|---|---|
| Low | 1080p / 60 | 1.0 (native 1080p) | Off | Off | 2,048, 2 splits | Off | 15,000 |
| Medium | 1440p / 60 | 0.77 | On (half) | Off | 4,096, 2 splits | Off | 30,000 |
| High | 4K / 60 | 0.67 | On | On | 4,096, 3 splits | Off | 45,000 |
| Ultra | 4K / 60 | 1.0 (native) | On | On | 8,192, 4 splits | On | 60,000 |
| Performance | 4K / 120 | 0.50 | On (half) | Off | 4,096, 2 splits | Off | 30,000 |

---

## 4.4 PBR material library

All materials are `StandardMaterial3D` or `ShaderMaterial` derived from four master shaders in `res://render/shaders/`. Values are the defaults; per-asset instances vary albedo tint only.

| Material | Master | Albedo | Metallic | Roughness | Special | Used for |
|---|---|---|---|---|---|---|
| Matte carbon | `m_carbon.gdshader` | #1C1E22 with a woven-fiber detail normal (tiling 6×) | 0.0 | 0.82 | Anisotropy 0.3 along weave direction | Veth-Kari chitin, armor underlayers, arena floors, UI frame 3D accents |
| Polished brass | `m_brass.gdshader` | #B5893A | 1.0 | 0.22 | Edge-wear mask (curvature map) raises roughness to 0.45 on edges | Kith-Lir instruments, buckles, ceremonial plates, boss joints |
| Emissive circuits | `m_circuit.gdshader` | Base porcelain #EDE8DF | 0.0 | 0.3, clearcoat 1.0 (roughness 0.1) | Emission mask with UV scroll; emission energy 2.0 idle, 6.0 chanting; color per element (4.4.1) | Aethel-Born traces, Ash-Dravan veins (tinted orange), boss Cores |
| Crystalline ice | `m_crystal.gdshader` | #BFE6FF, alpha 0.85 | 0.0 | 0.05 | Refraction (screen texture, strength 0.04), fresnel rim 3.0, internal facet normal map; Low preset: cubemap reflection only, opaque | Kith-Lir lenses, Induration VFX, Ice boss armor, Umbral Zero crystals |

### 4.4.1 Element color key (used by emission, particles, HUD glyphs)

| Element / property | Color | Hex |
|---|---|---|
| Blunt | Warm grey | #B8A99A |
| Piercing | Steel white | #E6EDF2 |
| Slashing | Crimson | #E0414F |
| Fire | Orange | #FF7A1F |
| Ice | Pale cyan | #8FE3FF |
| Wind | Mint | #7CF2B0 |
| Earth | Ochre | #C69A3A |
| Lightning | Violet-white | #C9A8FF |
| Water | Deep blue | #2F7BFF |
| Light | Gold-white | #FFF2B3 |
| Darkness | Indigo | #5A3FA8 |
| Umbral true damage | Magenta-black | #B0307A |
| True damage | Pure white with black outline | #FFFFFF |

Colorblind modes (Deuteranopia, Protanopia, Tritanopia) replace this table with pre-validated palettes and add a unique glyph shape per property so color is never the only signal.

---

## 4.5 Combat feedback: shakes, hit-stop, particles

### 4.5.1 Chain micro-shakes

Shakes are applied to a `ShakeController` node offsetting the camera (never rotating it). Noise: `FastNoiseLite` simplex, directional component along the strike vector projected onto the screen plane.

| Trigger | Amplitude (world units) | Duration | Frequency | Directional weight | Hit-stop | Extra |
|---|---|---|---|---|---|---|
| Level 1 property applied | 0.02 | 90 ms | 40 Hz | 0.6 | none | Property glyph pops on target |
| Critical hit | 0.035 | 110 ms | 38 Hz | 0.8 | 30 ms at time scale 0.1 | Number scales 1.4× |
| Level 2 Resonance Detonation | 0.06 | 180 ms | 30 Hz | 0.8 | 45 ms at time scale 0.1 | Radial distortion ring (screen shader), 0.3 s |
| Magic Burst | 0.05 | 150 ms | 32 Hz | 0.9 (caster → target) | 30 ms | Element flash on target |
| Level 3 Apex Detonation | 0.14 | 320 ms | 24 Hz | 0.7 | 60 ms at time scale 0.05 | Chromatic aberration pulse 0.25 s, glow intensity ×1.8 for 0.4 s, 0.5 s audio duck |
| Interrupt landed | 0.03 | 100 ms | 45 Hz | 1.0 | 20 ms | Interrupt node shatters on timeline |
| Boss Heavy strike on tank | 0.08 | 220 ms | 26 Hz | 0.9 | 40 ms | |

- Shakes add, capped at 0.18 amplitude total.
- Accessibility slider "Screen shake" 0–100% scales amplitude; "Hit-stop" toggle removes time-scale dips (sim is unaffected in all cases; hit-stop only pauses presentation).
- Hit-stop is presentation-only: `SimBridge` holds draining the next event batch for the hit-stop duration; the sim tick is not altered.

### 4.5.2 Directional burst particles

| Effect | Node | Direction | Cone | Count (High preset) | Lifetime | Material |
|---|---|---|---|---|---|---|
| L1 property spark | `GPUParticles3D` | Strike vector reflected off target surface | 35° | 120 | 0.35 s | Additive, element color |
| L2 detonation | `GPUParticles3D` ×2 (core burst + debris) | Caster → target vector, continuing through target | 25° | 1,200 + 300 debris | 0.6 s | Element sprite sheet (8×8 flipbook) + mesh debris for Blunt/Earth |
| Magic Burst | `GPUParticles3D` | Caster → target | 18° | 800 | 0.5 s | Element; Ice uses crystalline shard meshes |
| L3 Apex | `GPUParticles3D` ×3 + one-shot mesh shockwave | Radial + directional component | 360° radial + 20° directional spear | 4,000 total | 0.9 s | Solar Apex: gold-white; Umbral Zero: indigo + ice shards; Magma Core: orange + ember; Tempest Crown: violet + mint ribbons |
| Global Magic Burst (raid) | Screen-space overlay + `GPUParticles3D` | From screen edge of the originating squad's side | — | 600 | 1.0 s | Rift-white |

- Direction is computed from the `CombatEvent`'s actor and target world positions; bursts from casters standing behind the tank visibly spear past the tank toward the boss.
- All particle systems are pooled (`VfxPool` Autoload), 6 instances per effect type; `Emitting = true` with `OneShot`, `Restart()` on reuse.
- Particle budget per preset in 4.3.3; `VfxPool` drops the lowest-priority requests (L1 sparks first) when the live count would exceed budget.

### 4.5.3 Damage numbers

- Pooled `Label3D` (billboard, fixed-size, no depth test) with outline 8 px.
- Color by damage type (element key in 4.4.1); crits get a 1.4× scale punch; detonation numbers are stacked under the closing hit number with a chain-level badge ("L2 INDURATION +193").
- Maximum 40 concurrent numbers; overflow merges numbers on the same target into a sum.

---

## 4.6 HUD layout (reference resolution 3840 × 2160)

The HUD is authored at 3840×2160 with `Stretch Mode = canvas_items`, `Aspect = expand`, and a user UI scale option (80–140%).

| Region | Anchor | Size (px at 4K) | Widget |
|---|---|---|---|
| Top band | Top, full width minus 160 px margins | 3,520 × 220 | Timeline Bar (4.7) |
| Top-right | Top-right under timeline | 900 × 700 | Boss Sub-Target Anatomy (4.8) |
| Right | Right, below anatomy | 900 × 520 | Resonance Oscilloscope (4.9) |
| Bottom-left | Bottom-left | 1,500 × 460 | Party panel: 4–6 hero cards (HP, MP, Heat or Aquifer charges, current loadout icon, statuses) |
| Bottom-center | Bottom-center | 1,300 × 320 | Command wheel / ability bar for the selected hero (manual control) |
| Bottom-right | Bottom-right | 900 × 620 | Combat Math Console (4.10) |
| Top-left under timeline | Top-left | 520 × 140 | Aether Density Meter (dungeon only; 05 §5.2) |
| Center | Full screen overlay | — | Tactical Pause overlay and Gambit Deck editor |

---

## 4.7 Timeline Bar (10,000-AP)

### 4.7.1 Structure

- A horizontal track representing AP 0 (left) to 10,000 (right), plus a **debt zone** extending 400 px left of 0 for −4,000 to 0 AP.
- **Live portraits:** every unit (heroes and every boss part that acts) has a 120 px circular portrait chip positioned at its current AP. Positions interpolate every frame from sim state (presentation smoothing only; exact values in the tooltip).
- Portrait ring color: blue (hero), red (boss part); ring thickness 6 px; the unit currently acting gets a pulsing white ring.
- **Ready lane:** units at AP ≥ 10,000 stack in a vertical lane at the right edge, ordered by the tie-break order from 02 §2.2.3, so the player sees exactly who acts next.

### 4.7.2 Cast targets and interrupt nodes

- When a unit starts a chant, its portrait moves to a **cast lane** above the track, and a **diamond interrupt node** appears on the track at the position corresponding to "ticks until resolution," converted to the track scale (the track's upper edge is labeled in ticks-until-resolution during chants: 0 to 2,000 ticks mapped across the bar).
- A tether line connects the caster portrait to its **cast target** (a small portrait of the target, or a party icon for AoE).
- Interrupt node color by risk: green (Interrupt% on a boss-average hit < 20%), amber (20–50%), red (> 50%). The risk is computed by the sim's `ChantSystem.PreviewInterruptChance` against the boss's next scheduled attack.
- Boss chants show their interrupt node with the boss's Concentration value; hovering shows `Interrupt% needed damage = (Concentration × 0.01) / 2.5 × MaxHP_part`.

### 4.7.3 Previews

- Hovering an ability in manual mode shows a **ghost portrait** where the unit will land after paying that ability's recovery cost, plus any refund (Cadence Surge preview dashed at +1,200).
- Delays on enemies (Vector Stun, Pounce) show a ghost of the boss's new position.
- Resonance windows: a thin colored band along the bottom of the track shows each sub-target's chain window remaining time, scaled at 4,000 ticks = full width.

---

## 4.8 Boss Sub-Target Anatomy panel

Each boss has up to three sub-targets: **Core**, **Shield**, **Weapon Arm**. The panel shows a stylized silhouette (the boss's `AnatomyTexture`) with three clickable hotspots, plus one row per part.

| Row element | Detail |
|---|---|
| Part name + state | "CORE", "SHIELD — DISABLED 2,140", "WEAPON ARM — CASTING" |
| HP bar | Segmented every 10%; damage taken in the last 1,000 ticks shown as a trailing white segment |
| Chain glyph | Current chain state (empty, L1 property icon, L2 resonance icon) with a circular window timer |
| Burst glyph | If a burst window is open: element icons plus remaining ticks |
| Per-part threat bars | The 4 highest heroes on that part's table: stacked bar with VE (bright, animated shimmer) and CE (solid) segments, hero portrait at left, numeric VE / CE on hover |
| Target indicator | A red crosshair on the hero currently selected by argmax for that part |
| Threat warnings | Amber if a non-tank holds argmax on any part; red if a non-tank holds it and that part's next action is within 1,000 ticks |

The Shield row shows its absorb pool ("Absorbs 30% of damage to Core while active"; raid-specific numbers in 06). Clicking a hotspot sets the manual target and filters the Oscilloscope and Math Console to that part.

---

## 4.9 Resonance Oscilloscope

A dedicated instrument panel that visualizes chain state as waveforms.

- **Channels:** one channel per boss sub-target (up to 3 rows).
- **Waveform:** each applied property draws a sine segment in the property's color. Frequency encodes property (Blunt lowest, Light highest). Amplitude decays linearly over the 4,000-tick window.
- **Interference:** when a valid L2 happens, the two waveforms visibly combine into a composite wave with a bright peak marker labeled with the resonance ("INDURATION"). L3 shows a saturated square-wave spike.
- **Burst envelope:** an overlaid translucent envelope shows the 1,500-tick burst window, extending in real time when a Kith-Lir extends it (+600 / +900).
- **Route hints:** to the right of each channel, the panel lists valid next properties from the current state (for example "Induration → Darkness = UMBRAL ZERO") and which party members have an ability with that property ready within the remaining window (computed from AP and CT_eff).
- **Time base:** horizontal axis spans the last 2,000 and next 4,000 ticks (past on the left, future on the right, "now" line at one third).

Implementation: a `Control` with a `ShaderMaterial` (`canvas_item` shader) that receives up to 16 waveform segments per channel as a uniform array (`vec4 segments[48]`: start tick, frequency, amplitude, color index). No per-frame mesh generation; the shader evaluates waveforms per pixel. Cost measured below 0.15 ms GPU at 4K.

---

## 4.10 Combat Math Console

A streaming log that prints every sim calculation in human-readable form.

### 4.10.1 Line format

```
[t=1170] Zeph ▸ Blizzard II ▸ Core | BURST(Ice, Induration) | INT 666 × 3.00 = 1,998 × (1 − MDR 0.333) = 1,332 × CRIT 1.70 × (1 + 0.50 + 0.20) = 3,849 | MP +117 (Condenser 65%) | Window +600 → 2,912
```

### 4.10.2 Verbosity levels

| Level | Shows |
|---|---|
| 1 Summary | Damage, heals, detonations, KOs |
| 2 Tactical | Level 1 plus hit/miss, crits, chain transitions, interrupts, enmity target changes |
| 3 Full math | Every pipeline step (as in 4.10.1), enmity deltas, VE decay steps, CE shed calculations, AP changes, gear swaps |
| 4 Debug | Level 3 plus RNG stream ids and raw rolls (used by QA and arena replay auditing) |

### 4.10.3 Features

- Filters: by actor, by target part, by event type (Damage / Enmity / Resonance / Timeline / Gear).
- Clicking a line pauses and scrubs the replay to that tick (replays only).
- Export: copy visible lines or save the full log as `.txt` / `.jsonl` (the `.jsonl` is the same event schema the arena validator uses).
- Capacity: last 5,000 lines in memory; virtualized rendering of only visible lines.

---

## 4.11 Controls

### 4.11.1 Tactical Pause

- **Space** (keyboard) or **Command (⌘)** on macOS keyboards, and **Start/Options** on gamepads, toggles Tactical Pause.
- Paused state: the sim loop stops between ticks (never mid-tick). Saturation drops to 0.35, the HUD gains full interactivity (inspect any unit, loadout view read-only, Gambit Deck editing allowed, manual action queueing).
- Auto-pause options (each a toggle): on boss chant start; on any hero ready (full manual turn-based mode); on L2/L3 detonation; on hero below 25% HP; on non-tank gaining argmax.
- Ghost Protocol arena runs never pause (they are simulated; see 06). Raids use a pause budget (06 §6.5.4).

### 4.11.2 Default bindings

| Action | Keyboard / mouse | Gamepad |
|---|---|---|
| Tactical Pause | Space / ⌘ Command | Start / Options |
| Select hero 1–6 | 1–6 | D-pad cycles |
| Select target part | Click hotspot / Tab cycles | RB / LB |
| Open ability bar | Right-click hero | A / Cross |
| Queue manual action | Click ability | A / Cross |
| Open Gambit Deck | G | Back / Share |
| Toggle Math Console verbosity | ` (backtick) | Right-stick click |
| Sim speed 1× / 2× / 4× | F1 / F2 / F3 | Y / Triangle cycles |
| Rotate camera | Q / E | Left / right triggers |
| Pan camera | WASD / middle-drag | Left stick |
| Zoom | Mouse wheel | Right stick vertical |

All bindings are remappable through Godot's `InputMap`, persisted in `user://input.cfg`.

---

## 4.12 Gambit Deck: visual logic engine

Gambits are per-hero ordered rule lists that choose that hero's action whenever the hero becomes ready and no manual action is queued. They are also the only control available to Ghost Protocol arena squads, so the language must be deterministic, finite and validatable.

### 4.12.1 Grammar (EBNF)

```
Deck          = Gambit , { Gambit } ;                         (* max slots: see 4.12.5 *)
Gambit        = [ Label ":" ] , "IF" , ConditionList , "->" , Action , [ "ON" , TargetSpec ] , [ Flags ] ;
ConditionList = Condition , { "AND" , Condition } ;            (* 1 to 3 conditions *)
Condition     = [ "NOT" ] , Subject , "[" , Predicate , "]" ;
Subject       = "Self" | "Target" | "Boss" , [ "." , Part ] | "Ally" , [ "(" , Selector , ")" ] | "Party" | "Field" ;
Part          = "Core" | "Shield" | "WeaponArm" ;
Selector      = "Lowest HP%" | "Lowest MP%" | "Tank" | "Casting" | "Targeted" | HeroName ;
Predicate     = Name , [ ":" , Argument ] , [ Comparator , Number ] ;
Comparator    = "<" | "<=" | ">" | ">=" | "==" ;
Action        = Verb , "[" , AbilityName | Keyword , "]" ;
Verb          = "CAST" | "USE" | "DEFER" | "WAIT" | "RETARGET" ;
TargetSpec    = "Target" | "Self" | Part | "Ally" , "(" , Selector , ")" ;
Flags         = "{" , Flag , { "," , Flag } , "}" ;
Flag          = "SpendHeat" | "NoOverwrite" | "BurstOnly" ;
Label         = Identifier ;
```

`CAST` and `USE` are synonyms for the sim (CAST reads naturally for spells, USE for weapon skills and taunts); the editor picks the verb automatically from the ability type.

The two user-supplied examples parse as:

```
IF Target [Resonance: Ice] -> CAST [Blizzard II]
IF Boss [Casting] -> USE [Shield Bash]
```

(Shield Bash is the universal interrupt granted by the **Bulwark Bash** Schematic Chip, 05 §5.6.4, usable by any hero with that chip socketed.)

### 4.12.2 Condition catalog

| # | Subject | Predicate | Argument / comparator | True when |
|---|---|---|---|---|
| 1 | Target / Boss.Part | `Resonance` | `: <Element>` | An open burst window on that part includes the element |
| 2 | Target / Boss.Part | `Window` | `: <Property or Resonance>` | Chain state equals that L1 property or L2 resonance |
| 3 | Target / Boss.Part | `WindowLeft` | `< N` / `> N` ticks | Remaining chain window ticks compare to N |
| 4 | Target / Boss.Part | `BurstLeft` | `< N` / `> N` ticks | Remaining burst window ticks compare to N |
| 5 | Target / Boss.Part | `Casting` | optional `: <AbilityName>` | Part is chanting (optionally that ability) |
| 6 | Target / Boss.Part | `CastResolvesIn` | `< N` ticks | Part's chant resolves within N ticks |
| 7 | Target / Boss.Part | `HP%` | comparator N | Part HP% compares to N |
| 8 | Target / Boss.Part | `AP` | comparator N | Part AP compares to N (enables Pounce on the Upbeat timing) |
| 9 | Target / Boss.Part | `HasBuff` | `: <Name>` or `Count >= N` | Beneficial effect present / count |
| 10 | Target / Boss.Part | `HasDebuff` | `: <Name>` | Debuff present (use NOT for "missing") |
| 11 | Target / Boss.Part | `Disabled` | none | Sub-target disabled (Shield by Aetheric Seizure) |
| 12 | Target / Boss.Part | `TargetingMe` | none | argmax on that part's table is this hero |
| 13 | Target / Boss.Part | `MyEnmityRank` | comparator N | This hero's rank on that part's table |
| 14 | Target / Boss.Part | `TankMargin` | `< N` | (Tank VE+CE) − (highest non-tank VE+CE) is below N |
| 15 | Self | `HP%` | comparator N | |
| 16 | Self | `MP%` | comparator N | |
| 17 | Self | `MP` | `>= Cost: <AbilityName>` | Enough MP for that ability |
| 18 | Self | `Heat` | comparator N | Ash-Dravan only |
| 19 | Self | `Primed` | none | Heat = 100 |
| 20 | Self | `Aquifer` | comparator N | Tollen only |
| 21 | Self | `HasBuff` | `: <Name>` | |
| 22 | Self | `Deferred` | comparator N ticks | How long this hero has been deferring |
| 23 | Ally(Selector) | `HP%` | comparator N | Selected ally's HP% |
| 24 | Ally(Selector) | `MP%` | comparator N | |
| 25 | Ally(Selector) | `Casting` | optional `: <AbilityName>` | |
| 26 | Ally(Selector) | `ReadyIn` | `< N` ticks | Ticks until that ally reaches 10,000 AP |
| 27 | Ally(Selector) | `KO` | none | |
| 28 | Ally(Selector) | `HasDebuff` | `: <Name>` | |
| 29 | Party | `CountBelowHP%` | `: N >= M` | At least M allies below N% HP |
| 30 | Party | `ChainParticipants` | comparator N | Distinct contributors in the current chain on the current target |
| 31 | Field | `AetherDensity` | comparator N | Dungeon Aether Density Meter value |
| 32 | Field | `Tick` | comparator N | Fight time in ticks |
| 33 | Field | `GlobalBurst` | optional `: <Element>` | Raid global burst window active |
| 34 | Field | `Always` | none | Always true (use for the last slot) |

### 4.12.3 Action catalog

| Verb | Syntax | Effect |
|---|---|---|
| CAST / USE | `CAST [AbilityName]` | Executes that ability on the TargetSpec (default: current target part; heals default to Ally(Lowest HP%)) |
| USE [Attack] | `USE [Attack]` | Universal basic attack every hero has: Physical, no chain property, Standard recovery, 1.00 × ATK, CE 100 |
| DEFER | `DEFER [N]` | Do not act; re-evaluate the deck every tick for up to N ticks (N ≤ 1,500). AP keeps rising (overshoot capped at 12,000 while deferring). When N expires, the deck is evaluated skipping DEFER slots. |
| WAIT | `WAIT [Guard]` | Spend a Stance recovery (4,000 AP) doing nothing but gaining −20% damage taken until next action (counts as mitigation for CE shed) |
| RETARGET | `RETARGET [Part]` | Changes the hero's current target part without spending AP, then continues evaluating from the next slot (max once per evaluation) |

Flags:

- `SpendHeat`: Brannoch Ash-Well spends 20 Heat on this action (Tithe of Coals).
- `NoOverwrite`: the action is skipped if its chain property would restart (not advance) an open L2 window on the target. Protects L3 setups from careless openers.
- `BurstOnly`: the action is skipped unless its resolution tick (now + CT_eff) falls inside a matching open burst window. This is the automated version of the chant-timing play from the 02 §2.15 log.

### 4.12.4 Evaluation order (deterministic)

When a hero becomes ready (02 §2.2.3 step 4) and has no manual action queued:

1. Evaluate slots from top (slot 1) to bottom.
2. For each slot, evaluate conditions left to right with short-circuit AND; `NOT` inverts one condition.
3. If all conditions are true, check executability: enough MP, not Silenced (for chants), ability not locked by a status, target valid (alive, part not destroyed), and flags satisfied (`NoOverwrite`, `BurstOnly`).
4. The first slot passing steps 2–3 fires. Evaluation stops.
5. If no slot fires, the hero uses `USE [Attack]` on its current target.
6. `RETARGET` changes target and continues evaluation at the next slot (not from the top), at most once per evaluation.
7. All state reads come from the sim state as of that point in the tick (after earlier units in the same tick have resolved).
8. Gambit evaluation consumes no ticks and no RNG.

### 4.12.5 Slot limits and logic budget

| Limit | Value |
|---|---|
| Starting slots per hero | 6 |
| Slot unlocks | +1 slot at hero Rank 10, 20, 30 and 40 (max 10) |
| Conditions per slot | 1–3 |
| Logic budget per hero deck | 20 conditions total |
| DEFER slots per deck | max 2 |
| Arena uploads | Same limits; validated server-side (06 §6.3) |

### 4.12.6 Example decks

Zeph Tri-Lumen (Spike DPS):

```
1  IF Target [Resonance: Ice] AND Self [MP >= Cost: Blizzard II] -> CAST [Blizzard II]
2  IF Target [Resonance: Fire] AND Target [BurstLeft > 1000] -> CAST [Pyre Lattice]
3  IF Target [Resonance: Lightning] -> CAST [Hex Lance]
4  IF Target [Window: Piercing] AND Ally(Mirrim Ash-Pounce) [ReadyIn < 300] -> CAST [Blizzard II] {BurstOnly}
5  IF Target [Window: Fragmentation] -> CAST [Hex Lance]
6  IF Field [Always] -> DEFER [600]
```

Korrith Vael-Dun (Anchor Tank):

```
1  IF Boss [TankMargin < 1500] -> USE [Lattice Provoke]
2  IF Boss [CastResolvesIn < 800] AND NOT Self [HasBuff: Keratin Bastion] -> USE [Keratin Bastion]
3  IF Boss.WeaponArm [MyEnmityRank > 1] -> USE [Lattice Provoke] ON WeaponArm
4  IF Target [Window: Induration] -> USE [Attack] {NoOverwrite}
5  IF Field [Always] -> USE [Seismic Maul]
```

### 4.12.7 Visual editor

- Built as a vertical list layout rather than a `GraphEdit` node graph (gambits are ordered lists, not graphs): each slot is a horizontal "card strip" (`HBoxContainer`) of draggable condition chips, an arrow, an action chip and a target chip.
- Chips are color-coded: conditions blue, actions orange, flags grey; invalid chips glow red with a tooltip (for example "Blizzard II requires 180 MP; this hero's max MP is 540").
- A **dry-run** button evaluates the deck against the current paused state and highlights which slot would fire.
- Decks serialize to a compact text form (the grammar above) for sharing and arena upload.

---

## 4.13 Accessibility

| Feature | Detail |
|---|---|
| Colorblind palettes | 3 modes plus unique glyph per property |
| Text scale | 80–140% HUD scale; Math Console font size independent |
| Screen shake / hit-stop | Slider 0–100% / toggle |
| Full turn-based mode | Auto-pause on every hero ready |
| Screen reader hooks | Math Console Level 1 lines exposed via `DisplayServer.TtsSpeak` (opt-in) |
| Photosensitivity | L3 flash and chromatic aberration can be disabled; maximum luminance change clamped to 0.3 per 100 ms when enabled |
| Input | Full remapping, hold-to-toggle alternatives, single-stick mode |

---

## 4.14 Godot 4.x C# implementation notes

### 4.14.1 Battle scene tree

```
BattleScene (Node3D)                          BattleScene.cs
├─ WorldEnvironment                           env_resonance_combat.tres
├─ KeyLight (DirectionalLight3D)
├─ CameraRig (Node3D)                         CameraRig.cs (pan, rotate, zoom)
│  └─ ShakeController (Node3D)                ShakeController.cs
│     └─ Camera3D (orthographic)
├─ Arena (Node3D)                             arena_*.tscn (instanced)
│  ├─ LightmapGI
│  ├─ StaticGeometry (MeshInstance3D ×N, visibility ranges for LOD)
│  └─ ReflectionProbe
├─ Units (Node3D)
│  ├─ HeroActor ×(4–6)                        hero_actor.tscn / HeroActor.cs
│  │  ├─ Model (MeshInstance3D, skinned) + Skeleton3D
│  │  ├─ AnimationTree (state machine: Idle, Ready, Chant, Cast, WS, Hit, KO)
│  │  ├─ VfxSockets (Marker3D: Hand_R, Hand_L, Chest, Ground)
│  │  └─ SelectionArea (Area3D, CollisionShape3D)
│  └─ BossActor                               boss_actor.tscn / BossActor.cs
│     ├─ Model + AnimationTree
│     └─ PartSockets: Core, Shield, WeaponArm (Marker3D + Area3D each)
├─ VfxRoot (Node3D)                           particles pulled from VfxPool autoload
├─ DamageNumberRoot (Node3D)                  Label3D pool
└─ BattlePresenter (Node)                     BattlePresenter.cs: subscribes to SimBridge, maps CombatEvents to animation/VFX
```

### 4.14.2 HUD scene tree

```
Hud (CanvasLayer, layer = 10)                 Hud.cs
└─ Root (Control, full rect, Theme = res://ui/theme/resonance_theme.tres)
   ├─ TimelineBar (Control)                   TimelineBar.cs (custom _Draw for track and ticks)
   │  ├─ PortraitLayer (Control)              PortraitChip.tscn pooled ×12
   │  ├─ CastLane (Control)                   CastNode.tscn pooled ×8
   │  └─ ReadyLane (VBoxContainer)
   ├─ BossAnatomy (PanelContainer)            BossAnatomyPanel.cs
   │  └─ VBoxContainer
   │     ├─ Silhouette (TextureRect + hotspots as TextureButton ×3)
   │     └─ PartRow.tscn ×3                   PartRow.cs (ProgressBar, ThreatBar ×4, ChainGlyph, BurstGlyph)
   ├─ Oscilloscope (Control + ShaderMaterial) Oscilloscope.cs, oscilloscope.gdshader
   ├─ PartyPanel (HBoxContainer)              HeroCard.tscn ×6
   ├─ AbilityBar (HBoxContainer)              AbilityButton.tscn ×8
   ├─ MathConsole (PanelContainer)            MathConsole.cs (virtualized line renderer)
   ├─ AetherDensityMeter (Control)            AetherDensityMeter.cs
   ├─ TacticalPauseOverlay (ColorRect)        pause_desaturate.gdshader (fallback if WorldEnvironment tween is off)
   └─ GambitDeckEditor (PanelContainer, hidden) GambitDeckEditor.cs
      └─ ScrollContainer → VBoxContainer → GambitSlotRow.tscn ×10
```

### 4.14.3 Presentation rules for performance

- **No per-event node creation.** All portraits, cast nodes, damage numbers, particles and console lines are pooled.
- **Batch drain:** HUD widgets receive `ReadOnlySpan<CombatEvent>` once per frame from `SimBridge.EventsDrained` (a C# event, not a Godot signal, to avoid Variant boxing of struct arrays).
- **Dirty flags:** widgets redraw (`QueueRedraw()`) only when their data changed this frame.
- **Math Console virtualization:** a custom `Control` draws visible lines with `DrawString` from a cached `Font`; strings are formatted lazily when a line first becomes visible and cached. `RichTextLabel` is avoided for the stream (its reflow cost grows with content).
- **Threat bars** use `TextureProgressBar` with nine-patch textures, updated at most 10 times per second (visual smoothing hides the throttle).
- **Theme:** a single `Theme` resource with `StyleBoxFlat` (no per-control overrides) to keep canvas batching effective.
- **Shaders:** oscilloscope and pause shaders are `canvas_item` shaders; 3D master shaders avoid `discard` except on crystal alpha-scissor LODs.
- **LOD:** hero meshes have 3 LODs (Godot automatic mesh LOD enabled, `lod_bias` 1.0); boss meshes 4 LODs; visibility ranges for arena props.

### 4.14.4 Frame budget

| Budget line | 4K / 60 (16.67 ms) | 4K / 120 (8.33 ms) |
|---|---|---|
| Sim advance (`Resonance.Sim`, event-driven) | ≤ 0.30 ms | ≤ 0.30 ms |
| Event drain + presenter mapping | ≤ 0.40 ms | ≤ 0.30 ms |
| HUD script (C#) | ≤ 0.80 ms | ≤ 0.50 ms |
| Godot main-thread scene processing | ≤ 2.00 ms | ≤ 1.20 ms |
| Render thread / CPU submission | ≤ 3.00 ms | ≤ 2.00 ms |
| GPU: 3D scene | ≤ 9.50 ms | ≤ 5.00 ms |
| GPU: particles + post (glow, SSAO, FSR 2) | ≤ 3.50 ms | ≤ 2.00 ms |
| GPU: 2D HUD | ≤ 0.60 ms | ≤ 0.40 ms |
| Headroom | ~0.5 ms GPU | ~0.5 ms GPU |

| Resource budget | Value |
|---|---|
| Draw calls (3D, High) | ≤ 2,500 |
| Visible triangles | ≤ 3,000,000 |
| Hero mesh LOD0 | 60,000 triangles |
| Boss mesh LOD0 | 250,000 triangles |
| Texture memory (VRAM) | ≤ 3.5 GB at Ultra, ≤ 2.0 GB at High |
| Live particles | Per preset table 4.3.3 |
| Managed allocations per frame (steady state) | 0 bytes in sim and presenter; ≤ 2 KB in HUD |
| GC | .NET Server GC off, concurrent workstation GC on; no Gen2 collections during combat (verified with dotnet-counters in soak tests) |

Reference hardware for High / 4K / 60: RTX 3070 or RX 6700 XT, Ryzen 5 5600 / Core i5-12400, 16 GB RAM. Reference for Performance / 4K / 120: RTX 4070 or RX 7800 XT.
