using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Units;

namespace NotFireEmblem.Core;

/// <summary>All static game content, looked up by Id. Built by ContentLoader at boot, or directly by tests.</summary>
public sealed class ContentDatabase
{
    public Dictionary<string, TerrainDef> Terrains { get; } = new();
    public Dictionary<string, ClassDef> Classes { get; } = new();
    public Dictionary<string, WeaponDef> Weapons { get; } = new();
    public Dictionary<string, UnitDef> Units { get; } = new();
    public Dictionary<string, MapDef> Maps { get; } = new();
    public Dictionary<string, ArmorDef> Armors { get; } = new();
    public Dictionary<string, ShopDef> Shops { get; } = new();

    public TerrainDef Terrain(string id) => Get(Terrains, id, "terrain");
    public ClassDef Class(string id) => Get(Classes, id, "class");
    public WeaponDef Weapon(string id) => Get(Weapons, id, "weapon");
    public UnitDef Unit(string id) => Get(Units, id, "unit");
    public MapDef Map(string id) => Get(Maps, id, "map");
    public ArmorDef Armor(string id) => Get(Armors, id, "armour");
    public ShopDef Shop(string id) => Get(Shops, id, "shop");

    public WeaponDef? TryWeapon(string? id) => id is not null && Weapons.TryGetValue(id, out var w) ? w : null;
    public ArmorDef? TryArmor(string? id) => id is not null && Armors.TryGetValue(id, out var a) ? a : null;

    /// <summary>Any purchasable or holdable item id: weapons and armour/accessories.</summary>
    public bool IsItem(string id) => Weapons.ContainsKey(id) || Armors.ContainsKey(id);

    public TerrainDef TerrainBySymbol(char symbol) =>
        Terrains.Values.FirstOrDefault(t => t.Symbol == symbol)
        ?? throw new KeyNotFoundException($"No terrain uses map symbol '{symbol}'.");

    public void Add(TerrainDef t) => Terrains[t.Id] = t;
    public void Add(ClassDef c) => Classes[c.Id] = c;
    public void Add(WeaponDef w) => Weapons[w.Id] = w;
    public void Add(UnitDef u) => Units[u.Id] = u;
    public void Add(MapDef m) => Maps[m.Id] = m;
    public void Add(ArmorDef a) => Armors[a.Id] = a;
    public void Add(ShopDef s) => Shops[s.Id] = s;

    /// <summary>Checks every cross-reference. Returns a list of problems (empty when the content is valid).</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        foreach (var group in Terrains.Values.GroupBy(t => t.Symbol).Where(g => g.Count() > 1))
            errors.Add($"Terrain symbol '{group.Key}' is used by: {string.Join(", ", group.Select(t => t.Id))}");
        foreach (var c in Classes.Values)
            foreach (var p in c.PromotesTo.Where(p => !Classes.ContainsKey(p)))
                errors.Add($"Class '{c.Id}' promotes to unknown class '{p}'.");
        foreach (var u in Units.Values)
        {
            if (!Classes.ContainsKey(u.ClassId))
                errors.Add($"Unit '{u.Id}' has unknown class '{u.ClassId}'.");
            foreach (var item in u.Inventory.Where(i => !Weapons.ContainsKey(i)))
                errors.Add($"Unit '{u.Id}' carries unknown item '{item}'.");
            if (u.Armor is not null && TryArmor(u.Armor) is not { Slot: ArmorSlot.Body })
                errors.Add($"Unit '{u.Id}' starts in unknown body armour '{u.Armor}'.");
            if (u.Accessory is not null && TryArmor(u.Accessory) is not { Slot: ArmorSlot.Accessory })
                errors.Add($"Unit '{u.Id}' starts with unknown accessory '{u.Accessory}'.");
        }
        foreach (var m in Maps.Values)
            errors.AddRange(m.Validate(this));
        foreach (var shop in Shops.Values)
            foreach (var item in shop.Items.Where(i => !IsItem(i)))
                errors.Add($"Shop '{shop.Id}' sells unknown item '{item}'.");
        foreach (var personal in Weapons.Values.Select(w => (w.Id, w.PersonalTo)).Concat(Armors.Values.Select(a => (a.Id, a.PersonalTo))))
            if (personal.PersonalTo is not null && !Units.ContainsKey(personal.PersonalTo))
                errors.Add($"Item '{personal.Id}' is personal to unknown unit '{personal.PersonalTo}'.");
        return errors;
    }

    private static T Get<T>(Dictionary<string, T> table, string id, string kind) =>
        table.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Unknown {kind} id '{id}'.");
}
