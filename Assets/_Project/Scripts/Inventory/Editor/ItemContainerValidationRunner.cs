using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ItemContainerValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate Runtime Inventory Model";
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
            Debug.LogError($"Runtime inventory model validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        List<ItemDefinition> definitions = new List<ItemDefinition>();

        try
        {
            ItemDefinition crystalShard = CreateDefinition(
                definitions,
                "crystal_shard_test",
                "Crystal Shard Test",
                ItemCategory.Crafting,
                99
            );

            ItemDefinition monsterFang = CreateDefinition(
                definitions,
                "monster_fang_test",
                "Monster Fang Test",
                ItemCategory.Trophy,
                99
            );

            ItemDefinition sword = CreateDefinition(
                definitions,
                "sword_test_runtime",
                "Sword Runtime Test",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword
            );

            Dictionary<ItemCategory, int> capacities = CreateCapacities(4);
            ItemContainer container = new ItemContainer(capacities, 0);

            ValidateSections(container, capacities);
            ValidateCategoryRouting(container, crystalShard, monsterFang, sword);
            ValidateStackableAdd(container, crystalShard);
            ValidateNonStackableAdd(container, sword);
            ValidateExistingNonStackableAdd(sword);
            ValidateExistingStackableAdd(crystalShard);
            ValidateExistingStackablePartialAdd(crystalShard);
            ValidateInvalidExistingCategoryInsert(monsterFang);
            ValidateInvalidCategoryInsert(container, monsterFang);
            ValidateMoveSwapMerge(capacities, crystalShard, monsterFang, sword);

            return "Runtime inventory model validation passed.";
        }
        finally
        {
            foreach (ItemDefinition definition in definitions)
            {
                if (definition != null)
                    UnityEngine.Object.DestroyImmediate(definition);
            }
        }
    }

    private static void ValidateSections(ItemContainer container, Dictionary<ItemCategory, int> capacities)
    {
        foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
        {
            ItemContainerSection section = container.GetSection(category);
            Assert(section != null, $"Missing section for {category}.");
            Assert(section.Category == category, $"Section category mismatch for {category}.");
            Assert(section.Slots != null, $"Slots array is null for {category}.");
            Assert(section.Slots.Length == capacities[category], $"Unexpected capacity for {category}.");

            for (int i = 0; i < section.Slots.Length; i++)
            {
                Assert(section.Slots[i] != null, $"Slot {i} in {category} is null.");
                Assert(section.Slots[i].IsEmpty, $"Slot {i} in {category} should start empty.");
            }
        }
    }

    private static void ValidateCategoryRouting(
        ItemContainer container,
        ItemDefinition crystalShard,
        ItemDefinition monsterFang,
        ItemDefinition sword)
    {
        Assert(container.TryAdd(crystalShard, 1).FullyAdded, "Crafting item was not added.");
        Assert(container.CountOccupiedSlots(ItemCategory.Crafting) == 1, "Crafting item did not route to Crafting.");

        Assert(container.TryAdd(monsterFang, 1).FullyAdded, "Trophy item was not added.");
        Assert(container.CountOccupiedSlots(ItemCategory.Trophy) == 1, "Trophy item did not route to Trophy.");

        Assert(container.TryAdd(sword, 1).FullyAdded, "Equipment item was not added.");
        Assert(container.CountOccupiedSlots(ItemCategory.Equipment) == 1, "Equipment item did not route to Equipment.");
    }

    private static void ValidateStackableAdd(ItemContainer container, ItemDefinition crystalShard)
    {
        ItemContainer stackContainer = new ItemContainer(CreateCapacities(4), 0);
        AddItemResult result = stackContainer.TryAdd(crystalShard, 120);
        ItemContainerSection section = stackContainer.GetSection(ItemCategory.Crafting);

        Assert(result.FullyAdded, "Stackable x120 was not fully added.");
        Assert(result.AddedAmount == 120, "Stackable x120 added amount mismatch.");
        Assert(section.GetSlot(0).Stack.Amount == 99, "First stack should contain 99.");
        Assert(section.GetSlot(1).Stack.Amount == 21, "Second stack should contain 21.");
    }

    private static void ValidateNonStackableAdd(ItemContainer container, ItemDefinition sword)
    {
        ItemContainer instanceContainer = new ItemContainer(CreateCapacities(4), 0);
        AddItemResult result = instanceContainer.TryAdd(sword, 3);
        ItemContainerSection section = instanceContainer.GetSection(ItemCategory.Equipment);
        HashSet<string> ids = new HashSet<string>();

        Assert(result.FullyAdded, "Non-stackable x3 was not fully added.");
        Assert(section.CountOccupiedSlots() == 3, "Non-stackable x3 should occupy three slots.");

        for (int i = 0; i < 3; i++)
        {
            ItemStack stack = section.GetSlot(i).Stack;
            Assert(stack != null && stack.Instance != null, $"Equipment stack {i} has no instance.");
            Assert(ids.Add(stack.Instance.InstanceId), $"Duplicate instance id '{stack.Instance.InstanceId}'.");
        }
    }

    private static void ValidateExistingNonStackableAdd(ItemDefinition sword)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemInstance instance = new ItemInstance(sword);
        string instanceId = instance.InstanceId;
        ItemStack stack = ItemStack.CreateNonStackable(instance);

        AddItemResult result = container.TryAdd(stack);
        ItemStack storedStack = container.GetSlot(ItemCategory.Equipment, 0).Stack;

        Assert(result.FullyAdded, "Existing non-stackable stack was not fully added.");
        Assert(storedStack != null, "Existing non-stackable stack was not stored.");
        Assert(ReferenceEquals(storedStack, stack), "Stored stack should be the exact input stack.");
        Assert(ReferenceEquals(storedStack.Instance, instance), "Stored instance should be the exact input instance.");
        Assert(storedStack.Instance.InstanceId == instanceId, "Stored instance id changed.");

        ItemContainer fullContainer = new ItemContainer(CreateCapacities(1), 0);
        Assert(fullContainer.TryAdd(sword, 1).FullyAdded, "Could not prepare full equipment section.");

        ItemInstance rejectedInstance = new ItemInstance(sword);
        ItemStack rejectedStack = ItemStack.CreateNonStackable(rejectedInstance);
        AddItemResult rejectedResult = fullContainer.TryAdd(rejectedStack);

        Assert(!rejectedResult.AddedAnything, "Full section should reject existing non-stackable stack.");
        Assert(rejectedResult.RemainingAmount == 1, "Full section should return one remaining non-stackable item.");
        Assert(ReferenceEquals(rejectedStack.Instance, rejectedInstance), "Rejected existing instance should remain with caller.");
    }

    private static void ValidateExistingStackableAdd(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(4), 0);
        ItemStack sourceStack = CreateStackableSource(crystalShard, 120);

        AddItemResult result = container.TryAdd(sourceStack);
        ItemContainerSection section = container.GetSection(ItemCategory.Crafting);

        Assert(result.FullyAdded, "Existing stackable x120 was not fully added.");
        Assert(result.AddedAmount == 120, "Existing stackable x120 added amount mismatch.");
        Assert(section.GetSlot(0).Stack.Amount == 99, "First existing stackable slot should contain 99.");
        Assert(section.GetSlot(1).Stack.Amount == 21, "Second existing stackable slot should contain 21.");
        Assert(sourceStack.Amount == 120, "Existing stackable input stack should not be mutated.");
        Assert(sourceStack.Instance == null, "Existing stackable input stack should not have an instance.");
    }

    private static void ValidateExistingStackablePartialAdd(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(1), 0);
        ItemStack sourceStack = CreateStackableSource(crystalShard, 120);

        AddItemResult result = container.TryAdd(sourceStack);
        ItemContainerSection section = container.GetSection(ItemCategory.Crafting);

        Assert(!result.FullyAdded, "Existing stackable x120 should not fully fit in one slot.");
        Assert(result.AddedAmount == 99, "Partial existing stackable added amount mismatch.");
        Assert(result.RemainingAmount == 21, "Partial existing stackable remaining amount mismatch.");
        Assert(section.GetSlot(0).Stack.Amount == 99, "Partial existing stackable slot should contain 99.");
        Assert(sourceStack.Amount == 120, "Partial existing stackable input stack should not be mutated.");
    }

    private static void ValidateInvalidExistingCategoryInsert(ItemDefinition monsterFang)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemContainerSection craftingSection = container.GetSection(ItemCategory.Crafting);
        ItemStack wrongCategoryStack = ItemStack.CreateStackable(monsterFang, 1);

        AddItemResult result = craftingSection.TryAddExistingStack(wrongCategoryStack);

        Assert(!result.AddedAnything, "Invalid existing category insert should not add anything.");
        Assert(result.RemainingAmount == 1, "Invalid existing category insert should leave full amount remaining.");
        Assert(craftingSection.CountOccupiedSlots() == 0, "Invalid existing category insert changed section state.");
        Assert(wrongCategoryStack.Amount == 1, "Invalid existing category insert should not mutate input stack.");
    }

    private static void ValidateInvalidCategoryInsert(ItemContainer container, ItemDefinition monsterFang)
    {
        ItemContainerSection craftingSection = container.GetSection(ItemCategory.Crafting);
        int occupiedBefore = craftingSection.CountOccupiedSlots();
        AddItemResult result = craftingSection.TryAdd(monsterFang, 1);

        Assert(!result.AddedAnything, "Invalid category insert should not add anything.");
        Assert(result.RemainingAmount == 1, "Invalid category insert should leave full amount remaining.");
        Assert(craftingSection.CountOccupiedSlots() == occupiedBefore, "Invalid category insert changed section state.");
    }

    private static void ValidateMoveSwapMerge(
        Dictionary<ItemCategory, int> capacities,
        ItemDefinition crystalShard,
        ItemDefinition monsterFang,
        ItemDefinition sword)
    {
        ItemContainer mergeContainer = new ItemContainer(capacities, 0);
        ItemContainerSection craftingSection = mergeContainer.GetSection(ItemCategory.Crafting);
        craftingSection.GetSlot(0).Set(ItemStack.CreateStackable(crystalShard, 70));
        craftingSection.GetSlot(1).Set(ItemStack.CreateStackable(crystalShard, 20));

        Assert(mergeContainer.Move(ItemCategory.Crafting, 1, ItemCategory.Crafting, 0), "Compatible stacks did not merge.");
        Assert(craftingSection.GetSlot(0).Stack.Amount == 90, "Merged stack amount should be 90.");
        Assert(craftingSection.GetSlot(1).IsEmpty, "Merged source slot should be empty.");

        Assert(mergeContainer.Move(ItemCategory.Crafting, 0, ItemCategory.Crafting, 2), "Move to empty slot failed.");
        Assert(craftingSection.GetSlot(0).IsEmpty, "Source slot should be empty after move.");
        Assert(craftingSection.GetSlot(2).Stack.Amount == 90, "Moved stack amount mismatch.");

        ItemContainer swapContainer = new ItemContainer(capacities, 0);
        Assert(swapContainer.TryAdd(sword, 2).FullyAdded, "Could not prepare equipment swap test.");
        ItemContainerSection equipmentSection = swapContainer.GetSection(ItemCategory.Equipment);
        string firstId = equipmentSection.GetSlot(0).Stack.Instance.InstanceId;
        string secondId = equipmentSection.GetSlot(1).Stack.Instance.InstanceId;

        Assert(swapContainer.Move(ItemCategory.Equipment, 0, ItemCategory.Equipment, 1), "Non-stackable swap failed.");
        Assert(equipmentSection.GetSlot(0).Stack.Instance.InstanceId == secondId, "First slot was not swapped.");
        Assert(equipmentSection.GetSlot(1).Stack.Instance.InstanceId == firstId, "Second slot was not swapped.");

        ItemContainer invalidMoveContainer = new ItemContainer(capacities, 0);
        Assert(invalidMoveContainer.TryAdd(monsterFang, 1).FullyAdded, "Could not prepare invalid cross-category move test.");
        Assert(!invalidMoveContainer.Move(ItemCategory.Trophy, 0, ItemCategory.Crafting, 0), "Cross-category move should be rejected.");
    }

    private static Dictionary<ItemCategory, int> CreateCapacities(int capacity)
    {
        Dictionary<ItemCategory, int> capacities = new Dictionary<ItemCategory, int>();

        foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
        {
            capacities[category] = capacity;
        }

        return capacities;
    }

    private static ItemDefinition CreateDefinition(
        List<ItemDefinition> definitions,
        string id,
        string displayName,
        ItemCategory category,
        int maxStack,
        EquipmentSlotType equipmentSlotType = EquipmentSlotType.None,
        WeaponType weaponType = WeaponType.None)
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
