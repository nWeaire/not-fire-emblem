using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace NotFireEmblem.Core;

/// <summary>
/// Development hooks driven by user command-line args (after "--"):
///   --screenshot=&lt;path&gt;     save the viewport to a PNG after --frames (default 90) frames, then quit
///   --frames=&lt;n&gt;            frame to capture on
///   --shotevery=&lt;n&gt;         also save &lt;path&gt;_&lt;frame&gt;.png every n frames until then (checking animations)
///   --press=&lt;f:action,...&gt;   inject action presses at frames, e.g. --press=60:confirm,70:cursor_right
///   --speed=&lt;x&gt;             Engine.TimeScale multiplier (animations and timers run faster)
///   --autoplay               the AI also plays the player's side (soak tests)
///   --skipdialogue           dialogue scenes end at once
///   --autoadvance            prep, results and hub screens continue on their own
///   --newgame / --load=N     (title screen) start a campaign at once
/// BattleController also reads --seed=&lt;n&gt; (battle RNG); Boot reads --map=&lt;id&gt; (e.g. prologue, ch1, test_ford).
/// </summary>
public partial class DevTools : Node
{
    public static bool Autoplay { get; private set; }
    public static bool SkipDialogue { get; private set; }
    public static bool AutoAdvance { get; private set; }

    private string? _screenshotPath;
    private int _captureFrame = 90;
    private int _shotEvery;
    private readonly List<(int Frame, string Action)> _presses = new();
    private int _frame;

    public override void _Ready()
    {
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            Autoplay |= arg == "--autoplay";
            SkipDialogue |= arg == "--skipdialogue";
            AutoAdvance |= arg == "--autoadvance";
            var parts = arg.Split('=', 2);
            if (parts.Length != 2)
                continue;
            switch (parts[0])
            {
                case "--screenshot":
                    _screenshotPath = parts[1];
                    break;
                case "--shotevery":
                    _shotEvery = int.Parse(parts[1]);
                    break;
                case "--frames":
                    _captureFrame = int.Parse(parts[1]);
                    break;
                case "--speed":
                    Engine.TimeScale = double.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture);
                    break;
                case "--press":
                    foreach (var item in parts[1].Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        var kv = item.Split(':');
                        _presses.Add((int.Parse(kv[0]), kv[1]));
                    }
                    break;
            }
        }
        SetProcess(_screenshotPath is not null || _presses.Count > 0);
    }

    public override void _Process(double delta)
    {
        _frame++;
        foreach (var (_, action) in _presses.Where(p => p.Frame == _frame))
        {
            Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = true });
            Input.ParseInputEvent(new InputEventAction { Action = action, Pressed = false });
        }
        if (_screenshotPath is not null && _shotEvery > 0 && _frame % _shotEvery == 0 && _frame < _captureFrame)
            GetViewport().GetTexture().GetImage().SavePng(_screenshotPath.Replace(".png", $"_{_frame:D5}.png"));
        if (_screenshotPath is not null && _frame == _captureFrame)
        {
            GetViewport().GetTexture().GetImage().SavePng(_screenshotPath);
            GD.Print($"Screenshot saved to {_screenshotPath}");
            GetTree().Quit();
        }
    }
}
