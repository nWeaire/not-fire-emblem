# Chapter & Mission Plan

Every story mission, in order. Each entry is written so it can be turned directly into a `MapDefinition` (objectives, spawns, triggers) plus `DialogueScene`s. Story context is in [`STORY.md`](STORY.md); the scripts are in [`DIALOGUE.md`](DIALOGUE.md).

**Entry format**
- **Map:** setting, size, key terrain, and fog on/off.
- **Party:** who is deployed (forced units in **bold**).
- **Win / Lose:** objectives, using the `ObjectiveEvaluator` vocabulary (Rout, Defeat Boss, Survive N, Seize, Escape, Defend), plus any planned additions.
- **Enemies:** composition and AI profiles (A = Aggressive, S = Stationary, G = Guard, B = Boss).
- **Events:** `EventDirector` triggers (turn, region, death, flag).
- **Recruit / unlock:** new units, classes, items, crest changes.
- **Scenes:** dialogue scene ids (`ch01_pre`, `ch01_mira_fire`, …), named after the scene's purpose.

Default loss condition on every map: **Rowan dies**. Extra loss conditions are listed per chapter.

---

## Prologue — "Embers at Ashford Farm"
- **Map:** 22×16 (enlarged in session 7). The farmhouse sits on a gentle rise with the barn, a pond and the fields round it; the treeline lies to the north. Night (visual only). Height: the house sits on a rise (+1). No fog.
- **Party:** **Rowan** (Youth, the family's **Old Sword** (Mt 3; Osmund has him buy an Iron Sword at the first hub visit), in farm clothes), **Aldric** (Warden, Steel Sword, Warden's Hide).
- **Win:** Defeat Boss (the Binder). **Lose:** Rowan or Aldric dies.
- **Enemies:** 5 Gloam Hounds (A), 2 Gloam Brutes (A) plus one guarding the barn (G), the Binder (B, Shadow tome, shrouded crest, Dominion radius 3).
- **Events:**
  - T1, start (`pro_tutorial_move`): tutorial on moving.
  - T1, Rowan in range of an enemy (`pro_tutorial_attack`): tutorial on attacking and the forecast.
  - T2, start (`pro_aldric_reveal`): Aldric's armour reveal (he starts as a guest Wait-locked for a turn for drama, then joins).
  - A Gloamkin dies near the Binder: tutorial bubble on Dominion.
  - Binder dies → `pro_crest_awakens`: all remaining Gloamkin are Stunned, and the battle ends once they are routed. *(Build note: if severing is out of scope for the first build, simply make Defeat Boss end the map.)*
- **Recruit / unlock:** **Kindled Crest (Tier 1)**, crest skill **Ward**.
- **Scenes:** `pro_opening` (the evening on the farm), `pro_dogs` (the attack begins), `pro_aldric_reveal`, `pro_crest_awakens`, `pro_smoke` (glow over Brookhollow).

## Chapter 1 — "Brookhollow Burns"
- **Map:** 26×19 village (enlarged in session 7). A river runs south with three bridges; the chapel is across it to the east, the well square in the centre, cottages in a ring. Several houses are **burning** (impassable fire tiles that clear at T4). No fog.
- **Party:** **Rowan**, **Aldric**, **Kai** (Soldier, ally-controlled until reached), **Mira** (Acolyte, starts in the chapel).
- **Win:** Rout. **Lose:** Rowan dies, or 3 of 5 **villagers** (green NPCs) die. *(New objective: Defend/Protect NPCs.)*
- **Enemies:** 6 Gloam Hounds, 2 Brutes, 3 Gloam Stalkers (spike-throwers, range 2); Binder Sennet (B) at the north gate. Reinforcements: 3 Hounds from the north road on T4.
- **Events:**
  - Kai is a **Player** unit from the start, holding the well (`ch01_kai_joins` plays on turn 1; changed in session 7 for balance).
  - T3 (`ch01_mira_fire`): a Brute breaks into the chapel courtyard and Mira casts **Fire**. She gains the Fire tome; Kai's reaction is the comic beat.
  - T4 (`ch01_reinforcements`): fire tiles burn out and new enemies come from the north road.
  - Visit Elder Wenna's house: a Vulnerary + `ch01_wenna`.
- **Recruit / unlock:** Kai, Mira. **Brookhollow hub** opens after the map.
- **Scenes:** `ch01_pre`, `ch01_kai_joins`, `ch01_mira_fire`, `ch01_reinforcements`, `ch01_post` (Wenna: "protector").

## Interlude — "The Steward" (hub introduction, no battle)
- **Where:** Ashford Farm, then the Brookhollow village hall.
- **What happens:**
  - Osmund Thorne is introduced and becomes the hub advisor.
  - Rowan changes class from **Youth → Lord** (his new outfit).
  - Hub tutorial: Osmund walks through the Party, Convoy, Smithy (locked until Brann joins), Supports and Save screens, then briefs Ch 2.
- **Recruit / unlock:** Osmund (hub NPC; he never fights); Rowan's lord outfit; the hub's **Briefing** screen.
- **First visit tour (session 7):** Osmund shows the places, then has Rowan buy and equip an Iron Sword in place of the old farm sword (`HubScreen` tour; flag `tour_hub`).
- **Scenes:** `hub_intro`, `hub_outfit`, `hub_osmund_aldric`, `hub_briefing_ch02`.

---

## ACT I — THE THORNVALE

## Chapter 2 — "The Millbrook Road"
- **Map:** 29×19 (enlarged in session 7). Roads on both banks of the river and three bridges (chokepoints); the mill sits at the far north-east end. Brann's forge is on the south road two moves from the deploy slots, with the south-bank bandits further east, so the party reaches him as they do (moved in session 7 after a playtest). Some bandits are **Axe Throwers** (hand axes, range 1-2).
- **Party:** up to 5. **Win:** Seize the mill. **Lose:** default.
- **Enemies:** 13 bandits (fighters, archers, sellswords; A, with the mill guard on G/S) plus a 3-man ambush on T3, and a Bandit leader with a Hand Axe (B, on the mill).
- **Events:** Brann starts as an **Ally** defending his forge, and joins when Rowan *Talks* to him (`ch02_brann`). If Brann is left alone for 3 turns, he takes damage (tension). Two village houses can be Visited for items.
- **Recruit / unlock:** **Brann**; the hub **Smithy**. *(New commands: Talk, Visit.)*
- **Scenes:** `ch02_pre` ("bandits move in when the lord's men don't"), `ch02_brann`, `ch02_post`.

## Chapter 3 — "Thornwood Hunt"
- **Map:** 32×22 forest (enlarged in session 7), mostly forest tiles with clearings joined by hunters' trails: deploy (south-west), Tamsin's (centre), the dead dogs' (south-east) and Varga's round a ruined hunting lodge (north-east); two rock outcrops, a pond and a stream. **Fog of war ON.**
- **Party:** up to 6. **Win:** Defeat Boss (Binder Varga). **Lose:** default.
- **Enemies:** Gloam Stalkers in the trees (G), Hounds (A), and Binder Varga (B, Dominion), who moves between clearings.
- **Events:**
  - T2: Tamsin appears as a green unit fighting alone, and joins when Rowan Talks to her (`ch03_tamsin`).
  - Finding the dead hunting dogs region → `ch03_dogs` (her motivation).
  - When Varga dies, Elise's name appears in Varga's orders ("intercept the archive scholar").
- **Recruit / unlock:** **Tamsin**; first crest fragment.
- **Scenes:** `ch03_pre`, `ch03_tamsin`, `ch03_dogs`, `ch03_post` (the orders point to Elise and to Lord Halvric's men).

## Chapter 4 — "The Scholar of Calder"
- **Map:** 26×26 hill road (session 8): the road runs north to south through a toll-gate palisade, with woods and rocky hills on the flanks and the waystation (forts) to the south-east. Elise starts mid-road; the party starts 3–4 cells from her.
- **Party:** up to 6. **Win:** **Escape**: Elise reaches the south exit, and Rowan must exit too. **Lose:** default, or Elise dies.
- **Enemies:** Lord Halvric's soldiers and 2 cavaliers. Their eyes are wrong: they never speak and they fight to the death. They come in from the north each turn.
- **Events:** Elise starts as an Ally NPC and joins on Rowan's Talk (`ch04_elise`). The T3 bark `ch04_soldiers` reveals they are "under orders from Dain Hall".
- **Recruit / unlock:** **Elise**. The post-map scene (`ch04_lore`) is the big crest lore dump at the hub.
- **Scenes:** `ch04_pre`, `ch04_elise`, `ch04_soldiers`, `ch04_lore`.

## Chapter 5 — "Ferrow's Keep"
- **Map:** 29×22 border fort (session 8): a walled courtyard with a hall and two barracks for cover, open to the west; the east wall has a breach and a gate that breaks on T5. Gloamkin pour from the east field in waves on T2–T6.
- **Party:** up to 7, plus Dorran pre-deployed. **Win:** Survive 8 turns. **Lose:** default, or Dorran dies.
- **Enemies:** waves of Gloamkin, plus a Gloam Brute Alpha (B) and a Binder at the rear. Killing the Binder makes the waves stop early.
- **Events:**
  - T1 (`ch05_dorran`): Dorran sees Aldric's armour and keeps quiet.
  - T5: the gate breaks.
  - Severing the Binder ends the map early with a bonus.
- **Recruit / unlock:** **Dorran**. After the map, the hub event `hub_merrow_horses` lets **Kai** change class to **Cavalier**.
- **Scenes:** `ch05_pre`, `ch05_dorran`, `ch05_gate`, `ch05_post` (Dorran to Aldric, privately: "I thought the Wardens all burned.").

## Chapter 6 — "The Bandit King of Greyfen"
- **Map:** 32×22 marsh (session 8): a deep channel winds north to south with three boardwalk crossings; shallows (slow), reeds (avoid) and stilt houses everywhere else. Rusk's hall is north-east, Sable mid-fen; the bandits guard their ground.
- **Party:** up to 8. **Win:** Defeat Boss (Rusk). **Lose:** default.
- **Enemies:** bandits, archers, Sable (a myrmidon), and Rusk (B, a stolen shrouded crest, Dominion over the bandits).
- **Events:**
  - Kai Talks to Sable (`ch06_sable`) and she joins.
  - Rusk dies → `ch06_rusk`: his crest goes to Rowan, and the bandits under Dominion surrender.
  - **Pegasus scouts** fly over on T5 and leave without fighting (foreshadowing).
- **Recruit / unlock:** **Sable**; crest fragment.
- **Scenes:** `ch06_pre`, `ch06_sable`, `ch06_rusk` ("Crests can be stolen?" "Crests can be *taken*."), `ch06_post`.

## Chapter 7 — "Storm over Dain Hall"
- **Map:** 20×16 castle with a courtyard, ramparts (height +2) and the throne room.
- **Party:** up to 10. **Win:** Seize the throne. **Lose:** default.
- **Enemies:** dominated soldiers and knights, a pegasus patrol including Wren, and Lord Halvric (B, a shrouded crest; Dominion over his whole army).
- **Events:**
  - T2: Wren hesitates. Rowan Talks to her (`ch07_wren`) and she joins.
  - When Halvric's HP is below 50%: `ch07_halvric_plea`.
  - Seizing the throne with Halvric dead triggers `ch07_severance`: every soldier on the map surrenders.
  - Halvric's death scene (`ch07_halvric_dies`) names the Chancellor.
- **Recruit / unlock:** **Wren**, and the **Knight's Crest (Tier 2)**: promotions, the crest skill **Rally**, and Aldric's Warden Lord.
- **Scenes:** `ch07_pre`, `ch07_wren`, `ch07_halvric_plea`, `ch07_halvric_dies`, `ch07_post` (the people ask Rowan to rule, and he refuses the title), `hub_act1_end`.

---

## ACT II — THE BURNING CROWNS

## Chapter 8 — "Riot in the Capital"
- **Map:** 20×16 city square with market stalls and alleys. Rioters are a new **neutral** faction.
- **Win:** Defeat the 4 agitators (Veiled agents in disguise). **Lose:** default, or 4 civilians die.
- **Events:** the agitators wear purple sigils. Morvane's first appearance is in `ch08_post`, where he thanks Rowan warmly. Aldric leaves the room.
- **Scenes:** `ch08_pre`, `ch08_agitator`, `ch08_post_morvane`, `ch08_aldric_leaves`.

## Chapter 9 — "Beneath the Crown"
- **Map:** 18×18 undercity with locked doors, treasure rooms and sewers. **Fog ON.**
- **Win:** Seize the Veiled archive. **Recruit:** **Pip** (green, joins on Talk).
- **Unlock:** doors and chests (the Thief's Steal/Unlock). The Veiled letters with purple seals start the conspiracy thread.
- **Scenes:** `ch09_pre`, `ch09_pip`, `ch09_letters`.

## Chapter 10 — "The Border of Ash"
- **Map:** 22×14 border pass. A burning village sits mid-map.
- **Win:** Survive 10 turns, *or* defeat the imperial commander. **Lose:** default, or Seraphine dies.
- **Events:** Seraphine's escort is ambushed; she joins at T1 as a Player unit with **protect** status. In `ch10_insult`, a captured imperial reads the forged insult that started the war.
- **Recruit / unlock:** **Seraphine** (Troubadour; the second crest-bearer).
- **Scenes:** `ch10_pre`, `ch10_seraphine`, `ch10_insult`, `ch10_post`.

## Chapter 11 — "Wings of Dravenhold"
- **Map:** 20×20 mountain pass with cliffs (only fliers can cross) and narrow paths.
- **Win:** Rout. **Enemies:** wyvern riders and imperial knights. **Recruit:** **Ysra** (Talk, any crest-bearer: Rowan or Seraphine).
- **Scenes:** `ch11_pre`, `ch11_ysra` ("The Empress doesn't blink anymore."), `ch11_post`.

## Chapter 12 — "The Gilded Harbor"
- **Map:** 22×14 harbour; ships are linked by gangplanks (bridges), with water between.
- **Win:** Escape (the whole party reaches the ship). **Recruit:** **Halde** (Talk on her ship).
- **Events:** a crest auction in the background (`ch12_auction`) sets up the horror of a crest *market*.
- **Scenes:** `ch12_pre`, `ch12_halde`, `ch12_auction`.

## Chapter 13 — "The Merchant Prince"
- **Map:** 20×16 palazzo with gardens, a vault and a balcony.
- **Win:** Defeat Boss (Prince Castellan, who bears *three* shrouded crests; Dominion radius 4).
- **Unlock:** **Lord's Crest (Tier 3)**: Crestbound classes and the crest skill **Mend**. In `ch13_elise_pattern`, Elise realises every shrouded crest drains toward the Ashen Reach.
- **Scenes:** `ch13_pre`, `ch13_castellan`, `ch13_elise_pattern`, `ch13_post`.

## Chapter 14 — "The Empress Unbound"
- **Map:** 20×18 imperial citadel courtyard in snow.
- **Win:** Defeat Boss (**Vossk**). Empress Ysolde fights as a dominated **enemy you shouldn't kill**: if she is reduced to 0 HP she is "subdued", not killed. *(Build note: an `IsNonLethal` flag.)*
- **Events:** Vossk dies → `ch14_ysolde_wakes`. The Empress joins as an NPC ally for the finale.
- **Scenes:** `ch14_pre`, `ch14_vossk`, `ch14_ysolde_wakes`, `ch14_post` (peace summit agreed).

## Chapter 15 — "The Grey Chancellor"
- **Map:** 16×14 great hall in Calder: pillars, a dais, side doors to escape through.
- **Party:** everyone the player chooses, plus **Aldric** (forced).
- **Win:** Survive 5 turns, *then* `ch15_aldric_stays`. The rest of the army escapes through the side doors. **Lose:** Rowan dies, or the turn limit passes with Rowan still in the hall.
- **Enemies:** **Morvane** (invulnerable; a "Boss" that cannot be damaged; every attack forecast shows "—"), and Hollowed spawning from every door.
- **Events:**
  - T1 (`ch15_reveal`): Morvane takes King Osric's crest in front of the court.
  - T5 (`ch15_aldric_stays`): Aldric holds the door. This is a scripted death scene and **Aldric is removed from the roster**.
  - End (`ch15_crest_flickers`): Rowan's crest flickers purple.
- **Scenes:** `ch15_pre`, `ch15_reveal`, `ch15_aldric_stays`, `ch15_aldric_dies`, `ch15_crest_flickers`.

---

## ACT III — THE HOLLOW PATH

The crest's state changes for this whole act: the aura is off, and Ward/Rally/Mend are replaced by **Wrath**.

## Chapter 16 — "Ashes and Oaths"
- **Map:** 18×14 ruined village, still burning after a Hollowed raid.
- **Win:** Rout. Rowan's Wrath makes him overpowered.
- **Events:** if Rowan scores 3+ kills, `ch16_kai_worried` plays. The funeral opens the chapter in `ch16_pre_funeral`.
- **Scenes:** `ch16_pre_funeral`, `ch16_vow` ("I'll find him. I'll end him."), `ch16_kai_worried`.

## Chapter 17 — "The Hunter in Grey"
- **Map:** 20×16 abbey ruins. **Fog ON.**
- **Win:** Defeat Boss (Sister Ilse).
- **Events:** Ilse's death scene (`ch17_ilse`). In `ch17_mirror`, Rowan sees purple in his crest and hides his hand. The party argument follows in `ch17_argument`, where Kai and Mira confront him.
- **Scenes:** `ch17_pre`, `ch17_ilse`, `ch17_mirror`, `ch17_argument`.

## Chapter 18 — "The Broken Crest"
- **Map:** 24×14 cliffside approach to the Hollow Spire gate.
- **Win:** Escape. Everyone except Rowan must reach the exit. **Rowan cannot escape: this is a scripted loss.**
- **Events:**
  - T3 (`ch18_crest_fails`): Rowan's crest fails and his stats drop by 30% for the rest of the map.
  - T5 (`ch18_morvane`): Morvane arrives.
  - When the escape is complete or T8 is reached, `ch18_fall` plays: Rowan falls. The friends' capture happens off-screen.
- **Scenes:** `ch18_pre`, `ch18_crest_fails`, `ch18_morvane`, `ch18_fall`.

## Chapter 19 — "The Still Meadow"
- **Map:** 14×10. A grey, silent copy of Ashford Farm, identical to the Prologue layout. Fog ON.
- **Party:** **Rowan alone**, plus **Aldric's Echo** (a guest unit at a quarter of his strength, who cannot die: he fades and returns).
- **Win:** Defeat Boss (**Shadow Rowan**, Rowan's own stats with Wrath).
- **Events:** `ch19_father` (the heart of the game); `ch19_shadow` (Shadow Rowan speaks Rowan's own lines from Ch 16–17); `ch19_crest_returns`.
- **Unlock:** the crest returns at Tier 1, with Ward only. The aura is still off.
- **Scenes:** `ch19_wake`, `ch19_father`, `ch19_shadow`, `ch19_crest_returns`.

## Chapter 20 — "The Burning Ranch" (Kai's nightmare)
- **Map:** 16×12 copy of the Merrow ranch, endlessly burning.
- **Party:** Rowan, plus Kai once he is freed.
- **Win:** Defeat the Nightmare's anchor in Kai's ranch house. **Restores:** the Knight's Crest (Rally) and the aura at +5.
- **Scenes:** `ch20_pre`, `ch20_kai_freed` ("You came back." "I never should've left.").

## Chapter 21 — "Hands of Light" (Mira's nightmare)
- **Map:** 16×12 copy of the chapel, where every NPC is wounded and the staves are broken.
- **Win:** Defend: keep 4 villagers alive for 6 turns, then defeat the anchor. Freed, Mira heals them all. **Restores:** the Lord's Crest (Mend).
- **Scenes:** `ch21_pre`, `ch21_mira_freed`.

## Chapter 22 — "The Bonds That Hold"
- **Map:** 24×18 Hollow dreamscape with linked islands. Each remaining companion is trapped on an island guarded by their own fear.
- **Win:** Free every *recruited* companion, then Seize the heart. Each rescue gives a short `ch22_free_<unit>` scene. **Unlock:** the **Sovereign's Crest (Tier 4)**: final classes, the crest skill **Dawn**, and Rowan as **Sovereign**.
- **Paralogues (optional, available from Ch 20):** one per companion (`para_<unit>`). Each is a short personal map whose rescue scene is deeper, and gives a unique weapon.
- **Scenes:** `ch22_pre`, `ch22_free_<unit>` (one per companion), `ch22_sovereign`.

---

## FINALE

## Chapter 23 — "Three Crowns March"
- **Map:** 28×20 battlefield before the Hollow Spire. Allied armies (green) fight on both flanks.
- **Win:** Seize the Spire gate. **Lose:** default, or Seraphine or Ysolde dies.
- **Enemies:** the Hollow Legion and 2 Gloam Colossi (large bosses).
- **Scenes:** `ch23_pre_speech` (Rowan's speech, which echoes Aldric), `ch23_allies`, `ch23_post`.

## Chapter 24 — "The Hollow King"
- **Map:** 18×18 top of the Hollow Spire. Four crest pylons stand at the corners.
- **Win:** Defeat Boss (Morvane, in two phases).
  - **Phase 1:** Morvane is invulnerable while any pylon stands. Destroying all four (a new objective type) opens Phase 2.
  - **Phase 2:** the Hollow King: a large boss with Dominion over the whole map.
- **Events:**
  - `ch24_morvane_p1`, and `ch24_morvane_p2` when the phase changes.
  - `ch24_choice`: Rowan understands the halves of the Prime Crest.
  - `ch24_sacrifice`: the final cutscene.
- **Scenes:** `ch24_pre`, `ch24_morvane_p1`, `ch24_morvane_p2`, `ch24_choice`, `ch24_sacrifice`, `epilogue_*`.

---

## Equipment & regional shops

Gear progresses separately from class: classes change the look, gear changes the numbers (GDD §3, *Equipment*). Each region's shop opens when the story arrives and stocks what that region could plausibly make. The data is in `data/shops/`; the items are in `data/weapons/` and `data/armor/`.

| Shop | Region | Opens | Stock highlights | Tier |
|---|---|---|---|---|
| Brookhollow Trader | Thornvale | Ch 1 (hub) | Iron weapons, Fire, Padded Gambeson, Leather Jerkin, Apprentice Robe, Lucky Charm | 1 |
| Millbrook Forge | Thornvale | Ch 2 (Brann) | Javelin, Hand Axe, Wind, Traveller's Cloak, Hunter's Gloves | 1 |
| Ferrow's Keep Armoury | Thornvale | Ch 5 | Steel Sword/Lance/Axe, Chainmail, Iron Plate | 2 |
| Dain Hall Stores | Thornvale | Ch 7 | Steel Bow, Thunder, Killing Edge, Brigandine, Iron Ring | 2–3 |
| Calder Royal Market | Calder | Ch 8 | All steel, Scholar's Robe, Chainmail, Brigandine | 2 |
| Solmere Bazaar | Solmere | Ch 12 | Silver weapons, Elfire, Mithril Mail, Sage's Vestments, Swift Boots | 3 |
| Dravenhold Imperial Forge | Dravenhold | Ch 14 | Silver Lance/Axe, Iron and Silver Plate | 2–3 |

**Unique gear (never sold):**

| Item | Owner | How it's obtained |
|---|---|---|
| **Lyra's Blade** | Rowan | Act II side map, "The Old Warden Vault" |
| **Merrow's Pride** | Kai | His family spear, reforged by Brann; hub event after Ch 6 |
| *Warden's Hide* | Aldric | Starting gear (add when Aldric's unit exists) |
| *Crest-forged gear* | — | Act III: Brann forges Tier-4 items from crest fragments at the hub smithy (to design) |

Enemies follow the same tiers: Act I bandits carry iron and leather, Halvric's garrison has steel, and Act II/III elites have silver and plate. Generic spawns can wear armour (`SpawnDefinition.Armor`).

## Paralogues & optional content
- One per companion, unlocked from Ch 20, as listed under Ch 22.
- **Act I side map, "The Merrow Ranch":** Kai's family's horses, an early Cavalier option.
- **Act II side map, "The Old Warden Vault":** Aldric and Rowan find Lyra's sword; it is Rowan's personal weapon from Ch 15 on.

## Mapping to current content
- **Built so far:** `data/maps/prologue.tres` and `ch1.tres`–`ch6.tres`, with the objectives, triggers, recruitments, visits and Dominion described above, chained by `data/campaign/main.txt`. Their scenes are in `data/dialogue/`. Chapters 4–6 come from `tools/maps/act1_ch4_6.py` (its .tres output is canonical afterwards).
  - Ch 4: Elise starts as a Stationary Ally out of the first wave's reach; Dain Hall soldiers come through the toll gate in groups on turns 2–5. Escape: Elise and Rowan through the south road.
  - Ch 5: Dorran (new `knight` class) joins on turn 1. The gate is wall cells that turn to courtyard on turn 5; a breach in the south wall is the other way in. Killing Binder Hollin wins early with 500 gold. Kai becomes a Cavalier at the hub afterwards (`hub_merrow_horses`).
  - Ch 6: new `shallows` (s), `reeds` (r) and `stilt` (h) terrain. Kai Talks to Sable; Rusk holds Dominion over his men and his crest goes to Rowan. The T5 pegasus scouts are a scene only.
  - Simplifications: Aldric starts as an Ally on Stationary AI and joins on turn 2. Ch 2's "Brann takes damage" is a single turn-3 event. Crest fragments aren't tracked yet.
- The old systems test map ("Brigands at the Ford", with Brann and Elise) is now `data/maps/test_ford.tres` (`--map=test_ford`). Brann and Elise keep their ids and join at Ch 2 and Ch 4.
- Opening content in `data/`: classes `youth`, `warden`, `acolyte`, `villager`, `gloam_hound`, `gloam_brute`, `gloam_stalker` and `binder` (Youth and Lord use Swords only: Rowan is a sword user); unit `aldric` (Warden Lv 5, Steel Sword, Warden's Hide); weapons `shadow`, `gloam_fangs`, `gloam_claws` and `bone_spikes` (range 2). Rowan starts as `youth`, Kai as `soldier` and Mira as `acolyte`.
- **Art follows class:** sprite ids are `<unitId>_<classId>`. The starting outfits are being generated: `rowan_youth`, `rowan_lord`, `kai_soldier`, `kai_cavalier`, `mira_acolyte`, `brann_fighter` and `elise_mage`. Higher-tier outfits are written in the manifest as `planned`.
- The first generic-id sprites (`rowan`, `kai`, `mira`, `brann`, `elise`) are superseded. They stay only as a fallback for the test map (e.g. Mira's current `priest` class has no outfit yet).
- Art still needed:
  - Aldric
  - Gloamkin: Hound, Brute, Stalker, Alpha and Colossus
  - Binders and Morvane
  - the Act I and Act II companions
