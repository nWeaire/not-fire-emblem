namespace NotFireEmblem.Core;

/// <summary>
/// Casual (the default): fallen units retreat and return after the battle. Iron Man: allies who fall in combat are lost
/// for good. (Iron Man was "Classic" until session 13; saves store the number, so the values must not change.)
/// </summary>
public enum GameMode { IronMan = 0, Casual = 1 }

public static class GameModes
{
    public static string Name(this GameMode mode) => mode == GameMode.IronMan ? "Iron Man" : "Casual";
}

/// <summary>Hard raises enemy stats, uses aggressive AI weights and adds Hard-only reinforcements.</summary>
public enum Difficulty { Normal, Hard }
