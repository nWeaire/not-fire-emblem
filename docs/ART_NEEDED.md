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
- **Music and sound** commands (`@music`, `@sfx`) are parsed but play nothing.

Priority: **P1** is seen in the playable story (Prologue to Ch 3). **P2** is needed for upcoming chapters and promotions. **P3** is polish.

**Done in the GBA style (session 5):** map + battle sprites for `rowan_youth`, `rowan_lord`, `kai_soldier`,
`mira_acolyte`, `brann_fighter`, `aldric_warden`, `tamsin_archer`, `enemy_gloam_hound`, `enemy_gloam_brute`,
`enemy_gloam_stalker`, `enemy_binder`, and the `ally_villager` map sprite. Portraits `aldric`, `tamsin`, `wenna`,
`binder`, `sennet`, `varga`, `elise` (other takes in `tools/art/takes/portraits/`). Backgrounds `farm`, `village`,
`farmhouse_interior`, `village_hall`, `river_road`, `mill`, `thornwood`, `forest`, `plains`. The 32 px terrain set and
`fire` tiles. Clean UI panel, name plate and fonts.

---

## 1. Map sprites (48 px, 4 directions, `walk` `attack`; idle falls back to the still rotation)
Folder: `assets/sprites/units/<spriteId>/`. Units find art through `BattleUnit.SpriteCandidates()`:
`<spriteId>_<classId>` first, then `<spriteId>`.

| Priority | Sprite id | Who / what | Used in |
|---|---|---|---|
| P2 | `enemy_pegasus_knight` | Still session-2 art (map and battle); a flying mount | Test map |
| P2 | `boss_sennet`, `boss_varga`, `boss_gorran` | Distinct looks for named bosses (set the spawn `SpriteId`) | Ch 1–3 |
| P2 | `elise_mage`, `kai_cavalier` | Session-2 art (map and battle); regenerate in the current styles. Elise shows in Battle Select's default party | Ch 4+, promotion |
| P2 | `aldric_warden_lord`, `tamsin_sniper`, `brann_warrior`, `kai_paladin`, `mira_sage`, `rowan_crest_knight` | Tier 2 outfits (`PLANNED` in `tools/art/manifest.py`) | After promotion |

## 2. Battle-screen sprites (the "E" look: 84 px stills animated on 128×84, east only, `idle` `attack` `critical` `dodge` `hit`)
Folder: `assets/sprites/battle/<spriteId>/`. Same ids as §1; `ally_villager` has none (rarely fights).

## 3. Portraits (96 px; base portrait first, then expressions)
Folder: `assets/portraits/`. The dialogue box looks for `<speaker>_<expression>.png`, then `<speaker>.png`.
Expressions used by the scripts: `neutral smile laugh surprised worried sad angry determined hurt cold`.

| Priority | File | Notes |
|---|---|---|
| P1 | `gorran.png` | Ch 2 bandit leader (could reuse `boss_brigand_chief.png`) |
| P2 | `rowan`, `kai`, `mira`, `brann`, `osmund`, `boss_brigand_chief` | Session-2 portraits; check against the GBA style, regenerate if they clash |
| P2 | Expression sets for the main cast | e.g. `rowan_worried.png`. Without them the base portrait is used for every expression. |

## 4. Story backgrounds (`assets/backgrounds/<id>.png`)
| Priority | Ids |
|---|---|
| P2 | Hub backdrop (behind the hub menus); title screen illustration; locations for Ch 4+ |

## 5. Terrain
Folder: `assets/textures/terrain/<terrainId>[_N].png` (32 px top-down; `_N` are random alternates).

| Priority | Tile | Notes |
|---|---|---|
| P2 | House / chapel roofs | Generated (`tools/art/takes/tiles/village/` tiles 1/5/9/13 and 2/6/10/14) but need new terrain types; villages reuse `fort` (T) and `wall` (W) |
| P2 | Block side texture | Generated (cliff, tiles 3/7/11/15); needs code to texture the sides of raised cells |
| P3 | Marsh / reeds / shallow water | Needed by Ch 6 (Greyfen) |

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

## 8. Audio (not art, but also placeholders)
Ids used by the scripts. No audio player is wired yet.
- **Music:** `calm_farm`, `tension_low`, `tension_high`, `battle_urgent`, `aftermath`, `march`, `forest_tension`, plus player/enemy phase battle themes.
- **Sounds:** `gloam_howl`, `gloam_roar`, `floorboards`, `crest_rise`, `crest_burn`, `fire_cast`, `bow_shot`, plus UI clicks, hits, crits and misses.
