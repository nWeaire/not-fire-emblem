using System;
using Godot;

namespace NotFireEmblem.UI;

/// <summary>Shared look for code-built screens: panels, buttons, labels, headers. Placeholder styling until a Theme exists.</summary>
public static class UiKit
{
    public static readonly Color PanelBg = new(0.08f, 0.09f, 0.13f, 0.92f);
    public static readonly Color PanelBorder = new(0.35f, 0.4f, 0.55f, 0.9f);
    public static readonly Color Accent = new(1f, 0.85f, 0.35f);
    public static readonly Color Muted = new(1f, 1f, 1f, 0.6f);
    public static readonly Color Good = new(0.45f, 0.95f, 0.5f);
    public static readonly Color Bad = new(1f, 0.45f, 0.4f);

    public static StyleBoxFlat PanelStyle(Color? bg = null) => new()
    {
        BgColor = bg ?? PanelBg,
        BorderColor = PanelBorder,
        BorderWidthBottom = 2, BorderWidthTop = 2, BorderWidthLeft = 2, BorderWidthRight = 2,
        CornerRadiusTopLeft = 8, CornerRadiusTopRight = 8, CornerRadiusBottomLeft = 8, CornerRadiusBottomRight = 8,
        ContentMarginLeft = 14, ContentMarginRight = 14, ContentMarginTop = 10, ContentMarginBottom = 10,
    };

    public static PanelContainer Panel(Color? bg = null)
    {
        var panel = new PanelContainer();
        panel.AddThemeStyleboxOverride("panel", PanelStyle(bg));
        return panel;
    }

    public static Label Label(string text, int size = 16, Color? color = null, HorizontalAlignment align = HorizontalAlignment.Left)
    {
        var label = new Label { Text = text, HorizontalAlignment = align, AutowrapMode = TextServer.AutowrapMode.Off };
        label.AddThemeFontSizeOverride("font_size", size);
        if (color is { } c)
            label.Modulate = c;
        return label;
    }

    public static Label Title(string text, int size = 30) => Label(text, size, Accent);

    public static Button Button(string text, Action onPressed, int minWidth = 180, bool enabled = true)
    {
        var button = new Button { Text = text, CustomMinimumSize = new Vector2(minWidth, 38), Disabled = !enabled };
        button.AddThemeFontSizeOverride("font_size", 16);
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
