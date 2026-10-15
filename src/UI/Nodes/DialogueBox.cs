using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;
using NotFireEmblem.Core;
using NotFireEmblem.Dialogue;
using NotFireEmblem.Grid;

namespace NotFireEmblem.UI;

/// <summary>
/// Plays a <see cref="DialogueScene"/> (GDD §5): speaker portrait, name and a typewriter line. Confirm (or a click)
/// finishes the line or advances; Cancel skips the rest of the scene. Used by story screens, battle triggers and
/// supports. Portraits come from assets/portraits/&lt;speaker&gt;_&lt;expression&gt;.png, then &lt;speaker&gt;.png, else a
/// coloured placeholder with the speaker's initials.
/// </summary>
public partial class DialogueBox : Control
{
    public const string PortraitRoot = "res://assets/portraits";
    private const float CharsPerSecond = 55f;

    private PanelContainer _box = null!;
    private Control _portraitFrame = null!;
    private TextureRect _portrait = null!;
    private ColorRect _placeholder = null!;
    private Label _placeholderText = null!;
    private Label _name = null!;
    private PanelContainer _namePlate = null!;
    private RichTextLabel _text = null!;
    private Label _more = null!;

    private TaskCompletionSource<bool>? _advance;
    private bool _skip;
    private Tween? _typing;

    /// <summary>Battle scenes pass this to move the camera on @focus lines.</summary>
    public Func<GridPos, Task>? OnFocus { get; set; }
    /// <summary>Story screens pass this to change the backdrop on @bg lines.</summary>
    public Action<string>? OnBackground { get; set; }

    public bool IsPlaying { get; private set; }

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        Visible = false;

        _box = UiKit.Panel();
        _box.MouseFilter = MouseFilterEnum.Stop;
        AddChild(_box);
        _box.AnchorLeft = 0.08f;
        _box.AnchorRight = 0.92f;
        _box.AnchorTop = 1f;
        _box.AnchorBottom = 1f;
        _box.OffsetTop = -200;
        _box.OffsetBottom = -24;
        _box.GuiInput += e =>
        {
            if (e is InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left })
                Advance();
        };

        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 18);
        _box.AddChild(row);

        _portraitFrame = new Control { CustomMinimumSize = new Vector2(150, 150) };
        row.AddChild(_portraitFrame);
        _placeholder = UiKit.Fill(new ColorRect());
        _portraitFrame.AddChild(_placeholder);
        _placeholderText = UiKit.Fill(UiKit.Label("", 48, null, HorizontalAlignment.Center));
        _placeholderText.VerticalAlignment = VerticalAlignment.Center;
        _portraitFrame.AddChild(_placeholderText);
        _portrait = UiKit.Fill(new TextureRect
        {
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = TextureFilterEnum.Nearest,
        });
        _portraitFrame.AddChild(_portrait);

        var column = new VBoxContainer { SizeFlagsHorizontal = SizeFlags.ExpandFill };
        row.AddChild(column);
        // The speaker's name sits on a Clean name plate overlapping the top edge of the box, GBA style.
        _name = UiKit.Label("", 24, UiKit.Text);
        _namePlate = UiKit.Plate(_name);
        AddChild(_namePlate);
        _namePlate.AnchorLeft = _namePlate.AnchorRight = 0.08f;
        _namePlate.AnchorTop = _namePlate.AnchorBottom = 1f;
        _namePlate.OffsetLeft = 186;
        _namePlate.OffsetTop = -226;
        _namePlate.CustomMinimumSize = new Vector2(220, 0);
        _text = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = false,
            ScrollActive = false,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        _text.AddThemeFontSizeOverride("normal_font_size", 20);
        _text.AddThemeFontSizeOverride("italics_font_size", 20);
        column.AddChild(_text);
        _more = UiKit.Label("▼  Z: next   X: skip", 13, UiKit.Muted, HorizontalAlignment.Right);
        column.AddChild(_more);
    }

    public override void _Input(InputEvent e)
    {
        if (!IsPlaying)
            return;
        if (e.IsActionPressed(InputSetup.Confirm))
        {
            Advance();
            GetViewport().SetInputAsHandled();
        }
        else if (e.IsActionPressed(InputSetup.Cancel))
        {
            _skip = true;
            _advance?.TrySetResult(true);
            GetViewport().SetInputAsHandled();
        }
    }

    private void Advance()
    {
        if (_typing is not null && _typing.IsRunning())
        {
            _typing.Kill();
            _text.VisibleRatio = 1f;
            return;
        }
        _advance?.TrySetResult(true);
    }

    /// <summary>Plays the whole scene. Returns when it ends or is skipped.</summary>
    public async Task Play(DialogueScene scene, ContentDatabase db)
    {
        if (DevTools.SkipDialogue)
            return;
        IsPlaying = true;
        _skip = false;
        // The box appears with the first spoken line, so camera moves before it aren't covered by an empty box.
        Visible = false;
        try
        {
            foreach (var line in scene.Lines)
            {
                if (_skip)
                    break;
                switch (line)
                {
                    case SpeakLine speak:
                        Show(speak, db);
                        _advance = new TaskCompletionSource<bool>();
                        await _advance.Task;
                        break;
                    case WaitLine wait:
                        await ToSignal(GetTree().CreateTimer(wait.Seconds), SceneTreeTimer.SignalName.Timeout);
                        break;
                    case FocusLine focus when OnFocus is not null:
                        await OnFocus(focus.Cell);
                        break;
                    case BackgroundLine bg:
                        OnBackground?.Invoke(bg.Id);
                        break;
                }
            }
        }
        finally
        {
            Visible = false;
            IsPlaying = false;
        }
    }

    private void Show(SpeakLine line, ContentDatabase db)
    {
        Visible = true;
        bool narrator = line.IsNarrator;
        _portraitFrame.Visible = !narrator && line.Speaker != "???";
        _name.Text = narrator ? "" : SpeakerName(line.Speaker, db);
        _namePlate.Visible = !narrator;
        if (_portraitFrame.Visible)
        {
            var texture = Portrait(line.Speaker, line.Expression);
            _portrait.Texture = texture;
            _portrait.Visible = texture is not null;
            _placeholder.Visible = texture is null;
            _placeholderText.Visible = texture is null;
            _placeholder.Color = UiKit.HashColor(line.Speaker);
            _placeholderText.Text = Initials(SpeakerName(line.Speaker, db));
        }
        string text = Emphasis(line.Text);
        _text.Text = narrator ? $"[i]{text}[/i]" : text;
        _text.VisibleRatio = 0f;
        _typing?.Kill();
        _typing = CreateTween();
        _typing.TweenProperty(_text, "visible_ratio", 1f, Math.Max(0.15f, line.Text.Length / CharsPerSecond));
    }

    public static string SpeakerName(string speaker, ContentDatabase db) =>
        speaker == "???" ? "???" : db.Units.TryGetValue(speaker, out var unit) ? unit.Name : UiKit.TitleCase(speaker);

    /// <summary>The best portrait for a speaker and expression, or null.</summary>
    public static Texture2D? Portrait(string speaker, string expression)
    {
        foreach (var path in new[] { $"{PortraitRoot}/{speaker}_{expression}.png", $"{PortraitRoot}/{speaker}.png" })
            if (ResourceLoader.Exists(path))
                return ResourceLoader.Load<Texture2D>(path);
        return null;
    }

    private static string Initials(string name)
    {
        var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? "?" : parts.Length == 1 ? parts[0][..Math.Min(2, parts[0].Length)] : $"{parts[0][0]}{parts[^1][0]}";
    }

    /// <summary>*emphasis* in scripts becomes italics.</summary>
    private static string Emphasis(string text) => Regex.Replace(text.Replace("[", "[lb]"), @"\*([^*]+)\*", "[i]$1[/i]");
}
