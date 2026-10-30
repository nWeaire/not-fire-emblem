using NotFireEmblem.Battle;
using NotFireEmblem.Campaign;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

/// <summary>M5: EXP and level-ups, arts, healing, items, trade, crest skills and supports.</summary>
public class ProgressionTests
{
    private static (BattleState s, TurnManager tm) Start(TestBattle builder)
    {
        var s = builder.Build();
        var tm = new TurnManager(s);
        tm.StartBattle();
        return (s, tm);
    }

    // ------------------------------------------------------------------ EXP and level-ups

    [Fact]
    public void CombatExp_FollowsTheFe7StyleFormula()
    {
        var db = TestContent.Create();
        UnitState Unit(string cls, int level) => UnitState.FromDef(new UnitDef("x", "x", cls, level, default, default, Array.Empty<string>(), false));
        var me = Unit("merc", 1);
        Assert.Equal(10, ExpCalculator.CombatExp(db, me, Unit("merc", 1), dealtDamage: true, killed: false, foeIsBoss: false));
        Assert.Equal(1, ExpCalculator.CombatExp(db, me, Unit("merc", 1), dealtDamage: false, killed: false, foeIsBoss: false));
        Assert.Equal(30, ExpCalculator.CombatExp(db, me, Unit("merc", 1), dealtDamage: true, killed: true, foeIsBoss: false));
        Assert.Equal(70, ExpCalculator.CombatExp(db, me, Unit("merc", 1), dealtDamage: true, killed: true, foeIsBoss: true));
        Assert.Equal(12, ExpCalculator.CombatExp(db, me, Unit("merc", 6), dealtDamage: true, killed: false, foeIsBoss: false));
        Assert.Equal(13, ExpCalculator.CombatExp(db, me, Unit("hero", 1), dealtDamage: true, killed: false, foeIsBoss: false)); // promoted: +10 levels
        Assert.Equal(0, ExpCalculator.CombatExp(db, Unit("merc", 10), me, true, true, false)); // at the cap (10)
    }

    [Fact]
    public void RaiseToAverage_GivesTheExpectedGainsWithoutDice()
    {
        var db = TestContent.Create();
        var growths = default(StatBlock) with { HP = 85, Str = 50 };
        var unit = UnitState.FromDef(new UnitDef("x", "x", "merc", 1, default, growths, Array.Empty<string>(), false));
        LevelUpService.RaiseToAverage(db, unit, 5);
        Assert.Equal(5, unit.Level);
        Assert.Equal(3, unit.Gains.HP);  // 85% × 4 levels = 3.4
        Assert.Equal(2, unit.Gains.Str); // 50% × 4 levels = 2
        Assert.Equal(0, unit.Gains.Def);
        LevelUpService.RaiseToAverage(db, unit, 3); // never lowers
        Assert.Equal(5, unit.Level);
    }

    [Fact]
    public void LevelUp_RollsEachStatAgainstGrowths()
    {
        var db = TestContent.Create();
        var def = new UnitDef("x", "x", "merc", 1, default, new StatBlock(0, 100, 0, 0, 50, 0, 0, 0, 0), Array.Empty<string>(), false);
        var unit = UnitState.FromDef(def);
        unit.Level = 2;
        unit.Exp = 60;
        var levels = LevelUpService.AddExp(db, unit, 50, new ScriptedRng(99, 99, 99, 99, 10, 99, 99, 99));
        var level = Assert.Single(levels);
        Assert.Equal(3, level.Level);
        Assert.Equal(3, unit.Level);
        Assert.Equal(10, unit.Exp);
        Assert.Equal(1, level.Gains.Str);
        Assert.Equal(1, level.Gains.Spd);
        Assert.Equal(0, level.Gains.HP);
        Assert.Equal(level.Gains, unit.Gains);
    }

    [Fact]
    public void LevelUp_RaisesMaxHp_AtLeastEveryOtherLevel()
    {
        var db = TestContent.Create();
        var def = new UnitDef("x", "x", "merc", 1, default, new StatBlock(0, 100, 0, 0, 0, 0, 0, 0, 0), Array.Empty<string>(), false);
        var unit = UnitState.FromDef(def);
        var rng = new ScriptedRng(Enumerable.Repeat(99, 40).ToArray());
        var levels = LevelUpService.AddExp(db, unit, 400, rng);
        Assert.Equal(new[] { 1, 0, 1, 0 }, levels.Select(l => l.Gains.HP)); // levels 2 and 4 rise even though every roll missed
        unit.Level = 1;
        LevelUpService.RaiseToAverage(db, unit, 9);
        Assert.True(unit.Gains.HP >= 2 + 4);
    }

    [Fact]
    public void LevelUp_IsNeverEmpty()
    {
        var db = TestContent.Create();
        var def = new UnitDef("x", "x", "merc", 1, default, default, Array.Empty<string>(), false);
        var unit = UnitState.FromDef(def);
        var level = Assert.Single(LevelUpService.AddExp(db, unit, 100, new ScriptedRng(99, 99, 99, 99, 99, 99, 99, 99, 99)));
        Assert.Equal(1, StatBlock.All.Sum(stat => level.Gains[stat]));
    }

    [Fact]
    public void LevelUp_DoesNotHeal()
    {
        var (s, _) = Start(new TestBattle(".....").Lord(0, 0, "sword").Unit(Faction.Enemy, "merc", 4, 0));
        var lord = s.Units.First(u => u.IsLord);
        lord.CurrentHp = 5;
        BattleProgress.AwardExp(s, lord, 100, new List<BattleEvent>());
        Assert.Equal(2, lord.State.Level);
        Assert.Equal(5, lord.CurrentHp);
    }

    [Fact]
    public void Combat_GivesExpToRosterUnitsOnly()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Unit(Faction.Enemy, "merc", 4, 0));
        var r = tm.Submit(new ActCommand("u0", new GridPos(0, 0), new AttackAction("u1", new WeaponAttack("sword"))));
        var exp = r.Events.OfType<ExpGained>().Single();
        Assert.Equal("u0", exp.UnitId);
        Assert.Equal(10, exp.Amount);
        Assert.Equal(10, s.Unit("u0").State.Exp);
    }

    // ------------------------------------------------------------------ arts

    [Fact]
    public void Arts_ModifyTheWeapon_UseUpUses_AndCanPreventCounters()
    {
        var b = new TestBattle(".....").Named("hero", Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword").Unit(Faction.Enemy, "beast", 4, 0, "claws");
        b.Db.Add(b.Db.Unit("hero") with { Arts = new[] { "smash", "quiet_cut", "long_reach", "beastbane" } });
        var (s, tm) = Start(b);
        var hero = s.Unit("u0");
        Assert.Equal(2, hero.UsesLeft["smash"]);

        var smash = CombatCalculator.Forecast(s, hero, hero.Pos, s.Unit("u1"), new ArtAttack("smash", "sword"));
        Assert.Equal(13, smash.Attacker.Damage);
        Assert.Equal("Smash", smash.Attacker.WeaponName);
        var quiet = CombatCalculator.Forecast(s, hero, hero.Pos, s.Unit("u1"), new ArtAttack("quiet_cut", "sword"));
        Assert.False(quiet.Defender.CanStrike);
        Assert.True(CombatCalculator.InRange(s.Db, new ArtAttack("long_reach", "sword"), 2));
        var bane = CombatCalculator.Forecast(s, hero, new GridPos(3, 0), s.Unit("u2"), new ArtAttack("beastbane", "sword"));
        Assert.True(bane.Attacker.Effective);
        Assert.Equal(20, bane.Attacker.Damage); // 10 Str + 5×3 Mt - 5 Def

        Assert.True(tm.Submit(new ActCommand("u0", hero.Pos, new AttackAction("u1", new ArtAttack("smash", "sword")))).Ok);
        Assert.Equal(1, hero.UsesLeft["smash"]);
    }

    [Fact]
    public void BraveArt_StrikesTwiceFirst_AndPierceArt_IgnoresHalfOfAnyDef()
    {
        var b = new TestBattle(".....").Named("hero", Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword");
        b.Db.Add(b.Db.Unit("hero") with { Arts = new[] { "twin_cut", "pierce_cut" } });
        var (s, _) = Start(b);
        var hero = s.Unit("u0");
        var foe = s.Unit("u1");

        var plain = CombatCalculator.Forecast(s, hero, hero.Pos, foe, new WeaponAttack("sword"));
        var twin = CombatCalculator.Forecast(s, hero, hero.Pos, foe, new ArtAttack("twin_cut", "sword"));
        Assert.True(twin.Strikes[0].ByAttacker && twin.Strikes[1].ByAttacker);
        Assert.Equal(plain.Strikes.Count(x => x.ByAttacker) + 1, twin.Strikes.Count(x => x.ByAttacker));

        var pierce = CombatCalculator.Forecast(s, hero, hero.Pos, foe, new ArtAttack("pierce_cut", "sword"));
        int def = s.Stats(foe).Def;
        Assert.Equal(plain.Attacker.Damage + def / 2, pierce.Attacker.Damage);
    }

    // ------------------------------------------------------------------ healing, items, trade

    [Fact]
    public void Heal_RestoresHpUsesASpellAndGivesExp()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Named("mira", Faction.Player, "healer", 1, 0).Unit(Faction.Enemy, "merc", 4, 0));
        var lord = s.Unit("u0");
        lord.CurrentHp = 3;
        var mira = s.Unit("u1");
        Assert.Equal(3, mira.UsesLeft["heal"]);
        var r = tm.Submit(new ActCommand("u1", mira.Pos, new HealAction("u0", "heal")));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(18, lord.CurrentHp); // 10 + Mag 10 / 2
        Assert.Equal(2, mira.UsesLeft["heal"]);
        Assert.Equal(ExpCalculator.HealExp, r.Events.OfType<ExpGained>().Single().Amount);
    }

    [Fact]
    public void SelfHealSpell_HealsTheCasterAfterMovingButNoOneElse()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword")
            .Named("mira", Faction.Player, "monk", 2, 0).Unit(Faction.Enemy, "merc", 4, 0));
        var lord = s.Unit("u0");
        var mira = s.Unit("u1");
        lord.CurrentHp = 3;
        var dest = new GridPos(1, 0); // next to the hurt lord: range 0 still only reaches herself
        Assert.Empty(ActionRules.HealTargets(s, mira, dest, "prayer"));
        mira.CurrentHp = 2;
        Assert.Equal(new[] { mira }, ActionRules.HealTargets(s, mira, dest, "prayer"));
        var r = tm.Submit(new ActCommand("u1", dest, new HealAction("u1", "prayer")));
        Assert.True(r.Ok, r.Error);
        Assert.True(mira.CurrentHp > 2);
        Assert.Equal(2, mira.UsesLeft["prayer"]);
    }

    [Fact]
    public void Vulnerary_HealsAndIsUsedUp()
    {
        var (s, tm) = Start(new TestBattle(".....").Lord(0, 0, "sword", "vulnerary").Unit(Faction.Enemy, "merc", 4, 0));
        var lord = s.Unit("u0");
        Assert.Empty(ActionRules.UsableItems(s, lord)); // full HP
        lord.CurrentHp = 5;
        Assert.True(tm.Submit(new ActCommand("u0", lord.Pos, new UseItemAction("vulnerary"))).Ok);
        Assert.Equal(15, lord.CurrentHp);
        Assert.DoesNotContain("vulnerary", lord.State.Inventory);
        Assert.Equal("sword", lord.State.EquippedItemId);
    }

    [Fact]
    public void Trade_SwapsItems_AndTheUnitCanStillActInPlace()
    {
        var (s, tm) = Start(new TestBattle(".....", ".....").Lord(0, 0, "sword", "vulnerary")
            .Unit(Faction.Player, "merc", 2, 0, "killer").Unit(Faction.Enemy, "merc", 4, 1));
        var r = tm.Submit(new TradeCommand("u0", new GridPos(1, 0), "u1", new[] { "vulnerary" }, new[] { "killer" }));
        Assert.True(r.Ok, r.Error);
        Assert.Equal(new[] { "sword", "killer" }, s.Unit("u0").State.Inventory);
        Assert.Equal(new[] { "vulnerary" }, s.Unit("u1").State.Inventory);
        Assert.Equal(ActionState.Moved, s.Unit("u0").ActionState);
        Assert.False(tm.Submit(new ActCommand("u0", new GridPos(1, 1), new WaitAction())).Ok);
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(1, 0), new WaitAction())).Ok);
    }

    // ------------------------------------------------------------------ crest skills

    [Fact]
    public void CrestSkills_WardLastsUntilTheNextPlayerPhase_AndDawnRefreshes()
    {
        var (s, tm) = Start(new TestBattle(".......", ".......").Lord(0, 0, "sword")
            .Unit(Faction.Player, "merc", 1, 0, "sword").Unit(Faction.Player, "merc", 0, 1, "sword")
            .Unit(Faction.Enemy, "merc", 6, 1).Options(new BattleOptions(CrestTier: 4)));
        var lord = s.Unit("u0");
        // Dawn needs an ally who has already acted.
        Assert.Equal(new[] { CrestSkill.Ward, CrestSkill.Rally, CrestSkill.Mend }, ActionRules.ReadyCrestSkills(s, lord, lord.Pos).ToArray());

        // u2 acts first so Dawn has someone to refresh.
        Assert.True(tm.Submit(new ActCommand("u2", new GridPos(0, 1), new WaitAction())).Ok);
        Assert.True(tm.Submit(new ActCommand("u0", lord.Pos, new CrestAction(CrestSkill.Dawn, "u2"))).Ok);
        Assert.Equal(ActionState.Ready, s.Unit("u2").ActionState);

        Assert.True(tm.Submit(new ActCommand("u1", new GridPos(1, 0), new WaitAction())).Ok);
        // Ward needs the lord to act, so start a new turn first.
        tm.Submit(new EndPhaseCommand());
        tm.Submit(new EndPhaseCommand());
        Assert.Equal(2, s.Turn);
        Assert.True(tm.Submit(new ActCommand("u0", lord.Pos, new CrestAction(CrestSkill.Ward, "u1"))).Ok);
        Assert.Equal(9, s.Stats(s.Unit("u1")).Def);
        Assert.Equal(1, lord.UsesLeft[CrestSkills.UseKey(CrestSkill.Ward)]);
        tm.Submit(new EndPhaseCommand());
        tm.Submit(new EndPhaseCommand());
        Assert.Equal(5, s.Stats(s.Unit("u1")).Def);
    }

    [Fact]
    public void CrestSkills_NeedTheCrestTier()
    {
        var (s, _) = Start(new TestBattle("...").Lord(0, 0, "sword").Unit(Faction.Player, "merc", 1, 0).Unit(Faction.Enemy, "merc", 2, 0)
            .Options(new BattleOptions(CrestTier: 1)));
        Assert.Equal(new[] { CrestSkill.Ward }, ActionRules.ReadyCrestSkills(s, s.Unit("u0"), new GridPos(0, 0)).ToArray());
        Assert.False(ActionRules.Validate(s, s.Unit("u0"), new GridPos(0, 0), new CrestAction(CrestSkill.Rally, null)).Ok);
    }

    // ------------------------------------------------------------------ supports and the aura

    [Fact]
    public void Supports_EarnPointsNextToPartners_AndGiveBonuses()
    {
        var b = new TestBattle(".....", ".....").Lord(0, 0, "sword")
            .Named("partner", Faction.Player, "merc", 0, 1, "sword")
            .Unit(Faction.Enemy, "merc", 1, 0, "sword");
        b.Db.Add(b.Db.Unit("lord") with { Supports = new[] { new SupportDef("partner", 10, 20, 30) } });
        b.Options(new BattleOptions(SupportRank: (x, y) => 1));
        var (s, tm) = Start(b);
        var lord = s.Unit("u0");
        // The partner is right beside the lord: the support rank's bonus plus the one for any friend beside you.
        Assert.Equal((5 + BattleState.AdjacentHit, 5 + BattleState.AdjacentAvoid, 0), s.CombatBonus(lord, lord.Pos));
        Assert.Equal((5, 5, 0), s.CombatBonus(lord, new GridPos(2, 1)));
        Assert.Equal((0, 0, 0), s.CombatBonus(lord, new GridPos(3, 0)));
        tm.Submit(new ActCommand("u0", lord.Pos, new AttackAction("u2", new WeaponAttack("sword"))));
        Assert.Equal(1, s.SupportPoints[Supports.PairKey("lord", "partner")]);
        // Standing together earns at most AdjacentCap a battle, however many actions they take side by side.
        for (int i = 0; i < 10; i++)
            BattleProgress.SupportTick(s, lord);
        Assert.Equal(Supports.AdjacentCap, s.SupportPoints[Supports.PairKey("lord", "partner")]);
        Assert.Equal(1, Supports.Unlocked(s.Db, "lord", "partner", 10, 0));
        Assert.Null(Supports.Unlocked(s.Db, "lord", "partner", 9, 0));
        Assert.Equal("support_lord_partner_C", Supports.SceneId("partner", "lord", 1));
    }

    [Fact]
    public void CrestAura_HelpsAlliesNearTheLord()
    {
        var (s, _) = Start(new TestBattle("......").Lord(0, 0, "sword").Unit(Faction.Player, "merc", 2, 0)
            .Unit(Faction.Enemy, "merc", 5, 0).Options(new BattleOptions(CrestTier: 2)));
        Assert.Equal((10, 10, 0), s.CombatBonus(s.Unit("u1"), new GridPos(2, 0)));
        Assert.Equal((0, 0, 0), s.CombatBonus(s.Unit("u1"), new GridPos(3, 0)));
        Assert.Equal((0, 0, 0), s.CombatBonus(s.Unit("u0"), new GridPos(0, 0)));
    }
}
