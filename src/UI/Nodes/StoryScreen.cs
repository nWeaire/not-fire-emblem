using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>
/// Plays a full-screen story scene: placeholder backdrop (named after the scene's @bg id until background art
/// exists), the chapter title, and the dialogue box. Also shows the end card.
/// </summary>
public partial class StoryScreen : Control
{
    private TextureRect _backdrop = null!;
    private Label _bgName = null!;
    private Label _chapter = null!;
    private DialogueBox _dialogue = null!;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _backdrop = UiKit.Backdrop(new Color(0.1f, 0.1f, 0.15f), new Color(0.05f, 0.05f, 0.08f));
        AddChild(_backdrop);
        _bgName = UiKit.Label("", 14, UiKit.Muted);
        AddChild(_bgName);
        _bgName.Position = new Vector2(20, 16);
        _chapter = UiKit.Label("", 22, UiKit.Accent, HorizontalAlignment.Right);
        AddChild(_chapter);
        _chapter.SetAnchorsAndOffsetsPreset(LayoutPreset.TopRight);
        _chapter.GrowHorizontal = GrowDirection.Begin;
        _chapter.Position += new Vector2(-20, 16);
        _dialogue = new DialogueBox { OnBackground = SetBackground };
        AddChild(_dialogue);

        var node = GameSessionNode.Instance;
        _chapter.Text = node.Session?.ChapterTitle ?? "";
        _ = Run(node);
    }

    private async Task Run(GameSessionNode node)
    {
        await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
        if (node.PendingSceneId is { } id && node.Content.Scenes.TryGetValue(id, out var scene))
        {
            SetBackground(id);
            await _dialogue.Play(scene, node.Content);
            CampaignFlow.SceneFinished();
            return;
        }
        ShowEnd(node.EndText ?? "The end.");
        if (DevTools.AutoAdvance)
        {
            GD.Print("CAMPAIGN COMPLETE");
            GetTree().Quit();
        }
    }

    /// <summary>Placeholder backgrounds: a gradient tinted from the background id.</summary>
    private void SetBackground(string id)
    {
        var tint = UiKit.HashColor(id, 0.35f, 0.35f);
        if (id.Contains("night") || id.Contains("smoke"))
            tint = tint.Darkened(0.5f);
        RemoveChild(_backdrop);
        _backdrop.QueueFree();
        _backdrop = UiKit.Backdrop(tint.Lightened(0.15f), tint.Darkened(0.6f));
        AddChild(_backdrop);
        MoveChild(_backdrop, 0);
        _bgName.Text = $"[background: {id}]";
    }

    private void ShowEnd(string text)
    {
        var center = new CenterContainer();
        AddChild(UiKit.Fill(center));
        var column = new VBoxContainer { Alignment = BoxContainer.AlignmentMode.Center };
        column.AddThemeConstantOverride("separation", 20);
        center.AddChild(column);
        column.AddChild(UiKit.Label(text, 30, UiKit.Accent, HorizontalAlignment.Center));
        column.AddChild(UiKit.Label("Your progress was autosaved.", 16, UiKit.Muted, HorizontalAlignment.Center));
        var button = UiKit.Button("Back to Title", CampaignFlow.SceneFinished, 240);
        button.SizeFlagsHorizontal = SizeFlags.ShrinkCenter;
        column.AddChild(button);
        button.CallDeferred(Control.MethodName.GrabFocus);
    }
}
