using System.Collections.Generic;

namespace NotFireEmblem.Units;

/// <summary>A unit's persistent state: owned by the session for roster units, generated for enemies.</summary>
public sealed class UnitState
{
    public const int MaxInventory = 5;

    public required UnitDef Def { get; init; }
    public required string ClassId { get; set; }
    public int Level { get; set; } = 1;
    public int Exp { get; set; }
    public StatBlock Gains { get; set; }
    public List<string> Inventory { get; } = new();
    public int EquippedIndex { get; set; } = -1;
    public UnitStatus Status { get; set; } = UnitStatus.Active;

    public string? EquippedItemId =>
        EquippedIndex >= 0 && EquippedIndex < Inventory.Count ? Inventory[EquippedIndex] : null;

    public static UnitState FromDef(UnitDef def, StatBlock gains = default)
    {
        var state = new UnitState { Def = def, ClassId = def.ClassId, Level = def.Level, Gains = gains };
        state.Inventory.AddRange(def.Inventory);
        return state;
    }

    /// <summary>Equips the first inventory slot holding this item. Returns false if it isn't carried.</summary>
    public bool Equip(string itemId)
    {
        int index = Inventory.IndexOf(itemId);
        if (index < 0)
            return false;
        EquippedIndex = index;
        return true;
    }
}
