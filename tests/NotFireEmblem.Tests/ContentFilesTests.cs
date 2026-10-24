using System.Text.RegularExpressions;
using NotFireEmblem.Campaign;
using NotFireEmblem.Dialogue;

namespace NotFireEmblem.Tests;

/// <summary>
/// Checks the real text content in data/ without Godot: every dialogue script parses, and every scene that the
/// campaign or a map trigger refers to exists. (The .tres content is validated at boot by ContentDatabase.Validate.)
/// </summary>
public class ContentFilesTests
{
    private static string DataDir()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !Directory.Exists(Path.Combine(dir.FullName, "data", "dialogue")))
            dir = dir.Parent;
        return Path.Combine(dir?.FullName ?? throw new DirectoryNotFoundException("data/ not found"), "data");
    }

    private static Dictionary<string, DialogueScene> Scenes() =>
        Directory.GetFiles(Path.Combine(DataDir(), "dialogue"), "*.txt")
            .SelectMany(f => DialogueParser.Parse(File.ReadAllText(f), f))
            .ToDictionary(s => s.Id);

    [Fact]
    public void DialogueScripts_ParseWithUniqueIds_AndShortLines()
    {
        var all = Directory.GetFiles(Path.Combine(DataDir(), "dialogue"), "*.txt")
            .SelectMany(f => DialogueParser.Parse(File.ReadAllText(f), f)).ToList();
        Assert.NotEmpty(all);
        Assert.Equal(all.Count, all.Select(s => s.Id).Distinct().Count());
        foreach (var line in all.SelectMany(s => s.Lines).OfType<SpeakLine>())
            Assert.True(line.Text.Length <= 120, $"Line too long ({line.Text.Length}): {line.Text}");
    }

    [Fact]
    public void Campaign_ParsesAndItsScenesExist()
    {
        var campaign = CampaignParser.Parse(File.ReadAllText(Path.Combine(DataDir(), "campaign", "main.txt")));
        var scenes = Scenes();
        foreach (var step in campaign.Steps)
        {
            if (step is SceneStep s)
                Assert.True(scenes.ContainsKey(s.SceneId), $"Campaign scene '{s.SceneId}' is missing.");
            if (step is HubStep { BriefingSceneId: { } b })
                Assert.True(scenes.ContainsKey(b), $"Briefing scene '{b}' is missing.");
            if (step is SideStep side)
            {
                Assert.True(File.Exists(Path.Combine(DataDir(), "maps", $"{side.MapId}.tres")), $"Side map '{side.MapId}' is missing.");
                Assert.True(scenes.ContainsKey($"{side.MapId}_pre"), $"Side map '{side.MapId}' has no _pre scene.");
            }
        }
        Assert.IsType<EndStep>(campaign.Steps[^1]);
    }

    [Fact]
    public void MapTriggers_OnlyPlayScenesThatExist()
    {
        var scenes = Scenes();
        foreach (var map in Directory.GetFiles(Path.Combine(DataDir(), "maps"), "*.tres"))
            foreach (Match m in Regex.Matches(File.ReadAllText(map), @"scene ([a-z0-9_]+)"))
                Assert.True(scenes.ContainsKey(m.Groups[1].Value), $"{Path.GetFileName(map)} plays missing scene '{m.Groups[1].Value}'.");
    }
}
