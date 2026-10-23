using System;
using Godot;

namespace NotFireEmblem.UI;

/// <summary>
/// The crest's light landing on a unit (session 13): two rings of light spread out from where it stands while pixel
/// sparks rise, then it frees itself. It follows the unit on screen (the camera may be moving) through
/// <see cref="Anchor"/>. <see cref="Strength"/> grows with the crest tier: wider rings, more sparks.
/// </summary>
public partial class CrestBurst : Control
{
    private const float Duration = 0.9f;

    public required Func<Vector2> Anchor { get; init; }
    public Color Tint { get; init; } = Colors.White;
    public float Strength { get; init; } = 1f;

    private float _t;
    private readonly RandomNumberGenerator _rng = new(); // visual only, never the battle RNG
    private (Vector2 Offset, float Speed, float Size)[] _sparks = Array.Empty<(Vector2, float, float)>();

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _sparks = new (Vector2, float, float)[(int)(10 * Strength)];
        for (int i = 0; i < _sparks.Length; i++)
            _sparks[i] = (new Vector2(_rng.RandfRange(-28f, 28f) * Strength, _rng.RandfRange(-6f, 10f)),
                _rng.RandfRange(40f, 110f), _rng.RandiRange(2, 4));
    }

    public override void _Process(double delta)
    {
        _t += (float)delta;
        if (_t >= Duration)
        {
            QueueFree();
            return;
        }
        QueueRedraw();
    }

    public override void _Draw()
    {
        var centre = Anchor();
        float k = _t / Duration;
        // Two rings, the second a beat behind; flattened like the ground they spread over.
        foreach (float delay in new[] { 0f, 0.18f })
        {
            float p = Mathf.Clamp((k - delay) / (1f - delay), 0f, 1f);
            if (p <= 0f)
                continue;
            float radius = Mathf.Lerp(8f, 64f * Strength, 1f - (1f - p) * (1f - p));
            DrawSetTransform(centre, 0f, new Vector2(1f, 0.45f));
            DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 48, Tint with { A = 1f - p }, 4f);
            DrawSetTransform(Vector2.Zero);
        }
        // Sparks rise from the feet up past the head.
        foreach (var (offset, speed, size) in _sparks)
        {
            var at = centre + offset + Vector2.Up * speed * _t;
            DrawRect(new Rect2(at, new Vector2(size, size)), Tint with { A = 1f - k });
        }
    }
}
