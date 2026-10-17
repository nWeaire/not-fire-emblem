# Art Needed

Everything the game still shows as a placeholder, and where the game looks for the real file. Generate new art
following **`docs/ART_DIRECTION.md`** with `tools/art/generate.py` (CLAUDE.md §5). Sizes: map sprites 48 px, battle
sprites in the "E" look (84 px tall), portraits 96 px, terrain tiles 32 px, backgrounds 240×135.

**How placeholders work today**
- **Map units** without art are capsules in the faction colour.
- **Fights** fall back to on-map animations unless *both* units have battle sprites.
- **Portraits:** characters without one get a coloured square with their initials.
- **Story backgrounds** without a picture are tinted gradients labelled `[background: id]`. Variants reuse their
  location's picture with a tint (`farm_night_smoke` → `farm`), so only locations need pictures.

Priority: **P1** is seen in the playable story (Prologue to Ch 3). **P2** is needed for upcoming chapters and promotions. **P3** is polish.

**Done in the GBA style (session 5):** map + battle sprites for `rowan_youth`, `rowan_lord`, `kai_soldier`,
`mira_acolyte`, `brann_fighter`, `aldric_warden`, `tamsin_archer`, `enemy_gloam_hound`, `enemy_gloam_brute`,
`enemy_gloam_stalker`, `enemy_binder`, and the `ally_villager` map sprite. Portraits `aldric`, `tamsin`, `wenna`,
`binder`, `sennet`, `varga`, `elise` (other takes in `tools/art/takes/portraits/`). Backgrounds `farm`, `village`,
`farmhouse_interior`, `village_hall`, `river_road`, `mill`, `thornwood`, `forest`, `plains`. The 32 px terrain set and
`fire` tiles, plain grass detail and ground terrain (road, field, meadow, square), earth block sides
(`assets/textures/cliff_side.png`), and billboard map props (pine, oak, cottage, peak in `assets/sprites/props/`).
Clean UI panel, name plate and fonts.

---

## 1. Map sprites (48 px, 4 directions, `walk` `attack`; idle falls back to the still rotation)
Folder: `assets/sprites/units/<spriteId>/`. Units find art through `BattleUnit.SpriteCandidates()`:
`<spriteId>_<classId>` first, then `<spriteId>`.

| Priority | Sprite id | Who / what | Used in |
|---|---|---|---|
| Done (session 7) | `boss_sennet`, `boss_varga` | Named Binder bosses: map stills + battle sprites (Varga's battle animations pending, see NEXT_STEPS). Gorran already uses his own `boss_brigand_chief`. Map walk/attack still to do | Ch 1, Ch 3 |
| Done | `rowan_crest_knight`, `aldric_warden_lord`, `tamsin_sniper`, `brann_warrior`, `kai_paladin`, `mira_sage`, `elise_sage` | Tier 2 outfits, map + battle (session 5) | After promotion |
| P3 | Tier 3–4 outfits | When those classes exist (`PLANNED` in `tools/art/manifest.py`) | Act II+ |

## 2. Battle-screen sprites (the "E" look: 84 px stills animated on 128×84, east only, `idle` `attack` `critical` `dodge` `hit`)
Folder: `assets/sprites/battle/<spriteId>/`. Same ids as §1; `ally_villager` has none (rarely fights).

## 3. Portraits (96 px; base portrait first, then expressions)
Folder: `assets/portraits/`. The dialogue box looks for `<speaker>_<expression>.png`, then `<speaker>.png`.
Expressions used by the scripts: `neutral smile laugh surprised worried sad angry determined hurt cold`.

| Priority | File | Notes |
|---|---|---|
| P2 | `rowan`, `kai`, `mira`, `brann`, `osmund`, `boss_brigand_chief` | Session-2 portraits; check against the GBA style, regenerate if they clash |
| P2 | Expression sets for the main cast | e.g. `rowan_worried.png`. Without them the base portrait is used for every expression. |

## 4. Story backgrounds (`assets/backgrounds/<id>.png`)
| Priority | Ids |
|---|---|
| P2 | Hub backdrop (behind the hub menus); locations for Ch 7+ (title vista done in session 7) |

## 5. Terrain
Folder: `assets/textures/terrain/<terrainId>[_N].png` (32 px top-down; `_N` are random alternates).

| Priority | Tile | Notes |
|---|---|---|
| P2 | House / chapel roofs | Generated (`tools/art/takes/tiles/village/` tiles 1/5/9/13 and 2/6/10/14) but need new terrain types; villages reuse `fort` (T) and `wall` (W) |
| P3 | Marsh / reeds / shallow water | Needed by Ch 6 (Greyfen) |
| Done (session 7) | **Wheat** crop prop (replaces the cabbage `crop`) | User request (session 7): field cells (`:`) should show **rows of wheat** instead of lettuce/cabbages, for a golden/straw colour against all the green. 32 px billboard takes like the other props (`generate.py props`, `assets/sprites/props/wheat_N.png`); plant them as lines across the cell, not a 2×2 grid (`MapView.AddProps`, case `field`). Pilot 1 take first. |
| Done (session 7) | **Hedges** | Farm-map walls are solid 3D hedges in code (`MapView.AddHedge`, leaf texture from `tools/art/hedge_texture.py`). Fence art (`fence_*`, `fence_straight_*`) was generated but the user preferred hedges; it is unused. |

## 6. UI
| Priority | Item |
|---|---|
| P2 | Icons: weapon types (sword, lance, axe, bow, tome, staff, fist), items (vulnerary, boosters, seals), stats |
| Done | Crest emblems (`assets/crests/`), shown in the hub and unit panel; the Clean UI theme and fonts |
| P3 | Title logo; grid cursor and selection arrow art |

## 7. Effects
| Priority | Item |
|---|---|
| P2 | Spell effects for the battle screen and map: Fire, Shadow, Heal |
| P2 | Crest effects: the crest rising from a fallen Binder, Ward / Rally / Mend / Dawn auras, Severance burst |
| P3 | Level-up flourish; hit sparks; Dominion aura on dominated enemies |

## 8. Audio
Done in session 5: all music and sound effects are synthesized by `tools/audio/build.py` (instruments in
`synth.py`, tracks composed in `songs.py`, effects in `sfx.py`) into `assets/audio/music/<id>.ogg` and
`assets/audio/sfx/<id>.wav`, and played by the `AudioDirector` autoload. Replace any file with a hand-made one of the
same id to upgrade it. Ideas for later: per-chapter battle themes, a boss theme distinct from `battle_urgent`, and
footstep / door / menu-open sounds.
