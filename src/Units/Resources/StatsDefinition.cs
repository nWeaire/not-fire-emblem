using Godot;

namespace NotFireEmblem.Units;

/// <summary>Inspector-editable stat block, embedded as a sub-resource in class and unit definitions.</summary>
[GlobalClass]
public partial class StatsDefinition : Resource
{
    [Export] public int HP { get; set; }
    [Export] public int Str { get; set; }
    [Export] public int Mag { get; set; }
    [Export] public int Dex { get; set; }
    [Export] public int Spd { get; set; }
    [Export] public int Lck { get; set; }
    [Export] public int Def { get; set; }
    [Export] public int Res { get; set; }
    [Export] public int Mov { get; set; }
    [Export] public int Cha { get; set; }

    public StatBlock ToBlock() => new(HP, Str, Mag, Dex, Spd, Lck, Def, Res, Mov, Cha);

    public static StatBlock ToBlock(StatsDefinition? stats) => stats?.ToBlock() ?? default;
}
