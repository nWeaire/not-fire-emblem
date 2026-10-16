using System;
using System.Linq;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>
/// Title screen (IMPLEMENTATION §5.7): New Game (mode and difficulty), Continue, Load, Battle Select (single maps,
/// outside the campaign) and Quit. Built in code with placeholder styling.
/// </summary>
public partial class TitleScreen : Control
{
    private static readonly (string Id, string Label)[] Battles =
    {
        ("test_ford", "Skirmish: Brigands at the Ford"),
        ("prologue", "Prologue: Embers at Ashford Farm"),
        ("ch1", "Chapter 1: Brookhollow Burns"),
        ("ch2", "Chapter 2: The Millbrook Road"),
        ("ch3", "Chapter 3: Thornwood Hunt"),
    };

    private VBoxContainer _menu = null!;
    private GameMode _mode = GameMode.Classic;
    private Difficulty _difficulty = Difficulty.Normal;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        AudioDirector.PlayMusic("title");
        AddChild(UiKit.Backdrop(new Color(0.07f, 0.09f, 0.18f), new Color(0.2f, 0.13f, 0.2f)));

        var center = new CenterContainer();
        AddChild(UiKit.Fill(center));
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 14);
        center.AddChild(column);
        column.AddChild(UiKit.Label("THE RADIANT CREST", 56, UiKit.Accent, HorizontalAlignment.Center));
        column.AddChild(UiKit.Label("A tale of crests and the Thornvale", 18, UiKit.Muted, HorizontalAlignment.Center));
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });
        var panel = UiKit.Panel();
        column.AddChild(panel);
        _menu = new VBoxContainer();
        _menu.AddThemeConstantOverride("separation", 8);
        panel.AddChild(_menu);
        ShowMain();

        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--newgame")
                CallDeferred(MethodName.StartNew);
            else if (arg.StartsWith("--load=", StringComparison.Ordinal) && int.TryParse(arg["--load=".Length..], out int slot))
                Callable.From(() => CampaignFlow.Continue(slot)).CallDeferred();
        }
    }

    private void StartNew() => CampaignFlow.NewGame(_mode, _difficulty);

    private void Clear()
    {
        foreach (var child in _menu.GetChildren())
            child.QueueFree();
    }

    private void Focus()
    {
        if (_menu.GetChildren().OfType<Button>().FirstOrDefault(b => !b.Disabled) is { } first)
            first.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void ShowMain()
    {
        Clear();
        _menu.AddChild(UiKit.Button("New Game", ShowNewGame, 280));
        var latest = SaveService.List().Where(s => s.Label is not null).Select(s => s.Slot).ToList();
        _menu.AddChild(UiKit.Button("Continue", () => CampaignFlow.Continue(latest[0]), 280, latest.Count > 0));
        _menu.AddChild(UiKit.Button("Load", ShowLoad, 280, latest.Count > 0));
        _menu.AddChild(UiKit.Button("Battle Select", ShowBattles, 280));
        _menu.AddChild(UiKit.Button("Quit", () => GetTree().Quit(), 280));
        Focus();
    }

    private void ShowNewGame()
    {
        Clear();
        _menu.AddChild(UiKit.Title("New Game", 24));
        var mode = new OptionButton();
        mode.AddItem("Classic: fallen units are lost");
        mode.AddItem("Casual: fallen units return after battle");
        mode.Selected = (int)_mode;
        mode.ItemSelected += i => _mode = (GameMode)(int)i;
        _menu.AddChild(UiKit.Label("Mode", 14, UiKit.Muted));
        _menu.AddChild(mode);
        var difficulty = new OptionButton();
        difficulty.AddItem("Normal");
        difficulty.AddItem("Hard: tougher enemies, more of them");
        difficulty.Selected = (int)_difficulty;
        difficulty.ItemSelected += i => _difficulty = (Difficulty)(int)i;
        _menu.AddChild(UiKit.Label("Difficulty", 14, UiKit.Muted));
        _menu.AddChild(difficulty);
        _menu.AddChild(UiKit.Button("Begin", StartNew, 280));
        _menu.AddChild(UiKit.Button("Back", ShowMain, 280));
        Focus();
    }

    private void ShowLoad()
    {
        Clear();
        _menu.AddChild(UiKit.Title("Load", 24));
        foreach (var (slot, label) in SaveService.List())
            _menu.AddChild(UiKit.Button($"{SaveService.SlotName(slot)}: {label ?? "(empty)"}", () => CampaignFlow.Continue(slot), 420, label is not null));
        _menu.AddChild(UiKit.Button("Back", ShowMain, 420));
        Focus();
    }

    private void ShowBattles()
    {
        Clear();
        _menu.AddChild(UiKit.Title("Battle Select", 24));
        _menu.AddChild(UiKit.Label("Single maps outside the campaign: units start fresh.", 14, UiKit.Muted));
        foreach (var (id, label) in Battles.Where(b => GameSessionNode.Instance.Content.Maps.ContainsKey(b.Id)))
        {
            _menu.AddChild(UiKit.Button(label, () =>
            {
                GameSessionNode.Instance.EndSession();
                GameSessionNode.Instance.SkirmishMapId = id;
                GameSessionNode.Instance.GoTo(GameSessionNode.BattleScene);
            }, 420));
        }
        _menu.AddChild(UiKit.Button("Back", ShowMain, 420));
        Focus();
    }
}
