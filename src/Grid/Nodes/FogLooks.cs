using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// How fog of war is drawn (session 12 playtest: "make fog of war more obvious, a transparent eerie fog over the
/// tiles"). Style 0 is the original flat dark squares; the others lay drifting noise fog over the hidden cells, softer
/// at the edge of what the party can see. Each style is one or more layers at different heights.
/// </summary>
public static class FogLooks
{
    /// <summary>One fog layer: tint, how thick, how patchy, how high above the tiles, and how it drifts.</summary>
    public sealed record Layer(Color Tint, float Density, float Contrast, float Lift, float Scale, float Speed);

    public static readonly IReadOnlyList<(string Name, Layer[] Layers)> Styles = new (string, Layer[])[]
    {
        ("Flat squares (original)", System.Array.Empty<Layer>()),
        ("Dark murk", new[] { new Layer(new Color(0.05f, 0.05f, 0.12f), 0.82f, 0.35f, 0.03f, 0.35f, 0.04f) }),
        ("Eerie mist", new[]
        {
            new Layer(new Color(0.16f, 0.14f, 0.24f), 0.55f, 0.2f, 0.03f, 0.3f, 0.02f),
            new Layer(new Color(0.66f, 0.62f, 0.82f), 0.62f, 0.75f, 0.45f, 0.28f, 0.05f),
            new Layer(new Color(0.78f, 0.75f, 0.9f), 0.35f, 0.8f, 0.95f, 0.45f, 0.08f),
        }),
        ("Ghost fog (green)", new[]
        {
            new Layer(new Color(0.04f, 0.1f, 0.1f), 0.6f, 0.2f, 0.03f, 0.3f, 0.02f),
            new Layer(new Color(0.42f, 0.72f, 0.64f), 0.55f, 0.8f, 0.4f, 0.3f, 0.06f),
            new Layer(new Color(0.6f, 0.85f, 0.78f), 0.3f, 0.85f, 0.9f, 0.5f, 0.09f),
        }),
        ("Shroud (purple)", new[]
        {
            new Layer(new Color(0.1f, 0.03f, 0.14f), 0.7f, 0.25f, 0.03f, 0.3f, 0.02f),
            new Layer(new Color(0.55f, 0.3f, 0.75f), 0.5f, 0.8f, 0.5f, 0.32f, 0.05f),
        }),
    };

    private const string ShaderCode = """
        shader_type spatial;
        render_mode unshaded, cull_disabled, depth_draw_never, shadows_disabled;
        uniform vec3 tint = vec3(0.6, 0.6, 0.8);
        uniform float density = 0.7;
        uniform float contrast = 0.6;
        uniform float scale = 0.35;
        uniform float speed = 0.05;
        uniform float pull = 1.3;
        uniform sampler2D mask : filter_linear, repeat_disable;
        uniform vec2 grid_size = vec2(1.0);
        varying vec3 wpos;
        float hash(vec2 p) { return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453); }
        float noise(vec2 p) {
            vec2 i = floor(p); vec2 f = fract(p); vec2 u = f * f * (3.0 - 2.0 * f);
            return mix(mix(hash(i), hash(i + vec2(1.0, 0.0)), u.x), mix(hash(i + vec2(0.0, 1.0)), hash(i + vec2(1.0, 1.0)), u.x), u.y);
        }
        float fbm(vec2 p) { float v = 0.0; float a = 0.5; for (int k = 0; k < 4; k++) { v += a * noise(p); p *= 2.03; a *= 0.5; } return v; }
        void vertex() {
            wpos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
            // Pulled toward the camera a little further than the props (MapView.BillboardPull) and less than the units
            // (UnitPull): same place on screen, so the fog covers the trees in its cells but not units in front of it.
            vec4 view = MODELVIEW_MATRIX * vec4(VERTEX, 1.0);
            view.xyz -= normalize(view.xyz) * pull;
            POSITION = PROJECTION_MATRIX * view;
        }
        void fragment() {
            vec2 p = wpos.xz * scale;
            float m = mix(fbm(p + vec2(TIME * speed, TIME * speed * 0.6)), fbm(p * 1.7 - vec2(TIME * speed * 0.8, -TIME * speed * 0.3)), 0.5);
            ALBEDO = tint * (0.85 + 0.3 * m);
            // Cell (x, y) is centred on world (x, y); the filtered mask fades the fog's edge inside the hidden cells.
            float hidden = smoothstep(0.35, 0.85, texture(mask, (wpos.xz + 0.5) / grid_size).r);
            ALPHA = clamp(density * (1.0 - contrast + contrast * m) * hidden, 0.0, 1.0);
        }
        """;

    private static Shader? _shader;

    /// <summary>A material for one layer (FogPlane sets its mask).</summary>
    public static ShaderMaterial Material(Layer layer)
    {
        _shader ??= new Shader { Code = ShaderCode };
        var material = new ShaderMaterial { Shader = _shader };
        material.SetShaderParameter("tint", new Vector3(layer.Tint.R, layer.Tint.G, layer.Tint.B));
        material.SetShaderParameter("density", layer.Density);
        material.SetShaderParameter("contrast", layer.Contrast);
        material.SetShaderParameter("scale", layer.Scale);
        material.SetShaderParameter("speed", layer.Speed);
        material.SetShaderParameter("pull", (MapView.BillboardPull + MapView.UnitPull) / 2f + 0.1f);
        return material;
    }
}
