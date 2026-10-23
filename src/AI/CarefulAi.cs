using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.AI;

/// <summary>
/// Self-preservation the plain attack AI lacks: healing friends, drinking vulneraries and pulling back when hurt.
/// Used by the dev autoplay for the player's side, which otherwise threw a wounded lord at the enemy.
/// </summary>
public static class CarefulAi
{
    /// <summary>A friend below this share of HP is worth a heal spell.</summary>
    public const double HealBelow = 0.75;

    /// <summary>
    /// Heals the most hurt friend in reach (the lord first when tied), or null. With <paramref name="safeOnly"/> (a
    /// healer who must survive, like Ch 10's Seraphine) it only heals from cells no enemy can reach next phase.
    /// </summary>
    public static IBattleCommand? Heal(BattleState s, BattleUnit unit, ReachMap reach, bool safeOnly = false)
    {
        var threats = new System.Lazy<Dictionary<GridPos, int>>(() => Threats(s, unit.Faction));
        foreach (var spell in ActionRules.ReadySpells(s, unit))
        {
            var best = reach.StopCells.OrderBy(c => c.Y).ThenBy(c => c.X)
                .SelectMany(c => ActionRules.HealTargets(s, unit, c, spell.Id).Select(t => (Cell: c, Target: t)))
                .Where(x => HpRatio(s, x.Target) < HealBelow && (!safeOnly || threats.Value.GetValueOrDefault(x.Cell) == 0))
                .OrderBy(x => HpRatio(s, x.Target))
                .ThenByDescending(x => x.Target.IsLord)
                .ThenBy(x => threats.Value.GetValueOrDefault(x.Cell))
                .FirstOrDefault();
            if (best.Target is not null)
                return new ActCommand(unit.Id, best.Cell, new HealAction(best.Target.Id, spell.Id));
        }
        return null;
    }

    /// <summary>
    /// Below <paramref name="threshold"/> of its HP the unit retreats to the safest cell it can reach and drinks a
    /// healing item there if it has one. With <paramref name="needsItem"/>, it only pulls back when it can heal.
    /// </summary>
    public static IBattleCommand? Recover(BattleState s, BattleUnit unit, ReachMap reach, double threshold, bool needsItem)
    {
        if (HpRatio(s, unit) >= threshold)
            return null;
        var item = ActionRules.UsableItems(s, unit).FirstOrDefault(i => i.Kind == ItemKind.Heal);
        // Without an item, pulling back only helps while enemies can reach the unit; otherwise it would never advance.
        if (item is null && (needsItem || Threats(s, unit.Faction).GetValueOrDefault(unit.Pos) == 0))
            return null;
        var cell = SafestCell(s, unit, reach);
        return new ActCommand(unit.Id, cell, item is null ? new WaitAction() : new UseItemAction(item.Id));
    }

    /// <summary>
    /// A unit with no weapon (Mira before her tome) can't fight back, so it doesn't advance: it waits on the reachable
    /// cell the fewest enemies can hit, staying close to where it is. Null for armed units.
    /// </summary>
    public static IBattleCommand? Shelter(BattleState s, BattleUnit unit, ReachMap reach)
    {
        if (s.Db.TryWeapon(unit.State.EquippedItemId) is not null)
            return null;
        var threats = Threats(s, unit.Faction);
        var cell = reach.StopCells
            .OrderBy(c => threats.GetValueOrDefault(c))
            .ThenBy(c => c.DistanceTo(unit.Pos))
            .ThenByDescending(c => s.Grid[c].Terrain.Def + s.Grid[c].Terrain.Avoid / 10)
            .First();
        return new ActCommand(unit.Id, cell, new WaitAction());
    }

    /// <summary>The reachable cell the fewest enemies can hit next phase; ties go to cells near a healer, then cover.</summary>
    public static GridPos SafestCell(BattleState s, BattleUnit unit, ReachMap reach)
    {
        var threats = Threats(s, unit.Faction);
        var healers = s.LivingOf(unit.Faction)
            .Where(f => f != unit && ActionRules.ReadySpells(s, f).Any()).Select(f => f.Pos).ToList();
        return reach.StopCells
            .OrderBy(c => threats.GetValueOrDefault(c))
            .ThenBy(c => healers.Count == 0 ? 0 : healers.Min(h => h.DistanceTo(c)))
            .ThenByDescending(c => s.Grid[c].Terrain.Def + s.Grid[c].Terrain.Avoid / 10)
            .ThenBy(c => c.Y).ThenBy(c => c.X)
            .First();
    }

    private static double HpRatio(BattleState s, BattleUnit u) => u.CurrentHp / (double)System.Math.Max(1, s.Stats(u).HP);

    private static Dictionary<GridPos, int> Threats(BattleState s, Faction faction) => AiController.ThreatCounts(s, faction);
}
