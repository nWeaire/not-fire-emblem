using System.Collections.Generic;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public static class BattleSetup
{
    /// <summary>Growths every generic unit gets on top of its class growths.</summary>
    public static readonly StatBlock GenericGrowths = new(HP: 50, Str: 30, Mag: 30, Dex: 30, Spd: 30, Lck: 20, Def: 25, Res: 25, Mov: 0);

    /// <summary>
    /// Builds a battle from a map. <paramref name="roster"/> supplies persistent states for named units
    /// (keyed by unit id); named units without one are created fresh from their definition.
    /// </summary>
    public static BattleState Create(ContentDatabase db, MapDef map, ulong seed, IReadOnlyDictionary<string, UnitState>? roster = null)
    {
        var state = new BattleState(db, map, map.BuildGrid(db), new Rng(seed));
        int index = 0;
        foreach (var spawn in map.Spawns)
            state.AddUnit(CreateUnit(db, spawn, $"u{index++}", roster));
        return state;
    }

    public static BattleUnit CreateUnit(ContentDatabase db, SpawnDef spawn, string battleId, IReadOnlyDictionary<string, UnitState>? roster = null)
    {
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
            var def = new UnitDef($"generic_{cls.Id}", spawn.Name ?? cls.Name, cls.Id, level, default, GenericGrowths, spawn.Inventory, false,
                spawn.Armor);
            unitState = UnitState.FromDef(def, StatCalculator.GenericGains(cls, GenericGrowths, level));
        }

        if (unitState.EquippedItemId is null)
        {
            foreach (var weapon in Combat.CombatCalculator.UsableWeapons(db, unitState))
            {
                unitState.Equip(weapon.Id);
                break;
            }
        }

        return new BattleUnit
        {
            Id = battleId,
            State = unitState,
            Faction = spawn.Faction,
            Pos = spawn.Pos,
            Home = spawn.Pos,
            CurrentHp = StatCalculator.Effective(db, unitState).HP,
            AiProfile = spawn.Ai,
            IsBoss = spawn.IsBoss,
            SpriteId = spawn.ResolveSpriteId(),
        };
    }
}
