using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Combat;

/// <summary>Pure, deterministic combat maths (GDD §3).</summary>
public static class CombatCalculator
{
    public const int FollowUpThreshold = 4;
    public const int CritMultiplier = 3;
    public const int EffectiveMultiplier = 3;
    public const int HeightHitBonus = 10;

    public static bool CanWield(ContentDatabase db, UnitState unit, WeaponDef weapon) =>
        db.Class(unit.ClassId).Usable.Contains(weapon.Type) && weapon.Might > 0
        && (weapon.PersonalTo is null || weapon.PersonalTo == unit.Def.Id);

    /// <summary>Weapons in the unit's inventory that its class can attack with.</summary>
    public static IEnumerable<WeaponDef> UsableWeapons(ContentDatabase db, UnitState unit) =>
        unit.Inventory.Select(db.TryWeapon).OfType<WeaponDef>().Where(w => CanWield(db, unit, w)).Distinct();

    /// <summary>The weapon the unit counters with: its equipped weapon, else its first usable one.</summary>
    public static WeaponDef? CounterWeapon(ContentDatabase db, UnitState unit)
    {
        var equipped = db.TryWeapon(unit.EquippedItemId);
        if (equipped is not null && CanWield(db, unit, equipped))
            return equipped;
        return UsableWeapons(db, unit).FirstOrDefault();
    }

    public static bool InRange(WeaponDef weapon, int distance) => distance >= weapon.MinRange && distance <= weapon.MaxRange;

    public static CombatForecast Forecast(BattleState s, BattleUnit attacker, GridPos attackerPos, BattleUnit defender, ActionChoice choice)
    {
        var attackWeapon = choice switch
        {
            WeaponAttack w => s.Db.Weapon(w.WeaponId),
            _ => throw new NotSupportedException($"Action choice {choice} is not supported yet."),
        };
        var counterWeapon = CounterWeapon(s.Db, defender.State);
        int distance = attackerPos.DistanceTo(defender.Pos);
        bool canCounter = counterWeapon is not null && InRange(counterWeapon, distance);

        var aStats = s.Stats(attacker);
        var dStats = s.Stats(defender);
        int aAs = StatCalculator.AttackSpeed(aStats, attackWeapon, StatCalculator.ArmorWeight(s.Db, attacker.State));
        int dAs = StatCalculator.AttackSpeed(dStats, counterWeapon, StatCalculator.ArmorWeight(s.Db, defender.State));

        var aSide = Side(s, true, attacker, aStats, attackWeapon, attackerPos, aAs, defender, dStats, counterWeapon, defender.Pos, dAs);
        var dSide = canCounter
            ? Side(s, false, defender, dStats, counterWeapon!, defender.Pos, dAs, attacker, aStats, attackWeapon, attackerPos, aAs)
            : null;

        var strikes = new List<PlannedStrike> { aSide };
        if (dSide is not null)
            strikes.Add(dSide);
        if (aAs - dAs >= FollowUpThreshold)
            strikes.Add(aSide);
        else if (dSide is not null && dAs - aAs >= FollowUpThreshold)
            strikes.Add(dSide);

        return new CombatForecast(
            Summary(attacker, aStats, attackWeapon, aSide, aAs, strikes.Count(x => x.ByAttacker), counterWeapon, defender, s),
            Summary(defender, dStats, counterWeapon, dSide, dAs, strikes.Count(x => !x.ByAttacker), attackWeapon, attacker, s),
            strikes,
            distance);
    }

    private static PlannedStrike Side(
        BattleState s, bool byAttacker,
        BattleUnit me, StatBlock myStats, WeaponDef myWeapon, GridPos myPos, int myAs,
        BattleUnit them, StatBlock theirStats, WeaponDef? theirWeapon, GridPos theirPos, int theirAs)
    {
        int tri = WeaponTriangle.Advantage(myWeapon.Type, theirWeapon?.Type);
        bool effective = IsEffective(s, myWeapon, them);
        bool theyFly = StatCalculator.MovementTypeOf(s.Db, them.State) == MovementType.Flying;
        var theirTerrain = s.Grid[theirPos].Terrain;
        int terrainAvoid = theyFly ? 0 : theirTerrain.Avoid;
        int terrainDef = theyFly ? 0 : theirTerrain.Def;

        int might = myWeapon.Might * (effective ? EffectiveMultiplier : 1);
        int attack = (myWeapon.Magic ? myStats.Mag : myStats.Str) + might + tri * WeaponTriangle.DamageBonus;
        int defense = (myWeapon.Magic ? theirStats.Res : theirStats.Def) + terrainDef;
        int damage = Math.Max(0, attack - defense);

        int heightBonus = s.Grid[myPos].Height > s.Grid[theirPos].Height ? HeightHitBonus : 0;
        int hit = myStats.Dex * 2 + myStats.Lck / 2 + myWeapon.Hit + tri * WeaponTriangle.HitBonus + heightBonus;
        int avoid = theirAs * 2 + theirStats.Lck / 2 + terrainAvoid;
        int displayedHit = Math.Clamp(hit - avoid, 0, 100);

        int crit = Math.Clamp((myStats.Dex + myStats.Lck) / 2 + myWeapon.Crit - theirStats.Lck, 0, 100);

        return new PlannedStrike(byAttacker, damage, displayedHit, crit);
    }

    private static bool IsEffective(BattleState s, WeaponDef weapon, BattleUnit target)
    {
        var moveType = StatCalculator.MovementTypeOf(s.Db, target.State).ToString();
        return weapon.EffectiveAgainst.Contains(moveType, StringComparer.OrdinalIgnoreCase);
    }

    private static CombatantForecast Summary(
        BattleUnit unit, StatBlock stats, WeaponDef? weapon, PlannedStrike? strike, int attackSpeed, int strikeCount,
        WeaponDef? opposingWeapon, BattleUnit opponent, BattleState s) =>
        new(unit.Id,
            unit.Name,
            unit.CurrentHp,
            stats.HP,
            weapon?.Name,
            strike is not null,
            strike?.Damage ?? 0,
            strike?.Hit ?? 0,
            strike?.Crit ?? 0,
            attackSpeed,
            strikeCount,
            weapon is null ? 0 : WeaponTriangle.Advantage(weapon.Type, opposingWeapon?.Type),
            weapon is not null && strike is not null && IsEffective(s, weapon, opponent));

    /// <summary>Rolls the forecast's strikes in order. Stops as soon as either side dies.</summary>
    public static CombatResult Resolve(CombatForecast forecast, IRng rng)
    {
        int attackerHp = forecast.Attacker.Hp;
        int defenderHp = forecast.Defender.Hp;
        var strikes = new List<Strike>();

        foreach (var planned in forecast.Strikes)
        {
            if (attackerHp <= 0 || defenderHp <= 0)
                break;
            bool hit = rng.Roll2(planned.Hit);
            bool crit = hit && rng.Roll1(planned.Crit);
            int raw = hit ? planned.Damage * (crit ? CritMultiplier : 1) : 0;
            int targetHp = planned.ByAttacker ? defenderHp : attackerHp;
            int dealt = Math.Min(raw, targetHp);
            targetHp -= dealt;
            if (planned.ByAttacker)
                defenderHp = targetHp;
            else
                attackerHp = targetHp;
            strikes.Add(new Strike(planned.ByAttacker, hit, crit, dealt, targetHp));
        }
        return new CombatResult(strikes, attackerHp, defenderHp);
    }
}
