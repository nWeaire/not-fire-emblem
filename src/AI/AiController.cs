using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.AI;

/// <summary>Scoring weights. Hard difficulty can swap in <see cref="Aggressive"/>.</summary>
public sealed record AiWeights(
    double Damage = 1.0,
    double Kill = 40.0,
    double CounterDamage = 0.7,
    double SelfDeath = 60.0,
    double Danger = 1.5,
    double LowHpTarget = 8.0,
    double LordTarget = 8.0,
    double HealerTarget = 8.0,
    double NoCounterBonus = 4.0,
    double TerrainAvoid = 0.1,
    double TerrainDef = 1.0,
    double ArtCost = 3.0,
    double ProtectTarget = 4.0)
{
    public static readonly AiWeights Default = new();
    public static readonly AiWeights Aggressive = new(CounterDamage: 0.3, SelfDeath: 25.0, Danger: 0.5);
}

/// <summary>An AI unit's chosen command, with its score (attacks only) for ordering units within a phase.</summary>
public sealed record AiDecision(BattleUnit Unit, IBattleCommand Command, double? AttackScore);

/// <summary>Pure, deterministic AI. Picks one command per unit using the same forecasts the player sees.</summary>
public static class AiController
{
    /// <summary>How far a Boss may step from its starting cell to attack, unless its spawn sets a Leash.</summary>
    public const int BossLeash = 1;

    /// <summary>
    /// Picks the next unit to act in the current phase and its command: units that can attack go first
    /// (highest score first), then the rest in id order. Returns null when no unit is left to act.
    /// </summary>
    public static AiDecision? NextDecision(BattleState s, AiWeights? weights = null)
    {
        var decisions = s.LivingOf(s.Phase)
            .Where(u => u.ActionState != ActionState.Done)
            .Select(u => Decide(s, u, weights))
            .ToList();
        return decisions.Where(d => d.AttackScore is not null).OrderByDescending(d => d.AttackScore).FirstOrDefault()
               ?? decisions.FirstOrDefault();
    }

    public static AiDecision Decide(BattleState s, BattleUnit unit, AiWeights? weights = null)
    {
        weights ??= AiWeights.Default;
        var reach = s.Reach(unit);

        IEnumerable<GridPos> attackFrom = unit.AiProfile switch
        {
            AiProfile.Stationary => new[] { unit.Pos },
            AiProfile.Boss => reach.StopCells.Where(c => c.DistanceTo(unit.Home) <= (unit.Leash > 0 ? unit.Leash : BossLeash)),
            _ => reach.StopCells,
        };

        var best = BestAttack(s, unit, attackFrom, weights);
        if (best is { } attack)
            return new AiDecision(unit, attack.Command, attack.Score);

        // Stationary and Boss units hold position; Guard units stay asleep until a foe comes within their alert range.
        if (unit.AiProfile is AiProfile.Stationary or AiProfile.Boss || unit.AiProfile == AiProfile.GuardRegion && !Alerted(s, unit))
            return Wait(unit);
        if (unit.AiProfile == AiProfile.Healer && BestHeal(s, unit, reach) is { } heal)
            return new AiDecision(unit, heal, null);
        return new AiDecision(unit, new ActCommand(unit.Id, ApproachCell(s, unit, reach), new WaitAction()), null);
    }

    /// <summary>How many cells beyond its attack reach (move + weapon range) a guard notices a foe and moves in.</summary>
    public const int GuardAlertMargin = 2;

    /// <summary>
    /// A guard wakes when a foe stands within its move plus weapon range plus <see cref="GuardAlertMargin"/> (straight-
    /// line cells). Before, guards only acted on foes they could already hit, so the party had to walk right up to
    /// them (session 7 playtest).
    /// </summary>
    public static bool Alerted(BattleState s, BattleUnit unit)
    {
        int range = CombatCalculator.UsableWeapons(s.Db, unit.State).Select(w => w.MaxRange).DefaultIfEmpty(1).Max();
        int alert = s.Stats(unit).Mov + range + GuardAlertMargin;
        return s.Living.Any(u => Picks(s, unit, u) && u.Pos.DistanceTo(unit.Pos) <= alert);
    }

    /// <summary>
    /// Whether the AI would go after <paramref name="target"/>: any hostile unit, except that Neutral units (rioters)
    /// only start fights with Ally units (civilians), never the player's army, and nobody goes after the keys its
    /// spawn ignores ("civilian", "gull|halde") while those units aren't under the player's command. Nobody goes
    /// after a unit that can't be hurt (Ch 15's Morvane), nor, on the player's side, a recruit a talk would win over
    /// (session 17: autoplay chose to attack Ch 11's Ysra, the rules refused, and the whole Player Phase ended).
    /// </summary>
    public static bool Picks(BattleState s, BattleUnit unit, BattleUnit target) =>
        unit.Faction.IsHostileTo(target.Faction) && !target.Invulnerable && !ActionRules.SparedRecruit(s, unit, target)
        && (unit.Faction != Faction.Neutral || target.Faction == Faction.Ally)
        && (unit.Ignores is null || target.Faction == Faction.Player || !unit.Ignores.Split('|').Contains(target.Key));

    /// <summary>Healer profile: heal the most injured friend it can reach.</summary>
    private static IBattleCommand? BestHeal(BattleState s, BattleUnit unit, ReachMap reach)
    {
        foreach (var spell in ActionRules.ReadySpells(s, unit))
        {
            var best = reach.StopCells.OrderBy(c => c.Y).ThenBy(c => c.X)
                .SelectMany(c => ActionRules.HealTargets(s, unit, c, spell.Id).Select(t => (Cell: c, Target: t)))
                .OrderBy(x => x.Target.CurrentHp * 100 / System.Math.Max(1, s.Stats(x.Target).HP))
                .FirstOrDefault();
            if (best.Target is not null)
                return new ActCommand(unit.Id, best.Cell, new HealAction(best.Target.Id, spell.Id));
        }
        return null;
    }

    private static AiDecision Wait(BattleUnit unit) => new(unit, new ActCommand(unit.Id, unit.Pos, new WaitAction()), null);

    private static (IBattleCommand Command, double Score)? BestAttack(BattleState s, BattleUnit unit, IEnumerable<GridPos> cells, AiWeights w)
    {
        var choices = CombatCalculator.AttackChoices(s.Db, unit).ToList();
        if (choices.Count == 0)
            return null;
        // Under fog, the AI only attacks what its side can see.
        var hostiles = s.Living.Where(t => Picks(s, unit, t) && s.CanSee(unit.Faction, t)).ToList();
        var danger = new Lazy<Dictionary<GridPos, int>>(() => ThreatCounts(s, unit.Faction));

        (IBattleCommand Command, double Score)? best = null;
        // Sorted so ties break the same way every time.
        foreach (var cell in cells.OrderBy(c => c.Y).ThenBy(c => c.X))
        {
            foreach (var target in hostiles)
            {
                int distance = cell.DistanceTo(target.Pos);
                foreach (var choice in choices.Where(c => CombatCalculator.InRange(s.Db, c, distance)))
                {
                    var forecast = CombatCalculator.Forecast(s, unit, cell, target, choice);
                    double score = Score(s, unit, target, cell, forecast, w, danger);
                    // Arts are limited: only spend one when it is clearly better than a plain attack.
                    if (choice is ArtAttack)
                        score -= w.ArtCost;
                    if (best is null || score > best.Value.Score + 1e-9)
                        best = (new ActCommand(unit.Id, cell, new AttackAction(target.Id, choice)), score);
                }
            }
        }
        return best;
    }

    public static double Score(BattleState s, BattleUnit unit, BattleUnit target, GridPos cell, CombatForecast f, AiWeights w,
        Lazy<Dictionary<GridPos, int>>? danger = null)
    {
        var odds = CombatOdds.Evaluate(f);
        double score = w.Damage * odds.ExpectedDamageToDefender
                       + w.Kill * odds.DefenderDies
                       - w.CounterDamage * odds.ExpectedDamageToAttacker
                       - w.SelfDeath * odds.AttackerDies;

        double hpRatio = f.Defender.MaxHp > 0 ? f.Defender.Hp / (double)f.Defender.MaxHp : 1;
        score += w.LowHpTarget * (1 - hpRatio);
        if (target.IsLord)
            score += w.LordTarget;
        if (IsProtected(s, target))
            score += w.ProtectTarget;
        if (s.Db.Class(target.State.ClassId).Usable.Contains(WeaponType.WhiteMagic))
            score += w.HealerTarget;
        if (!f.Defender.CanStrike)
            score += w.NoCounterBonus;

        var terrain = s.Grid[cell].Terrain;
        score += w.TerrainAvoid * terrain.Avoid + w.TerrainDef * terrain.Def;
        if (danger is not null && danger.Value.TryGetValue(cell, out int threats))
            score -= w.Danger * Math.Max(0, threats - 1); // the target itself is always one threat
        return score;
    }

    /// <summary>
    /// How many steps nearer a unit the player must protect (a map's "protect TAG N") counts as when choosing where
    /// to head. Session 9 playtest: Ch 1's Gloam walked past the villagers and piled onto Rowan.
    /// </summary>
    public const int ProtectPull = 2;

    /// <summary>Whether the map's defeat conditions make <paramref name="unit"/> one the player must protect.</summary>
    public static bool IsProtected(BattleState s, BattleUnit unit) =>
        unit.Tag is { } tag && s.Map.Goals.Protect.Any(p => p.Tag == tag);

    /// <summary>For each cell, how many units hostile to <paramref name="faction"/> could attack it next phase.</summary>
    public static Dictionary<GridPos, int> ThreatCounts(BattleState s, Faction faction)
    {
        var counts = new Dictionary<GridPos, int>();
        foreach (var enemy in s.Living.Where(u => u.Faction.IsHostileTo(faction)))
            foreach (var cell in BattleQueries.AttackCells(s, enemy))
                counts[cell] = counts.GetValueOrDefault(cell) + 1;
        return counts;
    }

    /// <summary>
    /// Where to move when nothing can be attacked: as far as possible along the terrain path toward the
    /// nearest hostile unit (units the player must protect count as <see cref="ProtectPull"/> steps nearer);
    /// falls back to the reachable cell closest to it.
    /// </summary>
    public static GridPos ApproachCell(BattleState s, BattleUnit unit, ReachMap reach)
    {
        var moveType = s.MoveType(unit);
        IReadOnlyList<GridPos>? bestPath = null;
        BattleUnit? goal = null;
        int bestLength = int.MaxValue;
        // Prefer targets the side can see; under fog with nothing in sight, head for the nearest anyway.
        var hostiles = s.Living.Where(t => Picks(s, unit, t)).ToList();
        var seen = hostiles.Where(t => s.CanSee(unit.Faction, t)).ToList();
        foreach (var target in (seen.Count > 0 ? seen : hostiles).OrderBy(t => t.Id, StringComparer.Ordinal))
        {
            var path = Pathfinder.FindPath(s.Grid, unit.Pos, target.Pos, moveType);
            int length = path is null ? int.MaxValue : path.Count - (IsProtected(s, target) ? ProtectPull : 0);
            if (path is not null && length < bestLength)
            {
                bestPath = path;
                goal = target;
                bestLength = length;
            }
        }
        if (bestPath is null || goal is null)
            return unit.Pos;

        for (int i = bestPath.Count - 1; i > 0; i--)
            if (reach.CanStop(bestPath[i]))
                return bestPath[i];

        return reach.StopCells
            .OrderBy(c => c.DistanceTo(goal.Pos))
            .ThenBy(c => reach.CostTo(c))
            .ThenBy(c => c.Y).ThenBy(c => c.X)
            .First();
    }
}
