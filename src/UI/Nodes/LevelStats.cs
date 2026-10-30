using System.Linq;
using Godot;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>
/// The level-up panels' stat column (map and battle screen, session 18): the stat's icon, its name and value, and the
/// gain in gold.
/// </summary>
public static class LevelStats
{
    private static readonly Color Gain = new(0.95f, 0.79f, 0.3f);

    public static void Fill(VBoxContainer box, StatBlock stats, LevelUp level, int size)
    {
        UiKit.ClearChildren(box);
        foreach (var stat in StatBlock.All.Where(st => st is not (Stat.Mov or Stat.Cha)))
        {
            var row = ItemIcons.StatLine(stat, $"{stat,-4}{stats[stat],3}", size);
            if (level.Gains[stat] > 0)
                row.AddChild(UiKit.Label($"+{level.Gains[stat]}", size, Gain));
            box.AddChild(row);
        }
    }
}
