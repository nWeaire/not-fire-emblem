# The Radiant Crest — Story Bible

The canonical story, world and crest lore. Mission design lives in [`CHAPTERS.md`](CHAPTERS.md), and character voices and scripts in [`DIALOGUE.md`](DIALOGUE.md). When a chapter, a line of dialogue or a new unit contradicts this file, this file wins. If the story changes, update this file first.

Working title of the story: **Crest of the Lords**.

---

## 1. Logline

A farm boy from the edge of the world inherits a crest of power on the night monsters burn his home. He uses it to free his homeland from lords who rule by force, and then a continent. His father dies, and Rowan nearly becomes the tyrant he swore to stop. In the end he gives up the power of crests forever, so that no one can be bent to another's will again.

## 2. Themes

| Theme | How the game expresses it |
|---|---|
| **Leading vs. ruling** | A crest used to *lift* others shines white. Used to *bend* them, it turns purple. Rowan's crest skills buff and heal allies; enemy crests dominate their own troops. |
| **Inheritance** | Rowan inherits his father's sword, his secrets and his duty. He also inherits the crest of the first man he kills. Every crest he claims carries a little of its last bearer. |
| **Revenge is a corruption** | After Aldric dies, Rowan's crest literally darkens. The mechanics punish the revenge arc: his skills fail and his friends fall away. |
| **Bonds are power** | The crest is restored by his friends, not by a stronger weapon. Supports and companions are the story's answer, not just a stat bonus. |
| **Power should end with the righteous** | The ending gives up the crest system instead of "using it well". Rowan's last act as a lord is to make lords unnecessary. |

Tone: earnest, warm, with dry humour between friends. It gets dark in Act III but never grimdark. Villains are people with reasons, except the Gloamkin, which are tragedies.

---

## 3. The World of Aldmere

```
                    DRAVENHOLD EMPIRE (north)
                 militaristic, wyverns, iron, snow
                              |
   KINGDOM OF CALDER ---------+--------- THE ASHEN REACH (east)
   (west, farmland, rivers)   |          ruins of the First Kingdom,
     └─ Thornvale (home)      |          where the Gloamkin come from
                              |
                  FREE CITIES OF SOLMERE (south)
                 merchant princes, harbours, crests for sale
```

- **Kingdom of Calder.** Old, rural and tired. King Osric is ageing and listens to his Chancellor. The **Thornvale** is a remote river valley on Calder's eastern edge, governed by **Lord Halvric Dain**. It is a week's ride from any city, so news arrives late and help later.
  - **Ashford Farm:** Rowan's home, on a hill above the river.
  - **Brookhollow:** the nearest village and the game's **hub**.
  - Other Thornvale settlements: **Millbrook** (mill and forge), the **Thornwood** (old forest), **Ferrow's Keep** (border fort), **Greyfen** (marsh), and **Dain Hall** (the lord's seat).
- **Dravenhold Empire.** Disciplined and proud, led by **Empress Ysolde**. Its wyvern riders patrol the mountains. Recently it has become aggressive toward Calder, for reasons nobody can explain.
- **Free Cities of Solmere.** A league of harbour cities run by merchant princes. Here crests can be *bought*: bearers are bribed or killed for them. This makes Solmere the most corrupted place in Aldmere, and also the richest.
- **The Ashen Reach.** The grey wasteland where the First Kingdom fell. Its ruins hide the **Hollow Spire**. Gloamkin pour out of it in growing numbers.

---

## 4. Crests — the Power System

### 4.1 Lore
- A thousand years ago, **Aurelan the First King** bore the **Prime Crest**. At his death it shattered into many lesser crests: the **Crests of the Lords**. Ever since, every recognised ruler in Aldmere has borne one, from village reeves to emperors.
- **A crest passes to the one who ends its bearer.** When a bearer dies by another's hand, the crest leaves them and burns itself onto the victor's hand. If a bearer dies peacefully, it goes to the heir they choose. This is why lords fear assassins, why bandits hunt crest-bearers, and why Solmere's crest markets exist.
- **A crest takes the colour of the heart that holds it.**
  - **Radiant (white–gold):** used to protect, strengthen and heal those who follow you. This is the old ideal of lordship.
  - **Shrouded (dark purple):** used to command, compel and dominate. A shrouded crest can force lesser wills to obey. Soldiers stop feeling fear, and beasts stop being beasts.
- **The secret (revealed in Act II):** the Prime Crest didn't only shatter. Its *shadow half*, the **Hollow Crest**, survived intact. Every crest that turns shrouded quietly feeds power to it. Someone has been feeding it on purpose.

### 4.2 Crest tiers (story progression = gameplay progression)
Rowan's crest grows by absorbing the crests of the major foes he defeats. Each tier is a story milestone, and each one unlocks class tiers and crest skills for the whole army.

| Tier | Name | Reached | Unlocks |
|---|---|---|---|
| 0 | Unmarked | Start of the game | — |
| 1 | **Kindled Crest** | Prologue (defeating the Binder) | Base classes; crest skill **Ward** |
| 2 | **Knight's Crest** | Ch 7 (defeating Lord Halvric) | First promotions (advanced classes); crest skill **Rally** |
| 3 | **Lord's Crest** | Ch 13 (defeating Prince Castellan) | **Crestbound** classes (unique third tier per companion); crest skill **Mend** |
| — | *Shrouded* (story state) | Ch 15–18 | Crest skills degrade into **Wrath**; at Ch 18 the crest **fails** entirely |
| 4 | **Sovereign's Crest** (King tier) | Ch 22 (all bonds restored) | Final classes; crest skill **Dawn**; Rowan becomes **Sovereign** |

### 4.3 Crest rules in gameplay
- **Crest skills** are Rowan's personal command abilities. They use the same uses-per-map system as arts and spells (GDD §3).
  - **Ward:** one adjacent ally gets +4 Def/Res until the next player phase.
  - **Rally:** allies within 2 tiles get +2 to all stats for one turn.
  - **Mend:** allies within 2 tiles heal 30% of their max HP.
  - **Dawn:** one ally who has already acted this turn may act again, once per map.
  - **Wrath** (shrouded, Act III only): Rowan gets +5 Str and +20 Crit, but allies within 2 tiles get −10 Avoid ("they're afraid of him").
- **Crest aura:** while Rowan's crest is radiant, allies within 2 tiles of him get +5 Hit and +5 Avoid, scaled by tier (+5 per tier). Act III removes this aura; restoring it is the payoff of the trials.
- **Enemy shrouded crests:** enemy crest-bearers (Binders, corrupt lords, bandit kings) project **Dominion**.
  - **Dominion:** enemies within 3 tiles of the bearer get +2 Str/Def and never retreat or flee.
  - **Severance:** when a crest-bearer dies, the units it dominated are severed. Beasts become **Stunned** (they skip their next phase), and dominated humans **surrender**: they leave the map and count as defeated, but give no EXP. This makes "kill the controller" a constant tactical choice on every map with a crest-bearer.
- **Claiming:** defeating a crest-bearer boss with Rowan grants a **crest fragment** (bonus crest EXP). Defeating it with anyone else still works for the story: the crest passes to Rowan anyway, because the radiant crest pulls it in. There is no need to force a Rowan kill.
- **Class tier gates:** a promotion needs Lv 10 *and* `crestTier ≥ class tier`. The crest is the story's way of gating the class tree.
- **Equipment is not crest-gated.** Weapons, armour and accessories progress through regional shops and story rewards, separate from class and look. See GDD §3 *Equipment* and the shop table in `CHAPTERS.md`. Osmund's line on it: *"The crest decides what you are, my lord. The armourer decides whether you survive being it."*

---

## 5. Factions & Antagonists

| Faction | What they are | In-game role |
|---|---|---|
| **Gloamkin** | Humanoid beasts out of old fairy tales. They are the descendants of the First Kingdom's people, warped by the Hollow Crest when it fell. They are mindless unless dominated, and they whimper when severed. | The "monster" enemies across the whole game. The tragic reveal comes in Act II. |
| **The Veiled** | Cloaked crest-bearers serving the Grey Chancellor. Their leaders are **Binders**, low-ranking bearers with small shrouded crests stolen from murdered village reeves. | Controllers. They are almost always the priority kill. |
| **Lord's armies** | Ordinary soldiers of corrupt lords, often dominated against their will. | The human enemies of Acts I–II. Many surrender when severed. |
| **Bandits** | Opportunists who have stolen a crest or two. | Act I filler and comic villains, plus the Bandit King boss. |
| **Hollowed** | Soldiers and Gloamkin fully consumed by the Hollow Crest. They are grey, silent and relentless. | Act III / finale enemies. |

### The Big Bad — Morvane, the Grey Chancellor
- **Public face:** Chancellor of Calder for twenty years. He is a calm, kindly, grey-robed advisor who is always reasonable. Under other names he has also served as an advisor in Dravenhold and Solmere.
- **Truth:** Morvane is the **First King's brother**, kept alive for a thousand years by the Hollow Crest. He believes free will is the root of every war. If one crest held every will, there would be no more kings, riots or wars, only obedience. He calls it *peace*.
- **Method:** he starts riots, forges insults between crowns and tempts lords into domination. Every crest that turns purple feeds the Hollow Crest. When enough have turned, he will absorb them all and become the only lord in the world.
- **Connection to the hero:** twenty years ago Morvane destroyed the **Crestwardens**, the order that guarded crest-bearers. One Warden escaped carrying a newborn: Aldric, with Rowan.
- **Why he's frightening:** he is never cruel for its own sake. He apologises to Aldric before killing him.

### Lieutenants (recurring bosses)
| Name | Role | Fate |
|---|---|---|
| **The Binder** (unnamed) | Leads the raid on Ashford Farm | Dies in the Prologue; his crest awakens Rowan's |
| **Rusk, the Bandit King** | Stole a reeve's crest; rules the Greyfen marsh | Ch 6 boss |
| **Lord Halvric Dain** | Thornvale's lord, seduced by Morvane's promises of order | Ch 7 boss; dying words point to "the Chancellor" |
| **Vossk, the Veiled Hand** | Morvane's field commander; dominates the Empress | Ch 14 boss |
| **Prince Castellan of Solmere** | Merchant prince who buys and hoards crests | Ch 13 boss |
| **Sister Ilse** | Veiled zealot who hunts Rowan through Act III | Ch 17 boss; her death shows Rowan the purple in his own crest |
| **Morvane** | — | Ch 15 (unbeatable), Ch 24 final boss |

---

## 6. Characters

The names are working names; rename freely. Their ids match `data/units/` where a unit already exists.

### Main cast

| Unit (id) | Who | Class path | Joins | Leaves |
|---|---|---|---|---|
| **Rowan Ashford** (`rowan`) | 18, the farmer's son: messy dark-blue hair, freckles, a linen shirt and patched trousers. Quiet, stubborn, with a dry sense of humour and a deep sense of fairness. Taught the bow and sword by his father, "for wolves". | **Youth** (sword + bow) → *(after Ch 1, at the hub)* **Lord** → Crest Knight → High Lord → **Sovereign** | Prologue | — |
| **Aldric Ashford** (`aldric`) | Rowan's father, ~50. Gruff, dry, secretive and warm underneath. He fights far too well for a farmer and wears **Warden's hide**, black leather armour stitched with old silver sigils. He is the last of the Crestwardens. | **Warden** (unique, pre-promoted, sword; promotes to Warden Lord at tier 2) | Prologue | Dies Ch 15 |
| **Kai Merrow** (`kai`) | 18, Rowan's best friend since they could walk: shaggy sandy hair, sunburn, a padded gambeson and his family's old farm spear. A loud, brave horse-rancher's son. Competes with Rowan at everything. In Act III he is the first to tell Rowan to stop. | Soldier → (promotion, Lv 10) Cavalier → Paladin → **Vanguard** | Ch 1 | — |
| **Mira Fenn** (`mira`) | 18, the village healer's apprentice: a blonde braid, a wool dress and an apron, and a satchel of herbs. Gentle and clever, and tired of being the one who's protected. She has been secretly teaching herself fire magic from a book she shouldn't have. | Acolyte (staff + light fire magic) → Sage → **Oracle** | Ch 1 | — |

### Companions — Act I (the Thornvale)

| Unit | Who | Class path | Joins |
|---|---|---|---|
| **Brann Holt** (`brann`) | Millbrook's blacksmith. Big, gentle and practical. Talks to his hammer. Once he joins, he opens the hub **smithy**. | Fighter → Warrior → Ironheart | Ch 2 (talk to him, as an ally defending his forge) |
| **Tamsin Reed** | A Thornwood hunter (and poacher), sardonic and solitary. The Gloamkin killed her dogs. | Archer → Sniper → Wildstalker | Ch 3 (Rowan talks to her) |
| **Elise Varrow** (`elise`) | A scholar from the Calder royal archive sent to study "crest disturbances". Talks fast and treats Rowan's hand as a research subject. She delivers the crest lore. | Mage → Sage → Crestscribe | Ch 4 (escorted to safety) |
| **Dorran Kell** | Retired captain of Ferrow's Keep. Stoic, an old soldier. Recognises Aldric's armour and keeps the secret. | Knight → General → Bulwark | Ch 5 |
| **Sable** | A mercenary swordswoman in the Bandit King's pay. She grew up near Kai's ranch and still owes him money. | Myrmidon → Swordmaster → Blade Dancer | Ch 6 (Kai talks to her) |
| **Wren Alder** | A sky-patrol rider in Lord Halvric's service. She deserts when she sees what he has become. | Pegasus Knight → Falcon Knight → Skyward | Ch 7 (Rowan talks to her) |

### Companions — Act II (the wider world)

| Unit | Who | Class path | Joins |
|---|---|---|---|
| **Pip** | A Calder undercity thief, 15, cheerful and morally flexible. Opens doors and chests. | Thief → Rogue → Shadowstep | Ch 9 |
| **Princess Seraphine Calder** | King Osric's daughter and heir, bearer of a small radiant crest. Idealistic and brave, a poor rider. She is the second crest-bearer in the party; her crest goes white because of Rowan's example. | Troubadour (mounted staff) → Valkyrie → Lightbearer | Ch 10 |
| **Ysra Thane** | A Dravenhold wyvern rider who refuses to follow an empress who "has stopped blinking". | Wyvern Rider → Wyvern Lord → Dragonheart | Ch 11 (talk) |
| **Captain Halde Marr** | A Solmere privateer with a conscience and a crossbow. | Archer (crossbow) → Sniper → Corsair | Ch 12 |

### The Advisor — Osmund Thorne (`osmund`)
- **Who:** about 60, once **Royal Steward of Calder**. He was dismissed twenty years ago when the newly appointed Chancellor Morvane "streamlined" the court. Osmund was too honest, too thorough, and asked too many questions about the Crestwardens' disappearance. He retired to the end of the world and became Brookhollow's scribe. He tells everyone it's for his lungs.
- **Look:** neat grey beard, round spectacles, a high-collared bottle-green coat with brass buttons, and a quill permanently behind his ear.
- **Voice:** precise, dry, fussy, devastatingly polite. He speaks in footnotes and lectures on etiquette in the middle of emergencies, and he is the most loyal person in the game.
- **Introduced:** after Ch 1 (`hub_intro`). Elder Wenna sends for him the morning after the battle, because "the boy has a crest and no idea what that means. You do."
- **Role in play:**
  - **Hub advisor:** the hub menu's guide. He gives briefings before each chapter, explains objectives, and points out the next village in need.
  - **Outfitter:** he dresses Rowan as a lord. *"A lord who looks like a goatherd will be treated as one, my lord. Usually by goats."* This is Rowan's class change from **Youth → Lord**, and the first visible step of the outfit progression (§6.1). From then on he arranges new kit when anyone promotes, which is the in-story reason equipment gets grander.
  - **Court guide:** in Act II he navigates the capital, recognises Morvane, and is the first to say out loud that the Chancellor is "not ageing correctly".
  - **Comic relief:** he and Aldric get on terribly: two old men who each suspect the other of having a past.
- **Arc:** Osmund kept a private ledger of every crest-bearer who died strangely over twenty years. In Ch 13, his ledger and Elise's research together prove the pattern. After Aldric dies (Ch 15), he is the one who tells Rowan, quietly, that grief is not a mandate. Rowan doesn't listen until Ch 19.

### Key NPCs
- **Elder Wenna:** Brookhollow's elder. She runs the village; Osmund runs the hub menus.
- **Hollis Merrow:** Kai's father, the rancher. He gives the party horses after Ch 5.
- **King Osric of Calder:** tired, decent and manipulated by Morvane.
- **Empress Ysolde:** dominated through Vossk. Once freed in Ch 14, she is fierce and ashamed, and becomes an ally.
- **Aldric's spirit:** appears in the Still Meadow (Ch 19) and in the ending.
- **Lyra Ashford (backstory only):** Rowan's mother, a Crestwarden. She died the night the Wardens fell. Aldric still keeps her ribbon on his sword.

### 6.1 Visual progression — "from farmhands to legends"
Everyone starts **plain**. Gear gets visibly grander only as **class tiers** rise, so the art itself tells the player how far the army has come. Sprites follow the class (`<unitId>_<classId>`), so a promotion swaps the look automatically.

| Tier | Look | Examples |
|---|---|---|
| 0 — Country | Homespun linen, wool, patched trousers, worn boots, farm tools as weapons. Muted earth colours, no armour, no jewellery. | Rowan (Youth), Kai (Soldier, gambeson and dented iron cap), Mira (wool dress and apron), Brann (smith's apron) |
| 1 — First kit | One step up: leather armour, a proper (but plain) weapon, a cloak. Rowan's lord outfit is here: a dark blue doublet, grey cloak and crest buckle, modest but clearly noble. | Rowan (Lord), Kai (Cavalier: boiled leather on a brown farm horse) |
| 2 — Steel | Real armour and good robes: steel plate, chainmail, embroidered robes, faction colours (Calder blue) appear. | Crest Knight, Paladin, Sage, Warrior |
| 3 — Gold | Ornate: gold trim, white capes, plumes, glowing staves, barded horses. | High Lord, Vanguard, Oracle, Ironheart |
| 4 — Sovereign | Rowan alone: radiant white-and-gold regal armour and a slim circlet. | Sovereign |

The prompts for every outfit are in `tools/art/manifest.py` (`OUTFITS`). Tiers above 1 are marked `planned` until their classes exist.

Enemy and NPC equivalents follow the same rule: bandits are ragged, lords' soldiers wear issue steel, and Veiled officers wear finer purple-trimmed robes the higher they rank.

---

## 7. Story Outline

### Prologue — Ashford Farm
The game opens on a quiet evening of farm chores and banter between Rowan and Aldric. That night the dogs go silent and Gloamkin come out of the treeline, led by a cloaked figure whose hand burns with a **purple crest**. Rowan uses his bow and sword. Aldric pulls a sword and black leather armour from under the floorboards, and fights like a man from a song. When the Binder falls, his crest rises from his hand, hangs before Rowan's palm, **turns from purple to white**, and sears itself onto Rowan's skin. Aldric goes pale and says the words *"a crest of the lords"*. He explains what crests are, but not how he knows. Smoke rises from the direction of Brookhollow.

### Chapter 1 — Brookhollow Burns
Rowan and Aldric run to the village and find it under attack. Kai is holding the well square with a spear, and Mira is tending the wounded in the chapel. During the fight Mira reveals her secret fire magic. Together the four of them save the village. Elder Wenna names Rowan the village's protector, half joking and half not. Brookhollow becomes the hub.

### Interlude — The Steward (hub introduction)
The next morning Wenna brings **Osmund Thorne** to the farm. He inspects Rowan's crest through his spectacles, then his clothes, and is more upset by the clothes. He produces a dark blue doublet "kept for an occasion that never came", and a cloak. Kai laughs until he cries, and Mira tells Rowan he looks "like someone people would follow", which is worse. Rowan feels ridiculous and stands up straighter anyway. Aldric watches from the doorway and says nothing; his face says a great deal. Osmund sets up in the village hall, and Brookhollow becomes a proper headquarters.

### Act I — The Thornvale (Ch 2–7)
- Rowan and his friends set out to protect the other villages. The attacks are spreading, and Lord Halvric's soldiers aren't coming.
- **Recruitment:** Brann at Millbrook (Ch 2) and Tamsin in the Thornwood (Ch 3). They rescue Elise from Halvric's own soldiers, who are "detaining travellers" (Ch 4); she explains the crest lore. At Ferrow's Keep they hold the line with Dorran (Ch 5), and Kai gets his family's horses. They break Rusk the Bandit King, which shows that crests can be *stolen* (Ch 6).
- Every clue points to Dain Hall. Lord Halvric has gone purple: his soldiers fight with blank eyes, and his sky patrol includes Wren, who deserts (Ch 7).
- Halvric falls, and his soldiers wake up as if from a dream. With his last breath he says: *"The Chancellor promised... order. No more burning..."*
- Rowan's crest grows to the **Knight's Crest**. The Thornvale is free, and its people ask Rowan to be their lord. He refuses the title and accepts the duty.

### Act II — The Burning Crowns (Ch 8–15)
- Rowan travels to the capital to report Halvric's crimes. He finds the city rioting (Ch 8) and meets the gentle, helpful **Chancellor Morvane**, who thanks him warmly. Aldric won't stand in the same room as Morvane, and won't say why.
- **Recruitment:** Pip in the undercity (Ch 9), who finds Veiled letters sealed in purple wax. Dravenhold attacks the border on a forged insult (Ch 10), where Princess Seraphine joins, and Ysra deserts the imperial wyvern corps (Ch 11).
- **In Solmere** (Ch 12–13): crests are sold at auction. Castellan's defeat raises Rowan's crest to the **Lord's Crest**, and Elise finally sees the pattern: every shrouded crest drains toward the Ashen Reach. Something is drinking them.
- **Ch 14:** the party frees Empress Ysolde from Vossk's domination, and peace between the three crowns becomes possible.
- **Ch 15:** at the peace summit in Calder, Morvane reveals himself. He takes King Osric's crest in front of everyone and orders the Hollowed into the hall. Aldric stays behind so that the others can escape: *"Twenty years, Morvane. I've been ready for twenty years."* He holds for five turns. He dies.
  - At the end, Rowan sees his father's sword fall, and his crest flickers **purple** for the first time.

### Act III — The Hollow Path (Ch 16–22)
- **Ch 16–17:** Rowan buries his father and hunts Morvane's lieutenants with a speed and cruelty that frightens his friends. His crest skills turn into **Wrath**. He wins every fight, and his friends start quietly falling out of formation.
  - Kai finally says it: *"You're starting to sound like them."*
  - When Sister Ilse dies, Rowan sees the purple in his crest and pretends he didn't.
- **Ch 18 — The Broken Crest:** at the gates of the Hollow Spire, Rowan's crest **fails** mid-battle. Morvane is waiting. *"You see? Even you. Everyone bends, in the end."* Rowan is struck from the cliffs into the abyss. His friends are scattered and taken by the Hollowed.
- **Ch 19 — The Still Meadow:** Rowan wakes in a grey copy of Ashford Farm. His father is there, sitting on the fence, as if nothing happened. Aldric doesn't scold him; he tells him the truth about the Wardens, about Rowan's mother, and why he hid. *"A crest isn't a weapon, son. It's a promise to the people standing behind you. You stopped looking behind you."* Rowan fights the shadows of his own rage, ending with a purple copy of himself. His crest returns faintly white, but it is not whole.
- **Ch 20–22 — The Bonds That Hold:** Rowan crosses the Hollow dreamscape to free his friends. Each one is trapped in a nightmare built from their worst fear: Kai's ranch burning (Ch 20), Mira's hands too weak to heal anyone (Ch 21), and a final linked rescue of the whole army (Ch 22). Each freed friend gives back a facet of the crest. When the last bond is restored, the crest blazes white-gold: the **Sovereign's Crest**.
  - Optional paralogues give each companion their own rescue scene for extra rewards.

### Finale (Ch 23–24)
- **Ch 23 — Three Crowns March:** Calder (under Seraphine), Dravenhold (under Ysolde) and Solmere's free captains march together on the Hollow Spire. The allied armies fight on the map as green units.
- **Ch 24 — The Hollow King:** Morvane has absorbed hundreds of crests and become the Hollow King.
  - **Phase 1:** destroy the crest pylons feeding him.
  - **Phase 2:** he takes his true form.
  - At the end the radiant and hollow halves of the Prime Crest are face to face, and Rowan understands that destroying one destroys both, along with every crest in the world. Morvane is horrified: *"You'd throw it away? All of it? Then who will keep them in line?"* Rowan answers: *"Nobody. That's the point."*
  - Rowan sacrifices his crest. White light crosses the continent, and every crest in Aldmere fades from every hand.

### Epilogue
Kings and queens wake up as ordinary people. Some step down and some choose to keep leading without power, which was the hardest test all along. Rowan goes home and rebuilds Ashford Farm. Paired endings come from support levels. The final shot is Rowan's bare hand on his father's fence post at sunrise.

---

## 8. Bringing In New Unit Types

Every new class arrives with a reason, on a map that teaches it.

| Ch | New player unit type | Why it appears | New enemy type | What the map teaches |
|---|---|---|---|---|
| P | Youth (Rowan), Warden (Aldric) | Home, family | Gloam Hound (fast beast), Gloam Brute (heavy), Binder (dark mage, crest-bearer) | Movement, attacking, Dominion/Severance |
| 1 | Soldier (Kai), Acolyte (Mira) | Childhood friends | — | Allied NPCs (villagers), healing, magic vs Res |
| 2 | Fighter (Brann) | Defending his forge | Bandits (fighters, brigands) | Weapon triangle, talk-to-recruit |
| 3 | Archer (Tamsin) | Hunting Gloamkin | Gloam Stalker (spike-thrower, range 2) | Fog of war, bows, forests |
| 4 | Mage (Elise) | Escaping Halvric's men | Lord's soldiers, cavaliers | Escape objective, cavalry |
| 5 | Knight (Dorran) | Defending his keep | Gloam Horde, Gloam Brute Alpha | Survive objective, chokepoints |
| 5→6 | Cavalier (Kai) | Merrow ranch horses (hub event); Kai promotes into it at Lv 10 | — | Movement types |
| 6 | Myrmidon (Sable) | A mercenary turned | Bandit King + stolen crest | Defeat Boss, swamp terrain |
| 7 | Pegasus Knight (Wren) | Deserts Halvric | Pegasus patrols, armoured knights | Fliers vs bows, Seize, freeing dominated troops |
| 9 | Thief (Pip) | Undercity guide | Rogues | Doors, chests, stealing |
| 10 | Troubadour (Seraphine) | Her escort ambushed | Imperial soldiers | Mounted healing, protecting a VIP |
| 11 | Wyvern Rider (Ysra) | Imperial deserter | Wyvern riders | Heavy fliers |
| 12 | Crossbow Archer (Halde) | Solmere privateer | Marines, ship maps | Bridges/ships, Escape |
| 16+ | — | — | Hollowed (no fear, no retreat) | The shrouded Rowan |
| 19 | — | — | Shadows of Rowan | Solo map |
| 23 | Allied armies (green) | Alliance | Hollow Legion, Gloam Colossus | Ally phase at scale |

---

## 9. What This Means for the Systems

These are additions the story needs. They extend the M4–M6 milestones and are not built yet.

- **Crest system:** `CrestState` on the GameSession (tier, fragments, radiant/shrouded); crest skills as an `ArtDef`-like `CrestSkillDef`; a Dominion aura and Severance effects on crest-bearing enemies; tier gates on promotion (`ClassDef.RequiredCrestTier`).
- **Commands:** a **Talk** command (recruitment and story conversations) and a **Visit** command (village houses).
- **Statuses:** **Stunned** (skip the next phase) and **Surrendered** (leaves the map).
- **Objectives:**
  - **Defend:** keep N villagers or houses alive.
  - **Protect unit:** Seraphine, Elise.
  - **Survive while the boss is invulnerable:** Ch 15.
  - **Phase bosses:** Ch 24.
- **Story flags:** Aldric's death (he is removed from the roster) and the Act III crest state (aura off, Wrath skill on).
- **Ally phase:** green units at scale (Ch 1, Ch 23).
- **Unique classes:** Youth, Warden, Acolyte, and the Crestbound third tier.
- **Youth → Lord:** a story class change in `hub_intro`. Same stats and growths; it is a visual and narrative step that also unlocks the hub's Promotion screen.
- **Rowan's Youth and Lord classes:** both allow **Sword and Bow** (`data/classes/youth.tres`, `lord.tres`).
