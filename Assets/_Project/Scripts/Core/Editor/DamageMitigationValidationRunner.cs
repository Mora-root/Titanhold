using UnityEditor;
using UnityEngine;

public static class DamageMitigationValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Damage Mitigation")]
    public static void Validate()
    {
        ValidateCalculator();
        ValidateHealthDamage();

        Debug.Log("DamageMitigation validation passed.");
    }

    private static void ValidateCalculator()
    {
        AssertApproximately(DamageMitigationCalculator.ApplyArmor(100f, 0f), 100f, "0 armor");
        AssertApproximately(DamageMitigationCalculator.ApplyArmor(100f, 100f), 50f, "100 armor");
        AssertApproximately(DamageMitigationCalculator.ApplyArmor(100f, 300f), 25f, "300 armor");
        AssertApproximately(DamageMitigationCalculator.ApplyArmor(100f, -50f), 100f, "negative armor");
        AssertApproximately(DamageMitigationCalculator.ApplyArmor(-10f, 100f), 0f, "negative damage");
    }

    private static void ValidateHealthDamage()
    {
        GameObject unarmoredObject = new("DamageMitigationUnarmoredHealth");
        GameObject armoredObject = new("DamageMitigationArmoredHealth");

        try
        {
            Health unarmoredHealth = unarmoredObject.AddComponent<Health>();
            unarmoredHealth.RestoreFull();
            unarmoredHealth.TakeDamage(40f);
            AssertApproximately(unarmoredHealth.CurrentHealth, 60f, "Health without CharacterStats takes full damage");

            CharacterStats stats = armoredObject.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
            stats.Block.SetBaseValue(StatType.Armor, 100f);

            Health armoredHealth = armoredObject.AddComponent<Health>();
            SerializedObject serializedHealth = new(armoredHealth);
            serializedHealth.FindProperty("characterStats").objectReferenceValue = stats;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            armoredHealth.RestoreFull();
            armoredHealth.TakeDamage(40f);
            AssertApproximately(armoredHealth.CurrentHealth, 80f, "Health with armor takes reduced damage");
        }
        finally
        {
            Object.DestroyImmediate(unarmoredObject);
            Object.DestroyImmediate(armoredObject);
        }
    }

    private static void AssertApproximately(float actual, float expected, string label)
    {
        if (Mathf.Approximately(actual, expected))
            return;

        throw new System.InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
    }
}
