using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerEquipment : MonoBehaviour
{
    public readonly struct EquipmentChangeResult
    {
        public EquipmentChangeResult(
            bool success,
            EquipmentSlotId equippedSlot,
            ItemDefinition equippedItem,
            IReadOnlyList<ItemDefinition> unequippedItems)
        {
            Success = success;
            EquippedSlot = equippedSlot;
            EquippedItem = equippedItem;
            UnequippedItems = unequippedItems ?? Array.Empty<ItemDefinition>();
        }

        public bool Success { get; }
        public EquipmentSlotId EquippedSlot { get; }
        public ItemDefinition EquippedItem { get; }
        public IReadOnlyList<ItemDefinition> UnequippedItems { get; }
    }

    [Serializable]
    private sealed class EquippedItem
    {
        [SerializeField] private EquipmentSlotId slot;
        [SerializeField] private ItemDefinition item;

        public EquippedItem()
        {
        }

        public EquippedItem(EquipmentSlotId slot)
        {
            this.slot = slot;
        }

        public EquipmentSlotId Slot => slot;
        public ItemDefinition Item => item;

        public void SetSlot(EquipmentSlotId slot)
        {
            this.slot = slot;
        }

        public void SetItem(ItemDefinition item)
        {
            this.item = item;
        }
    }

    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private EquippedItem[] equippedItems;

    public event Action<EquipmentSlotId, ItemDefinition> OnEquipmentChanged;

    private void Awake()
    {
        characterStats ??= GetComponent<CharacterStats>();
        EnsureSlots();
    }

    public bool CanEquip(ItemDefinition item, EquipmentSlotId targetSlot)
    {
        if (item == null)
            return false;

        EnsureSlots();

        if (!item.IsEquippable)
            return false;

        if (!item.IsWeapon)
            return IsValidEquipmentTargetSlot(item, targetSlot);

        if (item.WeaponFamily == WeaponFamily.None)
            return false;

        if (item.Handedness == WeaponHandedness.TwoHand)
            return targetSlot == EquipmentSlotId.MainHand;

        if (targetSlot == EquipmentSlotId.MainHand)
            return true;

        if (targetSlot != EquipmentSlotId.OffHand)
            return false;

        ItemDefinition mainHandItem = GetEquipped(EquipmentSlotId.MainHand);

        return IsMatchingOneHandWeapon(mainHandItem, item.WeaponFamily);
    }

    public bool Equip(ItemDefinition item, EquipmentSlotId targetSlot)
    {
        return EquipWithResult(item, targetSlot).Success;
    }

    public bool Equip(ItemDefinition item)
    {
        if (item == null)
            return false;

        return Equip(item, GetPreferredSlot(item));
    }

    public EquipmentChangeResult AutoEquip(ItemDefinition item)
    {
        if (item == null)
            return CreateFailedResult(item);

        if (!item.IsWeapon)
            return EquipWithResult(item, GetPreferredSlot(item));

        if (item.Handedness == WeaponHandedness.TwoHand)
            return EquipWithResult(item, EquipmentSlotId.MainHand);

        if (item.Handedness != WeaponHandedness.OneHand)
            return CreateFailedResult(item);

        ItemDefinition mainHandItem = GetEquipped(EquipmentSlotId.MainHand);
        ItemDefinition offHandItem = GetEquipped(EquipmentSlotId.OffHand);

        if (mainHandItem == null)
            return EquipWithResult(item, EquipmentSlotId.MainHand);

        if (IsMatchingOneHandWeapon(mainHandItem, item.WeaponFamily))
        {
            if (offHandItem == null)
                return EquipWithResult(item, EquipmentSlotId.OffHand);

            return EquipWithResult(item, EquipmentSlotId.MainHand);
        }

        return EquipWithResult(item, EquipmentSlotId.MainHand);
    }

    public EquipmentSlotId GetPreferredSlot(ItemDefinition item)
    {
        if (item == null)
            return default;

        EnsureSlots();

        switch (item.EquipmentSlotType)
        {
            case EquipmentSlotType.Weapon:
                return EquipmentSlotId.MainHand;
            case EquipmentSlotType.Shield:
                return EquipmentSlotId.OffHand;
            case EquipmentSlotType.Head:
                return EquipmentSlotId.Head;
            case EquipmentSlotType.Chest:
                return EquipmentSlotId.Chest;
            case EquipmentSlotType.Hands:
                return EquipmentSlotId.Hands;
            case EquipmentSlotType.Legs:
                return EquipmentSlotId.Legs;
            case EquipmentSlotType.Feet:
                return EquipmentSlotId.Feet;
            case EquipmentSlotType.Amulet:
                return EquipmentSlotId.Amulet;
            case EquipmentSlotType.Ring:
                if (GetEquipped(EquipmentSlotId.Ring1) == null)
                    return EquipmentSlotId.Ring1;

                if (GetEquipped(EquipmentSlotId.Ring2) == null)
                    return EquipmentSlotId.Ring2;

                return EquipmentSlotId.Ring1;
            case EquipmentSlotType.Artifact:
                return EquipmentSlotId.Artifact;
            default:
                return default;
        }
    }

    public EquipmentChangeResult EquipWithResult(ItemDefinition item, EquipmentSlotId targetSlot)
    {
        if (!CanEquip(item, targetSlot))
            return CreateFailedResult(item);

        EquipmentSlotId equippedSlot = GetResolvedEquipSlot(item, targetSlot);

        if (!HasSlot(equippedSlot))
            return CreateFailedResult(item);

        List<ItemDefinition> unequippedItems = new List<ItemDefinition>();

        if (item.IsWeapon && item.Handedness == WeaponHandedness.TwoHand)
        {
            ClearSlot(EquipmentSlotId.MainHand, unequippedItems);
            ClearSlot(EquipmentSlotId.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlotId.MainHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlotId.MainHand, item, unequippedItems);
        }

        if (IsShield(item))
        {
            ItemDefinition mainHandItem = GetEquipped(EquipmentSlotId.MainHand);

            if (mainHandItem != null && mainHandItem.OccupiesBothHands)
                ClearSlot(EquipmentSlotId.MainHand, unequippedItems);

            ClearSlot(EquipmentSlotId.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlotId.OffHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlotId.OffHand, item, unequippedItems);
        }

        if (item.IsWeapon && item.Handedness == WeaponHandedness.OneHand)
        {
            if (targetSlot == EquipmentSlotId.MainHand)
            {
                ItemDefinition offHandItem = GetEquipped(EquipmentSlotId.OffHand);

                if (IsIncompatibleOffHandWeapon(item, offHandItem))
                    ClearSlot(EquipmentSlotId.OffHand, unequippedItems);

                ClearSlot(EquipmentSlotId.MainHand, unequippedItems);
                if (!SetSlotItem(EquipmentSlotId.MainHand, item))
                    return CreateFailedResult(item);

                return CreateSuccessResult(EquipmentSlotId.MainHand, item, unequippedItems);
            }

            ClearSlot(EquipmentSlotId.OffHand, unequippedItems);
            if (!SetSlotItem(EquipmentSlotId.OffHand, item))
                return CreateFailedResult(item);

            return CreateSuccessResult(EquipmentSlotId.OffHand, item, unequippedItems);
        }

        ClearSlot(targetSlot, unequippedItems);
        if (!SetSlotItem(targetSlot, item))
            return CreateFailedResult(item);

        return CreateSuccessResult(targetSlot, item, unequippedItems);
    }

    public bool Unequip(EquipmentSlotId slot)
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

    public ItemDefinition GetEquipped(EquipmentSlotId slot)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);
        return equippedItem != null ? equippedItem.Item : null;
    }

    private bool ClearSlot(EquipmentSlotId slot)
    {
        return ClearSlot(slot, null);
    }

    private bool ClearSlot(EquipmentSlotId slot, List<ItemDefinition> unequippedItems)
    {
        EnsureSlots();

        EquippedItem equippedItem = FindEntry(slot);

        if (characterStats != null)
            characterStats.RemoveModifiersFromSource(slot);

        if (equippedItem == null || equippedItem.Item == null)
            return false;

        ItemDefinition removedItem = equippedItem.Item;
        equippedItem.SetItem(null);
        unequippedItems?.Add(removedItem);
        OnEquipmentChanged?.Invoke(slot, null);
        return true;
    }

    private bool SetSlotItem(EquipmentSlotId slot, ItemDefinition item)
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

    private bool IsIncompatibleOffHandWeapon(ItemDefinition mainHandItem, ItemDefinition offHandItem)
    {
        if (mainHandItem == null || offHandItem == null)
            return false;

        if (!offHandItem.IsWeapon || offHandItem.Handedness != WeaponHandedness.OneHand)
            return false;

        if (mainHandItem.WeaponFamily == WeaponFamily.None || offHandItem.WeaponFamily == WeaponFamily.None)
            return true;

        return mainHandItem.WeaponFamily != offHandItem.WeaponFamily;
    }

    private bool IsMatchingOneHandWeapon(ItemDefinition item, WeaponFamily weaponFamily)
    {
        return item != null &&
               item.IsWeapon &&
               item.Handedness == WeaponHandedness.OneHand &&
               item.WeaponFamily == weaponFamily &&
               item.WeaponFamily != WeaponFamily.None;
    }

    private EquipmentSlotId GetResolvedEquipSlot(ItemDefinition item, EquipmentSlotId targetSlot)
    {
        if (item.IsWeapon && item.Handedness == WeaponHandedness.TwoHand)
            return EquipmentSlotId.MainHand;

        if (IsShield(item))
            return EquipmentSlotId.OffHand;

        return targetSlot;
    }

    private bool IsShield(ItemDefinition item)
    {
        return item != null && item.IsEquipment && item.EquipmentSlotType == EquipmentSlotType.Shield;
    }

    private bool IsValidEquipmentTargetSlot(ItemDefinition item, EquipmentSlotId targetSlot)
    {
        if (item == null || !item.IsEquipment)
            return false;

        if (item.EquipmentSlotType == EquipmentSlotType.Shield)
            return targetSlot == EquipmentSlotId.OffHand;

        return IsValidSlotForEquipmentType(item.EquipmentSlotType, targetSlot);
    }

    private bool IsValidSlotForEquipmentType(EquipmentSlotType equipmentSlotType, EquipmentSlotId targetSlot)
    {
        switch (equipmentSlotType)
        {
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
            case EquipmentSlotType.Shield:
                return targetSlot == EquipmentSlotId.OffHand;
            case EquipmentSlotType.Artifact:
                return targetSlot == EquipmentSlotId.Artifact;
            default:
                return false;
        }
    }

    private bool HasSlot(EquipmentSlotId slot)
    {
        EnsureSlots();
        return FindEntry(slot) != null;
    }

    private EquipmentChangeResult CreateFailedResult(ItemDefinition item)
    {
        return new EquipmentChangeResult(false, default, item, Array.Empty<ItemDefinition>());
    }

    private EquipmentChangeResult CreateSuccessResult(
        EquipmentSlotId equippedSlot,
        ItemDefinition equippedItem,
        List<ItemDefinition> unequippedItems)
    {
        IReadOnlyList<ItemDefinition> resultItems = unequippedItems != null && unequippedItems.Count > 0
            ? unequippedItems.ToArray()
            : Array.Empty<ItemDefinition>();

        return new EquipmentChangeResult(true, equippedSlot, equippedItem, resultItems);
    }

    private void EnsureSlots()
    {
        EquipmentSlotId[] slotValues = (EquipmentSlotId[])Enum.GetValues(typeof(EquipmentSlotId));

        if (HasOneEntryPerSlot(slotValues))
            return;

        EquippedItem[] currentItems = equippedItems ?? Array.Empty<EquippedItem>();
        EquippedItem[] rebuiltItems = new EquippedItem[slotValues.Length];

        for (int i = 0; i < slotValues.Length; i++)
        {
            EquipmentSlotId slot = slotValues[i];
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

    private bool HasOneEntryPerSlot(EquipmentSlotId[] slotValues)
    {
        if (slotValues == null)
            return false;

        if (equippedItems == null || equippedItems.Length != slotValues.Length)
            return false;

        foreach (EquipmentSlotId slot in slotValues)
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

    private EquippedItem FindEntry(EquipmentSlotId slot)
    {
        return FindEntry(equippedItems, slot);
    }

    private EquippedItem FindEntry(EquippedItem[] items, EquipmentSlotId slot)
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
