using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Tests;

public class PathfinderTests
{
    private static ReachMap ReachOf(NotFireEmblem.Battle.BattleState s, int unitIndex = 0) => s.Reach(s.Units[unitIndex]);

    [Fact]
    public void OpenField_ReachesDiamondOfMov()
    {
        var s = new TestBattle(
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........",
            "...........").Unit(Faction.Player, "merc", 5, 5).Build();
        var reach = ReachOf(s);
        // Mov 5 diamond: 2*5*6 + 1 = 61 cells.
        Assert.Equal(61, reach.StopCells.Count);
        Assert.True(reach.CanStop(new GridPos(0, 5)));
        Assert.False(reach.CanReach(new GridPos(0, 4)));
    }

    [Fact]
    public void Forest_CostsMoreForCavalryThanInfantry()
    {
        var rows = new[] { "FFFFFFFF" };
        var inf = new TestBattle(rows).Unit(Faction.Player, "merc", 0, 0).Build();
        var cav = new TestBattle(rows).Unit(Faction.Player, "cav", 0, 0).Build();
        // Infantry: Mov 5 / cost 2 = 2 steps. Cavalry: Mov 7 / cost 3 = 2 steps.
        Assert.True(ReachOf(inf).CanStop(new GridPos(2, 0)));
        Assert.False(ReachOf(inf).CanStop(new GridPos(3, 0)));
        Assert.True(ReachOf(cav).CanStop(new GridPos(2, 0)));
        Assert.False(ReachOf(cav).CanStop(new GridPos(3, 0)));
    }

    [Fact]
    public void Impassable_BlocksGroundButNotFlyingOverWater()
    {
        var rows = new[] { ".~~.", "####" };
        var inf = new TestBattle(rows).Unit(Faction.Player, "merc", 0, 0).Build();
        var fly = new TestBattle(rows).Unit(Faction.Player, "flier", 0, 0).Build();
        Assert.False(ReachOf(inf).CanReach(new GridPos(3, 0)));
        Assert.True(ReachOf(fly).CanStop(new GridPos(3, 0)));
        Assert.False(ReachOf(fly).CanReach(new GridPos(0, 1))); // walls stop fliers too
    }

    [Fact]
    public void Armored_CannotEnterMountains()
    {
        var s = new TestBattle(".M.").Unit(Faction.Player, "knight", 0, 0).Build();
        Assert.False(ReachOf(s).CanReach(new GridPos(1, 0)));
    }

    [Fact]
    public void Height_StepsOfMoreThanOneAreBlockedExceptForFliers()
    {
        var rows = new[] { "....." };
        var heights = new[] { "01310" };
        var inf = new TestBattle(rows).Heights(heights).Unit(Faction.Player, "merc", 0, 0).Build();
        var fly = new TestBattle(rows).Heights(heights).Unit(Faction.Player, "flier", 0, 0).Build();
        Assert.True(ReachOf(inf).CanStop(new GridPos(1, 0)));
        Assert.False(ReachOf(inf).CanReach(new GridPos(2, 0))); // 1 -> 3
        Assert.True(ReachOf(fly).CanStop(new GridPos(4, 0)));
    }

    [Fact]
    public void Allies_CanBePassedButNotStoppedOn()
    {
        var s = new TestBattle(".....")
            .Unit(Faction.Player, "merc", 0, 0)
            .Unit(Faction.Ally, "merc", 1, 0)
            .Build();
        var reach = ReachOf(s);
        Assert.True(reach.CanReach(new GridPos(1, 0)));
        Assert.False(reach.CanStop(new GridPos(1, 0)));
        Assert.True(reach.CanStop(new GridPos(2, 0)));
    }

    [Fact]
    public void Enemies_Block()
    {
        var s = new TestBattle(".....")
            .Unit(Faction.Player, "merc", 0, 0)
            .Unit(Faction.Enemy, "merc", 2, 0)
            .Build();
        var reach = ReachOf(s);
        Assert.False(reach.CanReach(new GridPos(2, 0)));
        Assert.False(reach.CanReach(new GridPos(3, 0)));
    }

    [Fact]
    public void ZoneOfControl_StopsMovementNextToEnemy()
    {
        var s = new TestBattle(
            ".......",
            ".......",
            ".......")
            .Unit(Faction.Player, "cav", 0, 1)
            .Unit(Faction.Enemy, "merc", 3, 0)
            .Build();
        var reach = ReachOf(s);
        // (3,1) is adjacent to the enemy: enterable but movement can't continue through it.
        Assert.True(reach.CanStop(new GridPos(3, 1)));
        // (4,1) is reachable only by going around via row 2.
        Assert.Equal(new[] { new GridPos(0, 1), new GridPos(1, 1), new GridPos(1, 2), new GridPos(2, 2), new GridPos(3, 2), new GridPos(4, 2), new GridPos(4, 1) }.Length,
            reach.PathTo(new GridPos(4, 1)).Count);
        Assert.DoesNotContain(new GridPos(3, 1), reach.PathTo(new GridPos(4, 1)).Skip(1).SkipLast(1));
    }

    [Fact]
    public void ZoneOfControl_DoesNotTrapUnitAlreadyAdjacent()
    {
        var s = new TestBattle(".....")
            .Unit(Faction.Player, "merc", 1, 0)
            .Unit(Faction.Enemy, "merc", 0, 0)
            .Build();
        Assert.True(ReachOf(s).CanStop(new GridPos(4, 0)));
    }

    [Fact]
    public void PathTo_StartsAtOriginAndEndsAtTarget()
    {
        var s = new TestBattle("....", ".##.", "....").Unit(Faction.Player, "merc", 0, 1).Build();
        var path = ReachOf(s).PathTo(new GridPos(3, 1));
        Assert.Equal(new GridPos(0, 1), path[0]);
        Assert.Equal(new GridPos(3, 1), path[^1]);
        Assert.Equal(6, path.Count);
        for (int i = 1; i < path.Count; i++)
            Assert.Equal(1, path[i - 1].DistanceTo(path[i]));
    }

    [Fact]
    public void FindPath_IgnoresMovLimitAndFindsRouteAroundWalls()
    {
        var s = new TestBattle(
            "..........",
            "#########.",
            "..........").Build();
        var path = Pathfinder.FindPath(s.Grid, new GridPos(0, 0), new GridPos(0, 2), MovementType.Infantry);
        Assert.NotNull(path);
        Assert.Equal(21, path!.Count);
        Assert.Null(Pathfinder.FindPath(s.Grid, new GridPos(0, 0), new GridPos(0, 1), MovementType.Infantry));
    }
}
