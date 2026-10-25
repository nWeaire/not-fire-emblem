using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public static class BattleSetup
{
    /// <summary>
    /// Growths every generic unit gets on top of its class growths. Class growths are shared with the player's units,
    /// so this is where enemies are made to scale with their level (session 9: +55% Str/Mag a level, was 30%).
    /// </summary>
    public static readonly StatBlock GenericGrowths = new(HP: 80, Str: 55, Mag: 55, Dex: 40, Spd: 35, Lck: 25, Def: 40, Res: 30, Mov: 0);

    /// <summary>On Hard, generic enemies get the stat gains of a unit this many levels higher.</summary>
    public const int HardLevelBonus = 3;

    /// <summary>
    /// Bosses get this share of their level as extra Res (session 15 playtest: mages one-shot most bosses; Elise's
    /// Thunder did 23 a hit to Castellan, now about 13).
    /// </summary>
    public const int BossResPercentOfLevel = 67;

    public static int BossResBonus(int level) => level * BossResPercentOfLevel / 100;

    /// <summary>
    /// Builds a battle from a map.
    /// <list type="bullet">
    /// <item><paramref name="roster"/> supplies persistent states for named units (keyed by unit id); named units
    /// without one are created fresh from their definition. Dead roster units are left off the map.</item>
    /// <item><paramref name="deployed"/> lists roster unit ids chosen in prep. Those without a fixed spawn on the
    /// map take the map's deploy slots in order. Null = only the map's fixed spawns.</item>
    /// </list>
    /// </summary>
    public static BattleState Create(ContentDatabase db, MapDef map, ulong seed, IReadOnlyDictionary<string, UnitState>? roster = null,
        BattleOptions? options = null, IReadOnlyList<string>? deployed = null)
    {
        options ??= BattleOptions.Default;
        var state = new BattleState(db, map, map.BuildGrid(db), new Rng(seed), options);

        foreach (var spawn in map.Spawns)
        {
            if (spawn.Group is not null || (spawn.HardOnly && options.Difficulty != Difficulty.Hard))
                continue;
            if (spawn.UnitId is { } id && roster is not null && roster.TryGetValue(id, out var existing) && existing.Status == UnitStatus.Dead)
                continue;
            state.AddUnit(CreateUnit(db, spawn, $"u{state.NextUnitIndex++}", roster, options));
        }

        if (deployed is not null && roster is not null)
        {
            var placed = state.Units.Select(u => u.State.Def.Id).ToHashSet();
            var slots = new Queue<Grid.GridPos>(map.Slots.Where(c => state.UnitAt(c) is null));
            foreach (var unitId in deployed.Where(id => !placed.Contains(id)))
            {
                if (slots.Count == 0 || !roster.TryGetValue(unitId, out var unitState) || unitState.Status == UnitStatus.Dead)
                    continue;
                var spawn = new SpawnDef(unitId, null, null, unitState.Level, System.Array.Empty<string>(), Faction.Player, slots.Dequeue());
                state.AddUnit(CreateUnit(db, spawn, $"u{state.NextUnitIndex++}", roster, options));
            }
        }
        return state;
    }

    public static BattleUnit CreateUnit(ContentDatabase db, SpawnDef spawn, string battleId, IReadOnlyDictionary<string, UnitState>? roster = null,
        BattleOptions? options = null)
    {
        options ??= BattleOptions.Default;
        UnitState unitState;
        if (spawn.UnitId is { } unitId)
        {
            unitState = roster is not null && roster.TryGetValue(unitId, out var existing)
                ? existing
                : UnitState.FromDef(db.Unit(unitId));
        }
        else
        {
            var cls = db.Class(spawn.ClassId!);
            int level = System.Math.Max(1, spawn.Level);
            int statLevel = level + (options.Difficulty == Difficulty.Hard && spawn.Faction == Faction.Enemy ? HardLevelBonus : 0);
            var def = new UnitDef($"generic_{cls.Id}", spawn.Name ?? cls.Name, cls.Id, level, default, GenericGrowths, spawn.Inventory, false,
                spawn.Armor);
            var gains = StatCalculator.GenericGains(cls, GenericGrowths, statLevel);
            if (spawn.IsBoss)
                gains = gains.With(Stat.Res, gains.Res + BossResBonus(level));
            unitState = UnitState.FromDef(def, gains);
        }

        if (unitState.EquippedItemId is null || db.TryWeapon(unitState.EquippedItemId) is null)
        {
            foreach (var weapon in Combat.CombatCalculator.UsableWeapons(db, unitState))
            {
                unitState.Equip(weapon.Id);
                break;
            }
        }

        var unit = new BattleUnit
        {
            Id = battleId,
            State = unitState,
            Faction = spawn.Faction,
            Pos = spawn.Pos,
            Home = spawn.Pos,
            CurrentHp = StatCalculator.Effective(db, unitState).HP,
            AiProfile = spawn.Ai,
            IsBoss = spawn.IsBoss,
            Leash = spawn.Leash,
            SpriteId = spawn.ResolveSpriteId(),
            Key = spawn.Key ?? battleId,
            Tag = spawn.Tag,
            DominatedBy = spawn.DominatedBy,
            CrestBearer = spawn.CrestBearer,
            DominionRadius = spawn.DominionRadius,
            NonLethal = spawn.NonLethal,
            Invulnerable = spawn.Invulnerable,
            Ignores = spawn.Ignores,
            EarlierClasses = spawn.UnitId is null ? System.Array.Empty<string>() : Outfits.Earlier(db, unitState.ClassId),
            WeaponArt = spawn.UnitId is null ? db.TryWeapon(unitState.EquippedItemId)?.Type.ToString().ToLowerInvariant() : null,
        };
        RefillUses(db, unit, options.CrestTier);
        return unit;
    }

    /// <summary>Arts, spells and (for the lord) crest skills start every map with full uses (GDD §3).</summary>
    public static void RefillUses(ContentDatabase db, BattleUnit unit, int crestTier)
    {
        unit.UsesLeft.Clear();
        foreach (var art in Abilities.Arts(db, unit.State))
            unit.UsesLeft[art.Id] = art.Uses;
        foreach (var spell in Abilities.Spells(db, unit.State))
            unit.UsesLeft[spell.Id] = spell.Uses;
        if (unit.IsLord && unit.Faction == Faction.Player)
            foreach (var skill in CrestSkills.Available(crestTier))
                unit.UsesLeft[CrestSkills.UseKey(skill)] = CrestSkills.Uses(skill);
    }
}
