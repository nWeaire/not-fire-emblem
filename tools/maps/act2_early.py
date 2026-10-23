"""Writes the Act II maps (docs/story/CHAPTERS.md), at the Act I scale (session 7: +60% per side over the outline),
and the content they bring in: door and chest terrain, keys, the Thief and Rogue classes, and Pip.

Each map is painted onto a grid like act1_early.py. The .tres files it writes are canonical afterwards (edit them
directly); rerunning this overwrites them.
    python tools/maps/act2_early.py
Dialogue @focus cells for these maps live in data/dialogue/ch08.txt and ch09.txt.
"""
from act1_ch4_6 import DATA, PLAYER, ENEMY, ALLY, AGGRESSIVE, STATIONARY, GUARD, BOSS, map_tres, stats, terrain, unit, write
from act1_early import Grid, assert_connected, finish

NEUTRAL = 3


# ------------------------------------------------------------------ content

def terrains():
    # Locked doors: impassable to everyone, block sight; drawn like Ch 5's gate. Opened (Unlock) they become floor (#).
    terrain("door", "Locked Door", "D", 0, 0, 0, 0, 0, 0, "Color(0.45, 0.30, 0.16, 1)")
    path = DATA / "terrain" / "door.tres"
    path.write_text(path.read_text(encoding="utf-8").replace("BlocksSight = false", "BlocksSight = true"),
                    encoding="utf-8", newline="\n")
    # Chests: floor with a chest on it. Stand on it and Unlock (Locktouch or a Chest Key) for what's inside.
    terrain("chest", "Chest", "C", 1, 1, 1, 1, 0, 0, "Color(0.72, 0.55, 0.20, 1)")


def items():
    for iid, name, price in [("door_key", "Door Key", 100), ("chest_key", "Chest Key", 150)]:
        write(DATA / "items" / f"{iid}.tres", f"""[gd_resource type="Resource" script_class="ItemDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/ItemDefinition.cs" id="1_item"]

[resource]
script = ExtResource("1_item")
Id = "{iid}"
Name = "{name}"
Kind = 2
Amount = 0
Stat = 0
Price = {price}
""")


INFANTRY, ARMORED, CAVALRY, FLYING = 0, 1, 2, 3


def weapon_tres(wid, name, wtype, might, hit, crit, weight, rmin, rmax, price, tier, effective=()):
    q = lambda xs: ", ".join(f'"{x}"' for x in xs)
    write(DATA / "weapons" / f"{wid}.tres", f"""[gd_resource type="Resource" script_class="WeaponDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/WeaponDefinition.cs" id="1_weapon"]

[resource]
script = ExtResource("1_weapon")
Id = "{wid}"
Name = "{name}"
Type = {wtype}
Might = {might}
Hit = {hit}
Crit = {crit}
Weight = {weight}
MinRange = {rmin}
MaxRange = {rmax}
Magic = false
EffectiveAgainst = PackedStringArray({q(effective)})
Price = {price}
Tier = {tier}
""")


def weapons():
    # Halde's crossbow (Ch 12): hits hard, aims well, heavy to carry.
    weapon_tres("crossbow", "Crossbow", 3, 11, 70, 5, 9, 2, 2, 1600, 2, ["Flying"])


LIGHT, MEDIUM, HEAVY = 0, 1, 2  # ArmorClass: the heaviest body armour a class can wear


def class_tres(cid, name, bases, growths, weapons, promotes_to, promoted, tier, traits, arts=(), spells=(), move=INFANTRY,
               armor=LIGHT, promotes_from=""):
    caps = dict(HP=60, Str=30, Mag=30, Dex=30, Spd=30, Lck=30, Def=30, Res=30, Mov=10)
    q = lambda xs: ", ".join(f'"{x}"' for x in xs)
    write(DATA / "classes" / f"{cid}.tres", f"""[gd_resource type="Resource" script_class="ClassDefinition" load_steps=6 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/ClassDefinition.cs" id="1_class"]
[ext_resource type="Script" path="res://src/Units/Resources/StatsDefinition.cs" id="2_stats"]

{stats("StatsDefinition_1", **bases)}
{stats("StatsDefinition_2", **growths)}
{stats("StatsDefinition_3", **caps)}
[resource]
script = ExtResource("1_class")
Id = "{cid}"
Name = "{name}"
Bases = SubResource("StatsDefinition_1")
Growths = SubResource("StatsDefinition_2")
Caps = SubResource("StatsDefinition_3")
MovementType = {move}
UsableWeapons = PackedStringArray({q(weapons)})
PromotesTo = PackedStringArray({q(promotes_to)})
IsPromoted = {"true" if promoted else "false"}
MaxArmor = {armor}
Tier = {tier}
Traits = PackedStringArray({q(traits)})
Arts = PackedStringArray({q(arts)})
Spells = PackedStringArray({q(spells)})
PromotesFrom = "{promotes_from}"
""")


def classes():
    # Pip's line (STORY §6: Thief -> Rogue -> Shadowstep). Fast and lucky, light swords, opens locks and steals.
    class_tres("thief", "Thief", dict(HP=16, Str=3, Dex=5, Spd=9, Lck=5, Def=2, Res=1, Mov=6),
               dict(HP=5, Str=5, Dex=10, Spd=15, Lck=10), ["Sword"], ["rogue"], False, 1, ["Locktouch", "Steal"])
    class_tres("rogue", "Rogue", dict(HP=22, Str=6, Mag=1, Dex=10, Spd=13, Lck=7, Def=4, Res=3, Mov=7),
               dict(HP=10, Str=5, Dex=10, Spd=10, Lck=10), ["Sword"], [], True, 2, ["Locktouch", "Steal"])
    # Seraphine's line (Troubadour -> Valkyrie -> Lightbearer): a mounted healer who learns attack magic on promotion.
    class_tres("troubadour", "Troubadour", dict(HP=15, Mag=5, Dex=4, Spd=7, Lck=6, Def=2, Res=6, Mov=7),
               dict(HP=5, Mag=10, Spd=5, Res=10), ["WhiteMagic"], ["valkyrie"], False, 1, [], spells=["heal"], move=CAVALRY)
    class_tres("valkyrie", "Valkyrie", dict(HP=20, Mag=8, Dex=6, Spd=9, Lck=7, Def=4, Res=9, Mov=8),
               dict(HP=10, Mag=10, Spd=5, Res=10), ["WhiteMagic", "BlackMagic"], [], True, 2, [], spells=["heal", "physic"],
               move=CAVALRY)
    # Dravenhold's wyvern corps (Ch 10 on; Ysra in Ch 11): heavy fliers, lances; bows and Wind are effective.
    class_tres("wyvern_rider", "Wyvern Rider", dict(HP=20, Str=7, Dex=6, Spd=5, Lck=2, Def=8, Mov=7),
               dict(HP=10, Str=10, Def=10), ["Lance"], ["wyvern_lord"], False, 1, [], move=FLYING)
    class_tres("wyvern_lord", "Wyvern Lord", dict(HP=26, Str=10, Dex=8, Spd=7, Lck=3, Def=11, Res=2, Mov=8),
               dict(HP=10, Str=10, Def=10), ["Lance", "Axe"], [], True, 2, [], move=FLYING)
    tier2_classes()
    crestbound_classes()
    # The Hollowed (Ch 15 on): soldiers consumed by the Hollow Crest. Grey, silent, relentless; any blade they find.
    class_tres("hollowed", "Hollowed", dict(HP=26, Str=9, Dex=6, Spd=5, Def=7, Res=2, Mov=5),
               dict(HP=10, Str=5, Def=5), ["Sword", "Lance", "Axe"], [], False, 1, ["Hollowed"], armor=MEDIUM)


def tier2_classes():
    # Tier 2 for the Act I companions who had none (session 12): Dorran, Sable and Wren couldn't promote after Ch 7.
    # The General takes up axes too (the "knights that can use axes" idea from session 11).
    class_tres("general", "General", dict(HP=28, Str=10, Dex=7, Spd=4, Lck=2, Def=14, Res=3, Mov=5),
               dict(HP=15, Str=5, Def=15), ["Lance", "Axe"], [], True, 2, [], move=ARMORED, armor=HEAVY, promotes_from="knight")
    class_tres("swordmaster", "Swordmaster", dict(HP=22, Str=8, Dex=12, Spd=14, Lck=5, Def=6, Res=3, Mov=6),
               dict(HP=5, Str=5, Dex=10, Spd=15), ["Sword"], [], True, 2, [], arts=["swift_cut"], promotes_from="myrmidon")
    class_tres("falcon_knight", "Falcon Knight", dict(HP=22, Str=7, Mag=3, Dex=9, Spd=13, Lck=7, Def=6, Res=9, Mov=8),
               dict(Dex=5, Spd=15, Lck=5, Res=10), ["Lance", "Sword"], [], True, 2, [], arts=["sky_lance"], move=FLYING,
               promotes_from="pegasus_knight")


def crestbound_classes():
    # Tier 3 (STORY §4.2, the Lord's Crest after Ch 13): one class per companion (UnitDef.Crestbound), each about its
    # tier 2 plus 4 HP and +2 to what the class lives by, sometimes a new weapon. Signature arts are still to design.
    def cb(cid, name, base, bases, growths, weapons, arts=(), spells=(), traits=(), move=INFANTRY, armor=LIGHT):
        class_tres(cid, name, bases, growths, weapons, [], True, 3, list(traits), arts=arts, spells=spells, move=move,
                   armor=armor, promotes_from=base)
    cb("high_lord", "High Lord", "crest_knight", dict(HP=28, Str=11, Mag=3, Dex=12, Spd=12, Lck=8, Def=10, Res=6, Mov=7),
       dict(HP=10, Str=5, Dex=5, Spd=5, Lck=5), ["Sword", "Lance"], armor=MEDIUM)
    cb("vanguard", "Vanguard", "paladin", dict(HP=30, Str=11, Mag=1, Dex=10, Spd=10, Lck=6, Def=12, Res=6, Mov=8),
       dict(HP=10, Str=5, Dex=5, Spd=5, Def=5, Res=5), ["Sword", "Lance", "Axe"], move=CAVALRY, armor=HEAVY)
    cb("oracle", "Oracle", "sage", dict(HP=23, Str=2, Mag=13, Dex=8, Spd=9, Lck=6, Def=5, Res=12, Mov=6),
       dict(Mag=15, Dex=5, Spd=5, Res=10), ["BlackMagic", "WhiteMagic"], spells=["heal", "physic", "mend"])
    cb("crestscribe", "Crestscribe", "sage", dict(HP=22, Str=2, Mag=14, Dex=9, Spd=9, Lck=5, Def=4, Res=11, Mov=6),
       dict(Mag=15, Dex=5, Spd=5, Res=10), ["BlackMagic", "WhiteMagic"], spells=["heal"])
    cb("ironheart", "Ironheart", "warrior", dict(HP=34, Str=14, Dex=8, Spd=9, Lck=4, Def=10, Res=3, Mov=6),
       dict(HP=20, Str=10, Dex=5, Spd=5), ["Axe", "Bow"], armor=HEAVY)
    cb("wildstalker", "Wildstalker", "sniper", dict(HP=25, Str=10, Dex=14, Spd=12, Lck=6, Def=7, Res=4, Mov=7),
       dict(HP=10, Str=5, Dex=10), ["Bow"], arts=["longshot"])
    cb("corsair", "Corsair", "sniper", dict(HP=26, Str=11, Dex=13, Spd=11, Lck=6, Def=8, Res=4, Mov=6),
       dict(HP=10, Str=5, Dex=10), ["Bow", "Sword"], arts=["longshot"])
    cb("bulwark", "Bulwark", "general", dict(HP=32, Str=12, Dex=8, Spd=5, Lck=3, Def=17, Res=5, Mov=5),
       dict(HP=15, Str=5, Def=15), ["Lance", "Axe", "Sword"], move=ARMORED, armor=HEAVY)
    cb("blade_dancer", "Blade Dancer", "swordmaster", dict(HP=25, Str=10, Dex=14, Spd=16, Lck=7, Def=7, Res=4, Mov=7),
       dict(HP=5, Str=5, Dex=10, Spd=15), ["Sword"], arts=["swift_cut"])
    cb("skyward", "Skyward", "falcon_knight", dict(HP=25, Str=9, Mag=4, Dex=11, Spd=15, Lck=8, Def=7, Res=11, Mov=9),
       dict(Dex=5, Spd=15, Lck=5, Res=10), ["Lance", "Sword"], arts=["sky_lance"], move=FLYING)
    cb("shadowstep", "Shadowstep", "rogue", dict(HP=25, Str=8, Mag=1, Dex=12, Spd=16, Lck=9, Def=5, Res=4, Mov=8),
       dict(HP=10, Str=5, Dex=10, Spd=10, Lck=10), ["Sword"], traits=["Locktouch", "Steal"])
    cb("lightbearer", "Lightbearer", "valkyrie", dict(HP=24, Mag=11, Dex=8, Spd=11, Lck=9, Def=5, Res=12, Mov=8),
       dict(HP=10, Mag=10, Spd=5, Res=10), ["WhiteMagic", "BlackMagic"], spells=["heal", "physic", "mend"], move=CAVALRY)
    cb("dragonheart", "Dragonheart", "wyvern_lord", dict(HP=30, Str=13, Dex=10, Spd=9, Lck=4, Def=13, Res=4, Mov=8),
       dict(HP=10, Str=10, Def=10), ["Lance", "Axe"], move=FLYING)


# Each companion's Crestbound class (STORY §6 class paths). Aldric has none: his line ends at Warden Lord.
CRESTBOUND = {"rowan": "high_lord", "kai": "vanguard", "mira": "oracle", "elise": "crestscribe", "brann": "ironheart",
              "tamsin": "wildstalker", "halde": "corsair", "dorran": "bulwark", "sable": "blade_dancer", "wren": "skyward",
              "pip": "shadowstep", "seraphine": "lightbearer", "ysra": "dragonheart"}


def tier1_routes():
    """Adds the new tier 2 classes to the tier 1 classes' PromotesTo, and each unit's Crestbound class."""
    import re
    for cid, target in [("knight", "general"), ("myrmidon", "swordmaster"), ("pegasus_knight", "falcon_knight")]:
        path = DATA / "classes" / f"{cid}.tres"
        text = path.read_text(encoding="utf-8")
        text = re.sub(r'PromotesTo = PackedStringArray\(\)', f'PromotesTo = PackedStringArray("{target}")', text)
        path.write_text(text, encoding="utf-8", newline="\n")
    for uid, cid in CRESTBOUND.items():
        path = DATA / "units" / f"{uid}.tres"
        text = path.read_text(encoding="utf-8")
        text = re.sub(r'\nCrestbound = "[^"]*"', "", text).rstrip("\n") + f'\nCrestbound = "{cid}"\n'
        path.write_text(text, encoding="utf-8", newline="\n")


def units():
    unit("pip", "Pip", "thief", 6, dict(HP=1, Dex=2, Spd=3, Lck=4),
         dict(HP=50, Str=30, Mag=5, Dex=55, Spd=75, Lck=70, Def=15, Res=25), ["iron_sword", "vulnerary"], "")
    unit("seraphine", "Seraphine", "troubadour", 9, dict(HP=1, Mag=2, Lck=2, Res=3),
         dict(HP=45, Str=15, Mag=55, Dex=40, Spd=50, Lck=60, Def=15, Res=55), ["vulnerary"], "")
    unit("halde", "Halde", "archer", 12, dict(HP=2, Str=1, Dex=2, Lck=1),
         dict(HP=55, Str=45, Mag=5, Dex=55, Spd=35, Lck=40, Def=30, Res=20), ["crossbow", "steel_bow"], "leather_jerkin")
    # Empress Ysolde: Ch 14's dominated enemy you mustn't kill (NonLethal); an NPC ally for the finale.
    unit("ysolde", "Ysolde", "wyvern_lord", 16, dict(HP=4, Str=2, Dex=2, Spd=2, Lck=3, Def=2, Res=3),
         dict(HP=50, Str=50, Mag=10, Dex=45, Spd=40, Lck=40, Def=40, Res=30), ["silver_lance", "steel_axe"], "")
    unit("ysra", "Ysra", "wyvern_rider", 11, dict(HP=2, Str=2, Dex=1, Def=2),
         dict(HP=60, Str=50, Mag=5, Dex=45, Spd=35, Lck=30, Def=40, Res=15), ["steel_lance", "javelin"], "chainmail")


# ------------------------------------------------------------------ Chapter 8: Riot in the Capital (32×26)

def ch8():
    g = Grid(32, 26, "#")
    # The palace precinct wall along the north edge, its gate shut; the city wall and gate to the south.
    g.rect(0, 0, 31, 1, "W")
    g.raise_(0, 0, 31, 1, 2)
    g.spots("G", [(15, 1), (16, 1)])
    g.raise_(15, 1, 16, 1, 0)
    g.rect(0, 25, 31, 25, "W")
    g.raise_(0, 25, 31, 25, 2)
    g.spots("=", [(15, 25), (16, 25)])
    g.raise_(15, 25, 16, 25, 0)
    # The royal avenue (north-south) and the guild street (east-west), meeting at the fountain.
    g.rect(15, 2, 16, 24, "=")
    g.rect(0, 12, 31, 13, "=")
    g.rect(14, 11, 17, 14, "#")
    g.rect(15, 12, 16, 13, "~")

    def block(x0, y0, x1, y1, height=2):
        g.rect(x0, y0, x1, y1, "W")
        g.raise_(x0, y0, x1, y1, height)

    # North-west: tenements and the alleys between them (the first agitator works the alleys).
    block(1, 3, 5, 5)
    block(8, 3, 12, 5)
    block(1, 8, 3, 10)
    block(6, 7, 9, 10, 1)
    block(11, 8, 12, 10)
    g.spots("T", [(4, 6), (10, 6)])
    # North-east: the market. Rows of stalls (barricades, drawn as stalls and carts), shops behind them.
    block(27, 3, 30, 6)
    block(19, 3, 22, 4, 1)
    g.spots("k", [(19, 7), (21, 7), (23, 7), (25, 7), (19, 9), (21, 9), (23, 9), (25, 9), (28, 9)])
    g.spots("T", [(24, 4), (29, 7)])
    # South-west: the Lantern Garden (trees and lawn) and a chapel.
    g.rect(2, 16, 9, 21, ",")
    g.spots("F", [(2, 16), (3, 21), (8, 16), (9, 20), (5, 18)])
    block(11, 16, 13, 18)
    block(1, 23, 6, 24, 1)
    g.put(12, 19, "T")
    # South-east: the guildhalls, with a covered yard.
    block(19, 16, 22, 18)
    block(25, 15, 30, 17)
    block(24, 20, 28, 22)
    block(19, 21, 21, 23, 1)
    g.spots("T", [(23, 19), (29, 19)])
    g.spots("k", [(26, 18), (22, 20)])
    assert_connected(g, (15, 24))

    # The Veiled leave the townsfolk to the mob (Ignores) and fight the party. Agitators hang back and shout (Guard).
    def agitator(cls, weapon, pos, lvl=9, **kv):
        return dict(ClassId=cls, Name="Veiled Agitator", Tag="agitator", SpriteId="enemy_veiled_agitator", Level=lvl,
                    Inventory=[weapon], Pos=pos, Armor="padded_gambeson", Ignores="civilian", Ai=GUARD, **kv)

    def thug(cls, weapon, pos, lvl=8, group="", **kv):
        return dict(ClassId=cls, Name="Veiled Thug", SpriteId="enemy_veiled_thug", Level=lvl, Inventory=[weapon],
                    Pos=pos, Group=group, Ignores="civilian", **kv)

    def rioter(pos):
        # Townsfolk with cudgels (Mt 1: a riled mob takes about four turns to kill a townsperson); riled by any agitator within 3 cells (Dominion), calm again once he falls.
        return dict(ClassId="fighter", Name="Rioter", SpriteId="neutral_rioter", Level=3, Inventory=["cudgel"],
                    Pos=pos, Faction=NEUTRAL, DominatedBy="agitator")

    def civilian(pos):
        return dict(ClassId="villager", Name="Townsfolk", Tag="civilian", SpriteId="ally_townsfolk", Faction=ALLY,
                    Pos=pos, Ai=STATIONARY)

    spawns = [
        # The north-west alleys.
        agitator("myrmidon", "steel_sword", (6, 6)),
        rioter((5, 7)), rioter((7, 6)), rioter((4, 11)),
        civilian((4, 6)), civilian((2, 11)),
        thug("myrmidon", "iron_sword", (9, 11)),
        # The market.
        agitator("mage", "fire", (23, 8)),
        rioter((22, 8)), rioter((24, 10)), rioter((26, 8)),
        civilian((24, 5)), civilian((28, 10)),
        thug("archer", "iron_bow", (20, 5), Ai=STATIONARY), thug("soldier", "steel_lance", (27, 11)),
        # The Lantern Garden.
        agitator("fighter", "steel_axe", (6, 19)),
        rioter((5, 17)), rioter((7, 20)), rioter((4, 20)),
        civilian((12, 19)), civilian((8, 18)),
        thug("archer", "iron_bow", (10, 22)),
        # The guildhalls.
        agitator("myrmidon", "killing_edge", (26, 19), 10),
        rioter((24, 19)), rioter((27, 19)), rioter((23, 21)),
        civilian((23, 19)), civilian((29, 19)),
        thug("fighter", "iron_axe", (28, 23)), thug("mage", "fire", (30, 14)),
        dict(thug("myrmidon", "steel_sword", (21, 14)), HardOnly=True),
        # Turn 4: more Veiled come over the guild street from both ends.
        thug("myrmidon", "iron_sword", (0, 13), 9, "veiled"), thug("soldier", "steel_lance", (31, 12), 9, "veiled"),
        dict(thug("archer", "steel_bow", (31, 13), 9, "veiled"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch08_start",
                "defeated agitator => scene ch08_agitator",
                "turn 4 enemy => spawn veiled; scene ch08_veiled",
                "visit 4,6 => scene ch08_tenement; give actor elixir",
                "visit 29,7 => scene ch08_cloth_merchant; gold 400"]
    highlights = ["agitator => Veiled agitators wear a purple sigil. Rioters near one fight harder; drop him and they go home.",
                  "4,6 29,7 => Doors you can visit."]
    slots = [(15, 23), (16, 23), (14, 23), (17, 23), (15, 22), (16, 22), (13, 23), (18, 23), (14, 24), (17, 24)]
    map_tres("ch8", "Chapter 8: Riot in the Capital", g.rows(), g.height_rows(), spawns, "boss agitator",
             ["protect civilian 4"], triggers, slots, background="calder_square", highlights=highlights)


# ------------------------------------------------------------------ Chapter 9: Beneath the Crown (29×29)

def reachable(g, start, blocked):
    """Cells someone on foot can reach from start without crossing the blocked symbols."""
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if (0 <= nx < g.w and 0 <= ny < g.h and (nx, ny) not in seen and g.cells[ny][nx] not in blocked
                    and abs(g.heights[ny][nx] - g.heights[y][x]) <= 1):
                seen.add((nx, ny))
                todo.append((nx, ny))
    return seen


def ch9():
    # Solid rock (raised wall) with the undercity carved out of it. Fog of war; lit like night.
    g = Grid(29, 29, "W")
    g.raise_(0, 0, 28, 28, 2)

    def carve(x0, y0, x1, y1, c="#"):
        g.rect(x0, y0, x1, y1, c)
        g.raise_(x0, y0, x1, y1, 0)

    # The sewer outfall where the party comes in (south-west), and the south hall.
    carve(1, 22, 7, 27)
    carve(8, 24, 20, 26)
    # The canal across the middle, walkways on both banks, two bridges and a ford.
    carve(1, 13, 27, 15)
    g.hline(14, 1, 27, "~")
    g.spots("B", [(7, 14), (21, 14)])
    g.put(14, 14, "s")
    # Passages: west (outfall to the north hall), middle (south hall to the canal), east (canal to the archive side).
    carve(4, 7, 5, 21)
    carve(14, 16, 15, 23)
    carve(24, 11, 25, 21)
    # The north hall (it reaches the archive only through the locked door), and the long way round up the east side.
    carve(4, 9, 23, 10)
    carve(26, 3, 27, 12)
    # The Veiled archive (seize the reading desk), its locked shortcut door south into the north hall.
    carve(20, 1, 27, 6)
    carve(23, 7, 23, 8)
    g.put(23, 7, "D")
    # The Veiled barracks off the north hall.
    carve(10, 2, 16, 6)
    carve(13, 7, 13, 8)
    # Locked rooms: the north-west strongroom, the middle vault and the south-east vault.
    carve(1, 1, 6, 5)
    carve(4, 6, 4, 6)
    g.put(4, 6, "D")
    g.spots("C", [(2, 2), (5, 2)])
    carve(17, 17, 19, 20)
    g.put(16, 18, "D")
    g.raise_(16, 18, 16, 18, 0)
    g.put(18, 18, "C")
    carve(22, 23, 27, 27)
    g.put(21, 25, "D")
    g.raise_(21, 25, 21, 25, 0)
    g.spots("C", [(26, 26), (24, 24)])
    # Pip's bolt-hole off the south hall, a turn from the outfall.
    carve(9, 21, 11, 23)
    # Rubble and crates in the halls.
    g.spots("x", [(9, 25), (18, 9), (6, 13), (25, 18)])
    g.spots("k", [(11, 3), (15, 3), (2, 25)])
    assert_connected(g, (3, 24))
    open_route = reachable(g, (3, 24), set("W~kD"))
    assert (24, 2) in open_route, "the archive must be reachable without opening a door"
    assert (10, 21) in open_route, "Pip must be reachable without opening a door"

    # Levels: about the party's (10 by Ch 9 in a campaign, with a few promoted), the archive a step above.
    def thug(cls, weapon, pos, lvl=8, group="", **kv):
        return dict(ClassId=cls, Name="Veiled Thug", SpriteId="enemy_veiled_thug", Level=lvl, Inventory=weapon,
                    Pos=pos, Group=group, **kv)

    def cutpurse(pos, loot, lvl=8, group="", **kv):
        # Undercity thieves: fast, light, and carrying something worth stealing back.
        return dict(ClassId="thief", Name="Cutpurse", SpriteId="enemy_thief", Level=lvl, Inventory=["iron_sword", loot],
                    Pos=pos, Group=group, **kv)

    spawns = [
        dict(UnitId="pip", Faction=ALLY, Level=7, Pos=(10, 21), Ai=STATIONARY),
        # After Pip: a cutpurse coming along the south hall; a thug in the middle passage.
        cutpurse((17, 25), "door_key"), thug("fighter", ["iron_axe"], (14, 19)),
        # The canal.
        thug("archer", ["iron_bow"], (10, 13), Ai=STATIONARY), thug("fighter", ["steel_axe"], (18, 13)),
        thug("myrmidon", ["iron_sword"], (3, 15)),
        # The south hall.
        cutpurse((18, 25), "vulnerary"), thug("soldier", ["steel_lance"], (20, 25), Ai=GUARD),
        # The north hall and barracks.
        thug("soldier", ["steel_lance"], (8, 9)), thug("mage", ["fire"], (17, 10)),
        thug("myrmidon", ["steel_sword"], (12, 4), Ai=GUARD), thug("fighter", ["iron_axe", "chest_key"], (14, 5), Ai=GUARD),
        thug("archer", ["steel_bow"], (16, 2), 9, Ai=GUARD),
        # The east passage.
        thug("myrmidon", ["killing_edge"], (24, 17), 9), thug("soldier", ["steel_lance", "elixir"], (25, 11), 9),
        # The archive: the Archivist (a Binder; his Dominion holds the guards) and his guards.
        dict(ClassId="binder", Name="Archivist Corvin", Tag="archivist", SpriteId="enemy_binder", Level=10,
             Inventory=["shadow"], Pos=(24, 3), Ai=BOSS, IsBoss=True, CrestBearer=True, Leash=2),
        thug("knight", ["steel_lance"], (22, 4), 9, Ai=GUARD, DominatedBy="archivist", Armor="iron_plate"),
        thug("mage", ["thunder"], (26, 4), 9, Ai=GUARD, DominatedBy="archivist"),
        thug("archer", ["steel_bow"], (27, 7), 9, Ai=STATIONARY, DominatedBy="archivist"),
        dict(thug("myrmidon", ["steel_sword"], (21, 2), 10, Ai=GUARD, DominatedBy="archivist"), HardOnly=True),
        # Turn 4: cutpurses up through the outfall grates. Turn 6: the night watch of the Veiled from the east.
        cutpurse((1, 27), "chest_key", 8, "grate"), cutpurse((7, 27), "vulnerary", 8, "grate"),
        thug("myrmidon", ["steel_sword"], (27, 10), 9, "watch"), thug("mage", ["fire"], (26, 9), 9, "watch"),
        dict(thug("fighter", ["steel_axe"], (26, 11), 9, "watch"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch09_start",
                "talk any pip => scene ch09_pip; join pip",
                "enter player 20,1-27,6 => scene ch09_archive",
                "combat archivist => scene ch09_archivist",
                "turn 4 enemy => spawn grate; scene ch09_grate",
                "turn 6 enemy => spawn watch",
                "chest 2,2 => give actor speedwing",
                "chest 5,2 => gold 1500",
                "chest 18,18 => give actor silver_sword",
                "chest 24,24 => give actor elixir",
                "chest 26,26 => give actor secret_book"]
    highlights = ["pip => Someone small is hiding from the Veiled. Talk to him.",
                  "4,6 16,18 21,25 23,7 => Locked doors. A thief or a Door Key opens them.",
                  "2,2 5,2 18,18 24,24 26,26 => Chests. Stand on one with a thief or a Chest Key to open it."]
    slots = [(3, 24), (4, 24), (5, 24), (3, 25), (4, 25), (5, 25), (2, 23), (6, 23), (3, 26), (5, 26)]
    map_tres("ch9", "Chapter 9: Beneath the Crown", g.rows(), g.height_rows(), spawns, "seize 24,2", [],
             triggers, slots, background="calder_undercity", fog=True, highlights=highlights)
    finish("ch9", {"Lighting": "night"})


# ------------------------------------------------------------------ Chapter 10: The Border of Ash (35×22)

def ch10():
    g = Grid(35, 22)
    # Ridges along the north and south edges (mountain and pine), the pass between them.
    g.rect(0, 0, 34, 1, "M")
    g.rect(0, 20, 34, 21, "M")
    for x0, x1 in [(3, 7), (24, 27)]:
        g.rect(x0, 2, x1, 2, "F")
    for x0, x1 in [(8, 12), (27, 31)]:
        g.rect(x0, 19, x1, 19, "F")
    g.spots("M", [(13, 2), (14, 2), (20, 19), (21, 19)])
    # The border road from Calder (west) to the Dravenhold camp (east).
    g.rect(0, 10, 34, 11, "=")
    # The burning village in the middle: cottages (some ablaze), its fields.
    for x, y in [(14, 6), (18, 6), (14, 13), (19, 13)]:
        g.house(x, y)
    g.spots("^", [(18, 6), (19, 6), (20, 6), (19, 13), (20, 13), (21, 13)])
    g.rect(14, 3, 21, 4, ":")
    g.rect(15, 16, 21, 17, ":")
    g.spots("F", [(11, 5), (12, 15), (23, 4), (23, 16)])
    # The Dravenhold camp: a palisade with a gate on the road, tents (forts) inside.
    g.vline(28, 3, 18, "W")
    g.raise_(28, 3, 28, 18, 1)
    g.spots("=", [(28, 10), (28, 11)])
    g.raise_(28, 10, 28, 11, 0)
    g.spots("T", [(30, 6), (32, 8), (30, 14), (32, 13)])
    assert_connected(g, (2, 10))

    # Levels: a step under the party (11 by Ch 10), the commander above it.
    def imperial(cls, weapon, pos, lvl=9, group="", name=None, **kv):
        names = {"soldier": "Imperial Soldier", "knight": "Imperial Knight", "archer": "Imperial Archer",
                 "cavalier": "Imperial Cavalier", "wyvern_rider": "Wyvern Rider", "mage": "Imperial Mage"}
        armor = {"knight": "iron_plate", "soldier": "chainmail", "cavalier": "chainmail"}.get(cls)
        spawn = dict(ClassId=cls, Name=name or names[cls], SpriteId="enemy_imperial", Level=lvl, Inventory=weapon,
                     Pos=pos, Group=group, **kv)
        if armor:
            spawn["Armor"] = armor
        return spawn

    def escort(pos):
        return dict(ClassId="knight", Name="Calder Escort", SpriteId="ally_calder_knight", Level=10, Faction=ALLY,
                    Inventory=["steel_lance"], Pos=pos, Armor="iron_plate", Ai=AGGRESSIVE)

    spawns = [
        # Seraphine and what's left of her escort, ambushed at the village's west edge.
        dict(UnitId="seraphine", Faction=PLAYER, Level=9, Pos=(12, 10)),
        escort((13, 9)), escort((13, 12)),
        # The ambush: they reach the escort on turn 1; the party is a turn away.
        imperial("soldier", ["iron_lance"], (17, 9)), imperial("soldier", ["iron_lance"], (17, 12), 8),
        imperial("archer", ["iron_bow"], (18, 8), 8), imperial("cavalier", ["iron_sword"], (18, 15), 8),
        imperial("soldier", ["steel_lance", "vulnerary"], (22, 10), name="Imperial Herald", Tag="herald"),
        # The camp.
        imperial("knight", ["silver_lance", "javelin"], (31, 10), 13, name="Commander Hesk", Tag="commander",
                 Ai=BOSS, IsBoss=True, Leash=3),
        imperial("knight", ["steel_lance"], (30, 9), 10, Ai=GUARD), imperial("knight", ["steel_lance"], (30, 12), 10, Ai=GUARD),
        imperial("archer", ["steel_bow"], (32, 8), Ai=STATIONARY), imperial("mage", ["thunder"], (32, 13), Ai=GUARD),
        dict(imperial("cavalier", ["steel_lance"], (29, 6), 10), HardOnly=True),
        # Waves: T2 riders down the road, T4 both flanks, T6 wyverns over the north ridge, T8 the camp empties.
        imperial("cavalier", ["iron_lance"], (34, 10), 8, "wave2"), imperial("cavalier", ["iron_sword"], (34, 11), 8, "wave2"),
        imperial("soldier", ["iron_lance"], (25, 3), group="wave4"), imperial("soldier", ["steel_lance"], (25, 18), group="wave4"),
        imperial("archer", ["iron_bow"], (26, 3), group="wave4"),
        # The wyverns come in over the ridge's east end: a turn's warning before they can reach Seraphine (session 12
        # soak: from right above the village they killed her the turn they arrived, 6 tries in a row).
        imperial("wyvern_rider", ["iron_lance"], (26, 2), group="wave6"), imperial("wyvern_rider", ["iron_lance"], (31, 3), group="wave6"),
        dict(imperial("wyvern_rider", ["steel_lance"], (29, 2), group="wave6"), HardOnly=True),
        imperial("knight", ["steel_lance"], (33, 6), 10, "wave8"), imperial("cavalier", ["steel_lance"], (34, 15), group="wave8"),
        imperial("soldier", ["steel_lance"], (33, 10), group="wave8"),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch10_start; scene ch10_seraphine",
                "defeated herald => scene ch10_insult",
                "turn 2 enemy => spawn wave2",
                "turn 4 enemy => spawn wave4",
                "turn 5 => terrain 19,6-20,6 20,13-21,13 x; scene ch10_collapse",
                "turn 6 enemy => spawn wave6; scene ch10_wyverns",
                "turn 8 enemy => spawn wave8",
                "combat commander => scene ch10_commander"]
    highlights = ["seraphine => Princess Seraphine. Her escort is falling; if she falls, so does the kingdom's hope.",
                  "commander => Commander Hesk leads the raid from the camp. Beat him and his soldiers stand down."]
    slots = [(7, 10), (7, 11), (6, 10), (6, 11), (6, 9), (6, 12), (5, 10), (5, 11), (5, 9), (5, 12)]
    map_tres("ch10", "Chapter 10: The Border of Ash", g.rows(), g.height_rows(), spawns, "survive 10 or boss commander",
             ["die seraphine"], triggers, slots, background="border_pass", highlights=highlights)


# ------------------------------------------------------------------ Chapter 11: Wings of Dravenhold (32×32)

def ch11():
    # Heights make the pass: walkers step at most one level, fliers ignore heights (Pathfinder), so every drop of two
    # or more is a cliff only Wren, Ysra and the wyverns can cross.
    g = Grid(32, 32)
    # The south valley (height 0), where the party comes up from Calder.
    g.spots("F", [(2, 27), (3, 28), (6, 30), (24, 29), (27, 27), (29, 30), (11, 26), (21, 26)])
    # The gorge (height 0) down the east side, its river, joining the valley.
    g.vline(27, 3, 24, "~")
    g.spots("s", [(27, 10), (27, 18)])
    g.spots("F", [(24, 6), (30, 12), (25, 20), (30, 22)])
    # The middle shelf (height 2): cliffs to the valley and the gorge, two ramps up from the valley.
    g.raise_(0, 13, 22, 24, 2)
    for x in (8, 9, 18, 19):
        g.heights[24][x] = 1
        g.heights[23][x] = 1
    # The upper shelf (height 4), a wall of rock below it but for one ramp (height 3) on the road.
    g.raise_(0, 2, 22, 10, 4)
    g.rect(0, 11, 22, 12, "W")
    g.raise_(0, 11, 22, 12, 5)
    g.rect(14, 11, 16, 12, "=")
    g.raise_(14, 11, 16, 12, 3)
    # The roost: a plateau (height 6) on the upper shelf's west end, one stair up (height 5).
    g.raise_(1, 3, 5, 8, 6)
    g.heights[6][6] = 5
    # The ridge along the north edge.
    g.rect(0, 0, 31, 1, "M")
    g.raise_(0, 0, 22, 1, 4)
    # The road: up the east ramp, across the middle shelf, up the rock ramp, over the upper shelf.
    g.rect(18, 22, 19, 24, "=")
    g.rect(16, 18, 19, 19, "=")
    g.rect(15, 13, 16, 19, "=")
    g.rect(15, 3, 16, 10, "=")
    g.spots("F", [(3, 15), (4, 19), (11, 16), (12, 21), (6, 22), (20, 14), (9, 4), (12, 7), (19, 5), (2, 10)])
    g.spots("M", [(7, 17), (8, 17), (21, 9), (11, 3)])
    assert_connected(g, (15, 29))

    def imperial(cls, weapon, pos, lvl=10, group="", name=None, **kv):
        names = {"soldier": "Imperial Soldier", "knight": "Imperial Knight", "archer": "Imperial Archer",
                 "cavalier": "Imperial Cavalier", "wyvern_rider": "Wyvern Rider", "wyvern_lord": "Wyvern Lord",
                 "mage": "Imperial Mage"}
        armor = {"knight": "iron_plate", "soldier": "chainmail", "cavalier": "chainmail"}.get(cls)
        spawn = dict(ClassId=cls, Name=name or names[cls], SpriteId="enemy_imperial", Level=lvl, Inventory=weapon,
                     Pos=pos, Group=group, **kv)
        if armor:
            spawn["Armor"] = armor
        return spawn

    spawns = [
        # Ysra, hanging back on the shelf's edge over the gorge.
        dict(UnitId="ysra", Faction=ENEMY, Level=11, Pos=(21, 17), Ai=STATIONARY),
        # The middle shelf.
        imperial("soldier", ["steel_lance"], (10, 19)), imperial("soldier", ["iron_lance"], (14, 21)),
        imperial("cavalier", ["iron_sword"], (5, 16)), imperial("cavalier", ["steel_lance"], (12, 14)),
        imperial("mage", ["thunder"], (18, 15), Ai=GUARD), imperial("archer", ["steel_bow"], (20, 20), Ai=GUARD),
        # The rock ramp and the upper shelf.
        imperial("knight", ["steel_lance"], (14, 12), 10, Ai=GUARD), imperial("soldier", ["steel_lance"], (16, 12), 10, Ai=GUARD),
        imperial("archer", ["iron_bow"], (15, 9), Ai=STATIONARY), imperial("soldier", ["steel_lance"], (19, 7)),
        imperial("wyvern_lord", ["silver_lance", "steel_axe"], (15, 4), 12, name="Wing-Captain Orvald",
                 Tag="orvald", Ai=BOSS, IsBoss=True, Leash=4),
        # The roost.
        imperial("archer", ["steel_bow"], (3, 5), Ai=STATIONARY), imperial("mage", ["fire"], (2, 7), Ai=STATIONARY),
        dict(imperial("archer", ["iron_bow"], (5, 4), Ai=STATIONARY), HardOnly=True),
        # Wyverns in the gorge, and more: T3 up the gorge, T5 over the north ridge.
        # (Session 12 autoplay: six wyverns plus the captain cost 5-7 units a run; now two, then one a wave.)
        imperial("wyvern_rider", ["iron_lance"], (25, 14), 9), imperial("wyvern_rider", ["iron_lance"], (29, 8), 9),
        imperial("wyvern_rider", ["javelin", "iron_lance"], (30, 3), 9, "gorge"),
        dict(imperial("wyvern_rider", ["iron_lance"], (24, 3), 10, "gorge"), HardOnly=True),
        imperial("wyvern_rider", ["iron_lance"], (20, 2), 10, "ridge"),
        dict(imperial("wyvern_lord", ["steel_lance"], (13, 2), 12, "ridge"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch11_start",
                "talk rowan|seraphine ysra => scene ch11_ysra; join ysra",
                "turn 3 enemy => spawn gorge; scene ch11_gorge",
                "turn 5 enemy => spawn ridge",
                "combat orvald => scene ch11_orvald"]
    highlights = ["ysra => A wyvern rider hanging back from the fight. A crest-bearer might talk her round: Rowan or Seraphine.",
                  "14,11-16,12 => The only way up for anyone on foot. Fliers can go over the cliffs anywhere."]
    slots = [(15, 29), (16, 29), (14, 29), (17, 29), (15, 30), (16, 30), (13, 29), (18, 29), (14, 30), (17, 30)]
    map_tres("ch11", "Chapter 11: Wings of Dravenhold", g.rows(), g.height_rows(), spawns, "rout", [],
             triggers, slots, background="dravenhold_pass", highlights=highlights)


# ------------------------------------------------------------------ Chapter 12: The Gilded Harbor (35×22)

def ch12():
    # Solmere's harbour: the stone quays west and south, wooden piers into the water, three ships moored at the piers
    # and to each other by gangplanks. Halde's ship, the Gull, is furthest out: everyone must get aboard.
    g = Grid(35, 22, "~")

    def land(x0, y0, x1, y1, c="#", h=0):
        g.rect(x0, y0, x1, y1, c)
        g.raise_(x0, y0, x1, y1, h)

    land(0, 0, 9, 21)           # the west quay
    land(0, 18, 20, 21)         # the south quay
    # Warehouses and the auction hall on the quays; crates and carts between.
    for x0, y0, x1, y1 in [(1, 1, 4, 4), (6, 1, 8, 3), (1, 7, 3, 10), (1, 13, 3, 15), (12, 20, 16, 21)]:
        g.rect(x0, y0, x1, y1, "W")
        g.raise_(x0, y0, x1, y1, 2)
    g.spots("T", [(5, 4), (4, 11)])
    g.spots("k", [(7, 6), (8, 9), (6, 15), (9, 19), (17, 19)])
    # Piers (bridge planks over the water).
    g.rect(10, 5, 18, 6, "B")
    g.rect(10, 12, 22, 13, "B")
    g.rect(12, 14, 13, 17, "B")
    # The ships: decks raised a step, gangplanks between them.
    for x0, y0, x1, y1 in [(19, 3, 24, 8), (23, 14, 28, 18), (29, 6, 33, 12)]:
        g.rect(x0, y0, x1, y1, "B")
        g.raise_(x0, y0, x1, y1, 1)
    g.rect(25, 7, 28, 7, "B")      # merchant hulk -> the Gull
    g.rect(23, 12, 24, 13, "B")    # pier -> galley
    g.raise_(23, 12, 24, 13, 1)
    g.rect(29, 13, 30, 14, "B")    # galley -> the Gull
    g.raise_(29, 13, 30, 14, 1)
    g.spots("k", [(21, 4), (25, 16), (31, 7)])
    assert_connected(g, (3, 18))

    # Levels: two under the party (13 by Ch 12), elites one under. They leave Halde and her crew alone (Ignores) until
    # she joins (session 12 autoplay: the ships' guards killed the Gull's crew on T2 and Halde on T3).
    def gilded(cls, weapon, pos, lvl=11, group="", name=None, **kv):
        names = {"soldier": "Gilded Guard", "knight": "Gilded Knight", "archer": "Gilded Archer", "mage": "Gilded Mage",
                 "myrmidon": "Harbour Sellsword", "fighter": "Harbour Sellsword", "thief": "Dockside Cutpurse"}
        armor = {"knight": "iron_plate", "soldier": "chainmail"}.get(cls)
        spawn = dict(ClassId=cls, Name=name or names[cls], SpriteId="enemy_gilded" if cls in ("soldier", "knight", "archer", "mage")
                     else f"enemy_{cls}", Level=lvl, Inventory=weapon, Pos=pos, Group=group, Ignores="gull|halde", **kv)
        if armor:
            spawn["Armor"] = armor
        return spawn

    def deckhand(pos, weapon):
        return dict(ClassId="fighter", Name="Gull Deckhand", Tag="gull", SpriteId="ally_sailor", Level=11, Faction=ALLY,
                    Inventory=[weapon], Pos=pos, Ai=GUARD)

    spawns = [
        # Halde and her crew on the Gull.
        dict(UnitId="halde", Faction=ALLY, Level=12, Pos=(32, 9), Ai=STATIONARY),
        deckhand((30, 8), "steel_axe"), deckhand((30, 11), "hand_axe"),
        # The quays.
        gilded("soldier", ["steel_lance"], (7, 12)), gilded("myrmidon", ["steel_sword"], (6, 7)),
        gilded("archer", ["steel_bow"], (5, 2), Ai=STATIONARY), gilded("fighter", ["steel_axe"], (9, 16)),
        gilded("thief", ["iron_sword", "chest_key"], (4, 5)),
        # The piers and the merchant hulk.
        gilded("knight", ["steel_lance"], (14, 6), 12, Ai=GUARD), gilded("knight", ["steel_lance"], (16, 13), 12, Ai=GUARD),
        gilded("mage", ["fire"], (21, 6), Ai=GUARD), gilded("archer", ["steel_bow"], (23, 4), Ai=STATIONARY),
        gilded("myrmidon", ["killing_edge"], (19, 13)),
        # Castellan's galley.
        gilded("soldier", ["steel_lance"], (24, 15)), gilded("mage", ["thunder"], (27, 17), Ai=STATIONARY),
        gilded("knight", ["silver_lance"], (26, 14), 13, name="Galley Captain Dresh", Tag="dresh", Ai=GUARD),
        dict(gilded("archer", ["steel_bow"], (28, 16), Ai=STATIONARY), HardOnly=True),
        # Reinforcements: T3 from the warehouses, T5 boarding boats at the piers' ends, T7 more from the auction.
        gilded("soldier", ["steel_lance"], (5, 0), group="warehouse"), gilded("myrmidon", ["steel_sword"], (0, 5), group="warehouse"),
        # Boarders tie up at the far ends of the piers, by the ships, not in the party's path.
        gilded("fighter", ["steel_axe"], (18, 6), group="boats"), gilded("myrmidon", ["iron_sword"], (21, 13), group="boats"),
        gilded("soldier", ["steel_lance"], (0, 17), group="auction"), gilded("knight", ["steel_lance"], (2, 20), group="auction"),
        dict(gilded("mage", ["elfire"], (0, 19), 11, "auction"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch12_start",
                "turn 2 => scene ch12_auction",
                "talk any halde => scene ch12_halde; join halde",
                "turn 3 enemy => spawn warehouse",
                "turn 5 enemy => spawn boats; scene ch12_boats",
                "turn 7 enemy => spawn auction"]
    highlights = ["halde => Captain Halde Marr of the Gull. Talk to her: she'll take you on, crew and all.",
                  "31,8-32,10 => The Gull's deck. Everyone must get aboard; Rowan goes last."]
    slots = [(3, 18), (4, 18), (5, 18), (3, 19), (4, 19), (5, 19), (6, 18), (6, 19), (2, 17), (2, 19)]
    map_tres("ch12", "Chapter 12: The Gilded Harbor", g.rows(), g.height_rows(), spawns, "escape 31,8-32,10 all", [],
             triggers, slots, background="solmere_harbor", highlights=highlights)


# ------------------------------------------------------------------ Chapter 13: The Merchant Prince (32×26)

def ch13():
    # Castellan's palazzo above the sea: the shore and sea gate (south), terraced gardens, the palazzo (north) with the
    # great hall, a locked vault (west wing) and a balcony (east wing, a step up over the gardens).
    g = Grid(32, 26, ",")
    g.hline(25, 0, 31, "~")
    g.rect(0, 23, 31, 24, ":")       # the strand: open sand below the walls (field rules)
    for x in range(0, 32):
        g.put(x, 23, "=") if x in (15, 16) else None
    # The garden wall along y 21, the sea gate in it.
    g.hline(21, 0, 31, "W")
    g.raise_(0, 21, 31, 21, 2)
    g.spots("=", [(15, 21), (16, 21)])
    g.raise_(15, 21, 16, 21, 0)
    # The gardens: paths, a fountain, clipped trees, flower beds.
    g.rect(15, 12, 16, 22, "=")
    g.rect(4, 16, 27, 16, "=")
    g.rect(14, 15, 17, 17, "#")
    g.rect(15, 16, 16, 16, "~")
    g.spots("F", [(5, 13), (9, 19), (12, 13), (20, 19), (24, 13), (27, 19), (3, 19), (28, 13)])
    g.spots("x", [(7, 17), (23, 15)])
    # The palazzo: walls (height 3) and halls inside (height 1).
    g.rect(0, 0, 31, 11, "W")
    g.raise_(0, 0, 31, 11, 3)

    def hall(x0, y0, x1, y1, c="#", h=1):
        g.rect(x0, y0, x1, y1, c)
        g.raise_(x0, y0, x1, y1, h)

    hall(10, 1, 21, 9)               # the great hall
    hall(15, 10, 16, 11)             # its doors onto the garden steps
    g.put(15, 2, "Y")                # Castellan's seat
    g.put(16, 2, "Y")
    for x in (12, 19):
        for y in (3, 6):
            g.put(x, y, "W")
            g.raise_(x, y, x, y, 3)
    hall(2, 6, 8, 9)                 # the west wing...
    hall(9, 5, 9, 6)                 # ...open to the great hall
    hall(2, 10, 3, 11)               # ...and down to the gardens
    hall(3, 2, 6, 4)                 # the vault, walled in, behind a locked door
    g.put(5, 5, "D")
    g.raise_(5, 5, 5, 5, 1)
    g.spots("C", [(3, 2), (5, 2), (6, 4)])
    hall(23, 2, 29, 9, "#", 2)       # the east wing: the balcony, a step above the hall
    hall(22, 5, 22, 6, "#", 2)
    hall(28, 10, 29, 11, "#", 1)     # the balcony stair down to the gardens
    g.spots("T", [(4, 8), (26, 3)])
    assert_connected(g, (15, 23))

    def gilded(cls, weapon, pos, lvl=12, group="", name=None, **kv):
        names = {"soldier": "Gilded Guard", "knight": "Gilded Knight", "archer": "Gilded Archer", "mage": "Gilded Mage",
                 "general": "Gilded Captain", "myrmidon": "Palazzo Sellsword", "fighter": "Palazzo Sellsword",
                 "thief": "Vault Thief", "cavalier": "Gilded Rider", "sniper": "Gilded Marksman"}
        armor = {"knight": "iron_plate", "general": "silver_plate", "soldier": "chainmail", "cavalier": "chainmail"}.get(cls)
        spawn = dict(ClassId=cls, Name=name or names[cls], SpriteId="enemy_gilded" if cls not in ("myrmidon", "fighter", "thief")
                     else f"enemy_{cls}", Level=lvl, Inventory=weapon, Pos=pos, Group=group, DominatedBy="castellan", **kv)
        if armor:
            spawn["Armor"] = armor
        return spawn

    spawns = [
        # Prince Castellan: three shrouded crests, Dominion to 4 cells over the whole house guard.
        dict(ClassId="general", Name="Prince Castellan", Tag="castellan", SpriteId="boss_castellan", Level=15,
             Inventory=["silver_lance", "javelin", "elixir"], Pos=(15, 2), Armor="silver_plate", Ai=BOSS, IsBoss=True,
             CrestBearer=True, DominionRadius=4, Leash=2),
        gilded("knight", ["steel_lance"], (14, 4), 13, Ai=GUARD), gilded("knight", ["steel_lance"], (17, 4), 13, Ai=GUARD),
        gilded("mage", ["fire"], (13, 7), Ai=GUARD), gilded("mage", ["thunder"], (18, 7), Ai=GUARD),
        # The gardens.
        gilded("soldier", ["steel_lance"], (10, 16)), gilded("soldier", ["steel_lance"], (21, 16)),
        gilded("cavalier", ["steel_sword"], (6, 14)), gilded("cavalier", ["steel_lance"], (25, 18)),
        gilded("myrmidon", ["killing_edge"], (15, 13)), gilded("fighter", ["steel_axe"], (20, 13)),
        # The balcony: archers and a mage over the gardens.
        gilded("archer", ["steel_bow"], (24, 9), Ai=STATIONARY), gilded("archer", ["silver_bow"], (27, 9), 13, Ai=STATIONARY),
        gilded("mage", ["fire"], (26, 6), Ai=GUARD),
        # The west wing and its vault.
        gilded("thief", ["steel_sword", "chest_key"], (4, 7)), gilded("knight", ["steel_lance"], (7, 9), Ai=GUARD),
        dict(gilded("general", ["silver_lance"], (8, 7), 13, Ai=GUARD), HardOnly=True),
        # Reinforcements: T3 riders round the east of the gardens, T6 the west wing's guard comes out.
        gilded("cavalier", ["steel_lance"], (31, 17), group="east"), gilded("cavalier", ["steel_sword"], (31, 14), group="east"),
        gilded("soldier", ["steel_lance"], (2, 10), 11, "west"), gilded("myrmidon", ["steel_sword"], (3, 10), 11, "west"),
        dict(gilded("mage", ["elfire"], (2, 11), 12, "west"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch13_start",
                "enter player 10,1-21,9 => scene ch13_hall",
                "combat castellan => scene ch13_castellan",
                "defeated castellan => scene ch13_castellan_falls",
                "turn 3 enemy => spawn east",
                "turn 6 enemy => spawn west",
                "chest 3,2 => give actor silver_sword",
                "chest 5,2 => gold 3000",
                "chest 6,4 => give actor seraph_robe"]
    highlights = ["castellan => Prince Castellan. Three crests: his Dominion reaches four cells and holds his whole house guard.",
                  "5,5 => The vault. A thief or a Door Key opens it.",
                  "23,2-29,9 => The balcony. Archers up there cover the whole east garden."]
    slots = [(15, 23), (16, 23), (14, 23), (17, 23), (15, 24), (16, 24), (13, 23), (18, 23), (14, 24), (17, 24)]
    map_tres("ch13", "Chapter 13: The Merchant Prince", g.rows(), g.height_rows(), spawns, "boss castellan", [],
             triggers, slots, background="solmere_palazzo", highlights=highlights)


# ------------------------------------------------------------------ Chapter 14: The Empress Unbound (32×29)

def ch14():
    # The imperial citadel in snow: the approach and outer gate (south), the courtyard with frozen ponds, the inner keep
    # (north) with Vossk on the throne. The Empress fights in the courtyard under his Dominion; she can't be killed.
    g = Grid(32, 29)
    g.spots("F", [(2, 24), (5, 27), (9, 25), (22, 26), (26, 24), (29, 27), (1, 18), (30, 18), (4, 13), (27, 13),
                  (11, 19), (20, 19), (8, 13), (23, 13)])
    g.rect(15, 23, 16, 28, "=")
    # The outer wall and gate.
    g.hline(22, 0, 31, "W")
    g.raise_(0, 22, 31, 22, 2)
    g.spots("=", [(15, 22), (16, 22)])
    g.raise_(15, 22, 16, 22, 0)
    # The courtyard: the road to the keep, frozen ponds, carts left in the snow.
    g.rect(15, 12, 16, 21, "=")
    g.rect(5, 15, 7, 17, "~")
    g.rect(24, 15, 26, 17, "~")
    g.spots("k", [(11, 14), (20, 16), (9, 20), (22, 20)])
    g.spots("x", [(13, 18), (18, 13)])
    # The inner keep: walls (height 3), the throne hall (height 1), doors onto the courtyard steps.
    g.rect(0, 0, 31, 11, "W")
    g.raise_(0, 0, 31, 11, 3)
    g.rect(8, 2, 23, 10, "#")
    g.raise_(8, 2, 23, 10, 1)
    g.rect(15, 11, 16, 11, "=")
    g.raise_(15, 11, 16, 11, 1)
    g.spots("Y", [(15, 3), (16, 3)])
    for x in (11, 20):
        for y in (4, 7):
            g.put(x, y, "W")
            g.raise_(x, y, x, y, 3)
    # Side doors: the guard's stairs from the wall walks down into the courtyard (reinforcements come this way).
    for x in (3, 28):
        g.rect(x, 8, x, 11, "=")
        g.raise_(x, 8, x, 8, 3)
        g.raise_(x, 9, x, 9, 2)
        g.raise_(x, 10, x, 10, 1)
        g.raise_(x, 11, x, 11, 0)
    assert_connected(g, (15, 26))

    # Levels: a few under the party (15 by Ch 14). The Veiled casters are Mages, not Sages: a promoted caster's tome
    # gains a point of might per three of its levels plus ten (session 12: L14 Veiled Sages killed 11 units in 4 runs).
    def imperial(cls, weapon, pos, lvl=13, group="", name=None, **kv):
        names = {"soldier": "Imperial Guard", "knight": "Imperial Knight", "general": "Imperial General",
                 "archer": "Imperial Archer", "sniper": "Imperial Marksman", "wyvern_rider": "Wyvern Rider",
                 "wyvern_lord": "Wyvern Lord", "mage": "Veiled Mage", "cavalier": "Imperial Cavalier"}
        armor = {"knight": "iron_plate", "general": "silver_plate", "soldier": "chainmail", "cavalier": "chainmail"}.get(cls)
        spawn = dict(ClassId=cls, Name=name or names[cls], SpriteId="enemy_veiled" if cls == "mage" else "enemy_imperial",
                     Level=lvl, Inventory=weapon, Pos=pos, Group=group, DominatedBy="vossk", **kv)
        if armor:
            spawn["Armor"] = armor
        return spawn

    spawns = [
        # Vossk, the Veiled Hand, on the Empress's throne; his Dominion holds her and her guard.
        dict(ClassId="binder", Name="Vossk, the Veiled Hand", Tag="vossk", SpriteId="boss_vossk", Level=17,
             Inventory=["shadow", "elixir"], Pos=(15, 3), Armor="sage_vestments", Ai=BOSS, IsBoss=True, CrestBearer=True,
             DominionRadius=4, Leash=2),
        # The Empress: she fights, but she must not die (NonLethal: beaten, she is subdued).
        dict(UnitId="ysolde", Level=15, Pos=(16, 14), Ai=GUARD, DominatedBy="vossk", NonLethal=True),
        # The throne hall.
        imperial("general", ["silver_lance"], (14, 5), 14, Ai=GUARD), imperial("general", ["steel_axe"], (17, 5), 14, Ai=GUARD),
        imperial("mage", ["fire"], (13, 8), 12, Ai=GUARD), imperial("mage", ["thunder"], (18, 8), 12, Ai=GUARD),
        # The courtyard.
        imperial("knight", ["steel_lance"], (12, 16)), imperial("knight", ["steel_lance"], (19, 16)),
        imperial("soldier", ["steel_lance"], (9, 18)), imperial("soldier", ["silver_lance"], (22, 18), 13),
        imperial("cavalier", ["steel_sword"], (4, 19)), imperial("cavalier", ["steel_lance"], (27, 19)),
        imperial("archer", ["steel_bow"], (10, 12), 13, Ai=STATIONARY), imperial("archer", ["steel_bow"], (21, 12), 13, Ai=STATIONARY),
        imperial("wyvern_rider", ["steel_lance"], (6, 13)),
        dict(imperial("wyvern_lord", ["silver_lance"], (25, 13), 13), HardOnly=True),
        # Reinforcements: T3 wyverns over the outer wall, T5 the guard down the side stairs, T7 more of both.
        imperial("wyvern_rider", ["iron_lance"], (2, 23), group="walls"), imperial("wyvern_rider", ["javelin", "iron_lance"], (29, 23), group="walls"),
        imperial("soldier", ["steel_lance"], (3, 11), group="stairs"), imperial("knight", ["steel_lance"], (28, 11), group="stairs"),
        imperial("mage", ["fire"], (3, 10), 11, "late"),
        dict(imperial("wyvern_rider", ["steel_lance"], (16, 21), group="late"), HardOnly=True),
        dict(imperial("general", ["steel_lance"], (28, 10), 13, "late"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch14_start",
                "combat ysolde => scene ch14_empress",
                "talk ysra|seraphine ysolde => scene ch14_ysolde_talk",
                "subdued ysolde => scene ch14_ysolde_subdued",
                "enter player 8,2-23,10 => scene ch14_hall",
                "combat vossk => scene ch14_vossk",
                "defeated vossk => scene ch14_ysolde_wakes; faction ysolde ally",
                "turn 3 enemy => spawn walls",
                "turn 5 enemy => spawn stairs",
                "turn 7 enemy => spawn late"]
    highlights = ["ysolde => Empress Ysolde, held by Vossk's Dominion. She can't be killed: beaten, she is only subdued.",
                  "vossk => Vossk, the Veiled Hand. His Dominion reaches four cells. Fell him and the Empress is free."]
    slots = [(15, 26), (16, 26), (14, 26), (17, 26), (15, 27), (16, 27), (14, 27), (17, 27), (13, 26), (18, 26)]
    map_tres("ch14", "Chapter 14: The Empress Unbound", g.rows(), g.height_rows(), spawns, "boss vossk", [],
             triggers, slots, background="dravenhold_citadel", highlights=highlights)
    finish("ch14", {"Lighting": "snow"})


# ------------------------------------------------------------------ Chapter 15: The Grey Chancellor (26×22)

def ch15():
    # Calder's great hall, at the peace summit: pillars, Morvane on the dais (north), the main doors (south) and the
    # west door, barred until Aldric stays behind (T5). Hold for five turns, then everyone out by turn 10.
    g = Grid(26, 22, "W")
    g.raise_(0, 0, 25, 21, 3)
    g.rect(1, 1, 24, 20, "#")
    g.raise_(1, 1, 24, 20, 0)
    g.rect(9, 1, 16, 3, "#")
    g.raise_(9, 1, 16, 3, 1)           # the dais
    g.spots("Y", [(12, 1), (13, 1)])
    for x, y in [(5, 5), (5, 10), (5, 15), (20, 5), (20, 10), (20, 15), (9, 8), (16, 8), (9, 13), (16, 13)]:
        g.put(x, y, "W")
        g.raise_(x, y, x, y, 3)
    g.spots("k", [(10, 10), (11, 10), (14, 10), (15, 10)])   # the summit table, overturned
    g.spots("G", [(12, 21), (13, 21), (0, 9), (0, 10), (0, 11)])  # the main doors, and the wide west door (barred)
    g.raise_(12, 21, 13, 21, 0)
    g.raise_(0, 9, 0, 11, 0)
    assert_connected(g, (12, 15))

    def hollowed(weapon, pos, lvl=14, group="", **kv):
        return dict(ClassId="hollowed", Name="Hollowed", SpriteId="enemy_hollowed", Level=lvl, Inventory=[weapon],
                    Pos=pos, Group=group, **kv)

    def guard(name, cls, weapon, pos, lvl=14, **kv):
        return dict(ClassId=cls, Name=name, SpriteId="ally_calder_knight" if cls == "knight" else "enemy_imperial",
                    Level=lvl, Inventory=[weapon], Pos=pos, Faction=ALLY, Ai=AGGRESSIVE, **kv)

    spawns = [
        # Morvane: unbeatable today. He takes the King's crest, and the hall fills with the Hollowed.
        dict(ClassId="binder", Name="Morvane, the Grey Chancellor", Tag="morvane", SpriteId="boss_morvane", Level=22,
             Inventory=["shadow"], Pos=(12, 2), Ai=BOSS, IsBoss=True, Invulnerable=True, Leash=1),
        # Aldric is deployed (forced); the Empress and the summit's guards fight beside the party.
        dict(UnitId="ysolde", Faction=ALLY, Level=15, Pos=(17, 12), Ai=AGGRESSIVE),
        guard("Calder Guard", "knight", "steel_lance", (8, 12)), guard("Calder Guard", "knight", "steel_lance", (8, 15)),
        guard("Dravenhold Guard", "soldier", "steel_lance", (18, 15)),
        # The first of the Hollowed, already in the hall.
        hollowed("steel_sword", (12, 19)), hollowed("steel_lance", (13, 19)),
        hollowed("steel_axe", (3, 2)), hollowed("steel_sword", (22, 2)),
    ]
    # Every turn more come: through the main doors (south) and out of the alcoves by the dais (north corners).
    doors = [(12, 20), (13, 20), (2, 1), (23, 1), (1, 3), (24, 3)]
    weapons = ["steel_sword", "steel_lance", "steel_axe"]
    for turn in range(1, 9):
        count = 2 if turn < 3 else 3 if turn < 6 else 4
        for i in range(count):
            spawns.append(hollowed(weapons[(turn + i) % 3], doors[(turn * 2 + i) % len(doors)], 13 + turn // 3, f"wave{turn}"))
        if turn >= 4:
            spawns.append(dict(hollowed(weapons[turn % 3], doors[(turn + 3) % len(doors)], 15, f"wave{turn}"), HardOnly=True))
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch15_reveal",
                "combat morvane => scene ch15_morvane",
                "turn 5 => scene ch15_aldric_stays; leave aldric; terrain 0,9-0,11 #"]
    triggers += [f"turn {t} enemy => spawn wave{t}" for t in range(1, 9)]
    highlights = ["morvane => Morvane. Nothing the party has can hurt him yet. Keep away from the dais.",
                  "0,9-0,11 => The west door. Barred for now. Everyone must get out through it, Rowan last, by turn 10."]
    slots = [(10, 14), (11, 14), (12, 14), (13, 14), (14, 14), (15, 14), (11, 16), (12, 16), (13, 16), (14, 16), (10, 16), (15, 16)]
    map_tres("ch15", "Chapter 15: The Grey Chancellor", g.rows(), g.height_rows(), spawns, "escape 0,9-0,11 all",
             ["die aldric", "turns 10"], triggers, slots, background="calder_palace", highlights=highlights)


if __name__ == "__main__":
    terrains()
    items()
    weapons()
    classes()
    units()
    tier1_routes()
    ch8()
    ch9()
    ch10()
    ch11()
    ch12()
    ch13()
    ch14()
    ch15()
