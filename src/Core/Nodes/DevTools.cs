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
///   --resume                 (title screen) resume the suspended battle
///   --exp=N                  player units start each battle with N EXP (to test level-ups)
///   --hideunits              the battle map shows no units or HUD (screenshots of the map art alone)
///   --focus=X,Y              the battle camera and cursor start on this cell instead of the lord
///   --level=N|auto           single battles (--map): player units below level N are raised to it with average gains;
///                            auto picks the map's campaign level (<see cref="CampaignLevels"/>)
/// BattleController also reads --seed=&lt;n&gt; (battle RNG); Boot reads --map=&lt;id&gt; (e.g. prologue, ch1, test_ford).
/// </summary>
public partial class DevTools : Node
{
    public static bool Autoplay { get; private set; }
    public static bool SkipDialogue { get; private set; }
    public static bool AutoAdvance { get; private set; }
    public static int? StartExp { get; private set; }
    public static bool HideUnits { get; private set; }
    public static NotFireEmblem.Grid.GridPos? FocusCell { get; private set; }
    /// <summary>--level: a level, or 0 for "auto" (the map's entry in <see cref="CampaignLevels"/>); null when unset.</summary>
    public static int? PartyLevel { get; private set; }

    /// <summary>
    /// About where a party stands entering each map in a real campaign (session 11: autoplay soak levels, rounded up
    /// because autoplay's Rowan rarely fights and survivors' +20 EXP isn't logged). Aldric starts at 5 and stays ahead.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, int> CampaignLevels = new Dictionary<string, int>
    {
        ["prologue"] = 1, ["ch1"] = 2, ["ch2"] = 3, ["ch3"] = 4, ["ch4"] = 5, ["ch5"] = 6, ["ch6"] = 7, ["ch7"] = 8, ["ch8"] = 9, ["ch9"] = 10, ["ch10"] = 11, ["ch11"] = 12, ["ch12"] = 13,
    };

    /// <summary>The --level to use on a map, or null when the flag is unset.</summary>
    public static int? PartyLevelFor(string mapId) =>
        PartyLevel is 0 ? CampaignLevels.GetValueOrDefault(mapId, 1) : PartyLevel;

    private string? _screenshotPath;
    private int _captureFrame = 90;
    private int _shotEvery;
    private readonly List<(int Frame, string Action)> _presses = new();
    private int _frame;

    public override void _Ready()
    {
        ProcessMode = ProcessModeEnum.Always; // presses and screenshots also work while the battle is paused
        foreach (var arg in OS.GetCmdlineUserArgs())
        {
            Autoplay |= arg == "--autoplay";
            SkipDialogue |= arg == "--skipdialogue";
            AutoAdvance |= arg == "--autoadvance";
            HideUnits |= arg == "--hideunits";
            var parts = arg.Split('=', 2);
            if (parts.Length != 2)
                continue;
            switch (parts[0])
            {
                case "--exp":
                    StartExp = int.Parse(parts[1]);
                    break;
                case "--level":
                    PartyLevel = parts[1] == "auto" ? 0 : int.Parse(parts[1]);
                    break;
                case "--focus":
                    var xy = parts[1].Split(',');
                    FocusCell = new NotFireEmblem.Grid.GridPos(int.Parse(xy[0]), int.Parse(xy[1]));
                    break;
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
