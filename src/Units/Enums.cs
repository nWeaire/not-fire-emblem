namespace NotFireEmblem.Units;

public enum Stat { HP, Str, Mag, Dex, Spd, Lck, Def, Res, Mov, Cha }

public enum WeaponType { Sword, Lance, Axe, Bow, Fist, BlackMagic, WhiteMagic }

public enum MovementType { Infantry, Armored, Cavalry, Flying }

/// <summary>Player and Ally are friendly to each other; Enemy is hostile to both.</summary>
public enum Faction { Player, Enemy, Ally }

public enum ActionState { Ready, Moved, Done }

public enum UnitStatus { Active, Dead, Retreated }

public enum AiProfile { Aggressive, Stationary, GuardRegion, Boss, TargetUnit, TargetCell, Healer }

public static class FactionExtensions
{
    public static bool IsHostileTo(this Faction a, Faction b) => (a == Faction.Enemy) != (b == Faction.Enemy);
}
