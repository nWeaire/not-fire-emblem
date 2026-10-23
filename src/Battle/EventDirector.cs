using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Runs a map's triggers (IMPLEMENTATION §5.5). State-based conditions (defeated, enter, adjacent, flag, enemy
/// count, HP) are checked after every command and at every phase start; turn conditions at phase start; talk,
/// visit and combat conditions when those actions happen. Every trigger fires at most once.
/// </summary>
public static class EventDirector
{
    private const int MaxChainedPasses = 8;

    /// <summary>At the start of a phase: turn triggers, then any state triggers they unlocked.</summary>
    public static void OnPhaseStart(BattleState s, List<BattleEvent> events)
    {
        foreach (var t in Pending(s).Where(t => t.Condition is TurnStartCondition c && c.Turn == s.Turn && c.Phase == s.Phase).ToList())
            Fire(s, t, null, events);
        AfterCommand(s, events);
    }

    /// <summary>After a command: fires state-based triggers until nothing new fires.</summary>
    public static void AfterCommand(BattleState s, List<BattleEvent> events, BattleUnit? actor = null)
    {
        for (int pass = 0; pass < MaxChainedPasses; pass++)
        {
            bool fired = false;
            foreach (var t in Pending(s).ToList())
            {
                if (StateConditionMet(s, t.Condition, out var who))
                {
                    Fire(s, t, who ?? actor, events);
                    fired = true;
                }
            }
            if (!fired)
                return;
        }
    }

    /// <summary>Before a fight: boss quotes and other "combat KEY" triggers.</summary>
    public static void BeforeCombat(BattleState s, BattleUnit attacker, BattleUnit defender, List<BattleEvent> events)
    {
        foreach (var t in Pending(s).Where(t => t.Condition is CombatCondition c && (c.Key == attacker.Key || c.Key == defender.Key)).ToList())
            Fire(s, t, attacker, events);
    }

    // ------------------------------------------------------------------ talk and visit

    /// <summary>The pending talk trigger between these two units, if any.</summary>
    public static TriggerDef? TalkTrigger(BattleState s, BattleUnit actor, BattleUnit target) =>
        Pending(s).FirstOrDefault(t => t.Condition is TalkCondition c
                                       && (c.Actor == "any" ? actor.Faction == Faction.Player : c.Actor.Split('|').Contains(actor.Key))
                                       && c.Target == target.Key);

    /// <summary>Units <paramref name="actor"/> could talk to from <paramref name="from"/> (adjacent, with a talk trigger).</summary>
    public static IEnumerable<BattleUnit> TalkTargets(BattleState s, BattleUnit actor, GridPos from) =>
        s.Living.Where(t => t != actor && t.Pos.DistanceTo(from) == 1 && TalkTrigger(s, actor, t) is not null);

    public static TriggerDef? VisitTrigger(BattleState s, GridPos cell) =>
        Pending(s).FirstOrDefault(t => t.Condition is VisitCondition c && c.Cell == cell);

    public static void FireTalk(BattleState s, BattleUnit actor, BattleUnit target, List<BattleEvent> events)
    {
        events.Add(new UnitTalked(actor.Id, target.Id));
        if (TalkTrigger(s, actor, target) is { } t)
            Fire(s, t, actor, events);
    }

    /// <summary>A chest's contents: fires the cell's "chest X,Y" trigger, if any, for the unit that opened it.</summary>
    public static void FireChest(BattleState s, BattleUnit actor, GridPos cell, List<BattleEvent> events)
    {
        if (Pending(s).FirstOrDefault(t => t.Condition is ChestCondition c && c.Cell == cell) is { } t)
            Fire(s, t, actor, events);
    }

    public static void FireVisit(BattleState s, BattleUnit actor, List<BattleEvent> events)
    {
        s.VisitedCells.Add(actor.Pos);
        events.Add(new CellVisited(actor.Id, actor.Pos));
        if (VisitTrigger(s, actor.Pos) is { } t)
            Fire(s, t, actor, events);
    }

    // ------------------------------------------------------------------ internals

    private static IEnumerable<TriggerDef> Pending(BattleState s) =>
        s.Map.AllTriggers.Where(t => !s.FiredTriggers.Contains(t.Id) && !t.Unless.Any(g => GuardHolds(s, g)));

    private static bool GuardHolds(BattleState s, TriggerGuard guard) => guard.Kind switch
    {
        "flag" => s.Flags.Contains(guard.Arg),
        "joined" => s.ByKey(guard.Arg) is { Faction: Faction.Player },
        _ => false,
    };

    private static bool StateConditionMet(BattleState s, TriggerCondition condition, out BattleUnit? who)
    {
        who = null;
        switch (condition)
        {
            case DefeatedCondition c:
                // Any unit with the key (several can share one, like Ch 8's agitators).
                who = s.AllByKey(c.Key).FirstOrDefault(u => u is { IsOnMap: false, Exit: not (MapExit.Escaped or MapExit.Departed or MapExit.Subdued) });
                return who is not null;
            case SubduedCondition c:
                who = s.AllByKey(c.Key).FirstOrDefault(u => u.Exit == MapExit.Subdued);
                return who is not null;
            case EnterCondition c:
                who = Matching(s, c.Who).FirstOrDefault(u => c.Cells.Contains(u.Pos));
                return who is not null;
            case AdjacentCondition c:
                var a = s.ByKey(c.A);
                var b = s.ByKey(c.B);
                who = a;
                return a is { IsOnMap: true } && b is { IsOnMap: true } && a.Pos.DistanceTo(b.Pos) == 1;
            case FlagCondition c:
                return s.Flags.Contains(c.Flag);
            case EnemiesAtMostCondition c:
                return s.LivingOf(Faction.Enemy).Count() <= c.Count;
            case HpBelowCondition c:
                return s.ByKey(c.Key) is { IsOnMap: true } h && h.CurrentHp * 100 < s.Stats(h).HP * c.Percent && (who = h) is not null;
            default:
                return false;
        }
    }

    private static IEnumerable<BattleUnit> Matching(BattleState s, string who) => who switch
    {
        "player" => s.LivingOf(Faction.Player),
        "enemy" => s.LivingOf(Faction.Enemy),
        "ally" => s.LivingOf(Faction.Ally),
        "neutral" => s.LivingOf(Faction.Neutral),
        "any" => s.Living,
        _ => s.Living.Where(u => u.Key == who),
    };

    private static void Fire(BattleState s, TriggerDef trigger, BattleUnit? actor, List<BattleEvent> events)
    {
        s.FiredTriggers.Add(trigger.Id);
        foreach (var effect in trigger.Effects)
            Apply(s, effect, actor, events);
    }

    private static BattleUnit? Resolve(BattleState s, string key, BattleUnit? actor) =>
        key == "actor" ? actor : s.ByKey(key);

    private static void Apply(BattleState s, TriggerEffect effect, BattleUnit? actor, List<BattleEvent> events)
    {
        switch (effect)
        {
            case SceneEffect e:
                if (s.Db.Scenes.TryGetValue(e.SceneId, out var scene))
                    s.Flags.UnionWith(scene.Flags);
                events.Add(new DialogueRequested(e.SceneId));
                break;
            case SpawnEffect e:
                foreach (var spawn in s.Map.Spawns.Where(sp => sp.Group == e.Group))
                {
                    if (spawn.HardOnly && s.Options.Difficulty != Core.Difficulty.Hard)
                        continue;
                    var unit = BattleSetup.CreateUnit(s.Db, spawn, $"u{s.NextUnitIndex++}", null, s.Options);
                    unit.Pos = s.NearestFreeCell(spawn.Pos, StatCalculator.MovementTypeOf(s.Db, unit.State));
                    s.AddUnit(unit);
                    // Reinforcements arrive ready to act on their side's next phase.
                    events.Add(new UnitSpawned(unit.Id));
                }
                break;
            case FlagEffect e:
                s.Flags.Add(e.Flag);
                break;
            case FactionEffect e when Resolve(s, e.Key, actor) is { IsOnMap: true } u:
                u.Faction = e.Faction;
                u.ActionState = ActionState.Ready;
                if (e.Faction == Faction.Player)
                {
                    u.DominatedBy = null;
                    u.AiProfile = AiProfile.Aggressive; // a guard post no longer applies once the player commands them
                }
                events.Add(new FactionChanged(u.Id, e.Faction));
                break;
            case GiveEffect e when Resolve(s, e.Key, actor) is { } u:
                bool toConvoy = u.State.Inventory.Count >= UnitState.MaxInventory || !s.Db.IsInventoryItem(e.ItemId);
                if (toConvoy)
                    s.ItemsToConvoy.Add(e.ItemId);
                else
                    u.State.Inventory.Add(e.ItemId);
                events.Add(new ItemReceived(u.Id, e.ItemId, toConvoy));
                break;
            case GoldEffect e:
                s.GoldGained += e.Amount;
                events.Add(new GoldReceived(e.Amount));
                break;
            case TerrainEffect e:
                var terrain = s.Db.TerrainBySymbol(e.Symbol);
                foreach (var cell in e.Cells.Where(s.Grid.InBounds))
                    s.Grid.SetTerrain(cell, terrain);
                events.Add(new TerrainChanged(e.Cells));
                break;
            case DamageEffect e when Resolve(s, e.Key, actor) is { IsOnMap: true, Invulnerable: false } u:
                int dealt = System.Math.Min(e.Amount, u.CurrentHp - 1);
                u.CurrentHp -= dealt;
                events.Add(new UnitDamaged(u.Id, dealt, u.CurrentHp));
                break;
            case LeaveEffect e:
                foreach (var u in s.Units.Where(u => u.IsOnMap && u.Key == e.Key).ToList())
                {
                    s.RemoveFromMap(u, MapExit.Departed);
                    events.Add(new UnitDeparted(u.Id));
                }
                break;
            case EndEffect e:
                s.ForcedOutcome = e.Outcome;
                break;
        }
    }
}
