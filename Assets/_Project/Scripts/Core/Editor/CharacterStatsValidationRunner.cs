using UnityEditor;
using UnityEngine;

public static class CharacterStatsValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate CharacterStats")]
    public static void Validate()
    {
        ValidatePlainStatBlock();

        GameObject gameObject = new("CharacterStatsValidationRunner");
        try
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            ValidateModifierLayers(stats);
            ValidateSourceRemoval(stats);
            ValidateEquipmentSlotSources(stats);

            Debug.Log("CharacterStats validation passed.");
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void ValidatePlainStatBlock()
    {
        StatBlock block = new();
        int changedCount = 0;
        block.StatChanged += _ => changedCount++;

        block.SetBaseValue(StatType.Damage, 10f);
        block.AddModifier(new StatModifier(StatType.Damage, StatModifierType.Flat, 5f));
        block.AddModifier(
            new StatModifier(StatType.Damage, StatModifierType.Increased, 100f),
            StatModifierSource.ForSystem("Validation.System"));

        AssertApproximately(block.GetValue(StatType.Damage), 30f, "StatBlock base/flat/increased calculation");
        AssertApproximately(block.GetValue(StatType.Damage), 30f, "StatBlock cached value");

        block.RemoveModifiersFromSource(StatModifierSource.ForSystem("Validation.System"));
        AssertApproximately(block.GetValue(StatType.Damage), 15f, "StatBlock source removal recalculates cache");

        if (changedCount < 3)
            throw new System.InvalidOperationException("StatBlock did not raise expected StatChanged events.");
    }

    private static void ValidateModifierLayers(CharacterStats stats)
    {
        stats.AddModifier(new StatModifier(StatType.Damage, StatModifierType.Flat, 100f));
        stats.AddModifier(new StatModifier(
            StatType.Damage,
            StatModifierType.Increased,
            50f),
            StatModifierSource.ForBuff("Validation.Increased"));
        stats.AddModifier(new StatModifier(
            StatType.Damage,
            StatModifierType.More,
            20f),
            StatModifierSource.ForActivity("Validation.More"));

        AssertApproximately(stats.GetValue(StatType.Damage), 180f, "Flat/Increased/More calculation");
    }

    private static void ValidateSourceRemoval(CharacterStats stats)
    {
        stats.RemoveModifiersFromSource(StatModifierSource.ForBuff("Validation.Increased"));
        AssertApproximately(stats.GetValue(StatType.Damage), 120f, "Buff source removal");

        stats.RemoveModifiersFromSource(StatModifierSource.ForActivity("Validation.More"));
        AssertApproximately(stats.GetValue(StatType.Damage), 100f, "Activity source removal");
    }

    private static void ValidateEquipmentSlotSources(CharacterStats stats)
    {
        StatModifierSource mainHand = StatModifierSource.ForEquipmentSlot(EquipmentSlotId.MainHand);
        StatModifierSource offHand = StatModifierSource.ForEquipmentSlot(EquipmentSlotId.OffHand);

        stats.AddModifier(new StatModifier(StatType.Armor, StatModifierType.Flat, 10f), mainHand);
        stats.AddModifier(new StatModifier(StatType.Armor, StatModifierType.Flat, 20f), offHand);
        AssertApproximately(stats.GetValue(StatType.Armor), 30f, "Equipment slot source add");

        stats.RemoveModifiersFromSource(mainHand);
        AssertApproximately(stats.GetValue(StatType.Armor), 20f, "Equipment slot source isolation");
    }

    private static void AssertApproximately(float actual, float expected, string label)
    {
        if (Mathf.Approximately(actual, expected))
            return;

        throw new System.InvalidOperationException($"{label} failed. Expected {expected}, got {actual}.");
    }
}
