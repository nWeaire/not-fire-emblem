using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class EquipmentTests
{
    private static StatBlock Bonus(int def = 0, int res = 0, int lck = 0, int mov = 0) =>
        new(HP: 0, Str: 0, Mag: 0, Dex: 0, Spd: 0, Lck: lck, Def: def, Res: res, Mov: mov);

    private static TestBattle WithGear(TestBattle b)
    {
        b.Db.Add(new ArmorDef("jerkin", "Jerkin", ArmorSlot.Body, ArmorClass.Light, Bonus(def: 2), 0, 500));
        b.Db.Add(new ArmorDef("plate", "Plate", ArmorSlot.Body, ArmorClass.Heavy, Bonus(def: 6), 6, 1600, Tier: 2));
        b.Db.Add(new ArmorDef("charm", "Charm", ArmorSlot.Accessory, ArmorClass.Light, Bonus(lck: 5), 0, 300));
        b.Db.Add(new ArmorDef("boots", "Boots", ArmorSlot.Accessory, ArmorClass.Light, Bonus(mov: 1), 0, 4000, Tier: 3));
        return b;
    }

    [Fact]
    public void ArmorAndAccessory_AddToEffectiveStats()
    {
        var s = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword").Build();
        var unit = s.Units[0].State;
        var before = s.Stats(s.Units[0]);
        unit.ArmorId = "jerkin";
        unit.AccessoryId = "boots";
        var after = s.Stats(s.Units[0]);
        Assert.Equal(before.Def + 2, after.Def);
        Assert.Equal(before.Mov + 1, after.Mov);
    }

    [Fact]
    public void EquipmentBonuses_CanExceedClassCaps()
    {
        var s = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword").Build();
        var unit = s.Units[0].State;
        unit.Gains = Bonus(lck: 100); // far past the Lck cap of 40
        unit.AccessoryId = "charm";
        Assert.Equal(45, s.Stats(s.Units[0]).Lck);
    }

    [Fact]
    public void ArmorWeight_SlowsAttackSpeedUnlessStrongEnough()
    {
        // merc: Spd 10, Str 10 (Str/5 = 2). Plate weight 6 + sword weight 0 -> penalty 4.
        var s = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[0].State.ArmorId = "plate";
        var f = CombatCalculator.Forecast(s, s.Units[0], s.Units[0].Pos, s.Units[1], new WeaponAttack("sword"));
        Assert.Equal(6, f.Attacker.AttackSpeed);
        Assert.Equal(10, f.Defender.AttackSpeed);
    }

    [Fact]
    public void ArmorDefence_ReducesIncomingDamage()
    {
        var s = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[1].State.ArmorId = "jerkin";
        var f = CombatCalculator.Forecast(s, s.Units[0], s.Units[0].Pos, s.Units[1], new WeaponAttack("sword"));
        Assert.Equal(8, f.Attacker.Damage); // 10 + 5 - (5 + 2)
    }

    [Fact]
    public void ClassArmorLimit_IsEnforced()
    {
        var b = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword");
        var s = b.Build();
        var unit = s.Units[0].State; // test classes default to Medium
        Assert.False(EquipmentService.EquipArmor(b.Db, unit, "plate").Ok);
        var (ok, replaced, _) = EquipmentService.EquipArmor(b.Db, unit, "jerkin");
        Assert.True(ok);
        Assert.Null(replaced);
        Assert.Equal("jerkin", unit.ArmorId);
        Assert.True(EquipmentService.EquipArmor(b.Db, unit, "charm").Ok);
        Assert.Equal("charm", unit.AccessoryId);
        Assert.Equal("jerkin", unit.ArmorId); // accessories use their own slot
    }

    [Fact]
    public void ClassChangeToLighterClass_RemovesArmorThatNoLongerFits()
    {
        var b = WithGear(new TestBattle("..")).Unit(Faction.Player, "merc", 0, 0, "sword");
        b.Db.Classes["heavy"] = b.Db.Class("merc") with { Id = "heavy", MaxArmor = ArmorClass.Heavy };
        var s = b.Build();
        var unit = s.Units[0].State;
        unit.ClassId = "heavy";
        Assert.True(EquipmentService.EquipArmor(b.Db, unit, "plate").Ok);
        unit.ClassId = "merc";
        Assert.Equal("plate", EquipmentService.RemoveInvalidArmor(b.Db, unit));
        Assert.Null(unit.ArmorId);
    }

    [Fact]
    public void PersonalWeapons_OnlyWorkForTheirOwner()
    {
        var b = new TestBattle("..");
        b.Db.Add(new WeaponDef("lyras_blade", "Lyra's Blade", WeaponType.Sword, 11, 95, 15, 4, 1, 1, false, Array.Empty<string>(), 0, 4, "lord"));
        var s = b.Lord(0, 0, "lyras_blade").Unit(Faction.Player, "merc", 1, 0, "lyras_blade").Build();
        Assert.True(CombatCalculator.CanWield(b.Db, s.Unit("u0").State, b.Db.Weapon("lyras_blade")));
        Assert.False(CombatCalculator.CanWield(b.Db, s.Unit("u1").State, b.Db.Weapon("lyras_blade")));
    }

    [Fact]
    public void Shops_OpenWithTheStory()
    {
        var db = WithGear(new TestBattle("..")).Db;
        db.Add(new ShopDef("village", "Village", "thornvale", 1, new[] { "sword", "jerkin" }));
        db.Add(new ShopDef("keep", "Keep", "thornvale", 5, new[] { "plate", "sword" }));
        Assert.Empty(EquipmentService.OpenShops(db, 0));
        Assert.Equal(new[] { "sword", "jerkin" }, EquipmentService.PurchasableItems(db, 3));
        Assert.Equal(new[] { "sword", "jerkin", "plate" }, EquipmentService.PurchasableItems(db, 5));
    }

    [Fact]
    public void Validation_CatchesBadShopItemsAndUnknownPersonalOwners()
    {
        var db = TestContent.Create();
        db.Add(new ShopDef("bad", "Bad", "x", 1, new[] { "no_such_item" }));
        db.Add(new WeaponDef("orphan", "Orphan", WeaponType.Sword, 1, 1, 0, 0, 1, 1, false, Array.Empty<string>(), 0, 4, "nobody"));
        var errors = db.Validate();
        Assert.Contains(errors, e => e.Contains("no_such_item"));
        Assert.Contains(errors, e => e.Contains("nobody"));
    }

    [Fact]
    public void GenericSpawns_CanWearArmor()
    {
        var b = WithGear(new TestBattle(".."));
        var map = new MapDef("m", "M", new[] { ".." }, Array.Empty<string>(), new[]
        {
            new SpawnDef(null, "merc", "Guard", 1, new[] { "sword" }, Faction.Enemy, new NotFireEmblem.Grid.GridPos(0, 0), Armor: "jerkin"),
        });
        var s = BattleSetup.Create(b.Db, map, 1);
        Assert.Equal("jerkin", s.Units[0].State.ArmorId);
        Assert.Empty(map.Validate(b.Db));
    }

    [Fact]
    public void Bosses_GetExtraResForTheirLevel()
    {
        var b = new TestBattle("..");
        var map = new MapDef("m", "M", new[] { ".." }, Array.Empty<string>(), new[]
        {
            new SpawnDef(null, "merc", "Grunt", 15, new[] { "sword" }, Faction.Enemy, new NotFireEmblem.Grid.GridPos(0, 0)),
            new SpawnDef(null, "merc", "Boss", 15, new[] { "sword" }, Faction.Enemy, new NotFireEmblem.Grid.GridPos(1, 0), IsBoss: true),
        });
        var s = BattleSetup.Create(b.Db, map, 1);
        Assert.Equal(10, BattleSetup.BossResBonus(15));
        Assert.Equal(s.Stats(s.Units[0]).Res + 10, s.Stats(s.Units[1]).Res);
        Assert.Equal(s.Stats(s.Units[0]).Def, s.Stats(s.Units[1]).Def);
    }
}
