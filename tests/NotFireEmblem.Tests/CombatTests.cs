using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class CombatTests
{
    private static CombatForecast Forecast(BattleState s, string weapon = "sword") =>
        CombatCalculator.Forecast(s, s.Units[0], s.Units[0].Pos, s.Units[1], new WeaponAttack(weapon));

    [Fact]
    public void Counter_UsesACarriedWeaponThatReachesWhenTheEquippedOneCant()
    {
        // A javelin thrown from 2 cells: the defender's equipped lance can't reach, its javelin can.
        var s = new TestBattle("...").Unit(Faction.Player, "soldier", 0, 0, "javelin")
            .Unit(Faction.Enemy, "soldier", 2, 0, "lance", "javelin").Build();
        var f = Forecast(s, "javelin");
        Assert.True(f.Defender.CanStrike);
        // With nothing that reaches, still no counter.
        var t = new TestBattle("...").Unit(Faction.Player, "soldier", 0, 0, "javelin")
            .Unit(Faction.Enemy, "soldier", 2, 0, "lance").Build();
        Assert.False(Forecast(t, "javelin").Defender.CanStrike);
    }

    [Fact]
    public void Tomes_GrowWithTheCaster_AndElementsDoubleMightOnTheirFoe()
    {
        // Mag 10, Fire Mt 5, Res 5. Level 1: no bonus.
        var b = new TestBattle("..").Unit(Faction.Player, "mage", 0, 0, "fire").Unit(Faction.Enemy, "merc", 1, 0, "sword");
        b.Db.Add(b.Db.Weapon("fire") with { EffectiveAgainst = new[] { "Beast" } });
        var s = b.Build();
        Assert.Equal(10, Forecast(s, "fire").Attacker.Damage);
        s.Units[0].State.Level = 7;                                    // +1 might per 3 levels: +2
        Assert.Equal(12, Forecast(s, "fire").Attacker.Damage);

        // Fire on a beast: might doubled, (5 + 2) * 2 = 14, so 10 + 14 - 5.
        var c = new TestBattle("..").Unit(Faction.Player, "mage", 0, 0, "fire").Unit(Faction.Enemy, "beast", 1, 0, "claws");
        c.Db.Add(c.Db.Weapon("fire") with { EffectiveAgainst = new[] { "Beast" } });
        var t = c.Build();
        t.Units[0].State.Level = 7;
        Assert.Equal(19, Forecast(t, "fire").Attacker.Damage);
    }

    [Fact]
    public void BaseFormulas_MatchGdd()
    {
        // Both: Str 10, Dex 10, Spd 10, Lck 0, Def 5. Weapon Mt 5, Hit 100, Wt 0.
        var s = new TestBattle("..").Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        var f = Forecast(s);
        Assert.Equal(10, f.Attacker.Damage);         // 10 + 5 - 5
        Assert.Equal(100, f.Attacker.Hit);           // 10*2 + 0 + 100 - 10*2 = 100
        Assert.Equal(5, f.Attacker.Crit);            // (10+0)/2 + 0 - 0
        Assert.Equal(10, f.Attacker.AttackSpeed);
        Assert.True(f.Defender.CanStrike);
        Assert.Equal(2, f.Strikes.Count);            // no follow-up at equal speed
    }

    [Fact]
    public void WeightSlowsAttackSpeed_AndEnablesEnemyFollowUp()
    {
        // heavy_sword Wt 10, Str 10 -> penalty 10 - 2 = 8 -> AS 2. Enemy AS 10 -> doubles.
        var s = new TestBattle("..").Unit(Faction.Player, "merc", 0, 0, "heavy_sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        var f = Forecast(s, "heavy_sword");
        Assert.Equal(2, f.Attacker.AttackSpeed);
        Assert.Equal(new[] { true, false, false }, f.Strikes.Select(x => x.ByAttacker));
        Assert.Equal(2, f.Defender.StrikeCount);
    }

    [Theory]
    [InlineData(3)] // AS difference 3: no follow-up
    [InlineData(4)] // exactly 4: follow-up
    public void FollowUpThreshold(int speedDiff)
    {
        var s = new TestBattle("..").Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[0].State.Gains = new StatBlock(Spd: speedDiff, HP: 0, Str: 0, Mag: 0, Dex: 0, Lck: 0, Def: 0, Res: 0, Mov: 0);
        var f = Forecast(s);
        Assert.Equal(speedDiff >= 4 ? 2 : 1, f.Attacker.StrikeCount);
    }

    [Theory]
    [InlineData("merc", "sword", "fighter", "axe", 1)]
    [InlineData("fighter", "axe", "soldier", "lance", 1)]
    [InlineData("soldier", "lance", "merc", "sword", 1)]
    [InlineData("merc", "sword", "soldier", "lance", -1)]
    [InlineData("soldier", "lance", "fighter", "axe", -1)]
    [InlineData("fighter", "axe", "merc", "sword", -1)]
    [InlineData("archer", "bow", "merc", "sword", 0)]
    public void Triangle_AdjustsHitAndDamage(string atkClass, string atkWeapon, string defClass, string defWeapon, int expected)
    {
        int range = atkWeapon == "bow" ? 2 : 1;
        var s = new TestBattle("...").Unit(Faction.Player, atkClass, 0, 0, atkWeapon).Unit(Faction.Enemy, defClass, range, 0, defWeapon).Build();
        var f = Forecast(s, atkWeapon);
        Assert.Equal(expected, f.Attacker.TriangleAdvantage);
        Assert.Equal(10 + expected * WeaponTriangle.DamageBonus, f.Attacker.Damage);
        Assert.Equal(Math.Clamp(100 + expected * WeaponTriangle.HitBonus, 0, 100), f.Attacker.Hit);
    }

    [Fact]
    public void Bows_AreEffectiveAgainstFliers()
    {
        var s = new TestBattle("...").Unit(Faction.Player, "archer", 0, 0, "bow").Unit(Faction.Enemy, "flier", 2, 0, "lance").Build();
        var f = Forecast(s, "bow");
        Assert.True(f.Attacker.Effective);
        Assert.Equal(10 + 15 - 5, f.Attacker.Damage);
    }

    [Fact]
    public void Lances_PierceHalfAnArmouredFoesDef()
    {
        // Knight: Def 5, so a lance ignores 2 of it (rounded down); a sword gets no help.
        var s = new TestBattle("...").Unit(Faction.Player, "cav", 0, 0, "lance", "sword").Unit(Faction.Enemy, "knight", 1, 0, "lance").Build();
        var pierce = Forecast(s, "lance");
        Assert.True(pierce.Attacker.Effective);
        Assert.Equal(10 + 5 - 3, pierce.Attacker.Damage);
        Assert.Equal(10 + 5 - 5 - WeaponTriangle.DamageBonus, Forecast(s, "sword").Attacker.Damage); // sword loses to lance
        // Not against an unarmoured foe.
        var t = new TestBattle("...").Unit(Faction.Player, "cav", 0, 0, "lance").Unit(Faction.Enemy, "soldier", 1, 0, "lance").Build();
        Assert.False(Forecast(t, "lance").Attacker.Effective);
        Assert.Equal(10 + 5 - 5, Forecast(t, "lance").Attacker.Damage);
    }

    [Fact]
    public void DefenderOutOfRange_CannotCounter()
    {
        var s = new TestBattle("...").Unit(Faction.Player, "archer", 0, 0, "bow").Unit(Faction.Enemy, "merc", 2, 0, "sword").Build();
        var f = Forecast(s, "bow");
        Assert.False(f.Defender.CanStrike);
        Assert.All(f.Strikes, x => Assert.True(x.ByAttacker));
    }

    [Fact]
    public void Magic_TargetsResistance()
    {
        var s = new TestBattle("...").Unit(Faction.Player, "mage", 0, 0, "fire").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[1].State.Gains = new StatBlock(HP: 0, Str: 0, Mag: 0, Dex: 0, Spd: 0, Lck: 0, Def: 0, Res: 3, Mov: 0);
        Assert.Equal(10 + 5 - 8, Forecast(s, "fire").Attacker.Damage);
    }

    [Fact]
    public void Terrain_AddsAvoidAndDefenceToDefender()
    {
        var s = new TestBattle(".F").Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        var f = Forecast(s);
        Assert.Equal(9, f.Attacker.Damage);
        Assert.Equal(80, f.Attacker.Hit);
    }

    [Fact]
    public void HigherGround_GivesHitBonus()
    {
        var s = new TestBattle("..").Heights("10").Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[1].State.Gains = new StatBlock(HP: 0, Str: 0, Mag: 0, Dex: 0, Spd: 10, Lck: 0, Def: 0, Res: 0, Mov: 0);
        var f = Forecast(s);
        Assert.Equal(100 - 20 + CombatCalculator.HeightHitBonus, f.Attacker.Hit);
    }

    [Fact]
    public void Resolve_IsDeterministicWithScriptedRolls()
    {
        var s = new TestBattle("..").Unit(Faction.Player, "merc", 0, 0, "killer").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        var f = Forecast(s, "killer");
        Assert.Equal(35, f.Attacker.Crit); // 5 + 30
        // Attacker: hit rolls (0,0) -> hit, crit roll 10 < 35 -> crit: 30 dmg, kills (20 HP).
        var result = CombatCalculator.Resolve(f, new ScriptedRng(0, 0, 10));
        Assert.Single(result.Strikes);
        Assert.True(result.Strikes[0].Crit);
        Assert.Equal(20, result.Strikes[0].Damage); // clamped to remaining HP
        Assert.True(result.DefenderDied);
        Assert.Equal(20, result.AttackerHpAfter);
    }

    [Fact]
    public void Resolve_MissesDealNoDamage()
    {
        var s = new TestBattle("..").Unit(Faction.Player, "merc", 0, 0, "sword").Unit(Faction.Enemy, "merc", 1, 0, "sword").Build();
        s.Units[1].State.Gains = new StatBlock(HP: 0, Str: 0, Mag: 0, Dex: 0, Spd: 0, Lck: 0, Def: 0, Res: 0, Mov: 0, Cha: 0);
        var f = Forecast(s) with { Strikes = new[] { new PlannedStrike(true, 10, 50, 0), new PlannedStrike(false, 10, 50, 0) } };
        // Strike 1: rolls 60+60 -> avg 60 >= 50 -> miss. Strike 2: rolls 10+20 -> hit, crit roll 99 -> no crit.
        var result = CombatCalculator.Resolve(f, new ScriptedRng(60, 60, 10, 20, 99));
        Assert.False(result.Strikes[0].Hit);
        Assert.Equal(0, result.Strikes[0].Damage);
        Assert.True(result.Strikes[1].Hit);
        Assert.Equal(10, result.AttackerHpAfter);
    }

    [Fact]
    public void TwoRn_IsSkewedTowardExtremes()
    {
        var rng = new Rng(42);
        IRng r = rng;
        int hits = 0;
        const int trials = 20000;
        for (int i = 0; i < trials; i++)
            if (r.Roll2(80))
                hits++;
        // True 2RN chance for displayed 80 is ~92%.
        Assert.InRange(hits / (double)trials, 0.90, 0.94);
    }

    [Fact]
    public void Rng_IsReproducibleFromSeed()
    {
        var a = new Rng(1234);
        var b = new Rng(1234);
        for (int i = 0; i < 100; i++)
            Assert.Equal(a.Next(100), b.Next(100));
    }
}
