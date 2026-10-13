using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace NotFireEmblem.Units;

/// <summary>
/// Placeholder unit visual: a capsule in the faction colour with a facing cone and a floating name/HP label.
/// Driven only by BattleEventPlayer and PlayerInputController previews; it never changes game state.
/// </summary>
public partial class UnitView : Node3D
{
    public static readonly Color PlayerColor = new(0.25f, 0.5f, 1f);
    public static readonly Color EnemyColor = new(0.9f, 0.25f, 0.2f);
    public static readonly Color AllyColor = new(0.3f, 0.8f, 0.35f);

    private Node3D _model = null!;
    private StandardMaterial3D _material = null!;
    private Label3D _label = null!;
    private Color _baseColor;
    private string _name = "";
    private string _title = "";

    public string UnitId { get; private set; } = "";

    public static Color ColorFor(Faction faction) => faction switch
    {
        Faction.Player => PlayerColor,
        Faction.Enemy => EnemyColor,
        _ => AllyColor,
    };

    public void Setup(string unitId, string name, string title, Faction faction, bool isBoss, int hp, int maxHp)
    {
        UnitId = unitId;
        _name = name;
        _title = title;
        _baseColor = ColorFor(faction);
        Name = $"Unit_{unitId}";

        _model = new Node3D();
        AddChild(_model);
        _material = new StandardMaterial3D { AlbedoColor = _baseColor, Roughness = 0.6f };
        float scale = isBoss ? 1.15f : 1f;
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
        if (isBoss)
        {
            _model.AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.08f, OuterRadius = 0.16f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.8f, 0.15f), Metallic = 0.6f },
                Position = new Vector3(0f, 1.15f, 0f),
            });
        }

        _label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = 40,
            OutlineSize = 10,
            PixelSize = 0.006f,
            Position = new Vector3(0f, isBoss ? 1.5f : 1.3f, 0f),
            RenderPriority = 1,
            OutlineRenderPriority = 0,
        };
        AddChild(_label);
        SetHp(hp, maxHp);
    }

    public void SetHp(int hp, int maxHp)
    {
        float ratio = maxHp > 0 ? hp / (float)maxHp : 0f;
        _label.Text = $"{_name}\n{hp}/{maxHp}";
        _label.Modulate = ratio > 0.5f ? Colors.White : ratio > 0.25f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
    }

    public void SetDone(bool done) => _material.AlbedoColor = done ? _baseColor.Darkened(0.6f) : _baseColor;

    public void FaceToward(Vector3 worldPoint)
    {
        var flat = new Vector3(worldPoint.X, GlobalPosition.Y, worldPoint.Z);
        if (flat.DistanceSquaredTo(GlobalPosition) > 0.0001f)
            _model.LookAt(flat, Vector3.Up);
    }

    public async Task MoveAlong(IReadOnlyList<Vector3> points, float secondsPerStep = 0.09f)
    {
        if (points.Count < 2)
            return;
        var tween = CreateTween();
        for (int i = 1; i < points.Count; i++)
        {
            var target = points[i];
            tween.TweenCallback(Callable.From(() => FaceToward(target)));
            tween.TweenProperty(this, "position", target, secondsPerStep);
        }
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    public async Task Lunge(Vector3 toward)
    {
        FaceToward(toward);
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
        tween.TweenProperty(_material, "albedo_color", Colors.White, 0.05f);
        tween.TweenProperty(_material, "albedo_color", _material.AlbedoColor, 0.15f);
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
        _material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        var tween = CreateTween();
        tween.TweenProperty(_material, "albedo_color", _material.AlbedoColor with { A = 0f }, 0.5f);
        tween.Parallel().TweenProperty(this, "position", Position + new Vector3(0f, -0.4f, 0f), 0.5f);
        tween.Parallel().TweenProperty(_label, "modulate", _label.Modulate with { A = 0f }, 0.3f);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
    }
}
