using System.Linq;
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
    ArmorClass MaxArmor = ArmorClass.Medium, // the heaviest body armour the class can wear
    int Tier = 1,                            // 1 base, 2 advanced, 3 crestbound, 4 final; promotion needs crest tier ≥ this
    IReadOnlyList<string>? Traits = null,    // e.g. "Beast" (effective damage, severed beasts are stunned)
    IReadOnlyList<string>? Arts = null,      // combat arts every unit of this class knows
    IReadOnlyList<string>? Spells = null,    // spells every unit of this class knows
    string? PromotesFrom = null)             // tier 3 Crestbound classes: the class they follow (art falls back to it)
{
    public bool HasTrait(string trait) => Traits is not null && Traits.Contains(trait);
}

/// <summary>
/// Equipment never changes a unit's look (the class does); it only changes numbers.
/// <see cref="Tier"/> is the gear tier (1 iron, 2 steel, 3 silver, 4 black steel or legendary, 5 mythril) used to stage shops by region.
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
    string? Accessory = null,  // starting accessory id
    IReadOnlyList<string>? Arts = null,
    IReadOnlyList<string>? Spells = null,
    IReadOnlyList<SupportDef>? Supports = null,
    string? Crestbound = null,  // the unit's own tier-3 class (STORY §4.2: a unique third tier per companion)
    bool Retreats = false);     // story-critical: at 0 HP the unit retreats wounded instead of dying, even in Iron Man

/// <summary>A support pairing seen from one unit: the points needed for ranks C, B and A.</summary>
public sealed record SupportDef(string PartnerId, int C, int B, int A)
{
    public int Threshold(int rank) => rank switch { 1 => C, 2 => B, 3 => A, _ => int.MaxValue };
}

/// <summary>Key items (Door Key, Chest Key) are used up by the Unlock action, not from the Item menu.</summary>
public enum ItemKind { Heal, StatBoost, Key }

/// <summary>A consumable or key item carried in the 5-slot inventory. Used up when used.</summary>
public sealed record ItemDef(
    string Id,
    string Name,
    ItemKind Kind,
    int Amount,          // HP healed (0 = full), or stat points gained
    Stat Stat,           // for StatBoost
    int Price);

/// <summary>
/// A combat art: a special attack with a weapon of <see cref="Type"/>. Uses refill after every map (GDD §3).
/// </summary>
public sealed record ArtDef(
    string Id,
    string Name,
    WeaponType Type,
    int MightMod,
    int HitMod,
    int CritMod,
    int RangeBonus,       // added to the weapon's max range
    int Uses,
    bool NoCounter = false,
    IReadOnlyList<string>? EffectiveAgainst = null); // movement types or class traits

/// <summary>A healing spell (staff-style). Offensive tomes are weapons. Uses refill after every map.</summary>
public sealed record SpellDef(
    string Id,
    string Name,
    int Power,            // HP healed = Power + Mag / 2
    int MinRange,
    int MaxRange,
    int Uses);

/// <summary>A shop that opens when the story reaches a region. Its stock is what that region can supply.</summary>
public sealed record ShopDef(
    string Id,
    string Name,
    string Region,
    int OpensAtChapter, // story order: 0 = prologue, 1 = Ch 1, ...
    IReadOnlyList<string> Items);
