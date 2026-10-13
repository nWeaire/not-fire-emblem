using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public readonly record struct ValidationResult(bool Ok, string? Error)
{
    public static readonly ValidationResult Valid = new(true, null);
    public static ValidationResult Fail(string error) => new(false, error);
}

/// <summary>Every change to battle state, from the player or the AI, goes through a command.</summary>
public interface IBattleCommand
{
    ValidationResult Validate(BattleState s);

    /// <summary>Mutates the state. Only called after <see cref="Validate"/> succeeded.</summary>
    IReadOnlyList<BattleEvent> Execute(BattleState s);
}

/// <summary>What a unit does after moving.</summary>
public abstract record BattleAction;

public sealed record WaitAction : BattleAction;

public sealed record AttackAction(string TargetId, ActionChoice Choice) : BattleAction;

/// <summary>
/// Moves a unit and then acts, atomically. The UI previews the move on its own; nothing changes in the
/// logic until this is submitted, so cancelling a move needs no undo.
/// </summary>
public sealed record ActCommand(string UnitId, GridPos Destination, BattleAction Action) : IBattleCommand
{
    public ValidationResult Validate(BattleState s)
    {
        var unit = s.TryUnit(UnitId);
        if (unit is null || unit.IsDefeated)
            return ValidationResult.Fail("Unit does not exist or is defeated.");
        if (unit.Faction != s.Phase)
            return ValidationResult.Fail($"It is not {unit.Faction}'s phase.");
        if (unit.ActionState == ActionState.Done)
            return ValidationResult.Fail($"{unit.Name} has already acted.");
        if (unit.ActionState == ActionState.Moved && Destination != unit.Pos)
            return ValidationResult.Fail($"{unit.Name} has already moved.");
        if (Destination != unit.Pos && !s.Reach(unit).CanStop(Destination))
            return ValidationResult.Fail($"{unit.Name} cannot move to {Destination}.");

        switch (Action)
        {
            case WaitAction:
                return ValidationResult.Valid;
            case AttackAction attack:
            {
                var target = s.TryUnit(attack.TargetId);
                if (target is null || target.IsDefeated)
                    return ValidationResult.Fail("Target does not exist or is defeated.");
                if (!unit.Faction.IsHostileTo(target.Faction))
                    return ValidationResult.Fail($"{target.Name} is not hostile.");
                if (attack.Choice is not WeaponAttack weaponAttack)
                    return ValidationResult.Fail("Unsupported attack choice.");
                if (!unit.State.Inventory.Contains(weaponAttack.WeaponId))
                    return ValidationResult.Fail($"{unit.Name} does not carry {weaponAttack.WeaponId}.");
                var weapon = s.Db.Weapon(weaponAttack.WeaponId);
                if (!CombatCalculator.CanWield(s.Db, unit.State, weapon))
                    return ValidationResult.Fail($"{unit.Name} cannot use {weapon.Name}.");
                if (!CombatCalculator.InRange(weapon, Destination.DistanceTo(target.Pos)))
                    return ValidationResult.Fail($"{target.Name} is out of range.");
                return ValidationResult.Valid;
            }
            default:
                return ValidationResult.Fail($"Unsupported action {Action}.");
        }
    }

    public IReadOnlyList<BattleEvent> Execute(BattleState s)
    {
        var unit = s.Unit(UnitId);
        var events = new List<BattleEvent>();

        if (Destination != unit.Pos)
        {
            var path = s.Reach(unit).PathTo(Destination);
            s.MoveUnit(unit, Destination);
            events.Add(new UnitMoved(unit.Id, path));
        }

        if (Action is AttackAction attack)
        {
            var target = s.Unit(attack.TargetId);
            if (attack.Choice is WeaponAttack w)
                unit.State.Equip(w.WeaponId);
            var forecast = CombatCalculator.Forecast(s, unit, unit.Pos, target, attack.Choice);
            var result = CombatCalculator.Resolve(forecast, s.Rng);
            unit.CurrentHp = result.AttackerHpAfter;
            target.CurrentHp = result.DefenderHpAfter;
            events.Add(new CombatResolved(unit.Id, target.Id, forecast, result));
            if (result.DefenderDied)
            {
                s.Defeat(target);
                events.Add(new UnitDefeated(target.Id));
            }
            if (result.AttackerDied)
            {
                s.Defeat(unit);
                events.Add(new UnitDefeated(unit.Id));
            }
        }

        if (!unit.IsDefeated)
        {
            unit.ActionState = ActionState.Done;
            events.Add(new UnitActionStateChanged(unit.Id, ActionState.Done));
        }
        return events;
    }
}

/// <summary>Ends the current phase: every unit that hasn't acted yet is marked Done.</summary>
public sealed record EndPhaseCommand : IBattleCommand
{
    public ValidationResult Validate(BattleState s) => ValidationResult.Valid;

    public IReadOnlyList<BattleEvent> Execute(BattleState s)
    {
        var events = new List<BattleEvent>();
        foreach (var unit in s.LivingOf(s.Phase).Where(u => u.ActionState != ActionState.Done))
        {
            unit.ActionState = ActionState.Done;
            events.Add(new UnitActionStateChanged(unit.Id, ActionState.Done));
        }
        return events;
    }
}
