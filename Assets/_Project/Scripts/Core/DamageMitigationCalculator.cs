using UnityEngine;

public static class DamageMitigationCalculator
{
    public static float ApplyArmor(float rawDamage, float armor)
    {
        rawDamage = Mathf.Max(0f, rawDamage);
        armor = Mathf.Max(0f, armor);

        return rawDamage * 100f / (100f + armor);
    }
}
