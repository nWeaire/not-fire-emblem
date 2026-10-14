using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NotFireEmblem.Grid;

namespace NotFireEmblem.Dialogue;

/// <summary>One step of a dialogue scene: a spoken line or a command (GDD §5, DIALOGUE.md §1).</summary>
public abstract record DialogueLine;

/// <summary>A spoken line. <see cref="Speaker"/> is a unit id in lower case, "narrator", or "???" (name hidden).</summary>
public sealed record SpeakLine(string Speaker, string Expression, string Text) : DialogueLine
{
    public bool IsNarrator => Speaker == "narrator";
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

    /// <summary>The unit ids that speak in this scene (no narrator or hidden speakers).</summary>
    public IEnumerable<string> Speakers =>
        Lines.OfType<SpeakLine>().Select(l => l.Speaker).Where(s => s is not ("narrator" or "???")).Distinct();
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
        return new SpeakLine(head.ToLowerInvariant(), expression, text);
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
