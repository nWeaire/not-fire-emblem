using Godot;

namespace NotFireEmblem.Units;

/// <summary>Body armour or an accessory. Stats only: equipment never changes a unit's look.</summary>
[GlobalClass]
public partial class ArmorDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public ArmorSlot Slot { get; set; } = ArmorSlot.Body;
    /// <summary>Body armour weight class (Light/Medium/Heavy); ignored for accessories.</summary>
    [Export] public ArmorClass Class { get; set; } = ArmorClass.Light;
    /// <summary>Flat stat bonuses while equipped.</summary>
    [Export] public StatsDefinition? Bonuses { get; set; }
    /// <summary>Adds to weapon weight for attack speed (offset by Str/5).</summary>
    [Export] public int Weight { get; set; }
    [Export] public int Price { get; set; }
    /// <summary>Gear tier, 1 (basic) to 4 (legendary). Used to stage shops by region.</summary>
    [Export] public int Tier { get; set; } = 1;
    /// <summary>Unit id for personal (unique) gear; empty = anyone allowed by class.</summary>
    [Export] public string PersonalTo { get; set; } = "";

    public ArmorDef ToDef() => new(
        Id, Name, Slot, Class, StatsDefinition.ToBlock(Bonuses), Weight, Price, Tier,
        string.IsNullOrEmpty(PersonalTo) ? null : PersonalTo);
}
