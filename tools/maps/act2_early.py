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


def class_tres(cid, name, bases, growths, weapons, promotes_to, promoted, tier, traits, arts=()):
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
MovementType = 0
UsableWeapons = PackedStringArray({q(weapons)})
PromotesTo = PackedStringArray({q(promotes_to)})
IsPromoted = {"true" if promoted else "false"}
MaxArmor = 0
Tier = {tier}
Traits = PackedStringArray({q(traits)})
Arts = PackedStringArray({q(arts)})
Spells = PackedStringArray()
""")


def classes():
    # Pip's line (STORY §6: Thief -> Rogue -> Shadowstep). Fast and lucky, light swords, opens locks and steals.
    class_tres("thief", "Thief", dict(HP=16, Str=3, Dex=5, Spd=9, Lck=5, Def=2, Res=1, Mov=6),
               dict(HP=5, Str=5, Dex=10, Spd=15, Lck=10), ["Sword"], ["rogue"], False, 1, ["Locktouch", "Steal"])
    class_tres("rogue", "Rogue", dict(HP=22, Str=6, Mag=1, Dex=10, Spd=13, Lck=7, Def=4, Res=3, Mov=7),
               dict(HP=10, Str=5, Dex=10, Spd=10, Lck=10), ["Sword"], [], True, 2, ["Locktouch", "Steal"])


def units():
    unit("pip", "Pip", "thief", 6, dict(HP=1, Dex=2, Spd=3, Lck=4),
         dict(HP=50, Str=30, Mag=5, Dex=55, Spd=75, Lck=70, Def=15, Res=25), ["iron_sword", "vulnerary"], "")


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


if __name__ == "__main__":
    terrains()
    items()
    classes()
    units()
    ch8()
    ch9()
