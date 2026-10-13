using System.Collections.Generic;
using Godot;

namespace NotFireEmblem.Grid;

/// <summary>Coloured translucent quads drawn on top of cells (move, attack, path, danger) with a single MultiMesh.</summary>
public partial class CellOverlay : MultiMeshInstance3D
{
    public static readonly Color MoveColor = new(0.2f, 0.45f, 1f, 0.6f);
    public static readonly Color AttackColor = new(1f, 0.12f, 0.1f, 0.6f);
    public static readonly Color HealColor = new(0.3f, 0.9f, 0.4f, 0.45f);
    public static readonly Color DangerColor = new(0.6f, 0.1f, 0.9f, 0.4f);
    public static readonly Color PathColor = new(1f, 1f, 1f, 0.8f);

    /// <summary>Gap left around each quad, as a fraction of the cell.</summary>
    [Export] public float Inset { get; set; } = 0.06f;
    [Export] public float Lift { get; set; } = 0.02f;

    private BattleGrid? _grid;

    public override void _Ready()
    {
        Multimesh = new MultiMesh
        {
            TransformFormat = MultiMesh.TransformFormatEnum.Transform3D,
            UseColors = true,
            Mesh = new PlaneMesh { Size = Vector2.One * GridMetrics.CellSize * (1f - Inset) },
        };
        MaterialOverride = new StandardMaterial3D
        {
            ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded,
            Transparency = BaseMaterial3D.TransparencyEnum.Alpha,
            VertexColorUseAsAlbedo = true,
            CullMode = BaseMaterial3D.CullModeEnum.Disabled,
        };
        CastShadow = ShadowCastingSetting.Off;
    }

    public void Init(BattleGrid grid) => _grid = grid;

    public void ShowCells(IReadOnlyList<(GridPos Pos, Color Color)> cells)
    {
        if (_grid is null)
            return;
        Multimesh.InstanceCount = cells.Count;
        for (int i = 0; i < cells.Count; i++)
        {
            var pos = GridMetrics.CellCenter(_grid, cells[i].Pos) + new Vector3(0f, Lift, 0f);
            Multimesh.SetInstanceTransform(i, new Transform3D(Basis.Identity, pos));
            Multimesh.SetInstanceColor(i, cells[i].Color);
        }
    }

    public void Clear() => Multimesh.InstanceCount = 0;
}
