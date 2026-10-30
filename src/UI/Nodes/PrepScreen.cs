using System.Collections.Generic;
using System.Linq;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Battle;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.UI;

/// <summary>Pre-battle deployment (IMPLEMENTATION §5.7 PrepScreen): pick who to bring, up to the map's limit.</summary>
public partial class PrepScreen : Control
{
    private readonly List<string> _chosen = new();
    private VBoxContainer _list = null!;
    private Label _count = null!;
    private Button _start = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AudioDirector.PlayMusic("prep");
        AddChild(UiKit.Backdrop(new Color(0.1f, 0.13f, 0.2f), new Color(0.05f, 0.06f, 0.1f)));
        var node = GameSessionNode.Instance;
        var runner = node.Runner!;
        var step = node.PendingBattleStep!;
        var map = node.Content.Map(step.MapId);
        _chosen.AddRange(runner.DefaultDeployment(step));

        var margin = new MarginContainer();
        AddChild(UiKit.Fill(margin));
        foreach (var side in new[] { "left", "right", "top", "bottom" })
            margin.AddThemeConstantOverride($"margin_{side}", 40);
        var column = new VBoxContainer();
        column.AddThemeConstantOverride("separation", 10);
        margin.AddChild(column);
        column.AddChild(UiKit.Title(map.Name));
        column.AddChild(UiKit.Label(GoalText(map), 16));
        _count = UiKit.Label("", 16, UiKit.Accent);
        column.AddChild(_count);

        var panel = UiKit.Panel();
        panel.SizeFlagsVertical = SizeFlags.ExpandFill;
        column.AddChild(panel);
        var scroll = new ScrollContainer();
        panel.AddChild(scroll);
        _list = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        scroll.AddChild(_list);

        var buttons = new HBoxContainer();
        column.AddChild(buttons);
        _start = UiKit.Button("Start Battle", () =>
        {
            node.PendingBattle = runner.Launch(step, _chosen);
            node.GoTo(GameSessionNode.BattleScene);
        });
        buttons.AddChild(_start);
        // A side map is optional: the player can still turn back to the hub from here.
        if (node.PendingSide is not null)
            buttons.AddChild(UiKit.Button("Back to Camp", CampaignFlow.LeaveSideMap));
        Refresh();
        _start.CallDeferred(Control.MethodName.GrabFocus);
        if (DevTools.AutoAdvance)
            GetTree().CreateTimer(0.3).Timeout += () => _start.EmitSignal(BaseButton.SignalName.Pressed);
    }

    private void Refresh()
    {
        var node = GameSessionNode.Instance;
        var runner = node.Runner!;
        var step = node.PendingBattleStep!;
        var fixedUnits = runner.FixedUnits(step);
        int limit = runner.DeployLimit(step);
        // Ticking a box rebuilds the list and frees the focused box: put the keyboard back on the same row after.
        int focus = UiKit.FocusIndex(_list);
        UiKit.ClearChildren(_list);
        foreach (var unit in runner.Session.ActiveRoster)
        {
            string id = unit.Def.Id;
            bool isFixed = fixedUnits.Contains(id);
            var stats = StatCalculator.Effective(node.Content, unit);
            var check = new CheckBox
            {
                Text = $"{unit.Def.Name}  —  {node.Content.Class(unit.ClassId).Name} Lv {unit.Level}   HP {stats.HP}  Str {stats.Str}  Mag {stats.Mag}  Spd {stats.Spd}  Def {stats.Def}"
                       + (isFixed ? "   (required)" : ""),
                ButtonPressed = _chosen.Contains(id),
                Disabled = isFixed || (!_chosen.Contains(id) && _chosen.Count >= limit),
            };
            check.Toggled += on =>
            {
                if (on && !_chosen.Contains(id))
                    _chosen.Add(id);
                else if (!on)
                    _chosen.Remove(id);
                Refresh();
            };
            _list.AddChild(check);
        }
        _count.Text = $"Deploy {_chosen.Count} / {limit}";
        if (focus >= 0)
            UiKit.RestoreFocus(_list, focus);
    }

    public static string GoalText(MapDef map)
    {
        var g = map.Goals;
        string win = g.Victory switch
        {
            VictoryKind.Rout => "Rout the enemy",
            VictoryKind.DefeatBoss => "Defeat the boss",
            VictoryKind.Seize => "Seize the objective with your lord",
            VictoryKind.Survive => $"Survive {g.Turns} turns",
            VictoryKind.Escape when g.EscapeAll => "Escape!",
            VictoryKind.Escape => "Escape through the exit",
            _ => "",
        };
        return $"Goal: {win}" + (map.Fog ? "   ·   Fog of war" : "");
    }
}
