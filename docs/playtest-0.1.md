# Playtest 0.1

A Windows build outside players can unzip and run. It does not need Godot or a .NET install. The zip is `ProjectResonance-<version>-win64.zip`, and the program inside is `ProjectResonance.exe`.

## Install and run

1. Download the zip from the GitHub Release for the `v*` tag (for this build, `v0.1.0`).
2. Unzip it into an empty folder.
3. Run `ProjectResonance.exe`.

Windows may show a SmartScreen warning because the build is unsigned. Choose **More info**, then **Run anyway**.

The game opens on a title screen: **Play**, **Settings**, **Quit**. Play goes to party select (four heroes), then the Cinder Throat floor, then a victory or defeat screen, then back to the title.

Settings are saved and restored the next time you launch:

- Windowed, borderless, or fullscreen
- 1280×720, 1600×900, 1920×1080, or 2560×1440
- 3D arena or grey-box
- Battle speed 1×, 2×, or 4×

The grey-box button on the fight HUD uses the same view setting. Space pauses. Auto-run plays the gambit decks. The MP cost of an ability is in the ability panel; the buttons show the full name.

A small version and seed label sits in the bottom-right corner of the fight. Include both in a bug report.

## What to test

- Launch, change every setting, quit, and launch again. The choices should stick.
- Build a party of four, including one that is not a preset. Enter the floor.
- Clear or wipe Cinder Throat: two trash rooms, the Kiln Warden, the rest, then the Carapace Engine.
- Try the floor in the 3D arena and in grey-box, at 1× and at 4×.
- On the result screen, open the log folder and confirm a new JSON file for that run.
- Read a few ability names on Zeph (Blizzard II, Pyre Lattice, Hex Lance, Tri Spark) and on Aurel. The full name should be visible, and the panels should not jump when you change heroes.

## Known issues

- **Kaelis Moon-Ravel's legs** are semi-digitigrade. The mesh generator gave him a long paw-foot, a raised heel, and a low hock, not a true high-hock leg. The geometry deforms cleanly. Evidence is `kaelis_moon_ravel_legs_evidence.png` in the hero model folder.
- **Saeli Thorn-Vesper's legs** were reshaped in Blender into a digitigrade stance (knee forward, hock raised, heel lifted). Deep poses can still look off. Her tail was rebuilt because the generator fused it into the thigh.
- **Korrith and the Carapace Engine** are Hunyuan3D-2 proof meshes. They are fine for this private playtest. They are not licensed for a public release (the Hunyuan 3D 2.0 Community License does not cover the EU, the UK, or South Korea, and the outputs must not be used to train other models). Decision 66 still stands for a public build.
- Trash rooms and the Kiln Warden do not have their own 3D sets. The arena shows the Carapace proof scene. Use grey-box when you want the room itself to read clearly.
- The build is unsigned, and there is no installer.

## Logs

Run logs are JSON files written when a floor ends (cleared, wiped, or abandoned):

- Windows: `%APPDATA%\Godot\app_userdata\Project Resonance\runs\`
- Linux: `~/.local/share/godot/app_userdata/Project Resonance/runs/`

Each file has the build version, seed, party, rooms cleared, result, duration, combat ticks, and the full combat log. The result screen button **Open log folder** opens that directory.

If the game throws an unhandled exception, a text file is written under `crash\` next to `runs\`.

## Feedback

Reply with the version and seed from the HUD corner, and the run log if a fight went wrong.

1. Could you tell what your party was about to do?
2. Did any ability name stay cut off?
3. Was the 3D fight readable, or did you stay on grey-box?
4. Where did the floor feel too easy or too hard?
5. Did settings survive a restart?
6. What broke, and what would you play again?
