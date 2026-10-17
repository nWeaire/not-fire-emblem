"""Writes the bigger Prologue, Ch 1 and Ch 2 maps (session 7: +60% per side, the user's pick).

Each map is painted onto a grid (base terrain, then roads, rivers, houses, fields...) so the layout reads as code.
The .tres files it writes are canonical afterwards (edit them directly); rerunning this overwrites them.
    python tools/maps/act1_early.py
Dialogue @focus cells for these maps live in data/dialogue/{prologue,ch01,ch02}.txt.
"""
from act1_ch4_6 import DATA, PLAYER, ENEMY, ALLY, AGGRESSIVE, STATIONARY, GUARD, BOSS, map_tres


class Grid:
    def __init__(self, w, h, base="P"):
        self.w, self.h = w, h
        self.cells = [[base] * w for _ in range(h)]
        self.heights = [[0] * w for _ in range(h)]

    def put(self, x, y, c):
        self.cells[y][x] = c

    def rect(self, x0, y0, x1, y1, c):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.cells[y][x] = c

    def hline(self, y, x0, x1, c="="):
        self.rect(x0, y, x1, y, c)

    def vline(self, x, y0, y1, c="="):
        self.rect(x, y0, x, y1, c)

    def spots(self, c, cells):
        for x, y in cells:
            self.cells[y][x] = c

    def house(self, x, y):
        """A 3×2 cottage: wall cells round a fort door at (x+1, y+1)."""
        self.rect(x, y, x + 2, y + 1, "W")
        self.put(x + 1, y + 1, "T")

    def raise_(self, x0, y0, x1, y1, height=1):
        for y in range(y0, y1 + 1):
            for x in range(x0, x1 + 1):
                self.heights[y][x] = height

    def rows(self):
        return ["".join(r) for r in self.cells]

    def height_rows(self):
        return ["".join(str(v) for v in r) for r in self.heights]


def finish(mid, extra):
    """map_tres writes the shared fields; these maps also set ArtSet / Lighting."""
    path = DATA / "maps" / f"{mid}.tres"
    text = path.read_text(encoding="utf-8").rstrip("\n") + "\n" + "".join(f'{k} = "{v}"\n' for k, v in extra.items())
    path.write_text(text, encoding="utf-8", newline="\n")


def gloam(cls, pos, by, **kv):
    weapon = {"gloam_hound": "gloam_fangs", "gloam_brute": "gloam_claws", "gloam_stalker": "bone_spikes"}[cls]
    level = 1 if cls == "gloam_hound" else 2
    return dict(ClassId=cls, DominatedBy=by, SpriteId=f"enemy_{cls}", Level=level, Inventory=[weapon], Pos=pos, **kv)


# ------------------------------------------------------------------ Prologue: Embers at Ashford Farm (22×16)

def prologue():
    g = Grid(22, 16)
    # The treeline to the north (on a rise) and woods round the edges.
    g.hline(0, 0, 21, "F")
    g.spots("F", [(0, 1), (1, 1), (3, 1), (4, 1), (5, 1), (7, 1), (9, 1), (11, 1), (12, 1), (13, 1), (16, 1),
                  (17, 1), (18, 1), (20, 1), (21, 1), (0, 2), (21, 2), (0, 11), (0, 12), (1, 12), (0, 13),
                  (1, 13), (0, 14), (1, 14), (2, 14), (19, 10), (20, 10), (20, 11), (21, 11), (21, 12),
                  (18, 14), (19, 14), (20, 14), (21, 14), (21, 13)])
    g.hline(15, 0, 21, "F")
    g.raise_(0, 0, 21, 0)
    # Meadow patches.
    g.spots(",", [(2, 1), (6, 1), (10, 1), (14, 1), (15, 1), (19, 1), (1, 2), (2, 2), (4, 2), (7, 2), (11, 2),
                  (13, 2), (14, 2), (18, 2), (20, 2), (1, 3), (20, 3), (2, 4), (7, 4), (13, 4), (18, 4),
                  (1, 6), (19, 6), (1, 7), (1, 8), (1, 9), (2, 9), (12, 10), (2, 11), (2, 13), (7, 14),
                  (10, 14), (14, 14), (20, 13)])
    # The farm track: down from the treeline, round the yard, out the south gate; a branch east to the barn.
    g.vline(8, 0, 8)
    g.hline(8, 7, 8)
    g.hline(9, 4, 7)
    g.vline(5, 9, 15)
    g.hline(6, 9, 15)
    # The farmhouse on its rise, the barn, the pond.
    g.house(2, 7)
    g.raise_(1, 7, 5, 9)
    g.house(16, 5)
    g.rect(14, 8, 15, 9, "~")
    # Fields south of the house.
    g.rect(6, 10, 10, 12, ":")
    g.rect(14, 11, 18, 12, ":")
    spawns = [
        dict(UnitId="rowan", Faction=PLAYER, Pos=(5, 10)),
        dict(UnitId="aldric", Faction=ALLY, Pos=(3, 8), Ai=STATIONARY),
        gloam("gloam_hound", (3, 2), "binder"),
        gloam("gloam_hound", (8, 3), "binder"),
        gloam("gloam_hound", (14, 2), "binder"),
        gloam("gloam_hound", (19, 2), "binder"),
        gloam("gloam_hound", (5, 1), "binder"),
        gloam("gloam_brute", (9, 3), "binder"),
        gloam("gloam_brute", (12, 3), "binder"),
        # The barn brute only stirs when someone comes near.
        gloam("gloam_brute", (17, 4), "binder", Ai=GUARD),
        dict(ClassId="binder", Name="The Binder", Tag="binder", SpriteId="enemy_binder", Level=2,
             Inventory=["shadow"], Pos=(11, 1), Ai=BOSS, IsBoss=True, CrestBearer=True),
        gloam("gloam_hound", (14, 1), "binder", HardOnly=True),
    ]
    triggers = ["turn 1 => scene pro_tutorial_move",
                "turn 2 => scene pro_aldric_reveal; join aldric",
                "enemies 8 => scene pro_tutorial_dominion",
                "combat binder => scene pro_binder_battle",
                "defeated binder => scene pro_crest_awakens"]
    map_tres("prologue", "Prologue: Embers at Ashford Farm", g.rows(), g.height_rows(), spawns, "boss binder",
             ["die aldric"], triggers, [])
    finish("prologue", {"ArtSet": "farm", "Lighting": "night"})


# ------------------------------------------------------------------ Chapter 1: Brookhollow Burns (26×19)

def ch1():
    g = Grid(26, 19)
    # Woods round the village.
    g.hline(0, 0, 25, "F")
    g.hline(18, 0, 25, "F")
    g.spots("F", [(0, 1), (1, 1), (0, 2), (24, 1), (25, 1), (25, 2), (0, 13), (0, 14), (1, 14), (0, 15),
                  (0, 16), (1, 16), (0, 17), (1, 17), (2, 17), (25, 9), (25, 10), (24, 10), (25, 15), (25, 16),
                  (24, 17), (25, 17), (6, 1), (15, 1), (23, 4), (23, 10)])
    g.spots(",", [(2, 1), (5, 1), (12, 1), (19, 1), (20, 1), (1, 3), (7, 3), (12, 3), (24, 3), (1, 6), (5, 7),
                  (13, 6), (19, 5), (24, 6), (1, 11), (7, 12), (14, 12), (19, 12), (24, 13), (2, 15),
                  (15, 16), (19, 15)])
    # The river runs south down the east side; three bridges.
    g.vline(17, 0, 18, "~")
    # Roads: the north gate road down through the square to the south, the north lane east to the chapel bridge,
    # and the main street.
    g.vline(10, 0, 18)
    g.hline(4, 1, 25)
    g.hline(11, 0, 25)
    for y in (4, 11, 15):
        g.put(17, y, "B")
    g.hline(15, 18, 22)
    # The well square (packed earth on a farm map) with the well in the middle.
    g.rect(8, 6, 12, 9, "=")
    g.put(10, 8, "T")
    # Cottages in a ring; the chapel across the river; Wenna's house to the south-west.
    for x, y in [(2, 1), (13, 1), (2, 7), (14, 7), (3, 13), (12, 13), (21, 12)]:
        g.house(x, y)
    g.rect(19, 6, 23, 7, "W")
    g.put(21, 7, "T")
    # Burning houses: fire that blocks until turn 4.
    g.spots("^", [(5, 5), (6, 5), (13, 9), (14, 9)])
    # Fields.
    g.rect(4, 15, 8, 17, ":")
    g.rect(19, 16, 23, 17, ":")
    g.rect(12, 16, 15, 17, ":")

    def hound(pos, **kv):
        return gloam("gloam_hound", pos, "sennet", **kv)

    def brute(pos, **kv):
        return gloam("gloam_brute", pos, "sennet", **kv)

    def stalker(pos, **kv):
        return gloam("gloam_stalker", pos, "sennet", **kv)

    def villager(pos):
        return dict(ClassId="villager", Name="Villager", Tag="villager", SpriteId="ally_villager", Faction=ALLY,
                    Pos=pos, Ai=STATIONARY)

    spawns = [
        dict(UnitId="rowan", Faction=PLAYER, Pos=(9, 17)),
        dict(UnitId="aldric", Faction=PLAYER, Pos=(11, 17)),
        dict(UnitId="mira", Faction=PLAYER, Pos=(21, 7)),
        dict(UnitId="kai", Faction=PLAYER, Pos=(10, 8)),
        # Villagers shelter in the south half; the Gloam come in from the north. Nothing starts on the chapel's
        # bank, so Mira has until the turn-3 brute.
        villager((2, 12)), villager((16, 9)), villager((13, 15)), villager((22, 14)), villager((7, 13)),
        hound((6, 2)), hound((8, 3)), hound((12, 2)), hound((4, 6)), hound((12, 5)), hound((3, 4)),
        brute((9, 4)), brute((11, 3)), brute((6, 4)),
        stalker((5, 3)), stalker((13, 5)), stalker((8, 1)),
        dict(ClassId="binder", Name="Sennet", Tag="sennet", SpriteId="boss_sennet", Level=4, Inventory=["shadow"],
             Pos=(10, 0), Ai=BOSS, IsBoss=True, CrestBearer=True),
        brute((20, 9), Group="chapel"),
        hound((8, 0), Group="north"), hound((9, 0), Group="north"), hound((11, 0), Group="north"),
        hound((12, 0), Group="north", HardOnly=True),
    ]
    triggers = ["turn 1 => scene ch01_kai_joins",
                "turn 3 => spawn chapel; scene ch01_mira_fire; give mira fire",
                "turn 4 => terrain 5,5-6,5 13,9-14,9 P; spawn north; scene ch01_reinforcements",
                "combat sennet => scene ch01_boss_sennet",
                "visit 4,14 => scene ch01_wenna; give actor vulnerary"]
    map_tres("ch1", "Chapter 1: Brookhollow Burns", g.rows(), g.height_rows(), spawns, "rout",
             ["protect villager 3"], triggers, [])
    finish("ch1", {"ArtSet": "farm"})


# ------------------------------------------------------------------ Chapter 2: The Millbrook Road (29×19)

def ch2():
    g = Grid(29, 19)
    # Hills and woods to the north, woods along the south edge.
    g.rect(4, 0, 7, 1, "M")
    g.raise_(4, 0, 7, 1)
    g.spots("M", [(5, 2), (6, 2)])
    g.raise_(5, 2, 6, 2)
    g.spots("F", [(0, 0), (1, 0), (2, 0), (3, 0), (8, 0), (9, 0), (10, 0), (0, 1), (1, 1), (9, 1), (16, 0),
                  (17, 0), (18, 0), (19, 0), (17, 1), (18, 1), (12, 3), (13, 3), (20, 4), (21, 4), (28, 0),
                  (28, 1), (28, 2), (2, 11), (3, 12), (12, 12), (13, 12), (13, 13), (19, 11), (20, 12),
                  (0, 17), (1, 17), (0, 18), (1, 18), (2, 18), (3, 18), (12, 18), (13, 18), (14, 18),
                  (26, 17), (27, 17), (28, 17), (25, 18), (26, 18), (27, 18), (28, 18), (27, 13), (28, 13),
                  (28, 14)])
    g.spots(",", [(11, 0), (14, 1), (15, 1), (22, 1), (2, 7), (10, 6), (24, 6), (0, 12), (8, 13), (21, 14),
                  (25, 12), (17, 16), (18, 16), (19, 16), (20, 16), (6, 17)])
    # The river, east-west across the middle; three bridges.
    g.hline(8, 0, 28, "~")
    for x in (6, 17, 25):
        g.put(x, 8, "B")
    # Roads: north bank (to the mill), south bank (the Millbrook road), and the bridge links.
    g.hline(5, 6, 27)
    g.hline(10, 0, 26)
    g.vline(6, 5, 10)
    g.vline(17, 5, 10)
    g.vline(25, 5, 10)
    g.vline(27, 2, 5)
    # The mill (seize tile) in the far north-east, Brann's forge on the south road, two village houses.
    g.rect(25, 1, 28, 2, "W")
    g.put(27, 2, "T")
    # Doors face south (house()), so each is reached from the cell below it.
    g.house(15, 12)  # the forge: door at (16, 13)
    g.house(9, 13)   # house A: door at (10, 14)
    g.house(13, 1)   # house B: door at (14, 2)
    # Fields.
    g.rect(0, 3, 3, 5, ":")
    g.rect(5, 15, 10, 16, ":")
    g.rect(22, 15, 25, 16, ":")
    g.rect(8, 2, 10, 3, ":")

    def bandit(pos, level=2, **kv):
        return dict(ClassId="fighter", Name="Bandit", Level=level, Inventory=["iron_axe"], Pos=pos, **kv)

    def thrower(pos, level=2, **kv):
        return dict(ClassId="fighter", Name="Bandit", Level=level, Inventory=["hand_axe"], Pos=pos, **kv)

    def archer(pos, level=2, **kv):
        return dict(ClassId="archer", Name="Bandit Archer", Level=level, Inventory=["iron_bow"], Pos=pos, **kv)

    spawns = [
        dict(UnitId="brann", Faction=ALLY, Pos=(16, 13), Ai=STATIONARY),
        # South bank, between the party and the forge: these come for the party first.
        bandit((11, 11), 1), thrower((12, 10)), bandit((10, 12), 1), bandit((13, 9)), archer((14, 11)),
        # North bank: the near ones roam; the far ones hold the road to the mill until someone comes close.
        bandit((9, 4), 1), bandit((12, 5), Ai=GUARD), thrower((15, 4), Ai=GUARD), archer((18, 3), Ai=GUARD), bandit((20, 6), Ai=GUARD),
        dict(ClassId="myrmidon", Name="Sellsword", Level=3, Inventory=["iron_sword"], Pos=(22, 5), Ai=GUARD),
        # Gorran's guard at the mill holds its post.
        archer((26, 4), 3, Ai=STATIONARY),
        bandit((24, 3), 3, Ai=GUARD),
        dict(ClassId="myrmidon", Name="Sellsword", Level=3, Inventory=["iron_sword"], Pos=(26, 0), Ai=GUARD),
        dict(ClassId="fighter", Name="Gorran", Tag="gorran", SpriteId="boss_brigand_chief", Armor="leather_jerkin",
             Level=5, Inventory=["iron_axe", "hand_axe"], Pos=(27, 2), Ai=BOSS, IsBoss=True),
        # East bank of the south road, toward the forge.
        bandit((22, 11), Ai=GUARD), thrower((23, 13), 3, HardOnly=True),
        # The ambush comes down from the hills on turn 3.
        archer((3, 1), Group="ambush"), bandit((2, 2), Group="ambush"), bandit((10, 1), Group="ambush"),
    ]
    triggers = ["talk rowan brann => scene ch02_brann; join brann",
                "turn 3 enemy unless joined brann => scene ch02_brann_pressed",
                "turn 3 => spawn ambush",
                "combat gorran => scene ch02_boss",
                "visit 10,14 => scene ch02_house_a; give actor vulnerary",
                "visit 14,2 => scene ch02_house_b; gold 600; give actor energy_drop"]
    slots = [(2, 13), (3, 13), (2, 14), (3, 14), (2, 15), (3, 15)]
    map_tres("ch2", "Chapter 2: The Millbrook Road", g.rows(), g.height_rows(), spawns, "seize 27,2", [],
             triggers, slots)
    finish("ch2", {"ArtSet": "farm"})


if __name__ == "__main__":
    prologue()
    ch1()
    ch2()
