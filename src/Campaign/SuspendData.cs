using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NotFireEmblem.Battle;

namespace NotFireEmblem.Campaign;

/// <summary>
/// A suspended campaign battle: the session as it was when the battle began (battles work on copies of the roster,
/// so the session itself is unchanged), the battle's seed and deployment, and every command submitted so far.
/// Resuming rebuilds the battle and replays the commands (<see cref="TurnManager.Replay"/>). There is one suspend at
/// a time; it is cleared when that battle ends, restarts or is quit.
/// </summary>
public sealed record SuspendData(
    int Version,
    string Label,
    SaveData Session,
    ulong Seed,
    List<string> Deployed,
    List<IBattleCommand> Commands,
    string? SideMap = null)   // the side map being played (SideStep), or null for the campaign's own battle
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static SuspendData FromBattle(GameSession session, BattleLaunch launch, BattleState state, string? sideMap = null)
    {
        string label = sideMap is null ? $"{session.ChapterTitle} · Turn {state.Turn}" : $"{state.Map.Name} · Turn {state.Turn}";
        return new SuspendData(CurrentVersion, label, SaveData.FromSession(session, label), launch.Seed,
            launch.Deployed.ToList(), state.CommandLog.ToList(), sideMap);
    }

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static SuspendData FromJson(string json) =>
        JsonSerializer.Deserialize<SuspendData>(json, Json) ?? throw new JsonException("Empty suspend file.");
}
