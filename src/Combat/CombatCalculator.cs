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

    /// <summary>
    /// The weapon the unit counters with: its equipped weapon, else its first usable one. Given the attack's
    /// <paramref name="distance"/>, a carried weapon that reaches is used when the equipped one can't (a fighter with
    /// a hand axe answers a thrown axe; session 7, the user expected units to hit back).
    /// </summary>
    public static WeaponDef? CounterWeapon(ContentDatabase db, UnitState unit, int? distance = null)
    {
        var equipped = db.TryWeapon(unit.EquippedItemId);
        var usable = equipped is not null && CanWield(db, unit, equipped) ? equipped : UsableWeapons(db, unit).FirstOrDefault();
        if (distance is { } d && usable is not null && !InRange(usable, d))
            return UsableWeapons(db, unit).FirstOrDefault(w => InRange(w, d)) ?? usable;
        return usable;
    }

    public static bool InRange(WeaponDef weapon, int distance) => distance >= weapon.MinRange && distance <= weapon.MaxRange;

    /// <summary>
    /// The weapon as the choice uses it: an art adds its Might/Hit/Crit/range and effectiveness to the weapon.
    /// The result keeps the weapon's id; its name becomes the art's name.
    /// </summary>
    public static WeaponDef EffectiveWeapon(ContentDatabase db, ActionChoice choice)
    {
        var weapon = db.Weapon(choice.WeaponId);
        if (choice is not ArtAttack a)
            return weapon;
        var art = db.Art(a.ArtId);
        return weapon with
        {
            Name = art.Name,
            Might = weapon.Might + art.MightMod,
            Hit = weapon.Hit + art.HitMod,
            Crit = weapon.Crit + art.CritMod,
            MaxRange = weapon.MaxRange + art.RangeBonus,
            EffectiveAgainst = weapon.EffectiveAgainst.Concat(art.EffectiveAgainst ?? Array.Empty<string>()).ToArray(),
        };
    }

    /// <summary>
    /// Everything the unit could attack with right now: each usable weapon, and each known art (with uses left)
    /// with each matching weapon.
    /// </summary>
    public static IEnumerable<ActionChoice> AttackChoices(ContentDatabase db, BattleUnit unit)
    {
        var weapons = UsableWeapons(db, unit.State).ToList();
        foreach (var w in weapons)
            yield return new WeaponAttack(w.Id);
        foreach (var art in Abilities.Arts(db, unit.State))
        {
            if (unit.UsesLeft.GetValueOrDefault(art.Id) <= 0)
                continue;
            foreach (var w in weapons.Where(w => w.Type == art.Type))
                yield return new ArtAttack(art.Id, w.Id);
        }
    }

    public static bool InRange(ContentDatabase db, ActionChoice choice, int distance) => InRange(EffectiveWeapon(db, choice), distance);

    public static CombatForecast Forecast(BattleState s, BattleUnit attacker, GridPos attackerPos, BattleUnit defender, ActionChoice choice)
    {
        var attackWeapon = EffectiveWeapon(s.Db, choice);
        bool noCounter = choice is ArtAttack art && s.Db.Art(art.ArtId).NoCounter;
        int distance = attackerPos.DistanceTo(defender.Pos);
        var counterWeapon = CounterWeapon(s.Db, defender.State, distance);
        bool canCounter = !noCounter && counterWeapon is not null && InRange(counterWeapon, distance);

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
        var myBonus = s.CombatBonus(me, myPos);
        var theirBonus = s.CombatBonus(them, theirPos);
        int hit = myStats.Dex * 2 + myStats.Lck / 2 + myWeapon.Hit + tri * WeaponTriangle.HitBonus + heightBonus + myBonus.Hit;
        int avoid = theirAs * 2 + theirStats.Lck / 2 + terrainAvoid + theirBonus.Avoid;
        int displayedHit = Math.Clamp(hit - avoid, 0, 100);

        int crit = Math.Clamp((myStats.Dex + myStats.Lck) / 2 + myWeapon.Crit + myBonus.Crit - theirStats.Lck, 0, 100);

        return new PlannedStrike(byAttacker, damage, displayedHit, crit);
    }

    /// <summary>Effective against the target's movement type ("Flying") or one of its class traits ("Beast").</summary>
    private static bool IsEffective(BattleState s, WeaponDef weapon, BattleUnit target)
    {
        var moveType = StatCalculator.MovementTypeOf(s.Db, target.State).ToString();
        var cls = s.Db.Class(target.State.ClassId);
        return weapon.EffectiveAgainst.Any(e =>
            string.Equals(e, moveType, StringComparison.OrdinalIgnoreCase) || cls.HasTrait(e));
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
