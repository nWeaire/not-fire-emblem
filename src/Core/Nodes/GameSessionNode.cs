using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Campaign;

namespace NotFireEmblem.Core;

/// <summary>
/// Autoload holding cross-scene state: the content database, the campaign session and runner, and the payloads
/// passed between screens (the next battle, scene or report). Also changes scenes with a fade (the SceneRouter role).
/// </summary>
public partial class GameSessionNode : Node
{
    public const string TitleScene = "res://scenes/title/Title.tscn";
    public const string StoryScene = "res://scenes/story/Story.tscn";
    public const string PrepScene = "res://scenes/prep/Prep.tscn";
    public const string BattleScene = "res://scenes/battle/Battle.tscn";
    public const string ResultsScene = "res://scenes/results/Results.tscn";
    public const string HubScene = "res://scenes/hub/Hub.tscn";

    private static GameSessionNode? _instance;

    public static GameSessionNode Instance => _instance ?? throw new System.InvalidOperationException("GameSessionNode autoload is missing.");

    public ContentDatabase Content { get; private set; } = new();

    /// <summary>The campaign in progress (null outside a campaign, e.g. a skirmish).</summary>
    public GameSession? Session { get; private set; }
    public CampaignRunner? Runner { get; private set; }

    /// <summary>The battle the Battle scene should start (campaign), with its step.</summary>
    public BattleLaunch? PendingBattle { get; set; }
    public BattleStep? PendingBattleStep { get; set; }
    /// <summary>The side map being played from the hub (its scenes, prep, battle and results), or null.</summary>
    public SideStep? PendingSide { get; set; }
    /// <summary>A resumed suspend: the commands the Battle scene replays before handing over control.</summary>
    public System.Collections.Generic.IReadOnlyList<Battle.IBattleCommand>? PendingReplay { get; set; }
    /// <summary>A map to fight outside the campaign (title-screen skirmish / chapter select).</summary>
    public string? SkirmishMapId { get; set; }
    public BattleReport? LastReport { get; set; }

    /// <summary>The scene the Story screen should play, and what happens afterwards.</summary>
    public string? PendingSceneId { get; set; }
    public StoryReturn PendingSceneReturn { get; set; }
    public (string A, string B, int Rank)? PendingSupport { get; set; }
    public string? EndText { get; set; }

    private ColorRect _fade = null!;

    public override void _EnterTree()
    {
        _instance = this;
        Content = ContentLoader.Load();
    }

    public override void _Ready()
    {
        var layer = new CanvasLayer { Layer = 100 };
        AddChild(layer);
        _fade = new ColorRect { Color = new Color(0, 0, 0, 0), MouseFilter = Control.MouseFilterEnum.Ignore };
        layer.AddChild(_fade);
        _fade.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
    }

    public override void _ExitTree()
    {
        if (_instance == this)
            _instance = null;
    }

    public void StartSession(GameSession session)
    {
        Session = session;
        Runner = new CampaignRunner(Content, session);
    }

    public void EndSession()
    {
        Session = null;
        Runner = null;
        PendingBattle = null;
        PendingBattleStep = null;
        PendingSide = null;
        PendingReplay = null;
    }

    /// <summary>Fades out, changes scene, fades back in.</summary>
    public async void GoTo(string scenePath)
    {
        var tween = CreateTween();
        tween.TweenProperty(_fade, "color:a", 1f, 0.2f);
        await ToSignal(tween, Tween.SignalName.Finished);
        GetTree().ChangeSceneToFile(scenePath);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        var back = CreateTween();
        back.TweenProperty(_fade, "color:a", 0f, 0.25f);
    }
}

/// <summary>Where the Story screen goes when its scene ends.</summary>
public enum StoryReturn { CampaignStep, Hub, Briefing, Title, SideBattle }
