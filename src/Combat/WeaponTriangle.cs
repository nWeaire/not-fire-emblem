using NotFireEmblem.Units;

namespace NotFireEmblem.Combat;

/// <summary>Sword &gt; Axe &gt; Lance &gt; Sword. Everything else sits outside the triangle.</summary>
public static class WeaponTriangle
{
    public const int HitBonus = 15;
    public const int DamageBonus = 1;

    /// <summary>+1 if <paramref name="mine"/> beats <paramref name="theirs"/>, -1 if it loses, 0 otherwise.</summary>
    public static int Advantage(WeaponType mine, WeaponType? theirs)
    {
        if (theirs is not { } other)
            return 0;
        if (Beats(mine, other))
            return 1;
        if (Beats(other, mine))
            return -1;
        return 0;
    }

    private static bool Beats(WeaponType a, WeaponType b) =>
        (a, b) is (WeaponType.Sword, WeaponType.Axe) or (WeaponType.Axe, WeaponType.Lance) or (WeaponType.Lance, WeaponType.Sword);
}
