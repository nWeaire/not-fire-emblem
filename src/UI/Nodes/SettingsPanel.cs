using Godot;
using NotFireEmblem.Audio;
using NotFireEmblem.Core;

namespace NotFireEmblem.UI;

/// <summary>The settings controls (volumes, text speed, battle scenes), used by the title screen and the hub.</summary>
public partial class SettingsPanel : VBoxContainer
{
    public override void _Ready()
    {
        AddThemeConstantOverride("separation", 10);
        AddChild(Slider("Music volume", Settings.MusicVolume, v => Settings.MusicVolume = v, null));
        AddChild(Slider("Sound effects volume", Settings.SfxVolume, v => Settings.SfxVolume = v, () => AudioDirector.PlaySfx("confirm")));

        AddChild(UiKit.Label("Text speed", 16, UiKit.Muted));
        var speed = new OptionButton { CustomMinimumSize = new Vector2(280, 0) };
        foreach (var name in new[] { "Slow", "Normal", "Fast", "Instant" })
            speed.AddItem(name);
        speed.Selected = (int)Settings.TextSpeed;
        speed.ItemSelected += i => Settings.TextSpeed = (TextSpeed)(int)i;
        AddChild(speed);

        var scenes = new CheckButton { Text = "Battle scenes (B toggles them in battle)", ButtonPressed = Settings.BattleScenes };
        scenes.Toggled += on => Settings.BattleScenes = on;
        AddChild(scenes);
    }

    private static Control Slider(string label, int value, System.Action<int> onChange, System.Action? onRelease)
    {
        var box = new VBoxContainer();
        box.AddThemeConstantOverride("separation", 2);
        var title = UiKit.Label($"{label}: {value}", 16, UiKit.Muted);
        box.AddChild(title);
        var slider = new HSlider
        {
            MinValue = 0, MaxValue = 100, Step = 5, Value = value,
            CustomMinimumSize = new Vector2(280, 24), FocusMode = Control.FocusModeEnum.All,
        };
        slider.ValueChanged += v =>
        {
            onChange((int)v);
            title.Text = $"{label}: {(int)v}";
        };
        if (onRelease is not null)
            slider.DragEnded += _ => onRelease();
        box.AddChild(slider);
        return box;
    }
}
