# Project Resonance — VS-4 art pack (HUD kit + icons)

Scope: **HUD 9-slice kit and 32px status/element icons only.** Hero portraits and Carapace Engine boss layers were descoped by the coordinator on 2026-10-03; no portrait or boss files were produced, so none are listed.

Style: Ant Legion UX density, mechanical space skin. Gunmetal fill, beveled metal border, cyan #00E5FF / magenta #E13CFF neon trim, chamfered corners with corner brackets. Drawn procedurally (Python/Pillow/numpy, 4x supersampled for the HUD and 8x for icons, then box/Lanczos downscaled), so alpha is native and there is no chroma key and no green fringe.

All files are RGBA PNG at **2x** the target size. Godot 4.7.2 import: Filter Linear, Mipmaps off.

## HUD 9-slice panels (`hud/`)

| File | Target (1x) | Shipped (2x) | Margins @2x L/T/R/B | Margins @1x L/T/R/B | Purpose |
|---|---|---|---|---|---|
| `hud/hud_party_card@2x.png` | 192×96 | 384×192 | 32/32/32/32 | 16/16/16/16 | Hero card in the bottom-left party panel (HP/MP/Heat bars, statuses). |
| `hud/hud_boss_part_panel@2x.png` | 224×72 | 448×144 | 32/24/32/24 | 16/12/16/12 | Row panel for one boss sub-target (Core / Shield / Weapon Arm) in the anatomy panel. |
| `hud/hud_ability_panel@2x.png` | 160×64 | 320×128 | 24/24/24/24 | 12/12/12/12 | Pinned ability details card / ability bar backing in bottom-center. |
| `hud/hud_timeline_bar@2x.png` | 320×32 | 640×64 | 48/24/48/24 | 24/12/24/12 | 10,000-AP timeline track backing (top band); ticks and chips drawn by TimelineBar.cs. |
| `hud/hud_button_normal@2x.png` | 128×40 | 256×80 | 24/24/24/24 | 12/12/12/12 | Generic command button, normal state. |
| `hud/hud_button_hover@2x.png` | 128×40 | 256×80 | 24/24/24/24 | 12/12/12/12 | Generic command button, hover state (brighter glow and trim). |
| `hud/hud_button_pressed@2x.png` | 128×40 | 256×80 | 24/24/24/24 | 12/12/12/12 | Generic command button, pressed state (darker fill, inset bevel, dim glow). |

9-slice notes:
- Use as `StyleBoxTexture` with `texture_margin_*` = the 2x margins (the texture is 2x), or set a 0.5 scale and use the 1x margins.
- The center cell is one flat color, and every edge stretch zone is a single repeated row or column (checked by script). Stretch, Tile, and TileFit are all seam-free.
- The outer glow is 6px at 2x of alpha padding inside the texture. Set `content_margin_*` at least the texture margins so text and bars stay off the trim.
- Button states share the same geometry and margins, so they swap without layout shift. Hover: brighter fill, trim, and outer glow. Pressed: darker fill, inverted (inset) bevel, dimmed trim and glow.

## Icons (`icons/`)

Hex badge: metal bezel, neon rim in the element color, dark gunmetal well, bold glyph with a dark outline and glow. Element colors follow docs/04 §4.4.1. Each property also has its own glyph shape, which covers the colorblind rule.

| File | Label | Color | Target | Shipped | Purpose |
|---|---|---|---|---|---|
| `icons/icon_status_stun@2x.png` | Stun | #FFC93C | 32×32 | 64×64 | Status: stunned (no AP gain). |
| `icons/icon_status_shield@2x.png` | Shield | #00E5FF | 32×32 | 64×64 | Status: absorb shield active. |
| `icons/icon_status_regen@2x.png` | Regen | #5CFF9D | 32×32 | 64×64 | Status: HP regen pulses. |
| `icons/icon_element_fire@2x.png` | Fire | #FF7A1F | 32×32 | 64×64 | Element/property: Fire. |
| `icons/icon_element_ice@2x.png` | Ice | #8FE3FF | 32×32 | 64×64 | Element/property: Ice. |
| `icons/icon_element_wind@2x.png` | Wind | #7CF2B0 | 32×32 | 64×64 | Element/property: Wind. |
| `icons/icon_element_light@2x.png` | Light | #FFF2B3 | 32×32 | 64×64 | Element/property: Light. |
| `icons/icon_element_darkness@2x.png` | Darkness | #5A3FA8 | 32×32 | 64×64 | Element/property: Darkness (glyph tinted lighter for legibility). |
| `icons/icon_element_earth@2x.png` | Earth | #C69A3A | 32×32 | 64×64 | Element/property: Earth. |
| `icons/icon_element_blunt@2x.png` | Blunt | #B8A99A | 32×32 | 64×64 | Physical property: Blunt. |
| `icons/icon_element_slashing@2x.png` | Slashing | #E0414F | 32×32 | 64×64 | Physical property: Slashing. |
| `icons/icon_element_piercing@2x.png` | Piercing | #E6EDF2 | 32×32 | 64×64 | Physical property: Piercing. |
| `icons/icon_element_lightning@2x.png` | Lightning | #C9A8FF | 32×32 | 64×64 | Element/property: Lightning. |
| `icons/icon_element_water@2x.png` | Water | #2F7BFF | 32×32 | 64×64 | Element/property: Water (glyph tinted lighter for legibility). |
| `icons/icon_element_umbral@2x.png` | Umbral | #B0307A | 32×32 | 64×64 | Damage type: Umbral true damage (glyph tinted lighter for legibility). |
| `icons/icon_element_true@2x.png` | True | #FFFFFF | 32×32 | 64×64 | Damage type: True damage (white with black outline). |

## Preview

`preview_contact_sheet.png` shows each panel at native 2x with magenta margin guides, the same panel 9-slice stretched, and each icon at 64px, at 32px, and at 32px zoomed 2x.

## Totals

- hud/: 7 PNG (4 panels + 3 button states)
- icons/: 16 PNG (3 status + 9 element/property + Lightning, Water, Umbral, True)
- preview_contact_sheet.png, MANIFEST.md, manifest.json
