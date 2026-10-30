using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>
/// Plays a full-screen story scene: the @bg background picture (assets/backgrounds/), the chapter title, and the
/// dialogue box. Also shows the end card.
/// </summary>
public partial class StoryScreen : Control
{
    private Control _backdrop = null!;
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
        if (node.PendingSceneId is { } id
            && (node.Session?.Scene(node.Content, id) ?? node.Content.Scenes.GetValueOrDefault(id)) is { } scene)
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

    /// <summary>
    /// Shows assets/backgrounds/&lt;id&gt;.png. Variants without their own picture reuse the location's picture with a
    /// time-of-day tint: "farm_night_smoke" falls back to "farm_night", then "farm", tinted for night with a smoke
    /// haze. Ids with no picture at all get a gradient tinted from the id.
    /// </summary>
    private void SetBackground(string id)
    {
        Control next;
        var (baseId, tint, haze) = ResolveBackground(id);
        if (baseId is not null && UiKit.Picture(baseId) is { } picture)
        {
            picture.Modulate = tint;
            next = picture;
            if (haze.A > 0f)
                picture.AddChild(UiKit.Fill(new ColorRect { Color = haze, MouseFilter = MouseFilterEnum.Ignore }));
            _bgName.Text = "";
        }
        else
        {
            var color = UiKit.HashColor(id, 0.35f, 0.35f);
            if (id.Contains("night") || id.Contains("smoke"))
                color = color.Darkened(0.5f);
            next = UiKit.Backdrop(color.Lightened(0.15f), color.Darkened(0.6f));
            _bgName.Text = $"[background: {id}]";
        }
        RemoveChild(_backdrop);
        _backdrop.QueueFree();
        _backdrop = next;
        AddChild(_backdrop);
        MoveChild(_backdrop, 0);
    }

    /// <summary>The picture to use for a background id, its tint and a haze colour laid over it.</summary>
    public static (string? BaseId, Color Tint, Color Haze) ResolveBackground(string id)
    {
        var tint = Colors.White;
        var haze = new Color(0, 0, 0, 0);
        foreach (var word in id.Split('_'))
        {
            switch (word)
            {
                case "evening": tint *= new Color(1f, 0.8f, 0.62f); break;
                case "dusk": tint *= new Color(0.92f, 0.7f, 0.66f); break;
                case "night": tint *= new Color(0.4f, 0.44f, 0.72f); break;
                case "snow": tint *= new Color(0.9f, 0.95f, 1.05f); haze = new Color(0.92f, 0.95f, 1f, 0.18f); break;
                case "dawn": tint *= new Color(1f, 0.86f, 0.8f); break;
                case "morning": tint *= new Color(1f, 0.98f, 0.9f); break;
                case "storm": tint *= new Color(0.55f, 0.6f, 0.72f); haze = new Color(0.2f, 0.24f, 0.32f, 0.25f); break;
                case "smoke": haze = new Color(0.35f, 0.2f, 0.18f, 0.35f); break;
                case "burning": tint *= new Color(1f, 0.62f, 0.45f); haze = new Color(0.8f, 0.25f, 0.05f, 0.18f); break;
            }
        }
        string candidate = id;
        while (true)
        {
            if (UiKit.Background(candidate) is not null)
                return (candidate, tint, haze);
            int cut = candidate.LastIndexOf('_');
            if (cut <= 0)
                return (null, tint, haze);
            candidate = candidate[..cut];
        }
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
