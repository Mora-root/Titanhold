using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerItemInventory : MonoBehaviour
{
    public readonly struct ItemInventorySlotView
    {
        public ItemInventorySlotView(int index, ItemDefinition item, bool isEmpty)
        {
            Index = index;
            Item = item;
            IsEmpty = isEmpty;
        }

        public int Index { get; }
        public ItemDefinition Item { get; }
        public bool IsEmpty { get; }
    }

    [Serializable]
    private sealed class ItemInventorySlot
    {
        [SerializeField] private ItemDefinition item;

        public ItemDefinition Item => item;
        public bool IsEmpty => item == null;

        public void Set(ItemDefinition item)
        {
            this.item = item;
        }

        public void Clear()
        {
            item = null;
        }
    }

    [SerializeField] private int capacity = 24;
    [SerializeField] private List<ItemInventorySlot> slots = new();

    public event Action OnChanged;

    private void Awake()
    {
        EnsureSlotCapacity();
    }

    public bool TryAdd(ItemDefinition item)
    {
        if (item == null)
            return false;

        EnsureSlotCapacity();

        ItemInventorySlot slot = FindFirstEmptySlot();

        if (slot == null)
            return false;

        slot.Set(item);
        OnChanged?.Invoke();
        return true;
    }

    public ItemDefinition RemoveAt(int index)
    {
        if (!IsValidSlotIndex(index))
            return null;

        ItemInventorySlot slot = slots[index];

        if (slot == null || slot.IsEmpty)
            return null;

        ItemDefinition item = slot.Item;
        slot.Clear();
        OnChanged?.Invoke();
        return item;
    }

    public ItemInventorySlotView GetSlot(int index)
    {
        EnsureSlotCapacity();

        if (!IsValidSlotIndex(index))
            return new ItemInventorySlotView(index, null, true);

        ItemInventorySlot slot = slots[index];
        bool isEmpty = slot == null || slot.IsEmpty;

        return new ItemInventorySlotView(index, isEmpty ? null : slot.Item, isEmpty);
    }

    public int GetSlots(List<ItemInventorySlotView> results, bool includeEmpty = true)
    {
        if (results == null)
            return 0;

        results.Clear();
        EnsureSlotCapacity();

        for (int i = 0; i < slots.Count; i++)
        {
            ItemInventorySlot slot = slots[i];
            bool isEmpty = slot == null || slot.IsEmpty;

            if (!includeEmpty && isEmpty)
                continue;

            results.Add(new ItemInventorySlotView(i, isEmpty ? null : slot.Item, isEmpty));
        }

        return results.Count;
    }

    public bool IsValidSlotIndex(int index)
    {
        EnsureSlotCapacity();
        return index >= 0 && index < slots.Count;
    }

    public bool IsSlotEmpty(int index)
    {
        if (!IsValidSlotIndex(index))
            return false;

        return slots[index] == null || slots[index].IsEmpty;
    }

    public bool MoveSlot(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return true;

        ItemInventorySlot source = slots[fromIndex];
        ItemInventorySlot target = slots[toIndex];

        if (source == null || source.IsEmpty)
            return false;

        if (target == null || target.IsEmpty)
        {
            target.Set(source.Item);
            source.Clear();
            OnChanged?.Invoke();
            return true;
        }

        return SwapSlots(fromIndex, toIndex);
    }

    public bool SwapSlots(int firstIndex, int secondIndex)
    {
        if (!IsValidSlotIndex(firstIndex) || !IsValidSlotIndex(secondIndex))
            return false;

        if (firstIndex == secondIndex)
            return true;

        ItemDefinition firstItem = slots[firstIndex].Item;
        slots[firstIndex].Set(slots[secondIndex].Item);
        slots[secondIndex].Set(firstItem);
        OnChanged?.Invoke();
        return true;
    }

    public int CountFreeSlots()
    {
        EnsureSlotCapacity();

        int count = 0;

        foreach (ItemInventorySlot slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                count++;
        }

        return count;
    }

    private void EnsureSlotCapacity()
    {
        capacity = Mathf.Max(0, capacity);
        slots ??= new List<ItemInventorySlot>();

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] ??= new ItemInventorySlot();
        }

        while (slots.Count < capacity)
        {
            slots.Add(new ItemInventorySlot());
        }
    }

    private ItemInventorySlot FindFirstEmptySlot()
    {
        EnsureSlotCapacity();

        foreach (ItemInventorySlot slot in slots)
        {
            if (slot != null && slot.IsEmpty)
                return slot;
        }

        return null;
    }
}
