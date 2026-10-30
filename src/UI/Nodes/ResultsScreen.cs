using System.Linq;
using Godot;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>After a victory: EXP and levels, spoils, recruits, losses and newly unlocked supports.</summary>
public partial class ResultsScreen : Control
{
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AddChild(UiKit.Backdrop(new Color(0.16f, 0.14f, 0.08f), new Color(0.06f, 0.05f, 0.03f)));
        var node = GameSessionNode.Instance;
        var report = node.LastReport;

        var center = new CenterContainer();
        AddChild(UiKit.Fill(center));
        var panel = UiKit.Panel();
        center.AddChild(panel);
        var column = new VBoxContainer { CustomMinimumSize = new Vector2(620, 0) };
        column.AddThemeConstantOverride("separation", 8);
        panel.AddChild(column);
        column.AddChild(UiKit.Title("Victory!", 40));
        if (report is not null)
        {
            column.AddChild(UiKit.Label("Units", 18, UiKit.Accent));
            foreach (var (name, exp, level) in report.Units)
                column.AddChild(UiKit.Label($"   {name,-12}  Lv {level,2}   +{exp} EXP", 16));
            if (report.Gold > 0 || report.Items.Count > 0)
            {
                column.AddChild(UiKit.Label("Spoils", 18, UiKit.Accent));
                if (report.Gold > 0)
                    column.AddChild(UiKit.Label($"   {report.Gold} gold", 16));
                foreach (var item in report.Items)
                    column.AddChild(UiKit.Label($"   {node.Content.ItemName(item)} (to town)", 16));
            }
            if (report.Recruited.Count > 0)
                column.AddChild(UiKit.Label("Joined: " + string.Join(", ", report.Recruited), 16, UiKit.Good));
            if (report.Fallen.Count > 0)
            {
                bool classic = node.Session?.Mode == GameMode.IronMan;
                column.AddChild(UiKit.Label((classic ? "Lost: " : "Retreated (will return): ") + string.Join(", ", report.Fallen), 16,
                    classic ? UiKit.Bad : UiKit.Muted));
            }
            if (report.NewSupports.Count > 0)
                column.AddChild(UiKit.Label("New support conversations: " + string.Join(", ", report.NewSupports), 16, UiKit.Good));
        }
        var button = UiKit.Button("Continue", CampaignFlow.ResultsDone, 220);
        column.AddChild(button);
        button.CallDeferred(Control.MethodName.GrabFocus);
        if (DevTools.AutoAdvance)
            GetTree().CreateTimer(0.5).Timeout += CampaignFlow.ResultsDone;
    }
}
