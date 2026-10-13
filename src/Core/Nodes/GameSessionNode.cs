using Godot;

namespace NotFireEmblem.Core;

/// <summary>Autoload holding cross-scene state. For now: the content database. The campaign session arrives in M6.</summary>
public partial class GameSessionNode : Node
{
    private static GameSessionNode? _instance;

    public static GameSessionNode Instance => _instance ?? throw new System.InvalidOperationException("GameSessionNode autoload is missing.");

    public ContentDatabase Content { get; private set; } = new();

    public override void _EnterTree()
    {
        _instance = this;
        Content = ContentLoader.Load();
    }

    public override void _ExitTree()
    {
        if (_instance == this)
            _instance = null;
    }
}
