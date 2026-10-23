using System.Collections.Generic;
using Godot;
using NotFireEmblem.Campaign;

namespace NotFireEmblem.Core;

/// <summary>
/// Writes and reads save slots as JSON in user://saves/ (IMPLEMENTATION §5.1). Slot 0 is the autosave. A suspended
/// battle has its own file beside them.
/// </summary>
public static class SaveService
{
    public const int SlotCount = 4;
    public const string Dir = "user://saves";

    public static string SlotPath(int slot) => $"{Dir}/slot{slot}.json";

    public static string SlotName(int slot) => slot == 0 ? "Autosave" : $"Slot {slot}";

    /// <param name="force">Write even under autoplay (--saveathub asked for this save).</param>
    public static string? Save(int slot, GameSession session, bool force = false)
    {
        // Autoplay test runs share the player's save folder; they must never overwrite real saves.
        if (DevTools.Autoplay && !force)
            return null;
        DirAccess.MakeDirRecursiveAbsolute(Dir);
        string label = $"{session.ChapterTitle} · {Time.GetDatetimeStringFromSystem(false, true)}";
        using var file = FileAccess.Open(SlotPath(slot), FileAccess.ModeFlags.Write);
        if (file is null)
            return $"Couldn't write {SlotPath(slot)}: {FileAccess.GetOpenError()}";
        file.StoreString(SaveData.FromSession(session, label).ToJson());
        return null;
    }

    public static SaveData? Read(int slot)
    {
        if (!FileAccess.FileExists(SlotPath(slot)))
            return null;
        try
        {
            return SaveData.FromJson(FileAccess.GetFileAsString(SlotPath(slot)));
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"Save slot {slot} is unreadable: {e.Message}");
            return null;
        }
    }

    /// <summary>(slot, label) for every slot, label null when empty.</summary>
    public static IEnumerable<(int Slot, string? Label)> List()
    {
        for (int i = 0; i < SlotCount; i++)
            yield return (i, Read(i)?.Label);
    }

    public const string SuspendPath = Dir + "/suspend.json";

    public static string? Suspend(SuspendData data)
    {
        if (DevTools.Autoplay)
            return null;
        DirAccess.MakeDirRecursiveAbsolute(Dir);
        using var file = FileAccess.Open(SuspendPath, FileAccess.ModeFlags.Write);
        if (file is null)
            return $"Couldn't write {SuspendPath}: {FileAccess.GetOpenError()}";
        file.StoreString(data.ToJson());
        return null;
    }

    public static SuspendData? ReadSuspend()
    {
        if (!FileAccess.FileExists(SuspendPath))
            return null;
        try
        {
            return SuspendData.FromJson(FileAccess.GetFileAsString(SuspendPath));
        }
        catch (System.Exception e)
        {
            GD.PushWarning($"The suspended battle is unreadable: {e.Message}");
            return null;
        }
    }

    public static void ClearSuspend()
    {
        if (!DevTools.Autoplay && FileAccess.FileExists(SuspendPath))
            DirAccess.RemoveAbsolute(SuspendPath);
    }

    public static bool AnySave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (FileAccess.FileExists(SlotPath(i)))
                return true;
        return false;
    }
}
