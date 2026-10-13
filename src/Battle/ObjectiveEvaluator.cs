using System.Linq;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Decides victory and defeat. For now every map is Rout (defeat all enemies) and is lost when the lord
/// falls or the player has no units left. Per-map objectives arrive in M4.
/// </summary>
public static class ObjectiveEvaluator
{
    public static BattleOutcome Evaluate(BattleState s)
    {
        bool lordFell = s.Units.Any(u => u.Faction == Faction.Player && u.IsLord && u.IsDefeated);
        if (lordFell || !s.LivingOf(Faction.Player).Any())
            return BattleOutcome.Defeat;
        if (!s.LivingOf(Faction.Enemy).Any())
            return BattleOutcome.Victory;
        return BattleOutcome.InProgress;
    }
}
