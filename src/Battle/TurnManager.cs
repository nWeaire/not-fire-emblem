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
    private static readonly Faction[] PhaseOrder = { Faction.Player, Faction.Enemy, Faction.Ally };

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
        return events;
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

        if (CheckOutcome(events))
            return new CommandResult(true, null, events);

        if (State.LivingOf(State.Phase).All(u => u.ActionState == ActionState.Done))
        {
            AdvancePhase(events);
            CheckOutcome(events);
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

    /// <summary>Player → Enemy → Ally → (Turn+1) Player. Phases with no living units are skipped.</summary>
    private void AdvancePhase(List<BattleEvent> events)
    {
        int index = System.Array.IndexOf(PhaseOrder, State.Phase);
        for (int i = 1; i <= PhaseOrder.Length; i++)
        {
            int next = (index + i) % PhaseOrder.Length;
            if (next == 0)
                State.Turn++;
            var faction = PhaseOrder[next];
            if (State.LivingOf(faction).Any())
            {
                BeginPhase(faction, events);
                return;
            }
        }
    }

    private void BeginPhase(Faction faction, List<BattleEvent> events)
    {
        State.Phase = faction;
        foreach (var unit in State.LivingOf(faction))
            unit.ActionState = ActionState.Ready;
        events.Add(new PhaseChanged(faction, State.Turn));
    }
}
