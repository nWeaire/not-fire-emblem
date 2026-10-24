using System;
using System.Collections.Generic;
using System.Linq;
using NotFireEmblem.Core;

namespace NotFireEmblem.Campaign;

public abstract record CampaignStep;
/// <summary>Sets the story chapter (shops open by chapter) and its display title.</summary>
public sealed record ChapterStep(int Number, string Title) : CampaignStep;
public sealed record SceneStep(string SceneId) : CampaignStep;
/// <summary>
/// A battle. <see cref="Deploy"/> is how many roster units the player may bring (0 = only the map's fixed units);
/// <see cref="Forced"/> must be deployed.
/// </summary>
public sealed record BattleStep(string MapId, int Deploy, IReadOnlyList<string> Forced) : CampaignStep;
public sealed record JoinStep(string UnitId) : CampaignStep;
public sealed record LeaveStep(string UnitId) : CampaignStep;
public sealed record ClassStep(string UnitId, string ClassId) : CampaignStep;
public sealed record CrestStep(int Tier) : CampaignStep;
public sealed record GoldStep(int Amount) : CampaignStep;
public sealed record ItemStep(string ItemId) : CampaignStep;
public sealed record FlagStep(string Flag) : CampaignStep;
/// <summary>Unlocks a hub feature ("smithy", "promotion", "supports", "shop").</summary>
public sealed record UnlockStep(string Feature) : CampaignStep;
/// <summary>
/// An optional side map (session 14: the Old Warden Vault). From this step on, the hub offers it until the player plays
/// and wins it, or until the campaign reaches chapter <see cref="Until"/> (0 = no limit). Its scenes are
/// "&lt;map&gt;_pre" (before the prep screen) and "&lt;map&gt;_post" (after the results), when they exist. Playing it
/// doesn't advance the campaign: the hub comes back afterwards.
/// </summary>
public sealed record SideStep(string MapId, int Deploy, IReadOnlyList<string> Forced, int Until, string Blurb) : CampaignStep
{
    public BattleStep Battle => new(MapId, Deploy, Forced);
}
/// <summary>Opens the hub. The briefing scene plays when the player chooses to march on.</summary>
public sealed record HubStep(string? BriefingSceneId) : CampaignStep;
/// <summary>The end of the content built so far.</summary>
public sealed record EndStep(string Text) : CampaignStep;

public sealed record CampaignDef(IReadOnlyList<CampaignStep> Steps)
{
    public IEnumerable<string> Validate(ContentDatabase db)
    {
        var errors = new List<string>();
        void Unit(string id, int i)
        {
            if (!db.Units.ContainsKey(id))
                errors.Add($"Campaign step {i}: unknown unit '{id}'.");
        }
        for (int i = 0; i < Steps.Count; i++)
        {
            switch (Steps[i])
            {
                case SceneStep s when !db.Scenes.ContainsKey(s.SceneId):
                    errors.Add($"Campaign step {i}: unknown scene '{s.SceneId}'."); break;
                case HubStep { BriefingSceneId: { } b } when !db.Scenes.ContainsKey(b):
                    errors.Add($"Campaign step {i}: unknown briefing scene '{b}'."); break;
                case SideStep side:
                    if (!db.Maps.ContainsKey(side.MapId))
                        errors.Add($"Campaign step {i}: unknown side map '{side.MapId}'.");
                    foreach (var f in side.Forced) Unit(f, i);
                    break;
                case BattleStep b:
                    if (!db.Maps.ContainsKey(b.MapId))
                        errors.Add($"Campaign step {i}: unknown map '{b.MapId}'.");
                    foreach (var f in b.Forced) Unit(f, i);
                    break;
                case JoinStep j: Unit(j.UnitId, i); break;
                case LeaveStep l: Unit(l.UnitId, i); break;
                case ClassStep c:
                    Unit(c.UnitId, i);
                    if (!db.Classes.ContainsKey(c.ClassId))
                        errors.Add($"Campaign step {i}: unknown class '{c.ClassId}'.");
                    break;
                case ItemStep it when !db.IsItem(it.ItemId):
                    errors.Add($"Campaign step {i}: unknown item '{it.ItemId}'."); break;
            }
        }
        return errors;
    }
}

/// <summary>
/// Parses the campaign script (data/campaign/main.txt), one step per line; '#' starts a comment.
/// <code>
/// chapter 1 Chapter 1: Brookhollow Burns
/// scene ch01_pre
/// battle ch1 [deploy 5] [force rowan,aldric]
/// join kai · leave aldric · class rowan lord · crest 1 · gold 500 · item vulnerary · flag name
/// unlock smithy · hub [briefing_scene] · end To be continued...
/// side vault [deploy 8] [force rowan,aldric] [until 15] : what the hub says about it
/// </code>
/// </summary>
public static class CampaignParser
{
    public static CampaignDef Parse(string text)
    {
        var steps = new List<CampaignStep>();
        int lineNo = 0;
        foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
        {
            lineNo++;
            int hash = raw.IndexOf('#');
            string line = (hash >= 0 ? raw[..hash] : raw).Trim();
            if (line.Length == 0)
                continue;
            var t = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string Arg(int i) => i < t.Length ? t[i] : throw new FormatException($"campaign:{lineNo}: missing argument in '{line}'.");
            int Int(int i) => int.TryParse(Arg(i), out int v) ? v : throw new FormatException($"campaign:{lineNo}: expected a number in '{line}'.");
            string Rest(int from) => string.Join(' ', t.Skip(from));
            steps.Add(t[0] switch
            {
                "chapter" => new ChapterStep(Int(1), Rest(2)),
                "scene" => new SceneStep(Arg(1)),
                "battle" => ParseBattle(t, lineNo),
                "side" => ParseSide(line, lineNo),
                "join" => new JoinStep(Arg(1)),
                "leave" => new LeaveStep(Arg(1)),
                "class" => new ClassStep(Arg(1), Arg(2)),
                "crest" => new CrestStep(Int(1)),
                "gold" => new GoldStep(Int(1)),
                "item" => new ItemStep(Arg(1)),
                "flag" => new FlagStep(Arg(1)),
                "unlock" => new UnlockStep(Arg(1)),
                "hub" => new HubStep(t.Length > 1 ? t[1] : null),
                "end" => new EndStep(t.Length > 1 ? Rest(1) : "To be continued."),
                _ => throw new FormatException($"campaign:{lineNo}: unknown step '{t[0]}'."),
            });
        }
        return new CampaignDef(steps);
    }

    private static SideStep ParseSide(string line, int lineNo)
    {
        var parts = line.Split(" : ", 2);
        var t = parts[0].Split(' ', StringSplitOptions.RemoveEmptyEntries);
        int until = 0;
        var battleTokens = new List<string>();
        for (int i = 0; i < t.Length; i++)
        {
            if (t[i] == "until" && i + 1 < t.Length)
                until = int.TryParse(t[++i], out int v) ? v : throw new FormatException($"campaign:{lineNo}: expected a chapter after 'until'.");
            else
                battleTokens.Add(t[i]);
        }
        var battle = ParseBattle(battleTokens.ToArray(), lineNo);
        return new SideStep(battle.MapId, battle.Deploy, battle.Forced, until, parts.Length > 1 ? parts[1].Trim() : "");
    }

    private static BattleStep ParseBattle(string[] t, int lineNo)
    {
        if (t.Length < 2)
            throw new FormatException($"campaign:{lineNo}: battle needs a map id.");
        int deploy = 0;
        var forced = new List<string>();
        for (int i = 2; i + 1 < t.Length; i += 2)
        {
            switch (t[i])
            {
                case "deploy": deploy = int.Parse(t[i + 1]); break;
                case "force": forced.AddRange(t[i + 1].Split(',', StringSplitOptions.RemoveEmptyEntries)); break;
                default: throw new FormatException($"campaign:{lineNo}: unknown battle option '{t[i]}'.");
            }
        }
        return new BattleStep(t[1], deploy, forced);
    }
}
