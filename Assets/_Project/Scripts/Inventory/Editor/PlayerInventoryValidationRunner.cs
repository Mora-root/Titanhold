using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class PlayerInventoryValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate PlayerInventory Wrapper";
    private static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.NonPublic;

    [MenuItem(MenuPath)]
    public static void ValidateFromMenu()
    {
        try
        {
            string report = RunValidation();
            Debug.Log(report);
        }
        catch (Exception exception)
        {
            Debug.LogError($"PlayerInventory wrapper validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        GameObject temporaryObject = null;
        List<ItemDefinition> definitions = new List<ItemDefinition>();

        try
        {
            temporaryObject = EditorUtility.CreateGameObjectWithHideFlags(
                "PlayerInventoryValidationRunner_Temporary",
                HideFlags.HideAndDontSave);

            PlayerInventory inventory = temporaryObject.AddComponent<PlayerInventory>();
            inventory.EnsureInitialized();

            ItemDefinition crystalShard = CreateDefinition(
                definitions,
                "crystal_shard_player_inventory_test",
                "Crystal Shard PlayerInventory Test",
                ItemCategory.Crafting,
                99,
                craftingSubtype: CraftingSubtype.Material);

            ItemDefinition monsterFang = CreateDefinition(
                definitions,
                "monster_fang_player_inventory_test",
                "Monster Fang PlayerInventory Test",
                ItemCategory.Trophy,
                99,
                trophySubtype: TrophySubtype.MonsterPart);

            ItemDefinition sword = CreateDefinition(
                definitions,
                "sword_player_inventory_test",
                "Sword PlayerInventory Test",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ValidateSections(inventory);
            ValidateEvents(inventory, crystalShard);
            ValidateCategoryRouting(inventory, monsterFang, sword);

            return "PlayerInventory wrapper validation passed.";
        }
        finally
        {
            foreach (ItemDefinition definition in definitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }

            if (temporaryObject != null)
                UnityEngine.Object.DestroyImmediate(temporaryObject);
        }
    }

    private static void ValidateSections(PlayerInventory inventory)
    {
        foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
        {
            ItemContainerSection section = inventory.GetSection(category);
            Assert(section != null, $"Missing section for {category}.");
            Assert(section.Category == category, $"Section category mismatch for {category}.");
            Assert(section.Capacity > 0, $"Capacity for {category} should be positive.");
            Assert(inventory.CountFreeSlots(category) == section.Capacity, $"Free slot count mismatch for {category}.");
            Assert(inventory.CountOccupiedSlots(category) == 0, $"Section {category} should start empty.");

            for (int i = 0; i < section.Capacity; i++)
            {
                ItemSlot slot = inventory.GetSlot(category, i);
                Assert(slot != null, $"Slot {i} in {category} is null.");
                Assert(slot.IsEmpty, $"Slot {i} in {category} should start empty.");
            }
        }
    }

    private static void ValidateEvents(PlayerInventory inventory, ItemDefinition crystalShard)
    {
        int changedCount = 0;
        List<ItemCategory> sectionChanges = new List<ItemCategory>();

        inventory.Changed += () => changedCount++;
        inventory.SectionChanged += category => sectionChanges.Add(category);

        AddItemResult failedResult = inventory.TryAdd(crystalShard, 0);
        Assert(!failedResult.AddedAnything, "Zero amount should not add anything.");
        Assert(changedCount == 0, "Changed should not fire after failed add.");
        Assert(sectionChanges.Count == 0, "SectionChanged should not fire after failed add.");

        AddItemResult successResult = inventory.TryAdd(crystalShard, 1);
        Assert(successResult.FullyAdded, "Successful add did not fully add item.");
        Assert(changedCount == 1, "Changed should fire once after successful add.");
        Assert(sectionChanges.Count == 1, "SectionChanged should fire once after successful add.");
        Assert(sectionChanges[0] == ItemCategory.Crafting, "SectionChanged reported wrong category.");
    }

    private static void ValidateCategoryRouting(
        PlayerInventory inventory,
        ItemDefinition monsterFang,
        ItemDefinition sword)
    {
        AddItemResult trophyResult = inventory.TryAdd(monsterFang, 1);
        Assert(trophyResult.FullyAdded, "Trophy item was not added.");
        Assert(inventory.CountOccupiedSlots(ItemCategory.Trophy) == 1, "Trophy item did not route to Trophy.");

        AddItemResult equipmentResult = inventory.TryAdd(sword, 1);
        Assert(equipmentResult.FullyAdded, "Equipment item was not added.");
        Assert(inventory.CountOccupiedSlots(ItemCategory.Equipment) == 1, "Equipment item did not route to Equipment.");
    }

    private static ItemDefinition CreateDefinition(
        List<ItemDefinition> definitions,
        string id,
        string displayName,
        ItemCategory category,
        int maxStack,
        EquipmentSlotType equipmentSlotType = EquipmentSlotType.None,
        WeaponType weaponType = WeaponType.None,
        TrophySubtype trophySubtype = TrophySubtype.None,
        CraftingSubtype craftingSubtype = CraftingSubtype.None)
    {
        ItemDefinition definition = ScriptableObject.CreateInstance<ItemDefinition>();
        definition.name = displayName;

        SetField(definition, "id", id);
        SetField(definition, "displayName", displayName);
        SetField(definition, "shortName", displayName);
        SetField(definition, "category", category);
        SetField(definition, "maxStack", maxStack);
        SetField(definition, "equipmentSlotType", equipmentSlotType);
        SetField(definition, "weaponType", weaponType);
        SetField(definition, "trophySubtype", trophySubtype);
        SetField(definition, "craftingSubtype", craftingSubtype);

        definitions.Add(definition);
        return definition;
    }

    private static void SetField<T>(ItemDefinition definition, string fieldName, T value)
    {
        FieldInfo field = typeof(ItemDefinition).GetField(fieldName, FieldFlags);

        if (field == null)
            throw new MissingFieldException(typeof(ItemDefinition).Name, fieldName);

        field.SetValue(definition, value);
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }
}
