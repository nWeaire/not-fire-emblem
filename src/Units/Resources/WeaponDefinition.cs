using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Units;

[GlobalClass]
public partial class WeaponDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public WeaponType Type { get; set; } = WeaponType.Sword;
    [Export] public int Might { get; set; }
    [Export] public int Hit { get; set; }
    [Export] public int Crit { get; set; }
    [Export] public int Weight { get; set; }
    [Export] public int MinRange { get; set; } = 1;
    [Export] public int MaxRange { get; set; } = 1;
    [Export] public bool Magic { get; set; }
    /// <summary>MovementType names this weapon deals effective (×3 Might) damage against.</summary>
    [Export] public string[] EffectiveAgainst { get; set; } = Array.Empty<string>();
    [Export] public int Price { get; set; }

    public WeaponDef ToDef() =>
        new(Id, Name, Type, Might, Hit, Crit, Weight, MinRange, MaxRange, Magic, EffectiveAgainst.ToArray(), Price);
}
