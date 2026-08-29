using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ItemLootTableValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate ItemLootTable")]
    public static void Validate()
    {
        ItemDefinition material = ScriptableObject.CreateInstance<ItemDefinition>();
        ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();
        ItemLootTable table = ScriptableObject.CreateInstance<ItemLootTable>();

        try
        {
            ConfigureCraftingMaterial(material);
            ConfigureWeapon(sword);
            ConfigureTable(table, material, sword);

            ValidateStackableSplit(table, material);
            ValidateNonStackableInstances(table, sword);
            ValidateDeterministicRolls(table);

            Debug.Log("ItemLootTable validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(sword);
            UnityEngine.Object.DestroyImmediate(table);
        }
    }

    private static void ValidateStackableSplit(ItemLootTable table, ItemDefinition material)
    {
        List<ItemStack> drops = table.Roll(new System.Random(10));
        ItemStack firstMaterial = FindStack(drops, material, 0);
        ItemStack secondMaterial = FindStack(drops, material, 1);

        Assert(firstMaterial != null, "First material stack was not generated.");
        Assert(secondMaterial != null, "Second material stack was not generated.");
        Assert(firstMaterial.Amount == 99, "First material stack should contain MaxStack 99.");
        Assert(secondMaterial.Amount == 21, "Second material stack should contain remaining 21.");
        Assert(firstMaterial.Instance == null && secondMaterial.Instance == null, "Stackable drops should not create ItemInstance.");
    }

    private static void ValidateNonStackableInstances(ItemLootTable table, ItemDefinition sword)
    {
        List<ItemStack> drops = table.Roll(new System.Random(20));
        HashSet<string> ids = new();
        int swordCount = 0;

        foreach (ItemStack drop in drops)
        {
            if (!ReferenceEquals(drop.Definition, sword))
                continue;

            swordCount++;
            Assert(drop.Instance != null, "Equipment drop has no ItemInstance.");
            Assert(ids.Add(drop.Instance.InstanceId), "Equipment drop duplicated an InstanceId.");
            Assert(drop.Instance.GeneratedModifiers.Count == 2, "Equipment drop generated modifier count mismatch.");
            AssertGeneratedModifiersAreWholeNumbers(drop.Instance.GeneratedModifiers);
        }

        Assert(swordCount == 3, $"Expected 3 sword drops, got {swordCount}.");
    }

    private static void ValidateDeterministicRolls(ItemLootTable table)
    {
        List<ItemStack> first = table.Roll(new System.Random(12345));
        List<ItemStack> second = table.Roll(new System.Random(12345));

        Assert(first.Count == second.Count, "Deterministic table roll count mismatch.");

        for (int i = 0; i < first.Count; i++)
        {
            Assert(ReferenceEquals(first[i].Definition, second[i].Definition), "Deterministic table roll definition mismatch.");
            Assert(first[i].Amount == second[i].Amount, "Deterministic table roll amount mismatch.");

            if (first[i].Instance == null || second[i].Instance == null)
                continue;

            IReadOnlyList<StatModifierData> firstModifiers = first[i].Instance.GeneratedModifiers;
            IReadOnlyList<StatModifierData> secondModifiers = second[i].Instance.GeneratedModifiers;
            Assert(firstModifiers.Count == secondModifiers.Count, "Deterministic generated modifier count mismatch.");

            for (int modifierIndex = 0; modifierIndex < firstModifiers.Count; modifierIndex++)
            {
                Assert(firstModifiers[modifierIndex].Type == secondModifiers[modifierIndex].Type, "Deterministic modifier stat mismatch.");
                Assert(firstModifiers[modifierIndex].ModifierType == secondModifiers[modifierIndex].ModifierType, "Deterministic modifier type mismatch.");
                Assert(Mathf.Approximately(firstModifiers[modifierIndex].Value, secondModifiers[modifierIndex].Value), "Deterministic modifier value mismatch.");
            }
        }
    }

    private static ItemStack FindStack(List<ItemStack> drops, ItemDefinition definition, int occurrence)
    {
        int seen = 0;
        foreach (ItemStack drop in drops)
        {
            if (!ReferenceEquals(drop.Definition, definition))
                continue;

            if (seen == occurrence)
                return drop;

            seen++;
        }

        return null;
    }

    private static void ConfigureTable(ItemLootTable table, ItemDefinition material, ItemDefinition sword)
    {
        SerializedObject serialized = new(table);
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.arraySize = 2;

        SerializedProperty materialEntry = entries.GetArrayElementAtIndex(0);
        materialEntry.FindPropertyRelative("item").objectReferenceValue = material;
        materialEntry.FindPropertyRelative("dropChance").floatValue = 1f;
        materialEntry.FindPropertyRelative("minAmount").intValue = 120;
        materialEntry.FindPropertyRelative("maxAmount").intValue = 120;
        materialEntry.FindPropertyRelative("minGeneratedModifiers").intValue = 0;
        materialEntry.FindPropertyRelative("maxGeneratedModifiers").intValue = 0;

        SerializedProperty swordEntry = entries.GetArrayElementAtIndex(1);
        swordEntry.FindPropertyRelative("item").objectReferenceValue = sword;
        swordEntry.FindPropertyRelative("dropChance").floatValue = 1f;
        swordEntry.FindPropertyRelative("minAmount").intValue = 3;
        swordEntry.FindPropertyRelative("maxAmount").intValue = 3;
        swordEntry.FindPropertyRelative("minGeneratedModifiers").intValue = 2;
        swordEntry.FindPropertyRelative("maxGeneratedModifiers").intValue = 2;

        SerializedProperty modifierRules = swordEntry.FindPropertyRelative("generatedModifierRules");
        modifierRules.arraySize = 2;
        ConfigureModifierRule(modifierRules.GetArrayElementAtIndex(0), StatType.Damage, StatModifierType.Flat, 3f, 7f);
        ConfigureModifierRule(modifierRules.GetArrayElementAtIndex(1), StatType.AttackSpeed, StatModifierType.Increased, 5f, 10f);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureModifierRule(
        SerializedProperty property,
        StatType type,
        StatModifierType modifierType,
        float minValue,
        float maxValue)
    {
        property.FindPropertyRelative("type").enumValueIndex = (int)type;
        property.FindPropertyRelative("modifierType").enumValueIndex = (int)modifierType;
        property.FindPropertyRelative("minValue").floatValue = minValue;
        property.FindPropertyRelative("maxValue").floatValue = maxValue;
        property.FindPropertyRelative("wholeNumberValues").boolValue = true;
    }

    private static void ConfigureCraftingMaterial(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_loot_table_crystal";
        serialized.FindProperty("displayName").stringValue = "Validation Loot Table Crystal";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Crafting;
        serialized.FindProperty("maxStack").intValue = 99;
        serialized.FindProperty("craftingSubtype").enumValueIndex = (int)CraftingSubtype.Material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureWeapon(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_loot_table_sword";
        serialized.FindProperty("displayName").stringValue = "Validation Loot Table Sword";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Equipment;
        serialized.FindProperty("maxStack").intValue = 1;
        serialized.FindProperty("equipmentSlotType").enumValueIndex = (int)EquipmentSlotType.Weapon;
        serialized.FindProperty("weaponType").enumValueIndex = (int)WeaponType.OneHandSword;
        serialized.FindProperty("weaponBaseDamage").floatValue = 5f;
        serialized.FindProperty("weaponBaseAttacksPerSecond").floatValue = 1.2f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Assert(bool condition, string label)
    {
        if (condition)
            return;

        throw new InvalidOperationException(label);
    }

    private static void AssertGeneratedModifiersAreWholeNumbers(IReadOnlyList<StatModifierData> modifiers)
    {
        for (int i = 0; i < modifiers.Count; i++)
        {
            float value = modifiers[i].Value;
            Assert(Mathf.Approximately(value, Mathf.Round(value)), "Loot table whole-number roll produced a fractional value.");
        }
    }
}
