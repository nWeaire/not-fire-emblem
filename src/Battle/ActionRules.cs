using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Which actions a unit can take from a cell. Shared by command validation, the player's action menu and the AI,
/// so the menu never offers something the command would reject.
/// </summary>
public static class ActionRules
{
    /// <summary>Living units on the same side as <paramref name="unit"/> (Player and Ally count as friends).</summary>
    public static IEnumerable<BattleUnit> Friends(BattleState s, BattleUnit unit) =>
        s.Living.Where(u => u != unit && !u.Faction.IsHostileTo(unit.Faction));

    public static ValidationResult Validate(BattleState s, BattleUnit unit, GridPos from, BattleAction action)
    {
        switch (action)
        {
            case WaitAction:
                return ValidationResult.Valid;
            case AttackAction attack:
            {
                var target = s.TryUnit(attack.TargetId);
                if (target is null || !target.IsOnMap)
                    return ValidationResult.Fail("Target is not on the map.");
                if (!unit.Faction.IsHostileTo(target.Faction))
                    return ValidationResult.Fail($"{target.Name} is not hostile.");
                if (!s.CanSee(unit.Faction, target))
                    return ValidationResult.Fail($"{target.Name} can't be seen.");
                if (!unit.State.Inventory.Contains(attack.Choice.WeaponId))
                    return ValidationResult.Fail($"{unit.Name} does not carry {attack.Choice.WeaponId}.");
                var weapon = s.Db.Weapon(attack.Choice.WeaponId);
                if (!CombatCalculator.CanWield(s.Db, unit.State, weapon))
                    return ValidationResult.Fail($"{unit.Name} cannot use {weapon.Name}.");
                if (attack.Choice is ArtAttack art)
                {
                    var artDef = s.Db.Art(art.ArtId);
                    if (!Abilities.Arts(s.Db, unit.State).Contains(artDef) || unit.UsesLeft.GetValueOrDefault(art.ArtId) <= 0)
                        return ValidationResult.Fail($"{unit.Name} can't use {artDef.Name} now.");
                    if (artDef.Type != weapon.Type)
                        return ValidationResult.Fail($"{artDef.Name} needs a {artDef.Type}.");
                }
                if (!CombatCalculator.InRange(s.Db, attack.Choice, from.DistanceTo(target.Pos)))
                    return ValidationResult.Fail($"{target.Name} is out of range.");
                return ValidationResult.Valid;
            }
            case HealAction heal:
            {
                var target = s.TryUnit(heal.TargetId);
                if (target is null || !HealTargets(s, unit, from, heal.SpellId).Contains(target))
                    return ValidationResult.Fail("Nobody to heal there.");
                return ValidationResult.Valid;
            }
            case TalkAction talk:
                return EventDirector.TalkTargets(s, unit, from).Any(t => t.Id == talk.TargetId)
                    ? ValidationResult.Valid
                    : ValidationResult.Fail("Nobody to talk to.");
            case VisitAction:
                return CanVisit(s, unit, from) ? ValidationResult.Valid : ValidationResult.Fail("Nothing to visit here.");
            case SeizeAction:
                return CanSeize(s, unit, from) ? ValidationResult.Valid : ValidationResult.Fail("Only the lord can seize, on the marked tile.");
            case EscapeAction:
                return CanEscape(s, unit, from) ? ValidationResult.Valid : ValidationResult.Fail("Not an exit.");
            case UseItemAction use:
                return UsableItems(s, unit).Any(i => i.Id == use.ItemId)
                    ? ValidationResult.Valid
                    : ValidationResult.Fail("That item can't be used now.");
            case UnlockAction unlock:
                return Unlockable(s, unit, from).Contains(unlock.Cell)
                    ? ValidationResult.Valid
                    : ValidationResult.Fail("Nothing to unlock there.");
            case StealAction steal:
                return StealTargets(s, unit, from).FirstOrDefault(t => t.Id == steal.TargetId) is { } mark
                       && Stealable(mark).Contains(steal.ItemId)
                    ? ValidationResult.Valid
                    : ValidationResult.Fail("Nothing to steal.");
            case CrestAction crest:
                return CrestTargets(s, unit, from, crest.Skill) is { } targets && (crest.TargetId is null
                    ? crest.Skill is CrestSkill.Rally or CrestSkill.Mend
                    : targets.Any(t => t.Id == crest.TargetId))
                    ? ValidationResult.Valid
                    : ValidationResult.Fail("That crest skill can't be used here.");
            default:
                return ValidationResult.Fail($"Unsupported action {action}.");
        }
    }

    /// <summary>Visible hostile units the unit could attack from <paramref name="from"/> with any choice.</summary>
    public static IEnumerable<BattleUnit> AttackTargets(BattleState s, BattleUnit unit, GridPos from)
    {
        var choices = CombatCalculator.AttackChoices(s.Db, unit).ToList();
        return s.Living.Where(t => t.Faction.IsHostileTo(unit.Faction) && s.CanSee(unit.Faction, t)
                                   && choices.Any(c => CombatCalculator.InRange(s.Db, c, from.DistanceTo(t.Pos))));
    }

    /// <summary>Spells the unit knows with uses left.</summary>
    public static IEnumerable<SpellDef> ReadySpells(BattleState s, BattleUnit unit) =>
        Abilities.Spells(s.Db, unit.State).Where(sp => unit.UsesLeft.GetValueOrDefault(sp.Id) > 0);

    /// <summary>Injured friends in range of the spell from <paramref name="from"/>.</summary>
    public static IEnumerable<BattleUnit> HealTargets(BattleState s, BattleUnit unit, GridPos from, string spellId)
    {
        var spell = ReadySpells(s, unit).FirstOrDefault(sp => sp.Id == spellId);
        if (spell is null)
            return Enumerable.Empty<BattleUnit>();
        var targets = Friends(s, unit).Where(f => f.CurrentHp < s.Stats(f).HP
                                                  && from.DistanceTo(f.Pos) >= spell.MinRange && from.DistanceTo(f.Pos) <= spell.MaxRange);
        // A spell with minimum range 0 also heals the caster (it stands on `from`, wherever its Pos still says).
        return spell.MinRange == 0 && unit.CurrentHp < s.Stats(unit).HP ? targets.Prepend(unit) : targets;
    }

    public static bool CanVisit(BattleState s, BattleUnit unit, GridPos from) =>
        unit.Faction == Faction.Player && !s.VisitedCells.Contains(from) && EventDirector.VisitTrigger(s, from) is not null;

    public static bool CanSeize(BattleState s, BattleUnit unit, GridPos from) =>
        unit.IsLord && unit.Faction == Faction.Player && s.Map.Goals.Victory == VictoryKind.Seize && s.Map.Goals.Cells.Contains(from);

    public static bool CanEscape(BattleState s, BattleUnit unit, GridPos from) =>
        unit.Faction == Faction.Player && s.Map.Goals.Victory == VictoryKind.Escape && s.Map.Goals.Cells.Contains(from);

    // ------------------------------------------------------------------ locks and thieves (Ch 9)

    public const string DoorTerrain = "door";
    public const string ChestTerrain = "chest";
    public const string DoorKey = "door_key";
    public const string ChestKey = "chest_key";
    /// <summary>What an opened door or chest becomes: stone floor.</summary>
    public const string OpenedTerrain = "square";

    /// <summary>Thieves open doors and chests without keys.</summary>
    public static bool HasLocktouch(BattleState s, BattleUnit unit) => s.Db.Class(unit.State.ClassId).HasTrait("Locktouch");

    /// <summary>Locked cells the unit can open from <paramref name="from"/>: doors beside it and a chest under it.</summary>
    public static IEnumerable<GridPos> Unlockable(BattleState s, BattleUnit unit, GridPos from)
    {
        bool touch = HasLocktouch(s, unit);
        bool doorKey = touch || unit.State.Inventory.Contains(DoorKey);
        bool chestKey = touch || unit.State.Inventory.Contains(ChestKey);
        if (chestKey && s.Grid[from].Terrain.Id == ChestTerrain)
            yield return from;
        if (!doorKey)
            yield break;
        foreach (var cell in new[] { new GridPos(from.X, from.Y - 1), new GridPos(from.X + 1, from.Y), new GridPos(from.X, from.Y + 1), new GridPos(from.X - 1, from.Y) })
            if (s.Grid.InBounds(cell) && s.Grid[cell].Terrain.Id == DoorTerrain)
                yield return cell;
    }

    /// <summary>Adjacent enemies a thief can rob: not faster than the thief, carrying something not in hand.</summary>
    public static IEnumerable<BattleUnit> StealTargets(BattleState s, BattleUnit thief, GridPos from)
    {
        if (!s.Db.Class(thief.State.ClassId).HasTrait("Steal") || thief.State.Inventory.Count >= UnitState.MaxInventory)
            return Enumerable.Empty<BattleUnit>();
        int speed = s.Stats(thief).Spd;
        return s.Living.Where(t => t.Faction.IsHostileTo(thief.Faction) && t.Pos.DistanceTo(from) == 1 && s.CanSee(thief.Faction, t)
                                   && s.Stats(t).Spd <= speed && Stealable(t).Any());
    }

    /// <summary>What can be stolen from a unit: anything it carries except the weapon in its hand.</summary>
    public static IEnumerable<string> Stealable(BattleUnit target) =>
        target.State.Inventory.Where((_, i) => i != target.State.EquippedIndex).Distinct();

    /// <summary>Consumables the unit could use on itself right now.</summary>
    public static IEnumerable<ItemDef> UsableItems(BattleState s, BattleUnit unit) =>
        unit.State.Inventory.Distinct().Select(s.Db.TryItem).OfType<ItemDef>().Where(i => i.Kind switch
        {
            ItemKind.Heal => unit.CurrentHp < s.Stats(unit).HP,
            ItemKind.StatBoost => true,
            _ => false,
        });

    /// <summary>Adjacent friends a unit could trade with.</summary>
    public static IEnumerable<BattleUnit> TradePartners(BattleState s, BattleUnit unit, GridPos from) =>
        s.LivingOf(unit.Faction).Where(p => p != unit && p.Pos.DistanceTo(from) == 1
                                            && (p.State.Inventory.Count > 0 || unit.State.Inventory.Count > 0));

    /// <summary>Crest skills the lord could use now, from <paramref name="from"/>.</summary>
    public static IEnumerable<CrestSkill> ReadyCrestSkills(BattleState s, BattleUnit unit, GridPos from) =>
        CrestSkills.Available(s.Options.CrestTier)
            .Where(k => unit.IsLord && unit.Faction == Faction.Player && unit.UsesLeft.GetValueOrDefault(CrestSkills.UseKey(k)) > 0)
            .Where(k => CrestTargets(s, unit, from, k) is { Count: > 0 } || k is CrestSkill.Rally or CrestSkill.Mend);

    /// <summary>
    /// Who a crest skill would affect: Ward, an adjacent friend; Dawn, an adjacent friend who has acted;
    /// Rally and Mend, friends within 2 (Mend: injured ones, including the lord). Null when unusable.
    /// </summary>
    public static List<BattleUnit>? CrestTargets(BattleState s, BattleUnit unit, GridPos from, CrestSkill skill)
    {
        if (!unit.IsLord || unit.Faction != Faction.Player || unit.UsesLeft.GetValueOrDefault(CrestSkills.UseKey(skill)) <= 0
            || CrestSkills.Tier(skill) > s.Options.CrestTier)
            return null;
        var friends = Friends(s, unit);
        return skill switch
        {
            CrestSkill.Ward => friends.Where(f => f.Pos.DistanceTo(from) == 1).ToList(),
            CrestSkill.Dawn => friends.Where(f => f.Pos.DistanceTo(from) == 1 && f.ActionState == ActionState.Done && f.Faction == Faction.Player).ToList(),
            CrestSkill.Rally => friends.Where(f => f.Pos.DistanceTo(from) <= CrestSkills.AreaRadius).ToList(),
            CrestSkill.Mend => friends.Where(f => f.Pos.DistanceTo(from) <= CrestSkills.AreaRadius && f.CurrentHp < s.Stats(f).HP).ToList(),
            _ => null,
        };
    }
}
