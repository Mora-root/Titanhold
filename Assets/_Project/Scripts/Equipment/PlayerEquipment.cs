using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerEquipment : MonoBehaviour
{
    public readonly struct EquipmentChangeResult
    {
        public EquipmentChangeResult(
            bool success,
            EquipmentSlot equippedSlot,
            EquipmentItemDefinition equippedItem,
            IReadOnlyList<EquipmentItemDefinition> unequippedItems)
        {
            Success = success;
            EquippedSlot = equippedSlot;
            EquippedItem = equippedItem;
            UnequippedItems = unequippedItems ?? Array.Empty<EquipmentItemDefinition>();
        }

        public bool Success { get; }
        public EquipmentSlot EquippedSlot { get; }
        public EquipmentItemDefinition EquippedItem { get; }
        public IReadOnlyList<EquipmentItemDefinition> UnequippedItems { get; }
    }

    [Serializable]
    private sealed class EquippedItem
    {
        [SerializeField] private EquipmentSlot slot;
        [SerializeField] private EquipmentItemDefinition item;

        public EquippedItem()
        {
        }

        public EquippedItem(EquipmentSlot slot)
        {
            this.slot = slot;
        }

        public EquipmentSlot Slot => slot;
        public EquipmentItemDefinition Item => item;

        public void SetSlot(EquipmentSlot slot)
        {
            this.slot = slot;
        }

        public void SetItem(EquipmentItemDefinition item)
        {
            this.item = item;
        }
    }

    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private EquippedItem[] equippedItems;

    public event Action<EquipmentSlot, EquipmentItemDefinition> OnEquipmentChanged;

    private void Awake()
    {
        characterStats ??= GetComponent<CharacterStats>();
        EnsureSlots();
    }

    public bool CanEquip(EquipmentItemDefinition item, EquipmentSlot targetSlot)
    {
        if (item == null)
            return false;

        EnsureSlots();

        if (item.IsWeapon && item.WeaponFamily == WeaponFamily.None)
        {
            return false;
        }

        if (item.IsEquipment)
            return IsValidEquipmentTargetSlot(item, targetSlot);

        if (item.Handedness == WeaponHandedness.TwoHand)
            return targetSlot == EquipmentSlot.MainHand;

        if (targetSlot == EquipmentSlot.MainHand)
            return true;

        if (targetSlot != EquipmentSlot.OffHand)
            return false;

        EquipmentItemDefinition mainHandItem = GetEquipped(EquipmentSlot.MainHand);

        return IsMatchingOneHandWeapon(mainHandItem, item.WeaponFamily);
    }

    public bool Equip(EquipmentItemDefinition item, EquipmentSlot targetSlot)
    {
        return EquipWithResult(item, targetSlot).Success;
    }

    public bool Equip(EquipmentItemDefinition item)
    {
        if (item == null)
            return false;

        return Equip(item, item.DefaultSlot);
    }

    public EquipmentChangeResult AutoEquip(EquipmentItemDefinition item)
    {
        if (item == null)
            return CreateFailedResult(item);

        if (item.IsEquipment)
            return EquipWithResult(item, item.DefaultSlot);

        if (!item.IsWeapon)
            return CreateFailedResult(item);

        if (item.Handedness == WeaponHandedness.TwoHand)
            return EquipWithResult(item, EquipmentSlot.MainHand);

        if (item.Handedness != WeaponHandedness.OneHand)
            return CreateFailedResult(item);

        EquipmentItemDefinition mainHandItem = GetEquipped(EquipmentSlot.MainHand);
        EquipmentItemDefinition offHandItem = GetEquipped(EquipmentSlot.OffHand);

        if (mainHandItem == null)
            return EquipWithResult(item, EquipmentSlot.MainHand);

        if (IsMatchingOneHandWeapon(mainHandItem, item.WeaponFamily))
        {
            if (offHandItem == null)
                return EquipWithResult(item, EquipmentSlot.OffHand);

            return EquipWithResult(item, EquipmentSlot.MainHand);
        }

        return EquipWithResult(item, EquipmentSlot.MainHand);
    }

    public EquipmentChangeResult EquipWithResult(EquipmentItemDefinition item, EquipmentSlot targetSlot)
    {
        if (!CanEquip(item, targetSlot))
            return CreateFailedResult(item);

        EquipmentSlot equippedSlot = GetResolvedEquipSlot(item, targetSlot);

        if (!HasSlot(equippedSlot))
            return CreateFailedResult(item);

        List<EquipmentItemDefinition> unequippedItems = new List<EquipmentItemDefinition>();

        if (item.IsWeapon && item.Handedness == WeaponHandedness.TwoHand)
        {
            ClearSlot(EquipmentSlot.MainHand, unequippedItems);
            ClearSlot(EquipmentSlot.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlot.MainHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlot.MainHand, item, unequippedItems);
        }

        if (IsShield(item))
        {
            EquipmentItemDefinition mainHandItem = GetEquipped(EquipmentSlot.MainHand);

            if (mainHandItem != null && mainHandItem.OccupiesBothHands)
                ClearSlot(EquipmentSlot.MainHand, unequippedItems);

            ClearSlot(EquipmentSlot.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlot.OffHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlot.OffHand, item, unequippedItems);
        }

        if (item.IsWeapon && item.Handedness == WeaponHandedness.OneHand)
        {
            if (targetSlot == EquipmentSlot.MainHand)
            {
                EquipmentItemDefinition offHandItem = GetEquipped(EquipmentSlot.OffHand);

                if (IsIncompatibleOffHandWeapon(item, offHandItem))
                    ClearSlot(EquipmentSlot.OffHand, unequippedItems);

                ClearSlot(EquipmentSlot.MainHand, unequippedItems);
                if (!SetSlotItem(EquipmentSlot.MainHand, item))
                    return CreateFailedResult(item);

                return CreateSuccessResult(EquipmentSlot.MainHand, item, unequippedItems);
            }

            ClearSlot(EquipmentSlot.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlot.OffHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlot.OffHand, item, unequippedItems);
        }

        ClearSlot(targetSlot, unequippedItems);
        if (!SetSlotItem(targetSlot, item))
            return CreateFailedResult(item);

        return CreateSuccessResult(targetSlot, item, unequippedItems);
    }

    public bool Unequip(EquipmentSlot slot)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);

        if (equippedItem == null || equippedItem.Item == null)
            return false;

        if (characterStats != null)
            characterStats.RemoveModifiersFromSource(slot);

        equippedItem.SetItem(null);
        OnEquipmentChanged?.Invoke(slot, null);
        return true;
    }

    public EquipmentItemDefinition GetEquipped(EquipmentSlot slot)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);
        return equippedItem != null ? equippedItem.Item : null;
    }

    private bool ClearSlot(EquipmentSlot slot)
    {
        return ClearSlot(slot, null);
    }

    private bool ClearSlot(EquipmentSlot slot, List<EquipmentItemDefinition> unequippedItems)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);

        if (characterStats != null)
            characterStats.RemoveModifiersFromSource(slot);

        if (equippedItem == null || equippedItem.Item == null)
            return false;

        EquipmentItemDefinition removedItem = equippedItem.Item;
        equippedItem.SetItem(null);
        unequippedItems?.Add(removedItem);
        OnEquipmentChanged?.Invoke(slot, null);
        return true;
    }

    private bool SetSlotItem(EquipmentSlot slot, EquipmentItemDefinition item)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);

        if (equippedItem == null)
            return false;

        equippedItem.SetItem(item);

        if (characterStats != null)
            characterStats.AddModifiers(item.Modifiers, slot);

        OnEquipmentChanged?.Invoke(slot, item);
        return true;
    }

    private bool IsIncompatibleOffHandWeapon(EquipmentItemDefinition mainHandItem, EquipmentItemDefinition offHandItem)
    {
        if (mainHandItem == null || offHandItem == null)
            return false;

        if (!offHandItem.IsWeapon || offHandItem.Handedness != WeaponHandedness.OneHand)
            return false;

        if (mainHandItem.WeaponFamily == WeaponFamily.None || offHandItem.WeaponFamily == WeaponFamily.None)
            return true;

        return mainHandItem.WeaponFamily != offHandItem.WeaponFamily;
    }

    private bool IsMatchingOneHandWeapon(EquipmentItemDefinition item, WeaponFamily weaponFamily)
    {
        return item != null &&
               item.IsWeapon &&
               item.Handedness == WeaponHandedness.OneHand &&
               item.WeaponFamily == weaponFamily &&
               item.WeaponFamily != WeaponFamily.None;
    }

    private EquipmentSlot GetResolvedEquipSlot(EquipmentItemDefinition item, EquipmentSlot targetSlot)
    {
        if (item.IsWeapon && item.Handedness == WeaponHandedness.TwoHand)
            return EquipmentSlot.MainHand;

        if (IsShield(item))
            return EquipmentSlot.OffHand;

        return targetSlot;
    }

    private bool IsShield(EquipmentItemDefinition item)
    {
        return item != null && item.IsEquipment && item.EquipmentType == EquipmentType.Shield;
    }

    private bool IsValidEquipmentTargetSlot(EquipmentItemDefinition item, EquipmentSlot targetSlot)
    {
        if (item == null || !item.IsEquipment)
            return false;

        if (item.EquipmentType == EquipmentType.Shield)
            return targetSlot == EquipmentSlot.OffHand;

        if (targetSlot != item.DefaultSlot)
            return false;

        return IsValidSlotForEquipmentType(item.EquipmentType, targetSlot);
    }

    private bool IsValidSlotForEquipmentType(EquipmentType equipmentType, EquipmentSlot targetSlot)
    {
        switch (equipmentType)
        {
            case EquipmentType.Head:
                return targetSlot == EquipmentSlot.Head;
            case EquipmentType.Chest:
                return targetSlot == EquipmentSlot.Chest;
            case EquipmentType.Hands:
                return targetSlot == EquipmentSlot.Hands;
            case EquipmentType.Legs:
                return targetSlot == EquipmentSlot.Legs;
            case EquipmentType.Feet:
                return targetSlot == EquipmentSlot.Feet;
            case EquipmentType.Amulet:
                return targetSlot == EquipmentSlot.Amulet;
            case EquipmentType.Ring:
                return targetSlot == EquipmentSlot.Ring1 || targetSlot == EquipmentSlot.Ring2;
            case EquipmentType.Shield:
                return targetSlot == EquipmentSlot.OffHand;
            default:
                return false;
        }
    }

    private bool HasSlot(EquipmentSlot slot)
    {
        EnsureSlots();
        return FindEntry(slot) != null;
    }

    private EquipmentChangeResult CreateFailedResult(EquipmentItemDefinition item)
    {
        return new EquipmentChangeResult(false, default, item, Array.Empty<EquipmentItemDefinition>());
    }

    private EquipmentChangeResult CreateSuccessResult(
        EquipmentSlot equippedSlot,
        EquipmentItemDefinition equippedItem,
        List<EquipmentItemDefinition> unequippedItems)
    {
        IReadOnlyList<EquipmentItemDefinition> resultItems = unequippedItems != null && unequippedItems.Count > 0
            ? unequippedItems.ToArray()
            : Array.Empty<EquipmentItemDefinition>();

        return new EquipmentChangeResult(true, equippedSlot, equippedItem, resultItems);
    }

    private void EnsureSlots()
    {
        EquipmentSlot[] slotValues = (EquipmentSlot[])Enum.GetValues(typeof(EquipmentSlot));

        if (HasOneEntryPerSlot(slotValues))
            return;

        EquippedItem[] currentItems = equippedItems ?? Array.Empty<EquippedItem>();
        EquippedItem[] rebuiltItems = new EquippedItem[slotValues.Length];

        for (int i = 0; i < slotValues.Length; i++)
        {
            EquipmentSlot slot = slotValues[i];
            EquippedItem existingItem = FindEntry(currentItems, slot);

            if (existingItem == null)
            {
                existingItem = new EquippedItem(slot);
            }
            else
            {
                existingItem.SetSlot(slot);
            }

            rebuiltItems[i] = existingItem;
        }

        equippedItems = rebuiltItems;
    }

    private bool HasOneEntryPerSlot(EquipmentSlot[] slotValues)
    {
        if (slotValues == null)
            return false;

        if (equippedItems == null || equippedItems.Length != slotValues.Length)
            return false;

        foreach (EquipmentSlot slot in slotValues)
        {
            int entryCount = 0;

            foreach (EquippedItem equippedItem in equippedItems)
            {
                if (equippedItem != null && equippedItem.Slot == slot)
                    entryCount++;
            }

            if (entryCount != 1)
                return false;
        }

        return true;
    }

    private EquippedItem FindEntry(EquipmentSlot slot)
    {
        return FindEntry(equippedItems, slot);
    }

    private EquippedItem FindEntry(EquippedItem[] items, EquipmentSlot slot)
    {
        if (items == null)
            return null;

        foreach (EquippedItem equippedItem in items)
        {
            if (equippedItem != null && equippedItem.Slot == slot)
                return equippedItem;
        }

        return null;
    }
}
