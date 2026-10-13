using NotFireEmblem.Core;

namespace NotFireEmblem.Units;

/// <summary>The only place that combines stat sources.</summary>
public static class StatCalculator
{
    /// <summary>Class bases + personal bases + gains, clamped to the class caps. Mov comes from the class.</summary>
    public static StatBlock Effective(ContentDatabase db, UnitState unit)
    {
        var cls = db.Class(unit.ClassId);
        return (cls.Bases + unit.Def.Bases + unit.Gains).ClampTo(cls.Caps);
    }

    public static MovementType MovementTypeOf(ContentDatabase db, UnitState unit) => db.Class(unit.ClassId).MovementType;

    /// <summary>Attack speed: Spd − max(0, Wt − Str/5).</summary>
    public static int AttackSpeed(StatBlock stats, WeaponDef? weapon) =>
        stats.Spd - (weapon is null ? 0 : System.Math.Max(0, weapon.Weight - stats.Str / 5));

    /// <summary>
    /// Level-up gains for a generic (non-roster) unit at the given level, using expected growth values
    /// rather than rolls so enemy stats are stable between plays.
    /// </summary>
    public static StatBlock GenericGains(ClassDef cls, StatBlock personalGrowths, int level)
    {
        var gains = default(StatBlock);
        foreach (var s in StatBlock.All)
        {
            if (s == Stat.Mov)
                continue;
            gains = gains.With(s, (level - 1) * (cls.Growths[s] + personalGrowths[s]) / 100);
        }
        return gains;
    }
}
