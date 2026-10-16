using System;
using System.Linq;
using Godot;

namespace NotFireEmblem.Core;

/// <summary>
/// Main scene. Content is loaded by the GameSessionNode autoload; this hands off to the title screen, or straight
/// into a battle when started with the dev flag <c>--map=&lt;id&gt;</c>.
/// </summary>
public partial class Boot : Node
{
    [Export] public string NextScene { get; set; } = "";

    public override void _Ready()
    {
        GD.Print("The Radiant Crest: boot");
        string? map = OS.GetCmdlineUserArgs().FirstOrDefault(a => a.StartsWith("--map=", StringComparison.Ordinal))?["--map=".Length..];
        if (map is not null)
        {
            GameSessionNode.Instance.SkirmishMapId = map;
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, GameSessionNode.BattleScene);
            return;
        }
        if (!string.IsNullOrEmpty(NextScene))
            GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, NextScene);
    }
}
