using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>One stop of the objective tour: a caption and the cells to mark (none: the camera stays put).</summary>
public sealed record TourStop(string Caption, IReadOnlyList<GridPos> Cells);

/// <summary>
/// What the battle shows before turn 1 (session 10): the win condition and where it is (exit cells, the seize tile,
/// the boss), the units that must not fall, then the map's own highlights (e.g. Ch 5's gate and Binder).
/// </summary>
public static class ObjectiveTour
{
    public static List<TourStop> Stops(BattleState s)
    {
        var goals = s.Map.Goals;
        var stops = new List<TourStop>();
        IReadOnlyList<GridPos> At(BattleUnit? u) => u is { IsOnMap: true } ? new[] { u.Pos } : Array.Empty<GridPos>();
        string lords = string.Join(" and ", s.LivingOf(Faction.Player).Where(u => u.IsLord).Select(u => u.Name).DefaultIfEmpty("your lord"));

        switch (goals.Victory)
        {
            case VictoryKind.Rout:
                stops.Add(new TourStop("Victory: defeat every enemy.", Array.Empty<GridPos>()));
                break;
            case VictoryKind.DefeatBoss:
                var bosses = s.AllByKey(goals.BossKey!).Where(u => u.IsOnMap).ToList();
                stops.Add(bosses.Count > 1
                    ? new TourStop($"Victory: defeat all {bosses.Count} {bosses[0].Name.Split(' ')[^1]}s.", bosses.Select(u => u.Pos).ToList())
                    : new TourStop($"Victory: defeat {bosses.FirstOrDefault()?.Name ?? "the boss"}.", At(bosses.FirstOrDefault())));
                break;
            case VictoryKind.Seize:
                stops.Add(new TourStop($"Victory: {lords} must seize this tile.", goals.Cells));
                break;
            case VictoryKind.Survive:
                stops.Add(new TourStop($"Victory: survive {goals.Turns} turns.", Array.Empty<GridPos>()));
                break;
            case VictoryKind.Escape:
                var names = s.Units.Where(u => u.Faction == Faction.Player && u.IsLord).Select(u => u.Name)
                    .Concat(goals.EscapeKeys.Select(k => s.ByKey(k)?.Name).OfType<string>()).Distinct();
                stops.Add(new TourStop($"Victory: get {string.Join(", ", names)} to the exit.", goals.Cells));
                break;
        }
        foreach (var key in goals.MustSurvive)
            if (s.ByKey(key) is { } unit)
                stops.Add(new TourStop($"Defeat if {unit.Name} falls.", At(unit)));
        foreach (var rule in goals.Protect)
        {
            var cells = s.Living.Where(u => u.Tag == rule.Tag && u.IsOnMap).Select(u => u.Pos).ToArray();
            stops.Add(new TourStop($"Protect the {rule.Tag}s: defeat if {rule.MaxLosses} fall.", cells));
        }
        if (goals.TurnLimit > 0)
            stops.Add(new TourStop($"Defeat if the battle runs past turn {goals.TurnLimit}.", Array.Empty<GridPos>()));
        foreach (var h in s.Map.AllHighlights)
            stops.Add(new TourStop(h.Caption, h.Key is null ? h.Cells : s.AllByKey(h.Key).SelectMany(At).ToList()));
        return stops;
    }

    /// <summary>The turn countdown for Survive maps at the start of a player phase, else null.</summary>
    public static string? Countdown(BattleState s)
    {
        var goals = s.Map.Goals;
        if (goals.Victory != VictoryKind.Survive)
            return null;
        int left = goals.Turns - s.Turn + 1;
        return left switch
        {
            <= 0 => null,
            1 => "Last turn!",
            _ => $"{left} turns to survive",
        };
    }
}
