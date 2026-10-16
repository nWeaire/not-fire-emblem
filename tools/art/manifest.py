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
MAP_ATTACKS = {
    "sword": "steps forward and slashes a sword in a quick arc, then returns to stance",
    "lance": "thrusts a spear forward in a powerful stab, then pulls back",
    "axe": "raises an axe overhead and chops down hard, then returns to stance",
    "bow": "draws the bow, aims and releases an arrow, then lowers the bow",
    "staff": "raises the staff as soft light glows from its tip",
    "dark": "thrusts a glowing purple hand forward, releasing a burst of shadow magic",
    "claws": "lunges forward and rakes with its claws, then recoils",
    "spikes": "rears back and hurls a bone spike forward, then recovers",
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
    "kai_soldier": dict(weapon="lance", sets=("map",),
        desc="18 year old ranch boy, shaggy sandy blond hair, padded tan cloth gambeson, dented iron cap, long wooden "
             "spear with an iron tip",
        adopt={"map": "871ef5a9-69bd-49ca-9697-b1ad6177a22d", "battle": "42d39467-c587-4c5e-9aaf-01c081463d9c"}),
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
    "rowan_lord": dict(weapon="sword", sets=("map",),
        desc="18 year old young lord, messy dark blue hair, fitted dark blue wool doublet with a few silver buttons, "
             "short grey travelling cloak, leather bracers, brown trousers, boots, plain iron sword"),
    "brann_fighter": dict(weapon="axe", sets=("map",),
        desc="big burly young village blacksmith, short brown beard, scorched leather apron over a plain shirt, bare "
             "muscular arms, heavy iron wood-cutting axe"),
    "ally_villager": dict(weapon="none", sets=("map",),
        desc="plain village farmer man, brown hair, beige linen shirt, green wool vest, brown trousers, no weapon"),
}

# Tier 2+ outfits for later (ART_NEEDED P2). Not generated until moved into CHARACTERS.
PLANNED = {
    "rowan_crest_knight": "young knight-lord, messy dark blue hair, steel breastplate over a dark blue tunic, short "
                          "blue cape with silver trim, steel sword",
    "aldric_warden_lord": "grizzled 50 year old lord-knight, grey-streaked beard, black leather armour with silver "
                          "plates and sigils, long black cloak, steel greatsword",
    "tamsin_sniper": "young woman sniper, auburn hair, dark green hooded cloak, leather armour, longbow",
    "brann_warrior": "burly warrior, brown beard, chainmail with a fur mantle, iron gauntlets, steel battleaxe",
    "kai_paladin": "young knight in steel plate with a blue tabard, sandy blond hair, riding an armoured grey horse",
    "mira_sage": "young sage woman, blonde braid, white and blue robes with silver embroidery, ornate silver staff",
}

# ------------------------------------------------------------------ portraits (generate-image-v2, 4 takes at 96 px)

PORTRAIT_STYLE = ("Game Boy Advance Fire Emblem dialogue portrait, anime bust shoulders up, facing left in "
                  "three-quarter view, 16-bit GBA pixel art, about 16 colours, hard cel shading, bold dark outline, "
                  "no gradients, like Fire Emblem: The Blazing Blade")
PORTRAITS = {
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
}

# ------------------------------------------------------------------ backgrounds (generate-image-v2, 240x135)
# One image per location: the Story screen tints it for the time of day (farm_evening, farm_night, ...), and the
# battle screen uses the same images behind the fighters. No people, ground in the lower half.

BG_STYLE = ("Game Boy Advance Fire Emblem battle background, 16-bit GBA pixel art, flat simple shading, no people, "
            "no characters")
BG_SIZE = (240, 135)
BACKGROUNDS = {
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
}

# ------------------------------------------------------------------ terrain (create-tiles-pro, 32 px)
# Extra tiles beyond the approved set (approved/terrain_gba32). Mapping of the 16 results is chosen by hand.

TILE_STYLE = ("Game Boy Advance Fire Emblem map tile, flat bright colours, very limited palette, simple shapes, "
              "seamless top-down")
TILESETS = {
    "village": [("fire", "burning house roof with orange flames"),
                ("house", "red tiled cottage roof"),
                ("chapel", "grey slate chapel roof"),
                ("cliff", "brown earth cliff face with stones")],
}
