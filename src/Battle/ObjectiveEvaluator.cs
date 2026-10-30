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
            VictoryKind.Rout => !RoutFoes(s).Any(),
            VictoryKind.DefeatBoss => BossDown(s, goals.BossKey!),
            VictoryKind.Seize => s.Seized,
            VictoryKind.Survive => s.SurviveComplete,
            VictoryKind.Escape => EscapeDone(s),
            _ => false,
        };
        return won ? BattleOutcome.Victory : BattleOutcome.InProgress;
    }

    /// <summary>
    /// The enemies a rout still needs beaten. A recruit nobody may attack (<see cref="ActionRules.SparedRecruit"/>)
    /// doesn't count: once only such units are left the field is won, and they go unrecruited (session 17: Ch 11 couldn't
    /// end until the player talked to Ysra).
    /// </summary>
    public static IEnumerable<BattleUnit> RoutFoes(BattleState s) =>
        s.LivingOf(Faction.Enemy).Where(e => EventDirector.Recruiter(s, e) is null);

    /// <summary>Recruitable units still on the enemy side, as the battle ends: the ones the player missed.</summary>
    public static IEnumerable<BattleUnit> MissedRecruits(BattleState s) =>
        s.LivingOf(Faction.Enemy).Where(e => EventDirector.Recruiter(s, e) is not null);

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
            // Only units still on the map have to escape: one a script took off (Ch 15: Aldric stays behind) or one
            // that retreated wounded doesn't (Ch 12 waited forever for a retreated Aldric).
            return lordOut && !s.Units.Any(u => u.Faction == Faction.Player && u.IsOnMap);
        bool keysOut = goals.EscapeKeys.All(k => s.ByKey(k) is { Exit: MapExit.Escaped });
        return lordOut && keysOut;
    }

    /// <summary>The goal line for the HUD.</summary>
    public static string Describe(BattleState s)
    {
        var goals = s.Map.Goals;
        string goal = goals.Victory switch
        {
            VictoryKind.Rout => $"Rout the enemy ({RoutFoes(s).Count()} left)",
            VictoryKind.DefeatBoss => DescribeBoss(s, goals.BossKey!),
            VictoryKind.Seize => "Seize the marked tile with your lord",
            VictoryKind.Survive => $"Survive {System.Math.Max(0, goals.Turns - s.Turn + 1)} more turn(s)",
            // Session 17 (user): just "Escape!" (the exit is marked on the map, and Rowan's Escape waits for the rest).
            VictoryKind.Escape when goals.EscapeAll => "Escape!",
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
