using Godot;

namespace NotFireEmblem.Core;

/// <summary>Main scene. Loads content, then hands off to the first real scene.</summary>
public partial class Boot : Node
{
    [Export] public string NextScene { get; set; } = "";

    public override void _Ready()
    {
        GD.Print("Not Fire Emblem: boot");
        if (!string.IsNullOrEmpty(NextScene))
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, NextScene);
    }
}
