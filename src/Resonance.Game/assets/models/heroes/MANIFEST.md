# Project Resonance VS4 3D proof: MANIFEST

Generated 2026-10-03. All files are in this folder.

## Axes, scale, loops

- Units: meters (1 glTF unit = 1 m). Up: +Y (glTF). Forward: model front faces glTF +Z (Blender -Y on export).
- Godot: Godot 4 imports glTF unchanged: model front faces +Z, i.e. Godot MODEL_FRONT (Vector3.MODEL_FRONT = +Z). A Node3D looking along its -Z (look_at) will show its back; rotate 180 deg about Y or use look_at(target, Vector3.UP, true) (use_model_front) to face a target.
- Frame rate 30 fps. glTF has no loop flag. The provided *.glb.import files set settings/loop_mode=1 (linear loop) for the looping clips; verified in Godot 4.7.2 headless import. Otherwise set loop mode in the Advanced Import dialog or in code.

## korrith_vael_dun.glb: Korrith Vael-Dun

- Size: 15.3 MB. Triangles total: **25,752**
- Triangles by mesh: Korrith_BastionFX 396, Korrith_Body 22,000, Korrith_DorsalRidge 1,432, Korrith_Maul 1,104, Korrith_Shield 820
- Height: 2.12 m. Race: Veth-Kari (chitin-fused subterranean vanguard; NOT Kith-Lir). Archetype: Anchor Tank.
- Textures (embedded PNG): korrith_body 2048px; korrith_shield 1024px; korrith_maul 1024px; korrith_ridge 512px

### Prop and FX bones

- `LeftHand_Prop`: shield grip (shield mesh bound here)
- `RightHand_Prop`: maul grip
- `FX_Bastion`: Keratin Bastion shell pivot at shield center

### Clips

| Clip | Frames | Duration | Loop | Notes |
|---|---|---|---|---|
| `idle` | 0-72 | 2.4 s | yes | slow side-to-side weight shift + breathing (docs 1.3.2) |
| `ready` | 0-40 | 1.333 s | yes | combat guard: shield across torso, maul low, knees bent |
| `attack` | 0-40 | 1.333 s | no | Seismic Maul overhead slam: dip f5, wind-up f13, impact f18, settle f24, recover to ready f40 |
| `shield_bash` | 0-36 | 1.2 s | no | coil f9, lunge, impact f15, recoil f17, recover f36 (heavy shield strike, interrupts chants) |
| `keratin_bastion` | 0-48 | 1.6 s | no | rise f6, slam into braced crouch f12-14, shield planted forward; FX_Bastion hex shell scales 0->1.25->0.95->1.12->1.0 (emissive pulse); ends HELD in brace |
| `hit_react` | 0-20 | 0.667 s | no | recoil f3, recover f20 |
| `stunned` | 0-60 | 2.0 s | yes | dazed head roll + sway, shield lowered |
| `death` | 0-64 | 2.133 s | no | stagger f6, knees buckle f26, topple forward onto shield f46, bounce f50 |

### Bones (59)

`Root`, `Hips`, `Spine`, `Chest`, `UpperChest`, `Neck`, `Head`, `LeftEye`, `RightEye`, `Jaw`, `LeftShoulder`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand`, `LeftIndexProximal`, `LeftIndexIntermediate`, `LeftIndexDistal`, `LeftMiddleProximal`, `LeftMiddleIntermediate`, `LeftMiddleDistal`, `LeftRingProximal`, `LeftRingIntermediate`, `LeftRingDistal`, `LeftLittleProximal`, `LeftLittleIntermediate`, `LeftLittleDistal`, `LeftThumbMetacarpal`, `LeftThumbProximal`, `LeftThumbDistal`, `LeftHand_Prop`, `FX_Bastion`, `RightShoulder`, `RightUpperArm`, `RightLowerArm`, `RightHand`, `RightIndexProximal`, `RightIndexIntermediate`, `RightIndexDistal`, `RightMiddleProximal`, `RightMiddleIntermediate`, `RightMiddleDistal`, `RightRingProximal`, `RightRingIntermediate`, `RightRingDistal`, `RightLittleProximal`, `RightLittleIntermediate`, `RightLittleDistal`, `RightThumbMetacarpal`, `RightThumbProximal`, `RightThumbDistal`, `RightHand_Prop`, `LeftUpperLeg`, `LeftLowerLeg`, `LeftFoot`, `LeftToes`, `RightUpperLeg`, `RightLowerLeg`, `RightFoot`, `RightToes`

- Naming: Godot SkeletonProfileHumanoid names (all 56 profile bones present, verified in Godot 4.7.2) + extras
- Rest pose: A-pose (arms ~35 deg below horizontal). Use Godot retarget "Fix Silhouette" if retargeting T-pose clips.
- Retarget note: Extra prop/FX bones are not in the humanoid profile; when retargeting clips from other rigs, props follow the hands but FX_Bastion/prop-local tracks are not transferred.

### Validation

- Khronos glTF-Validator: 0 errors, 9 warnings (MESH_PRIMITIVE_GENERATED_TANGENT_SPACE, NODE_SKINNED_MESH_NON_ROOT; these are standard Blender-exporter warnings).
- Blender re-import: 8 actions, skinned meshes: Korrith_BastionFX, Korrith_Body, Korrith_DorsalRidge, Korrith_Maul, Korrith_Shield. All clips move the skinned vertices (checked mid-clip against frame 0).
- Godot 4.7.2 headless import: scene, Skeleton3D, AnimationPlayer and all clips load; loop modes come from the .import file.

## carapace_engine_mk2.glb: Carapace Engine Mk. II

- Size: 13.6 MB. Triangles total: **35,343**
- Triangles by mesh: Carapace_Core 3,040, Carapace_CoreGlow 668, Carapace_Hull 22,613, Carapace_Shield 3,474, Carapace_WeaponArm 5,548
- Size: height 4.4 m, footprint ~6.6 m wide, ~7.6 m long including the lance.
- Textures (embedded PNG): carapace (hull+arm+shield shared atlas) 2048px; carapace_core 1024px; carapace_socket 512px; core_glow / vent_fire

### Destroyable part nodes

| Part | Mesh nodes (hide/destroy) | Bones |
|---|---|---|
| Core | Carapace_Core, Carapace_CoreGlow | Core, Core_Glow |
| WeaponArm | Carapace_WeaponArm | WeaponArm_Shoulder, WeaponArm, WeaponArm_Lance |
| Shield | Carapace_Shield | Shield_Shoulder, Shield |
| Hull | Carapace_Hull | Body, Leg_*_Upper/Lower/Foot x6 |

Boss faces glTF +Z. WeaponArm sits at glTF +X (the boss own left side; on the viewer right when the boss faces the camera, matching the concept), Shield at glTF -X. Korrith likewise has his left hand (shield) at +X.

### Clips

| Clip | Frames | Duration | Loop | Notes |
|---|---|---|---|---|
| `idle` | 0-60 | 2.0 s | yes | hull bob, arms sway, Core_Glow pulse |
| `attack` | 0-44 | 1.467 s | no | Piston Sweep: wind out f10, sweep f16-19, WeaponArm_Lance punches 1.1 m at f19, recover f44 |
| `charge` | 0-40 | 1.333 s | yes | Overpressure Lance chant (loop): braced low, Core rotates, Core_Glow swells 1.25+/-0.12, lance retracted & trembling |
| `stunned` | 0-60 | 2.0 s | yes | sagged 0.55 m, legs splayed, Core flicker |
| `part_destroyed_core` | 0-40 | 1.333 s | no | Core shakes, glow bursts 1.9 then scales to 0; hide Carapace_Core + Carapace_CoreGlow after |
| `part_destroyed_weapon_arm` | 0-40 | 1.333 s | no | arm jerks then droops limp; hide Carapace_WeaponArm after |
| `part_destroyed_shield` | 0-40 | 1.333 s | no | shield arm jerks, shield drops to ground; hide Carapace_Shield after |
| `death` | 0-60 | 2.0 s | no | legs buckle (IK-baked), hull drops 1.2 m, Core glow fades to 0 |

### Bones (27)

`Root`, `Body`, `Core`, `Core_Glow`, `WeaponArm_Shoulder`, `WeaponArm`, `WeaponArm_Lance`, `Shield_Shoulder`, `Shield`, `Leg_FL_Upper`, `Leg_FL_Lower`, `Leg_FL_Foot`, `Leg_FR_Upper`, `Leg_FR_Lower`, `Leg_FR_Foot`, `Leg_FC_Upper`, `Leg_FC_Lower`, `Leg_FC_Foot`, `Leg_BL_Upper`, `Leg_BL_Lower`, `Leg_BL_Foot`, `Leg_BR_Upper`, `Leg_BR_Lower`, `Leg_BR_Foot`, `Leg_BC_Upper`, `Leg_BC_Lower`, `Leg_BC_Foot`

- Legs: 6 legs (FL, FR, FC, BL, BR, BC) x Upper/Lower/Foot; leg motion solved with 2-bone IK during authoring and baked to FK keys

### Validation

- Khronos glTF-Validator: 0 errors, 10 warnings (MESH_PRIMITIVE_GENERATED_TANGENT_SPACE, NODE_SKINNED_MESH_NON_ROOT; these are standard Blender-exporter warnings).
- Blender re-import: 8 actions, skinned meshes: Carapace_Core, Carapace_CoreGlow, Carapace_Hull, Carapace_Shield, Carapace_WeaponArm. All clips move the skinned vertices (checked mid-clip against frame 0).
- Godot 4.7.2 headless import: scene, Skeleton3D, AnimationPlayer and all clips load; loop modes come from the .import file.

## Sources and licenses

- **Tencent Hunyuan3D-2 (hunyuan3d-dit-v2-0-turbo, fp16) image-to-3D shape generation, run locally on gregspc RTX 4070**. https://huggingface.co/tencent/Hunyuan3D-2 under the Tencent Hunyuan 3D 2.0 Community License Agreement (https://huggingface.co/tencent/Hunyuan3D-2/blob/main/LICENSE). Key terms:
  - Not licensed in the European Union, United Kingdom or South Korea; use/distribution of Outputs outside the Territory is unauthorized
  - Tencent claims no rights in Outputs (section 4.d); user is responsible for Outputs
  - Outputs must not be used to improve other AI models
  - If Licensee products had >1M MAU on the model release date a separate Tencent license is required
  - Acceptable Use Policy applies
- rembg isnet-general-use background removal: https://github.com/danielgatis/rembg (MIT (rembg); IS-Net weights Apache-2.0).
- The concept images in `concepts/` were provided by the coordinator.
- CC0 check: Checked Quaternius Universal Animation Library (CC0, UE-style rig: pelvis/spine_01..). Not used: it has no shield bash / defensive brace / maul clips and would need retargeting onto a non-standard bulky A-pose mesh; all clips are hand-keyed by script. No CC0 meshes used.
- Everything else (props, Core cage, ridge, FX, rigs, weights, clips, textures) was authored procedurally by script in Blender 4.2.23 LTS.

## Pipeline

- PC (gregspc, RTX 4070): rembg isnet -> Hunyuan3D-2 turbo shape (octree 448, 8 steps, seeds 1234/777; 1234 chosen) -> raw GLB (~0.8-1.1M tris, untextured)
- Box (Blender 4.2.23 LTS headless): normalize scale, remove floaters, decimate to budget, Smart-UV, Cycles CPU bake high->low: front-projected concept color/emissive + procedural gunmetal/hex backfill, tangent normal from high-poly + bump
- Procedural props (shield, maul, dorsal ridge, Bastion FX, boss Core cage) with bevels/insets/emissive trims, baked to their own atlases
- Script-built skeletons, nearest-bone weights, scripted keyframes (boss legs IK-baked), glTF export (KHR_materials_emissive_strength)
- Validation: Khronos gltf-validator 2.x (npm), Blender re-import, Godot 4.7.2 headless import

Scripts: `scripts/` in this folder (box-side Blender/Python + PC gen.py/setup.ps1/run_gen.ps1). Working copies: /workspace/vs4-work.

## Previews

- `korrith_vael_dun_turntable.png`
- `korrith_vael_dun_clips_contact.png`
- `carapace_engine_mk2_turntable.png`
- `carapace_engine_mk2_clips_contact.png`
## Quality assessment (honest)

This is a stylized proof, above a blockout but below production. It is not hand-sculpted AAA work.

What works:
- Silhouettes follow the concepts closely because the meshes are image-to-3D from them: Korrith's bulky trapezoid torso, honeycomb shoulder pads and forearm bucklers; the boss's six legs, hull, side shield and lance.
- From the front and 3/4 views (the game's 35 deg iso camera) the baked front-projected color and neon read well: gunmetal, cyan #00E5FF and magenta trims.
- The rigs import cleanly into Godot 4.7.2. Korrith has the full SkeletonProfileHumanoid set. The boss parts are separate mesh nodes with their own bones.
- The Keratin Bastion hex shell pulse and the boss part-destroyed beats read clearly in the previews.

Weak or below production quality:
- **Back and sides.** Projection only covers the front. The back, sides and the lance length use procedural gunmetal/hex fill, so they look plainer and smeared where projection grazes. Baked concept lighting is also in the albedo.
- **Mesh topology.** The meshes are decimated AI output (triangle soup, not quad retopo). Armor edges are soft and lumpy rather than crisp hard-surface, small details are mushy, and the normal maps carry some of the look. Expect artifacts in close-ups.
- **Face and hands.** The head is a helmet blob with no real face. Hands are generated fists/claws, and the finger bones are weighted approximately, so curls are crude.
- **Weights.** Automatic nearest-bone weights. Shoulder pads, hips and knees can pinch or stretch in extreme poses: the bastion crouch, death, and the attack wind-up. The boss hull and leg roots get similar distortion.
- **Animation.** Hand-keyed by script with basic anticipation and overlap: readable timing, but no polish pass, no foot locking on Korrith (feet can slide or float a little in bash, bastion and death), and the props are aimed by wrist rotation, so wrists sometimes twist unnaturally. The boss leg IK is baked but there is no gait. Death ground contact is approximate.
- **Props and Core.** Shield, maul, dorsal ridge and Core cage are procedural primitives (bevels, insets, emissive strips). They are clean but simpler than the concept.
- **Animated emissive.** No material animation. The pulse is done with bone scale (FX_Bastion, Core_Glow). Drive emission energy in-engine if wanted.
- **Seams.** Part-split seams on the boss are raw cuts in the generated mesh. Hiding WeaponArm or Shield leaves open edges at the shoulders, with no caps.

---

# Hero batch 2 (TRELLIS): Seraphine, Zeph, Kaelis

Added 2026-10-04. Same axes, scale (1 u = 1 m, glTF +Y up, front faces +Z), 30 fps and loop handling as Korrith (see top).

## seraphine_vol_ivory.glb: Seraphine Vol-Ivory

- Generator: TRELLIS-image-large (MIT), seed 2 of 1-5. Height 1.8 m.
- Size: 11.7 MB. Triangles total: **25,480**. By mesh: Seraphine_Body 22,000, Seraphine_CureFX 144, Seraphine_Halo 888, Seraphine_SanctuaryFX 2,448
- Textures (embedded PNG): body 2048px basecolor / metallicRoughness / normal / emissive; FX and props use untextured emissive materials.
- Design mapping:
  - porcelain ivory body (canon) from TRELLIS vertex colour, gloss ceramic shading
  - procedural cyan iso-line circuit traces baked on the porcelain (concept trace detail is finer than TRELLIS colour resolution)
  - dorsal halo ring -> Seraphine_Halo (6 neon nodes), rotates 60 deg per idle loop on FX_Halo (child of Head)
  - Cure Cascade -> Seraphine_CureFX hex glyph on FX_Cure
  - Phase Sanctuary -> Seraphine_SanctuaryFX hex-lattice dome on FX_Sanctuary (Root)

### Prop and FX bones

- `LeftHand_Prop`: empty (no handheld prop in concept)
- `RightHand_Prop`: empty
- `FX_Halo`: halo ring pivot behind head (rotation tracks)
- `FX_Cure`: Cure glyph (scale tracks)
- `FX_Sanctuary`: dome at feet (scale tracks)

### Clips

| Clip | Frames | Duration | Loop | Notes |
|---|---|---|---|---|
| `idle` | 0-72 | 2.4 s | yes | perfectly still apart from the rotating halo + faint breath |
| `ready` | 0-40 | 1.333 s | yes | casting guard, palms open |
| `attack` | 0-32 | 1.067 s | no | arcane palm pulse |
| `cure_cascade` | 0-48 | 1.6 s | no | gather f10, raise f22, pour f28; Cure glyph scales 0->1.35->0 |
| `phase_sanctuary` | 0-60 | 2.0 s | no | open f24, float f30-44 (feet stay planted, heels lift); dome scales 0->1.08->1.0 and collapses by f60 |
| `hit_react` | 0-20 | 0.667 s | no | recoil f3, recover f20 |
| `stunned` | 0-60 | 2.0 s | yes | head sway, halo wobbles without spinning |
| `death` | 0-64 | 2.133 s | no | sag f6, kneel f24, fall forward f46 |

### Bones (61)

`Root`, `Hips`, `Spine`, `Chest`, `UpperChest`, `Neck`, `Head`, `LeftEye`, `RightEye`, `Jaw`, `FX_Halo`, `LeftShoulder`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand`, `LeftIndexProximal`, `LeftIndexIntermediate`, `LeftIndexDistal`, `LeftMiddleProximal`, `LeftMiddleIntermediate`, `LeftMiddleDistal`, `LeftRingProximal`, `LeftRingIntermediate`, `LeftRingDistal`, `LeftLittleProximal`, `LeftLittleIntermediate`, `LeftLittleDistal`, `LeftThumbMetacarpal`, `LeftThumbProximal`, `LeftThumbDistal`, `LeftHand_Prop`, `RightShoulder`, `RightUpperArm`, `RightLowerArm`, `RightHand`, `RightIndexProximal`, `RightIndexIntermediate`, `RightIndexDistal`, `RightMiddleProximal`, `RightMiddleIntermediate`, `RightMiddleDistal`, `RightRingProximal`, `RightRingIntermediate`, `RightRingDistal`, `RightLittleProximal`, `RightLittleIntermediate`, `RightLittleDistal`, `RightThumbMetacarpal`, `RightThumbProximal`, `RightThumbDistal`, `RightHand_Prop`, `LeftUpperLeg`, `LeftLowerLeg`, `LeftFoot`, `LeftToes`, `RightUpperLeg`, `RightLowerLeg`, `RightFoot`, `RightToes`, `FX_Cure`, `FX_Sanctuary`

- Naming: Godot SkeletonProfileHumanoid names and hierarchy as Korrith (all 56 profile bones present, verified in Godot 4.7.2) + extras
- Feet/props: feet IK-locked during authoring (empties removed before export) and baked to FK keys; props have no wrist twist

### Validation

- Khronos glTF-Validator: 0 errors, 5 warnings (MESH_PRIMITIVE_GENERATED_TANGENT_SPACE, NODE_SKINNED_MESH_NON_ROOT; same Blender-exporter warnings as Korrith).
- Blender re-import: 8 actions; skinned meshes: Seraphine_Body, Seraphine_CureFX, Seraphine_Halo, Seraphine_SanctuaryFX.
- Godot 4.7.2 headless import: ANIM attack len=1.067 tracks=54 loop=0; ANIM cure_cascade len=1.6 tracks=54 loop=0; ANIM death len=2.133 tracks=54 loop=0; ANIM hit_react len=0.667 tracks=54 loop=0; ANIM idle len=2.4 tracks=54 loop=1; ANIM phase_sanctuary len=2.0 tracks=54 loop=0; ANIM ready len=1.333 tracks=54 loop=1; ANIM stunned len=2.0 tracks=54 loop=1; HUMANOID_PROFILE bones=56 missing=[]

Previews: `seraphine_vol_ivory_turntable.png`, `seraphine_vol_ivory_clips_contact.png`

## zeph_tri_lumen.glb: Zeph Tri-Lumen

- Generator: TRELLIS-image-large (MIT), seed 1 of 1-5. Height 1.0 m.
- Size: 12.5 MB. Triangles total: **16,370**. By mesh: Zeph_Body 16,000, Zeph_HexLanceFX 154, Zeph_PyreLatticeFX 216
- Textures (embedded PNG): body 2048px basecolor / metallicRoughness / normal / emissive; FX and props use untextured emissive materials.
- Design mapping:
  - compact Kith-Lir caster, ~1.0 m
  - three-lens helmet visor with cyan glow (TRELLIS colour)
  - procedural hex bump on the dark armour plates
  - Hex Lance -> Zeph_HexLanceFX crystalline lance on FX_Lance (child of RightHand_Prop)
  - Pyre Lattice -> Zeph_PyreLatticeFX sub-hex glyph on FX_Lattice (Root): grows overhead, spins, slams forward

### Prop and FX bones

- `LeftHand_Prop`: empty
- `RightHand_Prop`: parent of FX_Lance
- `FX_Lance`: Hex Lance pivot in right hand (scale tracks)
- `FX_Lattice`: Pyre Lattice glyph overhead (rotation/location/scale tracks)

### Clips

| Clip | Frames | Duration | Loop | Notes |
|---|---|---|---|---|
| `idle` | 0-60 | 2.0 s | yes | head-tilt scanning + breathing |
| `ready` | 0-40 | 1.333 s | yes | low caster stance |
| `attack` | 0-32 | 1.067 s | no | twin-palm spark push, f13 release |
| `hex_lance` | 0-36 | 1.2 s | no | right-arm thrust f14, lance extends then retracts |
| `pyre_lattice` | 0-60 | 2.0 s | no | arms up f22, glyph grows/spins f22-40, slams forward f45, fades by f60 |
| `hit_react` | 0-20 | 0.667 s | no | recoil f3, recover f20 |
| `stunned` | 0-60 | 2.0 s | yes | wobble |
| `death` | 0-60 | 2.0 s | no | stumble back, sit, topple |

### Bones (60)

`Root`, `Hips`, `Spine`, `Chest`, `UpperChest`, `Neck`, `Head`, `LeftEye`, `RightEye`, `Jaw`, `LeftShoulder`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand`, `LeftIndexProximal`, `LeftIndexIntermediate`, `LeftIndexDistal`, `LeftMiddleProximal`, `LeftMiddleIntermediate`, `LeftMiddleDistal`, `LeftRingProximal`, `LeftRingIntermediate`, `LeftRingDistal`, `LeftLittleProximal`, `LeftLittleIntermediate`, `LeftLittleDistal`, `LeftThumbMetacarpal`, `LeftThumbProximal`, `LeftThumbDistal`, `LeftHand_Prop`, `RightShoulder`, `RightUpperArm`, `RightLowerArm`, `RightHand`, `RightIndexProximal`, `RightIndexIntermediate`, `RightIndexDistal`, `RightMiddleProximal`, `RightMiddleIntermediate`, `RightMiddleDistal`, `RightRingProximal`, `RightRingIntermediate`, `RightRingDistal`, `RightLittleProximal`, `RightLittleIntermediate`, `RightLittleDistal`, `RightThumbMetacarpal`, `RightThumbProximal`, `RightThumbDistal`, `RightHand_Prop`, `FX_Lance`, `LeftUpperLeg`, `LeftLowerLeg`, `LeftFoot`, `LeftToes`, `RightUpperLeg`, `RightLowerLeg`, `RightFoot`, `RightToes`, `FX_Lattice`

- Naming: Godot SkeletonProfileHumanoid names and hierarchy as Korrith (all 56 profile bones present, verified in Godot 4.7.2) + extras
- Feet/props: feet IK-locked during authoring (empties removed before export) and baked to FK keys; props have no wrist twist

### Validation

- Khronos glTF-Validator: 0 errors, 4 warnings (MESH_PRIMITIVE_GENERATED_TANGENT_SPACE, NODE_SKINNED_MESH_NON_ROOT; same Blender-exporter warnings as Korrith).
- Blender re-import: 8 actions; skinned meshes: Zeph_Body, Zeph_HexLanceFX, Zeph_PyreLatticeFX.
- Godot 4.7.2 headless import: ANIM attack len=1.067 tracks=55 loop=0; ANIM death len=2.0 tracks=55 loop=0; ANIM hex_lance len=1.2 tracks=55 loop=0; ANIM hit_react len=0.667 tracks=55 loop=0; ANIM idle len=2.0 tracks=55 loop=1; ANIM pyre_lattice len=2.0 tracks=55 loop=0; ANIM ready len=1.333 tracks=55 loop=1; ANIM stunned len=2.0 tracks=55 loop=1; HUMANOID_PROFILE bones=56 missing=[]

Previews: `zeph_tri_lumen_turntable.png`, `zeph_tri_lumen_clips_contact.png`

## kaelis_moon_ravel.glb: Kaelis Moon-Ravel

- Generator: TRELLIS-image-large (MIT), seed 1 of 1-5. Height 1.88 m.
- Size: 10.2 MB. Triangles total: **22,256**. By mesh: Kaelis_BladeLeft 128, Kaelis_BladeRight 128, Kaelis_Body 22,000
- Textures (embedded PNG): body 2048px basecolor / metallicRoughness / normal / emissive; FX and props use untextured emissive materials.
- Design mapping:
  - hooded feline Sylvari-Mor executioner, dark suit with cyan/magenta neon lines (TRELLIS colour)
  - tail -> 7-bone Tail1..Tail7 chain off Hips, animated in every clip
  - legs: TRELLIS gave semi-digitigrade legs (long paw-foot, raised heel, low hock). Rig puts the hock at the Foot bone head and keeps heels raised in combat clips
  - twin crescent claw-blades (design addition; the concept shows claws only), rigid on LeftHand_Prop / RightHand_Prop

### Prop and FX bones

- `LeftHand_Prop`: left crescent blade grip
- `RightHand_Prop`: right crescent blade grip
- `Tail1-Tail7`: tail chain (deform bones, not in the humanoid profile)

### Clips

| Clip | Frames | Duration | Loop | Notes |
|---|---|---|---|---|
| `idle` | 0-90 | 3.0 s | yes | 3 s loop, weight on the balls of the feet, tail flick every 1.5 s |
| `ready` | 0-40 | 1.333 s | yes | stalker crouch, heels raised, tail swaying |
| `attack` | 0-32 | 1.067 s | no | right-blade slash: wind f7, cut f12 |
| `crescent_sever` | 0-40 | 1.333 s | no | coil f8, lunge step f15 (lead foot travels then plants), wide double slash, recover f40 |
| `ravel_execution` | 0-48 | 1.6 s | no | crouch f10-14, leap f19, twin-blade pierce lands f23, recover f48 |
| `hit_react` | 0-20 | 0.667 s | no | recoil f3, recover f20 |
| `stunned` | 0-60 | 2.0 s | yes | tail droops, head sway |
| `death` | 0-64 | 2.133 s | no | stagger f6, knees f24, fall forward f46 |

### Bones (65)

`Root`, `Hips`, `Spine`, `Chest`, `UpperChest`, `Neck`, `Head`, `LeftEye`, `RightEye`, `Jaw`, `LeftShoulder`, `LeftUpperArm`, `LeftLowerArm`, `LeftHand`, `LeftIndexProximal`, `LeftIndexIntermediate`, `LeftIndexDistal`, `LeftMiddleProximal`, `LeftMiddleIntermediate`, `LeftMiddleDistal`, `LeftRingProximal`, `LeftRingIntermediate`, `LeftRingDistal`, `LeftLittleProximal`, `LeftLittleIntermediate`, `LeftLittleDistal`, `LeftThumbMetacarpal`, `LeftThumbProximal`, `LeftThumbDistal`, `LeftHand_Prop`, `RightShoulder`, `RightUpperArm`, `RightLowerArm`, `RightHand`, `RightIndexProximal`, `RightIndexIntermediate`, `RightIndexDistal`, `RightMiddleProximal`, `RightMiddleIntermediate`, `RightMiddleDistal`, `RightRingProximal`, `RightRingIntermediate`, `RightRingDistal`, `RightLittleProximal`, `RightLittleIntermediate`, `RightLittleDistal`, `RightThumbMetacarpal`, `RightThumbProximal`, `RightThumbDistal`, `RightHand_Prop`, `LeftUpperLeg`, `LeftLowerLeg`, `LeftFoot`, `LeftToes`, `RightUpperLeg`, `RightLowerLeg`, `RightFoot`, `RightToes`, `Tail1`, `Tail2`, `Tail3`, `Tail4`, `Tail5`, `Tail6`, `Tail7`

- Naming: Godot SkeletonProfileHumanoid names and hierarchy as Korrith (all 56 profile bones present, verified in Godot 4.7.2) + extras
- Feet/props: feet IK-locked during authoring (empties removed before export) and baked to FK keys; props have no wrist twist

### Validation

- Khronos glTF-Validator: 0 errors, 4 warnings (MESH_PRIMITIVE_GENERATED_TANGENT_SPACE, NODE_SKINNED_MESH_NON_ROOT; same Blender-exporter warnings as Korrith).
- Blender re-import: 8 actions; skinned meshes: Kaelis_BladeLeft, Kaelis_BladeRight, Kaelis_Body.
- Godot 4.7.2 headless import: ANIM attack len=1.067 tracks=56 loop=0; ANIM crescent_sever len=1.333 tracks=56 loop=0; ANIM death len=2.133 tracks=56 loop=0; ANIM hit_react len=0.667 tracks=56 loop=0; ANIM idle len=3.0 tracks=56 loop=1; ANIM ravel_execution len=1.6 tracks=56 loop=0; ANIM ready len=1.333 tracks=56 loop=1; ANIM stunned len=2.0 tracks=56 loop=1; HUMANOID_PROFILE bones=56 missing=[]

Previews: `kaelis_moon_ravel_turntable.png`, `kaelis_moon_ravel_clips_contact.png`, `kaelis_moon_ravel_legs_evidence.png`

## Hero batch 2: sources and licenses

- **Microsoft TRELLIS (TRELLIS-image-large) image-to-3D, run locally on gregspc (RTX 4070, torch 2.5.1+cu124)**: https://huggingface.co/microsoft/TRELLIS-image-large (MIT (code and TRELLIS-image-large weights)). Output used: TRELLIS sparse-structure -> SLAT -> mesh decoder (FlexiCubes) with per-vertex colour. TRELLIS own GLB texture baking (to_glb) was NOT used because it needs nvdiffrast; colour was baked in Blender from the TRELLIS vertex colours instead.
- Dependencies (licenses checked):
  - DINOv2 ViT-L/14 reg (image encoder used by TRELLIS): Apache-2.0
  - rembg isnet-general-use (background removal in gen_trellis.py before TRELLIS preprocessing): MIT (rembg); IS-Net weights Apache-2.0
  - xformers 0.0.28.post3: BSD-3-Clause
  - spconv-cu124 2.3.8 / cumm-cu124: Apache-2.0 / MIT
  - utils3d (+moderngl, glcontext): MIT
  - easydict: LGPL-3.0 (unmodified library, dynamic use)
  - pccm, ccimport, lark, termcolor: MIT
  - fire, ninja: Apache-2.0
  - pybind11, portalocker: BSD
  - plyfile: GPL-3.0; installed as a TRELLIS import dependency but stubbed and never called; not shipped
  - xatlas (python, in Blender) for UV unwrapping: MIT
  - Blender 4.2.23 LTS (tool only): GPL (outputs are not covered)
- Avoided (non-commercial or out of scope):
  - nvdiffrast (NVIDIA Source Code License: non-commercial): not installed; TRELLIS to_glb texture bake skipped
  - diff-gaussian-rasterization (Inria/MPII Gaussian-Splatting License: non-commercial): not installed; gaussian renders not used
  - kaolin and open3d: stubbed in gen_trellis.py (not needed for mesh extraction)
  - Hunyuan3D-2: not used for this batch (proof-only per user)
- Procedural additions authored by script: Seraphine circuit traces, halo, Cure glyph, Sanctuary dome; Zeph hex bump, Hex Lance, Pyre Lattice; Kaelis crescent blades. All rigs, weights and clips are scripted.

## Hero batch 2: pipeline

- PC (gregspc, RTX 4070): TRELLIS-image-large, concept -> mesh with vertex colour, seeds 1-5 per hero (~10 s each), best seed picked from vertex-colour turnarounds
- Box (Blender 4.2.23 LTS headless): orient (+Z forward), scale to height, clean floaters, decimate, xatlas UV, 2K bake of basecolor/MR/normal/emissive from TRELLIS vertex colour via an HSV-classified material (neon -> emissive), procedural traces/hex bump
- Script-built Korrith-matched skeleton, bone-heat weights + rule fixes + joint blends, FootIK-authored clips baked to FK, procedural FX/props rigid on their own bones
- Validation: Khronos gltf-validator, Blender re-import, Godot 4.7.2 headless import with the shipped .glb.import

Scripts: vs4-3d/scripts (stage1t.py, stage2_hero.py, hero_rig.py, hero_*.py, fxlib.py, trellis_io.py, make_import.py, manifest_heroes.py); PC: C:\Users\liftd\AI3D\gen_trellis.py, setup_trellis.ps1, run_trellis.ps1

## Hero batch 2: quality assessment (honest)

| Hero | Gate result | Notes |
|---|---|---|
| Seraphine Vol-Ivory | **Shipped** | Clean silhouette, porcelain ivory reads well all around (no untextured patches). Halo, Cure glyph and Sanctuary dome animate correctly. Weak points: the concept's fine ornament is lost at TRELLIS's colour resolution (~7 mm), so the cyan circuit traces are procedural iso-lines, not the concept's exact layout. The skirt shards are skinned to the legs and splay a little in deep poses (kneel/death). |
| Zeph Tri-Lumen | **Shipped** | Readable compact caster with a three-lens visor and neon accents; 16.4k tris (under budget as allowed). The hex pattern is a procedural bump. The back is dark with few accents (TRELLIS colour). The Pyre Lattice glyph is large; the standard contact-sheet camera crops it, so a wide-framing row was added. |
| Kaelis Moon-Ravel | **Shipped, flagged for review** | Tail OK: 7-bone chain, free-hanging, clean weights (tail verts reassigned, leg/tail cross-weights removed), animated in every clip. Legs: TRELLIS produced **semi-digitigrade** legs (long paw-foot, raised heel, low hock), not a true high-hock leg. The geometry is intact and deforms cleanly, so it passed the gate, but it is not fully digitigrade. Evidence: `kaelis_moon_ravel_legs_evidence.png`. The inner hood lining glows magenta; the face itself stays dark as in the concept. The crescent blades are a design addition (the concept shows claws only). |

Common: textures are baked from TRELLIS vertex colour (not projected from the concept), so they cover the whole model but are softer than Korrith's concept-projected bake. TRELLIS's own texture baker was not used because it needs nvdiffrast (non-commercial).
