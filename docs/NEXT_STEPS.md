# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-14 (end of session 2)._

---

## Where things stand

### Built and committed
- **M0–M3** (see `IMPLEMENTATION.md`): grid, pathfinding and camera; turns, combat with exact forecasts, and enemy AI.
- **Battle cut-in screen** (`src/UI/Nodes/BattleSceneView.cs`): plays when both units have battle art, otherwise fights use on-map animations. **B** toggles it.
- **Outfit system:** art follows class. `BattleUnit.SpriteCandidates()` tries `<unitId>_<classId>` first, then `<unitId>`.
- **Equipment system** (stats only): weapon tiers, unique weapons, body armour, accessories, regional shops. `EquipmentService` holds the rules. No shop/equip UI yet (M6).
- **Story canon** in `docs/story/` (`STORY.md`, `CHAPTERS.md`, `DIALOGUE.md`).

### Session 2
- **Map sprites:** `UnitView` is now an `AnimatedSprite3D` billboard built by `SpriteLibrary.Map()` from `assets/sprites/units/<id>/`.
  - The frame direction (south/east/north/west) comes from the unit's facing relative to the camera, so rotating the camera turns the art.
  - Animations: idle (default), walk (moving), attack (lunge), hit (taking damage, plus a red flash). Done units are tinted grey.
  - Units without map art (Aldric, Gloamkin, villagers) still use the old faction-coloured capsule.
- **Opening story data:**
  - New classes: `youth`, `warden`, `acolyte`, `villager`, `gloam_hound`, `gloam_brute`, `gloam_stalker`, `binder`. Youth and Lord both use Sword + Bow.
  - New unit: `aldric` (Warden Lv 5, Steel Sword, Warden's Hide).
  - New weapons: `short_bow`, `shadow`, `gloam_fangs`, `gloam_claws`, `bone_spikes`.
  - Rowan now starts as `youth`, Kai as `soldier` and Mira as `acolyte` (with Fire).
- **Maps:**
  - `prologue` (now the default battle) and story `ch1`, both built from `CHAPTERS.md`.
  - The old test map is now `test_ford`. Choose a map with the `--map=<id>` dev flag.
  - Both story maps use **Rout** until M4. Kai and the villagers are Ally units on Stationary AI; the Ally phase already works.
- **Art tool fix:** `generate.py` no longer crashes when an old PixelLab job has expired (404). It polls the character instead.
- **Tests:** 63 passing.

### Art (PixelLab)
| Set | Status |
|---|---|
| Map sprites: 7 outfits + 6 enemy classes + Brigand Chief | ✅ Done, and now shown in game |
| Old first-pass `rowan`, `kai`, `mira`, `brann`, `elise` map sprites | Superseded. Nothing uses them now that all five have class outfits (`brann_fighter`, `elise_mage` on the test map). Safe to delete. |
| Portraits (7, incl. Osmund) | ✅ Done (`assets/portraits/`). **Not used in game yet** (needed by M4 dialogue). Elise's portrait reads as a modern hoodie; consider regenerating. |
| Terrain tiles | ✅ Done (`assets/textures/terrain/`). **Not applied** to `MapView` yet. |
| Battle sprites (`--set battle`, 14 characters) | ✅ Done: all 14 with idle, attack, critical, dodge and hit; the cut-in screen uses them. The Prologue and Ch 1 enemies have no battle art yet, so fights there stay on the map. |
| Aldric, Gloamkin (Hound, Brute, Stalker), Binder, villagers | ❌ Not in `tools/art/manifest.py` yet. Add specs, then `run --only <id>`. |

- **Budget:** 1,407 generations left this month (2,000 per month on Tier 1). Session 2 spent 202: 17 on portraits and 185 on the battle set.
- Pause generation by creating `tools/art/STOP` (gitignored).

---

## Next development tasks (in order)
1. **Art wiring:**
   - apply the terrain textures in `MapView`
   - add manifest entries and generate Aldric, the Gloamkin, the Binder and villagers (map + battle sets)
2. **M4** (objectives, events, fog, dialogue), using `docs/story/DIALOGUE.md`. The Prologue and Ch 1 need:
   - objectives: Defeat Boss, Defend (villagers), extra loss units (Aldric in the Prologue)
   - `EventDirector` triggers: Aldric's T2 reveal, Kai switching from Ally to Player when Rowan is adjacent, Mira gaining Fire on T3 (then remove Fire from `mira.tres`), T4 fire tiles clearing and reinforcements
   - the Talk and Visit commands (Elder Wenna's house), and dialogue scenes with the new portraits
   - crest Dominion and Severance
3. **M5/M6:** progression, crest tiers, the hub (Osmund, shop/equip screens, smithy) and saves.

---

## Gotchas learned so far
- **OneDrive locks files** briefly while syncing. `state.json` writes now retry atomically. If anything else fails with `OSError 22` / "Access is denied" on a write, retry.
- **Bash heredocs** containing certain quote patterns fail in this environment. Write Python patch scripts to files instead.
- **Godot `SetAnchorsPreset`** keeps old offsets; use `SetAnchorsAndOffsetsPreset` for code-built UI.
- **PixelLab detail values** for pixen are `low detail`, `medium detail` and `highly detailed` (not "high detail").
- **The data `.tres` files are canonical.** Edit them directly or with small scripts. The one-off generators used to create them were throwaway.
- **Headless checks:** `godot --headless --path . --quit-after 200` runs the game. The `DevTools` flags (`--screenshot`, `--press`, `--seed`, `--speed`) allow scripted visual checks; see `CLAUDE.md` §4.
- **PixelLab job ids expire** after about a day (`GET /background-jobs/<id>` returns 404). Characters stay; poll `/characters/<id>` instead.
- **Python output is buffered** when redirected to a file. Use `python -u` for background runs so the log updates live.
- **`git mv -f` after a script has already moved a file** overwrites the destination. Check `git status` before moving files by hand.
