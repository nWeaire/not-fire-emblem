using System;
using System.Linq;
using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>
/// Title screen (IMPLEMENTATION §5.7): Resume (a suspended battle), New Game (mode and difficulty), Continue, Load, Battle Select (single maps,
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
        ("ch4", "Chapter 4: The Scholar of Calder"),
        ("ch5", "Chapter 5: Ferrow's Keep"),
        ("ch6", "Chapter 6: The Bandit King of Greyfen"),
    };

    private const string VistaPath = "res://assets/backgrounds/title_vista.png";

    /// <summary>The crest intro plays once per launch, not every time the game returns to the title.</summary>
    private static bool _introPlayed;

    private VBoxContainer _menu = null!;
    private Control _column = null!;
    private Control? _intro;
    private Tween? _introTween;
    private GameMode _mode = GameMode.Classic;
    private Difficulty _difficulty = Difficulty.Normal;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        if (ResourceLoader.Exists(VistaPath))
        {
            // The world, as the crest's light reveals it, with a light veil so the title reads over the bright sky.
            AddChild(UiKit.Fill(new TextureRect
            {
                Texture = ResourceLoader.Load<Texture2D>(VistaPath),
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
            }));
            AddChild(UiKit.Fill(new ColorRect { Color = new Color(0.03f, 0.03f, 0.08f, 0.3f), MouseFilter = MouseFilterEnum.Ignore }));
        }
        else
            AddChild(UiKit.Backdrop(new Color(0.07f, 0.09f, 0.18f), new Color(0.2f, 0.13f, 0.2f)));

        var center = new CenterContainer();
        AddChild(UiKit.Fill(center));
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 14);
        center.AddChild(column);
        _column = column;
        column.AddChild(UiKit.Outlined(UiKit.Label("The Radiant Crest", 56, UiKit.Accent, HorizontalAlignment.Center), 10));
        column.AddChild(new Control { CustomMinimumSize = new Vector2(0, 20) });
        var panel = UiKit.Panel();
        column.AddChild(panel);
        _menu = new VBoxContainer();
        _menu.AddThemeConstantOverride("separation", 8);
        panel.AddChild(_menu);
        ShowMain();

        bool scripted = false;
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            if (arg == "--newgame")
            {
                scripted = true;
                CallDeferred(MethodName.StartNew);
            }
            else if (arg == "--resume")
            {
                scripted = true;
                Callable.From(() => CampaignFlow.Resume()).CallDeferred();
            }
            else if (arg.StartsWith("--load=", StringComparison.Ordinal) && int.TryParse(arg["--load=".Length..], out int slot))
            {
                scripted = true;
                Callable.From(() => CampaignFlow.Continue(slot)).CallDeferred();
            }
        }
        if (!_introPlayed && !scripted && !DevTools.AutoAdvance && ResourceLoader.Exists(VistaPath))
            PlayIntro();
        else
            AudioDirector.PlayMusic("title");
        _introPlayed = true;
    }

    /// <summary>
    /// The launch intro: in the dark, Rowan's crest at its highest tier rises and brightens until its light fills the
    /// screen, the white fades to the vista of the world, and the title and menu come up. Any key or click skips it.
    /// </summary>
    private void PlayIntro()
    {
        _column.Visible = false;
        var intro = UiKit.Fill(new Control { MouseFilter = MouseFilterEnum.Ignore });
        AddChild(intro);
        _intro = intro;
        var dark = UiKit.Fill(new ColorRect { Color = Colors.Black, MouseFilter = MouseFilterEnum.Ignore });
        intro.AddChild(dark);
        var holder = UiKit.Fill(new CenterContainer { MouseFilter = MouseFilterEnum.Ignore });
        intro.AddChild(holder);
        const float size = 300f;
        var crest = new CrestEmblem("rowan", 4, radiant: true, size)
        {
            Modulate = Colors.White with { A = 0f },
            PivotOffset = new Vector2(size / 2f, size / 2f),
            Scale = Vector2.One * 0.75f,
        };
        holder.AddChild(crest);
        var flash = UiKit.Fill(new ColorRect { Color = Colors.White with { A = 0f }, MouseFilter = MouseFilterEnum.Ignore });
        intro.AddChild(flash);

        var t = _introTween = CreateTween();
        t.TweenInterval(0.5f);
        // The crest rises out of the dark.
        t.TweenCallback(Callable.From(() => AudioDirector.PlaySfx("crest_rise")));
        t.TweenProperty(crest, "modulate:a", 1f, 1.4f).SetTrans(Tween.TransitionType.Sine);
        t.Parallel().TweenProperty(crest, "scale", Vector2.One, 2.2f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        t.TweenInterval(0.5f);
        // It flares until its light fills the screen.
        t.TweenCallback(Callable.From(() => AudioDirector.PlaySfx("crest_burn")));
        t.TweenProperty(crest, "scale", Vector2.One * 1.6f, 0.8f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
        t.Parallel().TweenProperty(flash, "color:a", 1f, 0.8f).SetTrans(Tween.TransitionType.Expo).SetEase(Tween.EaseType.In);
        // Behind the light: the world.
        t.TweenCallback(Callable.From(() =>
        {
            dark.Visible = false;
            crest.Visible = false;
            AudioDirector.PlayMusic("title");
        }));
        t.TweenInterval(0.25f);
        t.TweenProperty(flash, "color:a", 0f, 1.6f).SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.Out);
        t.TweenCallback(Callable.From(ShowTitle));
    }

    /// <summary>Ends the intro (finished or skipped): the title and menu fade in and take focus.</summary>
    private void ShowTitle()
    {
        if (_intro is null)
            return;
        _introTween?.Kill();
        _intro.QueueFree();
        _intro = null;
        AudioDirector.PlayMusic("title");
        _column.Visible = true;
        _column.Modulate = Colors.White with { A = 0f };
        CreateTween().TweenProperty(_column, "modulate:a", 1f, 0.6f);
        Focus();
    }

    public override void _Input(InputEvent e)
    {
        // Skip the intro on any key, button or click; the press must not also reach the menu.
        if (_intro is null || !e.IsPressed() || e.IsEcho())
            return;
        if (e is InputEventKey or InputEventMouseButton or InputEventJoypadButton)
        {
            GetViewport().SetInputAsHandled();
            ShowTitle();
        }
    }

    // Autoplay soak runs play Casual: in Classic one early death snowballed into Rowan alone by Ch 3, stalemated to turn 60.
    private void StartNew() => CampaignFlow.NewGame(DevTools.Autoplay ? GameMode.Casual : _mode, _difficulty);

    private void Clear()
    {
        // Out now, not at the end of the frame: Focus() picks the menu's first button, and a button only queued for
        // deletion would take the focus with it, leaving the keyboard dead.
        UiKit.ClearChildren(_menu);
    }

    private void Focus()
    {
        if (_menu.GetChildren().OfType<Button>().FirstOrDefault(b => !b.Disabled) is { } first)
            first.CallDeferred(Control.MethodName.GrabFocus);
    }

    private void ShowMain()
    {
        Clear();
        if (SaveService.ReadSuspend() is { } suspend)
            _menu.AddChild(UiKit.Button($"Resume: {suspend.Label}", () => CampaignFlow.Resume(), 280));
        _menu.AddChild(UiKit.Button("New Game", ShowNewGame, 280));
        var latest = SaveService.List().Where(s => s.Label is not null).Select(s => s.Slot).ToList();
        _menu.AddChild(UiKit.Button("Continue", () => CampaignFlow.Continue(latest[0]), 280, latest.Count > 0));
        _menu.AddChild(UiKit.Button("Load", ShowLoad, 280, latest.Count > 0));
        _menu.AddChild(UiKit.Button("Battle Select", ShowBattles, 280));
        _menu.AddChild(UiKit.Button("Settings", ShowSettings, 280));
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

    private void ShowSettings()
    {
        Clear();
        _menu.AddChild(UiKit.Title("Settings", 24));
        _menu.AddChild(new SettingsPanel());
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
