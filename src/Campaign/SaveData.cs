using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Campaign;

/// <summary>A saved roster unit. Content is referenced by id only (CLAUDE.md conventions).</summary>
public sealed record UnitSave(
    string UnitId,
    string ClassId,
    int Level,
    int Exp,
    int[] Gains,
    List<string> Inventory,
    int EquippedIndex,
    string? ArmorId,
    string? AccessoryId,
    UnitStatus Status);

/// <summary>Versioned save file contents (IMPLEMENTATION §5.1).</summary>
public sealed record SaveData(
    int Version,
    string Label,
    GameMode Mode,
    Difficulty Difficulty,
    int StepIndex,
    int Chapter,
    string ChapterTitle,
    int BattleCount,
    ulong Seed,
    List<UnitSave> Roster,
    List<string> Convoy,
    int Gold,
    int CrestTier,
    List<string> Flags,
    List<string> Unlocks,
    Dictionary<string, int> SupportPoints,
    Dictionary<string, int> SupportRanks,
    List<string> SeenScenes)
{
    public const int CurrentVersion = 1;

    private static readonly JsonSerializerOptions Json = new() { WriteIndented = true };

    public static SaveData FromSession(GameSession s, string label) => new(
        CurrentVersion,
        label,
        s.Mode,
        s.Difficulty,
        s.StepIndex,
        s.Chapter,
        s.ChapterTitle,
        s.BattleCount,
        s.Seed,
        s.Roster.Select(u => new UnitSave(u.Def.Id, u.ClassId, u.Level, u.Exp, u.Gains.ToArray(), u.Inventory.ToList(),
            u.EquippedIndex, u.ArmorId, u.AccessoryId, u.Status)).ToList(),
        s.Convoy.ToList(),
        s.Gold,
        s.CrestTier,
        s.Flags.OrderBy(f => f).ToList(),
        s.Unlocks.OrderBy(f => f).ToList(),
        new Dictionary<string, int>(s.SupportPoints),
        new Dictionary<string, int>(s.SupportRanks),
        s.SeenScenes.OrderBy(f => f).ToList());

    /// <summary>Rebuilds a session. Units whose definitions no longer exist are dropped.</summary>
    public GameSession ToSession(ContentDatabase db)
    {
        var s = new GameSession
        {
            Mode = Mode, Difficulty = Difficulty, StepIndex = StepIndex, Chapter = Chapter, ChapterTitle = ChapterTitle,
            BattleCount = BattleCount, Seed = Seed, Gold = Gold, CrestTier = CrestTier,
        };
        foreach (var u in Roster.Where(u => db.Units.ContainsKey(u.UnitId)))
        {
            var state = new UnitState
            {
                Def = db.Unit(u.UnitId), ClassId = db.Classes.ContainsKey(u.ClassId) ? u.ClassId : db.Unit(u.UnitId).ClassId,
                Level = u.Level, Exp = u.Exp, Gains = StatBlock.FromArray(u.Gains), EquippedIndex = u.EquippedIndex,
                ArmorId = u.ArmorId, AccessoryId = u.AccessoryId, Status = u.Status,
            };
            state.Inventory.AddRange(u.Inventory.Select(Renamed).Where(db.IsInventoryItem));
            s.Roster.Add(state);
        }
        s.Convoy.AddRange(Convoy.Select(Renamed).Where(db.IsItem));
        s.Flags.UnionWith(Flags);
        s.Unlocks.UnionWith(Unlocks);
        foreach (var (k, v) in SupportPoints) s.SupportPoints[k] = v;
        foreach (var (k, v) in SupportRanks) s.SupportRanks[k] = v;
        s.SeenScenes.UnionWith(SeenScenes);
        return s;
    }

    /// <summary>Items whose ids changed since older saves were written (session 14: the Killing Edge became the Silver Sword).</summary>
    private static readonly Dictionary<string, string> RenamedItems = new() { ["killing_edge"] = "silver_sword" };

    private static string Renamed(string id) => RenamedItems.GetValueOrDefault(id, id);

    public string ToJson() => JsonSerializer.Serialize(this, Json);

    public static SaveData FromJson(string json) =>
        JsonSerializer.Deserialize<SaveData>(json, Json) ?? throw new JsonException("Empty save file.");
}
