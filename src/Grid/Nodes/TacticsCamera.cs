using Godot;

namespace NotFireEmblem.Grid;

/// <summary>
/// A pivot at the focus point with a tilted child camera. Rotates in 90° steps, zooms, and pans smoothly
/// toward its target. Also maps screen directions to grid directions for the current rotation.
/// </summary>
public partial class TacticsCamera : Node3D
{
    [Export] public float PitchDegrees { get; set; } = 52f;
    [Export] public float Distance { get; set; } = 13f;
    [Export] public float MinDistance { get; set; } = 6f;
    [Export] public float MaxDistance { get; set; } = 26f;
    [Export] public float ZoomStep { get; set; } = 1.5f;
    [Export] public float FollowSharpness { get; set; } = 8f;

    private Vector3 _target;
    private float _yaw;
    private float _distance;
    private int _yawSteps; // unbounded, so the tween never wraps the long way round

    public Camera3D Camera { get; private set; } = null!;

    /// <summary>Current rotation in quarter turns, 0–3.</summary>
    public int YawStep => ((_yawSteps % 4) + 4) % 4;

    public override void _Ready()
    {
        Camera = new Camera3D { Fov = 45f };
        AddChild(Camera);
        Camera.MakeCurrent();
        _distance = Distance;
        _target = Position;
        ApplyTransform();
    }

    public override void _Process(double delta)
    {
        float t = 1f - Mathf.Exp(-FollowSharpness * (float)delta);
        Position = Position.Lerp(_target, t);
        _yaw = Mathf.Lerp(_yaw, _yawSteps * Mathf.Pi / 2f, t);
        _distance = Mathf.Lerp(_distance, Distance, t);
        ApplyTransform();
    }

    /// <summary>Orthographic sizes (cells shown vertically) that keep pixels whole on a 720 px tall view: 16 px tiles at 5x, 4x, 3x, 2x.</summary>
    private static readonly float[] FlatSizes = { 9f, 11.25f, 15f, 22.5f };
    private int _flatZoom = 1;

    private void ApplyTransform()
    {
        if (GridMetrics.Flat)
        {
            // Straight down, orthographic, no rotation: the map reads like a SNES tactics screen.
            Rotation = Vector3.Zero;
            Camera.Projection = Camera3D.ProjectionType.Orthogonal;
            Camera.Size = FlatSizes[_flatZoom];
            Camera.Position = new Vector3(0f, 30f, 0f);
            Camera.Rotation = new Vector3(-Mathf.Pi / 2f, 0f, 0f);
            return;
        }
        Rotation = new Vector3(0f, _yaw, 0f);
        float pitch = Mathf.DegToRad(PitchDegrees);
        Camera.Position = new Vector3(0f, _distance * Mathf.Sin(pitch), _distance * Mathf.Cos(pitch));
        Camera.Rotation = new Vector3(-pitch, 0f, 0f);
    }

    public void Focus(Vector3 worldPoint, bool instant = false)
    {
        _target = worldPoint;
        if (instant)
        {
            Position = worldPoint;
            ApplyTransform();
        }
    }

    public void RotateStep(int direction)
    {
        if (!GridMetrics.Flat)
            _yawSteps += direction;
    }

    public void Zoom(int direction)
    {
        if (GridMetrics.Flat)
            _flatZoom = Mathf.Clamp(_flatZoom + direction, 0, FlatSizes.Length - 1);
        else
            Distance = Mathf.Clamp(Distance + direction * ZoomStep, MinDistance, MaxDistance);
    }

    /// <summary>Maps a screen direction (Up = away from the camera) to a grid direction for the current yaw.</summary>
    public GridPos ScreenToGrid(GridPos screenDir)
    {
        float a = YawStep * Mathf.Pi / 2f;
        var forward = new Vector3(-Mathf.Sin(a), 0f, -Mathf.Cos(a));
        var right = new Vector3(Mathf.Cos(a), 0f, -Mathf.Sin(a));
        var world = right * screenDir.X + forward * -screenDir.Y;
        return new GridPos(Mathf.RoundToInt(world.X), Mathf.RoundToInt(world.Z));
    }
}
