"""Writes the bigger Prologue and Ch 1-6 maps (session 7: +60% per side, the user's pick).

Each map is painted onto a grid (base terrain, then roads, rivers, houses, fields...) so the layout reads as code.
The .tres files it writes are canonical afterwards (edit them directly); rerunning this overwrites them.
    python tools/maps/act1_early.py
Dialogue @focus cells for these maps live in data/dialogue/{prologue,ch01..ch06}.txt.
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


def assert_connected(g, start):
    """Every cell someone on foot can stand on is reachable from start (clutter must not seal anything off)."""
    blocked = set("W~k^")
    seen, todo = {start}, [start]
    while todo:
        x, y = todo.pop()
        for nx, ny in ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1)):
            if (0 <= nx < g.w and 0 <= ny < g.h and (nx, ny) not in seen and g.cells[ny][nx] not in blocked
                    and abs(g.heights[ny][nx] - g.heights[y][x]) <= 1):
                seen.add((nx, ny))
                todo.append((nx, ny))
    stranded = [(x, y) for y in range(g.h) for x in range(g.w) if g.cells[y][x] not in blocked and (x, y) not in seen]
    assert not stranded, f"cut off: {stranded}"


# Gloamkin levels rise through the campaign (session 9: Ch 5's hounds did almost no damage): about Prologue 1,
# Ch 1 1-2, Ch 3 2-3, Ch 5 5-6, with the class growths (data/classes/gloam_*.tres) making each level count.
def gloam(cls, pos, by, **kv):
    weapon = {"gloam_hound": "gloam_fangs", "gloam_brute": "gloam_claws", "gloam_stalker": "bone_spikes",
              "gloam_shade": "gloam_bolt", "gloam_harrier": "gloam_talons"}[cls]
    if cls == "gloam_hound" and by == "binder":
        weapon = "gloam_fangs_young"  # session 11: Prologue hounds took Rowan down in 4 bites (6 each); now 4 a bite
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
        # The east pair and one brute wait until someone comes near (session 9: Rowan, alone until Aldric joins on
        # turn 2, died more often than not when all of them charged at once).
        gloam("gloam_hound", (14, 2), "binder", Ai=GUARD),
        gloam("gloam_hound", (19, 2), "binder", Ai=GUARD),
        gloam("gloam_hound", (5, 1), "binder"),
        gloam("gloam_brute", (9, 3), "binder"),
        gloam("gloam_brute", (12, 3), "binder", Ai=GUARD),
        # The barn brute only stirs when someone comes near.
        gloam("gloam_brute", (17, 4), "binder", Ai=GUARD),
        # The Binder holds the north-east corner beside the waiting pack, so going for it draws them in (session 9:
        # from the middle of the treeline the east hound and brute never joined the fight).
        dict(ClassId="binder", Name="The Binder", Tag="binder", SpriteId="enemy_binder", Level=2,
             Inventory=["shadow"], Pos=(20, 3), Ai=BOSS, IsBoss=True, CrestBearer=True),
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
    # Cottages in a ring; the chapel on the west side (session 9: moved from across the river so Mira, who appears
    # there on turn 3, isn't far from the party); Wenna's house to the south-west.
    for x, y in [(2, 1), (13, 1), (20, 6), (14, 7), (3, 13), (12, 13), (21, 12)]:
        g.house(x, y)
    g.rect(1, 7, 5, 8, "W")
    g.put(3, 8, "T")
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
        # Mira appears in the chapel only when her fire scene plays (session 9: the user wants her introduced there).
        dict(UnitId="mira", Faction=PLAYER, Pos=(3, 8), Group="mira"),
        dict(UnitId="kai", Faction=PLAYER, Pos=(10, 8)),
        # Villagers shelter in the south half; the Gloam come in from the north.
        villager((2, 12)), villager((16, 9)), villager((13, 15)), villager((22, 14)), villager((7, 13)),
        hound((6, 2)), hound((8, 3)), hound((12, 2)), hound((4, 6)), hound((12, 5)), hound((3, 4)),
        brute((9, 4)), brute((11, 3)),  # two brutes since Aldric was weakened (session 7)
        stalker((5, 3)), stalker((13, 5)), stalker((8, 1)),
        dict(ClassId="binder", Name="Sennet", Tag="sennet", SpriteId="boss_sennet", Level=4, Inventory=["shadow"],
             Pos=(10, 0), Ai=BOSS, IsBoss=True, CrestBearer=True),
        brute((4, 10), Group="chapel"),
        hound((8, 0), Group="north"), hound((9, 0), Group="north"), hound((11, 0), Group="north"),
        hound((12, 0), Group="north", HardOnly=True),
    ]
    triggers = ["turn 1 => scene ch01_kai_joins",
                "turn 3 => spawn mira; spawn chapel; scene ch01_mira_fire; give mira fire",
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
    # The forge sits two moves from the deploy slots (session 7: the user couldn't reach Brann in time); house A
    # is further east.
    g.house(9, 13)   # the forge: door at (10, 14)
    g.house(15, 12)  # house A: door at (16, 13)
    g.house(13, 1)   # house B: door at (14, 2)
    # Fields.
    g.rect(0, 3, 3, 5, ":")
    g.rect(5, 15, 10, 16, ":")
    g.rect(22, 15, 25, 16, ":")
    g.rect(8, 2, 10, 3, ":")

    def bandit(pos, level=2, **kv):
        return dict(ClassId="fighter", Name="Bandit", Level=level, Inventory=["iron_axe"], Pos=pos, **kv)

    def thrower(pos, level=2, **kv):
        # Named apart: they throw from 2 cells, where swords and lances can't answer.
        return dict(ClassId="fighter", Name="Axe Thrower", Level=level, Inventory=["hand_axe"], Pos=pos, **kv)

    def archer(pos, level=2, **kv):
        return dict(ClassId="archer", Name="Bandit Archer", Level=level, Inventory=["iron_bow"], Pos=pos, **kv)

    spawns = [
        dict(UnitId="brann", Faction=ALLY, Pos=(10, 14), Ai=STATIONARY),
        # South bank, east of the forge: they reach it about when the party does (turn 2), so you fight beside Brann.
        bandit((17, 11), 1), thrower((18, 10)), bandit((17, 15), 1), bandit((19, 13)), archer((20, 11)),
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
                "visit 16,13 => scene ch02_house_a; give actor vulnerary",
                "visit 14,2 => scene ch02_house_b; gold 300; give actor energy_drop"]
    slots = [(2, 13), (3, 13), (2, 14), (3, 14), (2, 15), (3, 15)]
    map_tres("ch2", "Chapter 2: The Millbrook Road", g.rows(), g.height_rows(), spawns, "seize 27,2", [],
             triggers, slots)
    finish("ch2", {"ArtSet": "farm"})


# ------------------------------------------------------------------ Chapter 3: Thornwood Hunt (32×22)

def ch3():
    g = Grid(32, 22, "F")
    # Clearings in the forest: deploy (south-west), Tamsin's (centre), the north-west glade, a small north glade,
    # Varga's (north-east, round a ruined hunting lodge) and the dead dogs' (south-east).
    for x0, y0, x1, y1 in [(1, 16, 7, 20), (11, 9, 19, 13), (3, 2, 9, 6), (14, 3, 17, 5), (21, 1, 29, 6), (21, 15, 27, 19)]:
        g.rect(x0, y0, x1, y1, "P")
    # Ragged clearing edges: a few trees back in, a few meadow patches.
    g.spots("F", [(1, 16), (7, 20), (11, 9), (19, 13), (3, 6), (9, 2), (21, 6), (29, 1), (27, 19), (21, 15), (17, 3)])
    g.spots(",", [(2, 19), (5, 16), (13, 12), (18, 10), (4, 3), (8, 5), (23, 2), (28, 5), (24, 18), (26, 16), (15, 4)])
    # Hunters' trails between the clearings.
    g.hline(18, 7, 11, "=")
    g.vline(11, 13, 18, "=")
    g.hline(11, 19, 22, "=")
    g.vline(22, 6, 11, "=")
    g.vline(16, 13, 17, "=")
    g.hline(17, 16, 21, "=")
    g.vline(10, 6, 9, "=")
    g.hline(6, 9, 10, "=")
    # Rock outcrops (raised) and a pond feeding a stream to the south.
    g.rect(8, 7, 9, 8, "M")
    g.raise_(8, 7, 9, 8)
    g.rect(24, 11, 25, 12, "M")
    g.raise_(24, 11, 25, 12)
    g.rect(28, 9, 30, 11, "~")
    g.vline(29, 12, 21, "~")
    # The ruined hunting lodge in Varga's clearing, and Tamsin's hunter's hut at the west of the central clearing,
    # near where the trail from the deploy clearing comes in: she holds it (fort: Def, Avoid, healing) until the party
    # reaches her.
    g.house(25, 1)
    g.put(13, 12, "T")

    def hound(pos, level=2, **kv):
        return gloam("gloam_hound", pos, "varga", **kv) | {"Level": level}

    def stalker(pos):
        return gloam("gloam_stalker", pos, "varga", Ai=GUARD)

    def brute(pos):
        return gloam("gloam_brute", pos, "varga") | {"Level": 3}

    spawns = [
        # Stalkers wait in the trees; hounds roam. None within a move of Tamsin's clearing (she must last until the
        # party arrives) or of the deploy clearing.
        stalker((9, 5)), stalker((5, 10)), stalker((8, 13)), stalker((23, 13)), stalker((27, 8)),
        # Varga's pack holds the north and east until someone comes near; Tamsin is an archer and can't answer
        # hounds at her side, so only her own two press her before the party arrives.
        hound((16, 4), Ai=GUARD), hound((19, 3), Ai=GUARD), hound((24, 9), Ai=GUARD),
        hound((22, 16), Ai=GUARD), hound((26, 18), Ai=GUARD),  # the pack at the dead dogs
        brute((27, 5)), brute((15, 3)) | {"Ai": GUARD},
        dict(ClassId="binder", Name="Varga", Tag="varga", SpriteId="boss_varga", Level=6, Inventory=["shadow"],
             Pos=(26, 3), Ai=BOSS, IsBoss=True, CrestBearer=True),
        hound((18, 12), HardOnly=True),
        # Turn 2: Tamsin, fighting two hounds alone in the central clearing.
        dict(UnitId="tamsin", Group="tamsin", Faction=ALLY, Pos=(13, 12)),
        hound((14, 10), 1, Group="tamsin"), hound((16, 12), 1, Group="tamsin"),
        # Varga's guard: one of each Gloamkin appears at her side when the party enters her clearing.
        hound((23, 4), 3, Group="varga_pack"), gloam("gloam_stalker", (28, 2), "varga", Group="varga_pack") | {"Level": 3},
        brute((25, 5)) | {"Group": "varga_pack"},
        # Shades (session 9): Gloamkin casters whose Gloom Bolt is effective against armour (Aldric, Dorran).
        gloam("gloam_shade", (24, 4), "varga", Group="varga_pack") | {"Level": 3},
        gloam("gloam_shade", (6, 3), "varga", Ai=GUARD),
    ]
    # Tamsin joins as soon as she appears (an ally AI ran her straight at Varga); the Talk is her conversation.
    triggers = ["turn 2 => spawn tamsin; scene ch03_tamsin_seen; join tamsin",
                "talk rowan tamsin => scene ch03_tamsin",
                "enter player 21,1-29,6 => spawn varga_pack; scene ch03_varga_pack",
                "enter player 23,16-26,18 => scene ch03_dogs",
                "combat varga => scene ch03_boss",
                "defeated varga => scene ch03_varga_dies"]
    slots = [(2, 17), (3, 17), (2, 18), (3, 18), (4, 18), (3, 19)]
    map_tres("ch3", "Chapter 3: Thornwood Hunt", g.rows(), g.height_rows(), spawns, "boss varga", [], triggers, slots,
             background="thornwood", fog=True)


# ------------------------------------------------------------------ Chapter 4: The Scholar of Calder (26×26)

def ch4():
    # Session 12: data/maps/ch4.tres mixes Dain Hall Sellswords (iron sword) and Axemen (iron axe) in among the lance
    # soldiers, edited by hand; port that here before rerunning this for Ch 4.
    g = Grid(26, 26)
    # Hill country: rocky hills in the corners (raised), woods along the flanks.
    for x0, y0, x1, y1 in [(0, 0, 3, 3), (22, 0, 25, 3), (0, 22, 2, 25), (23, 22, 25, 25)]:
        g.rect(x0, y0, x1, y1, "M")
        g.raise_(x0, y0, x1, y1, 2)
    g.raise_(0, 4, 3, 7)
    g.raise_(22, 4, 25, 7)
    for x0, y0, x1, y1 in [(4, 0, 7, 2), (18, 0, 21, 2), (0, 8, 2, 12), (23, 8, 25, 12), (4, 7, 7, 10), (18, 8, 21, 11),
                           (5, 15, 8, 18), (17, 19, 19, 21), (3, 20, 6, 23), (0, 14, 1, 19), (24, 14, 25, 19)]:
        g.rect(x0, y0, x1, y1, "F")
    g.spots(",", [(9, 6), (10, 7), (16, 6), (15, 8), (4, 12), (9, 13), (17, 13), (20, 14), (8, 20), (16, 23),
                  (10, 10), (21, 6), (6, 13), (19, 24), (9, 23)])
    # The north road through the toll gate (a palisade with the road through it) down to the south exit.
    g.vline(12, 0, 25)
    g.vline(13, 0, 25)
    g.hline(4, 3, 10, "W")
    g.hline(4, 15, 22, "W")
    g.raise_(3, 4, 10, 4, 2)   # session 12 playtest: real stone walls, not flat ones
    g.raise_(15, 4, 22, 4, 2)
    # The waystation to the south-east: three buildings (forts) where the party can rest.
    g.spots("T", [(20, 20), (21, 21), (22, 21)])
    g.spots("P", [(10, 25), (11, 25), (14, 25), (15, 25)])

    def soldier(pos, lvl=4, group=""):
        return dict(ClassId="soldier", Name="Dain Hall Soldier", SpriteId="enemy_soldier", Level=lvl,
                    Inventory=["iron_lance"], Pos=pos, Group=group, Armor="padded_gambeson")

    def archer(pos, group=""):
        return dict(ClassId="archer", Name="Dain Hall Archer", SpriteId="enemy_archer", Level=4,
                    Inventory=["iron_bow"], Pos=pos, Group=group)

    spawns = [
        # Elise on the road, just out of the first wave's reach; the soldiers wait at the toll gate.
        dict(UnitId="elise", Faction=ALLY, Level=4, Pos=(12, 13), Ai=STATIONARY),
        # No cavaliers and no east-woods ambush (session 9 playtest: with the ambushes the escape was far too hard).
        soldier((11, 2)), soldier((13, 3)), soldier((16, 2), 5), archer((9, 2)), soldier((10, 2), 5),
        soldier((15, 3)),
        # A wave down the road every turn.
        soldier((12, 0), 4, "north2"), soldier((13, 0), 4, "north2"),
        archer((12, 0), "north3"),
        soldier((12, 0), 5, "north4"), soldier((13, 0), 5, "north4"),
        soldier((12, 0), 5, "north5"), dict(archer((13, 0), "north5"), HardOnly=True),
        # Ambushes out of the southern woods (session 9 playtest: the party routed the north and strolled out), so
        # the escape means choosing where to fight. Each group comes out beside the road further south.
        soldier((7, 16), 5, "woods3"), soldier((6, 17), 5, "woods3"), archer((5, 15), "woods3"),
        soldier((5, 22), 5, "woods5"), soldier((6, 21), 5, "woods5"), archer((4, 21), "woods5"),
        dict(soldier((24, 16), 5, "woods5"), Name="Dain Hall Sergeant", Level=6),
    ]
    triggers = ["turn 1 => scene ch04_start",
                "talk rowan elise => scene ch04_elise; join elise",
                "turn 2 => spawn north2", "turn 3 => spawn north3; scene ch04_soldiers",
                "turn 4 => spawn north4", "turn 5 => spawn north5",
                # The party can reach the exit on turn 3, so the woods spring from the first enemy phase.
                "turn 1 enemy => spawn woods3; scene ch04_ambush",
                "turn 3 enemy => spawn woods5",
                "enter player 19,19-22,21 => scene ch04_waystation"]
    slots = [(15, 13), (16, 13), (15, 14), (16, 14), (15, 15), (16, 12)]
    map_tres("ch4", "Chapter 4: The Scholar of Calder", g.rows(), g.height_rows(), spawns,
             "escape 10,25-15,25 elise rowan", ["die elise"], triggers, slots, background="hill_road")


# ------------------------------------------------------------------ Chapter 5: Ferrow's Keep (29×22)

def ch5():
    g = Grid(29, 22)
    # The keep: walls round a stone courtyard, open to the west (the party's side); the east wall faces the Gloam.
    g.rect(4, 1, 18, 20, "#")
    g.hline(0, 3, 19, "W")
    g.hline(21, 3, 19, "W")
    g.vline(19, 0, 21, "W")
    g.vline(3, 0, 21, "W")
    g.vline(3, 8, 13, "#")       # the west gateway, open
    g.raise_(3, 0, 19, 0)
    g.raise_(3, 21, 19, 21)
    g.raise_(19, 0, 19, 21)
    g.put(19, 17, "P")           # a breach in the east wall, open from the start
    g.raise_(19, 17, 19, 17, 0)
    # Inside: the hall and two barrack blocks (walls, raised) break up the yard; forts are the keep's rooms.
    for x0, y0, x1, y1 in [(6, 8, 9, 13), (11, 3, 14, 5), (11, 16, 14, 18)]:
        g.rect(x0, y0, x1, y1, "W")
        g.raise_(x0, y0, x1, y1)
    g.spots("T", [(16, 2), (5, 3), (16, 10), (5, 18), (16, 19), (10, 10)])
    # The field to the east, where the waves come from.
    for x0, y0, x1, y1 in [(24, 1, 26, 3), (22, 17, 24, 19), (26, 10, 28, 12), (27, 18, 28, 21), (20, 0, 21, 1)]:
        g.rect(x0, y0, x1, y1, "F")
    g.spots(",", [(21, 7), (23, 13), (25, 6), (21, 20), (27, 15)])
    g.spots("F", [(0, 0), (1, 0), (0, 21), (1, 21), (0, 20)])
    # Siege clutter (session 9 playtest: the yard looked empty): wagons, stalls, crates and barrels as barricades
    # (no one on foot gets through), and rubble from the battered walls (slow ground with a little cover).
    g.spots("k", [(8, 3), (9, 3), (16, 6), (17, 13), (17, 14), (5, 14), (10, 15), (15, 13), (7, 19), (8, 19),
                  (12, 7), (5, 6), (16, 16), (10, 1)])
    g.spots("x", [(18, 16), (17, 17), (18, 18), (17, 9), (18, 12), (15, 15), (9, 6), (4, 11), (12, 20), (18, 5),
                  (13, 15), (6, 16), (21, 11), (22, 13), (21, 17), (20, 8)])
    assert_connected(g, (4, 10))

    def shade(pos, group=""):
        return dict(ClassId="gloam_shade", SpriteId="enemy_gloam_shade", Level=5, Group=group,
                    Inventory=["gloam_bolt"], Pos=pos, DominatedBy="hollin")

    def harrier(pos, group=""):
        return dict(ClassId="gloam_harrier", SpriteId="enemy_gloam_harrier", Level=5, Group=group,
                    Inventory=["gloam_talons"], Pos=pos, DominatedBy="hollin")

    def hound(pos, group=""):
        return dict(ClassId="gloam_hound", SpriteId="enemy_gloam_hound", Level=6, Group=group,
                    Inventory=["gloam_fangs"], Pos=pos, DominatedBy="hollin")

    def brute(pos, group=""):
        return dict(ClassId="gloam_brute", SpriteId="enemy_gloam_brute", Level=6, Group=group,
                    Inventory=["gloam_claws"], Pos=pos, DominatedBy="hollin")

    def stalker(pos, group=""):
        return dict(ClassId="gloam_stalker", SpriteId="enemy_gloam_stalker", Level=6, Group=group,
                    Inventory=["bone_spikes"], Pos=pos, DominatedBy="hollin")

    spawns = [
        dict(UnitId="dorran", Faction=ALLY, Level=8, Pos=(18, 11), Ai=STATIONARY),
        hound((22, 5)), hound((22, 15)), stalker((24, 8)), stalker((24, 16)), brute((24, 11)),
        # The Alpha is its own boss class (session 9: as a level-10 brute it was too weak).
        dict(ClassId="gloam_alpha", Name="Gloam Alpha", Tag="alpha", SpriteId="enemy_gloam_alpha", Level=10,
             Inventory=["alpha_claws"], Pos=(22, 11), Ai=BOSS, IsBoss=True, DominatedBy="hollin"),
        dict(ClassId="binder", Name="Binder Hollin", Tag="hollin", SpriteId="enemy_binder", Level=6,
             Inventory=["shadow"], Pos=(27, 1), Ai=STATIONARY, CrestBearer=True),
        hound((28, 6), "wave2"), hound((28, 15), "wave2"), stalker((28, 10), "wave2"),
        brute((28, 5), "wave3"), hound((28, 11), "wave3"), hound((28, 16), "wave3"),
        hound((28, 3), "wave4"), stalker((28, 13), "wave4"), brute((28, 17), "wave4"),
        # Harriers fly straight over the walls at whoever is behind the line.
        harrier((26, 2)), harrier((27, 7), "wave3"), harrier((27, 15), "wave3"), harrier((28, 1), "wave5"),
        harrier((28, 20), "wave5"),
        shade((25, 11)), shade((28, 7), "wave4"), shade((28, 12), "wave6"),
        hound((28, 9), "wave5"), stalker((28, 4), "wave5"),
        hound((28, 8), "wave6"), hound((28, 14), "wave6"), dict(brute((28, 11), "wave6"), HardOnly=True),
    ]
    triggers = ["turn 1 => scene ch05_dorran; join dorran",
                "turn 2 enemy => spawn wave2", "turn 3 enemy => spawn wave3", "turn 4 enemy => spawn wave4",
                "turn 5 => terrain 19,10-19,11 #; scene ch05_gate", "turn 5 enemy => spawn wave5",
                "turn 6 enemy => spawn wave6",
                "combat alpha => scene ch05_alpha",
                "defeated hollin => scene ch05_hollin; gold 300; win"]
    slots = [(13, 8), (14, 8), (13, 11), (14, 11), (12, 10), (14, 10), (13, 13)]
    map_tres("ch5", "Chapter 5: Ferrow's Keep", g.rows(), g.height_rows(), spawns, "survive 8", ["die dorran"], triggers,
             slots, background="ferrows_keep")


# ------------------------------------------------------------------ Chapter 6: The Bandit King of Greyfen (32×22)

def ch6():
    g = Grid(32, 22)
    # The fen: a deep channel winds north to south through the middle, edged by shallows; reed beds and
    # shallow pools everywhere else. Firm ground is the paths between them.
    channel = [11, 11, 12, 12, 13, 13, 12, 12, 13, 14, 14, 15, 15, 14, 14, 13, 13, 14, 15, 15, 16, 16]
    for y, x in enumerate(channel):
        g.rect(x - 1, y, x + 2, y, "s")
        g.rect(x, y, x + 1, y, "~")
    for x0, y0, x1, y1 in [(4, 1, 7, 3), (2, 8, 5, 10), (7, 13, 10, 15), (3, 18, 6, 20), (19, 6, 22, 8),
                           (24, 12, 27, 14), (20, 17, 23, 19), (27, 7, 29, 9), (17, 1, 19, 2)]:
        g.rect(x0, y0, x1, y1, "s")
    for x0, y0, x1, y1 in [(8, 2, 9, 5), (0, 4, 1, 6), (5, 11, 6, 12), (1, 14, 3, 15), (9, 18, 10, 20),
                           (18, 10, 20, 12), (22, 3, 23, 5), (25, 16, 27, 18), (29, 11, 31, 13), (21, 14, 22, 15),
                           (28, 19, 31, 21), (24, 0, 26, 1)]:
        g.rect(x0, y0, x1, y1, "r")
    for x0, y0, x1, y1 in [(0, 0, 3, 2), (6, 6, 8, 8), (0, 15, 2, 16), (11, 19, 13, 21), (16, 13, 18, 15),
                           (22, 9, 25, 10), (28, 15, 31, 17), (17, 19, 19, 21), (23, 0, 26, 0), (29, 0, 31, 1),
                           (8, 10, 10, 11), (4, 15, 6, 16), (26, 10, 28, 11), (20, 0, 22, 1)]:
        g.rect(x0, y0, x1, y1, "s")
    g.spots("r", [(7, 0), (3, 7), (0, 11), (12, 12), (16, 6), (23, 11), (27, 20), (15, 20), (7, 17), (30, 3)])
    # Boardwalks over the channel: north, middle and south crossings.
    for y in (3, 10, 17):
        x = channel[y]
        g.rect(x - 1, y, x + 2, y, "B")
    # Stilt houses: the village by the start, a hut mid-fen, and Rusk's hall to the north-east.
    g.spots("h", [(5, 6), (2, 12), (9, 9), (19, 15), (24, 9), (27, 3), (29, 5), (25, 5)])
    g.rect(26, 2, 29, 4, "P")
    g.put(27, 3, "h")

    def bandit(pos, lvl=5, group=""):
        return dict(ClassId="fighter", Name="Greyfen Bandit", SpriteId="enemy_fighter", Level=lvl,
                    Inventory=["iron_axe"], Pos=pos, Group=group, DominatedBy="rusk", Armor="leather_jerkin")

    def archer(pos, lvl=5):
        return dict(ClassId="archer", Name="Greyfen Archer", SpriteId="enemy_archer", Level=lvl,
                    Inventory=["iron_bow"], Pos=pos, DominatedBy="rusk")

    spawns = [
        dict(UnitId="sable", Faction=ENEMY, Level=6, Pos=(17, 9), Ai=STATIONARY),
        dict(ClassId="fighter", Name="Rusk", Tag="rusk", SpriteId="boss_rusk", Level=9, Armor="chainmail",
             Inventory=["steel_axe", "hand_axe"], Pos=(27, 3), Ai=BOSS, IsBoss=True, CrestBearer=True),
        bandit((9, 7)), bandit((16, 5)), bandit((19, 13)), bandit((22, 6), 6), bandit((21, 15)),
        bandit((26, 6), 6), bandit((29, 6), 6), bandit((24, 18), 6),
        archer((3, 5)), archer((20, 4)), archer((25, 2), 6),
        dict(ClassId="myrmidon", Name="Greyfen Sellsword", SpriteId="enemy_myrmidon", Level=6,
             Inventory=["iron_sword"], Pos=(30, 9), DominatedBy="rusk"),
        dict(bandit((30, 4), 6), HardOnly=True),
    ]
    for unit in spawns[2:]:
        unit.setdefault("Ai", GUARD)
    triggers = ["talk kai sable => scene ch06_sable; join sable",
                "turn 1 => scene ch06_start",
                "turn 5 => scene ch06_scouts",
                "visit 5,6 => scene ch06_house; give actor elixir",
                "combat rusk => scene ch06_boss",
                "defeated rusk => scene ch06_rusk"]
    slots = [(1, 17), (2, 17), (1, 18), (2, 18), (1, 19), (2, 19), (1, 20), (2, 20)]
    map_tres("ch6", "Chapter 6: The Bandit King of Greyfen", g.rows(), g.height_rows(), spawns, "boss rusk", [],
             triggers, slots, background="greyfen")


if __name__ == "__main__":
    prologue()
    ch1()
    ch2()
    ch3()
    ch4()
    ch5()
    ch6()
