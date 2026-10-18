using System;
using Godot;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>
/// A dimmed full-screen layer with a centred panel (the hub's "save before leaving?" prompt, the battle's settings).
/// Add controls to <see cref="Body"/>; a button calls <see cref="Close"/>, and so does Cancel (X / Esc).
/// </summary>
public partial class Modal : ColorRect
{
    public VBoxContainer Body { get; } = new();

    /// <summary>Raised once when the modal closes, by a button or by Cancel.</summary>
    public event Action? Closed;

    private bool _closed;

    public Modal(string title)
    {
        Color = new Color(0f, 0f, 0f, 0.6f);
        MouseFilter = MouseFilterEnum.Stop;
        Body.AddThemeConstantOverride("separation", 10);
        Body.AddChild(UiKit.Title(title, 24));
    }

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        var center = new CenterContainer();
        AddChild(UiKit.Fill(center));
        var panel = UiKit.Panel();
        center.AddChild(panel);
        panel.AddChild(Body);
        CallDeferred(MethodName.FocusFirst);
    }

    private void FocusFirst()
    {
        foreach (var node in Body.FindChildren("*", "BaseButton", true, false))
        {
            if (node is BaseButton { Disabled: false } button)
            {
                button.GrabFocus();
                return;
            }
        }
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed(InputSetup.Cancel))
            return;
        GetViewport().SetInputAsHandled();
        Close();
    }

    public void Close()
    {
        if (_closed)
            return;
        _closed = true;
        Closed?.Invoke();
        QueueFree();
    }
}
