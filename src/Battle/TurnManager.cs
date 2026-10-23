using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public sealed record CommandResult(bool Ok, string? Error, IReadOnlyList<BattleEvent> Events)
{
    public static CommandResult Rejected(string error) => new(false, error, System.Array.Empty<BattleEvent>());
}

/// <summary>The battle state machine, and the one place where battle state changes.</summary>
public sealed class TurnManager
{
    private static readonly Faction[] PhaseOrder = { Faction.Player, Faction.Enemy, Faction.Neutral, Faction.Ally };

    public BattleState State { get; }

    public TurnManager(BattleState state)
    {
        State = state;
    }

    /// <summary>Starts turn 1 with the Player phase.</summary>
    public IReadOnlyList<BattleEvent> StartBattle()
    {
        State.Turn = 1;
        var events = new List<BattleEvent>();
        BeginPhase(Faction.Player, events);
        CheckOutcome(events);
        return events;
    }

    /// <summary>
    /// Rebuilds a suspended battle: starts it and resubmits its logged commands, with no visuals. The same seed,
    /// units and commands give the same battle. False if a command is rejected (the content changed since).
    /// </summary>
    public bool Replay(IEnumerable<IBattleCommand> commands)
    {
        StartBattle();
        foreach (var command in commands)
            if (!Submit(command).Ok)
                return false;
        return true;
    }

    public CommandResult Submit(IBattleCommand command)
    {
        if (State.Outcome != BattleOutcome.InProgress)
            return CommandResult.Rejected("The battle is over.");
        var validation = command.Validate(State);
        if (!validation.Ok)
            return CommandResult.Rejected(validation.Error ?? "Invalid command.");

        var events = new List<BattleEvent>(command.Execute(State));
        State.CommandLog.Add(command);
        EventDirector.AfterCommand(State, events);

        if (CheckOutcome(events))
            return new CommandResult(true, null, events);

        // Keep advancing while a phase has nobody able to act (e.g. every unit stunned).
        for (int guard = 0; guard < 6 && State.LivingOf(State.Phase).All(u => u.ActionState == ActionState.Done); guard++)
        {
            AdvancePhase(events);
            if (CheckOutcome(events))
                break;
        }
        return new CommandResult(true, null, events);
    }

    private bool CheckOutcome(List<BattleEvent> events)
    {
        var outcome = ObjectiveEvaluator.Evaluate(State);
        if (outcome == BattleOutcome.InProgress)
            return false;
        State.Outcome = outcome;
        events.Add(new BattleEnded(outcome));
        return true;
    }

    /// <summary>Player → Enemy → Neutral → Ally → (Turn+1) Player. Phases with no units on the map are skipped.</summary>
    private void AdvancePhase(List<BattleEvent> events)
    {
        int index = System.Array.IndexOf(PhaseOrder, State.Phase);
        for (int i = 1; i <= PhaseOrder.Length; i++)
        {
            int next = (index + i) % PhaseOrder.Length;
            if (next == 0)
                State.Turn++;
            var faction = PhaseOrder[next];
            if (State.LivingOf(faction).Any() || next == 0)
            {
                BeginPhase(faction, events);
                return;
            }
        }
    }

    /// <summary>
    /// Phase start: the faction's crest buffs expire, everyone is Ready except stunned units (who skip this
    /// phase), forts heal, then turn triggers fire. Survive objectives complete when the player phase after the
    /// last turn begins.
    /// </summary>
    private void BeginPhase(Faction faction, List<BattleEvent> events)
    {
        State.Phase = faction;
        var goals = State.Map.Goals;
        if (faction == Faction.Player && goals.Victory == VictoryKind.Survive && State.Turn > goals.Turns)
            State.SurviveComplete = true;
        events.Add(new PhaseChanged(faction, State.Turn));

        foreach (var unit in State.LivingOf(faction).ToList())
        {
            unit.TempBonus = default;
            unit.TempBonusSource = null;
            if (unit.StunnedPhases > 0)
            {
                unit.StunnedPhases--;
                unit.ActionState = ActionState.Done;
                events.Add(new UnitStunnedSkip(unit.Id));
                events.Add(new UnitActionStateChanged(unit.Id, ActionState.Done));
                continue;
            }
            unit.ActionState = ActionState.Ready;
            int healPercent = State.Grid[unit.Pos].Terrain.HealPercent;
            if (healPercent > 0)
            {
                int amount = BattleProgress.RestoreHp(State, unit, State.Stats(unit).HP * healPercent / 100);
                if (amount > 0)
                    events.Add(new UnitHealed(unit.Id, unit.Id, amount, unit.CurrentHp, State.Grid[unit.Pos].Terrain.Name));
            }
        }
        EventDirector.OnPhaseStart(State, events);
    }
}
