using Godot;

namespace NotFireEmblem.Units;

/// <summary>A consumable (vulnerary, stat booster).</summary>
[GlobalClass]
public partial class ItemDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public ItemKind Kind { get; set; } = ItemKind.Heal;
    /// <summary>HP healed (0 = full) or stat points gained.</summary>
    [Export] public int Amount { get; set; }
    /// <summary>The stat a booster raises.</summary>
    [Export] public Stat Stat { get; set; } = Stat.HP;
    [Export] public int Price { get; set; }

    public ItemDef ToDef() => new(Id, Name, Kind, Amount, Stat, Price);
}
