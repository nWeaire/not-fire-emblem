using Godot;
using NotFireEmblem.Campaign;

namespace NotFireEmblem.Core;

/// <summary>
/// Routes the campaign to the right screen (IMPLEMENTATION §6): Title → Story → Prep → Battle → Results → Hub → next.
/// Each screen calls <see cref="Advance"/> when it is done with the current step.
/// </summary>
public static class CampaignFlow
{
    private static GameSessionNode Node => GameSessionNode.Instance;

    public static void NewGame(GameMode mode, Difficulty difficulty)
    {
        var session = new GameSession { Mode = mode, Difficulty = difficulty, Seed = (ulong)GD.Randi() | ((ulong)GD.Randi() << 32) };
        Node.StartSession(session);
        Advance();
    }

    public static bool Continue(int slot)
    {
        var data = SaveService.Read(slot);
        if (data is null)
            return false;
        Node.StartSession(data.ToSession(Node.Content));
        Advance();
        return true;
    }

    /// <summary>Resumes the suspended battle: its session, then the battle rebuilt and replayed (BattleController).</summary>
    public static bool Resume()
    {
        var data = SaveService.ReadSuspend();
        if (data is null)
            return false;
        Node.StartSession(data.Session.ToSession(Node.Content));
        var runner = Node.Runner!;
        Node.PendingSide = data.SideMap is { } sideId ? runner.SideMap(sideId) : null;
        var battle = Node.PendingSide?.Battle ?? runner.Current as BattleStep;
        if (battle is null)
        {
            SaveService.ClearSuspend();
            Advance();
            return true;
        }
        Node.PendingBattleStep = battle;
        Node.PendingBattle = runner.Launch(battle, data.Deployed) with { Seed = data.Seed };
        Node.PendingReplay = data.Commands;
        Node.GoTo(GameSessionNode.BattleScene);
        return true;
    }

    /// <summary>Goes to the screen for the next step that needs the player.</summary>
    public static void Advance()
    {
        var runner = Node.Runner;
        if (runner is null)
        {
            Node.GoTo(GameSessionNode.TitleScene);
            return;
        }
        switch (runner.NextInteractive())
        {
            case SceneStep scene:
                PlayScene(scene.SceneId, StoryReturn.CampaignStep);
                break;
            case BattleStep battle:
                GoToBattle(battle);
                break;
            case HubStep hub:
                SaveService.Save(0, runner.Session);
                if (DevTools.SaveAtHub is { } at && hub.BriefingSceneId == $"hub_briefing_ch{at.Chapter:D2}")
                {
                    if (DevTools.PartyLevelFor($"ch{at.Chapter}") is { } level)
                        runner.PrepareTestParty(level);
                    GD.Print(SaveService.Save(at.Slot, runner.Session, force: true)
                             ?? $"SAVED AT HUB before Ch {at.Chapter} to {SaveService.SlotName(at.Slot)}");
                    Node.GetTree().Quit();
                    return;
                }
                Node.GoTo(GameSessionNode.HubScene);
                break;
            case EndStep end:
                Node.EndText = end.Text;
                SaveService.Save(0, runner.Session);
                PlayScene(null, StoryReturn.Title);
                break;
            default:
                Node.EndSession();
                Node.GoTo(GameSessionNode.TitleScene);
                break;
        }
    }

    /// <summary>The prep screen if the player chooses who to bring, else straight into the battle.</summary>
    private static void GoToBattle(BattleStep battle)
    {
        var runner = Node.Runner!;
        Node.PendingBattleStep = battle;
        if (runner.NeedsPrep(battle))
        {
            Node.GoTo(GameSessionNode.PrepScene);
        }
        else
        {
            Node.PendingBattle = runner.Launch(battle);
            Node.GoTo(GameSessionNode.BattleScene);
        }
    }

    // ------------------------------------------------------------------ side maps (from the hub)

    /// <summary>The hub's side map button: its "_pre" scene, then prep and the battle. The campaign step stays the hub.</summary>
    public static void StartSideMap(SideStep side)
    {
        Node.PendingSide = side;
        string pre = $"{side.MapId}_pre";
        if (Node.Content.Scenes.ContainsKey(pre))
            PlayScene(pre, StoryReturn.SideBattle);
        else
            GoToBattle(side.Battle);
    }

    /// <summary>Gives up on the side map (lost, or backed out of): back to the hub, nothing kept.</summary>
    public static void LeaveSideMap()
    {
        SaveService.ClearSuspend();
        Node.PendingSide = null;
        Node.PendingBattle = null;
        Node.PendingBattleStep = null;
        Advance();
    }

    /// <summary>The results screen's Continue: after a side map, its "_post" scene and the hub again; else the campaign.</summary>
    public static void ResultsDone()
    {
        if (Node.PendingSide is { } side)
        {
            Node.PendingSide = null;
            string post = $"{side.MapId}_post";
            if (Node.Content.Scenes.ContainsKey(post))
            {
                PlayScene(post, StoryReturn.Hub);
                return;
            }
        }
        Advance();
    }

    public static void PlayScene(string? sceneId, StoryReturn after)
    {
        Node.PendingSceneId = sceneId;
        Node.PendingSceneReturn = after;
        Node.GoTo(GameSessionNode.StoryScene);
    }

    /// <summary>Called by the Story screen when its scene ends.</summary>
    public static void SceneFinished()
    {
        var runner = Node.Runner;
        switch (Node.PendingSceneReturn)
        {
            case StoryReturn.CampaignStep:
                runner?.Complete();
                Advance();
                break;
            case StoryReturn.SideBattle:
                if (Node.PendingSceneId is { } pre)
                    runner?.SceneWatched(pre);
                if (Node.PendingSide is { } side)
                    GoToBattle(side.Battle);
                else
                    Advance();
                break;
            case StoryReturn.Briefing:
                // The hub's "march on": the briefing finishes the hub step.
                runner?.Complete();
                Advance();
                break;
            case StoryReturn.Hub:
                if (Node.PendingSupport is { } support && runner is not null)
                {
                    runner.Session.MarkSupportSeen(support.A, support.B, support.Rank);
                    Node.PendingSupport = null;
                }
                else if (Node.PendingSceneId is { } id)
                {
                    runner?.SceneWatched(id);
                }
                Node.GoTo(GameSessionNode.HubScene);
                break;
            default:
                Node.EndSession();
                Node.GoTo(GameSessionNode.TitleScene);
                break;
        }
    }

    /// <summary>The player chose "march on" in the hub: play the briefing (if any), then continue.</summary>
    public static void LeaveHub()
    {
        if (Node.Runner?.Current is HubStep { BriefingSceneId: { } briefing })
        {
            PlayScene(briefing, StoryReturn.Briefing);
            return;
        }
        Node.Runner?.Complete();
        Advance();
    }

    /// <summary>After a victory: write the battle back and show the results.</summary>
    public static void BattleWon(Battle.BattleState state)
    {
        var runner = Node.Runner!;
        Node.LastReport = runner.ApplyVictory(state);
        // A side map leaves the campaign where it was (the hub); it just stops being offered.
        if (Node.PendingSide is { } side)
            runner.Session.Flags.Add(CampaignRunner.SideWonFlag(side.MapId));
        else
            runner.Complete();
        // Autosave after every battle (slot 0, separate from the manual slots), as well as on reaching the hub.
        SaveService.Save(0, runner.Session);
        Node.PendingBattle = null;
        Node.GoTo(GameSessionNode.ResultsScene);
    }
}
