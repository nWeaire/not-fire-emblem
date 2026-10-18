using Godot;

namespace NotFireEmblem.Core;

/// <summary>
/// Autoload that registers every input action in code (keyboard, mouse and gamepad),
/// so project.godot never holds hand-edited serialized InputEvents. Also runs the hold-to-fast-forward:
/// while <see cref="FastForward"/> is held, the whole game runs at <see cref="FastForwardSpeed"/>×.
/// </summary>
public partial class InputSetup : Node
{
    public const string Confirm = "confirm";
    public const string Cancel = "cancel";
    public const string CursorUp = "cursor_up";
    public const string CursorDown = "cursor_down";
    public const string CursorLeft = "cursor_left";
    public const string CursorRight = "cursor_right";
    public const string CamRotateLeft = "cam_rotate_left";
    public const string CamRotateRight = "cam_rotate_right";
    public const string ZoomIn = "zoom_in";
    public const string ZoomOut = "zoom_out";
    public const string EndTurn = "end_turn";
    public const string ToggleDanger = "toggle_danger";
    public const string NextUnit = "next_unit";
    public const string Info = "info";
    public const string FastForward = "fast_forward";

    /// <summary>Game speed while <see cref="FastForward"/> is held (on top of any --speed dev flag).</summary>
    public const double FastForwardSpeed = 4.0;

    private bool _fastForwarding;
    public const string ToggleBattleScenes = "toggle_battle_scenes";

    public override void _EnterTree()
    {
        ProcessMode = ProcessModeEnum.Always;
        Bind(Confirm, Key(Godot.Key.Z), Key(Godot.Key.Enter), Key(Godot.Key.Space), Mouse(MouseButton.Left), Pad(JoyButton.A));
        Bind(Cancel, Key(Godot.Key.X), Key(Godot.Key.Escape), Key(Godot.Key.Backspace), Mouse(MouseButton.Right), Pad(JoyButton.B));
        Bind(CursorUp, Key(Godot.Key.Up), Key(Godot.Key.W), Pad(JoyButton.DpadUp));
        Bind(CursorDown, Key(Godot.Key.Down), Key(Godot.Key.S), Pad(JoyButton.DpadDown));
        Bind(CursorLeft, Key(Godot.Key.Left), Key(Godot.Key.A), Pad(JoyButton.DpadLeft));
        Bind(CursorRight, Key(Godot.Key.Right), Key(Godot.Key.D), Pad(JoyButton.DpadRight));
        Bind(CamRotateLeft, Key(Godot.Key.Q), Pad(JoyButton.LeftShoulder));
        Bind(CamRotateRight, Key(Godot.Key.E), Pad(JoyButton.RightShoulder));
        Bind(ZoomIn, Mouse(MouseButton.WheelUp), Key(Godot.Key.Equal), Key(Godot.Key.KpAdd));
        Bind(ZoomOut, Mouse(MouseButton.WheelDown), Key(Godot.Key.Minus), Key(Godot.Key.KpSubtract));
        Bind(EndTurn, Key(Godot.Key.T), Pad(JoyButton.Start));
        Bind(ToggleDanger, Key(Godot.Key.G), Pad(JoyButton.Back));
        Bind(NextUnit, Key(Godot.Key.Tab), Pad(JoyButton.Y));
        Bind(Info, Key(Godot.Key.R), Pad(JoyButton.X));
        Bind(ToggleBattleScenes, Key(Godot.Key.B), Pad(JoyButton.RightStick));
        // Space also confirms (a press); holding it speeds everything up, like holding A in Fire Emblem.
        Bind(FastForward, Key(Godot.Key.Space));
        // Menu buttons (title, prep, results, hub) are pressed by Godot's built-in ui_accept, which only knows
        // Enter/Space: give the menus the game's own confirm / cancel keys and WASD too.
        Extend("ui_accept", Key(Godot.Key.Z));
        Extend("ui_cancel", Key(Godot.Key.X));
        Extend("ui_up", Key(Godot.Key.W));
        Extend("ui_down", Key(Godot.Key.S));
        Extend("ui_left", Key(Godot.Key.A));
        Extend("ui_right", Key(Godot.Key.D));
    }

    public override void _Process(double delta)
    {
        bool held = Input.IsActionPressed(FastForward);
        if (held == _fastForwarding)
            return;
        _fastForwarding = held;
        Engine.TimeScale = held ? Engine.TimeScale * FastForwardSpeed : Engine.TimeScale / FastForwardSpeed;
    }

    private static void Extend(string action, InputEvent e)
    {
        if (InputMap.HasAction(action) && !InputMap.ActionHasEvent(action, e))
            InputMap.ActionAddEvent(action, e);
    }

    private static void Bind(string action, params InputEvent[] events)
    {
        if (InputMap.HasAction(action))
            InputMap.EraseAction(action);
        InputMap.AddAction(action, 0.5f);
        foreach (var e in events)
            InputMap.ActionAddEvent(action, e);
    }

    private static InputEventKey Key(Key key) => new() { PhysicalKeycode = key };
    private static InputEventMouseButton Mouse(MouseButton button) => new() { ButtonIndex = button };
    private static InputEventJoypadButton Pad(JoyButton button) => new() { ButtonIndex = button };
}
