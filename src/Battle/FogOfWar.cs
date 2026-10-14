using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// Fog of war (GDD §3): on fog maps each side sees only cells within its units' vision radius
/// (3 + the map's vision bonus, Manhattan). Player and Ally units share their vision. Line of sight isn't modelled.
/// </summary>
public static class FogOfWar
{
    public const int BaseVision = 3;

    public static HashSet<GridPos> VisibleCells(BattleState s, Faction viewer)
    {
        if (!s.Map.Fog)
            return s.Grid.AllPositions().ToHashSet();
        int radius = BaseVision + s.Map.VisionBonus;
        var cells = new HashSet<GridPos>();
        foreach (var unit in s.Living.Where(u => !u.Faction.IsHostileTo(viewer)))
            cells.UnionWith(RangeService.CellsInRange(s.Grid, unit.Pos, 0, radius));
        return cells;
    }
}
