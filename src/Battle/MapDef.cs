using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

/// <summary>
/// A unit placed at battle start. Either a named unit (<see cref="UnitId"/>) or a generic one built from
/// <see cref="ClassId"/> + <see cref="Level"/> + <see cref="Inventory"/>.
/// </summary>
public sealed record SpawnDef(
    string? UnitId,
    string? ClassId,
    string? Name,
    int Level,
    IReadOnlyList<string> Inventory,
    Faction Faction,
    GridPos Pos,
    AiProfile Ai = AiProfile.Aggressive,
    bool IsBoss = false,
    string? SpriteId = null,
    string? Armor = null) // body armour for generic units (named units bring their own)
{
    /// <summary>
    /// The art set this unit uses: an explicit SpriteId, else the named unit's id, else "enemy_&lt;classId&gt;"
    /// (or "ally_"/"player_" for other factions).
    /// </summary>
    public string ResolveSpriteId() =>
        SpriteId ?? UnitId ?? $"{Faction.ToString().ToLowerInvariant()}_{ClassId}";
}

public sealed record MapDef(
    string Id,
    string Name,
    IReadOnlyList<string> TerrainRows,
    IReadOnlyList<string> HeightRows,
    IReadOnlyList<SpawnDef> Spawns)
{
    public BattleGrid BuildGrid(ContentDatabase db) => BattleGrid.FromRows(TerrainRows, HeightRows, db.TerrainBySymbol);

    public IEnumerable<string> Validate(ContentDatabase db)
    {
        var errors = new List<string>();
        BattleGrid? grid = null;
        try
        {
            grid = BuildGrid(db);
        }
        catch (System.Exception e)
        {
            errors.Add($"Map '{Id}': {e.Message}");
        }

        foreach (var spawn in Spawns)
        {
            string where = $"Map '{Id}' spawn at {spawn.Pos}";
            if (spawn.UnitId is null && spawn.ClassId is null)
                errors.Add($"{where} needs a UnitId or a ClassId.");
            if (spawn.UnitId is not null && !db.Units.ContainsKey(spawn.UnitId))
                errors.Add($"{where} has unknown unit '{spawn.UnitId}'.");
            if (spawn.ClassId is not null && !db.Classes.ContainsKey(spawn.ClassId))
                errors.Add($"{where} has unknown class '{spawn.ClassId}'.");
            errors.AddRange(spawn.Inventory.Where(i => !db.Weapons.ContainsKey(i)).Select(i => $"{where} carries unknown item '{i}'."));
            if (spawn.Armor is not null && db.TryArmor(spawn.Armor) is not { Slot: ArmorSlot.Body })
                errors.Add($"{where} wears unknown body armour '{spawn.Armor}'.");
            if (grid is not null && !grid.InBounds(spawn.Pos))
                errors.Add($"{where} is outside the map.");
        }
        foreach (var dup in Spawns.GroupBy(s => s.Pos).Where(g => g.Count() > 1))
            errors.Add($"Map '{Id}' has {dup.Count()} spawns on {dup.Key}.");
        return errors;
    }
}
