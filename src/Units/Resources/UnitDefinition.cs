using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

[GlobalClass]
public partial class UnitDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public string ClassId { get; set; } = "";
    [Export] public int Level { get; set; } = 1;
    /// <summary>Personal modifiers added on top of the class bases.</summary>
    [Export] public StatsDefinition? Bases { get; set; }
    /// <summary>Personal growths (%), added to the class growths on level up.</summary>
    [Export] public StatsDefinition? Growths { get; set; }
    /// <summary>Item ids.</summary>
    [Export] public string[] Inventory { get; set; } = Array.Empty<string>();
    [Export] public bool IsLord { get; set; }
    /// <summary>Starting body armour id (empty = none).</summary>
    [Export] public string Armor { get; set; } = "";
    /// <summary>Starting accessory id (empty = none).</summary>
    [Export] public string Accessory { get; set; } = "";
    /// <summary>Personal combat art ids.</summary>
    [Export] public string[] Arts { get; set; } = Array.Empty<string>();
    /// <summary>Personal spell ids.</summary>
    [Export] public string[] Spells { get; set; } = Array.Empty<string>();
    /// <summary>Support pairs as "partner:C,B,A" point thresholds, e.g. "kai:10,25,45".</summary>
    [Export] public string[] Supports { get; set; } = Array.Empty<string>();
    /// <summary>The unit's own tier-3 class, reached from its tier-2 class at crest tier 3 (empty = none).</summary>
    [Export] public string Crestbound { get; set; } = "";

    public UnitDef ToDef() => new(
        Id, Name, ClassId, Level, StatsDefinition.ToBlock(Bases), StatsDefinition.ToBlock(Growths), Inventory.ToArray(), IsLord,
        string.IsNullOrEmpty(Armor) ? null : Armor, string.IsNullOrEmpty(Accessory) ? null : Accessory,
        Arts.ToArray(), Spells.ToArray(), Supports.Select(ParseSupport).ToArray(),
        string.IsNullOrEmpty(Crestbound) ? null : Crestbound);

    private SupportDef ParseSupport(string text)
    {
        var parts = text.Split(':');
        var t = parts.Length == 2 ? parts[1].Split(',') : Array.Empty<string>();
        if (t.Length != 3)
            throw new FormatException($"Unit '{Id}' support '{text}' should look like 'partner:10,25,45'.");
        return new SupportDef(parts[0].Trim(), int.Parse(t[0]), int.Parse(t[1]), int.Parse(t[2]));
    }
}
