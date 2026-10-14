using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Read-only range questions about units in a battle, shared by the UI overlays and the AI.</summary>
public static class BattleQueries
{
    /// <summary>The unit's attack range over every weapon and ready art (an art can extend a weapon's reach).</summary>
    public static (int Min, int Max) WeaponRange(BattleState s, BattleUnit unit)
    {
        var weapons = CombatCalculator.AttackChoices(s.Db, unit).Select(c => CombatCalculator.EffectiveWeapon(s.Db, c)).ToList();
        return weapons.Count == 0 ? (0, 0) : (weapons.Min(w => w.MinRange), weapons.Max(w => w.MaxRange));
    }

    /// <summary>Every cell the unit could attack this turn (move, then strike).</summary>
    public static HashSet<GridPos> AttackCells(BattleState s, BattleUnit unit, ReachMap? reach = null)
    {
        reach ??= s.Reach(unit);
        var (min, max) = WeaponRange(s, unit);
        return RangeService.AttackCells(s.Grid, reach.StopCells, min, max);
    }

    /// <summary>The union of attack cells of every living unit hostile to <paramref name="viewer"/> that it can see.</summary>
    public static HashSet<GridPos> DangerZone(BattleState s, Faction viewer)
    {
        var cells = new HashSet<GridPos>();
        foreach (var unit in s.Living.Where(u => u.Faction.IsHostileTo(viewer) && s.CanSee(viewer, u) && u.StunnedPhases == 0))
            cells.UnionWith(AttackCells(s, unit));
        return cells;
    }

    /// <summary>Visible hostile units the unit could hit from <paramref name="from"/> with any weapon or art.</summary>
    public static IEnumerable<BattleUnit> TargetsFrom(BattleState s, BattleUnit unit, GridPos from) =>
        ActionRules.AttackTargets(s, unit, from);
}
