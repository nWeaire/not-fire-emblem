"""Writes Chapter 7, "Storm over Dain Hall" (docs/story/CHAPTERS.md): the map, the throne terrain and Wren.

The .tres files it writes are canonical afterwards (edit them directly); rerunning this overwrites them.
Since session 10/11 data/maps/ch7.tres has moved on (smith's house, highlights, sellswords and axemen, no rear
cavalry): don't rerun this for Ch 7 without porting those back.
    python tools/maps/act1_ch7.py
Dialogue @focus cells for this map live in data/dialogue/ch07.txt.
"""
from act1_ch4_6 import DATA, PLAYER, ENEMY, ALLY, AGGRESSIVE, STATIONARY, GUARD, BOSS, map_tres, terrain, unit
from act1_early import Grid


def terrains():
    terrain("throne", "Throne", "Y", 1, 1, 1, 1, 30, 3, "Color(0.62, 0.50, 0.22, 1)", heal=10)


def units():
    unit("wren", "Wren", "pegasus_knight", 7, dict(HP=2, Str=1, Dex=2, Spd=2, Lck=1, Def=1, Res=2),
         dict(HP=45, Str=40, Mag=10, Dex=50, Spd=60, Lck=45, Def=20, Res=40), ["steel_lance", "javelin"], "")


# ------------------------------------------------------------------ Chapter 7: Storm over Dain Hall (30×24)

def ch7():
    g = Grid(30, 24)
    # Outside the walls (south): the road up from the town, a moat with a drawbridge at the gate.
    for x0, y0, x1, y1 in [(0, 21, 3, 23), (26, 21, 29, 23), (0, 0, 1, 23), (28, 0, 29, 20)]:
        g.rect(x0, y0, x1, y1, "F")
    g.spots(",", [(5, 22), (9, 23), (20, 22), (24, 23), (7, 21), (22, 21)])
    g.hline(20, 2, 27, "~")
    g.vline(14, 20, 23)
    g.vline(15, 20, 23)
    g.spots("B", [(14, 20), (15, 20)])
    # The curtain wall: west, east and south sides (raised), with the gate through the south side.
    g.rect(2, 0, 27, 19, "#")
    g.vline(2, 0, 19, "W")
    g.vline(27, 0, 19, "W")
    g.hline(19, 2, 27, "W")
    g.raise_(2, 0, 2, 19, 2)
    g.raise_(27, 0, 27, 19, 2)
    g.raise_(2, 19, 27, 19, 2)
    g.spots("=", [(14, 19), (15, 19)])
    g.raise_(14, 19, 15, 19, 0)
    # Ramparts: walkways (height 2) inside the west and east walls, reached by a stair (height 1) at each end.
    for x in (3, 26):
        g.raise_(x, 4, x, 16, 2)
        g.put(x, 17, "#")
        g.raise_(x, 17, x, 17, 1)
        g.raise_(x, 3, x, 3, 1)
    # The courtyard: a well, two barracks (forts), a stable block and the chapel garden.
    g.put(9, 13, "~")
    for x0, y0, x1, y1 in [(5, 15, 8, 16), (21, 15, 24, 16), (20, 10, 23, 11)]:
        g.rect(x0, y0, x1, y1, "W")
        g.raise_(x0, y0, x1, y1)
    g.spots("T", [(6, 17), (23, 17), (21, 12), (5, 10)])
    g.rect(6, 10, 8, 11, ",")
    g.spots("F", [(5, 11), (8, 10)])
    g.vline(14, 9, 18)
    g.vline(15, 9, 18)
    # The keep (north): its walls (raised), a door in the south side, and the throne room inside.
    g.rect(8, 0, 21, 8, "W")
    g.raise_(8, 0, 21, 8, 2)
    g.rect(9, 1, 20, 7, "#")
    g.raise_(9, 1, 20, 7, 1)
    g.spots("#", [(14, 8), (15, 8)])
    g.raise_(14, 8, 15, 8, 1)
    for x in (11, 18):
        for y in (3, 5):
            g.put(x, y, "W")
            g.raise_(x, y, x, y, 2)
    g.vline(14, 2, 7, "=")
    g.vline(15, 2, 7, "=")
    g.put(15, 1, "Y")
    g.spots("T", [(9, 1), (20, 1)])

    def soldier(pos, lvl=6, group="", **kv):
        return dict(ClassId="soldier", Name="Dain Hall Soldier", SpriteId="enemy_soldier", Level=lvl,
                    Inventory=["iron_lance"], Pos=pos, Group=group, Armor="padded_gambeson", DominatedBy="halvric", **kv)

    def knight(pos, lvl=7, group="", **kv):
        return dict(ClassId="knight", Name="Dain Hall Knight", SpriteId="enemy_knight", Level=lvl,
                    Inventory=["iron_lance"], Pos=pos, Group=group, Armor="iron_plate", DominatedBy="halvric", **kv)

    def archer(pos, lvl=6, group="", **kv):
        return dict(ClassId="archer", Name="Dain Hall Archer", SpriteId="enemy_archer", Level=lvl,
                    Inventory=["iron_bow"], Pos=pos, Group=group, DominatedBy="halvric", **kv)

    def cavalier(pos, lvl=7, group="", **kv):
        return dict(ClassId="cavalier", Name="Dain Hall Cavalier", SpriteId="enemy_cavalier", Level=lvl,
                    Inventory=["iron_lance"], Pos=pos, Group=group, DominatedBy="halvric", **kv)

    def mage(pos, lvl=6, group="", **kv):
        return dict(ClassId="mage", Name="Dain Hall Mage", SpriteId="enemy_mage", Level=lvl,
                    Inventory=["fire"], Pos=pos, Group=group, DominatedBy="halvric", **kv)

    def flier(pos, lvl=6, group="", **kv):
        return dict(ClassId="pegasus_knight", Name="Sky Patrol", SpriteId="enemy_pegasus_knight", Level=lvl,
                    Inventory=["iron_lance"], Pos=pos, Group=group, DominatedBy="halvric", **kv)

    spawns = [
        dict(ClassId="knight", Name="Lord Halvric", Tag="halvric", SpriteId="boss_halvric", Level=12,
             Armor="iron_plate", Inventory=["silver_lance", "javelin"], Pos=(15, 1), Ai=BOSS, IsBoss=True,
             CrestBearer=True),
        # The gate and the courtyard.
        soldier((14, 17), Ai=GUARD), soldier((15, 17), Ai=GUARD), knight((15, 15), Ai=GUARD),
        soldier((10, 14)), soldier((19, 14), 7), cavalier((12, 11)), cavalier((18, 12)),
        knight((7, 13), Ai=GUARD), mage((22, 13), Ai=GUARD), soldier((5, 12), 7, Ai=GUARD),
        # Archers on the ramparts.
        archer((3, 6), Ai=STATIONARY), archer((3, 13), Ai=STATIONARY), archer((26, 6), Ai=STATIONARY),
        archer((26, 13), Ai=STATIONARY),
        # The keep door and the throne room.
        knight((14, 9), Ai=GUARD), knight((15, 9), 8, Ai=GUARD),
        mage((12, 4), 7, Ai=GUARD), archer((17, 4), 7, Ai=GUARD), knight((13, 2), 8, Ai=GUARD),
        knight((16, 2), 8, Ai=GUARD),
        dict(soldier((10, 2), 8, Ai=GUARD), HardOnly=True),
        # Turn 2: the sky patrol circles in over the east wall; Wren hangs back.
        dict(UnitId="wren", Faction=ENEMY, Level=7, Pos=(24, 14), Ai=STATIONARY, Group="patrol"),
        flier((25, 9), group="patrol"), flier((23, 7), 7, "patrol"),
        # Turn 4: the barracks empty; turn 6: riders come up the road behind the party.
        soldier((6, 17), 7, "barracks"), soldier((23, 17), 7, "barracks"), archer((21, 12), 7, "barracks"),
        cavalier((14, 23), 7, "rear"), cavalier((15, 23), 7, "rear"),
        dict(cavalier((16, 21), 8, "rear"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Ai", AGGRESSIVE)
    triggers = ["turn 1 => scene ch07_start",
                "turn 2 => spawn patrol; scene ch07_patrol",
                "talk rowan wren => scene ch07_wren; join wren",
                "turn 4 enemy => spawn barracks",
                "turn 6 enemy => spawn rear; scene ch07_rear",
                "enter player 9,1-20,7 => scene ch07_throne_room",
                "combat halvric => scene ch07_boss",
                "hp halvric 50 => scene ch07_halvric_plea",
                "defeated halvric => scene ch07_halvric_dies"]
    slots = [(13, 22), (14, 22), (15, 22), (16, 22), (12, 23), (13, 23), (16, 23), (17, 23), (11, 22), (18, 22)]
    map_tres("ch7", "Chapter 7: Storm over Dain Hall", g.rows(), g.height_rows(), spawns, "seize 15,1", [],
             triggers, slots, background="dain_hall")


if __name__ == "__main__":
    terrains()
    units()
    ch7()
