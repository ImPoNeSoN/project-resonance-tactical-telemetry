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
