using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class ItemDropGeneratorValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate ItemDropGenerator")]
    public static void Validate()
    {
        ItemDefinition material = ScriptableObject.CreateInstance<ItemDefinition>();
        ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();

        try
        {
            ConfigureCraftingMaterial(material);
            ConfigureWeapon(sword);

            ValidateStackableGeneration(material);
            ValidateNonStackableGeneratedModifiers(sword);
            ValidateWholeNumberRolls(sword);
            ValidateDeterministicRolls(sword);

            Debug.Log("ItemDropGenerator validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(material);
            UnityEngine.Object.DestroyImmediate(sword);
        }
    }

    private static void ValidateStackableGeneration(ItemDefinition material)
    {
        ItemModifierRollRule[] rules =
        {
            new(StatType.Damage, StatModifierType.Flat, 1f, 5f)
        };

        ItemStack stack = ItemDropGenerator.CreateStack(material, 12, rules, 1, 1, new System.Random(1));

        Assert(stack != null, "Stackable generator returned null.");
        Assert(ReferenceEquals(stack.Definition, material), "Stackable definition changed.");
        Assert(stack.Amount == 12, "Stackable amount mismatch.");
        Assert(stack.Instance == null, "Stackable item should not have an ItemInstance.");
    }

    private static void ValidateNonStackableGeneratedModifiers(ItemDefinition sword)
    {
        ItemModifierRollRule[] rules =
        {
            new(StatType.Damage, StatModifierType.Flat, 3f, 7f),
            new(StatType.AttackSpeed, StatModifierType.Increased, 5f, 10f)
        };

        ItemStack stack = ItemDropGenerator.CreateStack(sword, 1, rules, 2, 2, new System.Random(2));

        Assert(stack != null, "Non-stackable generator returned null.");
        Assert(stack.Instance != null, "Non-stackable generated item should have an ItemInstance.");
        Assert(ReferenceEquals(stack.Instance.Definition, sword), "Generated instance definition changed.");
        Assert(stack.Instance.GeneratedModifiers.Count == 2, "Generated modifier count mismatch.");
        Assert(stack.Amount == 1, "Non-stackable amount should be 1.");
    }

    private static void ValidateDeterministicRolls(ItemDefinition sword)
    {
        ItemModifierRollRule[] rules =
        {
            new(StatType.Damage, StatModifierType.Flat, 3f, 7f),
            new(StatType.AttackSpeed, StatModifierType.Increased, 5f, 10f),
            new(StatType.Agility, StatModifierType.Flat, 1f, 4f)
        };

        ItemStack first = ItemDropGenerator.CreateStack(sword, 1, rules, 2, 2, new System.Random(12345));
        ItemStack second = ItemDropGenerator.CreateStack(sword, 1, rules, 2, 2, new System.Random(12345));

        IReadOnlyList<StatModifierData> firstModifiers = first.Instance.GeneratedModifiers;
        IReadOnlyList<StatModifierData> secondModifiers = second.Instance.GeneratedModifiers;

        Assert(firstModifiers.Count == secondModifiers.Count, "Deterministic roll count mismatch.");

        for (int i = 0; i < firstModifiers.Count; i++)
        {
            Assert(firstModifiers[i].Type == secondModifiers[i].Type, "Deterministic roll stat type mismatch.");
            Assert(firstModifiers[i].ModifierType == secondModifiers[i].ModifierType, "Deterministic roll modifier type mismatch.");
            Assert(Mathf.Approximately(firstModifiers[i].Value, secondModifiers[i].Value), "Deterministic roll value mismatch.");
        }
    }

    private static void ValidateWholeNumberRolls(ItemDefinition sword)
    {
        ItemModifierRollRule[] rules =
        {
            new(StatType.AttackSpeed, StatModifierType.Increased, 5.25f, 10.75f, true)
        };

        ItemStack stack = ItemDropGenerator.CreateStack(sword, 1, rules, 1, 1, new System.Random(3));
        float value = stack.Instance.GeneratedModifiers[0].Value;

        Assert(Mathf.Approximately(value, Mathf.Round(value)), "Whole-number roll produced a fractional value.");
    }

    private static void ConfigureCraftingMaterial(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_drop_generator_crystal";
        serialized.FindProperty("displayName").stringValue = "Validation Drop Generator Crystal";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Crafting;
        serialized.FindProperty("maxStack").intValue = 99;
        serialized.FindProperty("craftingSubtype").enumValueIndex = (int)CraftingSubtype.Material;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigureWeapon(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_drop_generator_sword";
        serialized.FindProperty("displayName").stringValue = "Validation Drop Generator Sword";
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
}
