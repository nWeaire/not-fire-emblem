using System.Collections.Generic;

namespace NotFireEmblem.Grid;

public static class RangeService
{
    /// <summary>In-bounds cells whose Manhattan distance from <paramref name="from"/> is within [min, max].</summary>
    public static IEnumerable<GridPos> CellsInRange(BattleGrid grid, GridPos from, int min, int max)
    {
        for (int dy = -max; dy <= max; dy++)
        {
            for (int dx = -max; dx <= max; dx++)
            {
                int d = System.Math.Abs(dx) + System.Math.Abs(dy);
                if (d < min || d > max)
                    continue;
                var p = new GridPos(from.X + dx, from.Y + dy);
                if (grid.InBounds(p))
                    yield return p;
            }
        }
    }

    /// <summary>Every cell within [min, max] of any cell the unit can stand on.</summary>
    public static HashSet<GridPos> AttackCells(BattleGrid grid, IEnumerable<GridPos> standCells, int min, int max)
    {
        var result = new HashSet<GridPos>();
        if (max <= 0)
            return result;
        foreach (var stand in standCells)
            foreach (var p in CellsInRange(grid, stand, min, max))
                result.Add(p);
        return result;
    }
}
