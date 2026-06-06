using System;
using System.Collections.Generic;

public sealed class EquipmentService
{
    private readonly PlayerInventory inventory;
    private readonly CharacterEquipment equipment;

    public EquipmentService(PlayerInventory inventory, CharacterEquipment equipment)
    {
        this.inventory = inventory;
        this.equipment = equipment;
    }

    public EquipmentOperationResult TryEquipFromInventory(ItemCategory category, int slotIndex)
    {
        EquipmentOperationResult validation = ValidateEquipSource(category, slotIndex, out ItemStack sourceStack, out ItemInstance instance);
        if (!validation.Success)
            return validation;

        if (!TryResolveTargetSlot(instance, out EquipmentSlotId targetSlot))
        {
            return EquipmentOperationResult.Failed(
                EquipmentOperationError.InvalidTargetSlot,
                message: "Could not resolve a valid equipment slot.");
        }

        return TryEquipFromInventory(category, slotIndex, sourceStack, instance, targetSlot);
    }

    public EquipmentOperationResult TryEquipFromInventory(ItemCategory category, int slotIndex, EquipmentSlotId preferredSlot)
    {
        EquipmentOperationResult validation = ValidateEquipSource(category, slotIndex, out ItemStack sourceStack, out ItemInstance instance);
        if (!validation.Success)
            return validation;

        if (!IsTargetSlotCompatible(instance.Definition, preferredSlot))
        {
            return EquipmentOperationResult.Failed(
                EquipmentOperationError.InvalidTargetSlot,
                preferredSlot,
                message: "Preferred slot is not compatible with the item.");
        }

        return TryEquipFromInventory(category, slotIndex, sourceStack, instance, preferredSlot);
    }

    public EquipmentOperationResult TryUnequipToInventory(EquipmentSlotId slotId)
    {
        if (inventory == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingInventory, slotId);

        if (equipment == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingEquipment, slotId);

        ItemInstance instance = equipment.GetEquipped(slotId);
        if (instance == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.EmptyInventorySlot, slotId);

        if (inventory.CountFreeSlots(ItemCategory.Equipment) < 1)
            return EquipmentOperationResult.Failed(EquipmentOperationError.InventoryFull, slotId, instance);

        if (ShouldNormalizeOffHandWeaponAfterMainHandUnequip(slotId))
            return TryUnequipMainHandAndNormalizeOffHand(instance);

        ItemInstance cleared = equipment.ClearSlot(slotId);
        if (!ReferenceEquals(cleared, instance))
            return EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, slotId, instance);

        AddItemResult addResult = inventory.TryAddInstance(instance);
        if (addResult.FullyAdded)
            return EquipmentOperationResult.Succeeded(slotId, null, new[] { instance });

        bool restored = equipment.TrySetSlot(slotId, instance);
        return restored
            ? EquipmentOperationResult.Failed(EquipmentOperationError.CannotReturnReplacedItem, slotId, instance)
            : EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, slotId, instance);
    }

    private bool ShouldNormalizeOffHandWeaponAfterMainHandUnequip(EquipmentSlotId slotId)
    {
        return slotId == EquipmentSlotId.MainHand &&
               IsOneHandWeapon(equipment.GetDefinition(EquipmentSlotId.OffHand));
    }

    private EquipmentOperationResult TryUnequipMainHandAndNormalizeOffHand(ItemInstance mainHandInstance)
    {
        ItemInstance offHandInstance = equipment.GetEquipped(EquipmentSlotId.OffHand);
        if (offHandInstance == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, EquipmentSlotId.MainHand, mainHandInstance);

        ItemInstance clearedMainHand = equipment.ClearSlot(EquipmentSlotId.MainHand);
        if (!ReferenceEquals(clearedMainHand, mainHandInstance))
            return EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, EquipmentSlotId.MainHand, mainHandInstance);

        ItemInstance clearedOffHand = equipment.ClearSlot(EquipmentSlotId.OffHand);
        if (!ReferenceEquals(clearedOffHand, offHandInstance))
        {
            RestoreMainHandAfterFailedNormalization(mainHandInstance);
            return EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, EquipmentSlotId.MainHand, mainHandInstance);
        }

        if (!equipment.TrySetSlot(EquipmentSlotId.MainHand, offHandInstance))
        {
            bool restored = RestoreDualWieldState(mainHandInstance, offHandInstance);
            return restored
                ? EquipmentOperationResult.Failed(EquipmentOperationError.CannotSetEquipmentSlot, EquipmentSlotId.MainHand, mainHandInstance)
                : EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, EquipmentSlotId.MainHand, mainHandInstance);
        }

        AddItemResult addResult = inventory.TryAddInstance(mainHandInstance);
        if (addResult.FullyAdded)
            return EquipmentOperationResult.Succeeded(EquipmentSlotId.MainHand, null, new[] { mainHandInstance });

        bool rolledBack = RollbackMainHandNormalization(mainHandInstance, offHandInstance);
        return rolledBack
            ? EquipmentOperationResult.Failed(EquipmentOperationError.CannotReturnReplacedItem, EquipmentSlotId.MainHand, mainHandInstance)
            : EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, EquipmentSlotId.MainHand, mainHandInstance);
    }

    private bool RollbackMainHandNormalization(ItemInstance mainHandInstance, ItemInstance offHandInstance)
    {
        ItemInstance clearedMainHand = equipment.ClearSlot(EquipmentSlotId.MainHand);
        if (!ReferenceEquals(clearedMainHand, offHandInstance))
            return false;

        return RestoreDualWieldState(mainHandInstance, offHandInstance);
    }

    private bool RestoreDualWieldState(ItemInstance mainHandInstance, ItemInstance offHandInstance)
    {
        return equipment.TrySetSlot(EquipmentSlotId.MainHand, mainHandInstance) &&
               equipment.TrySetSlot(EquipmentSlotId.OffHand, offHandInstance);
    }

    private bool RestoreMainHandAfterFailedNormalization(ItemInstance mainHandInstance)
    {
        return equipment.GetEquipped(EquipmentSlotId.MainHand) != null ||
               equipment.TrySetSlot(EquipmentSlotId.MainHand, mainHandInstance);
    }

    private EquipmentOperationResult ValidateEquipSource(
        ItemCategory category,
        int slotIndex,
        out ItemStack sourceStack,
        out ItemInstance instance)
    {
        sourceStack = null;
        instance = null;

        if (inventory == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingInventory);

        if (equipment == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingEquipment);

        ItemSlot sourceSlot = inventory.GetSlot(category, slotIndex);
        if (sourceSlot == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.InvalidInventorySlot);

        if (sourceSlot.IsEmpty || sourceSlot.Stack == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.EmptyInventorySlot);

        sourceStack = sourceSlot.Stack;
        ItemDefinition definition = sourceStack.Definition;

        if (definition == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingItemInstance);

        if (definition.Category != ItemCategory.Equipment)
            return EquipmentOperationResult.Failed(EquipmentOperationError.ItemNotEquippable);

        if (definition.MaxStack > 1 || sourceStack.Amount != 1)
            return EquipmentOperationResult.Failed(EquipmentOperationError.StackableItemCannotBeEquipped);

        instance = sourceStack.Instance;
        if (instance == null)
            return EquipmentOperationResult.Failed(EquipmentOperationError.MissingItemInstance);

        if (instance.Definition == null || !instance.Definition.IsEquippable)
            return EquipmentOperationResult.Failed(EquipmentOperationError.ItemNotEquippable);

        return EquipmentOperationResult.Succeeded(default, instance);
    }

    private EquipmentOperationResult TryEquipFromInventory(
        ItemCategory category,
        int slotIndex,
        ItemStack sourceStack,
        ItemInstance instance,
        EquipmentSlotId targetSlot)
    {
        List<EquippedSlotSnapshot> conflicts = CollectConflicts(instance.Definition, targetSlot);

        int availableReturnSlots = inventory.CountFreeSlots(ItemCategory.Equipment) + 1;
        if (availableReturnSlots < conflicts.Count)
        {
            return EquipmentOperationResult.Failed(
                EquipmentOperationError.InventoryFull,
                targetSlot,
                instance,
                GetInstances(conflicts));
        }

        ItemStack takenStack = inventory.TakeStack(category, slotIndex);
        if (!ReferenceEquals(takenStack, sourceStack))
        {
            if (takenStack != null)
                inventory.SetStack(category, slotIndex, takenStack);

            return EquipmentOperationResult.Failed(
                EquipmentOperationError.InvalidInventorySlot,
                targetSlot,
                instance,
                message: "Source inventory slot changed before equip.");
        }

        List<EquippedSlotSnapshot> clearedSlots = ClearConflicts(conflicts);

        if (!equipment.TrySetSlot(targetSlot, instance))
        {
            return RollbackAfterSetFailure(category, slotIndex, sourceStack, clearedSlots, targetSlot, instance);
        }

        EquipmentOperationResult returnResult = ReturnConflictsToInventory(clearedSlots, targetSlot, instance);
        if (!returnResult.Success)
        {
            return RollbackAfterReturnFailure(category, slotIndex, sourceStack, clearedSlots, targetSlot, instance, returnResult.Error);
        }

        return EquipmentOperationResult.Succeeded(targetSlot, instance, GetInstances(clearedSlots));
    }

    private EquipmentOperationResult RollbackAfterSetFailure(
        ItemCategory sourceCategory,
        int sourceSlotIndex,
        ItemStack sourceStack,
        List<EquippedSlotSnapshot> clearedSlots,
        EquipmentSlotId targetSlot,
        ItemInstance instance)
    {
        bool restoredInventory = inventory.SetStack(sourceCategory, sourceSlotIndex, sourceStack);
        bool restoredEquipment = RestoreEquipment(clearedSlots);

        return restoredInventory && restoredEquipment
            ? EquipmentOperationResult.Failed(EquipmentOperationError.CannotSetEquipmentSlot, targetSlot, instance, GetInstances(clearedSlots))
            : EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, targetSlot, instance, GetInstances(clearedSlots));
    }

    private EquipmentOperationResult RollbackAfterReturnFailure(
        ItemCategory sourceCategory,
        int sourceSlotIndex,
        ItemStack sourceStack,
        List<EquippedSlotSnapshot> clearedSlots,
        EquipmentSlotId targetSlot,
        ItemInstance instance,
        EquipmentOperationError originalError)
    {
        ItemInstance equippedItem = equipment.GetEquipped(targetSlot);
        bool clearedEquipped = equippedItem == null || ReferenceEquals(equipment.ClearSlot(targetSlot), instance);
        bool restoredInventory = inventory.SetStack(sourceCategory, sourceSlotIndex, sourceStack);
        bool restoredEquipment = RestoreEquipment(clearedSlots);

        return clearedEquipped && restoredInventory && restoredEquipment
            ? EquipmentOperationResult.Failed(originalError, targetSlot, instance, GetInstances(clearedSlots))
            : EquipmentOperationResult.Failed(EquipmentOperationError.RollbackFailed, targetSlot, instance, GetInstances(clearedSlots));
    }

    private EquipmentOperationResult ReturnConflictsToInventory(
        List<EquippedSlotSnapshot> clearedSlots,
        EquipmentSlotId targetSlot,
        ItemInstance equippedInstance)
    {
        foreach (EquippedSlotSnapshot clearedSlot in clearedSlots)
        {
            AddItemResult result = inventory.TryAddInstance(clearedSlot.Item);
            if (!result.FullyAdded)
            {
                return EquipmentOperationResult.Failed(
                    EquipmentOperationError.CannotReturnReplacedItem,
                    targetSlot,
                    equippedInstance,
                    GetInstances(clearedSlots));
            }
        }

        return EquipmentOperationResult.Succeeded(targetSlot, equippedInstance, GetInstances(clearedSlots));
    }

    private List<EquippedSlotSnapshot> ClearConflicts(List<EquippedSlotSnapshot> conflicts)
    {
        List<EquippedSlotSnapshot> clearedSlots = new List<EquippedSlotSnapshot>();

        foreach (EquippedSlotSnapshot conflict in conflicts)
        {
            ItemInstance cleared = equipment.ClearSlot(conflict.SlotId);
            if (cleared != null)
                clearedSlots.Add(new EquippedSlotSnapshot(conflict.SlotId, cleared));
        }

        return clearedSlots;
    }

    private bool RestoreEquipment(List<EquippedSlotSnapshot> clearedSlots)
    {
        bool restored = true;

        foreach (EquippedSlotSnapshot clearedSlot in clearedSlots)
        {
            if (!equipment.TrySetSlot(clearedSlot.SlotId, clearedSlot.Item))
                restored = false;
        }

        return restored;
    }

    private List<EquippedSlotSnapshot> CollectConflicts(ItemDefinition definition, EquipmentSlotId targetSlot)
    {
        List<EquippedSlotSnapshot> conflicts = new List<EquippedSlotSnapshot>();

        AddConflict(conflicts, targetSlot);

        if (definition.IsWeapon && definition.OccupiesBothHands)
        {
            AddConflict(conflicts, EquipmentSlotId.MainHand);
            AddConflict(conflicts, EquipmentSlotId.OffHand);
        }
        else if (IsOneHandWeapon(definition))
        {
            ItemDefinition offHandDefinition = equipment.GetDefinition(EquipmentSlotId.OffHand);
            if (targetSlot == EquipmentSlotId.MainHand &&
                IsOneHandWeapon(offHandDefinition) &&
                !AreDualWieldCompatible(definition, offHandDefinition))
            {
                AddConflict(conflicts, EquipmentSlotId.OffHand);
            }
        }
        else if (definition.IsShield)
        {
            ItemDefinition mainHandDefinition = equipment.GetDefinition(EquipmentSlotId.MainHand);
            if (mainHandDefinition != null && mainHandDefinition.OccupiesBothHands)
                AddConflict(conflicts, EquipmentSlotId.MainHand);
        }

        return conflicts;
    }

    private void AddConflict(List<EquippedSlotSnapshot> conflicts, EquipmentSlotId slotId)
    {
        foreach (EquippedSlotSnapshot conflict in conflicts)
        {
            if (conflict.SlotId == slotId)
                return;
        }

        ItemInstance item = equipment.GetEquipped(slotId);
        if (item != null)
            conflicts.Add(new EquippedSlotSnapshot(slotId, item));
    }

    private bool TryResolveTargetSlot(ItemInstance instance, out EquipmentSlotId targetSlot)
    {
        targetSlot = default;

        if (instance == null || instance.Definition == null)
            return false;

        ItemDefinition definition = instance.Definition;

        switch (definition.EquipmentSlotType)
        {
            case EquipmentSlotType.Weapon:
                return TryResolveWeaponTargetSlot(definition, out targetSlot);
            case EquipmentSlotType.Shield:
                targetSlot = EquipmentSlotId.OffHand;
                return true;
            case EquipmentSlotType.Head:
                targetSlot = EquipmentSlotId.Head;
                return true;
            case EquipmentSlotType.Chest:
                targetSlot = EquipmentSlotId.Chest;
                return true;
            case EquipmentSlotType.Hands:
                targetSlot = EquipmentSlotId.Hands;
                return true;
            case EquipmentSlotType.Legs:
                targetSlot = EquipmentSlotId.Legs;
                return true;
            case EquipmentSlotType.Feet:
                targetSlot = EquipmentSlotId.Feet;
                return true;
            case EquipmentSlotType.Amulet:
                targetSlot = EquipmentSlotId.Amulet;
                return true;
            case EquipmentSlotType.Ring:
                if (!equipment.IsOccupied(EquipmentSlotId.Ring1))
                    targetSlot = EquipmentSlotId.Ring1;
                else if (!equipment.IsOccupied(EquipmentSlotId.Ring2))
                    targetSlot = EquipmentSlotId.Ring2;
                else
                    targetSlot = EquipmentSlotId.Ring1;

                return true;
            case EquipmentSlotType.Artifact:
                targetSlot = EquipmentSlotId.Artifact;
                return true;
            default:
                return false;
        }
    }

    private bool TryResolveWeaponTargetSlot(ItemDefinition definition, out EquipmentSlotId targetSlot)
    {
        targetSlot = EquipmentSlotId.MainHand;

        if (definition == null || !definition.IsWeapon)
            return false;

        if (definition.OccupiesBothHands)
            return true;

        if (!IsOneHandWeapon(definition))
            return false;

        ItemDefinition mainHandDefinition = equipment.GetDefinition(EquipmentSlotId.MainHand);
        if (mainHandDefinition == null)
            return true;

        if (AreDualWieldCompatible(definition, mainHandDefinition) &&
            !equipment.IsOccupied(EquipmentSlotId.OffHand))
        {
            targetSlot = EquipmentSlotId.OffHand;
            return true;
        }

        targetSlot = EquipmentSlotId.MainHand;
        return true;
    }

    private bool IsTargetSlotCompatible(ItemDefinition definition, EquipmentSlotId targetSlot)
    {
        if (definition == null || !definition.IsEquippable)
            return false;

        switch (definition.EquipmentSlotType)
        {
            case EquipmentSlotType.Weapon:
                if (definition.OccupiesBothHands)
                    return targetSlot == EquipmentSlotId.MainHand;

                if (targetSlot == EquipmentSlotId.OffHand)
                    return CanEquipOneHandWeaponToOffHand(definition);

                return targetSlot == EquipmentSlotId.MainHand || targetSlot == EquipmentSlotId.OffHand;
            case EquipmentSlotType.Shield:
                return targetSlot == EquipmentSlotId.OffHand;
            case EquipmentSlotType.Head:
                return targetSlot == EquipmentSlotId.Head;
            case EquipmentSlotType.Chest:
                return targetSlot == EquipmentSlotId.Chest;
            case EquipmentSlotType.Hands:
                return targetSlot == EquipmentSlotId.Hands;
            case EquipmentSlotType.Legs:
                return targetSlot == EquipmentSlotId.Legs;
            case EquipmentSlotType.Feet:
                return targetSlot == EquipmentSlotId.Feet;
            case EquipmentSlotType.Amulet:
                return targetSlot == EquipmentSlotId.Amulet;
            case EquipmentSlotType.Ring:
                return targetSlot == EquipmentSlotId.Ring1 || targetSlot == EquipmentSlotId.Ring2;
            case EquipmentSlotType.Artifact:
                return targetSlot == EquipmentSlotId.Artifact;
            default:
                return false;
        }
    }

    private bool CanEquipOneHandWeaponToOffHand(ItemDefinition definition)
    {
        if (!IsOneHandWeapon(definition))
            return false;

        ItemDefinition mainHandDefinition = equipment.GetDefinition(EquipmentSlotId.MainHand);
        return AreDualWieldCompatible(definition, mainHandDefinition);
    }

    private static bool IsOneHandWeapon(ItemDefinition definition)
    {
        return definition != null &&
               definition.IsWeapon &&
               definition.Handedness == WeaponHandedness.OneHand &&
               definition.WeaponFamily != WeaponFamily.None;
    }

    private static bool AreDualWieldCompatible(ItemDefinition first, ItemDefinition second)
    {
        return IsOneHandWeapon(first) &&
               IsOneHandWeapon(second) &&
               first.WeaponFamily == second.WeaponFamily;
    }

    private static IReadOnlyList<ItemInstance> GetInstances(List<EquippedSlotSnapshot> slots)
    {
        if (slots == null || slots.Count == 0)
            return Array.Empty<ItemInstance>();

        ItemInstance[] instances = new ItemInstance[slots.Count];

        for (int i = 0; i < slots.Count; i++)
        {
            instances[i] = slots[i].Item;
        }

        return instances;
    }

    private readonly struct EquippedSlotSnapshot
    {
        public EquippedSlotSnapshot(EquipmentSlotId slotId, ItemInstance item)
        {
            SlotId = slotId;
            Item = item;
        }

        public EquipmentSlotId SlotId { get; }
        public ItemInstance Item { get; }
    }
}
