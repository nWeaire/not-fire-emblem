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

    /// <summary>World size of one sprite pixel: the map's shared art pixel (a 48 px frame stands 1.5 cells tall).</summary>
    private const float SpritePixelSize = NotFireEmblem.Grid.MapView.ArtPixelSize;
    /// <summary>How far the sprite is pulled toward the camera so raised terrain behind it can't cut through it.</summary>
    private const float PullTowardCamera = NotFireEmblem.Grid.MapView.UnitPull;
    /// <summary>Transparent rows below the feet in PixelLab frames (a fallback when a frame can't be measured).</summary>
    private const float SpriteFootPadding = 2f;
    /// <summary>
    /// How far toward the camera (in cells) the feet stand from the cell centre. A sprite is 1.5 cells tall, so with
    /// its feet at the centre the body covers the cell behind; near the front edge it stands on its own tile.
    /// </summary>
    private const float FeetForward = 0.3f;
    /// <summary>How far behind the feet (away from the camera) the ground shadow is centred, in cells.</summary>
    private const float ShadowBehindFeet = 0.14f;

    /// <summary>Per sprite set and animation_direction: the sprite offset that puts the feet (not the frame) on the pivot.</summary>
    private static readonly Dictionary<(ulong, string), Vector2> Anchors = new();

    private Node3D _model = null!;
    private StandardMaterial3D? _material;
    private AnimatedSprite3D? _sprite;
    private MeshInstance3D? _shadow;
    private AnimatedSprite3D? _caster;
    private Label3D _label = null!;
    private Color _baseColor;
    private Color _tint = Colors.White;
    private string _name = "";
    private string _title = "";
    private string _status = "";
    private int _hp;
    private int _maxHp;
    private Vector3 _facing = Vector3.Back;
    private string _anim = "idle";
    private string _dir = "south";
    private double _bobTime;
    /// <summary>The sprite's offset at rest; the idle bob dips it one pixel from here.</summary>
    private float _restOffsetY = 24f - SpriteFootPadding;

    public string UnitId { get; private set; } = "";
    public bool IsHidden { get; private set; }

    public static Color ColorFor(Faction faction) => faction switch
    {
        Faction.Player => PlayerColor,
        Faction.Enemy => EnemyColor,
        _ => AllyColor,
    };

    /// <summary>The ring drawn round a map sprite to show its side: blue army, red enemies, green allies.</summary>
    public static Color OutlineFor(Faction faction) => faction switch
    {
        Faction.Player => new Color(0.35f, 0.6f, 1f),
        Faction.Enemy => new Color(1f, 0.28f, 0.22f),
        _ => new Color(0.35f, 0.95f, 0.4f),
    };

    /// <summary>The unit's map frames without the side outline (the outline is redrawn if the unit changes side).</summary>
    private SpriteFrames? _plainFrames;

    public void Setup(string unitId, string name, string title, Faction faction, bool isBoss, int hp, int maxHp, SpriteFrames? mapFrames, float scale = 1f)
    {
        UnitId = unitId;
        _name = name;
        _title = title;
        _baseColor = ColorFor(faction);
        Name = $"Unit_{unitId}";

        _model = new Node3D();
        AddChild(_model);
        _plainFrames = mapFrames;
        if (mapFrames is not null)
            BuildSprite(SpriteLibrary.Outlined(mapFrames, OutlineFor(faction)), scale);
        else
            BuildCapsule(scale);
        if (isBoss)
        {
            _model.AddChild(new MeshInstance3D
            {
                Mesh = new TorusMesh { InnerRadius = 0.08f, OuterRadius = 0.16f },
                MaterialOverride = new StandardMaterial3D { AlbedoColor = new Color(1f, 0.8f, 0.15f), Metallic = 0.6f },
                Position = new Vector3(0f, mapFrames is not null ? 1.75f : 1.15f, 0f),
            });
        }

        _label = new Label3D
        {
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            NoDepthTest = true,
            FontSize = NotFireEmblem.UI.UiKit.Px(40),
            OutlineSize = 10,
            PixelSize = 0.006f,
            Position = new Vector3(0f, mapFrames is not null ? (isBoss ? 1.95f : 1.8f) : 1.3f, 0f),
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
            // Pulled toward the camera (PullToCamera), a sprite's shadow would land away from its feet.
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            // Pivot at the feet, so the billboard leans back from the ground instead of sinking into it.
            Offset = new Vector2(0f, 24f - SpriteFootPadding),
        };
        _sprite.AnimationFinished += () =>
        {
            if (_anim != "idle" && _anim != "walk")
                PlayAnim("idle");
        };
        _model.AddChild(_sprite);
        // The sun's shadow comes from an invisible copy standing on the feet, not pulled toward the camera; it
        // mirrors the visible sprite's animation every frame (see _Process).
        _caster = new AnimatedSprite3D
        {
            SpriteFrames = frames,
            Billboard = BaseMaterial3D.BillboardModeEnum.Enabled,
            TextureFilter = BaseMaterial3D.TextureFilterEnum.Nearest,
            AlphaCut = SpriteBase3D.AlphaCutMode.Discard,
            Shaded = false,
            PixelSize = SpritePixelSize * scale,
            CastShadow = GeometryInstance3D.ShadowCastingSetting.ShadowsOnly,
        };
        _model.AddChild(_caster);
        // A soft contact shadow on the ground marks the unit's tile under the sun's shadow.
        _model.AddChild(_shadow = new MeshInstance3D
        {
            Mesh = new CylinderMesh { TopRadius = 0.3f, BottomRadius = 0.3f, Height = 0.01f, RadialSegments = 24 },
            MaterialOverride = new StandardMaterial3D
            {
                AlbedoColor = new Color(0f, 0f, 0f, 0.22f),
                Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
                ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            },
            Scale = new Vector3(1f, 1f, 0.7f),
            Position = new Vector3(0f, 0.012f, 0f),
            CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
        });
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
        Bob(delta);
        PullToCamera();
        string dir = DirectionFromCamera();
        if (dir != _dir)
        {
            // Keep the animation's progress when only the view direction changes.
            int frame = _sprite.Frame;
            float progress = _sprite.FrameProgress;
            _dir = dir;
            _sprite.Play($"{_anim}_{_dir}");
            _sprite.SetFrameAndProgress(frame, progress);
            ApplyAnchor();
        }
    }

    private void ApplyAnchor()
    {
        if (_sprite is null)
            return;
        var anchor = AnchorFor(_sprite.SpriteFrames, $"{_anim}_{_dir}");
        _restOffsetY = anchor.Y;
        _sprite.Offset = anchor;
    }

    private void PullToCamera()
    {
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null)
            return;
        var centre = _model.GlobalPosition;
        var towardCamera = (camera.GlobalPosition - centre) with { Y = 0f };
        var toward = towardCamera.LengthSquared() > 0.0001f ? towardCamera.Normalized() : Vector3.Zero;
        var feet = centre + toward * FeetForward;
        _sprite!.GlobalPosition = feet + (camera.GlobalPosition - feet).Normalized() * PullTowardCamera;
        // The shadow sits mostly behind the feet: centred on them, its front half showed below the feet on screen and
        // made units look as if they hovered over it.
        if (_shadow is not null)
            _shadow.GlobalPosition = feet - toward * ShadowBehindFeet + new Vector3(0f, 0.012f, 0f);
        if (_caster is not null)
        {
            _caster.GlobalPosition = feet;
            if (_caster.SpriteFrames != _sprite.SpriteFrames)
                _caster.SpriteFrames = _sprite.SpriteFrames;
            if (_caster.Animation != _sprite.Animation)
                _caster.Animation = _sprite.Animation;
            _caster.Frame = _sprite.Frame;
            _caster.Offset = _sprite.Offset;
            _caster.Visible = _sprite.Visible && _sprite.Modulate.A > 0.5f;
        }
    }

    /// <summary>
    /// The offset that stands this direction's art on the pivot: its lowest opaque row on the ground and its legs
    /// (the mean column of the bottom third of the figure) centred, whatever the art's placement in the frame.
    /// Measured once per animation and direction from its first frame: walk and attack art is drawn with the feet
    /// higher (or on a bigger canvas) than the standing frame, so one anchor for all of them made walkers float.
    /// </summary>
    private static Vector2 AnchorFor(SpriteFrames frames, string anim)
    {
        var key = (frames.GetInstanceId(), anim);
        if (Anchors.TryGetValue(key, out var cached))
            return cached;
        var fallback = new Vector2(0f, 24f - SpriteFootPadding);
        var image = frames.HasAnimation(anim) && frames.GetFrameCount(anim) > 0
            ? frames.GetFrameTexture(anim, 0)?.GetImage()
            : null;
        if (image is null || image.IsEmpty())
            return Anchors[key] = fallback;
        if (image.IsCompressed())
            image.Decompress();
        var used = image.GetUsedRect();
        if (used.Size.Y == 0)
            return Anchors[key] = fallback;
        long sum = 0;
        int count = 0;
        for (int y = used.End.Y - Mathf.Max(1, used.Size.Y / 3); y < used.End.Y; y++)
        {
            for (int x = used.Position.X; x < used.End.X; x++)
            {
                if (image.GetPixel(x, y).A > 0.5f)
                {
                    sum += x;
                    count++;
                }
            }
        }
        float legsX = count > 0 ? sum / (float)count + 0.5f : used.Position.X + used.Size.X / 2f;
        return Anchors[key] = new Vector2(Mathf.Round(image.GetWidth() / 2f - legsX), used.End.Y - image.GetHeight() / 2f);
    }

    /// <summary>
    /// GBA map sprites step up and down a pixel while they stand. Units whose idle is a single still frame (the
    /// rotation image) get that step in code; each unit's phase comes from its id so they don't bob in unison.
    /// </summary>
    private void Bob(double delta)
    {
        if (_anim != "idle" || _sprite!.SpriteFrames.GetFrameCount(_sprite.Animation) > 1)
        {
            _sprite!.Offset = _sprite.Offset with { Y = _restOffsetY };
            return;
        }
        if (_bobTime == 0)
            _bobTime = (UnitId.GetHashCode() & 0xFF) / 255.0;
        _bobTime += delta;
        // The step dips a pixel rather than lifting one, so the feet never leave the ground.
        bool down = (int)(_bobTime / 0.45) % 2 == 1;
        _sprite.Offset = _sprite.Offset with { Y = _restOffsetY - (down ? 1f : 0f) };
    }

    /// <summary>Which art direction shows the unit's facing from the current camera (south = facing the camera).</summary>
    private string DirectionFromCamera()
    {
        var camera = GetViewport()?.GetCamera3D();
        if (camera is null)
            return _dir;
        var basis = camera.GlobalBasis;
        var forward = (-basis.Z with { Y = 0f }).Normalized();
        if (forward.LengthSquared() < 0.01f)
            forward = (basis.Y with { Y = 0f }).Normalized(); // looking straight down: screen-up is "forward"
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
        ApplyAnchor();
    }

    public void SetHp(int hp, int maxHp)
    {
        _hp = hp;
        _maxHp = maxHp;
        float ratio = maxHp > 0 ? hp / (float)maxHp : 0f;
        _label.Text = $"{_name}\n{hp}/{maxHp}" + (_status.Length > 0 ? $"\n{_status}" : "");
        _label.Modulate = ratio > 0.5f ? Colors.White : ratio > 0.25f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.4f, 0.35f);
    }

    /// <summary>A short status shown under the HP (e.g. "Stunned", "Ward").</summary>
    public void SetStatus(string status)
    {
        _status = status;
        SetHp(_hp, _maxHp);
    }

    /// <summary>Fog of war: hide units the player can't see.</summary>
    public void SetHidden(bool hidden)
    {
        IsHidden = hidden;
        Visible = !hidden;
    }

    /// <summary>A unit joined another side (Talk, story events): recolour it.</summary>
    public void SetFaction(Faction faction)
    {
        _baseColor = ColorFor(faction);
        if (_material is not null)
            _material.AlbedoColor = _baseColor;
        if (_sprite is not null && _plainFrames is not null)
        {
            _sprite.SpriteFrames = SpriteLibrary.Outlined(_plainFrames, OutlineFor(faction));
            PlayAnim(_anim);
        }
    }

    /// <summary>Reinforcements fade in from above.</summary>
    public async Task Appear()
    {
        var end = Position;
        Position = end + new Vector3(0f, 1.2f, 0f);
        Scale = new Vector3(0.6f, 0.6f, 0.6f);
        var tween = CreateTween();
        tween.TweenProperty(this, "position", end, 0.35f).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.Parallel().TweenProperty(this, "scale", Vector3.One, 0.35f);
        await ToSignal(tween, Tween.SignalName.Finished);
    }

    /// <summary>The unit leaves the map alive (escaped or surrendered).</summary>
    public async Task Leave()
    {
        var tween = CreateTween();
        tween.TweenProperty(this, "scale", new Vector3(0.1f, 0.1f, 0.1f), 0.4f);
        tween.Parallel().TweenProperty(this, "position", Position + new Vector3(0f, 0.8f, 0f), 0.4f);
        await ToSignal(tween, Tween.SignalName.Finished);
        QueueFree();
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

    /// <summary>The sprite brightens to <paramref name="color"/> and settles back over <paramref name="seconds"/>.</summary>
    public async Task Glow(Color color, float seconds)
    {
        if (_sprite is null)
            return;
        var tween = CreateTween();
        tween.TweenProperty(_sprite, "modulate", color, seconds * 0.25f);
        tween.TweenProperty(_sprite, "modulate", _tint, seconds * 0.75f);
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
