namespace NotFireEmblem.Units;

public enum Stat { HP, Str, Mag, Dex, Spd, Lck, Def, Res, Mov, Cha }

public enum WeaponType { Sword, Lance, Axe, Bow, Fist, BlackMagic, WhiteMagic }

public enum MovementType { Infantry, Armored, Cavalry, Flying }

/// <summary>
/// Player and Ally are one side, Enemy and Neutral the other. Neutral units (Ch 8's rioters) have their own phase and
/// only pick fights with Ally units (civilians); they still fight back when attacked.
/// </summary>
public enum Faction { Player, Enemy, Ally, Neutral }

public enum ActionState { Ready, Moved, Done }

public enum UnitStatus { Active, Dead, Retreated }

public enum AiProfile { Aggressive, Stationary, GuardRegion, Boss, TargetUnit, TargetCell, Healer }

public static class FactionExtensions
{
    public static bool IsHostileTo(this Faction a, Faction b) => a.IsPlayerSide() != b.IsPlayerSide();

    public static bool IsPlayerSide(this Faction f) => f is Faction.Player or Faction.Ally;
}
