using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Battle;
using NotFireEmblem.Campaign;
using NotFireEmblem.Dialogue;
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
    public Dictionary<string, ItemDef> Items { get; } = new();
    public Dictionary<string, ArtDef> Arts { get; } = new();
    public Dictionary<string, SpellDef> Spells { get; } = new();
    public Dictionary<string, DialogueScene> Scenes { get; } = new();
    /// <summary>The story, as an ordered list of steps (scenes, battles, hub visits...).</summary>
    public CampaignDef? Campaign { get; set; }

    public TerrainDef Terrain(string id) => Get(Terrains, id, "terrain");
    public ClassDef Class(string id) => Get(Classes, id, "class");
    public WeaponDef Weapon(string id) => Get(Weapons, id, "weapon");
    public UnitDef Unit(string id) => Get(Units, id, "unit");
    public MapDef Map(string id) => Get(Maps, id, "map");
    public ArmorDef Armor(string id) => Get(Armors, id, "armour");
    public ShopDef Shop(string id) => Get(Shops, id, "shop");
    public ItemDef Item(string id) => Get(Items, id, "item");
    public ArtDef Art(string id) => Get(Arts, id, "combat art");
    public SpellDef Spell(string id) => Get(Spells, id, "spell");
    public DialogueScene Scene(string id) => Get(Scenes, id, "dialogue scene");

    public WeaponDef? TryWeapon(string? id) => id is not null && Weapons.TryGetValue(id, out var w) ? w : null;
    public ArmorDef? TryArmor(string? id) => id is not null && Armors.TryGetValue(id, out var a) ? a : null;
    public ItemDef? TryItem(string? id) => id is not null && Items.TryGetValue(id, out var i) ? i : null;

    /// <summary>Any purchasable or holdable item id: weapons, consumables and armour/accessories.</summary>
    public bool IsItem(string id) => Weapons.ContainsKey(id) || Items.ContainsKey(id) || Armors.ContainsKey(id);

    /// <summary>Things that go in the 5-slot inventory (weapons and consumables, not worn gear).</summary>
    public bool IsInventoryItem(string id) => Weapons.ContainsKey(id) || Items.ContainsKey(id);

    /// <summary>Display name of any item id.</summary>
    public string ItemName(string id) =>
        TryWeapon(id)?.Name ?? TryItem(id)?.Name ?? TryArmor(id)?.Name ?? id;

    /// <summary>Shop price of any item id (0 = not sold).</summary>
    public int ItemPrice(string id) =>
        TryWeapon(id)?.Price ?? TryItem(id)?.Price ?? TryArmor(id)?.Price ?? 0;

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
    public void Add(ItemDef i) => Items[i.Id] = i;
    public void Add(ArtDef a) => Arts[a.Id] = a;
    public void Add(SpellDef s) => Spells[s.Id] = s;
    public void Add(DialogueScene d) => Scenes[d.Id] = d;

    /// <summary>Checks every cross-reference. Returns a list of problems (empty when the content is valid).</summary>
    public List<string> Validate()
    {
        var errors = new List<string>();
        foreach (var group in Terrains.Values.GroupBy(t => t.Symbol).Where(g => g.Count() > 1))
            errors.Add($"Terrain symbol '{group.Key}' is used by: {string.Join(", ", group.Select(t => t.Id))}");
        foreach (var c in Classes.Values)
        {
            foreach (var p in c.PromotesTo.Where(p => !Classes.ContainsKey(p)))
                errors.Add($"Class '{c.Id}' promotes to unknown class '{p}'.");
            if (c.PromotesFrom is { } from && !Classes.ContainsKey(from))
                errors.Add($"Class '{c.Id}' follows unknown class '{from}'.");
            foreach (var a in (c.Arts ?? Array.Empty<string>()).Where(a => !Arts.ContainsKey(a)))
                errors.Add($"Class '{c.Id}' teaches unknown art '{a}'.");
            foreach (var sp in (c.Spells ?? Array.Empty<string>()).Where(sp => !Spells.ContainsKey(sp)))
                errors.Add($"Class '{c.Id}' teaches unknown spell '{sp}'.");
        }
        foreach (var u in Units.Values)
        {
            if (!Classes.ContainsKey(u.ClassId))
                errors.Add($"Unit '{u.Id}' has unknown class '{u.ClassId}'.");
            foreach (var item in u.Inventory.Where(i => !IsInventoryItem(i)))
                errors.Add($"Unit '{u.Id}' carries unknown item '{item}'.");
            foreach (var a in (u.Arts ?? Array.Empty<string>()).Where(a => !Arts.ContainsKey(a)))
                errors.Add($"Unit '{u.Id}' knows unknown art '{a}'.");
            foreach (var sp in (u.Spells ?? Array.Empty<string>()).Where(sp => !Spells.ContainsKey(sp)))
                errors.Add($"Unit '{u.Id}' knows unknown spell '{sp}'.");
            if (u.Crestbound is { } cb && !(Classes.TryGetValue(cb, out var cbClass) && cbClass.Tier == 3))
                errors.Add($"Unit '{u.Id}' has Crestbound class '{cb}', which isn't a tier 3 class.");
            foreach (var sup in (u.Supports ?? Array.Empty<SupportDef>()).Where(sup => !Units.ContainsKey(sup.PartnerId)))
                errors.Add($"Unit '{u.Id}' has a support with unknown unit '{sup.PartnerId}'.");
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
        if (Campaign is not null)
            errors.AddRange(Campaign.Validate(this));
        return errors;
    }

    private static T Get<T>(Dictionary<string, T> table, string id, string kind) =>
        table.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Unknown {kind} id '{id}'.");
}
