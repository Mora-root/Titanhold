using System;
using System.Collections.Generic;

public readonly struct EquipmentOperationResult
{
    public EquipmentOperationResult(
        bool success,
        EquipmentOperationError error,
        EquipmentSlotId targetSlot,
        ItemInstance equippedInstance,
        IReadOnlyList<ItemInstance> unequippedInstances,
        string message)
    {
        Success = success;
        Error = error;
        TargetSlot = targetSlot;
        EquippedInstance = equippedInstance;
        UnequippedInstances = unequippedInstances ?? Array.Empty<ItemInstance>();
        Message = message;
    }

    public bool Success { get; }
    public EquipmentOperationError Error { get; }
    public EquipmentSlotId TargetSlot { get; }
    public ItemInstance EquippedInstance { get; }
    public IReadOnlyList<ItemInstance> UnequippedInstances { get; }
    public string Message { get; }

    public static EquipmentOperationResult Succeeded(
        EquipmentSlotId targetSlot,
        ItemInstance equippedInstance,
        IReadOnlyList<ItemInstance> unequippedInstances = null,
        string message = null)
    {
        return new EquipmentOperationResult(
            true,
            EquipmentOperationError.None,
            targetSlot,
            equippedInstance,
            unequippedInstances,
            message);
    }

    public static EquipmentOperationResult Failed(
        EquipmentOperationError error,
        EquipmentSlotId targetSlot = default,
        ItemInstance equippedInstance = null,
        IReadOnlyList<ItemInstance> unequippedInstances = null,
        string message = null)
    {
        return new EquipmentOperationResult(
            false,
            error,
            targetSlot,
            equippedInstance,
            unequippedInstances,
            message);
    }
}
