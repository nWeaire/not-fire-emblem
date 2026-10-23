"""Writes the data for Chapters 4-6 (docs/story/CHAPTERS.md): maps, new terrain, the Knight class, Dorran and Sable.

The .tres files it writes are canonical afterwards (edit them directly); rerunning this overwrites them.
    python tools/maps/act1_ch4_6.py
"""
from pathlib import Path

DATA = Path(__file__).resolve().parents[2] / "data"
PLAYER, ENEMY, ALLY = 0, 1, 2
AGGRESSIVE, STATIONARY, GUARD, BOSS = 0, 1, 2, 3


# ------------------------------------------------------------------ writers

def stats(sid, **kv):
    body = "\n".join(f"{k} = {v}" for k, v in kv.items())
    return f'[sub_resource type="Resource" id="{sid}"]\nscript = ExtResource("2_stats")\n{body}\n'


def write(path, text):
    path.write_text(text, encoding="utf-8", newline="\n")
    print("wrote", path.relative_to(DATA.parent))


def terrain(tid, name, symbol, inf, arm, cav, fly, avoid, defense, color, heal=0):
    write(DATA / "terrain" / f"{tid}.tres", f'''[gd_resource type="Resource" script_class="TerrainDefinition" load_steps=2 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/TerrainDefinition.cs" id="1_terrain"]

[resource]
script = ExtResource("1_terrain")
Id = "{tid}"
Name = "{name}"
Symbol = "{symbol}"
CostInfantry = {inf}
CostArmored = {arm}
CostCavalry = {cav}
CostFlying = {fly}
Avoid = {avoid}
Def = {defense}
HealPercent = {heal}
BlocksSight = false
Color = {color}
''')


def unit(uid, name, cls, level, bases, growths, inventory, armor):
    inv = ", ".join(f'"{i}"' for i in inventory)
    write(DATA / "units" / f"{uid}.tres", f'''[gd_resource type="Resource" script_class="UnitDefinition" load_steps=5 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/UnitDefinition.cs" id="1_unit"]
[ext_resource type="Script" path="res://src/Units/Resources/StatsDefinition.cs" id="2_stats"]

{stats("StatsDefinition_1", **bases)}
{stats("StatsDefinition_2", **growths)}
[resource]
script = ExtResource("1_unit")
Id = "{uid}"
Name = "{name}"
ClassId = "{cls}"
Level = {level}
Bases = SubResource("StatsDefinition_1")
Growths = SubResource("StatsDefinition_2")
Inventory = PackedStringArray({inv})
IsLord = false
Armor = "{armor}"
''')


def spawn_block(i, s):
    lines = [f'[sub_resource type="Resource" id="SpawnDefinition_{i}"]', 'script = ExtResource("2_spawn")']
    for key in ("UnitId", "ClassId", "Name", "Tag", "Group", "DominatedBy", "SpriteId", "Armor", "Ignores"):
        if s.get(key):
            lines.append(f'{key} = "{s[key]}"')
    lines.append(f"Level = {s.get('Level', 1)}")
    if s.get("Inventory"):
        lines.append("Inventory = PackedStringArray(" + ", ".join(f'"{x}"' for x in s["Inventory"]) + ")")
    lines.append(f"Faction = {s.get('Faction', ENEMY)}")
    x, y = s["Pos"]
    lines.append(f"Position = Vector2i({x}, {y})")
    lines.append(f"Ai = {s.get('Ai', AGGRESSIVE)}")
    if s.get("IsBoss"):
        lines.append("IsBoss = true")
    if s.get("CrestBearer"):
        lines.append("CrestBearer = true")
    if s.get("HardOnly"):
        lines.append("HardOnly = true")
    if s.get("Leash"):
        lines.append(f"Leash = {s['Leash']}")
    if s.get("NonLethal"):
        lines.append("NonLethal = true")
    if s.get("DominionRadius"):
        lines.append(f"DominionRadius = {s['DominionRadius']}")
    return "\n".join(lines) + "\n"


def map_tres(mid, name, rows, heights, spawns, victory, defeat, triggers, slots, background="", fog=False,
             highlights=()):
    w = len(rows[0])
    assert all(len(r) == w for r in rows), [len(r) for r in rows]
    assert len(heights) == len(rows) and all(len(h) == w for h in heights)
    for s in spawns:
        x, y = s["Pos"]
        assert 0 <= x < w and 0 <= y < len(rows), (mid, s)
        assert rows[y][x] not in "W~M", (mid, "spawn on impassable", s)
    blocks = "\n".join(spawn_block(i + 1, s) for i, s in enumerate(spawns))
    refs = ", ".join(f'SubResource("SpawnDefinition_{i + 1}")' for i in range(len(spawns)))
    q = lambda xs: ", ".join(f'"{x}"' for x in xs)
    marks = f"Highlights = PackedStringArray({q(highlights)})\n" if highlights else ""
    write(DATA / "maps" / f"{mid}.tres", f'''[gd_resource type="Resource" script_class="MapDefinition" load_steps={len(spawns) + 3} format=3]

[ext_resource type="Script" path="res://src/Battle/Resources/MapDefinition.cs" id="1_map"]
[ext_resource type="Script" path="res://src/Battle/Resources/SpawnDefinition.cs" id="2_spawn"]

{blocks}
[resource]
script = ExtResource("1_map")
Id = "{mid}"
Name = "{name}"
TerrainRows = PackedStringArray({q(rows)})
HeightRows = PackedStringArray({q(heights)})
Spawns = Array[ExtResource("2_spawn")]([{refs}])
Victory = "{victory}"
Defeat = PackedStringArray({q(defeat)})
Triggers = PackedStringArray({q(triggers)})
{marks}Fog = {"true" if fog else "false"}
DeploySlots = "{" ".join(f"{x},{y}" for x, y in slots)}"
Background = "{background}"
''')


# ------------------------------------------------------------------ content

def classes():
    write(DATA / "classes" / "knight.tres", f'''[gd_resource type="Resource" script_class="ClassDefinition" load_steps=6 format=3]

[ext_resource type="Script" path="res://src/Units/Resources/ClassDefinition.cs" id="1_class"]
[ext_resource type="Script" path="res://src/Units/Resources/StatsDefinition.cs" id="2_stats"]

{stats("StatsDefinition_1", HP=20, Str=7, Dex=5, Spd=2, Lck=1, Def=10, Res=1, Mov=4)}
{stats("StatsDefinition_2", HP=15, Str=5, Def=15)}
{stats("StatsDefinition_3", HP=60, Str=30, Mag=30, Dex=30, Spd=30, Lck=30, Def=30, Res=30, Mov=10)}
[resource]
script = ExtResource("1_class")
Id = "knight"
Name = "Knight"
Bases = SubResource("StatsDefinition_1")
Growths = SubResource("StatsDefinition_2")
Caps = SubResource("StatsDefinition_3")
MovementType = 1
UsableWeapons = PackedStringArray("Lance")
PromotesTo = PackedStringArray()
IsPromoted = false
MaxArmor = 2
''')


def terrains():
    terrain("shallows", "Shallows", "s", 2, 3, 3, 1, 0, 0, "Color(0.36, 0.55, 0.50, 1)")
    terrain("reeds", "Reeds", "r", 2, 3, 3, 1, 20, 0, "Color(0.55, 0.58, 0.30, 1)")
    terrain("stilt", "Stilt House", "h", 1, 1, 1, 1, 10, 1, "Color(0.50, 0.38, 0.24, 1)", heal=10)


def units():
    unit("dorran", "Dorran", "knight", 8, dict(HP=4, Str=2, Dex=2, Spd=1, Lck=2, Def=2, Res=1),
         dict(HP=60, Str=40, Mag=0, Dex=35, Spd=20, Lck=25, Def=45, Res=15), ["steel_lance", "javelin"], "iron_plate")
    unit("sable", "Sable", "myrmidon", 6, dict(HP=1, Str=1, Dex=3, Spd=4, Lck=2, Def=0, Res=1),
         dict(HP=50, Str=40, Mag=5, Dex=60, Spd=65, Lck=40, Def=20, Res=25), ["iron_sword", "killing_edge"],
         "leather_jerkin")


def ch4():
    rows = ["MMMFFPP==PPFFMMM", "MMFFPPP==PPPFFMM", "MFFWWWW==WWWWFFM", "FFP,,PP==PPP,,FF",
            "FPP,,PP==PPP,,PF", "PPPFFPP==PFFPPPP", "PP,FFP==PPFF,PPP", "P,,PPP==PPPP,,PP",
            "PPFF,PP==P,,FFPP", "PFFF,,P==PP,,FFP", "PPF,,PP==PPP,PPP", "PP,,PPP==PFFF,PP",
            "FFPPP,P==PFFT,PP", "FFP,,PP==PPPPTTF", "MFFPPPP==PPPPPFM", "MMFPPPP==PPPPFMM"]
    heights = ["2221110000011222", "2211000000001122", "2110000000000112", "1100000000000011",
               "1000000000000001", "1000000000000001", "0000000000000000", "0000000000000000",
               "0000000000000000", "1000000000000001", "1000000000000001", "0000000000000000",
               "1100000000000000", "1100000000000011", "2110000000000112", "2211000000001122"]
    soldier = lambda pos, lvl=4, group="": dict(ClassId="soldier", Name="Dain Hall Soldier", SpriteId="enemy_soldier",
                                               Level=lvl, Inventory=["iron_lance"], Pos=pos, Group=group,
                                               Armor="padded_gambeson")
    cavalier = lambda pos, group="": dict(ClassId="cavalier", Name="Dain Hall Cavalier", SpriteId="enemy_cavalier",
                                          Level=5, Inventory=["iron_lance"], Pos=pos, Group=group)
    archer = lambda pos, group="": dict(ClassId="archer", Name="Dain Hall Archer", SpriteId="enemy_archer", Level=4,
                                        Inventory=["iron_bow"], Pos=pos, Group=group)
    spawns = [
        # Elise starts just out of the first wave's reach; the soldiers wait beyond the toll gate.
        dict(UnitId="elise", Faction=ALLY, Level=4, Pos=(8, 10), Ai=STATIONARY),
        soldier((7, 1)), soldier((8, 2)), soldier((10, 1), 5), archer((5, 1)),
        cavalier((8, 0)), soldier((6, 1), 5),
        soldier((7, 0), 4, "north2"), soldier((8, 0), 4, "north2"),
        cavalier((8, 0), "north3"), archer((7, 0), "north3"),
        soldier((7, 0), 5, "north4"), soldier((8, 0), 5, "north4"),
        soldier((7, 0), 5, "north5"), dict(archer((8, 0), "north5"), HardOnly=True),
    ]
    triggers = ["turn 1 => scene ch04_start",
                "talk rowan elise => scene ch04_elise; join elise",
                "turn 2 => spawn north2", "turn 3 => spawn north3; scene ch04_soldiers",
                "turn 4 => spawn north4", "turn 5 => spawn north5",
                "enter player 12,12-14,13 => scene ch04_waystation"]
    map_tres("ch4", "Chapter 4: The Scholar of Calder", rows, heights, spawns,
             "escape 6,15-9,15 elise rowan", ["die elise"], triggers,
             [(11, 10), (12, 10), (11, 11), (12, 11), (13, 11), (12, 9)], background="hill_road")


def ch5():
    rows = ["PPWWWWWWWWWWWPPFFF", "PP######T###WPPPFF", "PP#T########WPPFFF", "PP##########WPPPPP",
            "PP##########WPPFPP", "PP##########WP,PPP", "PP########T#WPPPPP", "PP##########WPPPPP",
            "PP##########WPP,PP", "PP##########WPPFPP", "PP#T########WPPPPP", "PP##########PPPFFF",
            "PP######T###WPPPFF", "PPWWWWWWWWWWWPPFFF"]
    heights = ["001111111111100000", "000000000000100000", "000000000000100000", "000000000000100000",
               "000000000000100000", "000000000000100000", "000000000000100000", "000000000000100000",
               "000000000000100000", "000000000000100000", "000000000000100000", "000000000000000000",
               "000000000000100000", "001111111111100000"]
    # The gate is wall cells 12,6-12,7 until the T5 trigger turns them to courtyard; row 11 is a breach.
    hound = lambda pos, group="": dict(ClassId="gloam_hound", SpriteId="enemy_gloam_hound", Level=4, Group=group,
                                       Inventory=["gloam_fangs"], Pos=pos, DominatedBy="hollin")
    brute = lambda pos, group="": dict(ClassId="gloam_brute", SpriteId="enemy_gloam_brute", Level=4, Group=group,
                                       Inventory=["gloam_claws"], Pos=pos, DominatedBy="hollin")
    stalker = lambda pos, group="": dict(ClassId="gloam_stalker", SpriteId="enemy_gloam_stalker", Level=4,
                                         Group=group, Inventory=["bone_spikes"], Pos=pos, DominatedBy="hollin")
    spawns = [
        dict(UnitId="dorran", Faction=ALLY, Level=8, Pos=(11, 7), Ai=STATIONARY),
        hound((14, 3)), hound((14, 9)), stalker((15, 5)), stalker((15, 10)), brute((15, 7)),
        dict(ClassId="gloam_brute", Name="Gloam Alpha", Tag="alpha", SpriteId="enemy_gloam_alpha", Level=8,
             Inventory=["gloam_claws"], Pos=(14, 7), Ai=BOSS, IsBoss=True, DominatedBy="hollin"),
        dict(ClassId="binder", Name="Binder Hollin", Tag="hollin", SpriteId="enemy_binder", Level=6,
             Inventory=["shadow"], Pos=(17, 1), Ai=STATIONARY, CrestBearer=True),
        hound((17, 4), "wave2"), hound((17, 9), "wave2"), stalker((17, 6), "wave2"),
        brute((17, 3), "wave3"), hound((17, 7), "wave3"), hound((17, 10), "wave3"),
        hound((17, 2), "wave4"), stalker((17, 8), "wave4"), brute((17, 11), "wave4"),
        hound((17, 5), "wave6"), hound((17, 9), "wave6"), dict(brute((17, 7), "wave6"), HardOnly=True),
    ]
    triggers = ["turn 1 => scene ch05_dorran; join dorran",
                "turn 2 enemy => spawn wave2", "turn 3 enemy => spawn wave3", "turn 4 enemy => spawn wave4",
                "turn 5 => terrain 12,6-12,7 #; scene ch05_gate", "turn 6 enemy => spawn wave6",
                "combat alpha => scene ch05_alpha",
                "defeated hollin => scene ch05_hollin; gold 500; win"]
    map_tres("ch5", "Chapter 5: Ferrow's Keep", rows, heights, spawns, "survive 8", ["die dorran"], triggers,
             [(8, 5), (9, 5), (8, 7), (9, 7), (7, 6), (9, 6), (8, 8)], background="ferrows_keep")


def ch6():
    rows = ["~~ssrrPPsshh~~ssrrPP", "~ssrrPPsss~h~ssrrPPh", "ssrPPPsBs~~~ssrPPhhP", "srPPPsss~ss~srPPPPPP",
            "rPPhPss~~ssrrPPrrPPP", "PPPPsss~rPPPPss~~rrP", "PPrssr~~PPhPPsss~~rP", "PrrssBBsPPPPrrss~srr",
            "PrsssrrsssPPPrsss~sP", "PPssr~~rrrsPPhPPsBsr", "PPPsr~~ssPPPPPPrsss~", "PPPrss~sssrrPPPrrss~",
            "PPPPssBsrrPPhPPrrs~~", "PPPPrssssrPPPPPrss~~"]
    heights = ["0" * 20] * 14
    bandit = lambda pos, lvl=5, group="": dict(ClassId="fighter", Name="Greyfen Bandit", SpriteId="enemy_fighter",
                                              Level=lvl, Inventory=["iron_axe"], Pos=pos, Group=group,
                                              DominatedBy="rusk", Armor="leather_jerkin")
    archer = lambda pos, lvl=5: dict(ClassId="archer", Name="Greyfen Archer", SpriteId="enemy_archer", Level=lvl,
                                     Inventory=["iron_bow"], Pos=pos, DominatedBy="rusk")
    spawns = [
        dict(UnitId="sable", Faction=ENEMY, Level=6, Pos=(10, 6), Ai=STATIONARY),
        dict(ClassId="fighter", Name="Rusk", Tag="rusk", SpriteId="boss_rusk", Level=9, Armor="chainmail",
             Inventory=["steel_axe", "hand_axe"], Pos=(17, 2), Ai=BOSS, IsBoss=True, CrestBearer=True),
        bandit((6, 4)), bandit((9, 3)), bandit((12, 7)), bandit((14, 4), 6), bandit((13, 9)),
        bandit((16, 4), 6), bandit((18, 4), 6), archer((2, 3)), archer((13, 3)), archer((15, 1), 6),
        dict(ClassId="myrmidon", Name="Greyfen Sellsword", SpriteId="enemy_myrmidon", Level=6,
             Inventory=["iron_sword"], Pos=(19, 6), DominatedBy="rusk"),
        dict(bandit((19, 3), 6), HardOnly=True),
    ]
    triggers = ["talk kai sable => scene ch06_sable; join sable",
                "turn 1 => scene ch06_start",
                "turn 5 => scene ch06_scouts",
                "visit 3,4 => scene ch06_house; give actor elixir",
                "combat rusk => scene ch06_boss",
                "defeated rusk => scene ch06_rusk"]
    map_tres("ch6", "Chapter 6: The Bandit King of Greyfen", rows, heights, spawns, "boss rusk", [], triggers,
             [(1, 10), (2, 10), (1, 11), (2, 11), (1, 12), (2, 12), (1, 13), (2, 13)], background="greyfen")


if __name__ == "__main__":
    classes()
    terrains()
    units()
    ch4()
    ch5()
    ch6()
