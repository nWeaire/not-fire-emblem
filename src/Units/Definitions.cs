using System.Collections.Generic;

namespace NotFireEmblem.Units;

// Immutable runtime content records. ContentLoader converts the Godot Resources in data/ into these;
// pure logic never sees a Resource.

public sealed record TerrainDef(
    string Id,
    string Name,
    char Symbol,
    IReadOnlyDictionary<MovementType, int> MoveCost, // 0 = impassable
    int Avoid,
    int Def,
    int HealPercent,
    bool BlocksSight,
    string ColorHex)
{
    public int CostFor(MovementType type) => MoveCost.TryGetValue(type, out var cost) ? cost : 0;
}

public sealed record ClassDef(
    string Id,
    string Name,
    StatBlock Bases,
    StatBlock Growths,
    StatBlock Caps,
    MovementType MovementType,
    IReadOnlyList<WeaponType> Usable,
    IReadOnlyList<string> PromotesTo,
    bool IsPromoted);

public sealed record WeaponDef(
    string Id,
    string Name,
    WeaponType Type,
    int Might,
    int Hit,
    int Crit,
    int Weight,
    int MinRange,
    int MaxRange,
    bool Magic,
    IReadOnlyList<string> EffectiveAgainst, // movement type names, e.g. "Flying"
    int Price);

public sealed record UnitDef(
    string Id,
    string Name,
    string ClassId,
    int Level,
    StatBlock Bases,   // personal modifiers added on top of the class bases
    StatBlock Growths, // personal growths added to class growths
    IReadOnlyList<string> Inventory,
    bool IsLord);
