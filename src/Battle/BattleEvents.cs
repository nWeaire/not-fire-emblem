using System.Collections.Generic;
using NotFireEmblem.Combat;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public enum BattleOutcome { InProgress, Victory, Defeat }

/// <summary>
/// What happened, in order. Logic resolves instantly and returns these; the presentation layer replays them.
/// </summary>
public abstract record BattleEvent;

public sealed record PhaseChanged(Faction Phase, int Turn) : BattleEvent;

/// <summary>Path includes the start cell. A single-cell path means the unit stayed put.</summary>
public sealed record UnitMoved(string UnitId, IReadOnlyList<GridPos> Path) : BattleEvent;

public sealed record CombatResolved(string AttackerId, string DefenderId, CombatForecast Forecast, CombatResult Result) : BattleEvent;

public sealed record UnitDefeated(string UnitId) : BattleEvent;

public sealed record UnitActionStateChanged(string UnitId, ActionState State) : BattleEvent;

public sealed record BattleEnded(BattleOutcome Outcome) : BattleEvent;
