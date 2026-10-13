using Godot;

namespace NotFireEmblem.Grid;

/// <summary>The bobbing square that marks the selected cell. Purely visual; input lives in PlayerInputController.</summary>
public partial class GridCursor : Node3D
{
    [Export] public Color Color { get; set; } = new(1f, 0.85f, 0.2f);
    [Export] public float SlideTime { get; set; } = 0.08f;

    private BattleGrid? _grid;
    private Node3D _frame = null!;
    private Tween? _tween;
    private float _time;

    public GridPos Pos { get; private set; }

    public override void _Ready()
    {
        _frame = new Node3D();
        AddChild(_frame);
        var material = new StandardMaterial3D { AlbedoColor = Color, ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded };
        const float size = 0.96f, thickness = 0.07f;
        for (int i = 0; i < 4; i++)
        {
            bool horizontal = i < 2;
            float offset = (i % 2 == 0 ? -1 : 1) * (size - thickness) / 2f;
            _frame.AddChild(new MeshInstance3D
            {
                Mesh = new BoxMesh { Size = horizontal ? new Vector3(size, 0.04f, thickness) : new Vector3(thickness, 0.04f, size) },
                MaterialOverride = material,
                Position = horizontal ? new Vector3(0f, 0f, offset) : new Vector3(offset, 0f, 0f),
                CastShadow = GeometryInstance3D.ShadowCastingSetting.Off,
            });
        }
    }

    public void Init(BattleGrid grid) => _grid = grid;

    public override void _Process(double delta)
    {
        _time += (float)delta;
        _frame.Position = new Vector3(0f, 0.05f + 0.03f * Mathf.Sin(_time * 4f), 0f);
    }

    public void MoveTo(GridPos p, bool instant = false)
    {
        if (_grid is null || !_grid.InBounds(p))
            return;
        Pos = p;
        var target = GridMetrics.CellCenter(_grid, p);
        _tween?.Kill();
        if (instant)
        {
            Position = target;
            return;
        }
        _tween = CreateTween();
        _tween.TweenProperty(this, "position", target, SlideTime).SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
    }
}
