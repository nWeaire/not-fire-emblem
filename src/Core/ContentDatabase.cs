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

    public TerrainDef Terrain(string id) => Get(Terrains, id, "terrain");
    public ClassDef Class(string id) => Get(Classes, id, "class");
    public WeaponDef Weapon(string id) => Get(Weapons, id, "weapon");
    public UnitDef Unit(string id) => Get(Units, id, "unit");
    public MapDef Map(string id) => Get(Maps, id, "map");

    public WeaponDef? TryWeapon(string? id) => id is not null && Weapons.TryGetValue(id, out var w) ? w : null;

    public TerrainDef TerrainBySymbol(char symbol) =>
        Terrains.Values.FirstOrDefault(t => t.Symbol == symbol)
        ?? throw new KeyNotFoundException($"No terrain uses map symbol '{symbol}'.");

    public void Add(TerrainDef t) => Terrains[t.Id] = t;
    public void Add(ClassDef c) => Classes[c.Id] = c;
    public void Add(WeaponDef w) => Weapons[w.Id] = w;
    public void Add(UnitDef u) => Units[u.Id] = u;
    public void Add(MapDef m) => Maps[m.Id] = m;

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
        }
        foreach (var m in Maps.Values)
            errors.AddRange(m.Validate(this));
        return errors;
    }

    private static T Get<T>(Dictionary<string, T> table, string id, string kind) =>
        table.TryGetValue(id, out var value) ? value : throw new KeyNotFoundException($"Unknown {kind} id '{id}'.");
}
