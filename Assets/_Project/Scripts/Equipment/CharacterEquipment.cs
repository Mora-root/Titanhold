using System;
using System.Collections.Generic;

public sealed class CharacterEquipment
{
    private readonly Dictionary<EquipmentSlotId, ItemInstance> slots = new Dictionary<EquipmentSlotId, ItemInstance>();

    public CharacterEquipment()
    {
        foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
        {
            slots[slotId] = null;
        }
    }

    public event Action Changed;
    public event Action<EquipmentSlotId, ItemInstance, ItemInstance> SlotChanged;

    public ItemInstance GetEquipped(EquipmentSlotId slotId)
    {
        return slots.TryGetValue(slotId, out ItemInstance instance) ? instance : null;
    }

    public ItemDefinition GetDefinition(EquipmentSlotId slotId)
    {
        return GetEquipped(slotId)?.Definition;
    }

    public bool IsOccupied(EquipmentSlotId slotId)
    {
        return GetEquipped(slotId) != null;
    }

    public bool TrySetSlot(EquipmentSlotId slotId, ItemInstance instance)
    {
        if (!CanSetSlot(slotId, instance))
            return false;

        slots[slotId] = instance;
        SlotChanged?.Invoke(slotId, null, instance);
        Changed?.Invoke();
        return true;
    }

    public ItemInstance ClearSlot(EquipmentSlotId slotId)
    {
        if (!slots.TryGetValue(slotId, out ItemInstance oldItem) || oldItem == null)
            return null;

        slots[slotId] = null;
        SlotChanged?.Invoke(slotId, oldItem, null);
        Changed?.Invoke();
        return oldItem;
    }

    public void Clear()
    {
        bool changed = false;

        foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
        {
            if (!slots.TryGetValue(slotId, out ItemInstance oldItem) || oldItem == null)
                continue;

            slots[slotId] = null;
            changed = true;
            SlotChanged?.Invoke(slotId, oldItem, null);
        }

        if (changed)
            Changed?.Invoke();
    }

    internal void ReplaceState(CharacterEquipment restoredEquipment)
    {
        if (restoredEquipment == null)
            throw new ArgumentNullException(nameof(restoredEquipment));

        List<(EquipmentSlotId SlotId, ItemInstance OldItem, ItemInstance NewItem)> changes =
            new List<(EquipmentSlotId, ItemInstance, ItemInstance)>();

        foreach (EquipmentSlotId slotId in Enum.GetValues(typeof(EquipmentSlotId)))
        {
            ItemInstance oldItem = GetEquipped(slotId);
            ItemInstance newItem = restoredEquipment.GetEquipped(slotId);
            if (ReferenceEquals(oldItem, newItem))
                continue;

            slots[slotId] = newItem;
            changes.Add((slotId, oldItem, newItem));
        }

        for (int i = 0; i < changes.Count; i++)
        {
            (EquipmentSlotId slotId, ItemInstance oldItem, ItemInstance newItem) =
                changes[i];
            SlotChanged?.Invoke(slotId, oldItem, newItem);
        }

        if (changes.Count > 0)
            Changed?.Invoke();
    }

    private bool CanSetSlot(EquipmentSlotId slotId, ItemInstance instance)
    {
        if (!slots.ContainsKey(slotId))
            return false;

        if (slots[slotId] != null)
            return false;

        if (instance == null || instance.Definition == null)
            return false;

        ItemDefinition definition = instance.Definition;

        if (!definition.IsEquippable)
            return false;

        return IsCompatibleSlot(definition.EquipmentSlotType, slotId);
    }

    private static bool IsCompatibleSlot(EquipmentSlotType equipmentSlotType, EquipmentSlotId slotId)
    {
        switch (equipmentSlotType)
        {
            case EquipmentSlotType.Weapon:
                return slotId == EquipmentSlotId.MainHand || slotId == EquipmentSlotId.OffHand;
            case EquipmentSlotType.Shield:
                return slotId == EquipmentSlotId.OffHand;
            case EquipmentSlotType.Head:
                return slotId == EquipmentSlotId.Head;
            case EquipmentSlotType.Chest:
                return slotId == EquipmentSlotId.Chest;
            case EquipmentSlotType.Hands:
                return slotId == EquipmentSlotId.Hands;
            case EquipmentSlotType.Legs:
                return slotId == EquipmentSlotId.Legs;
            case EquipmentSlotType.Feet:
                return slotId == EquipmentSlotId.Feet;
            case EquipmentSlotType.Amulet:
                return slotId == EquipmentSlotId.Amulet;
            case EquipmentSlotType.Ring:
                return slotId == EquipmentSlotId.Ring1 || slotId == EquipmentSlotId.Ring2;
            case EquipmentSlotType.Artifact:
                return slotId == EquipmentSlotId.Artifact;
            default:
                return false;
        }
    }
}
