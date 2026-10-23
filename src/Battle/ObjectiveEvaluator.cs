using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Decides victory and defeat from the map's <see cref="Objectives"/> (GDD §3 Map objectives). Checked after every
/// command and at every phase change. Defeat is checked first: losing the lord loses the battle even if the same
/// blow would have won it.
/// </summary>
public static class ObjectiveEvaluator
{
    public static BattleOutcome Evaluate(BattleState s)
    {
        if (s.ForcedOutcome is { } forced)
            return forced;
        var goals = s.Map.Goals;

        bool Fell(BattleUnit? u) => u is not null && u.IsDefeated;
        bool lordFell = s.Units.Any(u => u.Faction == Faction.Player && u.IsLord && u.IsDefeated);
        if (lordFell || goals.MustSurvive.Any(k => Fell(s.ByKey(k))))
            return BattleOutcome.Defeat;
        if (goals.Protect.Any(r => s.TagLosses.GetValueOrDefault(r.Tag) >= r.MaxLosses))
            return BattleOutcome.Defeat;
        bool anyoneEscaped = s.Units.Any(u => u.Faction == Faction.Player && u.Exit == MapExit.Escaped);
        if (!s.LivingOf(Faction.Player).Any() && !anyoneEscaped)
            return BattleOutcome.Defeat;
        if (goals.TurnLimit > 0 && s.Turn > goals.TurnLimit)
            return BattleOutcome.Defeat;

        // "or boss KEY": felling that boss wins whatever the main goal is.
        if (goals.Victory != VictoryKind.DefeatBoss && goals.BossKey is { } alt && BossDown(s, alt))
            return BattleOutcome.Victory;
        bool won = goals.Victory switch
        {
            VictoryKind.Rout => !s.LivingOf(Faction.Enemy).Any(),
            VictoryKind.DefeatBoss => BossDown(s, goals.BossKey!),
            VictoryKind.Seize => s.Seized,
            VictoryKind.Survive => s.SurviveComplete,
            VictoryKind.Escape => EscapeDone(s),
            _ => false,
        };
        return won ? BattleOutcome.Victory : BattleOutcome.InProgress;
    }

    private static bool BossDown(BattleState s, string key) => s.AllByKey(key).Any() && s.AllByKey(key).All(u => !u.IsOnMap);

    /// <summary>"Defeat Halvric", or for a group sharing the key "Defeat the Agitators (3 left)".</summary>
    private static string DescribeBoss(BattleState s, string key)
    {
        var all = s.AllByKey(key).ToList();
        if (all.Count <= 1)
            return $"Defeat {all.FirstOrDefault()?.Name ?? "the boss"}";
        string name = all[0].Name.Split(' ')[^1];
        return $"Defeat the {name}s ({all.Count(u => u.IsOnMap)} left)";
    }

    private static bool EscapeDone(BattleState s)
    {
        var goals = s.Map.Goals;
        bool lordOut = s.Units.Where(u => u.Faction == Faction.Player && u.IsLord).All(u => u.Exit == MapExit.Escaped);
        if (goals.EscapeAll)
            // Units a script took off the map (Ch 15: Aldric stays behind) don't have to escape.
            return lordOut && s.Units.Where(u => u.Faction == Faction.Player && !u.IsDefeated && u.Exit != MapExit.Departed)
                .All(u => u.Exit == MapExit.Escaped);
        bool keysOut = goals.EscapeKeys.All(k => s.ByKey(k) is { Exit: MapExit.Escaped });
        return lordOut && keysOut;
    }

    /// <summary>The goal line for the HUD.</summary>
    public static string Describe(BattleState s)
    {
        var goals = s.Map.Goals;
        string goal = goals.Victory switch
        {
            VictoryKind.Rout => $"Rout the enemy ({s.LivingOf(Faction.Enemy).Count()} left)",
            VictoryKind.DefeatBoss => DescribeBoss(s, goals.BossKey!),
            VictoryKind.Seize => "Seize the marked tile with your lord",
            VictoryKind.Survive => $"Survive {System.Math.Max(0, goals.Turns - s.Turn + 1)} more turn(s)",
            VictoryKind.Escape when goals.EscapeAll => "Escape: everyone to the exit, your lord last",
            VictoryKind.Escape => "Escape: " + string.Join(", ", EscapeNames(s)) + " to the exit",
            _ => "",
        };
        if (goals.Victory != VictoryKind.DefeatBoss && goals.BossKey is { } alt)
            goal += ",\nor " + DescribeBoss(s, alt).ToLowerInvariant()[..1] + DescribeBoss(s, alt)[1..];
        var extra = goals.MustSurvive.Select(k => s.ByKey(k)?.Name).OfType<string>().Select(n => $"{n} must survive")
            .Concat(goals.Protect.Select(r => $"Protect the {r.Tag}s ({s.TagLosses.GetValueOrDefault(r.Tag)}/{r.MaxLosses} lost)"));
        if (goals.TurnLimit > 0)
            extra = extra.Append($"Turn limit {goals.TurnLimit}");
        var lines = extra.ToList();
        return lines.Count == 0 ? goal : goal + "\n" + string.Join("\n", lines);
    }

    private static System.Collections.Generic.IEnumerable<string> EscapeNames(BattleState s)
    {
        var lords = s.Units.Where(u => u.Faction == Faction.Player && u.IsLord).Select(u => u.Name);
        return lords.Concat(s.Map.Goals.EscapeKeys.Select(k => s.ByKey(k)?.Name).OfType<string>()).Distinct();
    }
}
