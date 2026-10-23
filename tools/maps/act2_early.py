"""Writes the Act II maps (docs/story/CHAPTERS.md), at the Act I scale (session 7: +60% per side over the outline).

Each map is painted onto a grid like act1_early.py. The .tres files it writes are canonical afterwards (edit them
directly); rerunning this overwrites them.
    python tools/maps/act2_early.py
Dialogue @focus cells for these maps live in data/dialogue/ch08.txt (and later chapters).
"""
from act1_ch4_6 import PLAYER, ENEMY, ALLY, AGGRESSIVE, STATIONARY, GUARD, map_tres
from act1_early import Grid, assert_connected

NEUTRAL = 3


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
        return dict(ClassId=cls, Name="Veiled Agitator", Tag="agitator", SpriteId=f"enemy_{cls}", Level=lvl,
                    Inventory=[weapon], Pos=pos, Armor="padded_gambeson", Ignores="civilian", Ai=GUARD, **kv)

    def thug(cls, weapon, pos, lvl=8, group="", **kv):
        return dict(ClassId=cls, Name="Veiled Thug", SpriteId=f"enemy_{cls}", Level=lvl, Inventory=[weapon],
                    Pos=pos, Group=group, Ignores="civilian", **kv)

    def rioter(pos):
        # Townsfolk with cudgels (Mt 1: a riled mob takes about four turns to kill a townsperson); riled by any agitator within 3 cells (Dominion), calm again once he falls.
        return dict(ClassId="fighter", Name="Rioter", SpriteId="ally_villager", Level=3, Inventory=["cudgel"],
                    Pos=pos, Faction=NEUTRAL, DominatedBy="agitator")

    def civilian(pos):
        return dict(ClassId="villager", Name="Townsfolk", Tag="civilian", SpriteId="ally_villager", Faction=ALLY,
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


if __name__ == "__main__":
    ch8()
