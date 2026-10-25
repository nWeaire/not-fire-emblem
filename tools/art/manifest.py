"""What art to request from PixelLab, in the approved GBA style (docs/ART_DIRECTION.md).

Edit this file to add units, tweak prompts or change animations. generate.py only submits what is not
already recorded in state.json (under "gba"), so re-running never pays twice for the same asset.
"""
from pathlib import Path

APPROVED = Path(__file__).with_name("approved")

# ------------------------------------------------------------------ characters
# create-character-pro, create_with_style, with the cut-out GBA fighters as the style reference (ART_DIRECTION §2).

STYLE_REF = APPROVED / "refs" / "gba_clean_fighters.png"
CHAR_STYLE = ("small chunky 16-bit pixel art sprite like the fighters in a Game Boy Advance battle scene, muted earthy "
              "palette, 2-3 tone flat shading per material, dark outline, low detail, bold readable silhouette")

MAP = {"size": 48, "view": "low top-down", "directions": ["south", "east", "north", "west"],
       "out": "assets/sprites/units"}
BATTLE = {"size": 64, "view": "side", "directions": ["east"], "out": "assets/sprites/battle"}

# Battle actions must say this or about half of them spin toward the camera mid-swing.
FACE = "keeps facing right the whole time, side view, never turns toward the camera"

MAP_WALK = {"kind": "template", "template": "walking-4-frames"}
# Mounted walks (template "horse": cavalry, pegasi). The "walk-4-frames" template lost the rider in session 11 (a
# riderless horse east/west, Kai on foot south), so the walk is described instead.
HORSE_WALK = {"kind": "v3", "frames": 4,
              "action": "the horse walks forward at a steady pace with the rider seated in the saddle the whole time, "
                        "holding the weapon upright"}
MAP_ATTACKS = {
    "sword": "steps forward and slashes a sword in a quick arc, then returns to stance",
    "lance": "thrusts a spear forward in a powerful stab, then pulls back",
    "axe": "raises an axe overhead and chops down hard, then returns to stance",
    "bow": "draws the bow, aims and releases an arrow, then lowers the bow",
    "staff": "raises the staff as soft light glows from its tip",
    "dark": "thrusts a glowing purple hand forward, releasing a burst of shadow magic",
    "claws": "lunges forward and rakes with its claws, then recoils",
    "spikes": "rears back and hurls a bone spike forward, then recovers",
    "anima": "thrusts the tome forward and hurls a ball of fire",
    # Session 15: the rioter's cudgel became an axe under the axe wording.
    "club": "raises a heavy wooden club overhead and clubs down hard, the same plain wooden club the whole time",
}
BATTLE_ATTACKS = {
    # weapon: (standard attack, critical)
    "axe": ("charges forward and swings the axe down in a heavy chop, then returns to stance",
            "raises the axe high overhead, leaps forward and slams it down in a crushing blow"),
    "sword": ("quick attack: steps forward and slashes the sword in a fast arc, then returns to stance",
              "over-the-top critical: crouches low, sword gleams, then dashes forward in a huge lunge with a wide "
              "sweeping slash, dramatic follow-through"),
    "lance": ("steps forward and thrusts the spear in a powerful stab, then pulls back",
              "twirls the spear, crouches, then lunges forward in a mighty piercing thrust with full body behind it"),
    "bow": ("draws the bow and looses an arrow forward, then lowers the bow",
            "kneels, draws the bow to full tension with a glint, and fires a powerful arrow"),
    "staff": ("raises the staff and swings it forward in a light strike",
              "raises the staff as bright light gathers at its tip, then strikes forward in a radiant burst"),
    "dark": ("raises the glowing purple hand, dark energy swirls, then thrusts it forward releasing a burst of purple "
             "magic", "dark energy swirls wildly around the caster, then a huge purple blast is hurled forward"),
    "claws": ("rears back and swipes both clawed arms forward in a savage rake, then recovers",
              "crouches, leaps forward and slashes both claws down in a frenzied double rake"),
    "spikes": ("rears back and hurls a bone spike forward, then recovers",
               "rips two bone spikes from its back and hurls them forward in a vicious double throw"),
    "club": ("raises the heavy wooden club and swings it down in a hard blow, then returns to stance. The same plain "
             "wooden club the whole time, no blade",
             "raises the wooden club high overhead with both hands and smashes it down in a wild furious blow. The same "
             "plain wooden club the whole time, no blade"),
    "anima": ("holds the tome forward and hurls a ball of fire from the free hand, then lowers it",
              "the tome blazes as flames swirl around the caster, then a huge fireball is hurled forward"),
}
BATTLE_IDLE = "stands in a battle-ready stance, weapon at the ready, subtle breathing"
BATTLE_HIT = "staggers backward from a heavy blow, then recovers"
BATTLE_DODGE = "quickly leans and hops backward to dodge an attack, then returns to stance"


def v3(action: str, frames: int, battle: bool) -> dict:
    return {"kind": "v3", "action": f"{action}. {FACE}" if battle else action, "frames": frames}


def map_anims(weapon: str) -> dict:
    if weapon == "none":  # villagers never attack
        return {"walk": MAP_WALK}
    return {"walk": MAP_WALK, "attack": v3(MAP_ATTACKS[weapon], 6, False)}


def battle_anims(weapon: str) -> dict:
    attack, crit = BATTLE_ATTACKS[weapon]
    return {"idle": v3(BATTLE_IDLE, 4, True), "attack": v3(attack, 8, True), "critical": v3(crit, 12, True),
            "hit": v3(BATTLE_HIT, 4, True), "dodge": v3(BATTLE_DODGE, 6, True)}


# Battle sprites are no longer made here: they are the "E" look, from battle_stills.py + animate_still.py
# (approved/refs/<id>_battle.png). These entries make the 48 px map sprites.
# Order is priority. Keys are sprite ids: playable outfits are "<unitId>_<classId>"; enemies "enemy_<classId>".
# "adopt" reuses a PixelLab character that already exists (the approved session-4 samples), so only the
# missing animations are paid for; "done_anims" maps animation names to existing PixelLab animation groups.
CHARACTERS = {
    # Battle art comes from the chosen still "E1" (approved/refs/rowan_battle_E1.png) via animate_still.py.
    "rowan_youth": dict(weapon="sword", sets=("map",),
        desc="18 year old farm boy hero, messy dark blue hair, off-white linen shirt, brown leather vest, brown "
             "trousers, boots, plain iron sword",
        adopt={"map": "cc3f5f68-665d-49f3-bd00-71690f384851"}),
    # Session 9 (user's pick "ranch hand" from the outfit mockups; the old tan gambeson and iron cap read as grey).
    "kai_soldier": dict(weapon="lance", sets=("map",),
        desc="cheerful 18 year old ranch boy, shaggy messy sandy blond hair, bright blue eyes, warm tanned skin, no "
             "helmet, cream linen shirt with rolled sleeves, rust red neckerchief, brown leather vest, dark green "
             "trousers, tall brown boots, long wooden spear with an iron tip"),
    "mira_acolyte": dict(weapon="staff", sets=("map",),
        desc="18 year old village healer girl, long blonde braid, grey-green wool dress with white apron, wooden staff",
        adopt={"map": "87180fa6-c885-41fd-9b8a-0742d3c2f4bd", "battle": "5970379e-05fc-4ff2-a524-77f82a9e825d"}),
    "enemy_gloam_brute": dict(weapon="claws", sets=("map",),
        desc="hulking hunched humanoid beast from a dark fairy tale, ash-grey and violet hide, long arms with big "
             "claws, tattered rags, small glowing pale eyes",
        adopt={"map": "2026ac6e-13b9-46c2-b5ef-23b4fa5fd0bc", "battle": "8b7dbc04-688c-4f8e-9aea-655613e6853a"},
        done_anims={"battle": {"attack": "504effbe-2ab2-48e9-b275-88735b41a7c7"}}),
    "enemy_binder": dict(weapon="dark", sets=("map",),
        desc="sinister cloaked dark mage, tattered deep purple hooded cloak, shadowed face, one hand glowing with a "
             "purple sigil, dark tome",
        adopt={"map": "60f8de6c-34b6-4e55-8d77-3b6d56c5dee8", "battle": "161aff36-fadb-4623-88a2-85e3952c559c"},
        done_anims={"battle": {"attack": "8c06d659-aa67-4924-b8a4-d537b942a3d3"}}),
    "aldric_warden": dict(weapon="sword", sets=("map",),
        desc="gruff 50 year old swordsman, short grey-streaked dark hair and beard, black leather armour stitched "
             "with faint silver sigils, dark grey cloak, steel sword"),
    "tamsin_archer": dict(weapon="bow", sets=("map",),
        desc="lean young woman forest hunter, short messy auburn hair, green hood, brown leather jerkin with a fur "
             "collar, quiver on her back, wooden hunting bow"),
    "enemy_gloam_hound": dict(weapon="claws", sets=("map",),
        desc="lean feral wolf-like humanoid beast from a dark fairy tale, hunched low and fast, long snout, ash-grey "
             "and violet fur, bony spine, small glowing pale eyes, sharp claws"),
    "enemy_gloam_stalker": dict(weapon="spikes", sets=("map",),
        desc="spindly tall thin humanoid beast from a dark fairy tale, ash-grey and violet hide, long thin limbs, bone "
             "spikes growing from its back and shoulders, small glowing pale eyes"),
    # Session 9: the first flying Gloamkin (Ch 5 onward). Flies over walls; bows are effective against it.
    "enemy_gloam_harrier": dict(weapon="claws", sets=("map",),
        desc="winged beast from a dark fairy tale, lean hunched body with huge ragged ash-grey bat wings spread wide, "
             "violet hide with glowing cracks, long hooked talons on its feet, small glowing pale eyes, flying"),
    # Session 9: Gloamkin caster (Ch 3 onward); its Gloom Bolt is effective against armour.
    "enemy_gloam_shade": dict(weapon="dark", sets=("map",),
        desc="hunched hooded shadow beast from a dark fairy tale, ragged ash-grey cloak-like hide, long thin clawed "
             "hands cupping a crackling orb of violet dark magic, small glowing pale eyes under the hood"),
    "rowan_lord": dict(weapon="sword", sets=("map",),
        desc="18 year old young lord, messy dark blue hair, fitted dark blue wool doublet with a few silver buttons, "
             "short grey travelling cloak, leather bracers, brown trousers, boots, plain iron sword"),
    "brann_fighter": dict(weapon="axe", sets=("map",),
        desc="big burly young village blacksmith, short brown beard, scorched leather apron over a plain shirt, bare "
             "muscular arms, heavy iron wood-cutting axe"),
    "elise_mage": dict(weapon="anima", sets=("map",),
        desc="young travelling scholar woman, long auburn hair, green eyes, worn grey travelling hood and grey cloak, "
             "satchel of papers, holding an open red tome in both hands in front of her, a small flame floating just "
             "above its open pages"),
    # Kai's tier 2: mounted on his family's ranch horse (Ch 5). The horse template fits a mounted body.
    "kai_cavalier": dict(weapon="lance", sets=("map",), template="horse",
        desc="cheerful 18 year old ranch boy riding a chestnut brown ranch horse with a plain leather saddle, shaggy "
             "messy sandy blond hair, no helmet, rust red neckerchief, brown leather jerkin with leather shoulder "
             "guards over a cream linen shirt, dark green trousers, tall brown boots, long wooden spear with an iron tip"),
    # Act I bandits and generic human enemies ("iron and leather", CHAPTERS.md). Gorran uses boss_brigand_chief.
    "enemy_fighter": dict(weapon="axe", sets=("map",),
        desc="rough bandit brigand, scruffy dark hair, stubble, red bandana, patched brown leather vest over a dirty "
             "shirt, bare arms, iron hand axe"),
    "enemy_archer": dict(weapon="bow", sets=("map",),
        desc="lean bandit archer, brown hooded cloak, red scarf over the lower face, leather jerkin, quiver, short "
             "wooden bow"),
    "enemy_myrmidon": dict(weapon="sword", sets=("map",),
        desc="wiry mercenary swordsman, long dark ponytail, dark red wrapped tunic, leather bracers and belt, slim "
             "curved iron sword"),
    "boss_brigand_chief": dict(weapon="axe", sets=("map",),
        desc="hulking bandit chieftain, dented iron helmet with two short horns, thick black beard, scarred face, "
             "red fur mantle over dark leather armour, heavy iron axe"),
    "enemy_soldier": dict(weapon="lance", sets=("map",),
        desc="garrison soldier, iron kettle helmet, chainmail shirt under a faded red tabard, brown trousers, iron "
             "spear"),
    "enemy_mage": dict(weapon="anima", sets=("map",),
        desc="rogue fire mage, young man with ragged dark hair, dark red hooded robe with a rope belt, holding a "
             "red tome, small flame flickering above the free hand"),
    "enemy_pegasus_knight": dict(weapon="lance", sets=("map",), template="horse",
        desc="mercenary pegasus knight, young woman in red and steel armour with a short red cape, riding a white "
             "winged pegasus with feathered wings, iron lance"),
    # Tier 2 outfits (promotion; the game looks for <unitId>_<classId> art first). Grander than the tier 1 clothes.
    "rowan_crest_knight": dict(weapon="sword", sets=("map",),
        desc="18 year old knight-lord, messy dark blue hair, steel breastplate over a dark blue tunic, short blue cape "
             "with silver trim, a faint white crest glow on the back of his hand, steel sword"),
    "aldric_warden_lord": dict(weapon="sword", sets=("map",),
        desc="grizzled 50 year old lord-knight, short grey-streaked dark hair and beard, black leather armour with "
             "silver plates and sigils, long black cloak, steel greatsword"),
    "tamsin_sniper": dict(weapon="bow", sets=("map",),
        desc="lean young woman sniper, short messy auburn hair, dark green hooded cloak, fitted leather armour with a "
             "fur collar, quiver on her back, tall recurve longbow"),
    "brann_warrior": dict(weapon="axe", sets=("map",),
        desc="big burly young warrior, short brown beard, chainmail shirt with a fur mantle, iron gauntlets, bare "
             "muscular arms, heavy steel battleaxe"),
    "kai_paladin": dict(weapon="lance", sets=("map",), template="horse",
        desc="confident 18 year old knight riding a chestnut brown warhorse with a dark green caparison, shaggy messy "
             "sandy blond hair, no helmet, rust red neckerchief, polished steel breastplate and pauldrons over a dark "
             "green tabard with brown leather trim, tall brown boots, steel lance"),
    "mira_sage": dict(weapon="staff", sets=("map",),
        desc="young sage woman, long blonde braid, white and blue robes with silver embroidery, ornate silver staff "
             "with a glowing white tip"),
    "elise_sage": dict(weapon="anima", sets=("map",),
        desc="young scholar sage woman, long auburn hair, green eyes, grey and deep blue hooded robes with silver "
             "embroidery, satchel of papers, glowing red tome, flame above her free hand"),
    # Chapters 4-6 (CHAPTERS.md): Dorran (Ch 5), Sable (Ch 6), Halvric's cavalry (Ch 4), the Alpha (Ch 5), Rusk (Ch 6).
    "dorran_knight": dict(weapon="lance", sets=("map",),
        desc="stocky 60 year old knight, short white hair, thick grey moustache, weathered stern face, heavy dented "
             "steel plate armour over a faded green tabard, steel lance"),
    "sable_myrmidon": dict(weapon="sword", sets=("map",),
        desc="confident young woman mercenary swordswoman, short black hair with a red headband, scar on one cheek, "
             "fitted black leather armour, red sash, slim curved sword"),
    "enemy_cavalier": dict(weapon="lance", sets=("map",), template="horse",
        desc="soldier knight riding a brown warhorse, steel helmet hiding the face, dark purple tabard over chainmail, "
             "faint purple glow in the eyes, steel lance"),
    "enemy_gloam_alpha": dict(weapon="claws", sets=("map",),
        desc="huge hulking alpha beast from a dark fairy tale, much bigger than the others, ash-grey hide with glowing "
             "violet cracks, a crown of bone spikes on its head, long arms with massive claws"),
    # Named Binder bosses (session 7): they reused the Prologue's hooded Binder. Looks follow their portraits.
    "boss_sennet": dict(weapon="dark", sets=("map",),
        desc="cold thin gaunt middle-aged man, slicked-back black hair, long deep purple high-collared coat, faint "
             "purple glow of a thorny sigil at his throat, dark tome in one hand"),
    "boss_varga": dict(weapon="dark", sets=("map",),
        desc="fierce scarred woman with a shaved head and tattoos, dark purple leather armour, cruel grin, a "
             "glowing purple sigil on the back of her hand, dark tome"),
    "boss_rusk": dict(weapon="axe", sets=("map",),
        desc="burly bandit king, shaved head, thick red beard, gold earrings, patched marsh-green leather coat over "
             "armour, a stolen glowing purple crest sigil on the back of his hand, heavy steel axe"),
    # Chapter 7 (CHAPTERS.md): Wren (joins on Talk) and Lord Halvric (the Act I boss).
    "wren_pegasus_knight": dict(weapon="lance", sets=("map",), template="horse",
        desc="earnest young woman pegasus knight, short chestnut bob, light steel armour over a deep green Dain Hall "
             "tabard with a gold trim, short green cape, riding a white winged pegasus with feathered wings, iron "
             "lance"),
    "enemy_knight": dict(weapon="lance", sets=("map",),
        desc="heavy armoured knight in thick steel plate with a closed visored helmet, faint purple glow through the "
             "visor slit, deep green tabard, big square steel shield, iron lance"),
    "boss_halvric": dict(weapon="lance", sets=("map",),
        desc="tall grim 55 year old lord in heavy ornate steel plate armour, swept-back grey hair, short grey beard, "
             "deep green and gold surcoat and cloak, faint purple glow in his eyes and a glowing purple crest sigil on "
             "his gauntlet, silver lance"),
    # Act II (session 12): the new party members, the bosses, and the commonest new enemies.
    "pip_thief": dict(weapon="sword", sets=("map",),
        desc="cheeky scrawny 15 year old street thief boy, messy dark brown hair under a battered brown cap, oversized "
             "patched brown coat, bare feet, short iron sword held low"),
    "seraphine_troubadour": dict(weapon="staff", sets=("map",), template="horse",
        desc="young princess riding a grey horse, fair golden hair in a braid, thin silver circlet, Calder-blue riding "
             "coat with silver trim, holding a white healer's staff"),
    "ysra_wyvern_rider": dict(weapon="lance", sets=("map",), template="horse",
        desc="young woman knight with a dark braid riding a grey-scaled wyvern with big leathery wings and a long "
             "tail, black iron armour with white fur at the collar, steel lance"),
    "halde_archer": dict(weapon="bow", sets=("map",),
        desc="weathered privateer captain woman, auburn hair under a dark tricorn hat, long red sea coat, brown boots, "
             "heavy wooden crossbow"),
    "boss_castellan": dict(weapon="lance", sets=("map",),
        desc="heavy smug merchant prince in gilded silver plate armour, oiled dark curls, small pointed beard, gold "
             "chains, purple glowing sigils on both gauntlets, silver lance"),
    "boss_vossk": dict(weapon="dark", sets=("map",),
        desc="pale hollow-cheeked man with a shaved head in grey and purple hooded robes, purple sigils crawling over "
             "both hands, holding a dark tome"),
    "boss_morvane": dict(weapon="dark", sets=("map",),
        desc="gentle grey-haired old chancellor in long plain grey court robes, neat grey beard, hands folded, a faint "
             "purple glow around his hands"),
    "ysolde_wyvern_lord": dict(weapon="lance", sets=("map",), template="horse",
        desc="tall fierce empress in black iron armour and a white fur mantle, black iron crown, black hair streaked "
             "with white, riding a black-scaled wyvern with big leathery wings, silver lance"),
    "enemy_wyvern_rider": dict(weapon="lance", sets=("map",), template="horse",
        desc="imperial wyvern rider in dark iron armour with a closed helm and a grey-white tabard with a red wolf, "
             "riding a grey-scaled wyvern with big leathery wings, iron lance"),
    "enemy_hollowed": dict(weapon="sword", sets=("map",),
        desc="hollowed soldier, ash-grey skin, empty pale eyes, rusted dented armour hanging off him, torn grey "
             "tabard, chipped iron sword, slack expressionless face"),
    "enemy_thief": dict(weapon="sword", sets=("map",),
        desc="lean undercity cutpurse, dark hood and scarf over the lower face, patched dark leathers, a long knife and "
             "a short sword"),
    # Session 12: Halvric's hired garrison in Ch 7 (they borrowed the bandit myrmidon / fighter looks).
    "enemy_sellsword": dict(weapon="sword", sets=("map",),
        desc="hired garrison swordsman, short dark hair, stubble, faint purple glow in the eyes, deep green Dain Hall "
             "tabard over a mail shirt, leather bracers and belt, brown boots, straight steel sword"),
    "enemy_axeman": dict(weapon="axe", sets=("map",),
        desc="burly garrison axeman, iron nasal helmet, thick beard, faint purple glow in the eyes, deep green Dain "
             "Hall tabard over a chainmail shirt, leather gloves, brown boots, iron battle axe"),
    # Session 15 (ART_NEEDED §0, group one): the commonest Act II stand-ins, by how many spawns use them.
    # Dravenhold's army (Ch 10, 11, 14; SpriteId enemy_imperial, so enemy_imperial_<classId>).
    "enemy_imperial_soldier": dict(weapon="lance", sets=("map",),
        desc="Dravenhold imperial soldier in dark iron armour with a grey-white tabard bearing a red wolf, white fur at the collar, open-faced iron helm, steel spear"),
    "enemy_imperial_archer": dict(weapon="bow", sets=("map",),
        desc="Dravenhold imperial archer in a dark iron mail shirt with a grey-white tabard bearing a red wolf, white "
             "fur at the collar, iron cap, quiver on the back, longbow"),
    "enemy_imperial_cavalier": dict(weapon="lance", sets=("map",), template="horse",
        desc="Dravenhold imperial cavalry knight in dark iron armour with a grey-white tabard bearing a red wolf, white "
             "fur at the collar, riding a dark grey warhorse with a grey-white caparison, steel lance"),
    # Weapon variants (BattleUnit.WeaponArt): "<sprite>_<class>_<weapon type>". Mounted units carry ordinary-sized
    # swords and axes; armoured ones are drawn with great swords and great axes (the user's call, session 15).
    "enemy_imperial_cavalier_sword": dict(weapon="sword", sets=("map",), template="horse",
        desc="Dravenhold imperial cavalry knight in dark iron armour with a grey-white tabard bearing a red wolf, white "
             "fur at the collar, riding a dark grey warhorse with a grey-white caparison, an ordinary one-handed steel "
             "sword"),
    "enemy_imperial_mage": dict(weapon="anima", sets=("map",),
        desc="Dravenhold imperial battle mage in long grey-white robes with a red wolf on the chest, dark iron "
             "pauldrons, white fur collar, holding an open tome crackling with yellow lightning"),
    "enemy_imperial_knight": dict(weapon="lance", sets=("map",),
        desc="heavy Dravenhold imperial knight in thick dark iron plate with a closed wolf-snouted helm, grey-white "
             "tabard with a red wolf, white fur at the collar, big dark iron tower shield, steel lance"),
    "enemy_imperial_fighter": dict(weapon="axe", sets=("map",),
        desc="burly Dravenhold imperial axeman in a dark iron mail shirt with a grey-white tabard bearing a red wolf, "
             "white fur mantle, horned iron helm, big steel battle axe"),
    "enemy_imperial_myrmidon": dict(weapon="sword", sets=("map",),
        desc="lean Dravenhold imperial swordsman in dark iron scale armour with a grey-white tabard bearing a red wolf, "
             "white fur at the collar, no helmet, short dark hair, straight steel sword"),
    # Castellan's house guard (Ch 12, 13; SpriteId enemy_gilded).
    "enemy_gilded_soldier": dict(weapon="lance", sets=("map",),
        desc="Solmere Gilded House guard in polished gilded half-plate, white and gold tabard with a golden sun crest, plumed gilded helm, gold-tipped steel spear"),
    "enemy_gilded_knight": dict(weapon="lance", sets=("map",),
        desc="heavy Solmere Gilded House knight in thick white-and-gold plate armour with a closed gilded helm, golden "
             "sun crest on the chest, big gilded round shield, steel lance"),
    # The Veiled in Calder (Ch 8, 9; SpriteId enemy_veiled_thug).
    "enemy_veiled_thug_myrmidon": dict(weapon="sword", sets=("map",),
        desc="Veiled street thug in a grey hooded cloak over dark leather armour with purple trim, a grey scarf over "
             "the lower face, a faint purple sigil glowing on the wrist, steel sword"),
    # Ch 8's riot (Neutral; amber outline in game) and the townsfolk it calms into.
    "neutral_rioter": dict(weapon="club", sets=("map",),
        desc="angry Calder townsman rioter, rolled-up sleeves, brown work vest, blue neckerchief, a faint purple glow "
             "in the eyes, a heavy wooden cudgel raised in one hand"),
    "ally_townsfolk": dict(weapon="none", sets=("map",),
        # Take 1 drew two women side by side in three of the four views (kept in state.json as ally_townsfolk_take1).
        desc="a single Calder townswoman standing alone, one person only, in city clothes, a blue headscarf, cream "
             "blouse, long brown skirt and a white apron, empty hands, no weapon"),
    # Seraphine's escort (Ch 10, 15) and the Gull's crew (Ch 12): map sprites only.
    "ally_calder_knight": dict(weapon="lance", sets=("map",),
        desc="Calder royal escort knight in polished steel plate armour with a Calder-blue tabard and silver trim, "
             "open steel helm, blue cape, steel lance and a kite shield"),
    "ally_sailor": dict(weapon="axe", sets=("map",),
        desc="weathered privateer deckhand, red-and-white striped shirt, dark bandana, rolled canvas trousers, bare "
             "forearms, a hand axe"),
    # Session 15, second batch ($30 top-up): the rest of Act II's stand-ins (ART_NEEDED "Outstanding", groups
    # 1-5). Armoured variants carry great weapons, mounted ones ordinary swords and axes (the user's call).
    "boss_caddoc": dict(weapon="lance", sets=("map",),
        desc='hollowed old knight, a Warden of twenty years ago, ash-grey skin, empty pale eyes, tarnished black leather armour with dull silver plates and sigils, a torn black cloak, a long silver lance held like a guard'),
    "enemy_imperial_knight_sword": dict(weapon="sword", sets=("map",),
        desc='heavy Dravenhold imperial knight in thick dark iron plate with a closed wolf-snouted helm, grey-white tabard with a red wolf, white fur at the collar, a huge two-handed great sword'),
    "enemy_imperial_knight_axe": dict(weapon="axe", sets=("map",),
        desc='heavy Dravenhold imperial knight in thick dark iron plate with a closed wolf-snouted helm, grey-white tabard with a red wolf, white fur at the collar, a huge two-handed great axe'),
    "enemy_imperial_general": dict(weapon="lance", sets=("map",),
        desc='massive Dravenhold imperial general in towering dark iron plate with a horned wolf helm, a long red cloak and a white fur mantle, grey-white tabard with a red wolf, a heavy iron tower shield, silver lance'),
    "enemy_imperial_general_axe": dict(weapon="axe", sets=("map",),
        desc='massive Dravenhold imperial general in towering dark iron plate with a horned wolf helm, a long red cloak and a white fur mantle, grey-white tabard with a red wolf, a huge two-handed great axe'),
    "enemy_imperial_general_sword": dict(weapon="sword", sets=("map",),
        desc='massive Dravenhold imperial general in towering dark iron plate with a horned wolf helm, a long red cloak and a white fur mantle, grey-white tabard with a red wolf, a huge two-handed great sword'),
    "enemy_imperial_wyvern_rider_axe": dict(weapon="axe", sets=("map",), template="horse",
        desc='imperial wyvern rider in dark iron armour with a closed helm and a grey-white tabard with a red wolf, riding a grey-scaled wyvern with big leathery wings, an ordinary one-handed steel axe'),
    "enemy_imperial_wyvern_lord": dict(weapon="lance", sets=("map",), template="horse",
        desc='imperial wyvern lord, a veteran commander in heavy dark iron armour with a red-plumed helm and a white fur mantle, grey-white tabard with a red wolf, riding a big dark grey wyvern with huge leathery wings, steel lance'),
    "enemy_gilded_archer": dict(weapon="bow", sets=("map",),
        desc='Solmere Gilded House archer in a white and gold tabard with a golden sun crest over a light gilded mail shirt, gilded cap with a short plume, quiver on the back, longbow'),
    "enemy_gilded_mage": dict(weapon="anima", sets=("map",),
        desc='Solmere Gilded House battle mage in long white and gold robes with a golden sun crest, a gilded collar and rings, holding an open red tome'),
    "enemy_gilded_cavalier": dict(weapon="lance", sets=("map",), template="horse",
        desc='Solmere Gilded House cavalry knight in gilded half-plate, white and gold tabard with a golden sun crest, plumed gilded helm, riding a white horse with a gold and white caparison, steel lance'),
    "enemy_gilded_cavalier_sword": dict(weapon="sword", sets=("map",), template="horse",
        desc='Solmere Gilded House cavalry knight in gilded half-plate, white and gold tabard with a golden sun crest, plumed gilded helm, riding a white horse with a gold and white caparison, an ordinary one-handed steel sword'),
    "enemy_gilded_general": dict(weapon="lance", sets=("map",),
        desc='massive Solmere Gilded House general in towering white-and-gold plate armour with a crested gilded helm, a golden sun crest on the chest, a long white cloak, a big gilded tower shield, silver lance'),
    "enemy_veiled_thug_soldier": dict(weapon="lance", sets=("map",),
        desc='Veiled street thug in a grey hooded cloak over dark leather armour with purple trim, a grey scarf over the lower face, a faint purple sigil glowing on the wrist, a plain iron spear'),
    "enemy_veiled_thug_fighter": dict(weapon="axe", sets=("map",),
        desc='burly Veiled street thug in a grey hooded cloak over dark leather armour with purple trim, a grey scarf over the lower face, a faint purple sigil glowing on the wrist, a heavy iron axe'),
    "enemy_veiled_thug_archer": dict(weapon="bow", sets=("map",),
        desc='a single Veiled street thug standing alone, one person only, in a grey hooded cloak over dark leather armour with purple trim, a grey scarf over the lower face, a faint purple sigil glowing on the wrist, a short bow and a quiver'),
    "enemy_veiled_thug_mage": dict(weapon="anima", sets=("map",),
        desc='Veiled street mage in a grey hooded cloak with purple trim over dark robes, a grey scarf over the lower face, purple sigils glowing on both wrists, holding a dark red tome'),
    "enemy_veiled_thug_knight": dict(weapon="lance", sets=("map",),
        desc='Veiled enforcer in heavy dark grey plate armour with purple trim and a hooded grey cloak over it, closed helm with a faint purple glow in the visor, a heavy iron lance and a dark shield'),
    "enemy_veiled_agitator_myrmidon": dict(weapon="sword", sets=("map",),
        desc='Veiled agitator disguised as a Calder townsman, plain grey cloak over ordinary city clothes, a purple sigil glowing on the wrist, a hard sly face, a short steel sword'),
    "enemy_veiled_agitator_mage": dict(weapon="anima", sets=("map",),
        desc='Veiled agitator disguised as a Calder townsman, plain grey cloak over ordinary city clothes, a purple sigil glowing on the wrist, a hard sly face, an open red tome'),
    "enemy_veiled_agitator_fighter": dict(weapon="axe", sets=("map",),
        desc='burly Veiled agitator disguised as a Calder townsman, plain grey cloak over ordinary city clothes, a purple sigil glowing on the wrist, a hard sly face, a hand axe'),
    "enemy_veiled_mage": dict(weapon="anima", sets=("map",),
        desc='Veiled caster in long grey and purple robes with a deep hood, a pale masked face, purple sigils crawling over both hands, holding a dark tome'),
    "dorran_general": dict(weapon="lance", sets=("map",),
        desc='stocky 60 year old general, short white hair, thick grey moustache, weathered stern face, heavy polished steel plate armour with gold trim over a deep green tabard, a green cape, a big steel tower shield, silver lance'),
    "sable_swordmaster": dict(weapon="sword", sets=("map",),
        desc='confident young woman swordmaster, short black hair with a long red headband trailing behind, scar on one cheek, fitted black leather coat with red trim and a red sash, a long slim curved sword'),
    "wren_falcon_knight": dict(weapon="lance", sets=("map",), template="horse",
        desc='young woman falcon knight, short chestnut bob, polished steel breastplate and pauldrons over a deep green tabard with gold trim, long green cape, riding a white winged pegasus with gold-trimmed barding, steel lance'),
    "seraphine_valkyrie": dict(weapon="staff", sets=("map",), template="horse",
        desc="young princess valkyrie always seated on her white horse with Calder-blue barding, fair golden hair in a braid, a silver circlet, a silver breastplate over a long Calder-blue coat with silver trim, a white cape, a white and gold healer's staff"),
    "ysra_wyvern_lord": dict(weapon="lance", sets=("map",), template="horse",
        desc='young woman wyvern lord with a dark braid riding a big grey-scaled wyvern with huge leathery wings, heavy black iron armour with a white fur mantle and a short black cape, a long steel lance'),
    "pip_rogue": dict(weapon="sword", sets=("map",),
        desc='cheeky lean 16 year old rogue, messy dark brown hair, a grin, fitted dark brown leathers with many pockets and a short dark cloak, soft boots, a proper steel sword held low'),
    "ally_villager": dict(weapon="none", sets=("map",),
        desc="plain village farmer man, brown hair, beige linen shirt, green wool vest, brown trousers, no weapon"),
}

# Later outfits (tier 3+) go here until their classes exist.
PLANNED: dict[str, str] = {}

# ------------------------------------------------------------------ portraits (generate-image-v2, 4 takes at 96 px)

PORTRAIT_STYLE = ("Game Boy Advance Fire Emblem dialogue portrait, anime bust shoulders up, facing left in "
                  "three-quarter view, 16-bit GBA pixel art, about 16 colours, hard cel shading, bold dark outline, "
                  "no gradients, like Fire Emblem: The Blazing Blade")
PORTRAITS = {
    "caddoc": "hollowed old knight, about 60, ash-grey skin, empty pale eyes, gaunt face with a grey beard, tarnished black "
              "leather armour with dull silver sigils, a torn black cloak hood, sorrowful and silent",
    "aldric": "gruff 50 year old man, short grey-streaked dark hair, short grey-streaked beard, weathered stern face, "
              "black leather armour collar with faint silver stitched sigils, dark grey cloak",
    "tamsin": "sardonic young woman hunter, short messy auburn hair, sharp green eyes, green hood down, brown leather "
              "jerkin with a fur collar",
    "wenna": "kindly but shrewd old village elder woman, white hair in a bun, wrinkled face, plain brown shawl over "
             "a green wool dress",
    "binder": "sinister hooded dark mage, deep purple hooded cloak, face mostly in shadow, pale chin, glowing purple "
              "eyes",
    "sennet": "cold thin middle-aged man, gaunt face, slicked black hair, deep purple high-collared coat, faint "
              "purple glow of a thorny sigil at his throat",
    "varga": "fierce scarred woman with a shaved head, tattoos, dark purple leather armour, cruel grin",
    "elise": "young travelling scholar woman, long auburn hair, curious green eyes, worn grey travelling hood, plain "
             "grey travelling cloak, ink smudge on her cheek",
    "dorran": "stern old knight captain, about 60, short white hair, thick grey moustache, weathered face, steel "
              "gorget and plate collar over a faded green tabard",
    "sable": "confident young woman mercenary, short black hair with a red headband, a scar on one cheek, sly smile, "
             "black leather collar and red sash",
    "rusk": "burly bandit king, shaved head, thick red beard, gold earrings, cruel grin, marsh-green leather coat, a "
            "faint purple glow from a sigil on his neck",
    "kai": "cheerful 18 year old ranch boy, shaggy messy spiky sandy blond hair, bright blue eyes, warm tanned skin, "
           "big confident grin, cream linen shirt collar, rust red neckerchief, brown leather vest",
    "wren": "earnest young woman pegasus knight, about 19, short chestnut bob, worried honest brown eyes, light "
            "steel shoulder guard over a deep green tabard with gold trim",
    "halvric": "tall grim lord, about 55, swept-back grey hair, short grey beard, hollow tired eyes with a faint "
               "purple glow, ornate steel gorget over a deep green and gold surcoat",
    # Session 7: the session-2 portraits of these two were rounder and more cartoony than the rest of the cast.
    "brann": "big gentle young village blacksmith, broad shoulders, short brown hair, short brown beard, kind brown "
             "eyes, soot smudges on his cheek, scorched leather smith's apron over a plain linen shirt",
    "osmund": "fussy precise old royal steward, about 60, neat grey beard, round spectacles, dry polite expression, "
              "high-collared bottle-green coat with brass buttons, a white quill tucked behind his ear",
    # Act II (session 12).
    "pip": "cheeky scrawny 15 year old street thief boy, messy dark brown hair under a battered cap, freckles, "
           "mischievous grin, oversized patched brown coat with the collar turned up",
    "seraphine": "brave idealistic 17 year old princess, fair golden hair in a travelling braid, clear grey eyes, a "
                 "thin silver circlet, Calder-blue riding coat with silver trim",
    "ysra": "blunt proud young woman wyvern rider, dark hair in a tight braid, a thin scar across one cheek, sharp "
            "dark eyes, black iron gorget over grey-white fur",
    "halde": "weathered privateer captain woman, about 35, sun-darkened skin, auburn hair tied back under a dark "
             "tricorn hat, a gold earring, dry half smile, long red sea coat",
    "morvane": "gentle soft-spoken grey-haired chancellor, about 60, kind tired eyes, neat grey beard, plain grey "
               "high-collared court robes, a faint purple shimmer in his pupils",
    "castellan": "heavy smug merchant prince, about 50, oiled dark curls, small pointed beard, heavy-lidded amused "
                 "eyes, gilded silver gorget and gold chains, faint purple glow at his throat",
    "vossk": "hollow-cheeked pale man, about 45, shaved head, calm devout expression, grey and purple hooded robes, "
             "faint purple sigils crawling up his neck",
    "ysolde": "tall fierce empress, about 40, black hair streaked with white, pale stern face, a black iron crown, "
              "white fur mantle over black iron armour",
}

# ------------------------------------------------------------------ backgrounds (generate-image-v2, 240x135)
# One image per location: the Story screen tints it for the time of day (farm_evening, farm_night, ...), and the
# battle screen uses the same images behind the fighters. No people, ground in the lower half.

BG_STYLE = ("Game Boy Advance Fire Emblem battle background, 16-bit GBA pixel art, flat simple shading, no people, "
            "no characters")
BG_SIZE = (240, 135)
BACKGROUNDS = {
    "hill_road": "a winding dirt road over grassy hills, a wooden toll gate in the distance, a stone milestone, "
                 "open road in the lower half",
    "ferrows_keep": "the courtyard of an old stone border fort, grey walls and a gatehouse, wooden walkways, banners, "
                    "packed earth ground in the lower half",
    "greyfen": "a misty marsh at dusk, reeds and shallow pools, wooden stilt houses on poles, boardwalk in the lower "
               "half",
    "farm": "a small farmstead in the countryside, wooden farmhouse with a thatched roof, fence and haystacks, "
            "fields and a dark treeline behind, open dirt yard in the lower half",
    "village": "a small medieval village square, timber and thatch cottages, stone well, little chapel, cobbled "
               "ground in the lower half",
    "farmhouse_interior": "inside a humble farmhouse, wooden beams, stone hearth with a fire, wooden table, "
                          "plank floor in the lower half",
    "village_hall": "inside a village hall, long wooden tables, banners, timber pillars, plank floor in the lower half",
    "river_road": "a dirt road beside a river with a wooden bridge, grassy banks, distant hills, road in the lower half",
    "mill": "a riverside watermill with a big wooden wheel, stone millhouse, grassy ground in the lower half",
    "thornwood": "a dark thorny old forest, twisted trees, brambles, mist, mossy ground in the lower half",
    "dain_hall": "the great hall of a stone castle, tall pillars, green and gold banners, a raised stone throne at "
                 "the far end, tall arched windows with storm clouds outside, flagstone floor in the lower half",
    # Act II (session 12): story and battle backdrops (UiKit.PlaceholderBackgrounds lists today's stand-ins).
    "calder_square": "the grand fountain square of an old capital city, tall stone townhouses with blue awnings, "
                     "market stalls, a palace on a hill behind, cobbled square in the lower half",
    "calder_streets": "a narrow street of an old capital city, tall stone and timber houses, hanging signs and "
                      "lanterns, blue banners, cobbled street in the lower half",
    "calder_palace": "a royal audience hall, tall pillars, deep blue banners with a silver crown, a long carpet to a "
                     "throne, high arched windows, polished stone floor in the lower half",
    "calder_lodgings": "comfortable rooms in a capital city inn, wooden beams, a writing desk covered in maps and "
                       "letters, a window over city rooftops, plank floor in the lower half",
    "calder_undercity": "underground sewers and cellars beneath a city, brick arches, a dark canal, lanterns, old "
                        "stone stairs, wet flagstones in the lower half",
    "border_pass": "a mountain border pass road, a small village of cottages burning in the distance with smoke, "
                   "rocky slopes, dirt road in the lower half",
    "dravenhold_pass": "a high snowy mountain pass, black pines, sheer grey cliffs, wyvern roosts on far crags, snow "
                       "and rock path in the lower half",
    "solmere_harbor": "a rich harbour city, many tall ships at wooden piers, gilded warehouses, gulls, orange tiled "
                      "roofs, stone quay in the lower half",
    "solmere_palazzo": "the gilded great hall of a merchant prince's palace, mirrors and gold leaf, marble pillars, "
                       "chandeliers, chequered marble floor in the lower half",
    "dravenhold_citadel": "a black stone imperial citadel courtyard in falling snow, iron gates, braziers, banners "
                          "with a red wolf, snowy flagstones in the lower half",
    # Hub mockup (session 7): Brookhollow from above, buildings to click (shop, forge, chapel, hall).
    "town_overview": "a small medieval farming village seen from above at a high three-quarter angle, a village "
                     "square with a stone well, timber and thatch cottages, a general store with a hanging sign, a "
                     "blacksmith forge with a chimney, a small chapel, a village hall with a flag, dirt paths, "
                     "wheat fields and a river at the edges, sunny day",
    # Title screen (session 7): revealed by the crest's light, the menu sits over the middle.
    "title_vista": "a grand sweeping vista of a peaceful fantasy kingdom at sunrise seen from a high hilltop, "
                   "rolling green valleys and farmland, a winding river, small villages, a castle on a distant "
                   "crag, snow-capped mountains on the horizon, golden dawn sky with soft clouds",
    # Session 15: the Old Warden Vault (stood in as hill_road) and Calder's great hall after Ch 15 (as dain_hall).
    "warden_vault": "the inside of an ancient ruined chapterhouse vault of the Wardens, cold grey stone pillars and "
                    "arches, broken stone seals glowing faintly white on the floor, a dusty stone altar at the back, "
                    "shafts of pale light from cracks in the roof, stone floor across the lower half",
    "hall_ruin": "a royal great hall after a battle, Calder-blue banners torn and hanging, overturned long tables and "
                 "broken chairs, a smashed oak door, scattered papers, stone floor across the lower half, grey light",
}

# ------------------------------------------------------------------ terrain (create-tiles-pro, 32 px)
# Extra tiles beyond the approved set (approved/terrain_gba32). Mapping of the 16 results is chosen by hand.

TILE_STYLE = ("Game Boy Advance Fire Emblem map tile, flat bright colours, very limited palette, simple shapes, "
              "seamless top-down")
# ------------------------------------------------------------------ map props (generate-image-v2, 16 takes at 64 px)
# Upright billboards on map cells (forest trees, mountain peaks, fort cottages); takes go to takes/props/<id>/.
PROP_STYLE = ("single object, Game Boy Advance Fire Emblem map sprite style, 16-bit pixel art, seen from a low "
              "three-quarter top-down angle, bold dark outline, flat bright colours, transparent background, centered, "
              "fills the frame")
PROPS = {
    "pine": "one tall dark green pine tree with a short brown trunk",
    "oak": "one round leafy green oak tree with a brown trunk",
    "cottage": "one small village cottage with a red tiled roof, cream plaster walls and a wooden door",
    "peak": "one rocky grey-brown mountain peak boulder with a little snow on top",
    # Small ground decorations (32 px canvas, so they share the trees' pixel scale): plain, meadow and field cells.
    "tuft": "one small clump of tall green grass blades",
    "flowers": "one small clump of wildflowers, white and yellow blossoms on short green stems",
    "crop": "one young leafy green cabbage plant growing in soil",
    "bush": "one small round leafy green bush",
}
# Smaller versions for one shared pixel scale (1 art pixel = 1/32 of a cell, the tiles' density): mockup B.
PROPS.update({
    "reed_s": "one small clump of tall brown and green marsh reeds",
    "stilt_house_s": "one small wooden marsh hut standing on stilt poles, thatched roof",
    "tuft_s": "one tiny clump of green grass blades",
    "flowers_s": "one tiny clump of white and yellow wildflowers",
    "crop_s": "one small young cabbage plant",
    "bush_s": "one small round green bush",
    "cottage_s": "one small village cottage with a red tiled roof, cream plaster walls and a wooden door",
    "peak_s": "one small rocky grey-brown mountain peak with a little snow on top",
})
# Session 7 (user requests): wheat replaces the cabbage crop on field cells; fences and hedges replace the lumped
# bushes on farm-map wall cells. Side-on so they read as a line on the billboard.
PROPS.update({
    "wheat": "one short row of ripe golden wheat stalks with drooping grain heads, straw yellow and pale gold, "
             "seen from the side",
    "fence": "one short straight section of rustic wooden farm fence, two horizontal split rails on three posts, "
             "weathered brown wood, seen from the side",
    "hedge": "one short straight section of a trimmed farm hedgerow, a dense low boxy green hedge with a flat top "
             "and leafy texture, seen from the side",
})
# Straight fence for the 3D fence lines (MapView.AddFence): flat front elevation whose rails run off both edges, so
# sections join into runs and corners along the grid.
PROPS.update({
    "fence_straight": "one straight section of rustic wooden split-rail farm fence seen exactly from the front, flat "
                      "side elevation with no perspective, two horizontal rails running the full width of the image "
                      "from the left edge to the right edge, one thick post in the middle, weathered brown wood",
})
# Session 9: siege clutter for castle maps (Ferrow's Keep): barricade cells pick one of these, rubble cells the last.
PROPS.update({
    "wagon": "one wooden supply wagon with two big spoked wheels and a grey canvas cover, tipped as a barricade",
    "stall": "one small wooden market stall with a striped red and cream cloth awning and a counter of crates",
    "crates": "one stack of three wooden supply crates with rope ties",
    "barrels": "one cluster of three wooden barrels with iron hoops",
    "cart": "one broken wooden hand cart lying on its side with a cracked wheel",
    "rubble": "one low pile of broken grey stone blocks and rubble from a collapsed wall",
})
# Session 15: locked chest cells (Ch 9, the vault) used crates until now (MapView looks for chest_N.png).
PROPS.update({
    "chest": "one closed wooden treasure chest with a rounded lid, iron bands and a heavy iron lock, sitting on the ground",
})
PROP_SIZE = {"tuft": 32, "flowers": 32, "crop": 32, "bush": 32, "wheat": 32, "fence": 32, "hedge": 32,
             "fence_straight": 32, "wagon": 32, "stall": 32, "crates": 32, "barrels": 32, "cart": 32, "rubble": 32, "chest": 32,
             "tuft_s": 16, "flowers_s": 16, "crop_s": 16, "bush_s": 24, "cottage_s": 32, "peak_s": 32, "reed_s": 16, "stilt_house_s": 32}  # others are 64

TILESETS = {
    # Greyfen marsh (Ch 6): shallow water (slow) and reed beds (cover). Picked by hand like the other sets.
    "marsh": [("shallows", "shallow murky green marsh water with small lily pads"),
              ("reeds", "dense tall marsh reeds over shallow water"),
              ("mud", "dark wet marsh mud with puddles"),
              ("boards", "wooden boardwalk planks")],
    # GBA-style top-down tiles with the object drawn into the tile (mockup A, the references' look).
    "objects": [("forest", "cluster of three round green treetops seen from directly above, grass showing between them"),
                ("mountain", "rocky brown mountain peak seen from above, grass around its base"),
                ("fort", "small village cottage with a red tiled roof seen from above, grass around it"),
                ("plain", "green grass with a few tiny flowers")],
    # Grass detail for plain cells (the base plain tile is flat): the main tile plus subtle variants.
    "grass": [("plain", "short green grass with small darker grass tufts"),
              ("plain", "green grass with a few tiny pebbles"),
              ("plain", "green grass with subtle lighter and darker patches"),
              ("plain", "green grass with small clover leaves")],
    # Plain-like ground (same rules as plain) to fill open map areas: road =, field :, meadow ,, square #.
    "ground": [("road", "packed brown dirt road with small pebbles, no edges, fills the whole tile"),
               ("field", "tilled farm field with rows of green crops"),
               ("meadow", "green grass meadow with small white and yellow wildflowers"),
               ("square", "grey cobblestone village square paving")],
    "village": [("fire", "burning house roof with orange flames"),
                ("house", "red tiled cottage roof"),
                ("chapel", "grey slate chapel roof"),
                ("cliff", "brown earth cliff face with stones")],
}
