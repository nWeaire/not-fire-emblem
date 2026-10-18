using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Dialogue;
using NotFireEmblem.Grid;
using NotFireEmblem.Units;

namespace NotFireEmblem.Battle;

public enum VictoryKind { Rout, DefeatBoss, Seize, Survive, Escape }

/// <summary>Defeat when <see cref="MaxLosses"/> units tagged <see cref="Tag"/> have fallen (Defend / Protect).</summary>
public sealed record ProtectRule(string Tag, int MaxLosses);

/// <summary>
/// A map's win and loss conditions (GDD §3 Map objectives). The lord falling is always a loss.
/// Unit references are keys: a spawn's Tag, else its named UnitId.
/// </summary>
public sealed record Objectives(
    VictoryKind Victory,
    string? BossKey,
    IReadOnlyList<GridPos> Cells,       // Seize: the throne/gate; Escape: the exit cells
    int Turns,                          // Survive: number of turns to hold
    IReadOnlyList<string> EscapeKeys,   // Escape: units that must leave (the lord always must)
    IReadOnlyList<string> MustSurvive,  // extra units whose death is a loss
    IReadOnlyList<ProtectRule> Protect,
    int TurnLimit)                      // 0 = none
{
    public static readonly Objectives Default = new(VictoryKind.Rout, null, Array.Empty<GridPos>(), 0, Array.Empty<string>(),
        Array.Empty<string>(), Array.Empty<ProtectRule>(), 0);
}

// ---------------------------------------------------------------- triggers

public abstract record TriggerCondition;
/// <summary>At the start of <see cref="Phase"/> on turn <see cref="Turn"/>.</summary>
public sealed record TurnStartCondition(int Turn, Faction Phase) : TriggerCondition;
public sealed record DefeatedCondition(string Key) : TriggerCondition;
/// <summary><see cref="Who"/> is a unit key or a faction name ("player", "enemy", "ally").</summary>
public sealed record EnterCondition(string Who, IReadOnlyList<GridPos> Cells) : TriggerCondition;
public sealed record AdjacentCondition(string A, string B) : TriggerCondition;
public sealed record FlagCondition(string Flag) : TriggerCondition;
public sealed record EnemiesAtMostCondition(int Count) : TriggerCondition;
public sealed record HpBelowCondition(string Key, int Percent) : TriggerCondition;
/// <summary>A Talk command: <see cref="Actor"/> (a key, or "any" player unit) talks to <see cref="Target"/>.</summary>
public sealed record TalkCondition(string Actor, string Target) : TriggerCondition;
/// <summary>A Visit command on a village cell.</summary>
public sealed record VisitCondition(GridPos Cell) : TriggerCondition;
/// <summary>The first time the unit enters combat (boss quotes play before the fight).</summary>
public sealed record CombatCondition(string Key) : TriggerCondition;

public abstract record TriggerEffect;
public sealed record SceneEffect(string SceneId) : TriggerEffect;
public sealed record SpawnEffect(string Group) : TriggerEffect;
public sealed record FlagEffect(string Flag) : TriggerEffect;
public sealed record FactionEffect(string Key, Faction Faction) : TriggerEffect;
/// <summary>Key "actor" means the unit that set the trigger off (talker, visitor, or the unit that entered).</summary>
public sealed record GiveEffect(string Key, string ItemId) : TriggerEffect;
public sealed record GoldEffect(int Amount) : TriggerEffect;
public sealed record TerrainEffect(IReadOnlyList<GridPos> Cells, char Symbol) : TriggerEffect;
public sealed record DamageEffect(string Key, int Amount) : TriggerEffect;
/// <summary>Every unit with this key leaves the map (scripted fly-bys, e.g. Ch 6's pegasus scouts).</summary>
public sealed record LeaveEffect(string Key) : TriggerEffect;
public sealed record EndEffect(BattleOutcome Outcome) : TriggerEffect;

/// <summary>
/// A point of interest the battle shows before turn 1 (ObjectiveTour): some cells, or a unit by key, and a caption.
/// </summary>
public sealed record MapHighlight(IReadOnlyList<GridPos> Cells, string? Key, string Caption);

/// <summary>A guard that holds a trigger back while it is true: "flag NAME" or "joined KEY" (KEY is on the player's side).</summary>
public sealed record TriggerGuard(string Kind, string Arg);

public sealed record TriggerDef(string Id, TriggerCondition Condition, IReadOnlyList<TriggerEffect> Effects)
{
    /// <summary>The trigger does not fire while any of these holds.</summary>
    public IReadOnlyList<TriggerGuard> Unless { get; init; } = Array.Empty<TriggerGuard>();
}

/// <summary>
/// Parses the small text language maps use for objectives and triggers, so maps stay readable text.
/// <code>
/// Victory: rout | boss KEY | seize X,Y | survive N | escape CELLS [KEY...]
/// Defeat:  die KEY | protect TAG N | turns N
/// Trigger: CONDITION [unless GUARD] => EFFECT; EFFECT; ...
///   conditions: turn N [player|enemy|ally] · defeated KEY · enter WHO CELLS · adjacent A B · flag NAME
///               enemies N (at most N left) · hp KEY PERCENT (below) · talk ACTOR TARGET · visit X,Y · combat KEY
///   guards:     flag NAME · joined KEY (KEY is on the player's side), e.g. "turn 3 enemy unless joined brann => ..."
///   effects:    scene ID · spawn GROUP · flag NAME · join KEY · faction KEY player|enemy|ally · give KEY ITEM
///               gold N · terrain CELLS SYMBOL · damage KEY N · leave KEY · win · lose
/// Highlight: CELLS => caption · KEY => caption   (shown with the objectives before turn 1)
/// CELLS: "x,y" or a rectangle "x1,y1-x2,y2", several separated by spaces.
/// </code>
/// </summary>
public static class MapScript
{
    public static Objectives ParseObjectives(string? victory, IEnumerable<string>? defeat)
    {
        var o = Objectives.Default;
        if (!string.IsNullOrWhiteSpace(victory))
        {
            var t = Tokens(victory);
            o = t[0] switch
            {
                "rout" => o,
                "boss" => o with { Victory = VictoryKind.DefeatBoss, BossKey = Arg(t, 1, victory) },
                "seize" => o with { Victory = VictoryKind.Seize, Cells = Cells(t.Skip(1), victory) },
                "survive" => o with { Victory = VictoryKind.Survive, Turns = Int(Arg(t, 1, victory)) },
                "escape" => o with
                {
                    Victory = VictoryKind.Escape,
                    Cells = Cells(t.Skip(1).Where(IsCell), victory),
                    EscapeKeys = t.Skip(1).Where(x => !IsCell(x)).ToArray(),
                },
                _ => throw new FormatException($"Unknown victory condition '{victory}'."),
            };
        }
        var mustSurvive = new List<string>();
        var protect = new List<ProtectRule>();
        int turnLimit = 0;
        foreach (var line in defeat ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var t = Tokens(line);
            switch (t[0])
            {
                case "die": mustSurvive.Add(Arg(t, 1, line)); break;
                case "protect": protect.Add(new ProtectRule(Arg(t, 1, line), Int(Arg(t, 2, line)))); break;
                case "turns": turnLimit = Int(Arg(t, 1, line)); break;
                default: throw new FormatException($"Unknown defeat condition '{line}'.");
            }
        }
        return o with { MustSurvive = mustSurvive, Protect = protect, TurnLimit = turnLimit };
    }

    public static List<TriggerDef> ParseTriggers(IEnumerable<string>? lines)
    {
        var triggers = new List<TriggerDef>();
        int index = 0;
        foreach (var line in lines ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var halves = line.Split("=>", 2);
            if (halves.Length != 2)
                throw new FormatException($"Trigger '{line}' needs 'CONDITION => EFFECTS'.");
            var parts = halves[0].Split(" unless ", StringSplitOptions.TrimEntries);
            var condition = ParseCondition(parts[0], line);
            var guards = parts.Skip(1).Select(g => ParseGuard(g, line)).ToArray();
            var effects = halves[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(e => ParseEffect(e, line)).ToArray();
            triggers.Add(new TriggerDef($"t{index++}", condition, effects) { Unless = guards });
        }
        return triggers;
    }

    /// <summary>Highlights, one per line: "CELLS => caption" or "KEY => caption".</summary>
    public static List<MapHighlight> ParseHighlights(IEnumerable<string>? lines)
    {
        var highlights = new List<MapHighlight>();
        foreach (var line in lines ?? Array.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            var halves = line.Split("=>", 2, StringSplitOptions.TrimEntries);
            if (halves.Length != 2 || halves[1].Length == 0)
                throw new FormatException($"Highlight '{line}' needs 'CELLS => caption' or 'KEY => caption'.");
            var t = Tokens(halves[0]);
            highlights.Add(t.Length > 0 && t.All(IsCell)
                ? new MapHighlight(Cells(t, line), null, halves[1])
                : new MapHighlight(Array.Empty<GridPos>(), Arg(t, 0, line), halves[1]));
        }
        return highlights;
    }

    private static TriggerGuard ParseGuard(string text, string line)
    {
        var t = Tokens(text);
        return t[0] is "flag" or "joined"
            ? new TriggerGuard(t[0], Arg(t, 1, line))
            : throw new FormatException($"Unknown trigger guard '{text}' in '{line}'.");
    }

    private static TriggerCondition ParseCondition(string text, string line)
    {
        var t = Tokens(text);
        return t[0] switch
        {
            "turn" => new TurnStartCondition(Int(Arg(t, 1, line)), t.Length > 2 ? ParseFaction(t[2], line) : Faction.Player),
            "defeated" => new DefeatedCondition(Arg(t, 1, line)),
            "enter" => new EnterCondition(Arg(t, 1, line), Cells(t.Skip(2), line)),
            "adjacent" => new AdjacentCondition(Arg(t, 1, line), Arg(t, 2, line)),
            "flag" => new FlagCondition(Arg(t, 1, line)),
            "enemies" => new EnemiesAtMostCondition(Int(Arg(t, 1, line))),
            "hp" => new HpBelowCondition(Arg(t, 1, line), Int(Arg(t, 2, line))),
            "talk" => new TalkCondition(Arg(t, 1, line), Arg(t, 2, line)),
            "visit" => new VisitCondition(DialogueParser.ParseCell(Arg(t, 1, line))),
            "combat" => new CombatCondition(Arg(t, 1, line)),
            _ => throw new FormatException($"Unknown trigger condition '{text}' in '{line}'."),
        };
    }

    private static TriggerEffect ParseEffect(string text, string line)
    {
        var t = Tokens(text);
        return t[0] switch
        {
            "scene" => new SceneEffect(Arg(t, 1, line)),
            "spawn" => new SpawnEffect(Arg(t, 1, line)),
            "flag" => new FlagEffect(Arg(t, 1, line)),
            "join" => new FactionEffect(Arg(t, 1, line), Faction.Player),
            "faction" => new FactionEffect(Arg(t, 1, line), ParseFaction(Arg(t, 2, line), line)),
            "give" => new GiveEffect(Arg(t, 1, line), Arg(t, 2, line)),
            "gold" => new GoldEffect(Int(Arg(t, 1, line))),
            "terrain" => new TerrainEffect(Cells(t.Skip(1).Take(t.Length - 2), line), Arg(t, t.Length - 1, line).Single()),
            "damage" => new DamageEffect(Arg(t, 1, line), Int(Arg(t, 2, line))),
            "leave" => new LeaveEffect(Arg(t, 1, line)),
            "win" => new EndEffect(BattleOutcome.Victory),
            "lose" => new EndEffect(BattleOutcome.Defeat),
            _ => throw new FormatException($"Unknown trigger effect '{text}' in '{line}'."),
        };
    }

    public static Faction ParseFaction(string text, string line) => text switch
    {
        "player" => Faction.Player,
        "enemy" => Faction.Enemy,
        "ally" => Faction.Ally,
        _ => throw new FormatException($"Unknown faction '{text}' in '{line}'."),
    };

    /// <summary>Cells from "x,y" and "x1,y1-x2,y2" tokens.</summary>
    public static GridPos[] Cells(IEnumerable<string> tokens, string line)
    {
        var cells = new List<GridPos>();
        foreach (var token in tokens)
        {
            var ends = token.Split('-');
            var a = DialogueParser.ParseCell(ends[0]);
            var b = ends.Length > 1 ? DialogueParser.ParseCell(ends[1]) : a;
            for (int y = Math.Min(a.Y, b.Y); y <= Math.Max(a.Y, b.Y); y++)
                for (int x = Math.Min(a.X, b.X); x <= Math.Max(a.X, b.X); x++)
                    cells.Add(new GridPos(x, y));
        }
        if (cells.Count == 0)
            throw new FormatException($"Expected cells in '{line}'.");
        return cells.Distinct().ToArray();
    }

    private static bool IsCell(string token) => token.Length > 0 && char.IsDigit(token[0]) && token.Contains(',');

    private static string[] Tokens(string text) => text.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static string Arg(string[] tokens, int i, string line) =>
        i < tokens.Length ? tokens[i] : throw new FormatException($"Missing argument {i} in '{line}'.");

    private static int Int(string text) =>
        int.TryParse(text, out int v) ? v : throw new FormatException($"Expected a number, got '{text}'.");
}
