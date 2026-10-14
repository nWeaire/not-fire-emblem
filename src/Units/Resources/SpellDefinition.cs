using Godot;

namespace NotFireEmblem.Units;

/// <summary>A healing spell (staff-style). Heals Power + Mag / 2. Offensive tomes are weapons.</summary>
[GlobalClass]
public partial class SpellDefinition : Resource
{
    [Export] public string Id { get; set; } = "";
    [Export] public string Name { get; set; } = "";
    [Export] public int Power { get; set; } = 10;
    [Export] public int MinRange { get; set; } = 1;
    [Export] public int MaxRange { get; set; } = 1;
    [Export] public int Uses { get; set; } = 4;

    public SpellDef ToDef() => new(Id, Name, Power, MinRange, MaxRange, Uses);
}
