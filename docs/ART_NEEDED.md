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

## 0. Act II (session 12 on): placeholders in use, art to make when credits come
Act II is being built systems-first. Every unit, portrait and backdrop below already works with a stand-in; the
stand-in is named so the real art can drop in under the same id (or the spawn's `SpriteId` can be pointed at it).
Rough cost per full character (map sprite + walk/attack, battle still takes + 5 animations): about $0.45.

| Priority | Id | Who / what | Stand-in today | Used in |
|---|---|---|---|---|
| P1 | `enemy_veiled_agitator` (map + battle) | Veiled agent dressed as a townsman: plain grey cloak, purple sigil glowing on the wrist, sword/tome/axe variants | `enemy_myrmidon` / `enemy_mage` / `enemy_fighter` | Ch 8 |
| P1 | `enemy_veiled_thug` (map + battle) | Veiled street muscle: grey hooded cloak over leather, purple-trimmed | bandit `enemy_*` looks | Ch 8, Ch 9 |
| P1 | `neutral_rioter` (map + battle) | Calder townsman with a cudgel and a torch, angry; amber outline in game | `ally_villager` | Ch 8 |
| P1 | `ally_townsfolk` (map) | Calder townsfolk in city clothes (Calder blue scarves, aprons) | `ally_villager` (a farmer) | Ch 8 |
| P1 | Portrait `morvane` | The Grey Chancellor: gentle, grey-robed, grey-haired, kind eyes (STORY §5) | initials | Ch 8 on |
| P1 | Backgrounds `calder_square`, `calder_streets`, `calder_palace` | The fountain square and market; a Calder street; the palace audience hall (Calder blue) | `town_overview`, `town_overview`, `dain_hall` (`UiKit.PlaceholderBackgrounds`) | Ch 8 |
| P2 | `calder_lodgings` background | The party's rooms in the capital (Act II hub) | `village_hall` | Act II hub |

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

## 3. Portraits (96 px, neutral only)
Folder: `assets/portraits/`. The dialogue box shows `<speaker>.png` for every line. Expression portraits were removed in
session 11 at the user's request: don't generate new ones.

| Priority | File | Notes |
|---|---|---|
| Done (session 7) | `brann`, `osmund` | Regenerated in the cast's style (picks: brann take 1, osmund take 0; others in `tools/art/takes/portraits/`). `rowan`, `kai`, `mira` and `boss_brigand_chief` were checked and kept: Rowan's is the style reference and the others match it |
| Done (session 7) | Expressions for the main cast | `tools/art/expressions.py` (edit_images-v2, one request per expression): Rowan determined/worried/surprised/smile, Aldric cold/determined/sad, Kai surprised/smile/laugh/determined, Mira smile/worried, Brann smile/surprised, Elise worried/surprised, Tamsin cold. Rarer ones fall back to the nearest (laugh→smile, angry→cold, hurt/sad/surprised→worried), then the base. Others (Dorran, Wenna, Sable, bosses) could follow. |

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
| Done (session 10) | **Stone wall sides** | Raised wall, square, throne and fort blocks (outside farm maps) show masonry instead of earth: `assets/textures/wall_side.png` / `_1` (dressed and mossy stone), plus `assets/textures/wall_decor/` overlays on about one exposed wall face in four (arrow slits, a barred window, green and red banners). All drawn in code by `tools/art/wall_art.py` (`MapView.StoneSideMaterial`, `AddWallDecor`). |
| Done (session 11) | **Castle gate** | Ch 5's east gate (terrain `G`): 3D oak doors along the wall's line with stone posts (`MapView.AddGate`), textures in `assets/textures/gate/` drawn by `tools/art/gate_textures.py`. Replaced session 10's camera-facing prop. |

## 6. UI
| Priority | Item |
|---|---|
| P2 | Icons: weapon types (sword, lance, axe, bow, tome, staff, fist), items (vulnerary, boosters, seals), stats |
| Done | Crest emblems (`assets/crests/`), shown in the hub and unit panel; the Clean UI theme and fonts |
| P3 | Title logo; grid cursor and selection arrow art |

## 7. Effects
| Priority | Item |
|---|---|
| Done (session 7, in code) | Spell effects: Fire (fireball + embers), Shadow (orbs gather and collapse), Wind, Thunder (bolt), Light; Heal sparkles on the map (`BattleSceneView.CastSpell`, `BattleEventPlayer.HealSparkles`). Pixel-art effect sprites could replace them later. |
| P2 | Crest effects: the crest rising from a fallen Binder, Ward / Rally / Mend / Dawn auras, Severance burst |
| P3 | Level-up flourish; hit sparks; Dominion aura on dominated enemies |

## 8. Audio
Done in session 5: all music and sound effects are synthesized by `tools/audio/build.py` (instruments in
`synth.py`, tracks composed in `songs.py`, effects in `sfx.py`) into `assets/audio/music/<id>.ogg` and
`assets/audio/sfx/<id>.wav`, and played by the `AudioDirector` autoload. Replace any file with a hand-made one of the
same id to upgrade it. Ideas for later: per-chapter battle themes, a boss theme distinct from `battle_urgent`, and
footstep / door / menu-open sounds.
