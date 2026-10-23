using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;

namespace NotFireEmblem.Units;

/// <summary>One level gained: the new level and the stat points it rolled.</summary>
public sealed record LevelUp(int Level, StatBlock Gains);

/// <summary>FE7-style experience (GDD §3 Progression, IMPLEMENTATION §5.3).</summary>
public static class ExpCalculator
{
    public const int ExpPerLevel = 100;
    public const int LevelCap = 20;
    public const int HealExp = 12;
    public const int BossKillBonus = 40;

    /// <summary>Promoted classes count as 20 levels higher per tier above the first.</summary>
    public static int EffectiveLevel(ContentDatabase db, UnitState unit) =>
        unit.Level + 20 * Math.Max(0, db.Class(unit.ClassId).Tier - 1);

    /// <summary>
    /// EXP for one combat: (31 + level difference) / 3 when damage was dealt, else 1; a kill adds
    /// 20 + 3 × the level difference (+40 for a boss). Always at least 1 and at most 100.
    /// </summary>
    public static int CombatExp(ContentDatabase db, UnitState me, UnitState foe, bool dealtDamage, bool killed, bool foeIsBoss)
    {
        if (me.Level >= LevelCap)
            return 0;
        int diff = EffectiveLevel(db, foe) - EffectiveLevel(db, me);
        int exp = dealtDamage ? Math.Max(1, (31 + diff) / 3) : 1;
        if (killed)
            exp += Math.Max(0, 20 + diff * 3) + (foeIsBoss ? BossKillBonus : 0);
        return Math.Clamp(exp, 1, 100);
    }
}

public static class LevelUpService
{
    /// <summary>
    /// Adds EXP and rolls a level-up for every 100 gained. Each stat (not Mov) rises by 1 when a roll is under
    /// the unit's personal growth + class growth; stats already at the class cap don't roll. Every level gives at
    /// least one point. Levels never heal: current HP is left alone.
    /// </summary>
    public static List<LevelUp> AddExp(ContentDatabase db, UnitState unit, int exp, IRng rng)
    {
        var levels = new List<LevelUp>();
        if (unit.Level >= ExpCalculator.LevelCap)
        {
            unit.Exp = 0;
            return levels;
        }
        unit.Exp += exp;
        while (unit.Exp >= ExpCalculator.ExpPerLevel && unit.Level < ExpCalculator.LevelCap)
        {
            unit.Exp -= ExpCalculator.ExpPerLevel;
            levels.Add(new LevelUp(unit.Level + 1, Roll(db, unit, rng)));
        }
        if (unit.Level >= ExpCalculator.LevelCap)
            unit.Exp = 0;
        return levels;
    }

    /// <summary>
    /// Raises the unit to <paramref name="level"/> (capped at the level cap) with the average gains for its growths,
    /// rounded per stat and stopped at the class caps: no dice, so a test party is the same every run. Used by the
    /// --level dev flag. Units already at or above the level are left alone.
    /// </summary>
    /// <summary>
    /// Dev single battles (--level): raises a unit the way a campaign would. Past level 10 it promotes whenever the
    /// crest tier allows (the next class back at level 1) and keeps levelling, so "level 15" at crest tier 2 is a tier 2
    /// unit at level 6, like a real Act II party, not a level 15 recruit.
    /// </summary>
    public static void RaiseLikeCampaign(ContentDatabase db, UnitState unit, int level, int crestTier)
    {
        while (level > PromotionService.MinLevel
               && PromotionService.Targets(db, unit).FirstOrDefault(t => db.Class(t).Tier <= crestTier) is { } target)
        {
            RaiseToAverage(db, unit, PromotionService.MinLevel);
            PromotionService.Promote(unit, target);
            level -= PromotionService.MinLevel - 1;
        }
        RaiseToAverage(db, unit, level);
    }

    public static void RaiseToAverage(ContentDatabase db, UnitState unit, int level)
    {
        int levels = Math.Min(level, ExpCalculator.LevelCap) - unit.Level;
        if (levels <= 0)
            return;
        var cls = db.Class(unit.ClassId);
        var current = cls.Bases + unit.Def.Bases + unit.Gains;
        var gains = default(StatBlock);
        foreach (var stat in StatBlock.All)
        {
            if (stat is Stat.Mov or Stat.Cha)
                continue;
            int growth = cls.Growths[stat] + unit.Def.Growths[stat];
            int points = (growth * levels + 50) / 100;
            if (cls.Caps[stat] > 0)
                points = Math.Clamp(cls.Caps[stat] - current[stat], 0, points);
            gains = gains.With(stat, points);
        }
        unit.Gains += gains;
        unit.Level += levels;
        unit.Exp = 0;
    }

    /// <summary>Rolls and applies one level.</summary>
    public static StatBlock Roll(ContentDatabase db, UnitState unit, IRng rng)
    {
        var cls = db.Class(unit.ClassId);
        var current = (cls.Bases + unit.Def.Bases + unit.Gains);
        var gains = default(StatBlock);
        foreach (var stat in StatBlock.All)
        {
            if (stat is Stat.Mov or Stat.Cha)
                continue;
            int growth = cls.Growths[stat] + unit.Def.Growths[stat];
            if (cls.Caps[stat] > 0 && current[stat] >= cls.Caps[stat])
                continue;
            // Growths over 100% give a guaranteed point plus a roll for the rest.
            int points = growth / 100 + (rng.Next(100) < growth % 100 ? 1 : 0);
            gains = gains.With(stat, points);
        }
        // No empty levels: if every roll missed, the highest-growth stat that isn't capped still rises.
        if (StatBlock.All.All(stat => gains[stat] == 0))
        {
            var pick = StatBlock.All.Where(stat => stat is not (Stat.Mov or Stat.Cha)
                                                   && !(cls.Caps[stat] > 0 && current[stat] >= cls.Caps[stat]))
                .OrderByDescending(stat => cls.Growths[stat] + unit.Def.Growths[stat]).Cast<Stat?>().FirstOrDefault();
            if (pick is { } stat)
                gains = gains.With(stat, 1);
        }
        unit.Gains += gains;
        unit.Level++;
        return gains;
    }
}

/// <summary>
/// Promotion (GDD §3, STORY §4.3): Lv 10+ and Rowan's crest tier at least the
/// class tier. A class's bases come from the class itself, so changing class applies the new bases automatically.
/// Tier 2 units go on to their own Crestbound class (UnitDef.Crestbound), not one shared by class.
/// </summary>
public static class PromotionService
{
    public const int MinLevel = 10;

    /// <summary>Classes the unit could promote to: its class's routes, and from tier 2 its own Crestbound class.</summary>
    public static IEnumerable<string> Targets(ContentDatabase db, UnitState unit)
    {
        var cls = db.Class(unit.ClassId);
        var targets = cls.PromotesTo.AsEnumerable();
        if (cls.Tier == 2 && unit.Def.Crestbound is { } crestbound && crestbound != unit.ClassId)
            targets = targets.Append(crestbound);
        return targets.Distinct();
    }

    public static string? CannotPromote(ContentDatabase db, UnitState unit, string targetClassId, int crestTier)
    {
        var cls = db.Class(unit.ClassId);
        if (!Targets(db, unit).Contains(targetClassId))
            return $"{cls.Name} can't become {db.Class(targetClassId).Name}.";
        var target = db.Class(targetClassId);
        if (unit.Level < MinLevel)
            return $"Needs level {MinLevel}.";
        if (crestTier < target.Tier)
            return $"Needs crest tier {target.Tier} (now {crestTier}).";
        return null;
    }

    /// <summary>Changes class and resets the level (EXP keeps accruing from level 1).</summary>
    public static void Promote(UnitState unit, string targetClassId)
    {
        unit.ClassId = targetClassId;
        unit.Level = 1;
        unit.Exp = 0;
    }
}

/// <summary>Earlier classes a unit's art can fall back to (Crestbound → its tier 2 → its tier 1), nearest first.</summary>
public static class Outfits
{
    public static IReadOnlyList<string> Earlier(ContentDatabase db, string classId)
    {
        var chain = new List<string>();
        string? current = classId;
        for (int i = 0; i < 3 && current is not null; i++)
        {
            var cls = db.Classes.GetValueOrDefault(current);
            current = cls?.PromotesFrom ?? db.Classes.Values.Where(c => c.PromotesTo.Contains(current!)).Select(c => c.Id).OrderBy(id => id, StringComparer.Ordinal).FirstOrDefault();
            if (current is not null && !chain.Contains(current))
                chain.Add(current);
        }
        return chain;
    }
}

/// <summary>Arts, spells and crest skills a unit can use.</summary>
public static class Abilities
{
    public static IEnumerable<ArtDef> Arts(ContentDatabase db, UnitState unit) =>
        (unit.Def.Arts ?? Array.Empty<string>()).Concat(db.Class(unit.ClassId).Arts ?? Array.Empty<string>())
        .Distinct().Select(db.Art);

    public static IEnumerable<SpellDef> Spells(ContentDatabase db, UnitState unit) =>
        (unit.Def.Spells ?? Array.Empty<string>()).Concat(db.Class(unit.ClassId).Spells ?? Array.Empty<string>())
        .Distinct().Select(db.Spell);
}

public enum CrestSkill { Ward, Rally, Mend, Dawn }

/// <summary>Rowan's crest skills (STORY §4.3), unlocked by crest tier, with uses per map.</summary>
public static class CrestSkills
{
    public static readonly StatBlock WardBonus = new(0, 0, 0, 0, 0, 0, 4, 4, 0);
    public static readonly StatBlock RallyBonus = new(0, 2, 2, 2, 2, 2, 2, 2, 0);
    public const int MendPercent = 30;
    public const int AreaRadius = 2;

    public static int Tier(CrestSkill skill) => (int)skill + 1;

    public static int Uses(CrestSkill skill) => skill == CrestSkill.Ward ? 2 : 1;

    public static string UseKey(CrestSkill skill) => $"crest:{skill}";

    public static string Describe(CrestSkill skill) => skill switch
    {
        CrestSkill.Ward => "Ward: an adjacent ally gets +4 Def/Res until your next phase.",
        CrestSkill.Rally => "Rally: allies within 2 get +2 to all stats for a turn.",
        CrestSkill.Mend => "Mend: allies within 2 heal 30% of their max HP.",
        CrestSkill.Dawn => "Dawn: an adjacent ally who has acted may act again.",
        _ => skill.ToString(),
    };

    /// <summary>The crest skills a lord has at this crest tier.</summary>
    public static IEnumerable<CrestSkill> Available(int crestTier) =>
        Enum.GetValues<CrestSkill>().Where(s => Tier(s) <= crestTier);
}

/// <summary>Support pairs, points and ranks (GDD §5).</summary>
public static class Supports
{
    // Pacing (session 13, user: "A shouldn't come until halfway through Act II"): a pair gains at most
    // SurvivePoints + AdjacentCap = 5 points a battle, so the thresholds in data/units are battles together × 5.
    public const int AdjacentPoints = 1;
    /// <summary>Most points a pair can earn by standing together in one battle.</summary>
    public const int AdjacentCap = 3;
    public const int SurvivePoints = 2;
    public static readonly string[] RankNames = { "-", "C", "B", "A" };

    /// <summary>Order-independent key for a pair of unit ids.</summary>
    public static string PairKey(string a, string b) => string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

    /// <summary>The support definition between two units (from either side), or null.</summary>
    public static SupportDef? Find(ContentDatabase db, string a, string b)
    {
        if (db.Units.TryGetValue(a, out var ua) && ua.Supports?.FirstOrDefault(s => s.PartnerId == b) is { } fromA)
            return fromA;
        if (db.Units.TryGetValue(b, out var ub) && ub.Supports?.FirstOrDefault(s => s.PartnerId == a) is { } fromB)
            return fromB with { PartnerId = b };
        return null;
    }

    /// <summary>The conversation id for reaching <paramref name="rank"/> (1 C, 2 B, 3 A): support_a_b_C.</summary>
    public static string SceneId(string a, string b, int rank)
    {
        var pair = PairKey(a, b).Split('|');
        return $"support_{pair[0]}_{pair[1]}_{RankNames[rank]}";
    }

    /// <summary>The next rank a pair can view, if their points reach its threshold.</summary>
    public static int? Unlocked(ContentDatabase db, string a, string b, int points, int currentRank)
    {
        if (currentRank >= 3 || Find(db, a, b) is not { } def)
            return null;
        int next = currentRank + 1;
        return points >= def.Threshold(next) ? next : null;
    }
}
