# Art Needed

Everything the game currently shows as a placeholder, and where the game looks for the real file. Nothing here has
been generated yet. Settle the art style first; the ids and paths below don't depend on it.

**How placeholders work today**
- **Map units** without art are capsules in the faction colour.
- **Fights** fall back to on-map animations unless *both* units have battle sprites.
- **Portraits:** characters without one get a coloured square with their initials.
- **Story backgrounds** are tinted gradients labelled `[background: id]`.
- **Music and sound** commands (`@music`, `@sfx`) are parsed but play nothing.

Priority: **P1** is seen in the playable story (Prologue to Ch 3). **P2** is needed for upcoming chapters and promotions. **P3** is polish.

---

## 1. Map sprites (48 px, 4 directions, `idle` `walk` `attack` `hit`)
Folder: `assets/sprites/units/<spriteId>/` (same layout as the existing sets). Units find art through
`BattleUnit.SpriteCandidates()`: `<spriteId>_<classId>` first, then `<spriteId>`.

| Priority | Sprite id | Who / what | Used in |
|---|---|---|---|
| P1 | `aldric_warden` | Aldric, Warden: black Warden's hide with silver sigils, steel sword, ~50, grey-streaked beard | Prologue onward |
| P1 | `tamsin_archer` | Tamsin, Archer: Thornwood hunter, sardonic, leather and furs, hunting bow | Ch 3 onward |
| P1 | `enemy_gloam_hound` | Gloam Hound: fast, lean beast-thing, "out of old fairy tales" | Prologue, Ch 1, Ch 3 (most common enemy) |
| P1 | `enemy_gloam_brute` | Gloam Brute: hulking, heavy claws | Prologue, Ch 1, Ch 3 |
| P1 | `enemy_gloam_stalker` | Gloam Stalker: spindly, throws bone spikes (range 2) | Ch 1, Ch 3 |
| P1 | `enemy_binder` | Binder: cloaked crest-bearer, purple glowing hand, shadow magic | All three Binders (The Binder, Sennet, Varga) |
| P1 | `ally_villager` | Brookhollow villager: plain country clothes (green NPCs) | Ch 1 |
| P2 | `boss_sennet`, `boss_varga` | Distinct looks for the named Binders (set the spawn `SpriteId` once they exist) | Ch 1, Ch 3 |
| P2 | `rowan_crest_knight` | Tier 2 outfit (already specced as `planned` in `tools/art/manifest.py`) | After promotion |
| P2 | `aldric_warden_lord`, `tamsin_sniper`, `brann_warrior`, `kai_paladin`, `mira_sage` | Tier 2 outfits for the classes that now exist | After promotion |
| P3 | Old `rowan`, `kai`, `mira`, `brann`, `elise` sets | Superseded first-pass art; nothing uses them now. Delete. | — |

## 2. Battle-screen sprites (96 px side view, east only, `idle` `attack` `critical` `dodge` `hit`)
Folder: `assets/sprites/battle/<spriteId>/`. The cut-in only plays when **both** fighters have these, so the
Prologue and Ch 1 currently never show it.

| Priority | Sprite ids |
|---|---|
| P1 | `aldric_warden`, `tamsin_archer`, `enemy_gloam_hound`, `enemy_gloam_brute`, `enemy_gloam_stalker`, `enemy_binder` |
| P2 | `ally_villager` (rarely fights), `boss_sennet`, `boss_varga`, and the tier 2 outfits listed above |

## 3. Portraits (96 px; base portrait first, then expressions)
Folder: `assets/portraits/`. The dialogue box looks for `<speaker>_<expression>.png`, then `<speaker>.png`.
Expressions used by the scripts: `neutral smile laugh surprised worried sad angry determined hurt cold`.

| Priority | File | Notes |
|---|---|---|
| P1 | `aldric.png` | Main cast; speaks in nearly every scene |
| P1 | `tamsin.png` | Joins in Ch 3 |
| P1 | `wenna.png` | Elder Wenna (Ch 1, hub intro) |
| P1 | `binder.png`, `sennet.png`, `varga.png` | Boss quotes. `binder` covers the Prologue's unnamed Binder. |
| P1 | `gorran.png` | Ch 2 bandit leader (could reuse `boss_brigand_chief.png` by copying it) |
| P1 | `elise.png` (regenerate) | The current one reads as a modern hoodie; she should wear a worn grey travelling hood |
| P2 | Expression sets for `rowan`, `aldric`, `kai`, `mira`, `brann`, `tamsin`, `osmund` | e.g. `rowan_worried.png`. Without them the base portrait is used for every expression. |

## 4. Story backgrounds (`@bg` ids)
Not wired to files yet. The Story screen would load `assets/backgrounds/<id>.png`; that's a one-line change in
`StoryScreen.SetBackground` once the images exist. Suggested size: 1280×720 or 640×360 pixel art.

| Priority | Ids |
|---|---|
| P1 | `farm_evening`, `farm_night`, `farm_night_smoke`, `village_burning`, `village_dawn`, `farm_morning`, `farmhouse_interior`, `farm_yard`, `village_hall`, `river_road`, `mill_dusk`, `thornwood`, `thornwood_dawn` |
| P2 | Hub backdrop (the Brookhollow village hall, behind the hub menus); title screen illustration |

## 5. Terrain
Folder: `assets/textures/terrain/<terrainId>[_N].png` (32 px top-down; `_N` are random alternates).

| Priority | Tile | Notes |
|---|---|---|
| P1 | `fire.png` | Burning houses in Ch 1 (terrain `fire`, symbol `^`); currently a flat orange block |
| P2 | Separate house / chapel / mill tiles | Villages currently reuse `fort` (T) and `wall` (W) |
| P2 | Block side texture (dirt/stone cliff) | The sides of raised cells are plain darkened colour |
| P3 | Marsh / reeds / shallow water | Needed by Ch 6 (Greyfen) |

## 6. UI
| Priority | Item |
|---|---|
| P2 | A UI theme: panel frames, buttons, a pixel font (screens use a placeholder style in `src/UI/Nodes/UiKit.cs`) |
| P2 | Icons: weapon types (sword, lance, axe, bow, tome, staff, fist), items (vulnerary, boosters, seals), stats |
| P2 | Crest emblems for each tier (Unmarked → Sovereign), shown in the hub and on Rowan's HUD |
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
