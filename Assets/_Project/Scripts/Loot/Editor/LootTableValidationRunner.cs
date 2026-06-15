using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LootTableValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate LootTable")]
    public static void Validate()
    {
        ItemDefinition material = ScriptableObject.CreateInstance<ItemDefinition>();
        ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();
        LootTable table = ScriptableObject.CreateInstance<LootTable>();

        try
        {
            ConfigureCraftingMaterial(material);
            ConfigureWeapon(sword);
            ConfigureTable(table, material, sword);

            List<LootDropResult> drops = table.Roll(new System.Random(100));
            ValidateGold(drops);
            ValidateStackableSplit(drops, material);
            ValidateNonStackableInstance(drops, sword);

            Debug.Log("LootTable validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(sword);
            UnityEngine.Object.DestroyImmediate(table);
        }
    }

    private static void ValidateGold(List<LootDropResult> drops)
    {
        LootDropResult gold = FindGold(drops);
        Assert(gold.Kind == LootDropKind.Gold, "Gold drop was not generated.");
        Assert(gold.GoldAmount == 25, $"Expected 25 gold, got {gold.GoldAmount}.");
    }

    private static void ValidateStackableSplit(List<LootDropResult> drops, ItemDefinition material)
    {
        ItemStack first = FindItemStack(drops, material, 0);
        ItemStack second = FindItemStack(drops, material, 1);

        Assert(first != null, "First stackable item stack was not generated.");
        Assert(second != null, "Second stackable item stack was not generated.");
        Assert(first.Amount == 99, "First stackable item stack should contain MaxStack 99.");
        Assert(second.Amount == 21, "Second stackable item stack should contain remaining 21.");
        Assert(first.Instance == null && second.Instance == null, "Stackable loot should not create ItemInstance.");
    }

    private static void ValidateNonStackableInstance(List<LootDropResult> drops, ItemDefinition sword)
    {
        ItemStack swordStack = FindItemStack(drops, sword, 0);

        Assert(swordStack != null, "Equipment loot stack was not generated.");
        Assert(swordStack.Instance != null, "Equipment loot stack has no ItemInstance.");
        Assert(ReferenceEquals(swordStack.Instance.Definition, sword), "Equipment ItemInstance definition mismatch.");
        Assert(swordStack.Instance.GeneratedModifiers.Count == 1, "Equipment generated modifier count mismatch.");
        AssertGeneratedModifiersAreWholeNumbers(swordStack.Instance.GeneratedModifiers);
    }

    private static LootDropResult FindGold(List<LootDropResult> drops)
    {
        for (int i = 0; i < drops.Count; i++)
        {
            if (drops[i].Kind == LootDropKind.Gold)
                return drops[i];
        }

        return default;
    }

    private static ItemStack FindItemStack(List<LootDropResult> drops, ItemDefinition definition, int occurrence)
    {
        int seen = 0;

        for (int i = 0; i < drops.Count; i++)
        {
            LootDropResult drop = drops[i];
            if (drop.Kind != LootDropKind.Item || drop.Stack == null)
                continue;

            if (!ReferenceEquals(drop.Stack.Definition, definition))
                continue;

            if (seen == occurrence)
                return drop.Stack;

            seen++;
        }

        return null;
    }

    private static void ConfigureTable(LootTable table, ItemDefinition material, ItemDefinition sword)
    {
        SerializedObject serialized = new(table);
        SerializedProperty entries = serialized.FindProperty("entries");
        entries.arraySize = 3;

        SerializedProperty goldEntry = entries.GetArrayElementAtIndex(0);
        goldEntry.FindPropertyRelative("kind").enumValueIndex = (int)LootDropKind.Gold;
        goldEntry.FindPropertyRelative("dropChance").floatValue = 1f;
        goldEntry.FindPropertyRelative("minAmount").intValue = 25;
        goldEntry.FindPropertyRelative("maxAmount").intValue = 25;

        SerializedProperty materialEntry = entries.GetArrayElementAtIndex(1);
        materialEntry.FindPropertyRelative("kind").enumValueIndex = (int)LootDropKind.Item;
        materialEntry.FindPropertyRelative("dropChance").floatValue = 1f;
        materialEntry.FindPropertyRelative("minAmount").intValue = 120;
        materialEntry.FindPropertyRelative("maxAmount").intValue = 120;
        materialEntry.FindPropertyRelative("item").objectReferenceValue = material;

        SerializedProperty swordEntry = entries.GetArrayElementAtIndex(2);
        swordEntry.FindPropertyRelative("kind").enumValueIndex = (int)LootDropKind.Item;
        swordEntry.FindPropertyRelative("dropChance").floatValue = 1f;
        swordEntry.FindPropertyRelative("minAmount").intValue = 1;
        swordEntry.FindPropertyRelative("maxAmount").intValue = 1;
        swordEntry.FindPropertyRelative("item").objectReferenceValue = sword;
        swordEntry.FindPropertyRelative("minGeneratedModifiers").intValue = 1;
        swordEntry.FindPropertyRelative("maxGeneratedModifiers").intValue = 1;

        SerializedProperty modifierRules = swordEntry.FindPropertyRelative("generatedModifierRules");
        modifierRules.arraySize = 1;
        ConfigureModifierRule(modifierRules.GetArrayElementAtIndex(0), StatType.Damage, StatModifierType.Flat, 3f, 7f);

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
        serialized.FindProperty("id").stringValue = "validation_unified_loot_table_crystal";
        serialized.FindProperty("displayName").stringValue = "Validation Unified Loot Table Crystal";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Crafting;
        serialized.FindProperty("maxStack").intValue = 99;
        serialized.FindProperty("craftingSubtype").enumValueIndex = (int)CraftingSubtype.Material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureWeapon(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_unified_loot_table_sword";
        serialized.FindProperty("displayName").stringValue = "Validation Unified Loot Table Sword";
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
