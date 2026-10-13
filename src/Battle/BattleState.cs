using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>Everything about a battle in progress. Changed only by commands submitted to the TurnManager.</summary>
public sealed class BattleState
{
    private readonly Dictionary<GridPos, BattleUnit> _occupancy = new();
    private readonly List<BattleUnit> _units = new();

    public ContentDatabase Db { get; }
    public MapDef Map { get; }
    public BattleGrid Grid { get; }
    public IRng Rng { get; }

    public int Turn { get; set; } = 1;
    public Faction Phase { get; set; } = Faction.Player;
    public BattleOutcome Outcome { get; set; } = BattleOutcome.InProgress;
    public List<IBattleCommand> CommandLog { get; } = new();

    public BattleState(ContentDatabase db, MapDef map, BattleGrid grid, IRng rng)
    {
        Db = db;
        Map = map;
        Grid = grid;
        Rng = rng;
    }

    /// <summary>All units, including defeated ones.</summary>
    public IReadOnlyList<BattleUnit> Units => _units;

    public IEnumerable<BattleUnit> Living => _units.Where(u => !u.IsDefeated);

    public IEnumerable<BattleUnit> LivingOf(Faction faction) => Living.Where(u => u.Faction == faction);

    public BattleUnit Unit(string id) =>
        _units.FirstOrDefault(u => u.Id == id) ?? throw new KeyNotFoundException($"No unit with id '{id}'.");

    public BattleUnit? TryUnit(string id) => _units.FirstOrDefault(u => u.Id == id);

    public BattleUnit? UnitAt(GridPos p) => _occupancy.TryGetValue(p, out var u) ? u : null;

    public Faction? OccupantFaction(GridPos p) => UnitAt(p)?.Faction;

    public void AddUnit(BattleUnit unit)
    {
        if (!Grid.InBounds(unit.Pos))
            throw new InvalidOperationException($"{unit} is outside the map.");
        if (_occupancy.ContainsKey(unit.Pos))
            throw new InvalidOperationException($"{unit} spawned on occupied cell {unit.Pos}.");
        _units.Add(unit);
        _occupancy[unit.Pos] = unit;
    }

    public void MoveUnit(BattleUnit unit, GridPos to)
    {
        if (unit.Pos == to)
            return;
        if (_occupancy.ContainsKey(to))
            throw new InvalidOperationException($"Cannot move {unit} onto occupied cell {to}.");
        _occupancy.Remove(unit.Pos);
        unit.Pos = to;
        _occupancy[to] = unit;
    }

    public void Defeat(BattleUnit unit)
    {
        unit.IsDefeated = true;
        unit.CurrentHp = 0;
        _occupancy.Remove(unit.Pos);
    }

    public StatBlock Stats(BattleUnit unit) => StatCalculator.Effective(Db, unit.State);

    public MovementType MoveType(BattleUnit unit) => StatCalculator.MovementTypeOf(Db, unit.State);

    public ReachMap Reach(BattleUnit unit) =>
        Pathfinder.GetReachable(Grid, new Mover(unit.Pos, unit.Faction, MoveType(unit), Stats(unit).Mov), OccupantFaction);
}
