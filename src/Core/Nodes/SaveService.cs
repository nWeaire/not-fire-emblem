using System.Collections.Generic;
using Godot;
using NotFireEmblem.Campaign;

namespace NotFireEmblem.Core;

/// <summary>Writes and reads save slots as JSON in user://saves/ (IMPLEMENTATION §5.1). Slot 0 is the autosave.</summary>
public static class SaveService
{
    public const int SlotCount = 4;
    public const string Dir = "user://saves";

    public static string SlotPath(int slot) => $"{Dir}/slot{slot}.json";

    public static string SlotName(int slot) => slot == 0 ? "Autosave" : $"Slot {slot}";

    public static string? Save(int slot, GameSession session)
    {
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

    public static bool AnySave()
    {
        for (int i = 0; i < SlotCount; i++)
            if (FileAccess.FileExists(SlotPath(i)))
                return true;
        return false;
    }
}
