using NotFireEmblem.Core;

namespace NotFireEmblem.Units;

/// <summary>The only place that combines stat sources.</summary>
public static class StatCalculator
{
    /// <summary>
    /// Class bases + personal bases + gains, clamped to the class caps, then equipment bonuses (body armour and
    /// accessory) on top, so gear can push a capped unit further. Mov comes from the class.
    /// </summary>
    public static StatBlock Effective(ContentDatabase db, UnitState unit)
    {
        var cls = db.Class(unit.ClassId);
        var stats = (cls.Bases + unit.Def.Bases + unit.Gains).ClampTo(cls.Caps);
        return stats + EquipmentBonuses(db, unit);
    }

    /// <summary>The summed stat bonuses of the unit's body armour and accessory.</summary>
    public static StatBlock EquipmentBonuses(ContentDatabase db, UnitState unit)
    {
        var bonus = default(StatBlock);
        if (db.TryArmor(unit.ArmorId) is { } armor)
            bonus += armor.Bonuses;
        if (db.TryArmor(unit.AccessoryId) is { } accessory)
            bonus += accessory.Bonuses;
        return bonus;
    }

    /// <summary>Weight of worn armour and accessory; it adds to weapon weight when working out attack speed.</summary>
    public static int ArmorWeight(ContentDatabase db, UnitState unit) =>
        (db.TryArmor(unit.ArmorId)?.Weight ?? 0) + (db.TryArmor(unit.AccessoryId)?.Weight ?? 0);

    public static MovementType MovementTypeOf(ContentDatabase db, UnitState unit) => db.Class(unit.ClassId).MovementType;

    /// <summary>Attack speed: Spd − max(0, weapon Wt + armour Wt − Str/5). Strong units carry heavy gear better.</summary>
    public static int AttackSpeed(StatBlock stats, WeaponDef? weapon, int armorWeight = 0) =>
        stats.Spd - System.Math.Max(0, (weapon?.Weight ?? 0) + armorWeight - stats.Str / 5);

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
