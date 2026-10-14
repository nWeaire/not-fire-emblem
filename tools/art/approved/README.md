# Approved art samples (session 4)

The samples the user approved when settling the art style. See `docs/ART_DIRECTION.md` for the decisions and how
to generate more. The `.gdignore` keeps Godot from importing this folder: these are references and inputs, not game
assets. When art is promoted into the game, copy it to `assets/` in the layout `SpriteLibrary` expects.

| Folder | Contents |
|---|---|
| `refs/` | Style references: `gba_clean_fighters.png` (the character style, background removed), crest style guides H1/G5/G6, and the round-1 crops of the user's examples |
| `characters/` | Rowan, Kai, Mira, Gloam Brute and Binder: `map_*` (48 px, 8 rotations) and `battle_*` (64 px, east) |
| `animations/` | `rowan_crit`, `rowan_attack`, `brute_attack`, `binder_cast` frames (east) |
| `terrain_gba16/` | The approved 16 px tile set as PixelLab returned it (`tile_N.png`; the mapping is in ART_DIRECTION §2) |
| `battle_bg/` | GBA forest without fighters (inpainted), the original with fighters, and GBA plains |
| `ui_clean/` | Clean UI window panel and name plate (9-slice sources) |
| `portraits/` | The chosen portrait style guides (Rowan, brigand chief) |
| `vfx/` | Impact burst, cut streak and claw rake used in the battle mockup |
| `fonts/` | Pixelify Sans and Silkscreen (SIL OFL; licences included) |
| `crests/` | Crest line-art sources; `tools/art/crest_recolor.py --build` turns them into `assets/crests/` |
| `pixellab_state/` | PixelLab ids from each round (characters can be re-downloaded or animated further from these) |
| `scripts/` | The one-off pilot and mockup scripts, for reference. They expect the session scratch layout (`pilot/`, `pilot2/`, `pilot3/`, `fonts/`, `refs/` next to the script), so adjust paths before rerunning. |
