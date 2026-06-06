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

            ItemDefinition axe = CreateDefinition(
                definitions,
                "axe_player_inventory_test",
                "Axe PlayerInventory Test",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandAxe);

            ValidateSections(inventory);
            ValidateEvents(inventory, crystalShard);
            ValidateCategoryRouting(inventory, monsterFang, sword);
            ValidateExistingInstanceAdd(inventory, sword);
            ValidateExistingStackableAdd(inventory, crystalShard);
            ValidateTransferBridge(crystalShard, sword, axe);

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

    private static void ValidateTransferBridge(
        ItemDefinition crystalShard,
        ItemDefinition sword,
        ItemDefinition axe)
    {
        ValidateTransferMergeEvents(crystalShard);
        ValidateTransferInvalidNoEvents(sword);
        ValidateTransferSameSlotNoEvents(crystalShard);
        ValidateTransferSwapPreservesIdentity(sword, axe);
    }

    private static void ValidateTransferMergeEvents(ItemDefinition crystalShard)
    {
        using (ValidationInventoryScope scope = new ValidationInventoryScope())
        {
            PlayerInventory inventory = scope.Inventory;
            inventory.GetSlot(ItemCategory.Crafting, 0).Set(ItemStack.CreateStackable(crystalShard, 20));
            inventory.GetSlot(ItemCategory.Crafting, 1).Set(ItemStack.CreateStackable(crystalShard, 50));

            int changedCount = 0;
            List<ItemCategory> sectionChanges = new List<ItemCategory>();
            inventory.Changed += () => changedCount++;
            inventory.SectionChanged += category => sectionChanges.Add(category);

            ItemTransferResult result = inventory.TryTransfer(
                ItemCategory.Crafting,
                0,
                ItemCategory.Crafting,
                1);

            Assert(result.Success, "PlayerInventory.TryTransfer stackable merge failed.");
            Assert(result.MovedAmount == 20, "PlayerInventory.TryTransfer merge moved amount mismatch.");
            Assert(inventory.GetSlot(ItemCategory.Crafting, 0).IsEmpty, "PlayerInventory.TryTransfer merge source should be empty.");
            Assert(inventory.GetSlot(ItemCategory.Crafting, 1).Stack.Amount == 70, "PlayerInventory.TryTransfer merge target amount mismatch.");
            Assert(changedCount == 1, "Changed should fire once after successful transfer.");
            Assert(sectionChanges.Count == 1, "SectionChanged should fire once after same-section transfer.");
            Assert(sectionChanges[0] == ItemCategory.Crafting, "SectionChanged should report Crafting after transfer.");
        }
    }

    private static void ValidateTransferInvalidNoEvents(ItemDefinition sword)
    {
        using (ValidationInventoryScope scope = new ValidationInventoryScope())
        {
            PlayerInventory inventory = scope.Inventory;
            ItemInstance swordInstance = new ItemInstance(sword);
            ItemStack swordStack = ItemStack.CreateNonStackable(swordInstance);
            inventory.GetSlot(ItemCategory.Equipment, 0).Set(swordStack);

            int changedCount = 0;
            int sectionChangedCount = 0;
            inventory.Changed += () => changedCount++;
            inventory.SectionChanged += _ => sectionChangedCount++;

            ItemTransferResult result = inventory.TryTransfer(
                ItemCategory.Equipment,
                0,
                ItemCategory.Crafting,
                0);

            Assert(!result.Success, "Invalid cross-section transfer should fail.");
            Assert(result.Error == ItemTransferError.TargetRejectsSource, "Invalid cross-section transfer error mismatch.");
            Assert(changedCount == 0, "Changed should not fire after failed transfer.");
            Assert(sectionChangedCount == 0, "SectionChanged should not fire after failed transfer.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Equipment, 0).Stack, swordStack), "Failed transfer should not mutate source.");
            Assert(inventory.GetSlot(ItemCategory.Crafting, 0).IsEmpty, "Failed transfer should not mutate target.");
        }
    }

    private static void ValidateTransferSameSlotNoEvents(ItemDefinition crystalShard)
    {
        using (ValidationInventoryScope scope = new ValidationInventoryScope())
        {
            PlayerInventory inventory = scope.Inventory;
            ItemStack stack = ItemStack.CreateStackable(crystalShard, 10);
            inventory.GetSlot(ItemCategory.Crafting, 0).Set(stack);

            int changedCount = 0;
            int sectionChangedCount = 0;
            inventory.Changed += () => changedCount++;
            inventory.SectionChanged += _ => sectionChangedCount++;

            ItemTransferResult result = inventory.TryTransfer(
                ItemCategory.Crafting,
                0,
                ItemCategory.Crafting,
                0);

            Assert(!result.Success, "Same-slot transfer should fail.");
            Assert(result.Error == ItemTransferError.SameSlot, "Same-slot transfer error mismatch.");
            Assert(changedCount == 0, "Changed should not fire after same-slot transfer.");
            Assert(sectionChangedCount == 0, "SectionChanged should not fire after same-slot transfer.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Crafting, 0).Stack, stack), "Same-slot transfer should not mutate stack.");
        }
    }

    private static void ValidateTransferSwapPreservesIdentity(
        ItemDefinition sword,
        ItemDefinition axe)
    {
        using (ValidationInventoryScope scope = new ValidationInventoryScope())
        {
            PlayerInventory inventory = scope.Inventory;
            ItemInstance swordInstance = new ItemInstance(sword);
            ItemInstance axeInstance = new ItemInstance(axe);
            ItemStack swordStack = ItemStack.CreateNonStackable(swordInstance);
            ItemStack axeStack = ItemStack.CreateNonStackable(axeInstance);
            string swordId = swordInstance.InstanceId;
            string axeId = axeInstance.InstanceId;

            inventory.GetSlot(ItemCategory.Equipment, 0).Set(swordStack);
            inventory.GetSlot(ItemCategory.Equipment, 1).Set(axeStack);

            int changedCount = 0;
            List<ItemCategory> sectionChanges = new List<ItemCategory>();
            inventory.Changed += () => changedCount++;
            inventory.SectionChanged += category => sectionChanges.Add(category);

            ItemTransferResult result = inventory.TryTransfer(
                ItemCategory.Equipment,
                0,
                ItemCategory.Equipment,
                1);

            Assert(result.Success, "PlayerInventory.TryTransfer non-stackable swap failed.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Equipment, 0).Stack, axeStack), "Swap should preserve target stack reference.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Equipment, 1).Stack, swordStack), "Swap should preserve source stack reference.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Equipment, 0).Stack.Instance, axeInstance), "Swap changed axe instance reference.");
            Assert(ReferenceEquals(inventory.GetSlot(ItemCategory.Equipment, 1).Stack.Instance, swordInstance), "Swap changed sword instance reference.");
            Assert(inventory.GetSlot(ItemCategory.Equipment, 0).Stack.Instance.InstanceId == axeId, "Swap changed axe instance id.");
            Assert(inventory.GetSlot(ItemCategory.Equipment, 1).Stack.Instance.InstanceId == swordId, "Swap changed sword instance id.");
            Assert(changedCount == 1, "Changed should fire once after successful swap.");
            Assert(sectionChanges.Count == 1, "SectionChanged should fire once after same-section swap.");
            Assert(sectionChanges[0] == ItemCategory.Equipment, "SectionChanged should report Equipment after swap.");
        }
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

    private sealed class ValidationInventoryScope : IDisposable
    {
        private readonly GameObject temporaryObject;

        public ValidationInventoryScope()
        {
            temporaryObject = EditorUtility.CreateGameObjectWithHideFlags(
                "PlayerInventoryTransferValidation_Temporary",
                HideFlags.HideAndDontSave);

            Inventory = temporaryObject.AddComponent<PlayerInventory>();
            Inventory.EnsureInitialized();
        }

        public PlayerInventory Inventory { get; }

        public void Dispose()
        {
            if (temporaryObject != null)
                UnityEngine.Object.DestroyImmediate(temporaryObject);
        }
    }
}
