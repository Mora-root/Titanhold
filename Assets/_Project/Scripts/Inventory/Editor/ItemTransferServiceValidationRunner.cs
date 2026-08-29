using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public static class ItemTransferServiceValidationRunner
{
    private const string MenuPath = "Tools/Titanhold/Validate ItemTransferService";
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
            Debug.LogError($"ItemTransferService validation failed: {exception}");
        }
    }

    public static string RunValidation()
    {
        List<ItemDefinition> definitions = new List<ItemDefinition>();

        try
        {
            ItemDefinition crystalShard = CreateDefinition(
                definitions,
                "item_transfer_crystal_shard",
                "Item Transfer Crystal Shard",
                ItemCategory.Crafting,
                99);

            ItemDefinition sword = CreateDefinition(
                definitions,
                "item_transfer_sword",
                "Item Transfer Sword",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandSword);

            ItemDefinition axe = CreateDefinition(
                definitions,
                "item_transfer_axe",
                "Item Transfer Axe",
                ItemCategory.Equipment,
                1,
                EquipmentSlotType.Weapon,
                WeaponType.OneHandAxe);

            ValidateMoveToEmpty(crystalShard);
            ValidateNonStackableSwapPreservesIdentity(sword, axe);
            ValidatePartialStackableMerge(crystalShard);
            ValidateFullStackableMerge(crystalShard);
            ValidateFullTargetSameStackFails(crystalShard);
            ValidateIncompatibleOccupiedTargetSwap(sword, axe);
            ValidateInvalidSourceTarget(crystalShard);
            ValidateSameSourceTarget(crystalShard);
            ValidateCrossCategoryInvalid(sword);

            return "ItemTransferService validation passed.";
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

    private static void ValidateMoveToEmpty(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemTransferService service = new ItemTransferService();
        ItemSlot source = container.GetSlot(ItemCategory.Crafting, 0);
        ItemSlot target = container.GetSlot(ItemCategory.Crafting, 1);
        ItemStack stack = ItemStack.CreateStackable(crystalShard, 20);

        source.Set(stack);

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 1));

        Assert(result.Success, "Move to empty slot failed.");
        Assert(result.MovedAmount == 20, "Move to empty slot moved amount mismatch.");
        Assert(source.IsEmpty, "Source should be empty after move.");
        Assert(ReferenceEquals(target.Stack, stack), "Target should contain the exact moved stack reference.");
    }

    private static void ValidateNonStackableSwapPreservesIdentity(ItemDefinition sword, ItemDefinition axe)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemTransferService service = new ItemTransferService();
        ItemInstance swordInstance = new ItemInstance(sword);
        ItemInstance axeInstance = new ItemInstance(axe);
        ItemStack swordStack = ItemStack.CreateNonStackable(swordInstance);
        ItemStack axeStack = ItemStack.CreateNonStackable(axeInstance);
        string swordId = swordInstance.InstanceId;
        string axeId = axeInstance.InstanceId;

        container.GetSlot(ItemCategory.Equipment, 0).Set(swordStack);
        container.GetSlot(ItemCategory.Equipment, 1).Set(axeStack);

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Equipment, 0),
            Address(container, ItemCategory.Equipment, 1));

        Assert(result.Success, "Non-stackable swap failed.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Equipment, 0).Stack, axeStack), "First slot should contain original axe stack.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Equipment, 1).Stack, swordStack), "Second slot should contain original sword stack.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Equipment, 0).Stack.Instance, axeInstance), "Axe instance reference changed.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Equipment, 1).Stack.Instance, swordInstance), "Sword instance reference changed.");
        Assert(container.GetSlot(ItemCategory.Equipment, 0).Stack.Instance.InstanceId == axeId, "Axe instance id changed.");
        Assert(container.GetSlot(ItemCategory.Equipment, 1).Stack.Instance.InstanceId == swordId, "Sword instance id changed.");
    }

    private static void ValidatePartialStackableMerge(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemTransferService service = new ItemTransferService();

        container.GetSlot(ItemCategory.Crafting, 0).Set(ItemStack.CreateStackable(crystalShard, 70));
        container.GetSlot(ItemCategory.Crafting, 1).Set(ItemStack.CreateStackable(crystalShard, 50));

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 1));

        Assert(result.Success, "Partial stackable merge failed.");
        Assert(result.MovedAmount == 49, "Partial stackable merge moved amount mismatch.");
        Assert(container.GetSlot(ItemCategory.Crafting, 0).Stack.Amount == 21, "Partial merge source should keep 21.");
        Assert(container.GetSlot(ItemCategory.Crafting, 1).Stack.Amount == 99, "Partial merge target should be 99.");
    }

    private static void ValidateFullStackableMerge(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemTransferService service = new ItemTransferService();

        container.GetSlot(ItemCategory.Crafting, 0).Set(ItemStack.CreateStackable(crystalShard, 20));
        container.GetSlot(ItemCategory.Crafting, 1).Set(ItemStack.CreateStackable(crystalShard, 50));

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 1));

        Assert(result.Success, "Full stackable merge failed.");
        Assert(result.MovedAmount == 20, "Full stackable merge moved amount mismatch.");
        Assert(container.GetSlot(ItemCategory.Crafting, 0).IsEmpty, "Full merge source should be empty.");
        Assert(container.GetSlot(ItemCategory.Crafting, 1).Stack.Amount == 70, "Full merge target should be 70.");
    }

    private static void ValidateFullTargetSameStackFails(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(2), 0);
        ItemTransferService service = new ItemTransferService();
        ItemStack sourceStack = ItemStack.CreateStackable(crystalShard, 10);
        ItemStack targetStack = ItemStack.CreateStackable(crystalShard, 99);

        container.GetSlot(ItemCategory.Crafting, 0).Set(sourceStack);
        container.GetSlot(ItemCategory.Crafting, 1).Set(targetStack);

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 1));

        Assert(!result.Success, "Full same-stack target should fail.");
        Assert(result.Error == ItemTransferError.CannotMerge, "Full same-stack target should return CannotMerge.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Crafting, 0).Stack, sourceStack), "Full target failure mutated source stack.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Crafting, 1).Stack, targetStack), "Full target failure mutated target stack.");
        Assert(sourceStack.Amount == 10, "Full target failure changed source amount.");
        Assert(targetStack.Amount == 99, "Full target failure changed target amount.");
    }

    private static void ValidateIncompatibleOccupiedTargetSwap(ItemDefinition sword, ItemDefinition axe)
    {
        ValidateNonStackableSwapPreservesIdentity(sword, axe);
    }

    private static void ValidateInvalidSourceTarget(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(1), 0);
        ItemTransferService service = new ItemTransferService();
        ItemStack sourceStack = ItemStack.CreateStackable(crystalShard, 10);
        container.GetSlot(ItemCategory.Crafting, 0).Set(sourceStack);

        ItemTransferResult invalidSource = service.TryTransfer(
            new ItemSlotAddress(null, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 0));

        Assert(!invalidSource.Success, "Invalid source should fail.");
        Assert(invalidSource.Error == ItemTransferError.InvalidSource, "Invalid source error mismatch.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Crafting, 0).Stack, sourceStack), "Invalid source mutated container.");

        ItemTransferResult invalidTarget = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            new ItemSlotAddress(null, ItemCategory.Crafting, 0));

        Assert(!invalidTarget.Success, "Invalid target should fail.");
        Assert(invalidTarget.Error == ItemTransferError.InvalidTarget, "Invalid target error mismatch.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Crafting, 0).Stack, sourceStack), "Invalid target mutated source.");

        ItemTransferResult emptySource = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 1),
            Address(container, ItemCategory.Crafting, 0));

        Assert(!emptySource.Success, "Invalid out-of-range source should fail.");
        Assert(emptySource.Error == ItemTransferError.InvalidSource, "Out-of-range source should return InvalidSource.");
    }

    private static void ValidateSameSourceTarget(ItemDefinition crystalShard)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(1), 0);
        ItemTransferService service = new ItemTransferService();
        ItemStack stack = ItemStack.CreateStackable(crystalShard, 10);

        container.GetSlot(ItemCategory.Crafting, 0).Set(stack);

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Crafting, 0),
            Address(container, ItemCategory.Crafting, 0));

        Assert(!result.Success, "Same source/target should fail.");
        Assert(result.Error == ItemTransferError.SameSlot, "Same source/target should return SameSlot.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Crafting, 0).Stack, stack), "Same source/target mutated stack.");
        Assert(stack.Amount == 10, "Same source/target changed amount.");
    }

    private static void ValidateCrossCategoryInvalid(ItemDefinition sword)
    {
        ItemContainer container = new ItemContainer(CreateCapacities(1), 0);
        ItemTransferService service = new ItemTransferService();
        ItemInstance swordInstance = new ItemInstance(sword);
        ItemStack swordStack = ItemStack.CreateNonStackable(swordInstance);

        container.GetSlot(ItemCategory.Equipment, 0).Set(swordStack);

        ItemTransferResult result = service.TryTransfer(
            Address(container, ItemCategory.Equipment, 0),
            Address(container, ItemCategory.Crafting, 0));

        Assert(!result.Success, "Cross-category transfer should fail.");
        Assert(result.Error == ItemTransferError.TargetRejectsSource, "Cross-category transfer should return TargetRejectsSource.");
        Assert(ReferenceEquals(container.GetSlot(ItemCategory.Equipment, 0).Stack, swordStack), "Cross-category transfer mutated source.");
        Assert(container.GetSlot(ItemCategory.Crafting, 0).IsEmpty, "Cross-category transfer mutated target.");
        Assert(ReferenceEquals(swordStack.Instance, swordInstance), "Cross-category transfer changed instance reference.");
    }

    private static ItemSlotAddress Address(ItemContainer container, ItemCategory category, int slotIndex)
    {
        return new ItemSlotAddress(container, category, slotIndex);
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
