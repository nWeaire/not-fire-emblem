using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;

namespace NotFireEmblem.Units;

/// <summary>
/// Equipment rules: who may equip what, and which shops are open. Equipment is separate from class:
/// it changes stats only, never the unit's look. Used by the prep/hub screens (M6), not during battle.
/// </summary>
public static class EquipmentService
{
    public static bool CanEquipArmor(ContentDatabase db, UnitState unit, ArmorDef armor)
    {
        if (armor.PersonalTo is not null && armor.PersonalTo != unit.Def.Id)
            return false;
        return armor.Slot == ArmorSlot.Accessory || armor.Class <= db.Class(unit.ClassId).MaxArmor;
    }

    /// <summary>Puts on body armour or an accessory (by its slot). Returns the item it replaced, or an error.</summary>
    public static (bool Ok, string? Replaced, string? Error) EquipArmor(ContentDatabase db, UnitState unit, string armorId)
    {
        var armor = db.TryArmor(armorId);
        if (armor is null)
            return (false, null, $"Unknown armour '{armorId}'.");
        if (!CanEquipArmor(db, unit, armor))
            return (false, null, $"{unit.Def.Name} can't wear {armor.Name}.");
        string? replaced;
        if (armor.Slot == ArmorSlot.Body)
        {
            replaced = unit.ArmorId;
            unit.ArmorId = armor.Id;
        }
        else
        {
            replaced = unit.AccessoryId;
            unit.AccessoryId = armor.Id;
        }
        return (true, replaced, null);
    }

    /// <summary>
    /// Body armour a unit can no longer wear (e.g. after a class change to a lighter class) is taken off.
    /// Returns the removed item id so the caller can send it to the convoy.
    /// </summary>
    public static string? RemoveInvalidArmor(ContentDatabase db, UnitState unit)
    {
        if (db.TryArmor(unit.ArmorId) is { } armor && !CanEquipArmor(db, unit, armor))
        {
            unit.ArmorId = null;
            return armor.Id;
        }
        return null;
    }

    /// <summary>Shops open at the given point in the story, in the order they opened.</summary>
    public static IEnumerable<ShopDef> OpenShops(ContentDatabase db, int chapter) =>
        db.Shops.Values.Where(s => s.OpensAtChapter <= chapter).OrderBy(s => s.OpensAtChapter).ThenBy(s => s.Id);

    /// <summary>Every item purchasable at this point in the story (union of all open shops).</summary>
    public static IEnumerable<string> PurchasableItems(ContentDatabase db, int chapter) =>
        OpenShops(db, chapter).SelectMany(s => s.Items).Distinct();
}
