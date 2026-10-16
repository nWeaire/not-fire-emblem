# Art Direction

The approved look for The Radiant Crest, settled in session 4 (2026-10-14) through three review rounds with the
user. New art must follow this. The approved samples live in `tools/art/approved/` (a `.gdignore` keeps Godot
from importing them); finished game assets go in `assets/` as usual.

Review pages (private to the user):
- Style pilot, rounds 1–3: https://claude.ai/artifact/MRRpmAW3U5enbzH82NRw3b
- Crests: https://claude.ai/artifact/49JsEJjZySz3ExJN3xGAuS

User's reference images: `assets/ExampleFiles/` (GBA Fire Emblem map-sprite class sheets and a GBA battle GIF).

---

## 1. Decisions

| Area | Decision | Approved sample |
|---|---|---|
| Overall | GBA Fire Emblem pixel art. More detail than the original GBA games is fine. | `assets/ExampleFiles/` |
| Characters | The style of the two fighters painted into the GBA forest background: small and chunky, muted earthy palette, 2–3 tones per material, dark outline | `approved/refs/gba_clean_fighters.png`, `approved/characters/` |
| Map sprites | 48 px canvas (~32 px body), `low top-down`, 8 rotations (the game uses 4) | `approved/characters/map_*` |
| Battle sprites | **Session 5: the brighter "E" look** for the whole battle cast: 84 px tall stills on a 128×84 canvas, more saturated, anime proportions, dynamic stance. Facing east (mirrored for the right-hand side). | `approved/refs/*_battle.png`, `rowan_battle_E1.png` |
| Attacks | Restrained sprite motion. The over-the-top feel comes from screen effects (§4). | `approved/animations/` |
| Terrain | GBA level, 32 px tiles (switched from 16 px in session 5), flat bright colours | `approved/terrain_gba32/` |
| Battle backgrounds | GBA level: 240×135, 32 colours, with no people in them | `approved/battle_bg/` |
| UI | Clean: flat charcoal panels, thin warm-white border with corner ticks | `approved/ui_clean/` |
| Font | Pixelify Sans for text, Silkscreen for small caps labels (both OFL) | `approved/fonts/` |
| Portraits | Rowan take #1 and brigand chief take #4 are the style guide | `approved/portraits/` |
| Crests | Grancrest-style glowing linework on a V/anchor core. Radiant white or Shrouded purple. | `assets/crests/`, §5 |

Rejected (don't drift back): the session-2 painterly high-res art, the round-1 96 px battle sprites (too detailed),
detailed or HD terrain, mid and high detail backgrounds, and the Classic blue, Parchment and Crest UI styles.

## 2. How to generate each type (PixelLab)

Costs are what the API actually charged in session 4.

### Characters: `POST /create-character-pro` (10 gens each)
```
method: create_with_style, template_id: mannequin, no_background: true
reference_image: tools/art/approved/refs/gba_clean_fighters.png   (must have no background, see §3)
map:    image_size 48×48, view "low top-down"
battle: image_size 64×64, view "side"
description: "<who>. small chunky 16-bit pixel art sprite like the fighters in a Game Boy Advance battle scene,
  muted earthy palette, 2-3 tone flat shading per material, dark outline, low detail, bold readable silhouette"
style_description: the same style sentence
```
The working script is `tools/art/approved/scripts/pilot3.py`. Its PixelLab character ids are in
`approved/pixellab_state/round3_characters.json`, so more animations (walk, hit, dodge) can be added to the five
approved characters without paying to recreate them.

### Battle sprites in the "E" look: still takes, then `tools/art/animate_still.py`
1. Get still takes: `POST /generate-image-v2`, 84×84, `no_background`, `target_palette {max_colors: 16}`, description
   `"<who>, full body, side view. Game Boy Advance Fire Emblem battle animation sprite, like Fire Emblem: The Blazing
   Blade, bright saturated colours, anime proportions, 3-tone cel shading, crisp dark outline, clean readable pixels"`
   (15 gens for 9 takes; the script is `tools/art/takes/pilot5/pilot5.py`, grid "E"). Pick a ready-stance take.
2. `python tools/art/animate_still.py <take.png> <sprite_id> --weapon <w>` animates that still directly
   (`/animate-with-text-v3`, about 7 gens for idle/attack/critical/hit/dodge) onto a 128×84 canvas.
   **Don't** build a character from the still (`create-character-pro` `create_from_concept`): it redrew Rowan as a
   big-headed chibi. The battle screen draws art up to 84 px tall 1:1 (`MaxNativeHeight` in `BattleSceneView`).
3. Check every animation frame by frame. The model paints effects (splashes, slashes) into hit frames even when told
   not to, so the tool drops idle/hit/dodge frames whose area grows >12% or that gain >30 new near-white pixels
   (`--clean-only` re-runs that filter for free). "Backward" is ambiguous for a right-facing still: say "to the LEFT".
   Weapons can vanish mid-animation (Brann's dodge did); redo those with `--only <anim> --redo <anim>`.

### Animations: `POST /characters/animations`, mode `v3`, east only (1–2 gens each)
End every action with: *"keeps facing right the whole time, side view, never turns toward the camera"*. Without it,
about half the attacks spun toward the camera mid-swing. 8–12 frames is enough.

### Terrain: `POST /create-tiles-pro` (10 gens per set)
`tile_size 32, tile_type square_topdown, tile_view top-down, outline_mode segmentation`, numbered prompts
(`"1). lush green grass field tile 2). …"`) plus *"Game Boy Advance Fire Emblem map tile, flat bright colours, very
limited palette, simple shapes, seamless top-down"*. It returns 16 tiles grouped by prompt, not one per prompt. The
approved 32 px set (`approved/terrain_gba32/T_terrain32`) maps as: plain 0,1,2 · forest 3,4 · mountain 5,6 · wall 7,8 ·
fort 9,10 · water 11,12 · bridge 13,14; fire is `V_village32` 0–3. **Don't pass 16 px style images**: they force
16 px output. The game samples tiles with nearest filtering. (The earlier 16 px set is in `approved/terrain_gba16/`.)

### Battle backgrounds: `POST /generate-image-v2` (10 gens at 240×135)
`target_palette: {max_colors: 32}`, *"… Game Boy Advance Fire Emblem battle background, 16-bit GBA pixel art, flat
simple shading"*, with the ground in the lower half for two fighters. Add *"no people, no characters"*: two of six
came back with fighters painted in. A background the user likes but that has figures in it can be fixed with
`POST /inpaint-v3` (10 gens). The `inpainting_image` and `mask_image` fields are `{image: Base64Image, size}`, and
white in the mask means regenerate. `forest_gba_clean.png` was made this way.

### Portraits: `POST /generate-image-v2` (10 gens per 4 takes at 96×96)
`target_palette: {max_colors: 20}`, *"<who>. Game Boy Advance Fire Emblem dialogue portrait, anime bust shoulders
up, facing left in three-quarter view, 16-bit GBA pixel art, about 16 colours, hard cel shading, bold dark outline,
no gradients, like Fire Emblem: The Blazing Blade"*. Generate 4 takes and pick one.

### UI panels: `POST /generate-ui-v2` (10 gens per 4 takes)
Panels are drawn with 9-slice scaling (corner margin 12 px for windows, 8 px for name plates) at a 3× UI pixel
scale (427×240 UI canvas → 1280×720). See `approved/scripts/world_ui.py` for the layout used in the mockup.

## 3. PixelLab gotchas (learned the hard way)
- **A style reference with a background leaks that background into every output.** The first character batch came
  back with forest painted behind them, which cost 80 gens to redo. Cut the reference out first: `POST
  /remove-background` with `remove_complex_background` (1 gen).
- **The word "hand" in a crest prompt makes it draw hands.** Use "floating crest emblem, no hand, no body".
- **Crest and icon prompts drift to gold badges** (shields, frames, coins) unless the prompt says *"no gold, no
  metal, no shield, no frame, no circle border"*.
- **`generate-image-v2` and `generate-with-style-v2` return a grid**: 16 images at ≤64 px, 9 at 65–84 px, 4 at
  86–170 px, 1 above 170 px. 84 px is the sweet spot for picking from many options.
- **`generate-with-style-v2` keeps each grid slot's subject across calls** with the same style images, so batching
  four tier descriptions with the same motif list gave 18 crest lines that grow coherently from tier to tier.
- **At most 8 concurrent jobs** on Tier 1. Queue and wait for the oldest job on HTTP 429.
- **Storage downloads sometimes return 525.** Retry the download; the asset is fine.
- **High-detail 480×270 images cost 25 gens**, and `create-tiles-pro` at 64 px costs 15.

## 4. Battle screen effects (to build in `BattleSceneView`)
The mockups (`approved/scripts/mockup3.py`) define the feel. Most of the drama is code, not sprite frames:
1. **Critical wind-up:** a white flash pop, then the screen dims toward dark violet (~50%) and a **CRITICAL!** banner shows.
2. **Lunge to contact:** the attacker dashes in over the wind-up frames and stops where the **weapon tip touches the
   target's body**. Compute that point from the sprites' bounding boxes. Never use the sprite's centre: a weapon or a
   mirrored axe pushes the centre into empty space, which was round 2's "swing in the wrong place" bug.
3. **On the hit frame:** a cut streak across the target's chest, a full white-out for 1–2 frames, and an impact
   burst scaling up at the contact point.
4. **Reaction:** screen shake (5–6 frames, decaying), the target flashing white and red alternately, knockback of a
   few pixels, a damage number rising, and the HP bar draining over ~4 frames.
5. **Recovery:** the attacker slides back. Counters reuse the same steps at a lower intensity, with no dim or banner.
Effect sprites: `approved/vfx/` (impact burst, cut streak, claw rake).

## 5. Crests
- **Rule:** a crest keeps its shape and takes its colour from the bearer's heart (STORY §4.1). Generate the line art
  once, then recolour with `tools/art/crest_recolor.py` into `_radiant` (white) and `_shrouded` (deep purple).
- **Style guide:** `approved/refs/crest_style_H1.png`, `_G5.png` and `_G6.png`. These are thin glowing calligraphic
  strokes, hooked blade-like curls and heraldic symmetry on a V/anchor core, in the spirit of Record of Grancrest War.
- **Tiers** get more elaborate: 1 Kindled → 2 Knight's → 3 Lord's → 4 Sovereign's.
- **Rowan's line** (`assets/crests/rowan/t1..t4`): H1 → T2-6 → T3-2 → T4-6. Each tier was generated from the
  previous one with `generate-image-v2` `reference_images`. The Lord's tier renders a little dimmer than the others;
  even it out when it goes into the UI.
- **Library:** 18 lines × 4 tiers in `assets/crests/<motif>/t<N>_{radiant,shrouded}.png`. The motifs are horns,
  thorns, feathers, flames, crescent, serpent, star, sword, tree, claws, antlers, spire, rose, lightning, sun, fins,
  skull and eye. Sources are in `tools/art/approved/crests/`; rebuild with `python tools/art/crest_recolor.py --build`.
- **Bearers** (accepted in session 5; in code as `CrestEmblem.Bearers`): the Binder serpent (t1), Sennet thorns (t1),
  Varga tree (t1), Rusk claws (t1), Halvric sword (t2), Castellan rose (t3), Vossk skull (t3), Ilse flames (t3),
  Morvane eye (t4).
- **In game:** `src/UI/Nodes/CrestEmblem.cs` draws a crest with its pulsing glow. Rowan's (radiant) shows in the hub
  and on his unit panel; an enemy crest-bearer's (shrouded) shows on theirs.
- Glow is a runtime effect (an additive blurred copy that can pulse), not baked into the PNGs.
