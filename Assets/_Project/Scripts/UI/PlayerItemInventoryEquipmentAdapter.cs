using UnityEngine;

public sealed class PlayerItemInventoryEquipmentAdapter : MonoBehaviour
{
    [SerializeField] private PlayerItemInventory itemInventory;
    [SerializeField] private PlayerEquipment playerEquipment;

    private void Awake()
    {
        itemInventory ??= FindAnyObjectByType<PlayerItemInventory>();
        playerEquipment ??= FindAnyObjectByType<PlayerEquipment>();
    }

    public bool TryEquipFromSlot(int slotIndex)
    {
        if (itemInventory == null || playerEquipment == null)
            return false;

        if (!itemInventory.IsValidSlotIndex(slotIndex))
            return false;

        PlayerItemInventory.ItemInventorySlotView slot = itemInventory.GetSlot(slotIndex);
        if (slot.IsEmpty || slot.Item == null)
            return false;

        if (itemInventory.CountFreeSlots() + 1 < 2)
            return false;

        ItemDefinition item = itemInventory.RemoveAt(slotIndex);
        if (item == null)
            return false;

        PlayerEquipment.EquipmentChangeResult result = playerEquipment.AutoEquip(item);
        if (!result.Success)
        {
            itemInventory.TryAdd(item);
            return false;
        }

        foreach (ItemDefinition unequippedItem in result.UnequippedItems)
        {
            if (unequippedItem == null)
                continue;

            if (!itemInventory.TryAdd(unequippedItem))
                Debug.LogWarning("Failed to return unequipped item to inventory after equip.");
        }

        return true;
    }

    public bool TryUnequipToInventory(EquipmentSlotId slot)
    {
        if (itemInventory == null || playerEquipment == null)
            return false;

        ItemDefinition item = playerEquipment.GetEquipped(slot);
        if (item == null)
            return false;

        if (itemInventory.CountFreeSlots() < 1)
            return false;

        bool unequipped = playerEquipment.Unequip(slot);
        if (!unequipped)
            return false;

        if (itemInventory.TryAdd(item))
            return true;

        Debug.LogWarning("Failed to return unequipped item to inventory.");
        playerEquipment.Equip(item, slot);
        return false;
    }
}
