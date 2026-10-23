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

**Done (session 12, user's picks):** portraits `pip` #1, `seraphine` #2, `ysra` #3, `halde` #1, `morvane` #1,
`castellan` #1, `vossk` #3, `ysolde` #3 (the bottom light bar cleared); map + battle sprites for `pip_thief`,
`seraphine_troubadour`, `ysra_wyvern_rider`, `halde_archer`, `ysolde_wyvern_lord`, `enemy_wyvern_rider`, `boss_castellan`,
`boss_vossk`, `boss_morvane`, `enemy_hollowed`, `enemy_thief` (still picks in `approved/battle_picks.txt`; baked-in
effects erased or dropped by hand, Halde's attack/critical redone so she keeps her crossbow); backgrounds
`calder_square`, `calder_streets`, `calder_palace`, `calder_lodgings`, `calder_undercity`, `border_pass`, `dravenhold_pass`,
`solmere_harbor`, `solmere_palazzo`, `dravenhold_citadel` (take 0 installed; other takes in `takes/backgrounds/`). The rows
below that these cover are done. Not yet checked in game: the wyverns and Castellan fill their 48 px map frames and
may want `SpriteLibrary.MapScales` like the Gloam Shade.

| Priority | Id | Who / what | Stand-in today | Used in |
|---|---|---|---|---|
| P1 | `enemy_veiled_agitator` (map + battle; or `_<classId>` per class) | Veiled agent dressed as a townsman: plain grey cloak, purple sigil glowing on the wrist, sword/tome/axe variants | the class look `enemy_<classId>` | Ch 8 |
| P1 | `enemy_veiled_thug` (map + battle; or `enemy_veiled_thug_<classId>` per class) | Veiled street muscle: grey hooded cloak over leather, purple-trimmed | the class look `enemy_<classId>` | Ch 8, Ch 9 |
| P1 | `neutral_rioter` (map + battle) | Calder townsman with a cudgel and a torch, angry; amber outline in game | `ally_villager` (`BattleUnit.PlaceholderSprites`) | Ch 8 |
| P1 | `ally_townsfolk` (map) | Calder townsfolk in city clothes (Calder blue scarves, aprons) | `ally_villager` (a farmer) | Ch 8 |
| P1 | Portrait `morvane` | The Grey Chancellor: gentle, grey-robed, grey-haired, kind eyes (STORY §5) | initials | Ch 8 on |
| P1 | Backgrounds `calder_square`, `calder_streets`, `calder_palace` | The fountain square and market; a Calder street; the palace audience hall (Calder blue) | `town_overview`, `town_overview`, `dain_hall` (`UiKit.PlaceholderBackgrounds`) | Ch 8 |
| P1 | `pip` / `pip_thief` (map + battle), portrait `pip` | Pip: 15, scrawny, cheeky grin, oversized patched coat, bare feet, a short sword he "found" | the class look (`enemy_thief` → `enemy_myrmidon`); initials | Ch 9 on |
| P1 | `enemy_thief` (map + battle) | Undercity cutpurse: hooded, lean, knife-and-short-sword, dark rags | `enemy_myrmidon` (`BattleUnit.PlaceholderSprites`) | Ch 9 |
| P1 | Prop `chest_N.png` | A treasure chest on a stone floor (32 px billboard like the crates) | `crates` prop | Ch 9 |
| P1 | Background `calder_undercity` | Lantern-lit sewers and cellars under Calder | `dain_hall` | Ch 9 |
| P1 | `seraphine_troubadour` (map + battle), portrait `seraphine` | Princess Seraphine: 17, fair hair in a travel braid, Calder-blue riding coat, healer's staff, grey horse; a small pale crest on the back of her hand | the class look (`enemy_troubadour` → `enemy_cavalier`) | Ch 10 on |
| P1 | `enemy_imperial` (or `enemy_imperial_<classId>`: soldier, knight, archer, cavalier, mage) | Dravenhold imperial troops: dark iron, grey-white tabards with a red wolf, fur at the collar | the class looks `enemy_<classId>` | Ch 10, 11, 14 |
| P1 | `enemy_wyvern_rider` (map + battle) | Grey-scaled wyvern, rider in dark iron with a lance; the Dravenhold wyvern corps | `enemy_pegasus_knight` | Ch 10, 11 |
| P1 | `ally_calder_knight` (map) | Calder royal escort: steel plate with Calder-blue tabard | `enemy_knight` (green ring) | Ch 10 |
| P1 | Background `border_pass` | A mountain pass over a burning border village | `hill_road` | Ch 10 |
| P1 | `ysra_wyvern_rider` (map + battle), portrait `ysra` | Ysra Thane: dark braid, scarred cheek, black iron and fur, on her grey wyvern Ash | `enemy_wyvern_rider` → `enemy_pegasus_knight` | Ch 11 on |
| P1 | `halde_archer` (map + battle), portrait `halde` | Captain Halde Marr: weathered privateer, red coat, tricorn, crossbow | `enemy_archer` | Ch 12 on |
| P1 | `enemy_gilded` (or per class) | Castellan's house guard: gilded half-plate, white and gold, sun crest | the class looks | Ch 12, 13 |
| P1 | `ally_sailor` (map) | The Gull's deckhands: striped shirts, axes | `enemy_fighter` | Ch 12 |
| P1 | Backgrounds `dravenhold_pass`, `solmere_harbor` | A snowy wyvern-roost pass; a gilded harbour with many masts | `hill_road`, `river_road` | Ch 11, 12 |
| P1 | `boss_castellan` (map + battle), portrait `castellan` | Prince Castellan: heavy, silver-plated, rings on every finger, three purple crests (both hands, throat) | `enemy_general` → `enemy_knight` | Ch 13 |
| P1 | Background `solmere_palazzo` | A gilded great hall with mirrors; gardens on cliffs above the sea | `dain_hall` | Ch 13 |
| P1 | Tier 2: `dorran_general`, `sable_swordmaster`, `wren_falcon_knight` (map + battle) | The classes added in session 12 | their tier 1 looks (`Outfits.Earlier`) | after promotion |
| P2 | Tier 3 Crestbound outfits: `rowan_high_lord`, `kai_vanguard`, `mira_oracle`, `elise_crestscribe`, `brann_ironheart`, `tamsin_wildstalker`, `halde_corsair`, `dorran_bulwark`, `sable_blade_dancer`, `wren_skyward`, `pip_shadowstep`, `seraphine_lightbearer`, `ysra_dragonheart` | Grander again than tier 2 (ART_DIRECTION, outfits follow class tier) | their tier 2 looks (`Outfits.Earlier`) | Ch 13 on |
| P1 | `boss_vossk` (map + battle), portraits `vossk`, `ysolde` | Vossk: a grey-robed, hollow-cheeked man with crests crawling over both hands; Ysolde: a tall empress in black iron and white fur, silver lance, on a black wyvern | `enemy_binder`; Ysolde `enemy_wyvern_lord` → `enemy_pegasus_knight` | Ch 14 |
| P1 | `enemy_veiled` (mage) | Veiled caster in grey and purple robes | `enemy_mage` | Ch 14 |
| P1 | Snow terrain tiles (plain, road, forest in snow) and a `dravenhold_citadel` background | Ch 14's citadel; the map uses `Lighting = "snow"` (a cold wash and snowfall) until then | `dain_hall` backdrop | Ch 14 |
| P1 | `boss_morvane` (map + battle), portrait `morvane` (also listed above) | The Grey Chancellor in his true light: grey robes, gentle face, a hundred crests faint under his skin | `enemy_binder` | Ch 15 |
| P1 | `enemy_hollowed` (map + battle) | The Hollowed: soldiers grey as ash, eyes empty, armour hanging off them, any blade they found | `enemy_soldier` | Ch 15, Act III |
| P2 | Background `hall_ruin` | Calder's great hall after the battle: overturned tables, a broken door | `dain_hall` | Ch 15 |
| P2 | Ship hull props | Hull sides under the ship decks in Ch 12 (decks are bridge cells today) | — | Ch 12 |
| P2 | `seraphine_valkyrie`, `ysra_wyvern_lord` | Tier-2 looks | — | after promotion |
| P2 | `pip_rogue` (map + battle) | Pip promoted: dark leathers, a proper sword, a grin | — | after promotion |
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
