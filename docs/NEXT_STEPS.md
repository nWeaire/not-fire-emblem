# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-13 (end of session 1)._

---

## Where things stand

### Built and committed
- **M0–M3** (see `IMPLEMENTATION.md`):
  - Grid, pathfinding and camera.
  - Turns, combat with exact forecasts, and enemy AI.
  - Chapter 1 *test map* ("Brigands at the Ford"), playable start to finish.
- **Battle cut-in screen** (`src/UI/Nodes/BattleSceneView.cs`): a Fire Emblem-style side-view combat screen.
  - It plays when both units have battle art. Until then fights fall back to on-map animations.
  - **B** toggles it in game.
- **Outfit system:** art follows class. `BattleUnit.SpriteCandidates()` tries `<unitId>_<classId>` first, then `<unitId>`.
- **Equipment system** (stats only, independent of looks):
  - weapon tiers and unique (personal) weapons
  - body armour (Light/Medium/Heavy, class limits, weight)
  - accessories
  - regional shops, with data in `data/armor/` and `data/shops/`
  - `EquipmentService` holds the rules. **No shop or equip UI yet** (that is M6).
- **Story canon** in `docs/story/`:
  - `STORY.md`: world, crests, cast, Osmund the advisor, the visual progression, and the plot.
  - `CHAPTERS.md`: every mission and the shop schedule.
  - `DIALOGUE.md`: voices and scripts for the Prologue, Ch 1, the hub interlude and the key scenes.
- **Tests:** 63 passing (`dotnet test tests/NotFireEmblem.Tests`).

### Art (PixelLab)
| Set | Status |
|---|---|
| Map sprites: enemies (6 classes) + Brigand Chief boss | ✅ Done and downloaded |
| Map sprites: new outfits `rowan_youth`, `rowan_lord`, `kai_soldier`, `kai_cavalier`, `mira_acolyte`, `brann_fighter`, `elise_mage` | ✅ Done and downloaded |
| Note | `rowan_youth` also has a spare `breathing-idle` animation (a duplicate idle created during the save crash; already paid for and harmless). |
| Map sprites: old first-pass `rowan`, `kai`, `mira`, `brann`, `elise` | Superseded (old lordly look). Kept only as a fallback for the test map. Safe to delete once the test map uses the new classes. |
| Portraits (7, incl. Osmund) | ❌ Not generated yet |
| Terrain tiles | ❌ Not generated yet |
| Battle sprites (`--set battle`, 14 characters) | ❌ Not started. 4–5 characters were **created on PixelLab but have no animations**; the next run adopts them automatically (no double charge). |

- **Budget:** about **1,609 generations** were left this month at handoff (2,000 per month on Tier 1).
- **Generation is paused** by the file `tools/art/STOP` (gitignored, so it isn't in the repo). It may already be gone on a fresh checkout; recreate it to pause again.

---

## Resuming tomorrow

### 1. Resume the art (≈ 130 generations)
```
del tools\art\STOP                                    # (or rm) allow generation again
python tools/art/generate.py balance                  # check budget
python tools/art/generate.py run                      # map set: generates the 7 portraits + terrain tiles
python tools/art/generate.py run --set battle         # battle-screen sprites for all 14 characters
python tools/art/generate.py download                 # fetch finished art only (never spends)
```
- Runs are resumable: `tools/art/state.json` records everything, so re-running never pays twice.
- PixelLab throttles to a few concurrent jobs, so the battle set takes 1–2 hours. Run it in the background.
- After downloading, open the Godot editor once (or run `godot --headless --path . --import`) so the new PNGs get imported.

### 2. Check the new art in game
- The test map's units already pick up the new outfits through class: `rowan_lord`, `kai_cavalier`, `brann_fighter`, `elise_mage`.
- Mira's test-map class is `priest`, so she still uses the old `mira` art until her class becomes `acolyte`.
- **Map sprites are not wired in yet.** Units are still capsules on the map. Billboard sprites (`AnimatedSprite3D` from `assets/sprites/units/<id>/`) are the next visual task.
- Battle sprites appear automatically in the cut-in screen once downloaded.

### 3. Suggested next development tasks (in order)
1. **Map sprites:** replace the capsule `UnitView` with an `AnimatedSprite3D` billboard. Pick the direction from the facing relative to the camera yaw; use idle/walk/attack/hit.
2. **Story data for the opening:**
   - add the `youth`, `acolyte` and `warden` classes, with bows on `youth` and `lord`
   - add Aldric
   - build the Prologue and Ch 1 maps from `docs/story/CHAPTERS.md`
3. **M4** (objectives, events, fog, dialogue), using the scripts in `docs/story/DIALOGUE.md`. The story also needs these mechanics:
   - the Talk and Visit commands
   - Defend/Protect objectives
   - crest Dominion and Severance
4. **M5/M6:** progression, crest tiers, the hub (Osmund, shop/equip screens, smithy) and saves.

---

## Gotchas learned this session
- **OneDrive locks files** briefly while syncing. `state.json` writes now retry atomically. If anything else fails with `OSError 22` / "Access is denied" on a write, retry.
- **Bash heredocs** containing certain quote patterns fail in this environment. Write Python patch scripts to files instead.
- **Godot `SetAnchorsPreset`** keeps old offsets; use `SetAnchorsAndOffsetsPreset` for code-built UI.
- **PixelLab detail values** for pixen are `low detail`, `medium detail` and `highly detailed` (not "high detail").
- **The data `.tres` files are canonical.** Edit them directly or with small scripts. The one-off generators used to create them were throwaway.
- **Headless checks:** `godot --headless --path . --quit-after 200` runs the game. The `DevTools` flags (`--screenshot`, `--press`, `--seed`, `--speed`) allow scripted visual checks; see `CLAUDE.md` §4.
