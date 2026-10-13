using System;

namespace NotFireEmblem.Combat;

/// <summary>Exact outcome probabilities of a forecast, used by the AI to score attacks.</summary>
public readonly record struct CombatOdds(
    double DefenderDies,
    double AttackerDies,
    double ExpectedDamageToDefender,
    double ExpectedDamageToAttacker)
{
    /// <summary>The true chance that a 2RN check against <paramref name="displayedHit"/> succeeds.</summary>
    public static double TrueHit(int displayedHit)
    {
        if (displayedHit <= 0)
            return 0;
        if (displayedHit >= 100)
            return 1;
        int successes = 0;
        for (int a = 0; a < 100; a++)
            successes += Math.Clamp(2 * displayedHit - a, 0, 100);
        return successes / 10000.0;
    }

    /// <summary>Enumerates every miss/hit/crit branch of the strike sequence (at most 3^4 leaves).</summary>
    public static CombatOdds Evaluate(CombatForecast f)
    {
        double defenderDies = 0, attackerDies = 0, damageToDefender = 0, damageToAttacker = 0;

        void Walk(int index, int atkHp, int defHp, double p)
        {
            if (p <= 0)
                return;
            if (index >= f.Strikes.Count || atkHp <= 0 || defHp <= 0)
            {
                if (defHp <= 0) defenderDies += p;
                if (atkHp <= 0) attackerDies += p;
                damageToDefender += p * (f.Defender.Hp - Math.Max(defHp, 0));
                damageToAttacker += p * (f.Attacker.Hp - Math.Max(atkHp, 0));
                return;
            }
            var strike = f.Strikes[index];
            double hit = TrueHit(strike.Hit);
            double crit = Math.Clamp(strike.Crit, 0, 100) / 100.0;
            int normal = strike.Damage;
            int critical = strike.Damage * CombatCalculator.CritMultiplier;

            Walk(index + 1, atkHp, defHp, p * (1 - hit));
            if (strike.ByAttacker)
            {
                Walk(index + 1, atkHp, defHp - normal, p * hit * (1 - crit));
                Walk(index + 1, atkHp, defHp - critical, p * hit * crit);
            }
            else
            {
                Walk(index + 1, atkHp - normal, defHp, p * hit * (1 - crit));
                Walk(index + 1, atkHp - critical, defHp, p * hit * crit);
            }
        }

        Walk(0, f.Attacker.Hp, f.Defender.Hp, 1.0);
        return new CombatOdds(defenderDies, attackerDies, damageToDefender, damageToAttacker);
    }
}
