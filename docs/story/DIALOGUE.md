# Dialogue Guide & Scripts

How characters talk, and the written scripts. The scene ids match [`CHAPTERS.md`](CHAPTERS.md). Story canon is in [`STORY.md`](STORY.md).

---

## 1. Script format

Scripts are written so they convert 1:1 into `DialogueScene` resources (GDD §5): an ordered list of lines and commands.

```
### scene_id
@bg farm_evening          ; background image (story scenes) — omitted for in-battle scenes
@music calm_farm
ROWAN [smile]: Spoken line.
ALDRIC [neutral]: Another line.
@wait 0.5                 ; pause, seconds
@focus 7,4                ; battle scenes: move the camera to a cell
@flag aldric_revealed     ; set a story flag
@sfx crest_burn
```

- **Speaker ids** are unit ids in caps (`ROWAN`, `ALDRIC`, `KAI`, `MIRA`…). `NARRATOR` has no portrait; `???` hides the speaker's name.
- **Expressions:** `neutral`, `smile`, `laugh`, `worried`, `sad`, `angry`, `determined`, `hurt`, `cold` (villains). The tags are kept in the scripts as acting notes, but the game shows only each speaker's neutral portrait (the user's call in session 11; no expression portraits are drawn).
- **Line length:** at most about 110 characters per line (two text-box rows). Split longer speeches.
- **In-battle scenes** (`chNN_*` triggered mid-map) stay short: 2–6 lines. The long talking happens in pre- and post-map scenes.

---

## 2. Voices

| Character | Voice | Says | Never says |
|---|---|---|---|
| **Rowan** | Plain words, short sentences, dry understatement. Gets quieter when angry, not louder. In Act III he gets clipped and cold. | "Right." "We go now." "That's not a plan, Kai, that's a shout." | Speeches about destiny (until Ch 23, when he earns one) |
| **Aldric** | Gruff, few words, farm metaphors. Warmth leaks out sideways. Never explains himself if a grunt will do. | "Eat first. Panic after." "A blade's like a plough: it goes where you put your weight." | Rowan's mother's name (until Ch 19) |
| **Kai** | Loud, quick, jokes under pressure, competitive. Honest to a fault. | "Bet I get more than you." "I'm not scared, I'm *energised*." | Anything cruel, even in Act III. He's hurt, not mean. |
| **Mira** | Warm, precise, a little prim. Under stress she gets crisp and commanding. A secret pride in her magic. | "Hold still. This will sting, and you'll deserve it." "I *can* help. Watch." | That she's afraid (she is) |
| **Brann** | Slow, kind, practical. Talks to his tools and to horses. | "Steady now, old girl." (to his hammer) | Long words |
| **Tamsin** | Sardonic, blunt, a loner warming up slowly. Hunting metaphors. | "You're loud. Gloamkin love loud." | "Thank you" (until her A support) |
| **Elise** | Fast, curious, footnotes in speech, gets excited at the worst times. Treats Rowan's crest like a manuscript. | "Fascinating — sorry — terrible, but *fascinating*." | That something is impossible |
| **Dorran** | Old soldier, formal, economical. Calls Rowan "my lord" to annoy him, then because he means it. | "Hold the line. Lines are what keep villages." | Jokes, except one, once |
| **Sable** | Mercenary swagger, keeps score, has a soft spot she denies. | "Kai Merrow. You still owe me eight silver." | Sentimental things, out loud |
| **Wren** | Earnest, guilty, a rule-follower learning that rules can be wrong. | "I was ordered to watch. I'm done watching." | — |
| **Pip** | Quick, cheeky undercity kid; prices everything, owns nothing. Calls Rowan "boss". Brave only when nobody's looking. | "For you, boss, no charge. Well. Small charge. Later." | Where he sleeps |
| **Seraphine** | Idealistic royal, polite, braver than she is skilled. Learns to lead from Rowan. | "A crown is supposed to *mean* something." | — |
| **Osmund** | Precise, dry, fussy, devastatingly polite. Calls Rowan "my lord" from day one, with a straight face. Footnotes, etiquette and quiet devotion. | "Strictly speaking, my lord, one does not *punch* a bandit lord. One has him punched." | Anything vulgar. Raises his voice exactly once in the whole game (Ch 15). |
| **Morvane** | Soft-spoken, reasonable, sorry. Calls everyone "child". Never shouts. | "I take no joy in this. That is rather the point." | Insults. He pities. |
| **Gloamkin** | No words. Sound cues only (`@sfx gloam_howl`). Severed Gloamkin whimper. | — | — |
| **Binders** | Whispering zealots who speak in "we". | "The quiet is coming. We are only early." | — |

---

## 3. Prologue — "Embers at Ashford Farm"

### pro_opening
@bg farm_evening
@music calm_farm
NARRATOR: The Thornvale lay a week's ride from anywhere that mattered.
NARRATOR: That suited the Ashfords just fine.
ALDRIC [neutral]: You left the south gate open again.
ROWAN [neutral]: I closed it.
ALDRIC [neutral]: Then the goats have learned latches. Should I be worried?
ROWAN [smile]: You should be impressed.
ALDRIC [smile]: Take the old sword and check the fence line before dark. Something's been at the hens.
ROWAN [neutral]: Foxes?
ALDRIC [worried]: ...Probably.
@wait 0.6
NARRATOR: The dogs had been quiet all evening.
NARRATOR: Neither of them said it out loud.

### pro_dogs
@bg farm_night
@music tension_low
@sfx gloam_howl
ROWAN [worried]: That's not a fox.
ROWAN [worried]: Father, there's something in the treeline. It's walking on two legs.
ALDRIC [determined]: Inside. Now.
ROWAN [determined]: I've got the sword.
ALDRIC [angry]: Rowan. *Inside.*
@sfx gloam_howl
???: There you are. We have been looking for a very long time.
ROWAN [worried]: Who—
???: Not you, child. Him.

### pro_aldric_reveal
@focus 6,5
@sfx floorboards
ROWAN [worried]: ...Where did you get that?
ALDRIC [cold]: Under the floor. Where I left it.
ROWAN [worried]: That's not farm armour.
ALDRIC [neutral]: No. It isn't.
ALDRIC [determined]: Stay on my left. Take what gets past me. Don't follow them into the trees.
ROWAN [determined]: ...Right.
@flag aldric_revealed

### pro_binder_battle (boss battle quote)
BINDER [cold]: The quiet is coming, Warden. We are only early.
ALDRIC [angry]: Then you can wait in the ground.

### pro_crest_awakens
@focus 9,3
@sfx crest_rise
NARRATOR: The cloaked man fell. The mark on his hand did not.
NARRATOR: It rose into the air — a sigil of dark violet light — and drifted, slow as ash, toward Rowan.
ROWAN [worried]: Father?
ALDRIC [worried]: Don't— Rowan, don't touch it—
@sfx crest_burn
NARRATOR: It touched him.
NARRATOR: The violet bled away like ink in water. What was left burned white.
ROWAN [hurt]: Agh— It's— it's *in* my hand—
@wait 0.8
ALDRIC [sad]: ...No. No, not you. Not yet.
ROWAN [worried]: What is it? You know what it is.
ALDRIC [neutral]: It's a crest, the mark of a lord.
ALDRIC [neutral]: Every ruler in Aldmere carries one: reeves, lords, kings. It's what makes them rulers.
ALDRIC [neutral]: It lets you reach the people around you, to strengthen them, shield them and heal them.
ALDRIC [cold]: Or reach into them and *make them kneel*. You saw what his did to those creatures.
ROWAN [worried]: Why did it come to me?
ALDRIC [neutral]: A crest goes to whoever ends its bearer. And it takes the colour of the heart that holds it.
ALDRIC [smile]: ...Yours went white. That's something.
ROWAN [determined]: How do you know all this?
ALDRIC [neutral]: Later.
@flag crest_tier_1

### pro_smoke
@bg farm_night_smoke
@music tension_high
ROWAN [worried]: Father. The sky over Brookhollow.
ALDRIC [determined]: Kai and Mira are down there. The whole village is.
ALDRIC [determined]: Get your sword. We run.

---

## 4. Chapter 1 — "Brookhollow Burns"

### ch01_pre
@bg village_burning
@music battle_urgent
NARRATOR: Brookhollow had forty houses, a chapel, a well, and no walls.
NARRATOR: Nobody had ever needed walls.
ROWAN [worried]: They're everywhere.
ALDRIC [determined]: Then we'll be busy. Make for the well square. Someone's still fighting there.
ROWAN [worried]: That's Kai's spear.
ALDRIC [neutral]: Of course it is.

### ch01_kai_joins
KAI [laugh]: Rowan! You took your time!
ROWAN [neutral]: You're bleeding.
KAI [smile]: Some of it's mine. I bet I've got more of them than you.
ROWAN [smile]: You started first.
KAI [worried]: Mr. Ashford, is that *armour*? Since when does your dad have—
ALDRIC [cold]: Since now. Fight.
KAI [determined]: Yes, sir!

### ch01_mira_fire
@focus 13,4
MIRA [worried]: Stay behind me. All of you, behind the pews—
@sfx gloam_roar
MIRA [determined]: ...No. Not in my chapel.
@sfx fire_cast
NARRATOR: The brute caught fire from inside its own shadow.
KAI [worried]: MIRA?!
MIRA [smile]: I've been practising.
KAI [worried]: Practising *what*? Since *when*?
MIRA [determined]: Since I got tired of bandaging you. Eyes forward, Kai.
ROWAN [smile]: ...Well. That's three of us.
@flag mira_fire

### ch01_reinforcements
@focus 8,0
@sfx gloam_howl
ALDRIC [determined]: More from the north road. Someone's herding them here.
ROWAN [determined]: The one in the cloak. There's always one in a cloak.
ALDRIC [neutral]: You're learning fast.

### ch01_boss_sennet (boss battle quote)
SENNET [cold]: You wear a stolen light, child. It will go dark like all the others.
ROWAN [determined]: Not while you're the one holding the lamp.

### ch01_post
@bg village_dawn
@music aftermath
NARRATOR: By dawn the fires were out. The village had lost two barns, a mill wheel, and old Teodor's prize goat.
NARRATOR: It had lost no one else.
WENNA [neutral]: Rowan Ashford. Show me your hand.
ROWAN [worried]: It's... it's nothing, Elder—
WENNA [sad]: It's a crest, boy. I'm old, not blind.
WENNA [neutral]: The last reeve this village had died forty years ago. We've done without since.
WENNA [smile]: It seems the village has a protector again, whether it wants one or not.
WENNA [neutral]: I'll send for Osmund in the morning. A crest-bearer who knows nothing about crests is a danger to everyone.
ROWAN [neutral]: I'm a farmer.
ALDRIC [neutral]: So was I.
KAI [laugh]: Lord Rowan of the Goats.
MIRA [smile]: Kai.
KAI [smile]: What? He's earned it.
ROWAN [determined]: ...If they hit Brookhollow, they'll hit Millbrook too. Then the Thornwood villages, and the Keep.
ALDRIC [neutral]: And Lord Halvric's men?
ROWAN [cold]: They haven't come. Not one of them.
ALDRIC [neutral]: Then we go.

---

## 4b. Interlude — "The Steward"

### hub_intro
@bg farm_morning
@music calm_farm
NARRATOR: The morning after, Brookhollow still smelled of smoke. Ashford Farm smelled of smoke and goats.
WENNA [neutral]: Rowan. This is Master Osmund Thorne. He keeps the village records.
OSMUND [neutral]: And, once upon a time, the King's household. Your hand, please.
ROWAN [worried]: It's... just the one crest.
OSMUND [worried]: "Just." He says "just".
OSMUND [neutral]: Radiant. Unmistakably radiant. I haven't seen a white crest since— well. Since.
ALDRIC [cold]: Since what?
OSMUND [neutral]: Since a great many things, Master Ashford. As I suspect you know.
@wait 0.6
OSMUND [neutral]: Now. The truly urgent matter.
ROWAN [worried]: The Gloamkin?
OSMUND [cold]: Your *trousers*.

### hub_outfit
@bg farmhouse_interior
OSMUND [neutral]: A lord who looks like a goatherd will be treated as one, my lord. Usually by goats.
ROWAN [neutral]: I'm not a lord.
OSMUND [neutral]: You bear a crest, you defended a village, and you have a steward. I'm afraid the evidence is against you.
NARRATOR: The doublet was dark blue wool, cut for some young noble who had never come to claim it.
NARRATOR: It fitted almost perfectly.
@flag rowan_class_lord
KAI [laugh]: Oh no. Oh, that's — Rowan. Turn around. Do a twirl.
ROWAN [cold]: Kai.
KAI [laugh]: *Lord* Rowan. Of the goats.
MIRA [smile]: Stop it. He looks... he looks like someone people would follow.
ROWAN [worried]: ...That's worse.
MIRA [smile]: I know.
@wait 0.6
NARRATOR: Aldric watched from the doorway and said nothing. His face said a great deal.

### hub_osmund_aldric
@bg farm_yard
OSMUND [neutral]: That armour of yours. Black hide, silver stitching. The Crestwardens wore something very like it.
ALDRIC [cold]: There's a lot of black leather in the world.
OSMUND [neutral]: Quite. And a lot of farmers who parry like duellists, I'm sure.
ALDRIC [cold]: You were at court. You were there when they disappeared.
OSMUND [sad]: I was dismissed for asking where they had gone. That was twenty years ago this spring.
ALDRIC [neutral]: ...Then you asked the right question.
OSMUND [neutral]: I usually do, Master Ashford. It's my least popular quality.

### hub_briefing_ch02
@bg village_hall
OSMUND [neutral]: Millbrook, my lord, half a day downriver. It has a forge, a mill and, as of this morning, bandits.
OSMUND [neutral]: The Gloamkin raids have stretched Lord Halvric's patrols thin, and opportunists have noticed.
ROWAN [neutral]: Halvric's men should be there.
OSMUND [cold]: Lord Halvric's men should be a great many places, my lord. I've begun a list.
ROWAN [determined]: Right. We go now.
OSMUND [smile]: "We go now." Concise. We'll work on the speeches.

## 5. Key scenes (the emotional spine)

### ch07_halvric_dies
HALVRIC [hurt]: Ah... it's quiet. It's so quiet now.
HALVRIC [sad]: They kept burning, boy. Every year another village. He said... with a firm enough hand... no more burning.
ROWAN [cold]: Who said?
HALVRIC [sad]: The Chancellor. He was so... reasonable...
@sfx crest_rise
NARRATOR: Halvric's crest rose, purple, and went white the moment it touched Rowan's.
ELISE [worried]: Your crest just — it *grew*. It absorbed it. That's not in any of the texts—
ALDRIC [cold]: The Chancellor.
ROWAN [worried]: Father?
ALDRIC [neutral]: ...Nothing. Let's get these people home.
@flag crest_tier_2

### ch15_aldric_stays
@focus 8,6
ALDRIC [determined]: Rowan. Take them through the west door.
ROWAN [angry]: No. We fight him together—
ALDRIC [neutral]: You can't fight him. Not yet. *I* can hold a door.
MORVANE [cold]: Aldric of the Wardens. I wondered where you'd gone. A farm. How very... humble.
ALDRIC [cold]: Twenty years, Morvane. I've been ready for twenty years.
MORVANE [sad]: I know. That's why I'm sorry.
ALDRIC [smile]: Rowan. Eat first. Panic after.
ROWAN [hurt]: Father—
ALDRIC [angry]: *GO.*
@flag aldric_stays

### ch15_aldric_dies
@bg hall_ruin
@music loss
NARRATOR: He held the door for as long as a man can hold a door. Then a little longer.
MORVANE [sad]: Rest, Warden. You were the last of a good order. I mean that.
ALDRIC [hurt]: ...Lyra... I kept him... safe...
@sfx sword_fall
@flag aldric_dead
NARRATOR: On the far side of the hall, Rowan heard the sword fall.

### ch15_crest_flickers
@sfx crest_corrupt
ROWAN [cold]: ...
MIRA [worried]: Rowan. Your hand.
NARRATOR: For a heartbeat, the white crest was the colour of a bruise.
ROWAN [cold]: I'm going to find him.
ROWAN [cold]: And I'm going to end him.
@flag crest_shrouded

### ch17_argument
KAI [angry]: Those soldiers surrendered, Rowan. They'd *dropped their swords*.
ROWAN [cold]: They'd have picked them up again.
KAI [hurt]: You don't know that. You used to care that you didn't know that.
MIRA [worried]: Show us your hand.
ROWAN [cold]: No.
KAI [sad]: ...You're starting to sound like them.
@wait 1.0
ROWAN [cold]: Then stay behind me. Like he told you to.

### ch19_father
@bg still_meadow
@music still_meadow
ROWAN [worried]: ...Father?
ALDRIC [smile]: You left the south gate open again.
ROWAN [hurt]: You're dead. I heard— I heard your sword—
ALDRIC [neutral]: I know. Sit. The fence holds two.
@wait 0.8
ALDRIC [neutral]: I was a Crestwarden. We guarded the bearers who used their crests right. There weren't many.
ALDRIC [sad]: Your mother was one of us. Lyra. Best sword I ever saw, and I've seen mine.
ALDRIC [sad]: Morvane burned the order in a night. I took you and ran, and I hid you, and I told myself it was for you.
ALDRIC [neutral]: Some of it was. Some of it was that I was tired of losing.
ROWAN [hurt]: I couldn't stop him. I couldn't even get *near* him. So I made myself into something that could.
ALDRIC [neutral]: And how's that going?
ROWAN [sad]: ...It broke.
ALDRIC [smile]: Good. It was the wrong shape.
ALDRIC [neutral]: A crest isn't a weapon, son. It's a promise to the people standing behind you.
ALDRIC [neutral]: You stopped looking behind you.
ROWAN [sad]: They're gone. He took them.
ALDRIC [determined]: Then go and get them. One at a time, if that's how it has to be. That's how you plough a field, too.
ALDRIC [smile]: And Rowan — close the gate on your way out.
@flag father_spoke

### ch24_choice
@focus 9,9
MORVANE [cold]: Look at it, child. Two halves of one crest. Radiant and hollow. The First King's light — and the shadow it always cast.
ELISE [worried]: They're the same crest. If one goes— Rowan, if one goes, they *all* go. Every crest in Aldmere.
MORVANE [worried]: ...You wouldn't. You *couldn't*. Without the crests, who keeps them in line? Who stops the burning?
ROWAN [neutral]: Nobody.
ROWAN [smile]: That's the point.

### ch24_sacrifice
@bg white
@music finale_theme
NARRATOR: Rowan Ashford closed his hand around the light, the way his father had taught him to hold a sword: with his weight behind it.
NARRATOR: And he let it go.
@sfx crest_shatter
NARRATOR: From the Ashen Reach to the Solmere harbours, on the hands of kings and reeves and thieves, the crests went out.
NARRATOR: Not dark. Just... gone. Like a held breath, finally released.
MORVANE [sad]: ...So quiet.
NARRATOR: The Hollow King came apart into grey ash, and a thousand years of it blew away on the wind.
@flag crests_ended

### epilogue_rowan
@bg farm_sunrise
@music home
NARRATOR: The kings and queens of Aldmere woke up ordinary.
NARRATOR: Some of them laid down their crowns. Some of them kept leading anyway, with nothing but their word.
NARRATOR: That turned out to be the harder thing. And the better one.
NARRATOR: Rowan Ashford went home, and rebuilt a farm, and closed the south gate.
NARRATOR: Most evenings.

---

## 6. Support conversations (pairs and themes)

Each pair has C/B/A ranks. Conversation ids follow `support_<a>_<b>_<rank>`, with ids sorted alphabetically.

| Pair | Theme (C → A) |
|---|---|
| Rowan × Kai | Rivalry → Kai admits he's scared of being left behind → *(post-Ch 20)* "You came back." |
| Rowan × Mira | Her secret magic → his secret crest fear → what they'll do "after" |
| Rowan × Aldric | Farm lessons → Warden lessons → *(must reach A before Ch 15)* Aldric almost says Lyra's name |
| Kai × Mira | Bickering → Kai realises she's stronger than him → a quiet promise |
| Kai × Sable | Eight silver → childhood on the ranch → why she became a sellsword |
| Mira × Elise | Magic theory nerd-out → Mira's self-taught gaps → teacher and student |
| Brann × Tamsin | He fixes her bow; she hates owing anyone → she brings him a stag → mutual respect |
| Dorran × Aldric | Old soldiers → "I thought the Wardens all burned" → Dorran swears to watch the boy |
| Wren × Seraphine | A deserter and a princess → duty vs conscience → Seraphine knights her |
| Elise × Rowan | His crest as a research subject → Elise's fear of what she's found → she writes the crest's history *with* him |
| Pip × Sable | Thieves' code vs mercenary code → a con gone wrong → found family |
| Ysra × Wren | Rival fliers → the Empress's eyes → flying together |
| Halde × Kai | Ships vs horses (heated) → seasickness → an unlikely friendship |

---

## 7. Writing checklist for new chapters
- Pre-map: who's here, what's at stake, one joke if the tone allows.
- Mid-map: recruitment talk, the reveal, and boss quotes (one line from each side).
- Boss death: a crest-bearer always says one human line before the crest leaves them.
- Post-map: consequence, the next destination, and the crest status if it changed (`@flag crest_tier_N`).
- Every new recruit gets at least one support pair listed in §6.
- Check voices against §2. If Rowan says more than three sentences in a row before Ch 23, cut.
