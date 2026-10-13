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
    bool IsPromoted,
    ArmorClass MaxArmor = ArmorClass.Medium); // the heaviest body armour the class can wear

/// <summary>
/// Equipment never changes a unit's look (the class does); it only changes numbers.
/// <see cref="Tier"/> is the gear tier (1 iron … 4 legendary) used to stage shops by region.
/// </summary>
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
    int Price,
    int Tier = 1,
    string? PersonalTo = null); // unit id for unique weapons only that unit can wield

public enum ArmorSlot { Body, Accessory }

/// <summary>Body armour weight class. A class can wear its <see cref="ClassDef.MaxArmor"/> or anything lighter.</summary>
public enum ArmorClass { Light, Medium, Heavy }

/// <summary>Body armour or an accessory: flat stat bonuses, with weight that slows weaker wearers.</summary>
public sealed record ArmorDef(
    string Id,
    string Name,
    ArmorSlot Slot,
    ArmorClass Class,          // ignored for accessories
    StatBlock Bonuses,
    int Weight,
    int Price,
    int Tier = 1,
    string? PersonalTo = null);

public sealed record UnitDef(
    string Id,
    string Name,
    string ClassId,
    int Level,
    StatBlock Bases,   // personal modifiers added on top of the class bases
    StatBlock Growths, // personal growths added to class growths
    IReadOnlyList<string> Inventory,
    bool IsLord,
    string? Armor = null,      // starting body armour id
    string? Accessory = null); // starting accessory id

/// <summary>A shop that opens when the story reaches a region. Its stock is what that region can supply.</summary>
public sealed record ShopDef(
    string Id,
    string Name,
    string Region,
    int OpensAtChapter, // story order: 0 = prologue, 1 = Ch 1, ...
    IReadOnlyList<string> Items);
