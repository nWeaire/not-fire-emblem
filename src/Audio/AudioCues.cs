using NotFireEmblem.Core;
using NotFireEmblem.Units;

namespace NotFireEmblem.Audio;

/// <summary>Which sound or track fits a game moment. Ids are files in assets/audio (see AudioDirector).</summary>
public static class AudioCues
{
    /// <summary>The sound of a unit's attack starting, from its equipped weapon.</summary>
    public static string Swing(ContentDatabase db, UnitState unit)
    {
        var weapon = db.TryWeapon(unit.EquippedItemId);
        return weapon?.Type switch
        {
            WeaponType.Lance => "lance_thrust",
            WeaponType.Axe => "axe_swing",
            WeaponType.Bow => "bow_shot",
            WeaponType.Fist => "claw",
            WeaponType.BlackMagic => weapon.Id == "shadow" ? "dark_cast" : "fire_cast",
            WeaponType.WhiteMagic => "heal",
            _ => "sword_swing",
        };
    }

    /// <summary>The sound when a strike lands (or doesn't).</summary>
    public static string Impact(bool hit, bool crit) => !hit ? "miss" : crit ? "hit_heavy" : "hit";

    /// <summary>Battle music for a phase. Allies share the player's theme.</summary>
    public static string PhaseMusic(Faction phase) => phase == Faction.Enemy ? "enemy_phase" : "player_phase";
}
