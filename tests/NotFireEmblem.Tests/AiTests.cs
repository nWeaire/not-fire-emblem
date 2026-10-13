using NotFireEmblem.AI;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class AiTests
{
    private static BattleState EnemyPhase(TestBattle builder)
    {
        var s = builder.Build();
        s.Phase = Faction.Enemy;
        return s;
    }

    private static AttackAction? AttackOf(AiDecision d) => (d.Command as ActCommand)?.Action as AttackAction;

    [Fact]
    public void TakesTheGuaranteedKill()
    {
        var s = EnemyPhase(new TestBattle(".......", ".......")
            .Unit(Faction.Player, "merc", 1, 0, "sword")
            .Unit(Faction.Player, "merc", 5, 0, "sword")
            .Unit(Faction.Enemy, "merc", 3, 1, "sword"));
        s.Unit("u1").CurrentHp = 5;
        var decision = AiController.Decide(s, s.Unit("u2"));
        Assert.Equal("u1", AttackOf(decision)?.TargetId);
        Assert.True(decision.Command.Validate(s).Ok);
    }

    [Fact]
    public void AvoidsSuicidalAttack_WhenSafeOneExists()
    {
        // The enemy has 5 HP. Attacking the sword user means a lethal counter; the archer can't counter at range 1.
        var s = EnemyPhase(new TestBattle(".......", ".......")
            .Unit(Faction.Player, "merc", 1, 0, "sword")
            .Unit(Faction.Player, "archer", 5, 0, "bow")
            .Unit(Faction.Enemy, "merc", 3, 1, "sword"));
        s.Unit("u2").CurrentHp = 5;
        var decision = AiController.Decide(s, s.Unit("u2"));
        Assert.Equal("u1", AttackOf(decision)?.TargetId);
    }

    [Fact]
    public void Stationary_WaitsWhenNothingIsInRangeOfItsCell()
    {
        var s = EnemyPhase(new TestBattle(".....")
            .Unit(Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 2, 0, "sword").WithAi(AiProfile.Stationary));
        var command = Assert.IsType<ActCommand>(AiController.Decide(s, s.Unit("u1")).Command);
        Assert.Equal(new GridPos(2, 0), command.Destination);
        Assert.IsType<WaitAction>(command.Action);
    }

    [Fact]
    public void Stationary_AttacksFromItsOwnCell()
    {
        var s = EnemyPhase(new TestBattle(".....")
            .Unit(Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "archer", 2, 0, "bow").WithAi(AiProfile.Stationary));
        var command = Assert.IsType<ActCommand>(AiController.Decide(s, s.Unit("u1")).Command);
        Assert.Equal(new GridPos(2, 0), command.Destination);
        Assert.IsType<AttackAction>(command.Action);
    }

    [Fact]
    public void Aggressive_ApproachesWhenNothingIsInReach()
    {
        var s = EnemyPhase(new TestBattle("....................")
            .Unit(Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 19, 0, "sword"));
        var command = Assert.IsType<ActCommand>(AiController.Decide(s, s.Unit("u1")).Command);
        Assert.Equal(new GridPos(14, 0), command.Destination);
        Assert.True(command.Validate(s).Ok);
    }

    [Fact]
    public void Aggressive_RoutesAroundWalls()
    {
        var s = EnemyPhase(new TestBattle(
                "........",
                "######.#",
                "........")
            .Unit(Faction.Player, "merc", 0, 2, "sword")
            .Unit(Faction.Enemy, "merc", 0, 0, "sword"));
        var command = Assert.IsType<ActCommand>(AiController.Decide(s, s.Unit("u1")).Command);
        Assert.Equal(new GridPos(5, 0), command.Destination); // 5 steps along the only route, through the gap at x=6
    }

    [Fact]
    public void GuardRegion_SleepsUntilSomethingIsInReach()
    {
        var far = EnemyPhase(new TestBattle("..............")
            .Unit(Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 13, 0, "sword").WithAi(AiProfile.GuardRegion));
        var idle = Assert.IsType<ActCommand>(AiController.Decide(far, far.Unit("u1")).Command);
        Assert.Equal(new GridPos(13, 0), idle.Destination);

        var near = EnemyPhase(new TestBattle("..............")
            .Unit(Faction.Player, "merc", 8, 0, "sword")
            .Unit(Faction.Enemy, "merc", 13, 0, "sword").WithAi(AiProfile.GuardRegion));
        Assert.NotNull(AttackOf(AiController.Decide(near, near.Unit("u1"))));
    }

    [Fact]
    public void Boss_OnlyStepsWithinLeash()
    {
        var s = EnemyPhase(new TestBattle("..........")
            .Unit(Faction.Player, "merc", 5, 0, "sword")
            .Unit(Faction.Enemy, "merc", 8, 0, "sword").WithAi(AiProfile.Boss));
        var command = Assert.IsType<ActCommand>(AiController.Decide(s, s.Unit("u1")).Command);
        Assert.Equal(new GridPos(8, 0), command.Destination); // target is 2 cells beyond the leash: hold

        var close = EnemyPhase(new TestBattle("..........")
            .Unit(Faction.Player, "merc", 6, 0, "sword")
            .Unit(Faction.Enemy, "merc", 8, 0, "sword").WithAi(AiProfile.Boss));
        var attack = Assert.IsType<ActCommand>(AiController.Decide(close, close.Unit("u1")).Command);
        Assert.Equal(new GridPos(7, 0), attack.Destination);
        Assert.IsType<AttackAction>(attack.Action);
    }

    [Fact]
    public void NextDecision_OrdersAttackersFirst()
    {
        var s = EnemyPhase(new TestBattle("..............")
            .Unit(Faction.Player, "merc", 0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 13, 0, "sword")
            .Unit(Faction.Enemy, "merc", 3, 0, "sword"));
        var decision = AiController.NextDecision(s);
        Assert.Equal("u2", decision!.Unit.Id);
        Assert.NotNull(AttackOf(decision));
    }

    [Fact]
    public void FullEnemyPhase_RunsThroughTurnManager()
    {
        var s = new TestBattle("..........", "..........")
            .Lord(0, 0, "sword")
            .Unit(Faction.Enemy, "merc", 9, 0, "sword")
            .Unit(Faction.Enemy, "merc", 9, 1, "sword")
            .Build();
        var tm = new TurnManager(s);
        tm.StartBattle();
        tm.Submit(new EndPhaseCommand());
        int guard = 0;
        while (s.Phase == Faction.Enemy && s.Outcome == BattleOutcome.InProgress && guard++ < 10)
            Assert.True(tm.Submit(AiController.NextDecision(s)!.Command).Ok);
        Assert.Equal(Faction.Player, s.Phase);
        Assert.Equal(2, s.Turn);
        Assert.All(s.LivingOf(Faction.Enemy), u => Assert.True(u.Pos.X < 9));
    }

    [Fact]
    public void TrueHit_MatchesTwoRnTable()
    {
        Assert.Equal(0.0, CombatOdds.TrueHit(0));
        Assert.Equal(1.0, CombatOdds.TrueHit(100));
        Assert.Equal(0.922, CombatOdds.TrueHit(80), 3);
        Assert.Equal(0.505, CombatOdds.TrueHit(50), 3);
    }
}
