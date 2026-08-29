using UnityEditor;
using UnityEngine;

public static class RegenerationValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate Regeneration")]
    public static void Validate()
    {
        ValidateHealthRegeneration();
        ValidateResourceRegeneration();

        Debug.Log("Regeneration validation passed.");
    }

    private static void ValidateHealthRegeneration()
    {
        GameObject gameObject = new("HealthRegenerationValidation");

        try
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxHealth, 100f);
            stats.Block.SetBaseValue(StatType.HPRegen, 10f);

            Health health = gameObject.AddComponent<Health>();
            SerializedObject serializedHealth = new(health);
            serializedHealth.FindProperty("characterStats").objectReferenceValue = stats;
            serializedHealth.ApplyModifiedPropertiesWithoutUndo();

            health.RestoreFull();
            health.TakeDamage(50f);
            AssertApproximately(health.CurrentHealth, 50f, "Health damaged before regen");

            health.TickRegeneration(1f);
            AssertApproximately(health.CurrentHealth, 60f, "HPRegen restores health over time");

            health.TickRegeneration(10f);
            AssertApproximately(health.CurrentHealth, 100f, "HPRegen does not exceed MaxHealth");

            stats.Block.SetBaseValue(StatType.HPRegen, 0f);
            health.TakeDamage(20f);
            health.TickRegeneration(1f);
            AssertApproximately(health.CurrentHealth, 80f, "0 HPRegen does not restore health");
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void ValidateResourceRegeneration()
    {
        GameObject gameObject = new("ResourceRegenerationValidation");

        try
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.MaxResource, 100f);
            stats.Block.SetBaseValue(StatType.ResourceRegen, 12f);

            PlayerResource resource = gameObject.AddComponent<PlayerResource>();
            SerializedObject serializedResource = new(resource);
            serializedResource.FindProperty("characterStats").objectReferenceValue = stats;
            serializedResource.ApplyModifiedPropertiesWithoutUndo();

            resource.Restore(100f);
            Assert(resource.TrySpend(50f), "Resource spend before regen failed.");
            AssertApproximately(resource.CurrentResource, 50f, "Resource spent before regen");

            resource.TickRegeneration(1f);
            AssertApproximately(resource.CurrentResource, 62f, "ResourceRegen restores resource over time");

            resource.TickRegeneration(10f);
            AssertApproximately(resource.CurrentResource, 100f, "ResourceRegen does not exceed MaxResource");

            stats.Block.SetBaseValue(StatType.ResourceRegen, 0f);
            Assert(resource.TrySpend(20f), "Resource spend before 0 regen failed.");
            resource.TickRegeneration(1f);
            AssertApproximately(resource.CurrentResource, 80f, "0 ResourceRegen does not restore resource");
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void Assert(bool condition, string label)
    {
        if (condition)
            return;

        throw new System.InvalidOperationException(label);
    }

    private static void AssertApproximately(float actual, float expected, string label)
    {
        if (Mathf.Approximately(actual, expected))
            return;

        throw new System.InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
    }
}
