using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Campaign context a battle needs: Rowan's crest tier (aura, crest skills), difficulty, and the support ranks
/// between roster units (by unit id; 0 none, 1 C, 2 B, 3 A).
/// </summary>
public sealed record BattleOptions(
    int CrestTier = 0,
    Difficulty Difficulty = Difficulty.Normal,
    Func<string, string, int>? SupportRank = null,
    bool GainExp = true)
{
    public static readonly BattleOptions Default = new();
}

/// <summary>Everything about a battle in progress. Changed only by commands submitted to the TurnManager.</summary>
public sealed class BattleState
{
    public const int DominionRadius = 3;
    public const int AuraRadius = 2;
    public const int SupportRadius = 2;
    public const int AdjacentHit = 10;
    public const int AdjacentAvoid = 5;
    public static readonly StatBlock DominionBonus = new(0, 2, 0, 0, 0, 0, 2, 0, 0);

    private readonly Dictionary<GridPos, BattleUnit> _occupancy = new();
    private readonly List<BattleUnit> _units = new();

    public ContentDatabase Db { get; }
    public MapDef Map { get; }
    public BattleGrid Grid { get; }
    public IRng Rng { get; }
    public BattleOptions Options { get; }

    public int Turn { get; set; } = 1;
    public Faction Phase { get; set; } = Faction.Player;
    public BattleOutcome Outcome { get; set; } = BattleOutcome.InProgress;
    public List<IBattleCommand> CommandLog { get; } = new();

    /// <summary>Story flags set during this battle (by scenes and triggers). Merged into the session on victory.</summary>
    public HashSet<string> Flags { get; } = new();
    public HashSet<string> FiredTriggers { get; } = new();
    public HashSet<GridPos> VisitedCells { get; } = new();
    /// <summary>Gold and items picked up this battle; they go to the session on victory.</summary>
    public int GoldGained { get; set; }
    public List<string> ItemsToConvoy { get; } = new();
    /// <summary>Support points earned this battle, keyed by <see cref="Supports.PairKey"/>.</summary>
    public Dictionary<string, int> SupportPoints { get; } = new();
    /// <summary>Losses among protected (tagged) units, for Defend objectives.</summary>
    public Dictionary<string, int> TagLosses { get; } = new();
    public int NextUnitIndex { get; set; }
    /// <summary>Objective progress: a Seize was performed; the Survive turns have passed; a trigger ended the battle.</summary>
    public bool Seized { get; set; }
    public bool SurviveComplete { get; set; }
    public BattleOutcome? ForcedOutcome { get; set; }

    public BattleState(ContentDatabase db, MapDef map, BattleGrid grid, IRng rng, BattleOptions? options = null)
    {
        Db = db;
        Map = map;
        Grid = grid;
        Rng = rng;
        Options = options ?? BattleOptions.Default;
    }

    /// <summary>All units, including defeated and departed ones.</summary>
    public IReadOnlyList<BattleUnit> Units => _units;

    /// <summary>Units still on the map.</summary>
    public IEnumerable<BattleUnit> Living => _units.Where(u => u.IsOnMap);

    public IEnumerable<BattleUnit> LivingOf(Faction faction) => Living.Where(u => u.Faction == faction);

    public BattleUnit Unit(string id) =>
        _units.FirstOrDefault(u => u.Id == id) ?? throw new KeyNotFoundException($"No unit with id '{id}'.");

    public BattleUnit? TryUnit(string id) => _units.FirstOrDefault(u => u.Id == id);

    /// <summary>The unit with this objective/trigger key (spawn tag or unit id), on the map or not.</summary>
    public BattleUnit? ByKey(string key) => _units.FirstOrDefault(u => u.Key == key);

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
        if (unit.Tag is { } tag)
            TagLosses[tag] = TagLosses.GetValueOrDefault(tag) + 1;
    }

    /// <summary>The unit leaves the map alive (escaped, or surrendered after severance).</summary>
    public void RemoveFromMap(BattleUnit unit, MapExit exit)
    {
        unit.Exit = exit;
        _occupancy.Remove(unit.Pos);
    }

    /// <summary>The nearest empty, enterable cell to <paramref name="p"/> (for reinforcements landing on someone).</summary>
    public GridPos NearestFreeCell(GridPos p, MovementType moveType)
    {
        bool Free(GridPos c) => Grid.InBounds(c) && UnitAt(c) is null && Grid[c].Terrain.CostFor(moveType) > 0;
        if (Free(p))
            return p;
        for (int r = 1; r < Grid.Width + Grid.Height; r++)
        {
            var ring = Grid.AllPositions().Where(c => c.DistanceTo(p) == r && Free(c)).OrderBy(c => c.Y).ThenBy(c => c.X);
            foreach (var c in ring)
                return c;
        }
        return p;
    }

    // ------------------------------------------------------------------ stats and bonuses

    /// <summary>Effective stats: class + personal + gains (capped) + gear, plus crest buffs and Dominion.</summary>
    public StatBlock Stats(BattleUnit unit)
    {
        var stats = StatCalculator.Effective(Db, unit.State) + unit.TempBonus;
        if (IsDominated(unit))
            stats += DominionBonus;
        return stats;
    }

    /// <summary>
    /// True when a unit the unit is dominated by stands within Dominion range. Several units can share the key
    /// (Ch 8's agitators): any of them will do.
    /// </summary>
    public bool IsDominated(BattleUnit unit) =>
        unit.DominatedBy is { } key && Living.Any(b => b.Key == key && b != unit && b.Pos.DistanceTo(unit.Pos) <= DominionRadius);

    /// <summary>Every unit with this key, on the map or not (a boss key can name a group, e.g. "agitator").</summary>
    public IEnumerable<BattleUnit> AllByKey(string key) => _units.Where(u => u.Key == key);

    /// <summary>
    /// Hit, avoid and crit bonuses for a unit standing at <paramref name="pos"/>: Rowan's crest aura
    /// (+5 hit/avoid per crest tier for allies within 2 tiles) and support partners within 2 tiles
    /// (C +5/+5, B +10/+10/+5 crit, A +15/+15/+10 crit). Any friend standing right next to the unit adds
    /// <see cref="AdjacentHit"/> hit and <see cref="AdjacentAvoid"/> avoid, support or not.
    /// </summary>
    public (int Hit, int Avoid, int Crit) CombatBonus(BattleUnit unit, GridPos pos)
    {
        int hit = 0, avoid = 0, crit = 0;
        if (unit.Faction.IsPlayerSide() && Options.CrestTier > 0)
        {
            bool inAura = LivingOf(Faction.Player).Any(l => l.IsLord && l != unit && l.Pos.DistanceTo(pos) <= AuraRadius);
            if (inAura)
            {
                hit += 5 * Options.CrestTier;
                avoid += 5 * Options.CrestTier;
            }
        }
        if (unit.Faction.IsPlayerSide() && HasFriendBeside(unit, pos))
        {
            hit += AdjacentHit;
            avoid += AdjacentAvoid;
        }
        if (Options.SupportRank is { } rankOf && unit.Faction == Faction.Player)
        {
            foreach (var partner in LivingOf(Faction.Player))
            {
                if (partner == unit || partner.Pos.DistanceTo(pos) > SupportRadius)
                    continue;
                int rank = rankOf(unit.State.Def.Id, partner.State.Def.Id);
                hit += rank * 5;
                avoid += rank * 5;
                crit += rank >= 2 ? (rank - 1) * 5 : 0;
            }
        }
        return (hit, avoid, crit);
    }

    /// <summary>Whether a living unit of <paramref name="unit"/>'s side stands next to <paramref name="pos"/>.</summary>
    public bool HasFriendBeside(BattleUnit unit, GridPos pos) =>
        Living.Any(f => f != unit && f.Faction == unit.Faction && f.Pos.DistanceTo(pos) == 1);

    public MovementType MoveType(BattleUnit unit) => StatCalculator.MovementTypeOf(Db, unit.State);

    public ReachMap Reach(BattleUnit unit) =>
        Pathfinder.GetReachable(Grid, new Mover(unit.Pos, unit.Faction, MoveType(unit), Stats(unit).Mov), OccupantFaction);

    // ------------------------------------------------------------------ fog of war

    /// <summary>Cells <paramref name="faction"/> (and its friends) can see. Without fog, every cell.</summary>
    public HashSet<GridPos> VisibleCells(Faction faction) => FogOfWar.VisibleCells(this, faction);

    /// <summary>Whether <paramref name="viewer"/> can see <paramref name="unit"/> (always, without fog or for friends).</summary>
    public bool CanSee(Faction viewer, BattleUnit unit) =>
        !Map.Fog || !viewer.IsHostileTo(unit.Faction) || VisibleCells(viewer).Contains(unit.Pos);
}
