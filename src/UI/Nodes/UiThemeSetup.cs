using Godot;

namespace NotFireEmblem.UI;

/// <summary>
/// Autoload that applies the Clean UI theme (<see cref="UiKit.Theme"/>) everywhere. A theme set on the root window does
/// not reach Controls under a CanvasLayer (the HUDs), so it is also merged into the engine's default theme, which is
/// every Control's last fallback.
/// </summary>
public partial class UiThemeSetup : Node
{
    public override void _EnterTree()
    {
        var theme = UiKit.Theme;
        ThemeDB.GetDefaultTheme().MergeWith(theme);
        ThemeDB.FallbackFont = theme.DefaultFont;
        ThemeDB.FallbackFontSize = theme.DefaultFontSize;
        GetTree().Root.Theme = theme;
    }
}
