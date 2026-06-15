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
            ValidateItemInstanceGeneratedModifiers();
            ValidateAttributeDerivedStats();

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

    private static void ValidateItemInstanceGeneratedModifiers()
    {
        GameObject gameObject = new("ItemInstanceGeneratedModifierValidation");
        ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();

        try
        {
            ConfigureEquipmentWeapon(sword);

            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            PlayerInventory inventory = gameObject.AddComponent<PlayerInventory>();
            PlayerEquipmentRuntime runtime = gameObject.AddComponent<PlayerEquipmentRuntime>();
            runtime.SetPlayerInventory(inventory);
            EquipmentStatsBinder binder = gameObject.AddComponent<EquipmentStatsBinder>();
            binder.Refresh();

            ItemInstance weakSword = new(
                sword,
                new[] { new StatModifierData(StatType.Damage, StatModifierType.Flat, 5f) });
            ItemInstance strongSword = new(
                sword,
                new[] { new StatModifierData(StatType.Damage, StatModifierType.Flat, 10f) });

            Assert(runtime.Equipment.TrySetSlot(EquipmentSlotId.MainHand, weakSword), "Generated modifier weak sword equip failed.");
            AssertApproximately(stats.GetValue(StatType.Damage), 5f, "Generated modifier weak sword damage");

            Assert(ReferenceEquals(runtime.Equipment.ClearSlot(EquipmentSlotId.MainHand), weakSword), "Generated modifier weak sword clear failed.");
            AssertApproximately(stats.GetValue(StatType.Damage), 0f, "Generated modifier removal after unequip");

            Assert(runtime.Equipment.TrySetSlot(EquipmentSlotId.MainHand, strongSword), "Generated modifier strong sword equip failed.");
            AssertApproximately(stats.GetValue(StatType.Damage), 10f, "Generated modifier strong sword damage");
        }
        finally
        {
            Object.DestroyImmediate(sword);
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void ValidateAttributeDerivedStats()
    {
        GameObject gameObject = new("AttributeDerivedStatsValidation");

        try
        {
            CharacterStats stats = gameObject.AddComponent<CharacterStats>();
            stats.Block.SetBaseValue(StatType.Strength, 10f);
            stats.Block.SetBaseValue(StatType.Agility, 5f);
            stats.Block.SetBaseValue(StatType.Intelligence, 4f);
            stats.Block.SetBaseValue(StatType.Stamina, 3f);
            stats.Block.SetBaseValue(StatType.AttackSpeed, 100f);

            CharacterAttributeDerivedStatsBinder binder =
                gameObject.AddComponent<CharacterAttributeDerivedStatsBinder>();
            binder.Refresh();

            AssertApproximately(stats.GetValue(StatType.Damage), 20f, "Attribute derived Strength -> Damage");
            AssertApproximately(stats.GetValue(StatType.Armor), 5f, "Attribute derived Strength -> Armor");
            AssertApproximately(stats.GetValue(StatType.AttackSpeed), 105f, "Attribute derived Agility -> AttackSpeed");
            AssertApproximately(stats.GetValue(StatType.MoveSpeed), 0.1f, "Attribute derived Agility -> MoveSpeed");
            AssertApproximately(stats.GetValue(StatType.MaxResource), 32f, "Attribute derived Intelligence -> MaxResource");
            AssertApproximately(stats.GetValue(StatType.ResourceRegen), 0.4f, "Attribute derived Intelligence -> ResourceRegen");
            AssertApproximately(stats.GetValue(StatType.MaxHealth), 30f, "Attribute derived Stamina -> MaxHealth");
            AssertApproximately(stats.GetValue(StatType.HPRegen), 0.3f, "Attribute derived Stamina -> HPRegen");

            binder.Recalculate();
            AssertApproximately(stats.GetValue(StatType.Damage), 20f, "Attribute derived stats should not double-stack");

            stats.AddModifier(
                new StatModifier(StatType.Strength, StatModifierType.Flat, 5f),
                StatModifierSource.ForBuff("Validation.Strength"));
            AssertApproximately(stats.GetValue(StatType.Damage), 30f, "Attribute derived Strength change recalculates Damage");
            AssertApproximately(stats.GetValue(StatType.Armor), 7.5f, "Attribute derived Strength change recalculates Armor");

            stats.AddModifier(
                new StatModifier(StatType.Damage, StatModifierType.Flat, 7f),
                StatModifierSource.ForBuff("Validation.Damage"));
            AssertApproximately(stats.GetValue(StatType.Damage), 37f, "Non-primary Damage change should not duplicate derived modifiers");

            binder.ClearDerivedModifiers();
            AssertApproximately(stats.GetValue(StatType.Damage), 7f, "Attribute derived modifiers removed on disable");
            AssertApproximately(stats.GetValue(StatType.Armor), 0f, "Attribute derived armor removed on disable");
        }
        finally
        {
            Object.DestroyImmediate(gameObject);
        }
    }

    private static void ConfigureEquipmentWeapon(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_generated_modifier_sword";
        serialized.FindProperty("displayName").stringValue = "Validation Generated Modifier Sword";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Equipment;
        serialized.FindProperty("equipmentSlotType").enumValueIndex = (int)EquipmentSlotType.Weapon;
        serialized.FindProperty("weaponType").enumValueIndex = (int)WeaponType.OneHandSword;
        serialized.FindProperty("weaponBaseDamage").floatValue = 0f;
        serialized.FindProperty("weaponBaseAttacksPerSecond").floatValue = 1f;
        serialized.ApplyModifiedPropertiesWithoutUndo();
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
