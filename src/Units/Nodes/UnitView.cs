using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace NotFireEmblem.Units;

/// <summary>
/// A unit on the map: a camera-facing pixel-art sprite (from SpriteLibrary.Map) that picks its south/east/north/west
/// frames from the unit's facing relative to the camera, with a floating name/HP label. Units without map art fall
/// back to a capsule in the faction colour with a facing cone.
/// Driven only by BattleEventPlayer and PlayerInputController previews; it never changes game state.
/// </summary>
public partial class UnitView : Node3D
{
    public static readonly Color PlayerColor = new(0.25f, 0.5f, 1f);
    public static readonly Color EnemyColor = new(0.9f, 0.25f, 0.2f);
    public static readonly Color AllyColor = new(0.3f, 0.8f, 0.35f);

    /// <summary>World size of one sprite pixel: a 48px frame stands about 1.2 cells tall.</summary>
    private const float SpritePixelSize = 0.025f;
    /// <summary>Transparent rows below the feet in PixelLab frames.</summary>
    private const float SpriteFootPadding = 2f;

    private Node3D _model = null!;
    private StandardMaterial3D? _material;
    private AnimatedSprite3D? _sprite;
    private Label3D _label = null!;
    private Color _baseColor;
    private Color _tint = Colors.White;
    private string _name = "";
    private string _title = "";
    private Vector3 _facing = Vector3.Back;
    private string _anim = "idle";
    private string _dir = "south";

    public string UnitId { get; private set; } = "";

    public static Color ColorFor(Faction faction) => faction switch
    {
        Faction.Player => PlayerColor,
        Faction.Enemy => EnemyColor,
        _ => AllyColor,
    };

    public void Setup(string unitId, string name, string title, Faction faction, bool isBoss, int hp, int maxHp, SpriteFrames? mapFrames)
    {
        UnitId = unitId;
        _name = name;
        _title = title;
        _baseColor = ColorFor(faction);
        Name = $"Unit_{unitId}";

        _model = new Node3D();
        AddChild(_model);
        float scale = isBoss ? 1.15f : 1f;
        if (mapFrames is not null)
            BuildSprite(mapFrames, scale);
        else
            BuildCapsule(scale);
        if (isBoss)
        {
            _model.AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.08f, OuterRadius = 0.16f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.8f, 0.15f), Metallic = 0.6f },
                Position = new Vector3(0f, mapFrames is not null ? 1.5f : 1.15f, 0f),
            });
        }

        _label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = 40,
            OutlineSize = 10,
            PixelSize = 0.006f,
            Position = new Vector3(0f, (mapFrames is not null ? 1.65f : 1.3f) * (isBoss ? 1.15f : 1f), 0f),
            RenderPriority = 1,
            OutlineRenderPriority = 0,
        };
        AddChild(_label);
        SetHp(hp, maxHp);
    }

    private void BuildSprite(SpriteFrames frames, float scale)
    {
        _sprite = new AnimatedSprite3D
        {
            SpriteFrames = frames,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            PixelSize = SpritePixelSize * scale,
            // Pivot at the feet, so the billboard leans back from the ground instead of sinking into it.
            Offset = new Vector2(0f, 24f - SpriteFootPadding),
        };
        _sprite.AnimationFinished += () =>
        {
            if (_anim != "idle" && _anim != "walk")
                PlayAnim("idle");
        };
        _model.AddChild(_sprite);
        PlayAnim("idle");
    }

    private void BuildCapsule(float scale)
    {
        _material = new StandardMaterial3D { AlbedoColor = _baseColor, Roughness = 0.6f };
        _model.AddChild(new MeshInstance3D
        {
            Mesh = new CapsuleMesh { Radius = 0.25f * scale, Height = 0.95f * scale },
            MaterialOverride = _material,
            Position = new Vector3(0f, 0.48f * scale, 0f),
        });
        // Facing marker points along local -Z (LookAt's forward).
        _model.AddChild(new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0f, BottomRadius = 0.1f, Height = 0.25f },
            MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(0.95f, 0.9f, 0.8f) },
            Position = new Vector3(0f, 0.6f * scale, -0.28f * scale),
            Rotation = new Vector3(-Mathf.Pi / 2f, 0f, 0f),
        });
    }

    public override void _Process(double delta)
    {
        if (_sprite is null)
            return;
        string dir = DirectionFromCamera();
        if (dir != _dir)
        {
            // Keep the animation's progress when only the view direction changes.
            int frame = _sprite.Frame;
            float progress = _sprite.FrameProgress;
            _dir = dir;
            _sprite.Play($"{_anim}_{_dir}");
            _sprite.SetFrameAndProgress(frame, progress);
        }
    }

    /// <summary>Which art direction shows the unit's facing from the current camera (south = facing the camera).</summary>
    private string DirectionFromCamera()
    {
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null)
            return _dir;
        var basis = camera.GlobalBasis;
        var forward = (-basis.Z with { Y = 0f }).Normalized();
        var right = (basis.X with { Y = 0f }).Normalized();
        float along = _facing.Dot(forward);
        float across = _facing.Dot(right);
        if (Mathf.Abs(across) > Mathf.Abs(along))
            return across > 0f ? "east" : "west";
        return along > 0f ? "north" : "south";
    }

    private void PlayAnim(string anim)
    {
        if (_sprite is null)
            return;
        _anim = anim;
        _dir = DirectionFromCamera();
        _sprite.Play($"{_anim}_{_dir}");
    }

    public void SetHp(int hp, int maxHp)
    {
        float ratio = maxHp > 0 ? hp / (float)maxHp : 0f;
        _label.Text = $"{_name}\n{hp}/{maxHp}";
        _label.Modulate = ratio > 0.5f ? Colors.White : ratio > 0.25f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
    }

    public void SetDone(bool done)
    {
        if (_material is not null)
            _material.AlbedoColor = done ? _baseColor.Darkened(0.6f) : _baseColor;
        if (_sprite is not null)
        {
            _tint = done ? new Color(0.45f, 0.45f, 0.5f) : Colors.White;
            _sprite.Modulate = _tint;
        }
    }

    public void FaceToward(Vector3 worldPoint)
    {
        var flat = new Vector3(worldPoint.X, GlobalPosition.Y, worldPoint.Z);
        if (flat.DistanceSquaredTo(GlobalPosition) <= 0.0001f)
            return;
        _facing = (flat - GlobalPosition).Normalized();
        if (_sprite is null)
            _model.LookAt(flat, Vector3.Up);
    }

    public async Task MoveAlong(IReadOnlyList<Vector3> points, float secondsPerStep = 0.09f)
    {
        if (points.Count < 2)
            return;
        PlayAnim("walk");
        var tween = CreateTween();
        for (int i = 1; i < points.Count; i++)
        {
            var target = points[i];
            tween.TweenCallback(Callable.From(() => FaceToward(target)));
            tween.TweenProperty(this, "position", target, secondsPerStep);
        }
        await ToSignal(tween, Tween.SignalName.Finished);
        PlayAnim("idle");
    }

    public async Task Lunge(Vector3 toward)
    {
        FaceToward(toward);
        PlayAnim("attack");
        var start = Position;
        var dir = (toward - start) with { Y = 0f };
        var peak = start + dir.Normalized() * Mathf.Min(0.4f, dir.Length() * 0.4f);
        var tween = CreateTween();
        tween.TweenProperty(this, "position", peak, 0.1f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(this, "position", start, 0.15f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Flash()
    {
        var tween = CreateTween();
        if (_sprite is not null)
        {
            PlayAnim("hit");
            tween.TweenProperty(_sprite, "modulate", new Color(1f, 0.35f, 0.35f), 0.05f);
            tween.TweenProperty(_sprite, "modulate", _tint, 0.2f);
        }
        else
        {
            tween.TweenProperty(_material!, "albedo_color", Colors.White, 0.05f);
            tween.TweenProperty(_material!, "albedo_color", _material!.AlbedoColor, 0.15f);
        }
        var start = Position;
        tween.Parallel().TweenProperty(this, "position", start + new Vector3(0.06f, 0f, 0f), 0.04f);
        tween.TweenProperty(this, "position", start, 0.06f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Dodge(Vector3 from)
    {
        var start = Position;
        var away = ((Position - from) with { Y = 0f }).Normalized().Cross(Vector3.Up) * 0.3f;
        var tween = CreateTween();
        tween.TweenProperty(this, "position", start + away, 0.08f);
        tween.TweenProperty(this, "position", start, 0.12f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Die()
    {
        var tween = CreateTween();
        if (_sprite is not null)
        {
            PlayAnim("hit");
            _sprite.AlphaCut = SpriteBase3D.AlphaCutMode.Disabled; // discard can't fade
            tween.TweenProperty(_sprite, "modulate", _tint with { A = 0f }, 0.5f);
        }
        else
        {
            _material!.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
            tween.TweenProperty(_material, "albedo_color", _material.AlbedoColor with { A = 0f }, 0.5f);
        }
        tween.Parallel().TweenProperty(this, "position", Position + new Vector3(0f, -0.4f, 0f), 0.5f);
        tween.Parallel().TweenProperty(_label, "modulate", _label.Modulate with { A = 0f }, 0.3f);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }
}
