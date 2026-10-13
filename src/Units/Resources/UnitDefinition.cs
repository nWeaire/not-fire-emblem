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

    public UnitDef ToDef() => new(
        Id, Name, ClassId, Level, StatsDefinition.ToBlock(Bases), StatsDefinition.ToBlock(Growths), Inventory.ToArray(), IsLord,
        string.IsNullOrEmpty(Armor) ? null : Armor, string.IsNullOrEmpty(Accessory) ? null : Accessory);
}
