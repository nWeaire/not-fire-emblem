using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Read-only range questions about units in a battle, shared by the UI overlays and the AI.</summary>
public static class BattleQueries
{
    public static (int Min, int Max) WeaponRange(BattleState s, BattleUnit unit)
    {
        var weapons = CombatCalculator.UsableWeapons(s.Db, unit.State).ToList();
        return weapons.Count == 0 ? (0, 0) : (weapons.Min(w => w.MinRange), weapons.Max(w => w.MaxRange));
    }

    /// <summary>Every cell the unit could attack this turn (move, then strike).</summary>
    public static HashSet<GridPos> AttackCells(BattleState s, BattleUnit unit, ReachMap? reach = null)
    {
        reach ??= s.Reach(unit);
        var (min, max) = WeaponRange(s, unit);
        return RangeService.AttackCells(s.Grid, reach.StopCells, min, max);
    }

    /// <summary>The union of attack cells of every living unit hostile to <paramref name="viewer"/>.</summary>
    public static HashSet<GridPos> DangerZone(BattleState s, Faction viewer)
    {
        var cells = new HashSet<GridPos>();
        foreach (var unit in s.Living.Where(u => u.Faction.IsHostileTo(viewer)))
            cells.UnionWith(AttackCells(s, unit));
        return cells;
    }

    /// <summary>Hostile units the unit could hit from <paramref name="from"/> with any usable weapon.</summary>
    public static IEnumerable<BattleUnit> TargetsFrom(BattleState s, BattleUnit unit, GridPos from)
    {
        var weapons = CombatCalculator.UsableWeapons(s.Db, unit.State).ToList();
        return s.Living.Where(t => t.Faction.IsHostileTo(unit.Faction)
                                   && weapons.Any(w => CombatCalculator.InRange(w, from.DistanceTo(t.Pos))));
    }
}
