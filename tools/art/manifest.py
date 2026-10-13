"""What art to request from PixelLab, in a GBA Fire Emblem style.

Edit this file to add units, tweak prompts or change animations; generate.py only submits what
is not already recorded in state.json, so re-running never pays twice for the same asset.
"""

# ------------------------------------------------------------------ shared style

STYLE = (
    "in the style of Game Boy Advance Fire Emblem map sprites, 16-bit GBA era pixel art, "
    "limited palette, clean dark outline, bold readable silhouette, slightly chibi proportions"
)
PORTRAIT_STYLE = (
    "Game Boy Advance Fire Emblem dialogue portrait, anime style bust, shoulders up, three-quarter view, "
    "16-bit GBA era pixel art, limited palette with soft cel shading, clean dark outline"
)
ENEMY = "red and dark brown colour scheme"

SPRITE_SIZE = 48          # character body size in px (canvas is larger to leave room for motion)
VIEW = "low top-down"     # matches the tilted tactics camera
DIRECTIONS = ["south", "east", "north", "west"]  # the camera rotates in 90° steps, so 4 are enough

# ------------------------------------------------------------------ animations
# kind "template": PixelLab skeleton template (cheap, humanoids only).
# kind "v3": custom motion from text (works for any body, including mounts).

IDLE = {"kind": "template", "template": "breathing-idle"}
WALK = {"kind": "template", "template": "walking-4-frames"}
HIT = {"kind": "v3", "action": "flinches backwards from a hit, then recovers", "frames": 4}


def attack(action: str, frames: int = 6) -> dict:
    return {"kind": "v3", "action": action, "frames": frames}


def mounted(motion: str, frames: int = 4) -> dict:
    return {"kind": "v3", "action": motion, "frames": frames}


SWORD = attack("steps forward and slashes a sword in a quick arc, then returns to stance")
LANCE = attack("thrusts a lance forward in a powerful stab, then pulls back")
AXE = attack("raises an axe overhead and chops down hard, then returns to stance")
BOW = attack("draws the bow, aims and releases an arrow, then lowers the bow")
FIRE = attack("raises a hand and casts a burst of fire magic from an open spellbook")
HEAL = attack("raises a staff as soft white healing light glows from its tip")

HUMANOID = {"template_id": "mannequin"}
HORSE = {"template_id": "horse"}


def humanoid_anims(attack_anim: dict) -> dict:
    return {"idle": IDLE, "walk": WALK, "attack": attack_anim, "hit": HIT}


def mount_anims(attack_anim: dict, gait: str) -> dict:
    return {
        "idle": mounted("idle, the mount shifts its weight and breathes, rider sits ready"),
        "walk": mounted(gait),
        "attack": attack_anim,
        "hit": HIT,
    }


# ------------------------------------------------------------------ outfits (playable characters)
# A playable character's look follows their CLASS: sprite id = "<unitId>_<classId>" (the game falls back
# to "<unitId>"). The cast starts as 18-year-old country kids in plain, boring gear; every class tier
# makes them visibly grander, ending in gold-trimmed armour and fancy robes.
#   tier 0 = country (no armour) · 1 = first real kit · 2 = proper steel / good robes · 3 = gold trim, ornate
# "planned": True entries are written down now but only generated when that class exists in the game.

COUNTRY = "plain worn country clothes, muted earthy colours, no armour, no jewellery"

OUTFITS = [
    # --- Rowan: farm boy -> lord -> sovereign
    dict(id="rowan_youth", tier=0, weapon="sword", body=HUMANOID,
         desc=f"18 year old country farm boy, messy dark blue hair, freckles, homespun off-white linen shirt with rolled "
              f"sleeves, brown leather vest, patched brown trousers, worn boots, short hunting bow on his back, "
              f"plain iron sword, {COUNTRY}"),
    dict(id="rowan_lord", tier=1, weapon="sword", body=HUMANOID,
         desc="18 year old young man newly dressed as a village lord, messy dark blue hair, fitted dark blue wool "
              "doublet with a few silver buttons, simple grey travelling cloak, leather bracers, belt with a small "
              "crest buckle, hunting bow on his back, iron sword, modest but clearly noble"),
    dict(id="rowan_crest_knight", tier=2, weapon="sword", body=HUMANOID, planned=True,
         desc="young knight-lord, messy dark blue hair, steel breastplate over a dark blue tunic, short blue cape "
              "with silver trim, steel sword, bow on his back"),
    dict(id="rowan_high_lord", tier=3, weapon="sword", body=HUMANOID, planned=True,
         desc="young high lord, dark blue hair, polished plate armour with gold trim, long white cape, fine longsword"),
    dict(id="rowan_sovereign", tier=4, weapon="sword", body=HUMANOID, planned=True,
         desc="young king, dark blue hair, radiant white and gold regal armour, flowing white cape with gold "
              "embroidery, slim gold circlet, glowing white sigil on his hand, gleaming sword"),

    # --- Kai: ranch boy with a farm spear -> knight
    dict(id="kai_soldier", tier=0, weapon="lance", body=HUMANOID,
         desc=f"18 year old country ranch boy, shaggy sandy blond hair, sun-tanned, cocky grin, padded cloth "
              f"gambeson over a work shirt, rope belt, dented old iron cap, long wooden farm spear with an iron tip, {COUNTRY}"),
    dict(id="kai_cavalier", tier=1, weapon="lance", body=HORSE,
         desc="18 year old ranch boy riding a sturdy brown farm horse, shaggy sandy blond hair, boiled leather armour "
              "over a work shirt, plain iron lance, simple saddle"),
    dict(id="kai_paladin", tier=2, weapon="lance", body=HORSE, planned=True,
         desc="young knight in steel plate armour with a blue tabard and silver trim, sandy blond hair, riding an "
              "armoured grey warhorse, steel lance"),
    dict(id="kai_vanguard", tier=3, weapon="lance", body=HORSE, planned=True,
         desc="young champion knight in ornate steel plate with gold trim, plumed helm, riding a barded white "
              "warhorse with a gold caparison, gleaming silver lance"),

    # --- Mira: healer's apprentice -> sage -> oracle
    dict(id="mira_acolyte", tier=0, weapon="staff", body=HUMANOID,
         desc=f"18 year old village healer's apprentice girl, long blonde hair in a single braid, plain grey-green wool "
              f"dress with a white apron, leather healer's satchel, simple wooden walking staff, {COUNTRY}"),
    dict(id="mira_sage", tier=2, weapon="fire", body=HUMANOID, planned=True,
         desc="young sage woman, blonde braid, white and blue robes with silver embroidery, ornate silver staff, "
              "spell tome at her hip"),
    dict(id="mira_oracle", tier=3, weapon="fire", body=HUMANOID, planned=True,
         desc="young oracle, blonde hair, flowing white and gold robes, gilded staff topped with a glowing crystal, "
              "delicate gold circlet"),

    # --- Brann: village blacksmith -> warrior -> ironheart
    dict(id="brann_fighter", tier=0, weapon="axe", body=HUMANOID,
         desc=f"big young village blacksmith, short brown beard, soot on his face, scorched leather apron over a "
              f"plain shirt, bare muscular arms, heavy iron wood-cutting axe, {COUNTRY}"),
    dict(id="brann_warrior", tier=2, weapon="axe", body=HUMANOID, planned=True,
         desc="burly warrior, brown beard, chainmail with a fur mantle, iron gauntlets, steel battleaxe"),
    dict(id="brann_ironheart", tier=3, weapon="axe", body=HUMANOID, planned=True,
         desc="burly smith-warrior in heavy self-forged plate armour with gold trim and rivets, huge ornate greataxe"),

    # --- Elise: travelling scholar -> sage -> crestscribe
    dict(id="elise_mage", tier=0, weapon="fire", body=HUMANOID,
         desc="young travelling scholar woman, long auburn hair, ink-stained fingers, worn grey travelling robe, "
              "satchel stuffed with books and scrolls, plain leather spell tome"),
    dict(id="elise_sage", tier=2, weapon="fire", body=HUMANOID, planned=True,
         desc="young sage, auburn hair, deep blue robes with gold embroidery, hovering spell tome"),
    dict(id="elise_crestscribe", tier=3, weapon="fire", body=HUMANOID, planned=True,
         desc="young crest scholar, auburn hair, ornate midnight blue and gold robes covered in glowing sigils, "
              "floating open grimoire"),
]

ACTIVE_OUTFITS = [o for o in OUTFITS if not o.get("planned")]

_MAP_ATTACK = {"sword": SWORD, "lance": LANCE, "axe": AXE, "bow": BOW, "fire": FIRE, "staff": HEAL}


def _map_anims(outfit: dict) -> dict:
    attack_anim = _MAP_ATTACK[outfit["weapon"]]
    if outfit["body"] is HORSE:
        return mount_anims(attack_anim, "the horse trots forward with the rider bobbing")
    return humanoid_anims(attack_anim)


# ------------------------------------------------------------------ characters (map sprites)
# Order is priority: when the monthly budget runs short, earlier entries are generated first.
# Keys are sprite ids: playable outfits are "<unitId>_<classId>"; generic enemies use "enemy_<classId>".

ENEMIES = {
    "enemy_fighter": {
        "description": f"bandit brigand, red bandana, shirtless with leather straps, iron axe, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(AXE),
    },
    "enemy_archer": {
        "description": f"bandit archer, red hood, leather armour, wooden bow and quiver, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(BOW),
    },
    "enemy_soldier": {
        "description": f"foot soldier, red steel helmet, red tabard over chainmail, iron lance, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(LANCE),
    },
    "enemy_myrmidon": {
        "description": f"lean mercenary swordsman, spiky dark hair, red sash and light garb, curved sword, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(SWORD),
    },
    "enemy_mage": {
        "description": f"hedge mage, dark red hooded robe, shadowed face, glowing spellbook, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(FIRE),
    },
    "enemy_pegasus_knight": {
        "description": f"pegasus knight riding a white winged horse, red armour, rider holding a lance, {ENEMY}",
        "body": HORSE, "animations": mount_anims(LANCE, "the pegasus flaps its wings and hovers forward"),
    },
    "boss_brigand_chief": {
        "description": f"hulking brigand chief, horned iron helmet, red fur cape, scarred bare chest, huge battle axe, {ENEMY}",
        "body": HUMANOID, "animations": humanoid_anims(AXE),
    },
}

CHARACTERS = {
    **{o["id"]: {"description": o["desc"], "body": o["body"], "animations": _map_anims(o)} for o in ACTIVE_OUTFITS},
    **ENEMIES,
}

# Animations are requested in this order across all characters, so every unit can at least walk
# and fight before any budget goes on hit reactions.
ANIMATION_PRIORITY = ["idle", "walk", "attack", "hit"]

# ------------------------------------------------------------------ dialogue portraits
# One portrait per character, drawn in their starting (country) look.

PORTRAIT_SIZE = 96
PORTRAITS = {
    "rowan": "18 year old country farm boy, messy dark blue hair, freckles, earnest steady eyes, off-white linen "
             "shirt and brown leather vest",
    "kai": "18 year old ranch boy, shaggy sandy blond hair, sun-tanned, big cocky grin, padded cloth gambeson",
    "mira": "18 year old village healer's apprentice girl, long blonde hair in a braid, kind but determined eyes, "
            "grey-green wool dress with a white apron collar",
    "osmund": "elderly former royal steward, neatly trimmed grey beard, small round spectacles, high-collared dark "
              "green coat with brass buttons, quill tucked behind one ear, dry knowing expression",
    "brann": "big young village blacksmith, short brown beard, soot-smudged cheerful face, scorched leather apron",
    "elise": "young travelling scholar woman, long auburn hair, curious green eyes, worn grey travelling hood, "
             "ink smudge on her cheek",
    "boss_brigand_chief": "menacing brigand chief, horned iron helmet, scarred face, wild black beard, red fur cape",
}

# ------------------------------------------------------------------ terrain tiles (top faces of the 3D map blocks)

TERRAIN_TILE_SIZE = 32
TERRAIN_TILES = [
    # (terrain id, prompt) — order matters: PixelLab returns tiles in the numbered order
    ("plain", "lush green grass field"),
    ("forest", "dense forest floor with small bushes and dark green undergrowth"),
    ("mountain", "rocky brown mountain stone with cracks"),
    ("wall", "grey castle stone brick wall top"),
    ("fort", "packed earth courtyard floor with stone border"),
    ("water", "clear blue river water with small ripples"),
    ("bridge", "wooden plank bridge running north to south"),
]
TERRAIN_STYLE = "Game Boy Advance Fire Emblem map tile, 16-bit GBA era pixel art, limited palette, seamless top-down"

# ------------------------------------------------------------------ battle sprites (side-view cut-in)
# Full-body side-view sprites for the Fire Emblem style battle screen. Only "east" (facing right) is
# generated: the game mirrors it for whoever stands on the right, so each animation costs one job.

BATTLE_STYLE = (
    "in the style of Game Boy Advance Fire Emblem battle animation sprites, side view, full body, "
    "16-bit GBA era pixel art, limited palette, clean dark outline, detailed cel shading"
)
BATTLE_SIZE = 96
BATTLE_VIEW = "side"
BATTLE_DIRECTIONS = ["east"]

BATTLE_IDLE = {"kind": "v3", "action": "stands in a battle-ready stance, weapon at the ready, subtle breathing", "frames": 4}
BATTLE_DODGE = {"kind": "v3", "action": "quickly leaps backward to dodge an attack, then returns to stance", "frames": 6}
BATTLE_HIT = {"kind": "v3", "action": "staggers backward from a heavy blow, then recovers", "frames": 4}

BATTLE_ATTACKS = {
    # weapon: (standard attack, critical attack)
    "sword": ("dashes forward and slashes the sword in a fast arc, then hops back to stance",
              "spins the sword overhead, blade gleaming, leaps forward and delivers a devastating double slash"),
    "lance": ("steps forward and thrusts the lance in a powerful stab, then pulls back",
              "twirls the lance, charges forward and drives it in a mighty, glowing piercing thrust"),
    "axe": ("charges forward and swings the axe down in a heavy chop, then returns",
            "hurls the axe into a spinning arc, leaps high and slams it down with a crushing overhead blow"),
    "bow": ("draws the bow and looses an arrow forward, then lowers the bow",
            "kneels, draws the bow to full tension with a glint, and fires a powerful glowing arrow"),
    "fire": ("raises an open spellbook and launches a fireball forward",
             "spellbook glows as flames swirl around the caster, then unleashes a huge roaring fire blast"),
    "staff": ("raises the staff and swings it forward in a light strike",
              "raises the staff as bright holy light gathers, then strikes forward in a radiant burst"),
}
BATTLE_PRIORITY = ["idle", "attack", "hit", "dodge", "critical"]

_BATTLE_WEAPON = {
    **{o["id"]: o["weapon"] for o in ACTIVE_OUTFITS},
    "enemy_fighter": "axe", "enemy_archer": "bow", "enemy_soldier": "lance", "enemy_myrmidon": "sword",
    "enemy_mage": "fire", "enemy_pegasus_knight": "lance", "boss_brigand_chief": "axe",
}


def _battle_anims(weapon: str) -> dict:
    attack_text, crit_text = BATTLE_ATTACKS[weapon]
    return {
        "idle": BATTLE_IDLE,
        "attack": {"kind": "v3", "action": attack_text, "frames": 8},
        "critical": {"kind": "v3", "action": crit_text, "frames": 12},
        "dodge": BATTLE_DODGE,
        "hit": BATTLE_HIT,
    }


BATTLE_CHARACTERS = {
    sid: {"description": CHARACTERS[sid]["description"], "body": CHARACTERS[sid]["body"], "animations": _battle_anims(w)}
    for sid, w in _BATTLE_WEAPON.items()
}

# ------------------------------------------------------------------ art sets (generate.py --set)

SETS = {
    "map": {"specs": CHARACTERS, "size": SPRITE_SIZE, "view": VIEW, "directions": DIRECTIONS,
            "priority": ANIMATION_PRIORITY, "style": STYLE, "out": "assets/sprites/units", "state_key": "characters"},
    "battle": {"specs": BATTLE_CHARACTERS, "size": BATTLE_SIZE, "view": BATTLE_VIEW, "directions": BATTLE_DIRECTIONS,
               "priority": BATTLE_PRIORITY, "style": BATTLE_STYLE, "out": "assets/sprites/battle", "state_key": "battle"},
}
