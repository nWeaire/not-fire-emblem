using System;
using System.Collections.Generic;
using Godot;
using NotFireEmblem.Battle;
using NotFireEmblem.Units;

namespace NotFireEmblem.Core;

/// <summary>Scans res://data/ and converts every definition Resource into the pure ContentDatabase.</summary>
public static class ContentLoader
{
    public const string DataRoot = "res://data";

    public static ContentDatabase Load()
    {
        var db = new ContentDatabase();
        foreach (var r in LoadAll<TerrainDefinition>("terrain")) db.Add(r.ToDef());
        foreach (var r in LoadAll<ClassDefinition>("classes")) db.Add(r.ToDef());
        foreach (var r in LoadAll<WeaponDefinition>("weapons")) db.Add(r.ToDef());
        foreach (var r in LoadAll<ArmorDefinition>("armor")) db.Add(r.ToDef());
        foreach (var r in LoadAll<ShopDefinition>("shops")) db.Add(r.ToDef());
        foreach (var r in LoadAll<UnitDefinition>("units")) db.Add(r.ToDef());
        foreach (var r in LoadAll<MapDefinition>("maps")) db.Add(r.ToDef());

        var errors = db.Validate();
        if (errors.Count > 0)
            throw new InvalidOperationException("Content validation failed:\n  " + string.Join("\n  ", errors));
        GD.Print($"Content loaded: {db.Terrains.Count} terrain, {db.Classes.Count} classes, {db.Weapons.Count} weapons, {db.Armors.Count} armour, {db.Shops.Count} shops, {db.Units.Count} units, {db.Maps.Count} maps.");
        return db;
    }

    private static IEnumerable<T> LoadAll<T>(string folder) where T : Resource
    {
        string dir = $"{DataRoot}/{folder}";
        if (!DirAccess.DirExistsAbsolute(dir))
            yield break;
        foreach (string file in DirAccess.GetFilesAt(dir))
        {
            // Exported builds list "X.tres.remap" instead of "X.tres".
            string name = file.EndsWith(".remap", StringComparison.Ordinal) ? file[..^".remap".Length] : file;
            if (!name.EndsWith(".tres", StringComparison.Ordinal))
                continue;
            string path = $"{dir}/{name}";
            var resource = ResourceLoader.Load(path);
            if (resource is not T typed)
                throw new InvalidOperationException($"{path} is not a {typeof(T).Name} (got {resource?.GetType().Name ?? "null"}).");
            yield return typed;
        }
    }
}
