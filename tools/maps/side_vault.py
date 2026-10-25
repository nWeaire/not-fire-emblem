"""Writes the Act II side map "The Old Warden Vault" (docs/story/CHAPTERS.md, Paralogues & optional content):
data/maps/vault.tres. Offered by the hub before Ch 15 (data/campaign/main.txt "side vault ..."); scenes in
data/dialogue/vault.txt.

Aldric takes Rowan to the ruined Crestwarden chapterhouse in the hills outside Calder. Two Warden seals bar the way
in; only Aldric's sigil opens them (he walks up to the plinth in front of each). Rowan seizes the altar, where
Lyra's Blade lies. Morvane has kept the place watched for twenty years: Hollowed in the courtyard, a fallen Warden
in the hall, Gloamkin in the woods. Aldric must survive.

The .tres it writes is canonical afterwards (edit it directly); rerunning this overwrites it.
    python tools/maps/side_vault.py
"""
from act1_ch4_6 import ENEMY, AGGRESSIVE, STATIONARY, GUARD, BOSS, map_tres
from act1_early import Grid, assert_connected, finish

W, H = 26, 24
# The two seals and the plinth cells in front of them (Aldric opens a seal by stepping onto its plinth row).
OUTER_SEAL, OUTER_PLINTH = (12, 10, 13, 10), (11, 11, 14, 11)
INNER_SEAL, INNER_PLINTH = (12, 5, 13, 5), (11, 6, 14, 6)
ALTAR = (12, 2)


def cells(r):
    x0, y0, x1, y1 = r
    return f"{x0},{y0}-{x1},{y1}" if (x0, y0) != (x1, y1) else f"{x0},{y0}"


def vault():
    g = Grid(W, H, "P")
    # The hillside: woods down both flanks, a road up the middle to the broken gate.
    g.rect(0, 11, 2, 23, "F")
    g.rect(23, 11, 25, 23, "F")
    g.spots("F", [(3, 19), (4, 22), (21, 20), (22, 17), (6, 21), (19, 22)])
    g.vline(12, 17, 23, "=")
    g.vline(13, 17, 23, "=")
    g.spots("x", [(9, 19), (16, 18), (5, 17), (20, 19)])

    # Rock round the whole chapterhouse.
    g.rect(0, 0, 25, 10, "W")
    g.raise_(0, 0, 25, 10, 2)
    g.rect(3, 11, 22, 16, "W")
    g.raise_(3, 11, 22, 16, 2)

    def carve(x0, y0, x1, y1, c="#"):
        g.rect(x0, y0, x1, y1, c)
        g.raise_(x0, y0, x1, y1, 0)

    # The courtyard, its south wall broken in three places (the gate and two breaches).
    carve(4, 11, 21, 15)
    carve(12, 16, 13, 16, "x")
    carve(7, 16, 8, 16, "x")
    carve(17, 16, 18, 16, "x")
    g.spots("x", [(6, 13), (19, 12), (15, 14), (9, 12)])
    # Fallen pillars: single raised stones.
    for x, y in [(7, 12), (18, 14), (10, 14), (16, 12)]:
        g.put(x, y, "W")
        g.raise_(x, y, x, y, 2)

    # The hall of the Wardens behind the outer seal, with a chest in each of its side chapels.
    carve(4, 6, 21, 9)
    for x, y in [(7, 7), (10, 8), (15, 8), (18, 7)]:
        g.put(x, y, "W")
        g.raise_(x, y, x, y, 2)
    g.spots("C", [(5, 6), (20, 6)])

    # The altar room behind the inner seal.
    carve(9, 2, 16, 4)
    g.put(*ALTAR, "Y")

    # The seals: gate stone, impassable until Aldric opens them.
    for (x0, y0, x1, y1) in (OUTER_SEAL, INNER_SEAL):
        g.rect(x0, y0, x1, y1, "G")
        g.raise_(x0, y0, x1, y1, 0)

    assert_connected_except_seals(g)

    def hollowed(weapon, pos, lvl=14, group="", **kv):
        return dict(ClassId="hollowed", Name="Hollowed", SpriteId="enemy_hollowed", Level=lvl, Inventory=[weapon],
                    Pos=pos, Group=group, **kv)

    def gloam(cls, weapon, pos, lvl=14, group="", **kv):
        names = {"gloam_hound": "Gloam Hound", "gloam_stalker": "Gloam Stalker", "gloam_shade": "Gloam Shade",
                 "gloam_harrier": "Gloam Harrier"}
        return dict(ClassId=cls, Name=names[cls], Level=lvl, Inventory=[weapon], Pos=pos, Group=group, **kv)

    spawns = [
        # The woods: Gloamkin held by the Veiled keeper in the courtyard.
        gloam("gloam_hound", "gloam_fangs", (2, 15), DominatedBy="keeper"),
        gloam("gloam_hound", "gloam_fangs", (23, 14), DominatedBy="keeper"),
        gloam("gloam_harrier", "gloam_talons", (21, 12), DominatedBy="keeper"),
        # The courtyard: the keeper, his shades, and the Hollowed on watch.
        dict(ClassId="binder", Name="Veiled Keeper", Tag="keeper", SpriteId="enemy_binder", Level=15,
             Inventory=["shadow"], Pos=(17, 11), Ai=GUARD, CrestBearer=True, Leash=3),
        gloam("gloam_shade", "gloam_bolt", (5, 12), DominatedBy="keeper", Ai=GUARD),
        hollowed("steel_sword", (11, 13)), hollowed("steel_lance", (14, 13)),
        hollowed("steel_axe", (8, 15), Ai=GUARD), hollowed("steel_lance", (20, 14), Ai=GUARD),
        dict(hollowed("silver_sword", (12, 12), 15), HardOnly=True),
        # The hall: Ser Caddoc, a Warden who didn't escape, and the hall guard.
        dict(ClassId="warden_lord", Name="Ser Caddoc, the Hollow Warden", Tag="caddoc", SpriteId="boss_caddoc",
             Level=16, Inventory=["silver_lance", "javelin"], Pos=(12, 7), Ai=BOSS, IsBoss=True, Leash=2,
             Armor="iron_plate"),
        dict(ClassId="knight", Name="Hollowed Knight", SpriteId="enemy_hollowed", Level=15, Inventory=["steel_lance"],
             Pos=(6, 8), Ai=GUARD, Armor="iron_plate"),
        dict(ClassId="knight", Name="Hollowed Knight", SpriteId="enemy_hollowed", Level=15, Inventory=["steel_lance"],
             Pos=(19, 8), Ai=GUARD, Armor="iron_plate"),
        hollowed("steel_axe", (9, 6), 15, Ai=GUARD),
        # When the outer seal opens, the dead in the courtyard stand up behind the party.
        hollowed("steel_sword", (6, 14), 14, "risen"), hollowed("steel_axe", (19, 13), 14, "risen"),
        hollowed("steel_lance", (12, 15), 14, "risen"),
        # Enemy turn 5: a Gloam pack comes up through the woods from the south.
        gloam("gloam_stalker", "gloam_claws", (0, 22), 15, "pack"), gloam("gloam_hound", "gloam_fangs", (1, 23), 14, "pack"),
        gloam("gloam_stalker", "gloam_claws", (25, 22), 15, "pack"), gloam("gloam_hound", "gloam_fangs", (24, 23), 14, "pack"),
        dict(gloam("gloam_harrier", "gloam_talons", (12, 23), 15, "pack"), HardOnly=True),
    ]
    for s in spawns:
        s.setdefault("Faction", ENEMY)
        s.setdefault("Ai", AGGRESSIVE)
    triggers = [
        "turn 1 => scene vault_start",
        f"enter aldric {cells(OUTER_PLINTH)} => terrain {cells(OUTER_SEAL)} #; scene vault_seal_outer; spawn risen",
        f"enter aldric {cells(INNER_PLINTH)} => terrain {cells(INNER_SEAL)} #; scene vault_seal_inner",
        "turn 5 enemy => spawn pack; scene vault_pack",
        "combat caddoc => scene vault_caddoc",
        "defeated caddoc => scene vault_caddoc_falls",
        "defeated keeper => scene vault_keeper",
        "chest 5,6 => give actor talisman",
        "chest 20,6 => gold 2000",
        f"enter rowan {ALTAR[0]},{ALTAR[1]} => scene vault_blade; give rowan lyras_blade",
    ]
    highlights = [
        f"{cells(OUTER_PLINTH)} => A Warden seal bars the hall. Only Aldric's sigil opens it: bring him to the plinth.",
        f"{cells(INNER_PLINTH)} => A second seal guards the altar room. Aldric opens this one too.",
        f"{ALTAR[0]},{ALTAR[1]} => The vault altar. Rowan must reach it.",
        "aldric => Aldric must survive.",
    ]
    slots = [(10, 21), (11, 21), (14, 21), (15, 21), (10, 22), (11, 22), (14, 22), (15, 22), (12, 22), (13, 22)]
    map_tres("vault", "Side Map: The Old Warden Vault", g.rows(), g.height_rows(), spawns, f"seize {ALTAR[0]},{ALTAR[1]}",
             ["die aldric"], triggers, slots, background="hill_road", highlights=highlights)
    finish("vault", {"Lighting": "night"})


def assert_connected_except_seals(g):
    """With the seals open, every floor cell is reachable from the deployment."""
    saved = [row[:] for row in g.cells]
    for (x0, y0, x1, y1) in (OUTER_SEAL, INNER_SEAL):
        g.rect(x0, y0, x1, y1, "#")
    assert_connected(g, (12, 21))
    g.cells = saved


if __name__ == "__main__":
    vault()
