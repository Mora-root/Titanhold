using UnityEngine;

public static class CombatDamageCalculator
{
    private const float DefaultFallbackDamage = 10f;

    public static float GetGlobalDamage(CharacterStats stats, float fallbackDamage = DefaultFallbackDamage)
    {
        if (stats != null)
        {
            float statDamage = stats.GetValue(StatType.Damage);
            if (statDamage > 0f)
                return statDamage;
        }

        return Mathf.Max(0f, fallbackDamage);
    }

    public static float GetSkillDamage(CharacterStats stats, SkillData skill, float fallbackDamage = DefaultFallbackDamage)
    {
        if (skill == null)
            return 0f;

        return GetGlobalDamage(stats, fallbackDamage) * Mathf.Max(0f, skill.DamageMultiplier);
    }
}
