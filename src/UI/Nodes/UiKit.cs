using System;
using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.UI;

/// <summary>
/// Shared look for code-built screens, in the approved "Clean" style (docs/ART_DIRECTION.md): flat charcoal panels
/// with a thin warm-white border and corner ticks (9-slice art in assets/ui/), Pixelify Sans for text and Silkscreen
/// for small-caps labels. <see cref="Theme"/> is set on the root window by <see cref="UiThemeSetup"/>, so every
/// Control picks it up; the helpers below build common pieces.
/// </summary>
public static class UiKit
{
    /// <summary>UI art is drawn at this many screen pixels per art pixel.</summary>
    public const int PixelScale = 3;

    public static readonly Color Text = new(0.933f, 0.91f, 0.863f);         // warm white (238, 232, 220)
    public static readonly Color PanelBg = new(0.17f, 0.17f, 0.18f, 0.96f);
    public static readonly Color PanelBorder = new(0.933f, 0.91f, 0.863f, 0.9f);
    public static readonly Color Accent = new(0.94f, 0.78f, 0.47f);          // (240, 200, 120)
    public static readonly Color Muted = new(0.933f, 0.91f, 0.863f, 0.6f);
    public static readonly Color Good = new(0.47f, 0.78f, 0.9f);             // player HP blue (120, 200, 230)
    public static readonly Color Bad = new(0.92f, 0.43f, 0.39f);             // enemy HP red (235, 110, 100)
    public static readonly Color Outline = new(0.05f, 0.05f, 0.07f);

    private static Theme? _theme;
    private static Font? _textFont;
    private static Font? _capsFont;
    private static readonly Dictionary<string, Texture2D?> Scaled = new();

    // VT323 for all text (session 7: the user found Pixelify Sans and Silkscreen hard to read, e.g. 5/S and B/G).
    public static Font TextFont => _textFont ??= LoadFont("res://assets/fonts/VT323-Regular.ttf");
    public static Font CapsFont => _capsFont ??= LoadFont("res://assets/fonts/VT323-Regular.ttf");

    /// <summary>
    /// The font size to use for a size written for the old font. VT323 draws smaller per point, and its pixels only
    /// stay even at multiples of 4 px (it is drawn without smoothing), so sizes scale by 1.3 and snap to 4. Nothing
    /// goes below 20 px: at 16 VT323's small letters are hard to read.
    /// </summary>
    public static int Px(int size)
    {
        double scaled = size * 1.3;
        // Big titles snap to 16 so their pixel steps stay even too.
        int step = scaled >= 40 ? 16 : 4;
        return Math.Max(20, step * (int)Math.Round(scaled / step, MidpointRounding.AwayFromZero));
    }

    private static Font LoadFont(string path)
    {
        var font = ResourceLoader.Exists(path) ? ResourceLoader.Load<FontFile>(path) : null;
        if (font is null)
            return ThemeDB.FallbackFont;
        font.Antialiasing = TextServer.FontAntialiasing.None;
        font.Hinting = TextServer.Hinting.None;
        font.SubpixelPositioning = TextServer.SubpixelPositioning.Disabled;
        return font;
    }

    /// <summary>A texture scaled up by whole pixels with nearest filtering, so 9-slice margins and pixel art stay crisp.</summary>
    public static Texture2D? PixelTexture(string path, int scale = PixelScale)
    {
        string key = $"{path}@{scale}";
        if (Scaled.TryGetValue(key, out var cached))
            return cached;
        Texture2D? result = null;
        if (ResourceLoader.Exists(path))
        {
            var image = ResourceLoader.Load<Texture2D>(path).GetImage();
            image.Decompress();
            image.Resize(image.GetWidth() * scale, image.GetHeight() * scale, Image.Interpolation.Nearest);
            result = ImageTexture.CreateFromImage(image);
        }
        Scaled[key] = result;
        return result;
    }

    /// <summary>The Clean window panel (9-slice, 12 art-pixel corners). A custom background colour gives a flat panel instead.</summary>
    public static StyleBox PanelStyle(Color? bg = null, int margin = 14)
    {
        var texture = PixelTexture("res://assets/ui/panel.png");
        if (texture is null || bg is not null)
            return FlatPanel(bg ?? PanelBg, margin);
        int m = 12 * PixelScale;
        // The frame line and corner ticks take about 6 art pixels; keep content clear of them.
        float side = 6 * PixelScale + margin * 0.5f;
        float top = 5 * PixelScale + margin * 0.4f;
        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = m, TextureMarginRight = m, TextureMarginTop = m, TextureMarginBottom = m,
            ContentMarginLeft = side, ContentMarginRight = side, ContentMarginTop = top, ContentMarginBottom = top,
        };
    }

    /// <summary>The Clean name plate (9-slice, 8 art-pixel corners).</summary>
    public static StyleBox PlateStyle(int margin = 8)
    {
        var texture = PixelTexture("res://assets/ui/name_plate.png");
        if (texture is null)
            return FlatPanel(PanelBg, margin);
        int m = 8 * PixelScale;
        return new StyleBoxTexture
        {
            Texture = texture,
            TextureMarginLeft = m, TextureMarginRight = m, TextureMarginTop = m, TextureMarginBottom = m,
            ContentMarginLeft = margin + 10, ContentMarginRight = margin + 10, ContentMarginTop = margin - 2, ContentMarginBottom = margin,
        };
    }

    /// <summary>A flat charcoal box with a thin warm-white border: tinted panels and buttons.</summary>
    public static StyleBoxFlat FlatPanel(Color bg, int margin = 14, Color? border = null, int borderWidth = 2) => new()
    {
        BgColor = bg,
        BorderColor = border ?? PanelBorder,
        BorderWidthBottom = borderWidth, BorderWidthTop = borderWidth, BorderWidthLeft = borderWidth, BorderWidthRight = borderWidth,
        ContentMarginLeft = margin, ContentMarginRight = margin, ContentMarginTop = margin * 0.7f, ContentMarginBottom = margin * 0.7f,
    };

    /// <summary>The game-wide theme.</summary>
    public static Theme Theme => _theme ??= BuildTheme();

    private static Theme BuildTheme()
    {
        var t = new Theme { DefaultFont = TextFont, DefaultFontSize = Px(18) };

        t.SetStylebox("panel", "PanelContainer", PanelStyle());
        t.SetStylebox("panel", "Panel", PanelStyle());
        t.SetColor("font_color", "Label", Text);
        t.SetColor("font_outline_color", "Label", Outline);

        var normal = FlatPanel(new Color(0.13f, 0.13f, 0.14f, 0.95f), 10, new Color(Text, 0.35f));
        var hover = FlatPanel(new Color(0.2f, 0.2f, 0.21f, 0.98f), 10, Text);
        var pressed = FlatPanel(new Color(0.3f, 0.26f, 0.18f, 1f), 10, Accent);
        var disabled = FlatPanel(new Color(0.1f, 0.1f, 0.11f, 0.8f), 10, new Color(Text, 0.15f));
        var focus = FlatPanel(new Color(0, 0, 0, 0), 10, Accent);
        focus.DrawCenter = false;
        foreach (var type in new[] { "Button", "OptionButton", "MenuButton", "CheckBox" })
        {
            t.SetStylebox("normal", type, normal);
            t.SetStylebox("hover", type, hover);
            t.SetStylebox("pressed", type, pressed);
            t.SetStylebox("hover_pressed", type, pressed);
            t.SetStylebox("disabled", type, disabled);
            t.SetStylebox("focus", type, focus);
            t.SetColor("font_color", type, Text);
            t.SetColor("font_hover_color", type, Colors.White);
            t.SetColor("font_focus_color", type, Accent);
            t.SetColor("font_pressed_color", type, Accent);
            t.SetColor("font_hover_pressed_color", type, Accent);
            t.SetColor("font_disabled_color", type, new Color(Text, 0.35f));
        }

        var edge = new Color(0.05f, 0.05f, 0.05f);
        t.SetStylebox("background", "ProgressBar", new StyleBoxFlat { BgColor = new Color(0.12f, 0.12f, 0.12f), BorderColor = edge, BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2 });
        t.SetStylebox("fill", "ProgressBar", new StyleBoxFlat { BgColor = Good, BorderColor = edge, BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2 });

        var field = new Color(0.1f, 0.1f, 0.11f, 0.92f);
        t.SetStylebox("panel", "PopupMenu", FlatPanel(PanelBg, 8));
        t.SetStylebox("panel", "TooltipPanel", FlatPanel(PanelBg, 8));
        t.SetStylebox("normal", "LineEdit", FlatPanel(field, 8, new Color(Text, 0.35f)));
        t.SetStylebox("panel", "ItemList", FlatPanel(field, 6, new Color(Text, 0.35f)));
        t.SetStylebox("panel", "Tree", FlatPanel(field, 6, new Color(Text, 0.35f)));
        t.SetStylebox("tab_selected", "TabContainer", FlatPanel(new Color(0.2f, 0.2f, 0.21f), 8, Accent));
        t.SetStylebox("tab_unselected", "TabContainer", FlatPanel(new Color(0.12f, 0.12f, 0.13f), 8, new Color(Text, 0.3f)));
        t.SetStylebox("panel", "TabContainer", PanelStyle());
        return t;
    }

    public static PanelContainer Panel(Color? bg = null)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", PanelStyle(bg));
        return panel;
    }

    /// <summary>A name plate holding one label (speaker names, unit names).</summary>
    public static PanelContainer Plate(Label label)
    {
        var plate = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        plate.AddThemeStyleboxOverride("panel", PlateStyle());
        plate.AddChild(label);
        return plate;
    }

    public static Label Label(string text, int size = 18, Color? color = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = align, AutowrapMode = TextServer.AutowrapMode.Off };
        label.AddThemeFontSizeOverride("font_size", Px(size));
        if (color is { } c)
            label.Modulate = c;
        return label;
    }

    /// <summary>A small-caps Silkscreen label (stat names, section headers).</summary>
    public static Label Caps(string text, int size = 14, Color? color = null)
    {
        var label = Label(text, size, color ?? Accent);
        label.AddThemeFontOverride("font", CapsFont);
        return label;
    }

    public static Label Title(string text, int size = 32) => Label(text, size, Accent);

    /// <summary>Gives a label a dark outline, for text drawn straight over the map or a picture.</summary>
    public static T Outlined<T>(T label, int size = 6) where T : Label
    {
        label.AddThemeConstantOverride("outline_size", size);
        label.AddThemeColorOverride("font_outline_color", Outline);
        return label;
    }

    public static Button Button(string text, Action onPressed, int minWidth = 180, bool enabled = true)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(minWidth, 38), Disabled = !enabled };
        button.AddThemeFontSizeOverride("font_size", Px(18));
        button.Pressed += onPressed;
        return button;
    }

    /// <summary>A full-screen vertical gradient used as a stand-in for story backgrounds and screen backdrops.</summary>
    public static TextureRect Backdrop(Color top, Color bottom)
    {
        var rect = new TextureRect
        {
            Texture = new GradientTexture2D
            {
                Gradient = new Gradient { Colors = new[] { top, bottom }, Offsets = new[] { 0f, 1f } },
                FillFrom = new Vector2(0, 0),
                FillTo = new Vector2(0, 1),
                Width = 8,
                Height = 128,
            },
            StretchMode = TextureRect.StretchModeEnum.Scale,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }

    /// <summary>
    /// A full-screen pixel-art picture from assets/backgrounds/&lt;id&gt;.png, scaled to cover the screen with nearest
    /// filtering, or null if there is no such file.
    /// </summary>
    public static TextureRect? Picture(string id)
    {
        var texture = Background(id);
        if (texture is null)
            return null;
        var rect = new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCovered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        rect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return rect;
    }

    /// <summary>assets/backgrounds/&lt;id&gt;.png, or null.</summary>
    public static Texture2D? Background(string id)
    {
        string path = $"res://assets/backgrounds/{id}.png";
        return ResourceLoader.Exists(path) ? ResourceLoader.Load<Texture2D>(path) : null;
    }

    /// <summary>A stable colour from a string (placeholder portraits and backgrounds).</summary>
    public static Color HashColor(string key, float saturation = 0.45f, float value = 0.55f)
    {
        uint h = 2166136261;
        foreach (char c in key)
            h = (h ^ c) * 16777619;
        return Color.FromHsv(h % 360 / 360f, saturation, value);
    }

    /// <summary>"elder_wenna" -> "Elder Wenna".</summary>
    public static string TitleCase(string id)
    {
        var words = id.Replace('_', ' ').Split(' ', StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < words.Length; i++)
            words[i] = char.ToUpperInvariant(words[i][0]) + words[i][1..];
        return string.Join(' ', words);
    }

    /// <summary>Fills a control's parent area.</summary>
    public static T Fill<T>(T control) where T : Control
    {
        control.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        return control;
    }
}
