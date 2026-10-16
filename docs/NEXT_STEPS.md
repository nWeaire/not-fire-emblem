# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-16 (end of session 5)._

---

## Session 5: the art pass, steps 1-4 done (read this first)

- **Crest bearers confirmed** (ART_DIRECTION §5, `CrestEmblem.Bearers`). `src/UI/Nodes/CrestEmblem.cs` draws a crest
  with its glow; Rowan's shows in the hub and on his unit panel, and enemy bearers show theirs.
- **Pipeline switched** to the GBA style: `generate.py` commands `chars | portraits | backgrounds | tiles | download`
  (see CLAUDE.md §5). State lives under `"gba"` in `state.json`. Session-2 art is parked in `tools/art/superseded/`.
- **New art in the game:** 48 px map and 64 px battle sprites with animations for Rowan (youth), Kai (soldier), Mira
  (acolyte), Aldric, Tamsin, the Gloam Brute, Hound and Stalker, and the Binder; a map sprite for the villager; the
  16 px terrain set; `forest` and `plains` battle/story backgrounds; Pixelify Sans and Silkscreen fonts; UI panel art.
- **Engine:** `BattleSceneView` loads background pictures, handles 64 px sprites and plays the §4 effects (dim,
  CRITICAL!, lunge, white-out, burst, shake, flash, knockback, damage pop). The Clean UI theme is in `UiKit` and is
  applied by the `UiThemeSetup` autoload. `StoryScreen` shows `@bg` pictures, and variants like `farm_night_smoke`
  fall back to `farm` with a tint. Map units bob a pixel while idle. Battle idles play as a short forward-back loop.
  `DevTools --shotevery=N` saves a screenshot every N frames.
- **Fix:** trigger guards (`... unless joined KEY | unless flag NAME => ...`). Ch 2's "Brann pressed" no longer fires
  after he joins.
- **Verified:** build clean, 102 tests pass, and a full autoplay campaign completes with no errors.
- **Budget:** 349 PixelLab generations left (as of 2026-10-16).

### Art generated at the end of session 5
- `rowan_lord` and `brann_fighter` (map + battle), 7 portraits, 7 story backgrounds, and the village tile set (218
  gens; **131 left**). Recommended portrait takes are installed (aldric #0, tamsin #2, wenna #0, binder #2,
  sennet #0, varga #0, elise #2), with the light bar some takes have along the bottom edge removed. The user can swap
  them from `tools/art/takes/portraits/`. Fire tiles are installed; roof and cliff tiles wait for code (ART_NEEDED §5).
- Fixed the dialogue name plate stretching to the bottom of the screen.

### Later in session 5 (user review)
- **Portraits:** the user's picks are installed (elise #3, aldric #0, tamsin #2, wenna #3, binder #1, sennet #3,
  varga #2, counting the sheet's #0–#3 labels).
- **Terrain is now 32 px** (`approved/terrain_gba32/`; the 16 px set stays in `approved/terrain_gba16/`).
- **Rowan's battle sprite** is the user's pick "E" (brighter, more GBA FE): `approved/refs/rowan_battle_E1.png`,
  animated with the new `tools/art/animate_still.py`. Pilots and review sheets are in `tools/art/takes/pilot5/` and
  `tools/art/takes/review/` (not committed).
- Wasted gens worth knowing about: 16 px style images force 16 px tiles (20 gens), `create_from_concept` made a chibi
  (16), and a "probe" request with an invalid `template_id` was accepted and charged (10). Read the API schema at
  `https://api.pixellab.ai/v2/openapi.json` instead of probing.
- Budget: the user bought more credits (about 2,700 generations as of 2026-10-16).

- **The whole battle cast is now in the "E" look** (Rowan, Rowan Lord, Kai, Mira, Aldric, Tamsin, Brann, Gloam Hound,
  Brute and Stalker, Binder). Stills: `tools/art/battle_stills.py` (takes in `takes/stills/`, picks in
  `approved/battle_picks.txt` and `approved/refs/<id>_battle.png`); animations: `tools/art/animate_still.py`. Every animation
  was checked frame by frame; effect frames were filtered out and Brann's dodge, Aldric's hit and Rowan's
  attack/hit/dodge were redone. The 64 px session-4 battle art is parked in `tools/art/superseded/battle_s4/`.
- Map sprites are unchanged (48 px session-4 look).
- **Bandits and human enemies restyled** (Fighter, Archer, Myrmidon, Gorran/`boss_brigand_chief`, Soldier, Mage): new
  48 px map sprites from `generate.py` and E-look battle sprites. Session-2 art parked in `tools/art/superseded/*_s2/`.
  New `anima` (fire) weapon animations in the manifest. The effect filter only catches white or growing effects:
  orange sparks and thin slashes still need checking by eye (the Mage's and Myrmidon's hits were cleaned by hand).

### Next
1. User review of the new battle cast in game; swap any still pick for ~7 gens (`animate_still.py`).
2. Remaining art in `docs/ART_NEEDED.md`: the Pegasus Knight (test map only) is the last session-2 unit. Elise and
   Kai's Cavalier (`template="horse"` for the map sprite) are restyled, and `gorran.png` is a copy of the brigand-chief
   portrait (his sprite id).
3. Switch `BattleController.MapId` to `prologue` if wanted, and playtest by hand.

## Session 4: art direction settled

Session 4 changed no game code. It settled the art style with the user through three review rounds of PixelLab
samples and mockups. Everything decided is in **`docs/ART_DIRECTION.md`**, and the approved samples are in
`tools/art/approved/`.

- **Look:** GBA Fire Emblem pixel art. Characters match the two fighters painted into the approved GBA forest
  background: small, chunky and earthy. Map sprites are 48 px and battle sprites 64 px side view.
- **Terrain:** 16 px GBA tiles. **Battle backgrounds:** GBA level (240×135). **UI:** the Clean style (charcoal panels,
  thin light borders) with the Pixelify Sans font.
- **Portraits:** Rowan take #1 and brigand chief take #4 set the style.
- **Battle drama** comes from screen effects: dim, CRITICAL! banner, lunge-to-contact, white-out, impact burst,
  shake, hit flash, knockback and damage pop. The spec is in ART_DIRECTION §4. Hits must land at the weapon's
  contact point on the target's body.
- **Crests are done as assets:** `assets/crests/` holds Rowan's four tiers and an 18-motif library (4 tiers each), in
  Radiant white and Shrouded purple. Bearer assignments are a suggestion awaiting the user's confirmation
  (ART_DIRECTION §5).
- **Approved samples, not yet in the game:** Rowan, Kai, Mira, Gloam Brute and Binder (map and battle) plus four
  attack animations. The game still uses the session-2 art.
- **Budget:** 564 PixelLab generations left this month (843 were used in session 4).

### Next: the art pass (in order)
1. **Confirm with the user:** the crest bearer list, and whether the crests are ready to use.
2. **Pipeline:** switch `tools/art/generate.py` and `manifest.py` to the approved style. That means
   `create-character-pro` with `approved/refs/gba_clean_fighters.png`, 48 px map and 64 px battle sprites, and the
   "keeps facing right" animation suffix. Add sets for 16 px terrain, GBA backgrounds and portraits.
3. **Promote the approved samples** into `assets/sprites/units|battle/` (`rowan_youth`, `kai_soldier`,
   `mira_acolyte`, `enemy_gloam_brute`, `enemy_binder`), and add the missing animations (idle/walk/hit/dodge) to them
   using the character ids in `approved/pixellab_state/round3_characters.json`.
4. **Engine work:** load battle background images and handle 64 px battle sprites in `BattleSceneView`, build the
   §4 effects there, build the Clean UI theme in `src/UI/Nodes/UiKit.cs` with the font, show the crest on Rowan's HUD
   and in the hub, and swap in the 16 px terrain set (ART_DIRECTION §2 has the tile mapping).
5. **Generate the rest** from `docs/ART_NEEDED.md` P1 in the new style. Budget it: about 20 gens per character
   (map and battle) plus 1–2 per animation, 10 per portrait batch and 10 per background.

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
- **Exported builds** need `*.txt` in the export filter so `data/dialogue/` and `data/campaign/` ship.

### Art (PixelLab)
- **Done:** map sprites, battle sprites, 7 portraits and terrain tiles for the original cast and test enemies (session 2).
- **Needed:** everything else; see `docs/ART_NEEDED.md`. P1 items: Aldric, Tamsin, the Gloamkin, the Binder, villagers, the missing portraits, and story backgrounds.
- **Budget:** 564 generations were left at the end of session 4. Pause generation with `tools/art/STOP`.
- **Style:** the session-2 art is superseded by the session-4 direction in `docs/ART_DIRECTION.md`.

---

## Next development tasks (in order)
1. **Playtest the campaign by hand** and tune balance (enemy levels, villager durability, the Ch 3 fog pace).
2. **Art pass:** the style is now settled (session 4). Follow *Next: the art pass* above, then switch `BattleController.MapId` (the scene default) to `prologue` if wanted.
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
- **PixelLab art gotchas** (a style reference with a background leaks it, "hand" draws hands, crests drift to gold badges, 8 concurrent jobs at most) are in `docs/ART_DIRECTION.md` §3.
- **Previewing art in the real game without committing it:** back up the target files, swap the new PNGs in, run `godot --headless --path . --import`, take a `--screenshot`, restore, then import again. A unit with no `animations/idle/` falls back to `rotations/<dir>.png`.
- **Story maps with only deploy slots** (Ch 2, Ch 3) need a roster. Battle Select gives them a default party (`BattleController.SkirmishParty`).
