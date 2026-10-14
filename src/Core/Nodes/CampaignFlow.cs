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
                break;
            case HubStep:
                SaveService.Save(0, runner.Session);
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
        runner.Complete();
        Node.PendingBattle = null;
        Node.GoTo(GameSessionNode.ResultsScene);
    }
}
