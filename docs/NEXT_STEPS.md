# Next Steps — Handoff

Where development stopped and how to pick it up. Update this file at the end of each session.

_Last updated: 2026-10-30 (session 18)._

---

## Session 18 (the four items before Act III)
1. **Act II hub.** `Campaign/HubPlace`: Brookhollow, Calder (palace lodgings) or Solmere, read from the hub briefing's
   first `@bg`. The hub shows the place's picture behind the panels, its name above the places list, its music
   (new synthesized `hub_calder`, a G minor minuet, and `hub_solmere`, a D major harbour tune) and Osmund's
   shop/idle lines.
2. **Crestbound signature arts:** one per tier 3 class (table in CHAPTERS.md, Ch 13). Two new art flags: **Brave**
   (two strikes before the counter) and **Pierce** (half of any target's Def, or Res for magic;
   `WeaponDef.PiercesAll`). The battle log now names the art. Autoplay at level 25 used Hunter's Mark, Pike Wall,
   Whirlwind and Falling Star.
3. **Iron Man fallen companions:** a companion who dies in Iron Man gets `fell:<id>` (not Aldric's scripted `leave`)
   and leaves later scenes like a missed recruit. Script lines can name stand-ins, `ELISE|MIRA [worried]: ...`
   (DIALOGUE.md section 1); 47 lines in Act I–II got one. Five `_no_<id>` scenes cover deaths in a recruiting chapter
   (Brann Ch 2 and his smithy, Tamsin Ch 3, Sable Ch 6, Halde Ch 12). Osmund names the dead at the next hub, and
   Holt's apprentice keeps the forge if Brann falls. Aldric, Kai, Mira and Seraphine retreat, so they never fall.
4. **Art leftovers:** nine stat icons (`assets/ui/icons/icon_stat_*`, $0.72) in the hub's party stats and both
   level-up panels (`UI/LevelStats`). Ch 6's marsh and the village roofs were already covered (shallows, reeds and
   stilt tiles; house props). **PixelLab: about $6.00 left.**
- Build clean, 158 tests pass, content loads (189 scenes, 21 arts).

## Next (plan for session 19)
1. Hand playtest: the Calder/Solmere hub (from slot 3), and a Crestbound unit's art in a real battle.
2. **Act III** ("The Hollow Path", Ch 16-22, CHAPTERS.md). Its shops must sell `mythril_*` and Mythril Mail. New
   scenes should give stand-ins (`A|B:`) to lines by companions who can fall.

## Session 17
- **Autoplay stalls fixed.** `AiController.Picks` now skips spared recruits: the autoplay party chose to attack Ysra
  (Ch 11) or Wren (Ch 7), the rules refused, and the refusal ended the whole Player Phase every turn. A rejected autoplay
  command now only makes that unit wait. A lone lord with no vulnerary and no healer no longer pulls back forever
  (`LastFighter`; Ch 2 stepped to and from the throne for 40 turns). From slot 2, autoplay won Ch 10–14 and made
  **slot 3** (hub before Ch 15); a new game reached Ch 8 with no stalls.
- **User's decisions (the session-14 list):**
  - Ch 8: calmed rioters don't count toward civilian losses (already so).
  - **Ch 11 / missed recruits:** a rout is won once only spared recruits are left (`ObjectiveEvaluator.RoutFoes`);
    unrecruited recruits get a `missed:<id>` flag on victory and their lines are dropped from later scenes, or a
    `<scene>_no_<id>` version plays (`GameSession.Scene`; DIALOGUE.md §1). Ysra has `_no_ysra` versions of
    `ch11_post`, `ch13_post`, `ch14_pre`, `ch14_ysolde_wakes`, `hub_briefing_ch12/14`.
  - **Rowan×Aldric A before Ch 15:** new campaign step `support aldric rowan A` (before the Ch 15 hub) plays any of
    their conversations not yet watched (`SupportStep`, `StoryReturn.Support`).
  - **"Town" everywhere** on screen (code names stay `Convoy`).
  - **Black Steel in Act II, Mythril from Act III:** `HubService.MaxForgeTier(chapter)` is 3 / 4 / 5 by act; the
    Dravenhold Imperial Forge (Ch 14) sells the four Black Steel weapons; Mythril Mail left Solmere. **Act III's shops
    must sell `mythril_*` and Mythril Mail.**
  - **Deploy at most 10** (`CampaignRunner.MaxDeploy`): Ch 15 12 → 10; `--map` battles take the campaign's deploy count.
- **Ch 15:** the user hand-played it with 12 units (`--map`) and won on T7, but found the dialogue poor. Aldric's
  lines in `ch15_pre` / `ch15_reveal` rewritten in his voice, Osmund's shout, Morvane's repeated line; the canon
  `ch15_aldric_stays` / `_dies` untouched. Rowan×Aldric A said "Fetch your bow": now "sword". Autoplay with 10 units
  at level 16 (seeds 1–4): **1/4** (win T10; losses T4 ×2, Aldric falling before his scene, and T8).

- **Later in session 17 (user's hand playtest from slot 3):** the vault was far too easy (enemies ~5 levels up, more
  silver, more risen); selecting a unit marks the cells only it can set off in yellow (Aldric's seal plinths); chests
  redrawn straight on to the camera (prop `chest_front`, takes 0/3/8/11, paid by subscription generations, the
  ≈ $17.04 untouched; `manifest.PROP_STYLES` gives a prop its own style). Ch 15 too easy with Black Steel: Hollowed
  level 18–19 with silver; the goal on escape-all maps is just "Escape!". **Both hand-played again from slot 3: the
  user signed them off ("chapter 15 and the side mission feel a lot better now").**
- **Art push before Act III (user: "everything done and polished before moving onto act 3"):** `generate.py`
  downloads in parallel (16 files, 4 characters at once; ~15 s for 4 characters, was ~4 min) and skips characters
  already on disk unless `--overwrite` (a re-fetch undid Sable's hand-cleaned frames once; restored from git). New
  manifest entries: `halde_sniper` (her missing tier 2), the 13 Crestbound outfits, `enemy_paladin/_sniper/_valkyrie/
  _swordmaster/_warrior/_sage`, 12 `ICONS`, `mast`, `snow_pine`, `chest_front2` (the old chest as its style image,
  `PROP_STYLE_REFS`) and the `snow` tile set. Map looks: 17 good at the first take; Tamsin's, Pip's and the enemy
  Sniper's drew two figures (old takes kept as `map:<id>_take1`, redone "a single ... one person only").
  Code: Ch 14 `ArtSet = "snow"` (MapView draws `<set>_<tile>` / `<set>_<prop>` art where it exists), ship hulls and
  rails and masts for Ch 12, item icons (`UI/ItemIcons`), level-up / severance sparkles, the Dominion aura, chests
  drawn at 70%.

## Next (plan for session 18)
1. **Hand playtest Ch 15 from slot 3** (`--load=3`: real prep screen, 10 units). If too hard at 10: the two door
   blockers Guard instead of Stationary, or one fewer top-right Hollowed. The Rowan×Aldric conversations play first if
   slot 3 hasn't seen them.
2. **Hand playtest the Old Warden Vault and the Side Maps tab** from slot 3.
3. Art with the ≈ $17 (item 5 of session 17's plan below), then the Act II hub, Crestbound arts, Act III.

## Where things stand (end of session 16)
- **Everything is committed and pushed to `main`.** Build clean, **148 tests pass**. Untracked on purpose:
  `assets/ExampleFiles/` and `tools/art/takes/`.
- **PixelLab: ≈ $17.04 left.**
- **Saves:** slots 0/1 the user's playthrough, slot 2 the Act II playtest save (Ch 8 hub; unchanged, a copy was in the
  session scratchpad). Slot 3 was **not** made (see item 4 below): still the old Act I test save. The old slot 3 (an Act I test save) is backed up as
  `slot3.before-session16.json` in the save folder.

## Next (plan for session 17)
1. **Hand playtest Ch 15 again** with the session-16 changes (countdown, Morvane not attackable, Aldric/Ysolde lines,
   13 Hollowed at the start: 5 holding the west door, 4 more in the top right). Autoplay at level 16 (seeds 1–4) now
   wins **2/4** (was 3/4): T8 and T10, losing 6–7 units (guards included); losses on T4 (Aldric fell before his scene)
   and T9 (Rowan). Autoplay is weaker than a player, so judge by hand; if too hard, make the two door blockers Guard
   instead of Stationary, or drop one top-right Hollowed.
2. **Hand playtest the Old Warden Vault** (`--map=vault --level=auto`) and the **Side Maps** tab (from slot 3 if it was
   made: the hub before Ch 15). Still never played by hand.
3. **Ask again** the session-14 decisions: Ch 11's rout needs Ysra recruited (autoplay now walks a recruiter over;
   a player may not know to: count the rout as won with only a spared recruit left?); calmed rioters and Ch 8's
   civilian count; Rowan×Aldric A before Ch 15; "Convoy" → "Town"; where Black Steel / Mythril are sold; smithy past
   Silver.
4. **Autoplay stalls, so slot 3 (the hub before Ch 15, for the Side Maps tab) couldn't be made.** The party waits every
   turn once the last foes hold back: **Ch 7** from a new game (two runs: by T11 Rowan, Aldric, Mira and Elise wait
   instead of pushing to the throne at 15,1; the lone-lord push only fires when the lord is alone) and **Ch 11** from
   slot 2 (`--load=2`; two runs: Wing-Captain Orvald, Boss AI with leash 4, and Ysra left, nobody goes after him). Fix:
   when nothing is in reach and only Boss/Stationary foes are left, advance on them (and `SeekRecruit` for Ysra, new
   this session but not yet exercised). Then: `--load=2 --autoplay --skipdialogue --autoadvance --speed=10
   --saveathub=15:3 --level=auto`.
5. **Art with the ≈ $17** (ART_NEEDED "Outstanding"): tier 3 Crestbound outfits ($6.10), generic promoted enemies
   ($2.80), polish ($0.80); snow tiles and ship hulls need terrain code first. Parallelise `generate.py`'s download step
   first (it fetches one character at a time, ~1 min each). Check new battle animations on a contact sheet before
   calling them done (session 16's axe fixes).
6. Then: the Act II hub, Crestbound signature arts, Act III ("The Hollow Path", Ch 16–22).

## Session 16
- **Seraphine and Ysra** have real stats for their join level (see the Session 15 bug note below): Seraphine L9 23 HP,
  Mag 13, Res 15; Ysra L10 (was L11, over the cap) 31 HP, Str 14, Def 15.
- **Ch 15 playtest** (user: "theres 6 enemies, im meant to escape? and is morvane invincible?"; Aldric swung at
  Morvane for 0 and died to him, a loss):
  - **Countdown** on timed escape maps (`ObjectiveTour.Countdown`): "The way out opens in N turns" until a player-turn
    trigger changes an exit cell's terrain, then "N turns to escape" / "Last turn to escape!".
  - **Invulnerable units can't be attacked** (`ActionRules`: not in `AttackTargets`, rejected by validation). Morvane
    still attacks; `ch15_morvane` plays then.
  - **Lines** at the end of `ch15_reveal`: Aldric "nobody goes near the Chancellor. Nothing we carry can touch him", and
    Ysolde "when that door gives, we run".
  - **More Hollowed** (user request, `act2_early.py` ch15): two Stationary on 1,9 and 1,11 and three Guard at 2,10 / 3,8 /
    3,12 hold the west door; four Aggressive in the top right (21,3 / 23,5 / 22,7 / 19,6). The door highlight says so.
- **Autoplay:** when the only foes left are recruits it may not attack, it walks a unit who can talk to one over
  (`BattleController.SeekRecruit`; Ch 11's Ysra stalled a run at T61). Autoplay needs a window and `--skipdialogue` on
  maps with scenes: `--headless` runs never get past the first turn, and without `--skipdialogue` they wait on dialogue.
- **Gallery** (https://claude.ai/artifact/Ciy27oRTDuhtVnczR9rfbm) now shows batch two too (rebuilt from the published
  page by a scratchpad script; not kept).
- **Axe animations redone ($0.26; PixelLab ≈ $17.04 left).** User: the wyvern rider's axe fused with the wyvern's head,
  the Veiled axemen's critical put the axe through the body. `animate_still.py` has a new `--note` (a sentence added
  to every animation's wording). Wordings that worked:
  - `enemy_imperial_wyvern_rider_axe` attack: "the rider, sitting high on the wyvern's back, swings his one axe in a
    level sweep out to the RIGHT at his own shoulder height, the axe passing above and ahead of the wyvern's head ...";
    critical: "the wyvern rears up and surges forward ... the rider raises his one axe high overhead and brings it round
    in a big arc that ends with the axe held out far ahead of the wyvern's head to the right, above its snout";
    `--note` "The axe ... always stays above the wyvern's head and neck or out ahead of its snout: never in front of the
    wyvern's neck, chest or legs". The dodge needed its own `--note` (the rider leans back LEFT, axe raised beside his
    helmet, never leaning along the neck). Use the same for other mounted axe users.
  - `enemy_veiled_thug_fighter` / `enemy_veiled_agitator_fighter` critical: "raises his one axe high overhead with both
    hands on the handle, the blade up above and behind his head, then leaps forward to the RIGHT and chops it down in a
    big arc out in front of him ... The axe's handle and blade always stay outside his body and cloak". The agitator's
    frames 8–9 had cyan sparks, erased by hand.
  - Old frames: in git history (before this commit).

## Session 15: Act II hand playtest Ch 10–14 and the Act II art
- **Hand playtested and signed off by the user:** Ch 10, 11, 12, 13, 14 (details in each chapter's CHAPTERS.md entry).
  Still to play: **Ch 15**, the **Old Warden Vault** and the Side Maps tab; then the session-14 decisions list below.
- **Ch 10:** ridge ambushes (T3, T7 at the start of the Player Phase) and camp reinforcements (T3, T5, T9); `ch10_flanks`.
- **Ch 11:** the roost's archers/mage come down (were Stationary), north-edge waves T4/T7; `ch11_north`.
- **Ch 12:** pursuit sooner and bigger (T2, T4, T6); **Halde joins on T1** by shouting across the water (no Talk).
- **Ch 14:** mixed weapons (knights and generals now use swords and axes too, wyvern riders axes); **Ysolde** Level 8,
  45 HP, Boss AI leash 4, and the new **Empress's Ward** (accessory, `ArmorDef.Ward`: no effective damage on her).
- **Rules:** bosses get two-thirds of their level as extra Res (`BattleSetup.BossResBonus`; mages one-shot bosses);
  the **crossbow** pierces armour and never strikes twice (`WeaponDef.Pierces`, `SingleStrike`).
- **Bug: named recruits get no gains for their starting level** (Halde "L12" and Ysolde "L16" had level-1 stats).
  Fixed for those two by hand; Seraphine and Ysra fixed in session 16 (Seraphine L9: 23 HP, Mag 13, Res 15; Ysra L10, was L11: 31 HP, Str 14, Def 15). Act I
  recruits were balanced around it, so leave them.
- **Bug fixed:** `--level=auto` promotions skipped the unit's earlier outfits (Seraphine showed as a plain cavalier).
- **Art, batch one ($5.78):** 12 full characters and 3 map-only allies; weapon-variant sprite lookup
  (`BattleUnit.WeaponArt`: `<spriteId>_<classId>_<weapon type>`), the effect cleaner `tools/art/keep_figure.py`, a
  gallery page (https://claude.ai/artifact/Ciy27oRTDuhtVnczR9rfbm, batch one only). The user's notes: the Imperial
  Archer's attack (bow became a spear) and the Swordsman's (chopped into the floor) were redone; `animate_still.py`'s
  default sword and bow wordings were fixed for future sprites.
- **Art, batch two ($12.72 of a $30 top-up):** the rest of Act II: Ser Caddoc (+ portrait; on his real id
  `boss_caddoc`), Imperial knights/generals with great weapons, wyvern rider (axe) and lord, the rest of the Gilded and
  Veiled, the tier-2 cast (Dorran, Sable, Wren, Seraphine, Ysra, Pip), backgrounds `warden_vault` (the vault uses it)
  and `hall_ruin`, `chest_0..3` props. Picks in `tools/art/approved/battle_picks.txt`. Details: ART_NEEDED §0.
- **Memory:** a background art batch was killed once when the machine ran low on memory (a game was open); the
  scripts resume without paying twice. Auto mode's safety check failed repeatedly on art commands at one point; the
  user switched out of auto mode to get past it.
- `tools/maps/act2_early.py` also rewrites `brann`, `kai` and `rowan.tres` with stale values: after a rerun,
  `git checkout -- data/units/brann.tres data/units/kai.tres data/units/rowan.tres`.


## Where things stand (end of session 14)
- **Everything is committed and pushed to `main`** (last commit: "Level cap 10; recruitable enemies can't be
  attacked"). Build clean, **144 tests pass**. Only `assets/ExampleFiles/` (reference images) and
  `tools/art/takes/` are untracked, on purpose.
- The user commits straight to `main` (session 14: a branch was made, then merged and deleted at their request).
- **Saves:** slots 0/1 are the user's new playthrough (Ch 6; Aldric Lv 12 and Mira Lv 11 are above the new cap and
  keep their stats). Slot 2 is the Act II playtest save (Ch 8 hub). A copy of all saves from early session 14 is in
  the session scratchpad only (not kept).

## Next (plan for session 15)
1. **Hand playtest Ch 10–15** (never played by hand) and the **Old Warden Vault** (`--map=vault --level=auto`, or the
   hub before Ch 15): the seals, Ser Caddoc, the altar and Lyra's Blade. Also see the **Side Maps** tab on screen (not
   yet seen) and re-check Ch 9 (tougher Corvin), fog 4, chests for anyone, the pinned shop card.
2. **Watch the level cap** in play: units stall at 10 while the crest gates promotion (tier 2 after Ch 7, tier 3
   after Ch 13). The user chose this on purpose (session 14, "option 1": no banked EXP, so players don't lean on one
   unit); only revisit if they ask.
3. **Decide with the user:** Ch 11 is a rout and Ysra can't be attacked while Rowan is on the field, so she must be
   recruited to win (an exception is possible); calmed rioters and Ch 8's civilian count; Rowan×Aldric A before Ch 15;
   "Convoy" → "Town" everywhere; where Black Steel / Mythril are first sold and whether the smithy forges past Silver.
4. Then: the Act II hub (still Brookhollow's hall), Crestbound signature arts, art (ART_NEEDED §0, incl. Ser Caddoc
   and a vault background), Act III ("The Hollow Path", Ch 16–22; paralogues can use side maps).

## Session 14 (end): level cap and recruit protection
- **Level cap 10** (`ExpCalculator.LevelCap`, was 20): at 10 a unit earns no EXP until it promotes; the hub's Party
  page says "EXP max: promote to grow". Promoted classes count as +10 levels per tier for EXP (was +20).
- **Recruitable enemies can't be attacked** (`ActionRules.SparedRecruit`): not in the attack menu, and rejected by the
  rules (so autoplay can't either), while a unit who could recruit them is on the field
  (`EventDirector.Recruiter` now checks that too; the yellow-green mark follows the same rule). Their own attacks
  still draw counters.

## Session 14: Ch 1–9 playtest notes
- **Kai** is no longer made a Cavalier by the campaign (`class kai cavalier` removed): Soldier now promotes to
  Cavalier (`soldier.tres` PromotesTo), then Paladin, then Vanguard. `hub_merrow_horses` gives him the horse and says so.
  The user's own saves (slots 0/1, Ch 10) already have Kai as a Paladin, which is unchanged.
- **Recruitable enemies** (an enemy that a talk trigger would make join: Sable, Wren, Brann, Elise, Pip, Ysra, Halde)
  get a yellow-green outline and a "Talk: Kai" line under their HP (`EventDirector.Recruiter`,
  `UnitView.SetRecruitable`, `BattleController.RefreshRecruit`). Hint lines in `ch06_start` (Elise) and `ch07_patrol`
  (Tamsin), plus a Ch 6 highlight on Sable.
- **HP:** Elise's personal base HP +3 (18 at Lv 1). Every even level whose HP roll misses still gains 1 max HP
  (`LevelUpService.Roll`; `RaiseToAverage` matches).
- **Promotion scene:** no "What? X is changing!" caption; "X became a/an Y!" fades in at the reveal.
- **Swords:** the Killing Edge is gone; the Silver Sword takes its place (Dain Hall Stores, Ch 12/13 myrmidons; Ch 8/9
  myrmidons and Sable carry Steel). Old saves map `killing_edge` to `silver_sword` (`SaveData.RenamedItems`). New
  **Black Steel** (tier 4) and **Mythril** (tier 5) swords, lances, axes and bows exist for Act III (Black Steel: Silver
  +3 Mt, -5 Hit, 5 Crt, +2 Wt, 3500 G; Mythril: +5 Mt, +10 Hit, 10 Crt, -3 Wt, 6000 G); no shop sells them and the smithy
  still stops at Silver (`MaxForgeTier` 3). "Mithril Mail" is now spelled "Mythril Mail" (id unchanged).
- **Chests open for anyone** on the player's side, no key or thief (`ActionRules.Unlockable`). Chest Keys are no longer
  sold or carried (enemies carry Door Keys instead); the item stays so old saves can sell it.
- **Ch 9 boss:** Archivist Corvin Lv 10 → 16 with Elfire (was Shadow); his four guards +2 levels, the myrmidon now on
  every difficulty.
- **Fog of war** vision 3 → 4 (`FogOfWar.BaseVision`).
- **Shop** (user's pick "A" from the mockups at https://claude.ai/artifact/9XwQBUTCJMYRX6KWsX4P1K): the item list
  scrolls in its own box sized to the page (`_shopList`, `ShopListHeight`) and the card stays pinned beside it.
- Dev: `--hubtab=Shop` opens the hub on a tab (loading a save made at a prep screen goes to prep, not the hub).

## Session 14 (later): side maps and the Old Warden Vault
- **Side maps** (new system, for the vault now and the Act III paralogues later): a campaign line
  `side MAP [deploy N] [force a,b] [until N] : blurb` (`SideStep`) makes the hub offer an optional battle from that
  point until chapter N, until it's won (flag `side_won:MAP`), or while a forced unit is dead. Hub tab **Side Maps**
  (only shown when one is on offer; "Last chance" when it goes at the next march). Flow (`CampaignFlow.StartSideMap`):
  `<map>_pre` scene, prep (with **Back to Camp**), battle, results, `<map>_post` scene, back to the same hub; the
  campaign step never moves. Losing offers **Back to Camp** as well as Retry. Suspends remember the side map
  (`SuspendData.SideMap`). Single battles (`--map=vault`) use the campaign's crest tier and `--level=auto` 16.
- **The Old Warden Vault** (`vault`): see CHAPTERS.md, Paralogues. Offered at the hub before Ch 15 only. **Lyra's
  Blade** buffed to Mt 14 (was 11, under the Silver Sword's 13), Wt 5. Not hand-played yet; autoplay can't do it (it
  never walks Aldric to a seal), so soak runs skip it (autoplay never picks side maps anyway).
- Art needed: Ser Caddoc (uses `enemy_hollowed` + boss ring), a vault background (uses `hill_road`).
- Shop: the list's scroll bar no longer covers the prices (right margin inside the list box).

## Session 13: Act II playtest setup and the user's playtest requests
**Nothing from session 13 is committed yet** (all on `main`'s working tree; build clean, **140 tests pass**, content
validates on boot). Commit it first next session.

- **Playtest save:** the user's Act II save is **Slot 2** (Ch 8 hub, Casual, crest tier 2; the user has since promoted
  Aldric/Mira/Dorran and spent gold). Made with the new `--saveathub=N[:SLOT]` flag (autoplay never writes saves
  otherwise; with `--level=auto` it adds every recruit so far, the campaign level and Rowan's Steel Sword:
  `CampaignRunner.PrepareTestParty`). Backups: `slot0.before-act2-soak.json`, `slot2.autoplay-raw.json` in the save
  folder. The user's own Ch 4 saves (slots 0/1) are untouched.
- **Bug fixed: Ch 12 looped forever** (Player/Ally phases flicking) when Aldric/Seraphine *retreated*: `escape … all`
  waited for them to escape. Now only units still on the map must escape (`ObjectiveEvaluator.EscapeDone`).
- **Promotion scene** (user: "Pokémon evolution"): `UI/Nodes/PromotionSequence.cs` over the hub: battle sprite whitens,
  old/new outfits flicker faster and faster, flash, reveal with "X became a Y!" and class stat changes. Confirm skips /
  closes. `--promotiondemo=unit:class` plays it without promoting.
- **Ch 8 rioters:** when an agitator falls, the rioters he held become green Ally **Townsfolk** (villager, unarmed,
  stationary, townsfolk sprite) instead of walking off (`BattleProgress.Calm`, `CrestSevered.Calmed`). They don't count
  toward `protect civilian 4` (the user can ask to change that).
- **Hub:** "To convoy" → **"To town"** on item buttons (the Convoy counter and battle "(to convoy)" text unchanged).
  Hovering the crest name lists Rowan's crest skills.
- **Ability box** (top-right of the battle HUD, `BattleHud.ShowAbility`): explains the highlighted crest skill, combat
  art (menu or Tab while targeting) or healing spell, text built from the data (`Units/AbilityText.cs`). No hints or
  tooltips on the menu itself (user's call; nothing for the plain "Crest" entry).
- **Crest animations:** using a crest skill raises Rowan's crest over his head (bigger per tier), then rings and sparks
  in the skill's colour round each target (`RaiseCrest`, `Bless`, `UI/Nodes/CrestBurst.cs`). **Battle-start blessing**:
  before turn 1 (crest tier ≥ 1, not under autoplay) the crest rises and the whole army glows white; a show only.
  The first battle at each tier in a campaign plays `crest_blessing_<tier>` (`data/dialogue/crest.txt`; lines from
  undeployed speakers are dropped, `DialogueScene.OnlySpeakers`; seen ids go in `SeenScenes`). Skill animation is
  not yet seen on screen (autoplay never uses crest skills).
- **Breakable doors** (Ch 9, Ch 13): any armed unit beside a locked door gets **Break Door (N to go)**; 3 strikes open
  it (`BreakDoorAction`, `BattleState.DoorStrikes`, `DoorStruck` event). Keys/Pip still open at once; chests unchanged.
- **Modes:** new games default to **Casual**; Classic is renamed **Iron Man** (`GameMode.IronMan = 0`, saves store the
  number, so old saves keep their mode). Picker lists Casual first.
- **Supports slowed** (user: A not before mid Act II): at most 5 points a pair per battle (survive 2, side by side up to
  `Supports.AdjacentCap` 3); thresholds Rowan×Kai/Mira and Kai×Mira 12/30/55, Rowan×Aldric 12/32/60, Brann×Tamsin
  10/25/45 (fastest A ≈ Ch 11). Pacing rule in DIALOGUE.md §6. **Risk:** Rowan×Aldric must reach A before Ch 15 and
  needs ~4 points a battle; existing saves keep their old points.
- **Ch 5 Gloam Alpha** ~30% less damage: Alpha Claws Mt 8 → 4, class base Str 13 → 11 (hits Tamsin 15, was ~21).
  Autoplay still loses Ch 5 often (long-standing).
- Dev: CLAUDE.md documents `--saveathub`. Autoplay soak from a new game reached Ch 12 before the fix above.

## Next (plan for session 14; done or carried into session 15)
1. **Commit session 13** (ask about pushing).
2. Keep **hand playtesting Act II** from Slot 2 (Ch 8 onward): the calmed townsfolk, the tier-2 blessing scene,
   Break Door in Ch 9, the promotion scene, the ability box, crest skill animations (not yet seen).
3. Decide with the user: calmed rioters counting toward Ch 8's civilian losses; guaranteeing Rowan×Aldric A before
   Ch 15; renaming "Convoy" elsewhere to "Town"; resetting Slot 2's support points to the new pacing.
4. Then the session-12 list below: story-critical units, the Old Warden Vault, the Act II hub, Crestbound arts, art,
   Act III.

## Session 12 (late): Act I playtest notes
- **Fog of war** is now drifting layered fog over the hidden cells (the user's pick, "Eerie mist": a dim floor and two
  pale violet-grey mist layers), drawn as map-wide planes sampling a one-pixel-per-cell mask with linear filtering
  (soft edges) and pulled toward the camera between the props and the units, so it covers the trees in its cells
  (`FogLooks`, `FogPlane`, `BattleController.DefaultFogStyle = 2`). `--fogstyle=N` shows the others (0 the old flat
  squares, 1 dark murk, 3 green ghost fog, 4 purple shroud); the pick sheets were Ch 3 and Ch 9.
- **Ch 4:** the toll-gate palisade along the north is raised into real stone walls (height 2), and the Dain Hall troops
  are mixed: 5 Sellswords (iron sword), 4 Axemen (iron axe), 5 lance soldiers, the archers and the sergeant (hand edit of
  `ch4.tres`; `act1_early.py` notes it). Ch 7 was mixed last session; Ch 6's Dain Hall units are the pegasus scouts.
- **Gold:** +150 a battle (`VictoryGold` = 300 + 50 × chapter).
- **Story-critical units retreat** (the user's answer to "what if Aldric dies before Ch 15?"): `UnitDef.Retreats`
  (Aldric, Kai, Mira, Seraphine) at 0 HP leave the field wounded (`MapExit.Retreated`, `UnitRetreated`, a
  `retreat_<id>` line from `data/dialogue/retreats.txt`) and are back next chapter, even in Classic. Where a map says
  the unit must survive (`die KEY`, e.g. Aldric in Ch 15), falling still loses. Other companions keep permadeath.
- **Soak:** the full campaign ran from a new game into Act II with no script errors, then stuck on Ch 12 (10 losses):
  autoplay never promoted anyone in the hub, and on escape maps walked the unarmed Seraphine at the guards. Autoplay
  now promotes everyone eligible in the hub and shelters unarmed units until it's time to leave.

## Session 12 (later): Act II built, systems first
The user asked to build Act II with the maps scaled like Act I (+60% per side), systems and functionality first, art
placeholders until PixelLab credits come. **All eight chapters (Ch 8–15) are built and chained in the campaign**;
`data/campaign/main.txt` now ends with the Act II text after Ch 15. Maps come from `tools/maps/act2_early.py` (also
the Act II content: door/chest terrain, keys, crossbow, classes, Pip/Seraphine/Ysra/Halde/Ysolde); dialogue in
`data/dialogue/ch08.txt`–`ch15.txt` plus `hub_briefing_ch08`–`ch15` in `hub.txt`. CHAPTERS.md has a "built" entry per
chapter; DIALOGUE.md §2 has voices for Pip, Ysra, Halde, Castellan, Vossk and Ysolde.
- **New systems (all with unit tests, 134 in total):**
  - **Neutral faction** (Ch 8 rioters): its own phase after the enemy's; only starts fights with Ally units.
  - **Shared-key Dominion**: several bearers can share a key; a fallen non-crest holder calms only the units round it
    ("Calmed!"); `boss KEY` needs every unit with the key down; `defeated KEY` fires for any of them.
  - **Per-spawn `Ignores`** (keys the AI leaves alone while the player doesn't command them), **`DominionRadius`**,
    **`NonLethal`** (subdued at 0 HP, `subdued KEY` trigger, freed not sent away by severance), **`Invulnerable`**
    (no damage, "—" in the forecast, the AI never targets it). The map writer now also writes `Leash` (it had been
    dropping it: Ch 9–11's boss leashes apply now).
  - **Locked doors and chests** (`D`/`C` terrain), the **Unlock** action (Thief's Locktouch or single-use Door/Chest
    Keys, sold in Calder), `chest X,Y => ...` triggers, and the Thief's **Steal**.
  - **Victory grammar:** `... or boss KEY` (Ch 10), `escape CELLS all` (everyone out, the lord last; units a script
    removed don't count), `talk a|b TARGET` (several actors).
  - **Classes:** Thief/Rogue, Troubadour/Valkyrie, Wyvern Rider/Lord, Hollowed; the missing tier 2 **General** (lance
    and axe), **Swordmaster**, **Falcon Knight** (Dorran, Sable and Wren couldn't promote after Ch 7); thirteen tier 3
    **Crestbound** classes, one per companion (`UnitDef.Crestbound`, offered from tier 2 at crest tier 3; signature
    arts still to design). Art falls back through earlier outfits (`Outfits.Earlier`).
  - **Placeholders:** `BattleUnit.PlaceholderSprites` (stand-in looks, then the class look) and
    `UiKit.PlaceholderBackgrounds`, so data uses the real ids today. `Lighting = "snow"` (cold wash + snowfall).
  - **Audio:** synthesized `wing_beat`, `sword_fall`, `crest_corrupt` and the `loss` lament.
  - **Dev:** single-map tests use the campaign's crest tier for the map, and `--level` promotes at 10 when the crest
    allows (an Act II party is tier 2); `--map=... --autoplay` quits after the result; content validation failures
    stop the game booting (the unit tests can't load .tres files, so boot once after data changes).
  - **Autoplay:** a lone lord presses on to a seize tile too (the soak stuck on Ch 7 at T61); must-survive healers heal
    from safety; escape-all maps fight, then leave (in a hurry under a turn limit), the lord waiting beside the exit.
- **Story additions to the outline:** Ch 11's end sails south to Solmere (Vossk's crests were bought there); Halde's
  Gull carries the party in Ch 12–14; Castellan's last words ("He drinks them, boy. I only sold him the cups.") lead
  to Elise's pattern scene; Osmund raises his voice (the once) at Morvane in Ch 15's reveal.
- **Art (end of session, after the user added $10):** Act II portraits, the new cast's, bosses' and three common
  enemies' map + battle sprites, and the ten Act II backgrounds are in (ART_NEEDED §0 "Done"). Still on stand-ins:
  agitators, Veiled thugs/mages, imperial and Gilded troops, townsfolk/rioters, Calder guards, sailors, tier 2/3
  outfits, snow tiles, chest/ship props. Not yet checked in game (map scale of the wyverns and Castellan).
- **Art (during the Act II build):** no PixelLab credit spent at first (**~$2.21 left** then). Everything Act II needs is listed with its
  stand-in in `docs/ART_NEEDED.md` §0 (portraits, the new cast's sprites, Veiled/imperial/Gilded/Hollowed enemies,
  Calder/Solmere/Dravenhold backgrounds, snow tiles, chest and ship props, tier 2/3 outfits).
- **Autoplay results** (single maps at campaign level, promoted): every Act II map is won by autoplay; Ch 8–9 cost
  3–8 units (an unpromoted level 9–10 party), Ch 10–14 0–4 (probably easy for a human), Ch 15 wins 3/4 (tight escape).

## Session 12: polish before Act II
- **Ch 7 garrison art:** new `enemy_sellsword` and `enemy_axeman` looks (deep green Dain Hall tabard over mail, purple
  Dominion glow in the eyes, like the Ch 7 knight), map sprites with walk/attack plus E-look battle sprites (user's
  picks: still take #0 of each, in `approved/battle_picks.txt`). The 7 Ch 7 Sellswords/Axemen use them (`SpriteId`).
  Effect frames cleaned by hand: the white slash arcs on the sellsword's attack frames 2–5 and the axeman's critical
  frames 4, 5 and 9 were erased (near-white blobs plus specks), and the sellsword's last critical frame (a yellow orb)
  was dropped. About $0.58 spent; **~$2.21 PixelLab credit left**.
- **`tools/maps/act1_ch7.py` is back in step with `ch7.tres`** (smith's house, highlights, sellswords and axemen, no
  rear cavalry, the new sprite ids): rerunning it reproduces the map byte for byte. `map_tres` in `act1_ch4_6.py`
  takes an optional `highlights` list (Ch 4–6 output unchanged).
- **Suspend saves** (pause menu → Suspend, campaign battles only): `SuspendData` (`user://saves/suspend.json`) keeps
  the session as it was when the battle began, the battle's seed and deployment, and `BattleState.CommandLog`
  (commands, battle actions and action choices carry `[JsonDerivedType]` tags). The title screen shows
  "Resume: <chapter> · Turn N" first while one exists (`CampaignFlow.Resume`, dev flag `--resume`); the battle is
  rebuilt and `TurnManager.Replay` resubmits the log with no visuals, then `BattleController.ShowResumed` restores
  done tints, statuses and phase music. If a command no longer fits (content changed), the battle starts over. One
  suspend at a time; a campaign battle that ends, is retried or is quit clears it. Tests: commands round-trip
  through JSON, and an AI-played battle suspended halfway replays to the same state (RNG included). Checked in game:
  Prologue suspended on T2 after 11 commands and resumed identically.
- **Knights with axes:** skipped; it was only wanted if armoured enemies still felt lance-heavy, and the new Ch 7 mix
  playtested well.

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
  Retry Battle moved here from the map menu's Settings (user request), so the pause menu is Return / Settings /
  Retry Battle / Quit to Title.
- **Lances pierce** (user request): against an Armored target (trait or movement type) a lance ignores half its Def
  (`CombatCalculator.LancePiercePercent`, terrain Def still counts); weapons already effective against it (Heavy
  Spear) keep ×3 instead. The forecast shows "!" for it. It works both ways: enemy lances pierce Aldric (warden).
- **Prologue Hounds** (playtest: too easy to lose Rowan): the Prologue's hounds carry `gloam_fangs_young` (Mt 3, was
  Fangs 5), so they bite Rowan for 4, not 6. Later hounds are unchanged (`act1_early.gloam` picks the weapon).
- **`--level=N|auto`** (DevTools): single battles raise the player side to a campaign-like level with average gains
  (`LevelUpService.RaiseToAverage`); `auto` uses `DevTools.CampaignLevels` (Prologue 1 … Ch 7 8). Ch 7 at level 8:
  autoplay wins 2/2 (T20, T13) but loses Aldric on T3 both times; Brann and Tamsin do 0 to Halvric, Elise's
  Thunder does 20.
- **Single battles match the story** (playtest: Mira could only heal, Rowan was still a Youth): `--map` battles apply
  the campaign's `class` steps before the map (`BattleController.ApplyStoryClasses`: Rowan Lord, Kai Cavalier) and,
  after Ch 1, `StoryItems` (Mira's Fire; a Steel Sword for Rowan, the user's testing pick over the tour's Iron).
- **Ch 7 enemy mix** (playtest: almost every melee enemy had a lance): four soldiers are now **Dain Hall Sellswords**
  (myrmidon, iron/steel sword), three **Dain Hall Axemen** (fighter, iron/steel axe), one courtyard cavalier carries
  an iron sword; the T6 rear cavalry ambush and `ch07_rear` were cut. Edited in `ch7.tres` directly (the generator
  `act1_ch7.py` is out of date for Ch 7). They use the bandit-style myrmidon/fighter art for now.
- **Gold** checked with the user: fine for now (table: 150 + 50 × chapter plus story gold, ~5,000 over Act I).
- **Dev:** `--focus=X,Y` starts the battle camera on a cell (with `--hideunits` for map-art screenshots).

## Where things stand (end of session 12)
- **Acts I and II are playable end to end** (Prologue to Ch 15); Act II is new and **not hand-playtested yet**. All on
  `main`, committed, **not pushed** (the user asked to commit; pushing wasn't confirmed).
- **Tests:** 134 pass. Content validates on boot.
- **PixelLab:** about $2.21 credit left; Act II runs on placeholders.

## Next (plan for session 13; item 1 started, the rest carried into session 14)
1. **Hand playtest Act II** (`--map=ch8` … `ch15` with `--level=auto`, or a campaign save at Ch 7): balance (Ch 8–9
   hardest, 10–14 maybe easy), the riot's neutral phase, doors/chests/Steal with Pip, Seraphine's escort, the cliffs,
   the Gull escape, the vault, the Empress (subdued, not killed), and Ch 15's door.
2. **Decide with the user:** story-critical units in Classic (Seraphine leads Calder in Act III, but can die in
   Ch 10–15 today: retreat instead of dying?); the **Old Warden Vault** side map for Lyra's Blade ("Rowan's weapon from
   Ch 15 on", needs Aldric, so before Ch 15: not built); the Act II hub (still Brookhollow's hall; `calder_lodgings`
   is a placeholder); Crestbound signature arts.
3. **Art** when credits come: ART_NEEDED §0, portraits and the new cast first.
4. **Act III** ("The Hollow Path", Ch 16–22) after that.

## Where things stood (end of session 11)
- **Act I is playable end to end and hand-playtested.** Everything from sessions 10 and 11 is merged into `main` and
  pushed to GitHub (`43b16002`); `act1-scale` is merged (fast-forward) and only exists locally. The repo is on `main`.
- **Windows build** re-exported to `build/windows/The Radiant Crest.exe` + `.pck` (gitignored) from `43b16002`; it
  boots and loads all content.
- **Tests:** 120 pass. Full autoplay soak reaches CAMPAIGN COMPLETE with no script errors.
- **PixelLab:** about $2.85 credit left.

## Next (plan for session 12, done)
1. **Testing workflow:** single battles at campaign strength with `--map=<id> --level=auto` (story classes and items
   included); `--level=auto` on a normal launch also applies to Battle Select. Retry from the Esc menu.
2. **Polish:** done in session 12 (garrison art, `act1_ch7.py` ported); knights with axes skipped.
3. **Suspend saves:** built in session 12. Art wish list in `docs/ART_NEEDED.md`.
4. **Act II, "The Burning Crowns"** (Ch 8 "Riot in the Capital" to Ch 15, in `docs/story/CHAPTERS.md`): the
   campaign currently ends with the Act I text after Ch 7. Building Act II is the next big piece of work once the user
   wants it (maps, dialogue, new units and art; crest tier 2 arrives after Ch 7, so tier-2 promotions open here).

## Plan for session 11 (history: all done in session 11; the crest-fragment idea was dropped)
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
