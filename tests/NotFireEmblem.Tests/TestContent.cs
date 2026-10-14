using NotFireEmblem.Battle;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

/// <summary>Minimal content database and battle builders for tests.</summary>
public static class TestContent
{
    public static StatBlock Stats(int hp, int str, int mag, int dex, int spd, int lck, int def, int res, int mov) =>
        new(hp, str, mag, dex, spd, lck, def, res, mov);

    private static Dictionary<MovementType, int> Costs(int inf, int arm, int cav, int fly) =>
        new() { [MovementType.Infantry] = inf, [MovementType.Armored] = arm, [MovementType.Cavalry] = cav, [MovementType.Flying] = fly };

    private static ClassDef Class(string id, MovementType mt, StatBlock bases, params WeaponType[] usable) =>
        new(id, id, bases, default, Stats(80, 40, 40, 40, 40, 40, 40, 40, 15), mt, usable, Array.Empty<string>(), false);

    private static WeaponDef Weapon(string id, WeaponType type, int mt, int hit, int crit, int wt, int min = 1, int max = 1, bool magic = false, params string[] eff) =>
        new(id, id, type, mt, hit, crit, wt, min, max, magic, eff, 0);

    public static ContentDatabase Create()
    {
        var db = new ContentDatabase();
        db.Add(new TerrainDef("plain", "Plain", '.', Costs(1, 1, 1, 1), 0, 0, 0, false, "#00ff00"));
        db.Add(new TerrainDef("forest", "Forest", 'F', Costs(2, 2, 3, 1), 20, 1, 0, false, "#008000"));
        db.Add(new TerrainDef("wall", "Wall", '#', Costs(0, 0, 0, 0), 0, 0, 0, true, "#808080"));
        db.Add(new TerrainDef("mountain", "Mountain", 'M', Costs(3, 0, 0, 1), 30, 2, 0, false, "#806040"));
        db.Add(new TerrainDef("water", "Water", '~', Costs(0, 0, 0, 1), 0, 0, 0, false, "#0000ff"));

        // A "plain" statline: 20 HP, 10 Str/Mag, 10 Dex, 10 Spd, 0 Lck, 5 Def/Res, 5 Mov.
        var basic = Stats(20, 10, 10, 10, 10, 0, 5, 5, 5);
        db.Add(Class("merc", MovementType.Infantry, basic, WeaponType.Sword));
        db.Add(Class("soldier", MovementType.Infantry, basic, WeaponType.Lance));
        db.Add(Class("fighter", MovementType.Infantry, basic, WeaponType.Axe));
        db.Add(Class("archer", MovementType.Infantry, basic, WeaponType.Bow));
        db.Add(Class("mage", MovementType.Infantry, basic, WeaponType.BlackMagic));
        db.Add(Class("cav", MovementType.Cavalry, basic with { Mov = 7 }, WeaponType.Sword, WeaponType.Lance));
        db.Add(Class("flier", MovementType.Flying, basic with { Mov = 7 }, WeaponType.Lance));
        db.Add(Class("knight", MovementType.Armored, basic with { Mov = 4 }, WeaponType.Lance));

        db.Add(Weapon("sword", WeaponType.Sword, 5, 100, 0, 0));
        db.Add(Weapon("lance", WeaponType.Lance, 5, 100, 0, 0));
        db.Add(Weapon("axe", WeaponType.Axe, 5, 100, 0, 0));
        db.Add(Weapon("javelin", WeaponType.Lance, 5, 100, 0, 0, 1, 2));
        db.Add(Weapon("bow", WeaponType.Bow, 5, 100, 0, 0, 2, 2, false, "Flying"));
        db.Add(Weapon("fire", WeaponType.BlackMagic, 5, 100, 0, 0, 1, 2, true));
        db.Add(Weapon("heavy_sword", WeaponType.Sword, 5, 100, 0, 10));
        db.Add(Weapon("killer", WeaponType.Sword, 5, 100, 30, 0));
        db.Add(Weapon("steel_sword", WeaponType.Sword, 8, 100, 0, 0) with { Tier = 2, Price = 1000 });
        db.Add(Weapon("iron_sword", WeaponType.Sword, 5, 100, 0, 0) with { Price = 400 });

        // M4/M5 content: a beast class, a crest-bearer class, a healer, items, arts, spells, scenes.
        db.Add(Class("beast", MovementType.Infantry, basic, WeaponType.Fist) with { Traits = new[] { "Beast" } });
        db.Add(Class("healer", MovementType.Infantry, basic, WeaponType.WhiteMagic) with { Spells = new[] { "heal" } });
        db.Add(Class("hero", MovementType.Infantry, basic, WeaponType.Sword) with { Tier = 2, IsPromoted = true });
        db.Add(Class("merc_p", MovementType.Infantry, basic, WeaponType.Sword) with { PromotesTo = new[] { "hero" } });
        db.Add(Weapon("claws", WeaponType.Fist, 5, 100, 0, 0));
        db.Add(new ItemDef("vulnerary", "Vulnerary", ItemKind.Heal, 10, Stat.HP, 300));
        db.Add(new ItemDef("energy_drop", "Energy Drop", ItemKind.StatBoost, 2, Stat.Str, 2500));
        db.Add(new ItemDef("seal", "Knight Seal", ItemKind.Seal, 0, Stat.HP, 2500, Tier: 2));
        db.Add(new ArtDef("smash", "Smash", WeaponType.Sword, 3, -10, 0, 0, 2));
        db.Add(new ArtDef("quiet_cut", "Quiet Cut", WeaponType.Sword, 0, 0, 0, 0, 1, NoCounter: true));
        db.Add(new ArtDef("long_reach", "Long Reach", WeaponType.Sword, 0, 0, 0, 1, 1));
        db.Add(new ArtDef("beastbane", "Beastbane", WeaponType.Sword, 0, 0, 0, 0, 1, EffectiveAgainst: new[] { "Beast" }));
        db.Add(new SpellDef("heal", "Heal", 10, 1, 1, 3));
        db.Add(new Dialogue.DialogueScene("scene_a", new Dialogue.DialogueLine[]
        {
            new Dialogue.SpeakLine("lord", "neutral", "Hello."),
            new Dialogue.FlagLine("met"),
        }));
        db.Add(new Dialogue.DialogueScene("support_lord_partner_C", new Dialogue.DialogueLine[] { new Dialogue.SpeakLine("lord", "smile", "Hi.") }));
        return db;
    }
}

/// <summary>Builds a small battle from ASCII terrain rows and explicitly placed units.</summary>
public sealed class TestBattle
{
    private readonly List<SpawnDef> _spawns = new();
    private readonly string[] _rows;
    private string[] _heights = Array.Empty<string>();

    public ContentDatabase Db { get; } = TestContent.Create();

    public TestBattle(params string[] rows)
    {
        _rows = rows;
    }

    public TestBattle Heights(params string[] heights)
    {
        _heights = heights;
        return this;
    }

    public TestBattle Unit(Faction faction, string classId, int x, int y, params string[] items)
    {
        _spawns.Add(new SpawnDef(null, classId, $"{faction}{_spawns.Count}", 1, items, faction, new GridPos(x, y)));
        return this;
    }

    /// <summary>Sets the AI profile of the most recently added unit.</summary>
    public TestBattle WithAi(AiProfile profile)
    {
        _spawns[^1] = _spawns[^1] with { Ai = profile };
        return this;
    }

    public TestBattle Lord(int x, int y, params string[] items)
    {
        Db.Add(new UnitDef("lord", "Lord", "merc", 1, default, default, items, true));
        _spawns.Add(new SpawnDef("lord", null, null, 1, Array.Empty<string>(), Faction.Player, new GridPos(x, y)));
        return this;
    }

    private string? _victory;
    private readonly List<string> _defeat = new();
    private readonly List<string> _triggers = new();
    private bool _fog;
    private BattleOptions? _options;

    /// <summary>Changes the most recently added spawn (tags, groups, Dominion...).</summary>
    public TestBattle With(Func<SpawnDef, SpawnDef> change)
    {
        _spawns[^1] = change(_spawns[^1]);
        return this;
    }

    /// <summary>Adds a named roster-style unit (so it gains EXP and supports).</summary>
    public TestBattle Named(string id, Faction faction, string classId, int x, int y, params string[] items)
    {
        Db.Add(new UnitDef(id, id, classId, 1, default, default, items, false));
        _spawns.Add(new SpawnDef(id, null, null, 1, Array.Empty<string>(), faction, new GridPos(x, y)));
        return this;
    }

    public TestBattle Victory(string victory) { _victory = victory; return this; }
    public TestBattle Defeat(params string[] lines) { _defeat.AddRange(lines); return this; }
    public TestBattle Triggers(params string[] lines) { _triggers.AddRange(lines); return this; }
    public TestBattle Fog() { _fog = true; return this; }
    public TestBattle Options(BattleOptions options) { _options = options; return this; }

    public MapDef Map() => new("test", "Test", _rows, _heights, _spawns,
        MapScript.ParseObjectives(_victory, _defeat), MapScript.ParseTriggers(_triggers), _fog);

    public BattleState Build(ulong seed = 1) => BattleSetup.Create(Db, Map(), seed, null, _options);

    public BattleState Build(IRng rng)
    {
        var map = Map();
        var built = BattleSetup.Create(Db, map, 1, null, _options);
        var s = new BattleState(Db, map, built.Grid, rng, built.Options);
        foreach (var u in built.Units)
            s.AddUnit(u);
        s.NextUnitIndex = built.NextUnitIndex;
        return s;
    }
}

/// <summary>An IRng that returns a fixed sequence of values.</summary>
public sealed class ScriptedRng : IRng
{
    private readonly Queue<int> _values;

    public ScriptedRng(params int[] values)
    {
        _values = new Queue<int>(values);
    }

    public int Next(int n) => _values.Count > 0 ? _values.Dequeue() % n : 0;
}
