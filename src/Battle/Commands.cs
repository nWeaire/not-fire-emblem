using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public readonly record struct ValidationResult(bool Ok, string? Error)
{
    public static readonly ValidationResult Valid = new(true, null);
    public static ValidationResult Fail(string error) => new(false, error);
}

/// <summary>
/// Every change to battle state, from the player or the AI, goes through a command. Commands are saved as JSON
/// (suspend saves keep the battle's command log), so each type has a tag.
/// </summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(ActCommand), "act")]
[JsonDerivedType(typeof(TradeCommand), "trade")]
[JsonDerivedType(typeof(EndPhaseCommand), "end")]
public interface IBattleCommand
{
    ValidationResult Validate(BattleState s);

    /// <summary>Mutates the state. Only called after <see cref="Validate"/> succeeded.</summary>
    IReadOnlyList<BattleEvent> Execute(BattleState s);
}

/// <summary>What a unit does after moving.</summary>
[JsonPolymorphic]
[JsonDerivedType(typeof(WaitAction), "wait")]
[JsonDerivedType(typeof(AttackAction), "attack")]
[JsonDerivedType(typeof(HealAction), "heal")]
[JsonDerivedType(typeof(TalkAction), "talk")]
[JsonDerivedType(typeof(VisitAction), "visit")]
[JsonDerivedType(typeof(SeizeAction), "seize")]
[JsonDerivedType(typeof(EscapeAction), "escape")]
[JsonDerivedType(typeof(UseItemAction), "item")]
[JsonDerivedType(typeof(CrestAction), "crest")]
[JsonDerivedType(typeof(UnlockAction), "unlock")]
[JsonDerivedType(typeof(StealAction), "steal")]
[JsonDerivedType(typeof(BreakDoorAction), "breakdoor")]
public abstract record BattleAction;

public sealed record WaitAction : BattleAction;

public sealed record AttackAction(string TargetId, ActionChoice Choice) : BattleAction;

public sealed record HealAction(string TargetId, string SpellId) : BattleAction;

public sealed record TalkAction(string TargetId) : BattleAction;

public sealed record VisitAction : BattleAction;

public sealed record SeizeAction : BattleAction;

public sealed record EscapeAction : BattleAction;

/// <summary>Uses a consumable on the unit itself (vulnerary, stat booster).</summary>
public sealed record UseItemAction(string ItemId) : BattleAction;

/// <summary>A crest skill. Ward and Dawn need an adjacent ally; Rally and Mend affect everyone nearby.</summary>
public sealed record CrestAction(CrestSkill Skill, string? TargetId) : BattleAction;

/// <summary>Opens a locked door next to the unit, or the chest it stands on (Locktouch, or a key that is used up).</summary>
public sealed record UnlockAction(GridPos Cell) : BattleAction;

/// <summary>Strikes a locked door next to the unit with its weapon; the door breaks on the third strike.</summary>
public sealed record BreakDoorAction(GridPos Cell) : BattleAction;

/// <summary>A thief takes an item the adjacent enemy isn't wielding (the enemy must not be faster).</summary>
public sealed record StealAction(string TargetId, string ItemId) : BattleAction;

/// <summary>
/// Moves a unit and then acts, atomically. The UI previews the move on its own; nothing changes in the
/// logic until this is submitted, so cancelling a move needs no undo.
/// </summary>
public sealed record ActCommand(string UnitId, GridPos Destination, BattleAction Action) : IBattleCommand
{
    public ValidationResult Validate(BattleState s)
    {
        var unit = s.TryUnit(UnitId);
        if (unit is null || !unit.IsOnMap)
            return ValidationResult.Fail("Unit does not exist or is not on the map.");
        if (unit.Faction != s.Phase)
            return ValidationResult.Fail($"It is not {unit.Faction}'s phase.");
        if (unit.ActionState == ActionState.Done)
            return ValidationResult.Fail($"{unit.Name} has already acted.");
        if (unit.ActionState == ActionState.Moved && Destination != unit.Pos)
            return ValidationResult.Fail($"{unit.Name} has already moved.");
        if (Destination != unit.Pos && !s.Reach(unit).CanStop(Destination))
            return ValidationResult.Fail($"{unit.Name} cannot move to {Destination}.");
        return ActionRules.Validate(s, unit, Destination, Action);
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

        switch (Action)
        {
            case AttackAction attack:
                Attack(s, unit, s.Unit(attack.TargetId), attack.Choice, events);
                break;
            case HealAction heal:
                Heal(s, unit, s.Unit(heal.TargetId), s.Db.Spell(heal.SpellId), events);
                break;
            case TalkAction talk:
                EventDirector.FireTalk(s, unit, s.Unit(talk.TargetId), events);
                break;
            case VisitAction:
                EventDirector.FireVisit(s, unit, events);
                break;
            case SeizeAction:
                s.Seized = true;
                events.Add(new CellSeized(unit.Id, unit.Pos));
                break;
            case EscapeAction:
                s.RemoveFromMap(unit, MapExit.Escaped);
                events.Add(new UnitEscaped(unit.Id));
                break;
            case UseItemAction use:
                UseItem(s, unit, s.Db.Item(use.ItemId), events);
                break;
            case CrestAction crest:
                UseCrest(s, unit, crest, events);
                break;
            case UnlockAction unlock:
                Unlock(s, unit, unlock.Cell, events);
                break;
            case BreakDoorAction strike:
                StrikeDoor(s, unit, strike.Cell, events);
                break;
            case StealAction steal:
                Steal(unit, s.Unit(steal.TargetId), steal.ItemId, events);
                break;
        }

        if (unit.IsOnMap)
        {
            unit.ActionState = ActionState.Done;
            events.Add(new UnitActionStateChanged(unit.Id, ActionState.Done));
        }
        return events;
    }

    private static void Attack(BattleState s, BattleUnit unit, BattleUnit target, ActionChoice choice, List<BattleEvent> events)
    {
        EventDirector.BeforeCombat(s, unit, target, events);
        unit.State.Equip(choice.WeaponId);
        var forecast = CombatCalculator.Forecast(s, unit, unit.Pos, target, choice);
        var result = CombatCalculator.Resolve(forecast, s.Rng);
        if (choice is ArtAttack art)
            unit.UsesLeft[art.ArtId] = unit.UsesLeft.GetValueOrDefault(art.ArtId) - 1;
        unit.CurrentHp = result.AttackerHpAfter;
        target.CurrentHp = result.DefenderHpAfter;
        events.Add(new CombatResolved(unit.Id, target.Id, forecast, result));

        if (result.DefenderDied)
            BattleProgress.Defeat(s, target, events);
        if (result.AttackerDied)
            BattleProgress.Defeat(s, unit, events);

        foreach (var (me, foe, byMe) in new[] { (unit, target, true), (target, unit, false) })
        {
            if (!me.IsOnMap || !me.IsRosterUnit)
                continue;
            bool dealt = result.Strikes.Any(x => x.ByAttacker == byMe && x.Hit && x.Damage > 0);
            bool killed = foe.IsDefeated;
            BattleProgress.AwardExp(s, me, ExpCalculator.CombatExp(s.Db, me.State, foe.State, dealt, killed, foe.IsBoss), events);
            BattleProgress.SupportTick(s, me);
        }
    }

    private static void Heal(BattleState s, BattleUnit unit, BattleUnit target, SpellDef spell, List<BattleEvent> events)
    {
        unit.UsesLeft[spell.Id] = unit.UsesLeft.GetValueOrDefault(spell.Id) - 1;
        int amount = BattleProgress.RestoreHp(s, target, spell.Power + s.Stats(unit).Mag / 2);
        events.Add(new UnitHealed(unit.Id, target.Id, amount, target.CurrentHp, spell.Name));
        if (unit.IsRosterUnit)
            BattleProgress.AwardExp(s, unit, unit.State.Level >= ExpCalculator.LevelCap ? 0 : ExpCalculator.HealExp, events);
        BattleProgress.SupportTick(s, unit);
    }

    private static void UseItem(BattleState s, BattleUnit unit, ItemDef item, List<BattleEvent> events)
    {
        unit.State.Inventory.Remove(item.Id);
        if (unit.State.EquippedIndex >= unit.State.Inventory.Count)
            unit.State.EquippedIndex = -1;
        BattleProgress.ReequipIfNeeded(s, unit);
        events.Add(new ItemUsed(unit.Id, item.Id));
        switch (item.Kind)
        {
            case ItemKind.Heal:
                int amount = BattleProgress.RestoreHp(s, unit, item.Amount <= 0 ? int.MaxValue : item.Amount);
                events.Add(new UnitHealed(unit.Id, unit.Id, amount, unit.CurrentHp, item.Name));
                break;
            case ItemKind.StatBoost:
                var bonus = default(StatBlock).With(item.Stat, item.Amount);
                unit.State.Gains += bonus;
                if (item.Stat == Stat.HP)
                    unit.CurrentHp += item.Amount;
                events.Add(new UnitBuffed(unit.Id, item.Name, bonus));
                break;
        }
    }

    private static void Unlock(BattleState s, BattleUnit unit, GridPos cell, List<BattleEvent> events)
    {
        bool chest = s.Grid[cell].Terrain.Id == ActionRules.ChestTerrain;
        if (!ActionRules.HasLocktouch(s, unit))
        {
            string? equipped = unit.State.EquippedItemId;
            unit.State.Inventory.Remove(chest ? ActionRules.ChestKey : ActionRules.DoorKey);
            unit.State.EquippedIndex = equipped is null ? -1 : unit.State.Inventory.IndexOf(equipped);
        }
        s.Grid.SetTerrain(cell, s.Db.Terrain(ActionRules.OpenedTerrain));
        events.Add(new TerrainChanged(new[] { cell }));
        events.Add(new LockOpened(unit.Id, cell, chest));
        if (chest)
            EventDirector.FireChest(s, unit, cell, events);
    }

    /// <summary>Session 13 (user request): doors don't need keys or Pip, just time. Three strikes break one open.</summary>
    private static void StrikeDoor(BattleState s, BattleUnit unit, GridPos cell, List<BattleEvent> events)
    {
        int hits = s.DoorStrikes.GetValueOrDefault(cell) + 1;
        s.DoorStrikes[cell] = hits;
        events.Add(new DoorStruck(unit.Id, cell, hits, ActionRules.DoorHits));
        if (hits < ActionRules.DoorHits)
            return;
        s.DoorStrikes.Remove(cell);
        s.Grid.SetTerrain(cell, s.Db.Terrain(ActionRules.OpenedTerrain));
        events.Add(new TerrainChanged(new[] { cell }));
    }

    private static void Steal(BattleUnit thief, BattleUnit target, string itemId, List<BattleEvent> events)
    {
        string? equipped = target.State.EquippedItemId;
        var inventory = target.State.Inventory;
        // Never the item in hand: take the first other copy.
        int index = Enumerable.Range(0, inventory.Count).First(i => inventory[i] == itemId && i != target.State.EquippedIndex);
        inventory.RemoveAt(index);
        target.State.EquippedIndex = equipped is null ? -1 : inventory.IndexOf(equipped);
        thief.State.Inventory.Add(itemId);
        events.Add(new ItemStolen(thief.Id, target.Id, itemId));
    }

    private static void UseCrest(BattleState s, BattleUnit lord, CrestAction crest, List<BattleEvent> events)
    {
        string key = CrestSkills.UseKey(crest.Skill);
        lord.UsesLeft[key] = lord.UsesLeft.GetValueOrDefault(key) - 1;
        var targets = new List<BattleUnit>();
        switch (crest.Skill)
        {
            case CrestSkill.Ward:
                var warded = s.Unit(crest.TargetId!);
                warded.TempBonus = CrestSkills.WardBonus;
                warded.TempBonusSource = "Ward";
                targets.Add(warded);
                events.Add(new CrestSkillUsed(lord.Id, crest.Skill, targets.Select(t => t.Id).ToList()));
                events.Add(new UnitBuffed(warded.Id, "Ward", CrestSkills.WardBonus));
                break;
            case CrestSkill.Rally:
                targets.AddRange(ActionRules.Friends(s, lord).Where(f => f.Pos.DistanceTo(lord.Pos) <= CrestSkills.AreaRadius));
                events.Add(new CrestSkillUsed(lord.Id, crest.Skill, targets.Select(t => t.Id).ToList()));
                foreach (var t in targets)
                {
                    t.TempBonus = CrestSkills.RallyBonus;
                    t.TempBonusSource = "Rally";
                    events.Add(new UnitBuffed(t.Id, "Rally", CrestSkills.RallyBonus));
                }
                break;
            case CrestSkill.Mend:
                targets.AddRange(ActionRules.Friends(s, lord).Where(f => f.Pos.DistanceTo(lord.Pos) <= CrestSkills.AreaRadius).Append(lord));
                events.Add(new CrestSkillUsed(lord.Id, crest.Skill, targets.Select(t => t.Id).ToList()));
                foreach (var t in targets)
                {
                    int amount = BattleProgress.RestoreHp(s, t, s.Stats(t).HP * CrestSkills.MendPercent / 100);
                    events.Add(new UnitHealed(lord.Id, t.Id, amount, t.CurrentHp, "Mend"));
                }
                break;
            case CrestSkill.Dawn:
                var refreshed = s.Unit(crest.TargetId!);
                refreshed.ActionState = ActionState.Ready;
                targets.Add(refreshed);
                events.Add(new CrestSkillUsed(lord.Id, crest.Skill, new[] { refreshed.Id }));
                events.Add(new UnitRefreshed(refreshed.Id));
                events.Add(new UnitActionStateChanged(refreshed.Id, ActionState.Ready));
                break;
        }
    }
}

/// <summary>
/// Moves (optionally) and swaps items with an adjacent friendly unit. The unit stays able to act from where it
/// stands (ActionState Moved), as in the GBA games.
/// </summary>
public sealed record TradeCommand(string UnitId, GridPos Destination, string PartnerId, IReadOnlyList<string> Give, IReadOnlyList<string> Take)
    : IBattleCommand
{
    public ValidationResult Validate(BattleState s)
    {
        var unit = s.TryUnit(UnitId);
        var partner = s.TryUnit(PartnerId);
        if (unit is null || !unit.IsOnMap || partner is null || !partner.IsOnMap)
            return ValidationResult.Fail("Both units must be on the map.");
        if (unit.Faction != s.Phase || unit.ActionState == ActionState.Done)
            return ValidationResult.Fail($"{unit.Name} can't trade now.");
        if (unit.ActionState == ActionState.Moved && Destination != unit.Pos)
            return ValidationResult.Fail($"{unit.Name} has already moved.");
        if (Destination != unit.Pos && !s.Reach(unit).CanStop(Destination))
            return ValidationResult.Fail($"{unit.Name} cannot move to {Destination}.");
        if (partner.Faction != unit.Faction || partner.Pos.DistanceTo(Destination) != 1)
            return ValidationResult.Fail($"{partner.Name} is not an adjacent ally.");
        if (!Contains(unit.State.Inventory, Give) || !Contains(partner.State.Inventory, Take))
            return ValidationResult.Fail("Trade lists items that aren't carried.");
        if (unit.State.Inventory.Count - Give.Count + Take.Count > UnitState.MaxInventory
            || partner.State.Inventory.Count - Take.Count + Give.Count > UnitState.MaxInventory)
            return ValidationResult.Fail("Not enough room.");
        return ValidationResult.Valid;
    }

    private static bool Contains(List<string> inventory, IReadOnlyList<string> items)
    {
        var copy = inventory.ToList();
        return items.All(copy.Remove);
    }

    public IReadOnlyList<BattleEvent> Execute(BattleState s)
    {
        var unit = s.Unit(UnitId);
        var partner = s.Unit(PartnerId);
        var events = new List<BattleEvent>();
        if (Destination != unit.Pos)
        {
            var path = s.Reach(unit).PathTo(Destination);
            s.MoveUnit(unit, Destination);
            events.Add(new UnitMoved(unit.Id, path));
        }
        foreach (var item in Give)
        {
            unit.State.Inventory.Remove(item);
            partner.State.Inventory.Add(item);
        }
        foreach (var item in Take)
        {
            partner.State.Inventory.Remove(item);
            unit.State.Inventory.Add(item);
        }
        BattleProgress.ReequipIfNeeded(s, unit);
        BattleProgress.ReequipIfNeeded(s, partner);
        unit.ActionState = ActionState.Moved;
        events.Add(new ItemsTraded(unit.Id, partner.Id));
        events.Add(new UnitActionStateChanged(unit.Id, ActionState.Moved));
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
