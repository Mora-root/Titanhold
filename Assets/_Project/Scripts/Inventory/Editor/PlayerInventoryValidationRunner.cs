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
            ValidateExistingInstanceAdd(inventory, sword);
            ValidateExistingStackableAdd(inventory, crystalShard);

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

    private static void ValidateExistingInstanceAdd(PlayerInventory inventory, ItemDefinition sword)
    {
        ItemInstance instance = new ItemInstance(sword);
        string instanceId = instance.InstanceId;
        ItemStack stack = ItemStack.CreateNonStackable(instance);

        AddItemResult stackResult = inventory.TryAdd(stack);
        Assert(stackResult.FullyAdded, "PlayerInventory did not add existing non-stackable stack.");

        ItemStack storedStack = FindStackByInstanceId(inventory.GetSection(ItemCategory.Equipment), instanceId);
        Assert(storedStack != null, "PlayerInventory did not store existing instance.");
        Assert(ReferenceEquals(storedStack.Instance, instance), "PlayerInventory should keep the exact existing instance reference.");
        Assert(storedStack.Instance.InstanceId == instanceId, "PlayerInventory changed existing instance id.");

        ItemInstance secondInstance = new ItemInstance(sword);
        string secondInstanceId = secondInstance.InstanceId;
        AddItemResult instanceResult = inventory.TryAddInstance(secondInstance);
        Assert(instanceResult.FullyAdded, "PlayerInventory.TryAddInstance did not add existing instance.");

        ItemStack secondStoredStack = FindStackByInstanceId(inventory.GetSection(ItemCategory.Equipment), secondInstanceId);
        Assert(secondStoredStack != null, "PlayerInventory.TryAddInstance did not store existing instance.");
        Assert(ReferenceEquals(secondStoredStack.Instance, secondInstance), "PlayerInventory.TryAddInstance should keep the exact existing instance reference.");
        Assert(secondStoredStack.Instance.InstanceId == secondInstanceId, "PlayerInventory.TryAddInstance changed existing instance id.");
    }

    private static void ValidateExistingStackableAdd(PlayerInventory inventory, ItemDefinition crystalShard)
    {
        ItemStack sourceStack = CreateStackableSource(crystalShard, 120);
        ItemContainerSection section = inventory.GetSection(ItemCategory.Crafting);
        int amountBefore = CountAmount(section, crystalShard);

        AddItemResult result = inventory.TryAdd(sourceStack);

        Assert(result.FullyAdded, "PlayerInventory did not fully add existing stackable x120.");
        Assert(result.AddedAmount == 120, "PlayerInventory existing stackable added amount mismatch.");
        Assert(sourceStack.Amount == 120, "PlayerInventory mutated existing stackable input stack.");
        Assert(CountAmount(section, crystalShard) == amountBefore + 120, "PlayerInventory existing stackable total amount mismatch.");
    }

    private static ItemStack FindStackByInstanceId(ItemContainerSection section, string instanceId)
    {
        if (section == null || string.IsNullOrWhiteSpace(instanceId))
            return null;

        for (int i = 0; i < section.Capacity; i++)
        {
            ItemStack stack = section.GetSlot(i).Stack;
            if (stack != null && stack.Instance != null && stack.Instance.InstanceId == instanceId)
                return stack;
        }

        return null;
    }

    private static int CountAmount(ItemContainerSection section, ItemDefinition definition)
    {
        if (section == null || definition == null)
            return 0;

        int total = 0;

        for (int i = 0; i < section.Capacity; i++)
        {
            ItemStack stack = section.GetSlot(i).Stack;

            if (stack != null && ReferenceEquals(stack.Definition, definition))
                total += stack.Amount;
        }

        return total;
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

    private static ItemStack CreateStackableSource(ItemDefinition definition, int amount)
    {
        ConstructorInfo constructor = typeof(ItemStack).GetConstructor(
            BindingFlags.Instance | BindingFlags.NonPublic,
            null,
            new[] { typeof(ItemDefinition), typeof(int), typeof(ItemInstance) },
            null);

        if (constructor == null)
            throw new MissingMethodException(typeof(ItemStack).Name, ".ctor");

        return (ItemStack)constructor.Invoke(new object[] { definition, amount, null });
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
