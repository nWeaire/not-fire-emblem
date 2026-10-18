using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Combat;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Campaign;

/// <summary>A smithy reforge: a weapon and what it can become, for a price.</summary>
public sealed record ForgeOption(string FromId, string ToId, int Cost);

/// <summary>
/// Hub rules (GDD §3 Equipment, IMPLEMENTATION §5.7 hub screens): shop, convoy, equipping, the smithy and
/// promotion. Every method returns an error message, or null on success.
/// </summary>
public static class HubService
{
    public const int SellPercent = 50;
    public const int MaxForgeTier = 3;

    // ------------------------------------------------------------------ shop

    public static IEnumerable<string> ShopStock(ContentDatabase db, GameSession s) =>
        EquipmentService.PurchasableItems(db, s.Chapter);

    public static string? Buy(ContentDatabase db, GameSession s, string itemId)
    {
        if (!ShopStock(db, s).Contains(itemId))
            return "Not sold here.";
        int price = db.ItemPrice(itemId);
        if (s.Gold < price)
            return "Not enough gold.";
        if (s.Convoy.Count >= GameSession.ConvoyCapacity)
            return "The convoy is full.";
        s.Gold -= price;
        s.Convoy.Add(itemId);
        return null;
    }

    /// <summary>
    /// Buys an item for <paramref name="unit"/>: weapons and items go straight into its pack when there is room (and
    /// a weapon is equipped if it holds none), anything else, or a full pack, goes to the convoy.
    /// </summary>
    public static string? BuyFor(ContentDatabase db, GameSession s, UnitState unit, string itemId)
    {
        bool toPack = db.IsInventoryItem(itemId) && unit.Inventory.Count < UnitState.MaxInventory;
        if (!toPack)
            return Buy(db, s, itemId);
        if (!ShopStock(db, s).Contains(itemId))
            return "Not sold here.";
        int price = db.ItemPrice(itemId);
        if (s.Gold < price)
            return "Not enough gold.";
        s.Gold -= price;
        unit.Inventory.Add(itemId);
        if (unit.EquippedItemId is null && db.TryWeapon(itemId) is { } w && CombatCalculator.CanWield(db, unit, w))
            unit.Equip(itemId);
        return null;
    }

    public static int SellPrice(ContentDatabase db, string itemId) => db.ItemPrice(itemId) * SellPercent / 100;

    public static string? Sell(ContentDatabase db, GameSession s, string itemId)
    {
        if (IsPersonal(db, itemId))
            return "That can't be sold.";
        if (!s.Convoy.Remove(itemId))
            return "Not in the convoy.";
        s.Gold += SellPrice(db, itemId);
        return null;
    }

    private static bool IsPersonal(ContentDatabase db, string itemId) =>
        db.TryWeapon(itemId)?.PersonalTo is not null || db.TryArmor(itemId)?.PersonalTo is not null;

    // ------------------------------------------------------------------ convoy and equipment

    /// <summary>Moves an item from the convoy into a unit's inventory (weapons and consumables only).</summary>
    public static string? GiveToUnit(ContentDatabase db, GameSession s, UnitState unit, string itemId)
    {
        if (!db.IsInventoryItem(itemId))
            return "Wear armour from the Equip menu instead.";
        if (unit.Inventory.Count >= UnitState.MaxInventory)
            return $"{unit.Def.Name}'s inventory is full.";
        if (!s.Convoy.Remove(itemId))
            return "Not in the convoy.";
        unit.Inventory.Add(itemId);
        if (unit.EquippedItemId is null && db.TryWeapon(itemId) is { } w && CombatCalculator.CanWield(db, unit, w))
            unit.Equip(itemId);
        return null;
    }

    /// <summary>Moves the item in an inventory slot to the convoy.</summary>
    public static string? TakeFromUnit(ContentDatabase db, GameSession s, UnitState unit, int slot)
    {
        if (slot < 0 || slot >= unit.Inventory.Count)
            return "Empty slot.";
        if (s.Convoy.Count >= GameSession.ConvoyCapacity)
            return "The convoy is full.";
        string itemId = unit.Inventory[slot];
        string? equipped = unit.EquippedItemId;
        unit.Inventory.RemoveAt(slot);
        s.Convoy.Add(itemId);
        unit.EquippedIndex = -1;
        if (equipped is not null && equipped != itemId)
            unit.Equip(equipped);
        else if (CombatCalculator.UsableWeapons(db, unit).FirstOrDefault() is { } w)
            unit.Equip(w.Id);
        return null;
    }

    /// <summary>Wears body armour or an accessory from the convoy; whatever it replaces goes back.</summary>
    public static string? WearFromConvoy(ContentDatabase db, GameSession s, UnitState unit, string armorId)
    {
        if (!s.Convoy.Contains(armorId))
            return "Not in the convoy.";
        var (ok, replaced, error) = EquipmentService.EquipArmor(db, unit, armorId);
        if (!ok)
            return error;
        s.Convoy.Remove(armorId);
        if (replaced is not null)
            s.Convoy.Add(replaced);
        return null;
    }

    public static string? TakeOff(GameSession s, UnitState unit, ArmorSlot slot)
    {
        string? worn = slot == ArmorSlot.Body ? unit.ArmorId : unit.AccessoryId;
        if (worn is null)
            return "Nothing to take off.";
        if (slot == ArmorSlot.Body)
            unit.ArmorId = null;
        else
            unit.AccessoryId = null;
        s.Convoy.Add(worn);
        return null;
    }

    /// <summary>Uses a stat booster from the convoy on a unit.</summary>
    public static string? UseBooster(ContentDatabase db, GameSession s, UnitState unit, string itemId)
    {
        if (db.TryItem(itemId) is not { Kind: ItemKind.StatBoost } item)
            return "That isn't a stat booster.";
        if (!s.Convoy.Remove(itemId))
            return "Not in the convoy.";
        unit.Gains += default(StatBlock).With(item.Stat, item.Amount);
        return null;
    }

    // ------------------------------------------------------------------ smithy

    /// <summary>
    /// What the smithy can reforge (not tomes): each weapon becomes the next tier of the same type (the strongest common,
    /// non-personal weapon of that tier), for the price difference. Brann can work one tier beyond what the open
    /// shops sell, up to Silver.
    /// </summary>
    public static IEnumerable<ForgeOption> ForgeOptions(ContentDatabase db, GameSession s, IEnumerable<string> weaponIds)
    {
        int shopTier = ShopStock(db, s).Select(db.TryWeapon).OfType<WeaponDef>().Select(w => w.Tier).DefaultIfEmpty(1).Max();
        int maxTier = System.Math.Min(MaxForgeTier, shopTier + 1);
        foreach (var id in weaponIds.Distinct())
        {
            // Tomes aren't reforged: they grow with their caster (CombatCalculator.TomeLevelBonus).
            if (db.TryWeapon(id) is not { PersonalTo: null, Magic: false } weapon || weapon.Tier >= maxTier)
                continue;
            var next = db.Weapons.Values
                .Where(w => w.Type == weapon.Type && w.Tier == weapon.Tier + 1 && w.PersonalTo is null && w.Price > 0
                            && w.MinRange == weapon.MinRange && w.MaxRange == weapon.MaxRange)
                .OrderBy(w => w.Price).ThenBy(w => w.Id).FirstOrDefault();
            if (next is not null)
                yield return new ForgeOption(weapon.Id, next.Id, System.Math.Max(100, next.Price - weapon.Price));
        }
    }

    /// <summary>Reforges a weapon held by <paramref name="unit"/> (or in the convoy when unit is null).</summary>
    public static string? Forge(ContentDatabase db, GameSession s, UnitState? unit, ForgeOption option)
    {
        if (!s.Unlocks.Contains("smithy"))
            return "The smithy isn't open yet.";
        if (s.Gold < option.Cost)
            return "Not enough gold.";
        var list = unit?.Inventory ?? s.Convoy;
        int index = list.IndexOf(option.FromId);
        if (index < 0)
            return "That weapon isn't there.";
        s.Gold -= option.Cost;
        list[index] = option.ToId;
        return null;
    }

    // ------------------------------------------------------------------ promotion

    public static string? CannotPromote(ContentDatabase db, GameSession s, UnitState unit, string classId)
    {
        if (!s.Unlocks.Contains("promotion"))
            return "Promotion isn't available yet.";
        return PromotionService.CannotPromote(db, unit, classId, s.CrestTier);
    }

    public static string? Promote(ContentDatabase db, GameSession s, UnitState unit, string classId)
    {
        if (CannotPromote(db, s, unit, classId) is { } error)
            return error;
        PromotionService.Promote(unit, classId);
        if (EquipmentService.RemoveInvalidArmor(db, unit) is { } removed)
            s.Convoy.Add(removed);
        return null;
    }
}
