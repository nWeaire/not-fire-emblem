using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NotFireEmblem.Grid;

namespace NotFireEmblem.Dialogue;

/// <summary>One step of a dialogue scene: a spoken line or a command (GDD §5, DIALOGUE.md §1).</summary>
public abstract record DialogueLine;

/// <summary>
/// A spoken line. <see cref="Speaker"/> is a unit id in lower case, "narrator", or "???" (name hidden).
/// <see cref="StandIns"/> say it instead, first one present, when the speaker isn't there (script <c>DORRAN|ALDRIC:</c>,
/// session 18: a companion who fell in Iron Man, or a missed recruit).
/// </summary>
public sealed record SpeakLine(string Speaker, string Expression, string Text, IReadOnlyList<string>? StandIns = null) : DialogueLine
{
    public bool IsNarrator => Speaker == "narrator";

    /// <summary>This line as the first of its speaker and stand-ins who is here, or null when none is.</summary>
    public SpeakLine? SaidBy(Func<string, bool> here)
    {
        if (here(Speaker))
            return this;
        string? standIn = StandIns?.FirstOrDefault(here);
        return standIn is null ? null : this with { Speaker = standIn, StandIns = null };
    }
}

public sealed record WaitLine(double Seconds) : DialogueLine;

/// <summary>Battle scenes: move the camera to a cell.</summary>
public sealed record FocusLine(GridPos Cell) : DialogueLine;

/// <summary>Sets a story flag when the line is reached.</summary>
public sealed record FlagLine(string Flag) : DialogueLine;

/// <summary>Story scenes: change the background image.</summary>
public sealed record BackgroundLine(string Id) : DialogueLine;

public sealed record MusicLine(string Id) : DialogueLine;

public sealed record SoundLine(string Id) : DialogueLine;

public sealed record DialogueScene(string Id, IReadOnlyList<DialogueLine> Lines)
{
    /// <summary>The story flags this scene sets.</summary>
    public IEnumerable<string> Flags => Lines.OfType<FlagLine>().Select(f => f.Flag);

    /// <summary>The unit ids that speak in this scene, stand-ins included (no narrator or hidden speakers).</summary>
    public IEnumerable<string> Speakers =>
        Lines.OfType<SpeakLine>().SelectMany(l => l.StandIns is null ? new[] { l.Speaker } : l.StandIns.Prepend(l.Speaker))
            .Where(s => s is not ("narrator" or "???")).Distinct();

    /// <summary>
    /// The scene without <paramref name="absent"/> speakers (recruits the player missed, companions who fell): their
    /// lines go to a stand-in if one is named and here, else are dropped.
    /// </summary>
    public DialogueScene Without(IReadOnlySet<string> absent) => Keep(id => !absent.Contains(id));

    /// <summary>
    /// The scene for whoever is <paramref name="present"/> (battle scenes, written for whoever was deployed, like the
    /// crest blessings); the narrator and hidden speakers always stay.
    /// </summary>
    public DialogueScene OnlySpeakers(IReadOnlySet<string> present) =>
        Keep(id => id is "narrator" or "???" || present.Contains(id));

    private DialogueScene Keep(Func<string, bool> here) =>
        this with { Lines = Lines.Select(l => l is SpeakLine s ? s.SaidBy(here) : l).OfType<DialogueLine>().ToList() };
}

/// <summary>
/// Parses the DIALOGUE.md script format:
/// <code>
/// ### scene_id
/// @bg farm_evening        ; comment
/// ROWAN [smile]: Spoken line.
/// NARRATOR: Narration.
/// @wait 0.5 / @focus 7,4 / @flag name / @music id / @sfx id
/// </code>
/// Anything before the first "###", blank lines, "---" rules and "## " headings are ignored.
/// </summary>
public static class DialogueParser
{
    public static List<DialogueScene> Parse(string text, string source = "")
    {
        var scenes = new List<DialogueScene>();
        string? id = null;
        var lines = new List<DialogueLine>();
        int lineNo = 0;

        void Flush()
        {
            if (id is not null)
                scenes.Add(new DialogueScene(id, lines.ToArray()));
            lines.Clear();
        }

        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            lineNo++;
            string line = raw.Trim();
            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                Flush();
                // "### pro_binder_battle (boss battle quote)" -> "pro_binder_battle"
                id = line[4..].Trim().Split(' ', 2)[0];
                continue;
            }
            if (id is null || line.Length == 0 || line == "---" || line.StartsWith("## ", StringComparison.Ordinal) || line.StartsWith("#", StringComparison.Ordinal))
                continue;
            try
            {
                lines.Add(line.StartsWith('@') ? ParseCommand(line) : ParseSpeech(line));
            }
            catch (FormatException e)
            {
                throw new FormatException($"{source}:{lineNo}: {e.Message}");
            }
        }
        Flush();
        return scenes;
    }

    private static DialogueLine ParseCommand(string line)
    {
        int comment = line.IndexOf(';');
        if (comment >= 0)
            line = line[..comment].TrimEnd();
        var parts = line[1..].Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        string arg = parts.Length > 1 ? parts[1].Trim() : "";
        return parts[0] switch
        {
            "wait" => new WaitLine(double.Parse(arg, CultureInfo.InvariantCulture)),
            "focus" => new FocusLine(ParseCell(arg)),
            "flag" => new FlagLine(Require(arg, "flag")),
            "bg" => new BackgroundLine(Require(arg, "bg")),
            "music" => new MusicLine(Require(arg, "music")),
            "sfx" => new SoundLine(Require(arg, "sfx")),
            _ => throw new FormatException($"Unknown dialogue command '@{parts[0]}'."),
        };
    }

    private static DialogueLine ParseSpeech(string line)
    {
        int colon = line.IndexOf(':');
        if (colon <= 0)
            throw new FormatException($"Expected 'SPEAKER [expression]: text', got '{line}'.");
        string head = line[..colon].Trim();
        string text = line[(colon + 1)..].Trim();
        string expression = "neutral";
        int bracket = head.IndexOf('[');
        if (bracket >= 0)
        {
            int close = head.IndexOf(']', bracket);
            if (close < 0)
                throw new FormatException($"Unclosed expression in '{head}'.");
            expression = head[(bracket + 1)..close].Trim().ToLowerInvariant();
            head = head[..bracket].Trim();
        }
        // "DORRAN|ALDRIC": Dorran's line, said by Aldric if Dorran isn't there.
        var names = head.ToLowerInvariant().Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (names.Length == 0)
            throw new FormatException($"No speaker in '{line}'.");
        return new SpeakLine(names[0], expression, text, names.Length > 1 ? names[1..] : null);
    }

    public static GridPos ParseCell(string text)
    {
        var xy = text.Split(',');
        if (xy.Length != 2 || !int.TryParse(xy[0], out int x) || !int.TryParse(xy[1], out int y))
            throw new FormatException($"Expected a cell 'x,y', got '{text}'.");
        return new GridPos(x, y);
    }

    private static string Require(string arg, string command) =>
        arg.Length > 0 ? arg : throw new FormatException($"@{command} needs an argument.");
}
