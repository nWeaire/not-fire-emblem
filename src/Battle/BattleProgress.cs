using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Shared state changes used by several commands: defeat and severance, EXP, supports, healing.</summary>
public static class BattleProgress
{
    /// <summary>
    /// Removes a fallen unit. A fallen crest-bearer severs everyone it dominated (STORY §4.3): beasts are stunned
    /// for their next phase, dominated humans surrender and leave the map. Anyone else others are dominated by (Ch 8's
    /// agitators, who share one key) only lets go of the units within their Dominion range that no other unit with
    /// the key still holds.
    /// </summary>
    public static void Defeat(BattleState s, BattleUnit unit, List<BattleEvent> events)
    {
        // Story-critical units (UnitDef.Retreats) retreat wounded instead of dying, unless the map says they must
        // survive (then their fall loses the battle, as before). Session 12: the user's answer to "what if Aldric dies
        // before Ch 15?".
        if (unit.IsRosterUnit && unit.State.Def.Retreats && unit.Faction == Faction.Player
            && !s.Map.Goals.MustSurvive.Contains(unit.Key ?? ""))
        {
            unit.CurrentHp = 1;
            s.RemoveFromMap(unit, MapExit.Retreated);
            events.Add(new UnitRetreated(unit.Id));
            string scene = $"retreat_{unit.State.Def.Id}";
            if (s.Db.Scenes.ContainsKey(scene))
                events.Add(new DialogueRequested(scene));
            return;
        }
        if (unit.NonLethal)
        {
            // Beaten, not killed (Ch 14's Empress): off the map, alive.
            unit.CurrentHp = 1;
            s.RemoveFromMap(unit, MapExit.Subdued);
            events.Add(new UnitSubdued(unit.Id));
            return;
        }
        s.Defeat(unit);
        events.Add(new UnitDefeated(unit.Id));
        if (unit.Key is not { } key)
            return;
        var stunned = new List<string>();
        var surrendered = new List<string>();
        var released = s.Living.Where(u => u.DominatedBy == key);
        if (!unit.CrestBearer)
            released = released.Where(u => u.Pos.DistanceTo(unit.Pos) <= BattleState.RadiusOf(unit) && !s.IsDominated(u));
        foreach (var held in released.ToList())
        {
            held.DominatedBy = null;
            if (held.NonLethal)
                continue; // freed, not sent away: a trigger decides what they do next (Ch 14's Empress wakes)
            if (s.Db.Class(held.State.ClassId).HasTrait("Beast"))
            {
                held.StunnedPhases = 1;
                stunned.Add(held.Id);
            }
            else
            {
                s.RemoveFromMap(held, MapExit.Surrendered);
                surrendered.Add(held.Id);
            }
        }
        if (stunned.Count + surrendered.Count > 0)
            events.Add(new CrestSevered(unit.Id, stunned, surrendered));
    }

    /// <summary>Gives EXP to a roster unit and rolls any level-ups with the battle's RNG.</summary>
    public static void AwardExp(BattleState s, BattleUnit unit, int amount, List<BattleEvent> events)
    {
        if (!s.Options.GainExp || amount <= 0 || !unit.IsRosterUnit)
            return;
        var levels = LevelUpService.AddExp(s.Db, unit.State, amount, s.Rng);
        unit.ExpEarned += amount;
        // Levels never heal: a higher max HP leaves current HP where it was.
        events.Add(new ExpGained(unit.Id, amount, unit.State.Exp, levels));
    }

    /// <summary>+1 support point with every support partner standing next to <paramref name="unit"/>.</summary>
    public static void SupportTick(BattleState s, BattleUnit unit)
    {
        if (!unit.IsRosterUnit)
            return;
        foreach (var partner in s.LivingOf(Faction.Player).Where(p => p != unit && p.Pos.DistanceTo(unit.Pos) == 1 && p.IsRosterUnit))
        {
            if (Supports.Find(s.Db, unit.State.Def.Id, partner.State.Def.Id) is null)
                continue;
            string key = Supports.PairKey(unit.State.Def.Id, partner.State.Def.Id);
            s.SupportPoints[key] = s.SupportPoints.GetValueOrDefault(key) + Supports.AdjacentPoints;
        }
    }

    /// <summary>Restores up to <paramref name="amount"/> HP, capped at max HP. Returns what was restored.</summary>
    public static int RestoreHp(BattleState s, BattleUnit unit, int amount)
    {
        int max = s.Stats(unit).HP;
        int restored = System.Math.Clamp(max - unit.CurrentHp, 0, System.Math.Max(0, amount));
        unit.CurrentHp += restored;
        return restored;
    }

    /// <summary>After items move around, make sure the unit still has a usable weapon equipped if it carries one.</summary>
    public static void ReequipIfNeeded(BattleState s, BattleUnit unit)
    {
        var equipped = s.Db.TryWeapon(unit.State.EquippedItemId);
        if (equipped is not null && CombatCalculator.CanWield(s.Db, unit.State, equipped))
            return;
        unit.State.EquippedIndex = -1;
        if (CombatCalculator.UsableWeapons(s.Db, unit.State).FirstOrDefault() is { } weapon)
            unit.State.Equip(weapon.Id);
    }
}
