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

// ---------------------------------------------------------------- map rules and events (M4)

/// <summary>A trigger asked for a dialogue scene. The scene's flags are already set in the battle state.</summary>
public sealed record DialogueRequested(string SceneId) : BattleEvent;

public sealed record UnitSpawned(string UnitId) : BattleEvent;

public sealed record FactionChanged(string UnitId, Faction Faction) : BattleEvent;

/// <summary>An item was given to a unit, or sent to the convoy when its inventory was full.</summary>
public sealed record ItemReceived(string UnitId, string ItemId, bool ToConvoy) : BattleEvent;

public sealed record GoldReceived(int Amount) : BattleEvent;

public sealed record TerrainChanged(IReadOnlyList<GridPos> Cells) : BattleEvent;

/// <summary>Damage outside combat (story events, terrain).</summary>
public sealed record UnitDamaged(string UnitId, int Amount, int HpAfter) : BattleEvent;

public sealed record UnitTalked(string UnitId, string TargetId) : BattleEvent;

public sealed record CellVisited(string UnitId, GridPos Cell) : BattleEvent;

public sealed record CellSeized(string UnitId, GridPos Cell) : BattleEvent;

public sealed record UnitEscaped(string UnitId) : BattleEvent;

/// <summary>A "leave KEY" trigger took the unit off the map.</summary>
public sealed record UnitDeparted(string UnitId) : BattleEvent;

/// <summary>A crest-bearer fell: <see cref="Stunned"/> beasts lose their next phase, <see cref="Surrendered"/> humans leave.</summary>
public sealed record CrestSevered(string BearerId, IReadOnlyList<string> Stunned, IReadOnlyList<string> Surrendered) : BattleEvent;

/// <summary>A stunned unit skipped its phase.</summary>
public sealed record UnitStunnedSkip(string UnitId) : BattleEvent;

// ---------------------------------------------------------------- progression and abilities (M5)

public sealed record ExpGained(string UnitId, int Amount, int ExpAfter, IReadOnlyList<LevelUp> LevelUps) : BattleEvent;

/// <summary>HP restored by a spell, item, crest skill or terrain. <see cref="SourceId"/> is the healer (or the unit itself).</summary>
public sealed record UnitHealed(string SourceId, string TargetId, int Amount, int HpAfter, string How) : BattleEvent;

/// <summary>A stat buff (Ward, Rally) or a stat booster.</summary>
public sealed record UnitBuffed(string UnitId, string Source, StatBlock Bonus) : BattleEvent;

/// <summary>Dawn: the unit may act again.</summary>
public sealed record UnitRefreshed(string UnitId) : BattleEvent;

public sealed record CrestSkillUsed(string UnitId, CrestSkill Skill, IReadOnlyList<string> TargetIds) : BattleEvent;

public sealed record ItemUsed(string UnitId, string ItemId) : BattleEvent;

public sealed record ItemsTraded(string UnitId, string PartnerId) : BattleEvent;
