# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-14 (end of session 3)._

---

## Where things stand

**The game is playable end to end with placeholder art:** Title → Prologue → Ch 1 → hub interlude → Ch 2 → hub → Ch 3 → "To be continued". Milestones M0–M6 from `IMPLEMENTATION.md` are built (see the *As built* notes there). Art still missing is listed in **`docs/ART_NEEDED.md`**. Session 3 added no new art.

### Systems (session 3)
- **Map rules (M4):**
  - **Objectives:** Rout, Defeat Boss, Seize, Survive, Escape. **Loss conditions:** lord dies, `die KEY`, `protect TAG N` (Defend), turn limits.
  - **Triggers:** turn, defeated, enter region, adjacent, flag, enemy count, HP, talk, visit, and a pre-combat boss quote. **Effects:** scene, spawn group, flag, join/faction, give item, gold, terrain change, damage, win/lose. Maps declare them as text lines (grammar on `MapScript` in `src/Battle/MapRules.cs`).
  - **Actions:** Talk, Visit, Seize, Escape.
  - **Fog of war:** logic, rendering and AI filtering.
  - **Crest Dominion and Severance:** +2 Str/Def near the bearer. When the bearer dies, beasts are stunned and humans surrender.
  - **Reinforcement groups** and Hard-only spawns.
- **Dialogue:** scripts in the DIALOGUE.md format (`data/dialogue/*.txt`, 50 scenes) and a dialogue box with portraits (placeholder initials when there's no portrait), typewriter text, camera `@focus`, and skip.
- **Progression and abilities (M5):**
  - FE7-style EXP, level-ups with a popup, combat arts (uses per map, no-counter, range, effectiveness vs Beast), healing spells and Healer AI, items (vulneraries, stat boosters, seals), and Trade.
  - Crest skills (Ward, Rally, Mend, Dawn) and Rowan's crest aura.
  - Promotion gated by level 10, a seal and crest tier.
  - Supports: points from fighting side by side and from surviving battles together, ranks C/B/A, combat bonuses, 15 support conversations.
- **Campaign (M6):**
  - `GameSession`, a campaign script (`data/campaign/main.txt`), Classic/Casual and Normal/Hard.
  - Screens: Title (New Game, Continue, Load, Battle Select), Story, Prep (deployment), Results, and the hub with Osmund (Party/equip/convoy, Shop, Smithy, Promotion, Supports, Save).
  - JSON saves in `user://saves/`, with an autosave whenever you reach the hub.
  - Battles use copies of roster units, so a lost battle can be retried cleanly. Each retry uses a new seed.
- **Content:**
  - Maps: Prologue, Ch 1, and new Ch 2 (Seize the mill, recruit Brann by Talk, two village Visits) and Ch 3 (fog, Defeat Varga, recruit Tamsin by Talk). Plus the old `test_ford` skirmish.
  - New data: Tamsin; 12 items, 8 arts, 3 spells; tier 2 classes Crest Knight, Sniper and Warden Lord; a fire terrain.
  - Ch 2 and Ch 3 dialogue are new writing, in the documented voices. The Prologue, Ch 1 and hub scripts are canon from DIALOGUE.md.
- **Dev flags:** `--autoplay` (the AI plays your side), `--skipdialogue`, `--autoadvance`, `--newgame`, `--load=N`. See CLAUDE.md §4. A full autoplay run of the campaign finished with zero errors.
- **Tests:** 101 passing (map rules, progression, campaign and saves, and checks that the real dialogue and campaign files parse and reference existing scenes).

### Balance changes from autoplay runs
- Prologue: Gloam Brute Str 9→8 and Claws Mt 5→4; the Binder is level 2.
- Ch 1: villagers are sturdier (20 HP, Def 3), and the enemies that started next to Mira's chapel moved west of the river.
- Ch 2: the enemies that started next to Brann's forge moved away, so he can't die before you reach him.
- These are first passes. Nothing has been balanced by a human playthrough yet.

### Known gaps
- **Not built:** crest fragments / crest EXP, suspend saves, a settings menu, audio playback (`@music`/`@sfx` are parsed and ignored), and story background images (`@bg` shows a tinted gradient).
- **Autoplay** never uses Talk, so automated runs skip optional recruits. Talk is covered by unit tests.
- **Ch 2's "Brann pressed" event** fires on turn 3 even if he has already joined.
- **Exported builds** need `*.txt` in the export filter so `data/dialogue/` and `data/campaign/` ship.

### Art (PixelLab)
- **Done:** map sprites, battle sprites, 7 portraits and terrain tiles for the original cast and test enemies (session 2).
- **Needed:** everything else; see `docs/ART_NEEDED.md`. P1 items: Aldric, Tamsin, the Gloamkin, the Binder, villagers, the missing portraits, and story backgrounds.
- **Budget:** 1,407 generations were left at the end of session 2. Pause generation with `tools/art/STOP`.

---

## Next development tasks (in order)
1. **Playtest the campaign by hand** and tune balance (enemy levels, villager durability, the Ch 3 fog pace).
2. **Art pass** once the style is settled: work through `docs/ART_NEEDED.md` P1, then switch `BattleController.MapId` (the scene default) to `prologue` if wanted.
3. **More chapters** from `docs/story/CHAPTERS.md`, starting with Ch 4 (Escape with Elise), Ch 5 (Survive at Ferrow's Keep) and Ch 6 (marsh terrain, Rusk's Dominion). The systems they need already exist.
4. **Polish backlog:** crest fragments, audio, settings, a UI theme, and suspend saves.

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
- **Autoplay soak runs** (`--newgame --autoplay --skipdialogue --autoadvance --speed=10`) are the quickest way to catch runtime errors across the campaign. A map that can't be won prints `AUTOPLAY STUCK` and quits.
- **Story maps with only deploy slots** (Ch 2, Ch 3) need a roster. Battle Select gives them a default party (`BattleController.SkirmishParty`).
