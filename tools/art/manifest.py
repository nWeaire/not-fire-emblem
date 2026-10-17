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
HORSE_WALK = {"kind": "template", "template": "walk-4-frames"}  # template "horse" (cavalry, pegasi)
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
    "elise_mage": dict(weapon="anima", sets=("map",),
        desc="young travelling scholar woman, long auburn hair, green eyes, worn grey travelling hood and grey cloak, "
             "satchel of papers, holding an open red tome, small flame above her free hand"),
    # Kai's tier 2: mounted on his family's ranch horse (Ch 5). The horse template fits a mounted body.
    "kai_cavalier": dict(weapon="lance", sets=("map",), template="horse",
        desc="18 year old ranch boy knight riding a brown ranch horse, shaggy sandy blond hair, dented iron cap, padded "
             "tan gambeson with a leather breastplate, long wooden spear with an iron tip"),
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
        desc="18 year old knight riding an armoured grey horse, shaggy sandy blond hair, steel plate armour with a blue "
             "tabard, steel lance"),
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
    # Session 7: the session-2 portraits of these two were rounder and more cartoony than the rest of the cast.
    "brann": "big gentle young village blacksmith, broad shoulders, short brown hair, short brown beard, kind brown "
             "eyes, soot smudges on his cheek, scorched leather smith's apron over a plain linen shirt",
    "osmund": "fussy precise old royal steward, about 60, neat grey beard, round spectacles, dry polite expression, "
              "high-collared bottle-green coat with brass buttons, a white quill tucked behind his ear",
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
    # Title screen (session 7): revealed by the crest's light, the menu sits over the middle.
    "title_vista": "a grand sweeping vista of a peaceful fantasy kingdom at sunrise seen from a high hilltop, "
                   "rolling green valleys and farmland, a winding river, small villages, a castle on a distant "
                   "crag, snow-capped mountains on the horizon, golden dawn sky with soft clouds",
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
PROP_SIZE = {"tuft": 32, "flowers": 32, "crop": 32, "bush": 32, "wheat": 32, "fence": 32, "hedge": 32,
             "fence_straight": 32,
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
