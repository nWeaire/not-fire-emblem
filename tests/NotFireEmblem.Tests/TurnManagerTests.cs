using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class TurnManagerTests
{
    private static (BattleState s, TurnManager tm) Start(TestBattle builder)
    {
        var s = builder.Build();
        var tm = new TurnManager(s);
        tm.StartBattle();
        return (s, tm);
    }

    private static TestBattle TwoVsOne() => new TestBattle(".......", ".......")
        .Lord(0, 0, "sword")
        .Unit(Faction.Player, "merc", 0, 1, "sword")
        .Unit(Faction.Enemy, "merc", 6, 0, "sword");

    [Fact]
    public void SpriteIds_ResolveFromSpriteIdThenUnitIdThenFactionAndClass()
    {
        Assert.Equal("boss", new SpawnDef("x", "fighter", null, 1, Array.Empty<string>(), Faction.Enemy, default, SpriteId: "boss").ResolveSpriteId());
        Assert.Equal("rowan", new SpawnDef("rowan", null, null, 1, Array.Empty<string>(), Faction.Player, default).ResolveSpriteId());
        Assert.Equal("enemy_fighter", new SpawnDef(null, "fighter", null, 1, Array.Empty<string>(), Faction.Enemy, default).ResolveSpriteId());
        var (s, _) = Start(TwoVsOne());
        Assert.Equal("lord", s.Unit("u0").SpriteId);
        Assert.Equal("enemy_merc", s.Unit("u2").SpriteId);
        Assert.Equal(new[] { "lord_merc", "lord" }, s.Unit("u0").SpriteCandidates());
    }

    [Fact]
    public void Battle_StartsOnPlayerPhaseTurnOne()
    {
        var (s, _) = Start(TwoVsOne());
        Assert.Equal(Faction.Player, s.Phase);
        Assert.Equal(1, s.Turn);
    }

    [Fact]
    public void PhaseAdvances_WhenAllPlayerUnitsHaveActed()
    {
        var (s, tm) = Start(TwoVsOne());
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(1, 0), new WaitAction())).Ok);
        Assert.Equal(Faction.Player, s.Phase);
        var result = tm.Submit(new ActCommand("u1", new GridPos(1, 1), new WaitAction()));
        Assert.True(result.Ok);
        Assert.Equal(Faction.Enemy, s.Phase);
        Assert.Contains(result.Events, e => e is PhaseChanged { Phase: Faction.Enemy });
    }

    [Fact]
    public void EnemyPhaseEnd_StartsNextTurnAndResetsUnits()
    {
        var (s, tm) = Start(TwoVsOne());
        tm.Submit(new EndPhaseCommand());
        Assert.Equal(Faction.Enemy, s.Phase);
        tm.Submit(new EndPhaseCommand());
        Assert.Equal(Faction.Player, s.Phase);
        Assert.Equal(2, s.Turn);
        Assert.All(s.LivingOf(Faction.Player), u => Assert.Equal(ActionState.Ready, u.ActionState));
    }

    [Fact]
    public void AllyPhase_RunsAfterEnemyPhaseWhenAlliesExist()
    {
        var (s, tm) = Start(TwoVsOne().Unit(Faction.Ally, "merc", 3, 1, "sword"));
        tm.Submit(new EndPhaseCommand());
        tm.Submit(new EndPhaseCommand());
        Assert.Equal(Faction.Ally, s.Phase);
        Assert.Equal(1, s.Turn);
    }

    [Fact]
    public void Validation_RejectsActingTwiceAndOutOfPhase()
    {
        var (s, tm) = Start(TwoVsOne());
        Assert.False(tm.Submit(new ActCommand("u2", new GridPos(5, 0), new WaitAction())).Ok); // enemy on player phase
        Assert.True(tm.Submit(new ActCommand("u0", new GridPos(0, 0), new WaitAction())).Ok);
        Assert.False(tm.Submit(new ActCommand("u0", new GridPos(1, 0), new WaitAction())).Ok);
        Assert.False(tm.Submit(new ActCommand("u1", new GridPos(6, 1), new WaitAction())).Ok); // beyond Mov 5
    }

    [Fact]
    public void Validation_RejectsOutOfRangeAttack()
    {
        var (_, tm) = Start(TwoVsOne());
        var result = tm.Submit(new ActCommand("u0", new GridPos(4, 0), new AttackAction("u2", new WeaponAttack("sword"))));
        Assert.False(result.Ok);
    }

    [Fact]
    public void Attack_KillsTargetAndWinsRout()
    {
        // Weaken the enemy so one hit kills it; seed doesn't matter for 100% hit.
        var builder = TwoVsOne();
        var (s, tm) = Start(builder);
        s.Unit("u2").CurrentHp = 1;
        var result = tm.Submit(new ActCommand("u0", new GridPos(5, 0), new AttackAction("u2", new WeaponAttack("sword"))));
        Assert.True(result.Ok, result.Error);
        Assert.Contains(result.Events, e => e is UnitDefeated { UnitId: "u2" });
        Assert.Equal(BattleOutcome.Victory, s.Outcome);
        Assert.IsType<BattleEnded>(result.Events[^1]);
        Assert.False(tm.Submit(new EndPhaseCommand()).Ok);
    }

    [Fact]
    public void LordDeath_IsDefeat()
    {
        var (s, tm) = Start(TwoVsOne());
        s.Unit("u0").CurrentHp = 1;
        tm.Submit(new ActCommand("u0", new GridPos(5, 0), new AttackAction("u2", new WeaponAttack("sword"))));
        // Enemy has 20 HP, takes 10; counter kills the 1-HP lord.
        Assert.Equal(BattleOutcome.Defeat, s.Outcome);
    }

    [Fact]
    public void ReplayFromSameSeedAndCommands_ProducesSameState()
    {
        IBattleCommand[] commands =
        {
            new ActCommand("u0", new GridPos(5, 0), new AttackAction("u2", new WeaponAttack("sword"))),
            new ActCommand("u1", new GridPos(5, 1), new WaitAction()),
            new EndPhaseCommand(),
        };
        string Run()
        {
            var (s, tm) = Start(TwoVsOne());
            foreach (var c in commands)
                tm.Submit(c);
            return string.Join(";", s.Units.Select(u => $"{u.Id}:{u.Pos}:{u.CurrentHp}:{u.ActionState}")) + $"|{s.Turn}{s.Phase}";
        }
        Assert.Equal(Run(), Run());
    }
}
