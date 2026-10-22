# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-22 (session 11)._

---

## Session 11: autoplay stalls, Kai's mounted looks, 3D gate, neutral portraits
- **Playtest:** the user played Ch 1 and Ch 5 by hand: both feel good (Ch 1's T1 surround is fixed; Ch 5's 12 turns
  need no extra waves), and the 3D gate, Kai's art and the pause menu all worked as intended.
- **Crests:** no crest fragments / crest EXP. The user wants crest tiers to rise only through the story, after a major
  fight and absorbing a crest, which the campaign's `crest N` steps already do. Dropped from the to-do list.
- **Autoplay stalls fixed** (`BattleController.AutoplayDecision` / `LordDecision`):
  - Ch 3 / Ch 6 ran to T61 because only Rowan (and an unarmed Mira) were left, Rowan waited for a safe attack and
    Varga / Rusk hold their posts. On Rout/Boss maps the lord now presses toward the boss (or nearest enemy) once
    nobody else can fight, stopping where at most one enemy can reach him.
  - Ch 4 (escape) lost 106 times in a row on T3: Rowan ran straight into the woods ambush. An escaping lord now takes
    the reachable cell that minimises steps-to-exit + 10 × enemies that can reach it, never moving away from the exit.
  - Soak runs quit with `AUTOPLAY STUCK ... lost N times in a row` after 10 straight defeats on one battle.
  - `--autoplay` new games play **Casual**: in Classic, early deaths left Rowan alone by Ch 3.
  - Result: a full soak reached **CAMPAIGN COMPLETE** with no script errors. Retries: Ch 1 ×2, Ch 2 ×1, **Ch 5 ×4**
    (Dorran dies T7–11 after most of the party falls), Ch 7 ×1. Autoplay still sends Kai into Sennet on Ch 1 T1.
- **Kai's Cavalier and Paladin redrawn** in the ranch-hand palette (sandy hair, rust neckerchief, green, chestnut
  horse; the Paladin adds a steel breastplate and a green caparison). Battle stills are take #8 of each (user's pick).
  The `walk-4-frames` horse template lost the rider (riderless horse east/west, Kai on foot south), so mounted map
  walks are now a v3 described walk (`manifest.HORSE_WALK`). Effect frames dropped by hand. Old state entries kept as
  `*_session5` (and `walk_template_session11`). About $0.70 spent; **~$2.85 PixelLab credit left**.
- **Ch 5 gate in 3D** (user's option A from https://claude.ai/artifact/K6BoFUrqmkJr8gd8b2kchr): `MapView.AddGate`
  builds oak door slabs along the wall's line (planked, iron-strapped top; door-leaf faces seen when the camera
  rotates) with a capped stone post at each end. Textures from `tools/art/gate_textures.py`. The session-10
  camera-facing `gate_0.png` and `gate_art.py` were deleted (in git history).
- **Portraits are neutral only** (user request): the 14 expression PNGs were deleted, `DialogueBox.Portrait(speaker)`
  loads `<speaker>.png`, `[expression]` tags stay in scripts as acting notes, `expressions.py` is retired.
- **Gloam Shade** map sprite drawn at 75% (`SpriteLibrary.MapScales`; it filled its whole 48 px frame).
- **Pause menu** (user request): Esc in battle pauses everything (`GetTree().Paused`; battle timers use
  `processAlways: false`) under Return to Battle / Settings / Quit to Title (with a check). Esc is now the `pause`
  action, no longer Cancel; X / Backspace / right-click still cancel, and Esc still closes modals and skips dialogue.
- **Lances pierce** (user request): against an Armored target (trait or movement type) a lance ignores half its Def
  (`CombatCalculator.LancePiercePercent`, terrain Def still counts); weapons already effective against it (Heavy
  Spear) keep ×3 instead. The forecast shows "!" for it. It works both ways: enemy lances pierce Aldric (warden).
- **Prologue Hounds** (playtest: too easy to lose Rowan): the Prologue's hounds carry `gloam_fangs_young` (Mt 3, was
  Fangs 5), so they bite Rowan for 4, not 6. Later hounds are unchanged (`act1_early.gloam` picks the weapon).
- **`--level=N|auto`** (DevTools): single battles raise the player side to a campaign-like level with average gains
  (`LevelUpService.RaiseToAverage`); `auto` uses `DevTools.CampaignLevels` (Prologue 1 … Ch 7 8). Ch 7 at level 8:
  autoplay wins 2/2 (T20, T13) but loses Aldric on T3 both times; Brann and Tamsin do 0 to Halvric, Elise's
  Thunder does 20.
- **Gold** checked with the user: fine for now (table: 150 + 50 × chapter plus story gold, ~5,000 over Act I).
- **Dev:** `--focus=X,Y` starts the battle camera on a cell (with `--hideunits` for map-art screenshots).

## Next (plan for session 12)
1. Finish the hand playtest from the list below: the Prologue (harder Hounds), Ch 7 (Sunder, the Hammer, Halvric's
   22 Def), the objective tour's length and Retry Battle. Ch 1 and Ch 5 are done.
2. Gold check, Windows build re-export, merge `act1-scale` into `main` (items 3, 4, 6 below).
3. Still not built: suspend saves.

## Plan for session 11 (carried over; items 2 and 5 done in session 11, 7's crest part dropped)
Everything from session 10 is committed on `act1-scale` (not pushed); `main` is still at the Aldric retune.
1. **Hand playtest what changed in session 10** (most of it was only checked by screenshots, unit tests or autoplay):
   - **Ch 1:** half the enemies at the start, the rest from the north edge on T2 / T5 / T6 (`wave2/5/6`). The user's
     complaint was Kai being surrounded on T1. Autoplay always charges Kai into Sennet, so it can't judge this.
   - **Hounds** hit for 12 (Str 7, Fangs 5): 4×2 on Kai, 1×2 on Aldric, 6 on Rowan in the **Prologue**. Check the
     prologue isn't too harsh now.
   - **Ch 5:** survive 12 turns (waves still stop at T6, so T7–12 may feel empty: ask the user whether to add more
     waves); the Alpha's `Leash = 2`; the gate, its dialogue and the turn countdown.
   - **Ch 7:** Sunder effective against Armored, the Hammer from the smith's house (6,21), the new shop weapons.
     Is Halvric still a fair boss wall (22 Def)?
   - **Objective tour** before turn 1 on every map: does it feel too long? Confirm skips a stop.
   - **Retry Battle** in the battle Settings menu, and Space no longer confirming (it only speeds up).
2. **Autoplay stalls** found in session 10: the full-campaign soak ran past turn 60 in a later chapter (probably Ch 3,
   with Stalkers), and a single `--map=ch6 --autoplay` run reached turn 50 with no result. Find out whether
   the AI is stuck (enemies holding, the player AI never reaching Rusk) or a map can't be finished.
3. **Gold:** still to confirm by hand that victory gold feels like "about one item a battle".
4. **Re-export the Windows build** (`godot --headless --export-release "Windows Desktop"
   "build/windows/The Radiant Crest.exe"`; the console wrapper idles after "savepack DONE": it's finished).
5. **Kai's tier-2/3 looks** (Cavalier, Paladin) still wear the old tan colours: redo them in the ranch-hand palette
   (`generate.py chars` + `battle_stills.py`, ~$0.5).
6. **Merge `act1-scale` into `main`** once the playtest is happy.
7. Still not built: crest fragments / crest EXP, suspend saves. Art wish list in `docs/ART_NEEDED.md`
   (~$3.57 PixelLab credit left).

## Session 10: playtest fixes, gates, walls, objectives and Ch 7 balance
- **Balance:** Gloam Hound Str 5 → 7 and Fangs might 4 → 5 (they did 0 to Aldric and Kai); Gloam Brute Spd 2 → 3
  so Mira's Fire (16 on every Beast) no longer doubles them. Ch 1 starts with 6 Gloamkin + Sennet; the 5 that stood
  nearest Kai now arrive from the north edge (`wave2` T2, `wave5` T5, `wave6` T6).
- **Ch 5:** survive 12 (was 8); the east gate is a new `gate` terrain (G, impassable, ground height) drawn as one
  camera-facing prop of two oak doors in a stone arch (`MapView.AddGate`, `assets/sprites/props/gate_0.png`,
  `tools/art/gate_art.py`). On T5 the gate cells become rubble. Before, the gate cells were raised walls whose
  terrain changed to floor, so units walked over a wall that was still drawn. New scenes `ch05_alpha_gate`,
  `ch05_harriers`, `ch05_harrier_down` (the first Harrier is tagged `harrier`), `ch05_gate_cracks`. The Alpha has
  `Leash = 2` (new per-spawn `SpawnDefinition.Leash`: how far a Boss may step from its start to attack).
- **Objective tour** (`ObjectiveTour.Stops`, `BattleController.ShowObjectives`): before turn 1 the camera visits
  the win condition (seize/escape cells, the boss), must-survive units, protected villagers and the map's new
  `Highlights` lines (`CELLS => caption` / `KEY => caption`), with a caption (`BattleHud.ShowCaption`). Confirm skips
  ahead; skipped under `--autoplay` / `--skipdialogue`. Survive maps show "N turns to survive" / "Last turn!" after
  each Player Phase banner (`ObjectiveTour.Countdown`).
- **Ch 6:** the T5 pegasus scouts are now three units (`scouts`) that appear for `ch06_scouts` and fly off with the
  new `leave KEY` trigger effect (`MapExit.Departed`; `defeated` triggers ignore departed units).
- **Ch 7:** Rowan's Sunder is effective against Armored (~24 to a knight); Hammer / Armorslayer / Heavy Spear
  (tier 2, effective against Armored, heavy) at the Ferrow's Keep armoury; a smith's house at 6,21 gives a Hammer
  (`ch07_smith`); highlights point at the smith and Halvric. The smithy only reforges into a weapon with the same
  effectiveness (otherwise iron axes became Hammers, which are cheaper than steel axes).
- **Art:** Sennet's east/west and Sable's east/north/west map sprites each had two figures side by side: cropped to
  one (Sable's west is the mirrored east). Stone sides for raised wall, square, throne and fort blocks off farm maps
  (`wall_side.png` / `_1`), with arrow slits, barred windows and green/red banners on about one exposed wall face in
  four (`assets/textures/wall_decor/`, `tools/art/wall_art.py`, `MapView.StoneSideMaterial` / `AddWallDecor`).
  Bridges turn from the water or shallows beside them, else follow their neighbouring bridge cells (Ch 6's bridges
  were half turned), and always use the main bridge tile.
- **UI / input:** Retry Battle (with a confirm) in the battle Settings menu (`BattleController.RetryBattle`, shared
  with the defeat screen); Space no longer confirms in battle or menus (removed from `ui_accept` too), it only
  fast-forwards.
- **Tests:** 118 (Boss leash, highlights parsing, the objective tour and countdown, `leave`, smithy specialists).

## Session 9: Ch 7, new Gloamkin, Kai's new look, playtest round 3
- **Ch 7 "Storm over Dain Hall"** (`tools/maps/act1_ch7.py`, 30×24 castle): moat and gate, courtyard with ramparts
  (height 2), keep and throne room; new `throne` terrain (Y: Def 3, Avo 30, heal 10%); Wren (pegasus knight, joins on
  Rowan's Talk on T2), Lord Halvric (knight L12 boss, Dominion over the garrison); `data/dialogue/ch07.txt`,
  `hub_briefing_ch07` / `hub_act1_end`; campaign `crest 2` then the Act I end text.
- **New enemies:** Gloam Harrier (flying beast), Gloam Shade (beast caster; Gloom Bolt doubles might against the new
  `Armored` trait on warden / warden_lord / knight; heavy so it never strikes twice), `gloam_alpha` boss class with
  Alpha Claws; armoured `enemy_knight` art for Ch 7.
- **Scaling:** `BattleSetup.GenericGrowths` raised (Str/Mag 55%, Def 40%, HP 80%) so every generic enemy grows ~0.6
  attack a level; Gloamkin class growths trimmed to match; levels per chapter noted above `gloam()` in act1_early.py.
- **Siege clutter** (Ch 5): `barricade` (k, impassable except to fliers) and `rubble` (x, slow, +10 avoid) terrain with
  props (`wagon/stall/crates/barrels/cart/rubble_N.png`, picks in `tools/art/approved/siege_props.txt`);
  `assert_connected` in act1_early.py checks no clutter seals off cells.
- **Map changes:** Prologue east pack waits (G) and the Binder sits at 20,3; Ch 1 chapel moved west (fort 3,8), Mira
  spawns there on T3, villagers 30 HP / 6 Def; Ch 3 Tamsin joins on T2 and `varga_pack` appears on entering her
  clearing; Ch 4 camera pan to the exit, woods ambushes T1 (west) and T3 (south), cavaliers removed.
- **AI:** enemies weigh units on a map's `protect` list (ProtectPull 2 steps, ProtectTarget +4); LordTarget 12 → 8.
- **Rewards:** `CampaignRunner.VictoryGold` = 150 + 50 × chapter, +20 EXP (`SurvivorExp`) for every survivor; story
  gold cut (Ch 1 500, Ch 2 house 300, Ch 5 Binder 300, Ch 6 500, Ch 7 800).
- **Combat:** adjacent friend gives +10 hit / +5 avoid (`BattleState.AdjacentHit/Avoid`, shown in the forecast);
  tomes gain +1 might per 3 caster levels (promoted +10 levels) and aren't offered by the smithy; Fire/Elfire → Beast,
  Thunder → Armored, Wind → Flying at ×2 (`ElementMultiplier`); Ch 7 mages carry Fire.
- **UI / QoL:** unit panel shows only name, level/class, HP, weapons, items, spells & arts; arts listed directly in
  the action menu; hold Space for 4× speed (`InputSetup.FastForward`); props fade round the cursor and in front of it
  (`MapView.FadeAround`); water ripples follow each body of water (`FindVerticalWater`); autosave after every battle;
  "save before returning to the title?" in the hub; Settings in the battle map menu (`UI/Nodes/Modal.cs`).
- **Art:** Wren, Halvric, knight, Harrier, Shade, Dain Hall backdrop, siege props; **Kai redrawn** in the user's
  "ranch hand" pick (portrait, determined/smile/laugh expressions, map, battle; old art not kept in the repo);
  Elise's map sprite redrawn holding the tome in front. `expressions.py --who <id>` redoes one character.
- **Dialogue:** no `[surprised]` (the user dislikes the shocked faces): lines remapped, portraits deleted, a content
  test enforces it. DIALOGUE.md says to use `worried` / `smile` instead.
- **Music:** the user's settings had music at 0 (not a bug); they've since raised it.
- **Tests:** 114 pass. Autoplay notes: a killed background task can leave its Godot child running; check with
  `Get-CimInstance Win32_Process` and stop only the `--autoplay` ones, never the editor.

## Session 8: playtest round 2 and Ch 4–6 at the new scale
- Shop as a list plus detail card, X jumps to Party, Aldric about 10% below his original strength, villagers 24 HP / 4 Def,
  Rowan's criticals redrawn, guards wake at move + range + 2, 18 portrait expressions, spell effects in code.
- **Ch 4–6 rebuilt** in `tools/maps/act1_early.py` (26×26, 29×22, 32×22; the .tres files are canonical, rerunning
  the script overwrites them) with dialogue `@focus` cells moved to match. Autoplay (seeds 1–3): Ch 4 wins 3/3 on
  T4, Ch 5 wins 3/3 (Dorran lives), Ch 6 wins 2/3 with heavy losses.

## Session 7: hand-check fixes, bigger early maps, art and polish
- **Playtest round 2:** Rowan's criticals redrawn without baked-in effects (`animate_still.py --critical`, effect
  frames dropped; crest versions rebuilt with `add_crest.py --redo`). Guards wake at move + range + 2 cells
  (`AiController.Alerted`). 18 portrait expressions; spell effects in code. Ch 1 lost a brute after Aldric's cut;
  autoplay still loses it 1 run in 3 (villagers), so check it by hand; next lever: sturdier villagers.
- **Shop as list + detail card** (user's pick from https://claude.ai/artifact/Bbu5Me4a2Ha88FLkXMXMc9): Buy / Sell tabs,
  rows of icon, name and price (greyed when the buyer can't use or afford it), and a card with the item's numbers and
  only the differences from the buyer's current weapon or armour (green better, red worse). X in the hub jumps back to
  Party. **Aldric about 10% weaker** (playtest: he beat Ch 2's bandits alone; −20% then proved too weak): personal
  bases HP −1, Dex/Lck/Res +1 (was +2/+1/+1/+1 HP/Str/Dex…) and growths about a tenth lower. Villagers 24 HP, 4 Def.
- **Hub redone (user's option A from https://claude.ai/artifact/RGw7jyT84DXTQGkAo5xBmj):** a top bar with gold (big,
  gold-coloured), chapter, convoy, crest, Title and March On; places in a left column with Osmund beneath; screens wrap
  and scroll so nothing runs off at any window size. The shop buys for a chosen unit straight into their pack
  (`HubService.BuyFor`). **Osmund's tour** on the first visit: places, then buy and equip an Iron Sword (Rowan now
  starts with the `old_sword`, Mt 3, tier 0, which Brann can reforge). Option C (a living 3D town that grows) is the
  plan for later; `--hideunits` shows a map with no units or HUD for screenshots like the mockup.

The user counts the Prologue as "chapter 1", so their "chapter 2" is Ch 1 (Brookhollow) and "chapter 3" is Ch 2 (Millbrook).
- **Units stand on their tile:** `UnitView` measures each sprite direction's legs and lowest pixel (some PixelLab
  frames are 5–15 px off-centre; Rowan Lord east by 13) and puts the feet 0.3 cell toward the camera from the cell
  centre, with the shadow. Before, feet were at the centre so the 1.5-cell sprite stood over the tile behind.
- **Aldric's attack** (Warden and Warden Lord) lost the frames that swung down into the ground; it now goes from
  the wind-up straight to the forward thrust. A regenerated attack would be better once credits arrive.
- **No stone on farm maps:** `ArtSet = "farm"` (Prologue, Ch 1, Ch 2) draws walls as bush hedgerows on grass and forts
  and squares on dirt (`MapView.TileFor`); Ch 1/Ch 2 `#` cells became road `=`.
- **Ch 1:** Kai is a player unit from turn 1 (his join scene plays on turn 1) and carries a vulnerary. Mira has a
  new self-heal spell, **Prayer** (range 0, 3 uses; `HealTargets` includes the caster when MinRange is 0).
  Autoplay ×3: no player deaths, one villager lost each run.
- **Ch 2:** Brann starts inside his forge (fort 15,8), no turn-3 damage, deploy slots moved 2 tiles east, and
  Gorran's archer (stationary) and bandit (guard) no longer cross the river to attack Brann. Autoplay ×4: Brann
  survived every run (seed 4 lost Rowan to the ambush archers, a known autoplay weakness).
- **New props (30 gens):** `wheat`, `fence` and `hedge` (`generate.py props`; takes in `tools/art/takes/props/`,
  picks cropped into `assets/sprites/props/`). Field cells show two rows of wheat (the cabbage `crop` stays as a
  fallback); farm-map walls show a fence next to a cottage (fort) and a hedge elsewhere.
- **Bigger maps (+60% per side, the user's pick from https://claude.ai/artifact/EG5R6b9d6wjsKeffoXxNvR):** Prologue
  22×16, Ch 1 26×19, Ch 2 29×19, written by `tools/maps/act1_early.py` (painted layouts; rerun to regenerate).
  More enemies, a barn brute on guard, three bridges each in Ch 1/Ch 2, Ch 2's mill guard holds the north bank.
  Dialogue `@focus` cells and `docs/story/CHAPTERS.md` updated. Brann's forge stays about as far from deploy as before.
- **Hedges:** farm-map walls are solid 3D hedges that follow the grid, joining neighbours and wrapping round yards
  (`MapView.AddHedge`), in a seamless leaf texture (`tools/art/hedge_texture.py` → `assets/textures/hedge_leaves.png`).
  Their shader pulls them toward the camera like the props, so units behind a hedge stay behind it. Grid-following
  fences (the user found them wrong) and the angled fence/hedge billboards were dropped; their art is still in
  `assets/sprites/props/` (`fence_*`, `fence_straight_*`, `hedge_*`).
- **Grounding:** the unit shadow sits a little behind the feet (centred on them, its front half showed below the feet
  and read as hovering), and the idle bob dips a pixel instead of lifting.
- **Font:** VT323 everywhere (user's pick from https://claude.ai/artifact/J56NzWNNMFDBUUm5iStHi3). All sizes go through
  `UiKit.Px`. The battle controls hint was shortened to fit.
- **Menus:** Z/X (and WASD) now work on menu buttons (`ui_accept`/`ui_cancel` extended in `InputSetup`); before,
  only Enter/Space pressed them (e.g. Continue on the victory screen).
- **Map sprites:** anchors are measured per animation, not just the standing frame (walk frames have the feet ~6 px
  higher, so walking Gloam floated).
- **Autoplay:** unarmed units (Mira before her tome) shelter instead of advancing (`CarefulAi.Shelter`).
- **Title intro:** on launch, Rowan's tier-4 radiant crest rises in the dark, flares to white and reveals the new
  `title_vista` background (sunrise valley, 240×135), then the title and menu fade in (`TitleScreen.PlayIntro`).
  Any key or click skips; it plays once per launch and never with `--newgame`, `--load` or `--autoadvance`.
  `generate.py backgrounds` now keeps every take in `takes/backgrounds/<id>/` and installs take 0.
- **Aldric's attacks redrawn** (Warden and Warden Lord) with a level chest-height slash: `animate_still.py --attack`
  overrides the shared sword wording, which swung into the ground.
- **Named Binder bosses:** `boss_sennet` (Ch 1) and `boss_varga` (Ch 3) have their own map sprites (stills only, no
  walk/attack yet) and battle stills (picks in `approved/battle_picks.txt`). Sennet's battle animations are done.
  Varga's are done too (fetched after a PixelLab outage). Gorran already has his own look (`boss_brigand_chief`).
- **Portraits:** Brann and Osmund regenerated to match the cast (they were rounder and cartoonier); Rowan, Kai,
  Mira and the brigand chief checked and kept. The bottom 4 rows of new portrait takes carry a light bar: clear them.
- **Side outlines:** every map sprite gets a 1 px ring in its side's colour just outside its own outline (blue
  army, red enemies, green allies; `SpriteLibrary.Outlined`, `UnitView.OutlineFor`), redrawn when a unit changes side.
  The user turned down the chibi NSO map-sprite look after a pilot (Rowan, $0.08): keep the current character style.
- **Crest absorption:** when a crest-bearer falls, its shrouded crest rises over the spot, shatters into white
  shards, and the shards stream across the map into Rowan (the camera follows; they track his place on screen), then
  his radiant crest flares and he glows (`BattleEventPlayer.PlayCrestAbsorb`). Plays before the bearer's death scene.
- **Lighting (Octopath-style):** a warmer, lower sun with softer shadows, mild bloom (`Battle.tscn`), and tilt-shift
  depth of field that follows the zoom (`TacticsCamera`). Units, props, wheat/bushes/reeds and hedges cast real
  shadows from invisible shadow-only copies at their true ground spot (the visible ones are pulled toward the camera).
- **Weapon picker:** Attack first lists the weapons that can reach an enemy from where the unit stands (skipped when
  only one can), then target select offers only enemies that weapon reaches; Tab cycles that weapon's arts
  (`PlayerInputController.OpenTargetSelect` / `OpenTargets`). Units with two weapons: Brann, Elise, Dorran, Sable.
- **Cleanup (user request):** deleted the parked flat 2D renderer (`FlatTerrain.cs`, `GridMetrics.Flat` and the flat
  paths in MapView/TacticsCamera/UnitView/SpriteLibrary), `tools/art/superseded/` (5,324 files), the unused fonts
  (Pixelify Sans, Silkscreen) and props (`fence_*`, `hedge_1-4`), and the untracked style-round takes and logs
  (`tools/art/takes/{snes,thracia,fe32,styles2,flat_styles,half,pilot5,review}`). Older notes below that point at
  them are history; the committed files are recoverable from git (last present in `dfabc7b`).
- **Budget:** subscription generations used up (0 left); about $9.3 of the user's $10 credit left (a background or
  map character costs about $0.08, a battle animation $0.02–0.04).

## Session 6: back to 2.5D

The user explored a fully flat 2D map style this session, then **decided to stay with the 2.5D view and its art**.
The game is back on the 2.5D blocks with the session-5 art.

**In the game (committed):**
- Rowan's crest shows on his sprite once it awakens (`_crest` sprite candidates, `BattleController.UpdateCrestSprites`,
  art from `tools/art/add_crest.py`). Rowan is sword-only (no bow on Youth/Lord/Crest Knight).
- Units have a ground shadow and a smaller camera pull, so trees in front hide them; crops sit inside their cell.
- **Hover sprite:** the unit panel (bottom left) shows the unit's battle-screen sprite, animated, whenever the cursor
  or mouse is over a unit (`BattleHud.ShowPortrait`).
- **Map options** on `MapDefinition`: `Lighting = "night"` tints the map and the battle screen (the prologue uses it);
  `ArtSet` (e.g. `"farm"`) only affects the parked flat renderer.

**Parked, then deleted in session 7** (recoverable from git, last present in `dfabc7b`): the flat 2D renderer. `GridMetrics.Flat = false` switches it off; `FlatTerrain.cs`,
`MapView.BuildFlat`, the straight-down camera in `TacticsCamera` and the flat sprite path in `UnitView`/
`SpriteLibrary.Field` stay dormant. Its art is in `tools/art/takes/flat_parked/` (not imported or committed). All
the style rounds (pilots, mockups, scripts) are in `tools/art/takes/{snes,thracia,fe32,gba,styles2}/`.

**What the reference work taught us** (worth applying to 2.5D, see Next):
- The user's references are in `assets/ExampleFiles/` (not committed): `battlemap.png` (a real GBA map at 1×),
  `Tilemap samples.png` (GBA tiles), two Switch NSO screenshots, `Style1-4.png` (GBA-style battle sprites, the target
  for character detail), and two GBA battle-screen captures. Use them only as references; never ship cut-up tiles.
- Measured GBA rules: 16 px tiles reused heavily (1,026 cells, 90 tiles); grass is every pixel picked at random
  from ~9 weighted greens (only 8% match their neighbour), not smooth noise; trees are separate small stippled
  pines with a blue-grey outline; paths are pale yellow sand with a stippled edge and no outline; houses have
  straw roofs in vertical streaks, a red ridge, white walls and a dark olive outline (not black). Palettes are in
  `tools/art/takes/gba/gba_mock.py`.
- Character detail the user wants (from the NSO shots): big heads, thick black outlines, a few bold colours,
  bright highlights. PixelLab follows a real reference image (`style_image`) far better than text like "GBA style".
- Our smooth, low-contrast textures were the user's main complaint ("too clean", "too smooth").

**Budget:** PixelLab is at about 0 generations; the user is buying more on 2026-10-18.

### Next (plan for session 7)
1. **Hand-check the 2.5D state** with the user (Rowan's crest switch, sword-only Rowan, shadows, the hover sprite,
   the night prologue). Their earlier 2.5D complaints were floating sprites, units drawn over trees and crops on
   cell corners; the fixes for these are in but not yet seen by them.
2. **Bring the GBA lessons into 2.5D, free first (code):**
   - Repaint the 32 px top tiles for plain/meadow/road in code to the measured rules (per-pixel weighted grass,
     pale stippled paths), so the ground stops looking smooth. Keep the old tiles to compare.
   - Recolour props toward the references (pines with a blue-grey outline and stipple, cottages with a streaked straw
     roof, red ridge and olive outline) by palette-mapping the existing prop PNGs.
3. **With new PixelLab credits** (use the references as `style_image`, pilot 1 take before any batch):
   - Map sprites at NSO detail for the cast (48 px, or test 32 px), team-coloured if the user still wants it
     (blue army, red enemies); then their walk/attack/hit animations.
   - Battle sprites matched to `Style1-4.png`; GBA-style trees, cottages and peaks as props.
   - Remaining `docs/ART_NEEDED.md` P2/P3 (named Binder looks, tier-2 outfits, expressions, roof/cliff tiles).
4. **Game work not blocked on art:** the user's deferred hand-playtest notes; Ch 7 (Storm over Dain Hall, ends
   Act I); crest fragments / crest EXP; suspend saves; installing export templates for a Windows build.
5. **Docs:** fold the GBA rules above into `docs/ART_DIRECTION.md`; refresh `docs/ART_NEEDED.md` after the art pass.

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

### End of session 5 (user requests)
- **Renamed to "The Radiant Crest"** (window title, title screen, docs). The solution, assembly and `NotFireEmblem`
  namespaces keep the old name. Godot's save folder follows the name: saves were copied from
  `%APPDATA%/Godot/app_userdata/Not Fire Emblem/` to `.../The Radiant Crest/`.
- **Levels:** a level-up never heals (current HP stays put) and always gives at least one stat (if every roll misses,
  the highest-growth uncapped stat rises). Promotion needs only Lv 10 and crest tier ≥ the class tier (seal items were removed at the user's request).
- **Ground terrain** `road =`, `field :`, `meadow ,`, `square #` (plain rules, 32 px tiles in
  `approved/terrain_gba32/ground/`). The prologue, Ch 1, Ch 2 and test map were repainted on plain cells only, so
  movement and balance are unchanged.
- **Map art filled in:** detailed grass on plain cells (the old plain tile was two flat greens), earth-and-stone block
  sides (triplanar `cliff_side.png`), and pixel-art billboard props replacing the primitive shapes: pines and oaks
  in forests, peaks on mountains, cottages on forts (`assets/sprites/props/<id>_N.png`, cropped so they stand on the
  ground; `generate.py props` makes 16 takes each). Props use full billboards like units (upright-only ones looked
  squashed under the high camera).
- **One pixel scale on the map** (the user chose "unified 2.5D" over a flat GBA map; mockups:
  https://claude.ai/artifact/CpzoeijhXG9QqLrSZfGpVa). Every map sprite is drawn at `MapView.ArtPixelSize` = 1/32
  cell per art pixel, the terrain tiles' density: units (now 1.5 cells tall per 48 px frame), trees, peaks,
  cottages and ground decor. Props are sized by their art, never scaled; bosses no longer get a bigger sprite.
  Cottages and peaks were redrawn at 32 px, decor at 16–24 px (larger versions in `approved/props/large/`). Fort
  cells use the cobble tile under the cottage.
- **EXP on the battle screen:** an EXP bar under HP fills after the fight and level-ups show there before the map.
  `--exp=N` starts player units with N EXP for testing.
- **Chapters 4–6 built** (see CHAPTERS.md "Mapping to current content"): maps from `tools/maps/act1_ch4_6.py`,
  scripts in `data/dialogue/ch04–ch06.txt` plus hub briefings, the Knight class, Dorran and Sable, marsh terrain,
  and art (Dorran, Sable, Rusk, Dain Hall cavalier, Gloam Alpha; three portraits; hill_road, ferrows_keep and
  greyfen backgrounds; marsh tiles, reeds and stilt houses). Maps can set `Background` for the battle screen.
  Autoplay now runs for the exit on escape maps and plays "die KEY" / escape units as carefully as the lord.
- **Settings** (title screen "Settings", hub tab "Settings"): music and sound volumes, text speed, battle scenes
  on/off. Saved to `user://settings.cfg` by `src/Core/Nodes/Settings.cs`; `AudioDirector` routes music and effects
  through "Music" and "SFX" buses so the volumes are independent; the B key in battle updates the saved setting.
- **Exports:** `export_presets.cfg` has a "Windows Desktop" preset that includes `*.txt` (dialogue and campaign
  scripts) and leaves out tools/, docs/, tests/ and the reference images. Exporting needs Godot's export templates
  (Editor > Manage Export Templates > Download); output goes to the gitignored `build/`.
- **Ground decorations** stand up like the trees: grass tufts and bushes on plain cells, flower clumps on meadows, a
  2×2 crop grid on fields (`tuft`, `bush`, `flowers`, `crop` props; 32 px takes via `PROP_SIZE`). They sit toward the
  cell corners so units stay clear.
- **Music and sound:** `tools/audio/` synthesizes GBA-style sampled-orchestra music and effects (numpy +
  soundfile; `pip install -r tools/audio/requirements.txt`, then `python tools/audio/build.py`). 14 tracks (title,
  hub, prep, player/enemy phase, the 7 story cues, victory, defeat) and 27 effects (menus, swings per weapon, hit,
  crit, miss, kill, heal, level-up jingle, the 7 story cues). `AudioDirector` (autoload) crossfades looping music
  and plays effects; menu sounds follow the confirm / cancel / cursor actions. Hooks: dialogue `@music`/`@sfx`
  (a skipped scene still plays its last track), title/hub/prep screens, phase changes, combat on the map and on
  the battle screen, kills, heals, level-ups, and the victory/defeat jingles. libsndfile's Vorbis encoder crashes
  the process on one big write, so the build writes OGG in 4096-sample blocks.
- **Tier-2 class art:** map + battle sprites for Crest Knight (Rowan), Warden Lord (Aldric), Sniper (Tamsin),
  Warrior (Brann), Paladin (Kai), and Sage (Mira and Elise), as `<unitId>_<classId>`; picks in
  `approved/battle_picks.txt`. Horse-template map sprites need the `walk-4-frames` template (`HORSE_WALK`); the
  Cavalier and Pegasus Knight had no map walk until this was fixed.
- **Dialogue copyedit** (all six scripts + the canon copies in DIALOGUE.md): question marks, fragments that read as
  errors, consistent past-tense narration, Varga's pronoun ("him" -> she), and Osmund's self-contradicting Ch 3
  briefing. Voices kept per DIALOGUE.md section 2.
- Aldric's attack redrawn (the old prompt "from high behind" painted a second sword).
- Hand playtest copy: `git worktree` at `C:/Users/nweai/nfe-playtest` (detached at the commit it was made from;
  `git -C C:/Users/nweai/nfe-playtest checkout --detach <commit>` then build + import to update it).

### Next
1. User review of the new battle cast in game; swap any still pick for ~7 gens (`animate_still.py`).
2. Every unit now has current-style art (Elise, Kai's Cavalier and the Pegasus Knight use `template="horse"` or a
   mannequin for map sprites; `gorran.png` copies the brigand-chief portrait, his sprite id). Remaining art in
   `docs/ART_NEEDED.md` is P2/P3: named Binder looks, tier-2 outfits, expressions, roof/cliff tiles (need code).
   A gallery of all session-5 art: https://claude.ai/artifact/M2rMGYkPFpD8H5FcLgx9o4 (private to the user).
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
- **Autoplay never saves** (`SaveService.Save` returns early under `--autoplay`; earlier runs overwrote the autosave in slot 0).
- **Autoplay** clears non-lords off a seize tile (Aldric used to park on Ch 2's mill fort, so Rowan could never
  seize and runs ended "AUTOPLAY STUCK"). Its Ch 2 losses (Rowan dying on turn 2–4, sometimes 6+ retries in a row)
  were autoplay weaknesses. `src/AI/CarefulAi.cs` now makes it heal (staff users heal anyone under 75%), drink
  vulneraries below 35% HP (the lord below 50%) and retreat to the safest tile; three campaign runs then finished with
  no losses. Still seen in every run: **Brann dies on Ch 2 turn 3** before anyone reaches him (the "pressed" damage plus
  bandits), and autoplay **kills Tamsin** in Ch 3 instead of recruiting her. Check both in the hand playtest. It still never uses Talk, so automated runs skip optional recruits. Talk is covered by unit tests.

### Art (PixelLab)
- **Done:** map sprites, battle sprites, 7 portraits and terrain tiles for the original cast and test enemies (session 2).
- **Needed:** everything else; see `docs/ART_NEEDED.md`. P1 items: Aldric, Tamsin, the Gloamkin, the Binder, villagers, the missing portraits, and story backgrounds.
- **Budget:** 564 generations were left at the end of session 4. Pause generation with `tools/art/STOP`.
- **Style:** the session-2 art is superseded by the session-4 direction in `docs/ART_DIRECTION.md`.

---

## Next development tasks (in order)
1. **Playtest the campaign by hand** and tune balance (enemy levels, villager durability, the Ch 3 fog pace).
2. **Art pass:** the style is now settled (session 4). Follow *Next: the art pass* above, then switch `BattleController.MapId` (the scene default) to `prologue` if wanted.
3. **More chapters** from `docs/story/CHAPTERS.md`: Ch 7 (Storm over Dain Hall, Wren, Halvric, the Knight's Crest) ends Act I.
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
