using System;
using UnityEditor;
using UnityEngine;

public static class PlayerInventoryItemStackLootRewardValidationRunner
{
    [MenuItem("Tools/Titanhold/Validate ItemStack Loot Reward")]
    public static void Validate()
    {
        GameObject player = new("ItemStackLootRewardValidationPlayer");
        GameObject pickup = new("ItemStackLootRewardValidationPickup");
        ItemDefinition sword = ScriptableObject.CreateInstance<ItemDefinition>();

        try
        {
            ConfigureWeapon(sword);

            PlayerInventory inventory = player.AddComponent<PlayerInventory>();
            PlayerInventoryItemStackLootReward reward = pickup.AddComponent<PlayerInventoryItemStackLootReward>();
            ItemStack stack = ItemDropGenerator.CreateStack(
                sword,
                1,
                new[] { new ItemModifierRollRule(StatType.Damage, StatModifierType.Flat, 3f, 3f) },
                1,
                1,
                new System.Random(1));

            ItemInstance originalInstance = stack.Instance;
            reward.SetStack(stack);

            Assert(reward.Collect(player), "Generated stack reward was not collected.");

            ItemStack storedStack = inventory.GetSlot(ItemCategory.Equipment, 0).Stack;
            Assert(storedStack != null, "Collected stack was not stored.");
            Assert(ReferenceEquals(storedStack, stack), "Collected stack reference was not preserved.");
            Assert(ReferenceEquals(storedStack.Instance, originalInstance), "Collected ItemInstance reference was not preserved.");
            Assert(storedStack.Instance.GeneratedModifiers.Count == 1, "Generated modifiers were not preserved.");

            Debug.Log("ItemStack loot reward validation passed.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(pickup);
            UnityEngine.Object.DestroyImmediate(sword);
        }
    }

    private static void ConfigureWeapon(ItemDefinition definition)
    {
        SerializedObject serialized = new(definition);
        serialized.FindProperty("id").stringValue = "validation_item_stack_reward_sword";
        serialized.FindProperty("displayName").stringValue = "Validation Item Stack Reward Sword";
        serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Equipment;
        serialized.FindProperty("maxStack").intValue = 1;
        serialized.FindProperty("equipmentSlotType").enumValueIndex = (int)EquipmentSlotType.Weapon;
        serialized.FindProperty("weaponType").enumValueIndex = (int)WeaponType.OneHandSword;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Assert(bool condition, string label)
    {
        if (condition)
            return;

        throw new InvalidOperationException(label);
    }
}
