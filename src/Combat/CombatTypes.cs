using System.Collections.Generic;

namespace NotFireEmblem.Combat;

/// <summary>What the attacker uses. Arts and spells are added in M5.</summary>
public abstract record ActionChoice;

public sealed record WeaponAttack(string WeaponId) : ActionChoice;

/// <summary>One side's numbers in a forecast. Displayed values are the exact values the game uses.</summary>
public sealed record CombatantForecast(
    string UnitId,
    string Name,
    int Hp,
    int MaxHp,
    string? WeaponName,
    bool CanStrike,
    int Damage,
    int Hit,
    int Crit,
    int AttackSpeed,
    int StrikeCount,
    int TriangleAdvantage, // +1 advantage, -1 disadvantage, 0 neutral
    bool Effective);

public sealed record PlannedStrike(bool ByAttacker, int Damage, int Hit, int Crit);

/// <summary>Read by both the UI forecast panel and the AI.</summary>
public sealed record CombatForecast(
    CombatantForecast Attacker,
    CombatantForecast Defender,
    IReadOnlyList<PlannedStrike> Strikes,
    int Distance);

public sealed record Strike(bool ByAttacker, bool Hit, bool Crit, int Damage, int TargetHpAfter);

public sealed record CombatResult(IReadOnlyList<Strike> Strikes, int AttackerHpAfter, int DefenderHpAfter)
{
    public bool AttackerDied => AttackerHpAfter <= 0;
    public bool DefenderDied => DefenderHpAfter <= 0;
}
