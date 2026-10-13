using System;
using System.Collections.Generic;
using NotFireEmblem.Units;

namespace NotFireEmblem.Grid;

/// <summary>Everything the pathfinder needs to know about the unit that is moving.</summary>
public readonly record struct Mover(GridPos Start, Faction Faction, MovementType MoveType, int Mov);

/// <summary>Result of a reachability search: cost and predecessor for every cell the unit can pass through.</summary>
public sealed class ReachMap
{
    private readonly Dictionary<GridPos, int> _cost;
    private readonly Dictionary<GridPos, GridPos> _prev;
    private readonly HashSet<GridPos> _stoppable;

    public GridPos Origin { get; }

    internal ReachMap(GridPos origin, Dictionary<GridPos, int> cost, Dictionary<GridPos, GridPos> prev, HashSet<GridPos> stoppable)
    {
        Origin = origin;
        _cost = cost;
        _prev = prev;
        _stoppable = stoppable;
    }

    /// <summary>Cells the unit can end its move on (includes its own cell).</summary>
    public IReadOnlyCollection<GridPos> StopCells => _stoppable;

    public bool CanReach(GridPos p) => _cost.ContainsKey(p);
    public bool CanStop(GridPos p) => _stoppable.Contains(p);
    public int CostTo(GridPos p) => _cost.TryGetValue(p, out var c) ? c : int.MaxValue;

    /// <summary>The path from the origin to <paramref name="target"/> inclusive, or empty if unreachable.</summary>
    public IReadOnlyList<GridPos> PathTo(GridPos target)
    {
        if (!_cost.ContainsKey(target))
            return Array.Empty<GridPos>();
        var path = new List<GridPos> { target };
        var current = target;
        while (_prev.TryGetValue(current, out var previous))
        {
            path.Add(previous);
            current = previous;
        }
        path.Reverse();
        return path;
    }
}

public static class Pathfinder
{
    /// <summary>
    /// Cost of stepping from <paramref name="from"/> into the adjacent cell <paramref name="to"/>,
    /// or -1 if the step is not allowed (impassable terrain or too steep).
    /// </summary>
    public static int StepCost(BattleGrid grid, GridPos from, GridPos to, MovementType moveType)
    {
        if (!grid.InBounds(to))
            return -1;
        int cost = grid[to].Terrain.CostFor(moveType);
        if (cost <= 0)
            return -1;
        if (moveType != MovementType.Flying && Math.Abs(grid[to].Height - grid[from].Height) > 1)
            return -1;
        return cost;
    }

    /// <summary>
    /// Dijkstra over movement cost. Friendly units can be passed through but not stopped on; hostile units block.
    /// Entering a cell next to a hostile unit (zone of control) ends movement there.
    /// </summary>
    public static ReachMap GetReachable(BattleGrid grid, Mover mover, Func<GridPos, Faction?> occupantAt)
    {
        var cost = new Dictionary<GridPos, int> { [mover.Start] = 0 };
        var prev = new Dictionary<GridPos, GridPos>();
        var stoppable = new HashSet<GridPos> { mover.Start };
        var frontier = new PriorityQueue<GridPos, int>();
        frontier.Enqueue(mover.Start, 0);

        bool IsHostile(GridPos p) => occupantAt(p) is { } f && f.IsHostileTo(mover.Faction);
        bool InZoc(GridPos p)
        {
            foreach (var n in p.Neighbors())
                if (grid.InBounds(n) && IsHostile(n))
                    return true;
            return false;
        }

        while (frontier.TryDequeue(out var current, out int currentCost))
        {
            if (currentCost > cost[current])
                continue; // stale entry
            if (current != mover.Start && InZoc(current))
                continue; // zone of control: can enter, can't continue

            foreach (var next in current.Neighbors())
            {
                int step = StepCost(grid, current, next, mover.MoveType);
                if (step < 0 || IsHostile(next))
                    continue;
                int nextCost = currentCost + step;
                if (nextCost > mover.Mov)
                    continue;
                if (cost.TryGetValue(next, out int known) && known <= nextCost)
                    continue;
                cost[next] = nextCost;
                prev[next] = current;
                if (occupantAt(next) is null)
                    stoppable.Add(next);
                frontier.Enqueue(next, nextCost);
            }
        }
        return new ReachMap(mover.Start, cost, prev, stoppable);
    }

    /// <summary>
    /// A* over terrain cost only (ignores units and zones of control). Used by the AI to head toward far goals.
    /// Returns the path from start to goal inclusive, or null if the goal can't be reached at all.
    /// </summary>
    public static IReadOnlyList<GridPos>? FindPath(BattleGrid grid, GridPos start, GridPos goal, MovementType moveType)
    {
        if (!grid.InBounds(goal))
            return null;
        var cost = new Dictionary<GridPos, int> { [start] = 0 };
        var prev = new Dictionary<GridPos, GridPos>();
        var frontier = new PriorityQueue<GridPos, int>();
        frontier.Enqueue(start, start.DistanceTo(goal));

        while (frontier.TryDequeue(out var current, out _))
        {
            if (current == goal)
            {
                var path = new List<GridPos> { goal };
                while (prev.TryGetValue(path[^1], out var p))
                    path.Add(p);
                path.Reverse();
                return path;
            }
            foreach (var next in current.Neighbors())
            {
                int step = StepCost(grid, current, next, moveType);
                if (step < 0)
                    continue;
                int nextCost = cost[current] + step;
                if (cost.TryGetValue(next, out int known) && known <= nextCost)
                    continue;
                cost[next] = nextCost;
                prev[next] = current;
                frontier.Enqueue(next, nextCost + next.DistanceTo(goal));
            }
        }
        return null;
    }
}
