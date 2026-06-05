using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerLootInventory : MonoBehaviour
{
    public readonly struct LootInventorySlotView
    {
        public LootInventorySlotView(int index, ItemDefinition item, int amount, bool isEmpty)
        {
            Index = index;
            Item = item;
            Amount = amount;
            IsEmpty = isEmpty;
        }

        public int Index { get; }
        public ItemDefinition Item { get; }
        public int Amount { get; }
        public bool IsEmpty { get; }
    }

    public readonly struct LootItemStackView
    {
        public LootItemStackView(ItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public ItemDefinition Item { get; }
        public int Amount { get; }
    }

    [Serializable]
    private sealed class LootInventorySlot
    {
        [SerializeField] private ItemDefinition item;
        [SerializeField] private int amount;

        public ItemDefinition Item => item;
        public int Amount => amount;
        public bool IsEmpty => item == null || amount <= 0;

        public void Set(ItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public void Add(int value)
        {
            amount += value;
        }

        public void Clear()
        {
            item = null;
            amount = 0;
        }
    }

    [SerializeField] private int capacity = 24;
    [SerializeField] private List<LootInventorySlot> slots = new List<LootInventorySlot>();

    public event Action OnChanged;

    private void Awake()
    {
        EnsureSlotCapacity();
    }

    public void Add(ItemDefinition item, int amount)
    {
        TryAdd(item, amount);
    }

    public bool TryAdd(ItemDefinition item, int amount)
    {
        if (item == null)
            return false;

        if (amount <= 0)
            return false;

        EnsureSlotCapacity();

        LootInventorySlot slot = FindSlot(item);

        if (slot != null)
        {
            slot.Add(amount);
            OnChanged?.Invoke();
            return true;
        }

        slot = FindFirstEmptySlot();

        if (slot == null)
            return false;

        slot.Set(item, amount);
        OnChanged?.Invoke();
        return true;
    }

    public int GetAmount(ItemDefinition item)
    {
        return TryGetAmount(item, out int amount) ? amount : 0;
    }

    public bool TryGetAmount(ItemDefinition item, out int amount)
    {
        amount = 0;

        if (item == null)
            return false;

        LootInventorySlot slot = FindSlot(item);

        if (slot == null)
            return false;

        amount = slot.Amount;
        return true;
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

    public bool SwapSlots(int firstIndex, int secondIndex)
    {
        if (!IsValidSlotIndex(firstIndex) || !IsValidSlotIndex(secondIndex))
            return false;

        if (firstIndex == secondIndex)
            return true;

        SwapSlotContents(slots[firstIndex], slots[secondIndex]);
        OnChanged?.Invoke();
        return true;
    }

    public bool MoveSlot(int fromIndex, int toIndex)
    {
        if (!IsValidSlotIndex(fromIndex) || !IsValidSlotIndex(toIndex))
            return false;

        if (fromIndex == toIndex)
            return true;

        LootInventorySlot source = slots[fromIndex];
        LootInventorySlot target = slots[toIndex];

        if (source == null || source.IsEmpty)
            return false;

        if (target == null || target.IsEmpty)
        {
            CopySlot(source, target);
            source.Clear();
            OnChanged?.Invoke();
            return true;
        }

        return SwapSlots(fromIndex, toIndex);
    }

    public int GetSlots(List<LootInventorySlotView> results, bool includeEmpty = true)
    {
        if (results == null)
            return 0;

        results.Clear();
        EnsureSlotCapacity();

        for (int i = 0; i < slots.Count; i++)
        {
            LootInventorySlot slot = slots[i];
            bool isEmpty = slot == null || slot.IsEmpty;

            if (!includeEmpty && isEmpty)
                continue;

            results.Add(new LootInventorySlotView(
                i,
                isEmpty ? null : slot.Item,
                isEmpty ? 0 : slot.Amount,
                isEmpty
            ));
        }

        return results.Count;
    }

    public int GetStacks(List<LootItemStackView> results)
    {
        if (results == null)
            return 0;

        results.Clear();
        EnsureSlotCapacity();

        foreach (LootInventorySlot slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                continue;

            results.Add(new LootItemStackView(slot.Item, slot.Amount));
        }

        return results.Count;
    }

    private void EnsureSlotCapacity()
    {
        capacity = Mathf.Max(0, capacity);

        slots ??= new List<LootInventorySlot>();

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i] ??= new LootInventorySlot();
        }

        while (slots.Count < capacity)
        {
            slots.Add(new LootInventorySlot());
        }
    }

    private LootInventorySlot FindSlot(ItemDefinition item)
    {
        EnsureSlotCapacity();

        foreach (LootInventorySlot slot in slots)
        {
            if (slot != null && !slot.IsEmpty && slot.Item == item)
                return slot;
        }

        return null;
    }

    private LootInventorySlot FindFirstEmptySlot()
    {
        EnsureSlotCapacity();

        foreach (LootInventorySlot slot in slots)
        {
            if (slot != null && slot.IsEmpty)
                return slot;
        }

        return null;
    }

    private void CopySlot(LootInventorySlot source, LootInventorySlot target)
    {
        if (source == null || target == null)
            return;

        if (source.IsEmpty)
        {
            target.Clear();
            return;
        }

        target.Set(source.Item, source.Amount);
    }

    private void SwapSlotContents(LootInventorySlot first, LootInventorySlot second)
    {
        if (first == null || second == null)
            return;

        ItemDefinition firstItem = first.Item;
        int firstAmount = first.Amount;

        CopySlot(second, first);

        if (firstItem == null || firstAmount <= 0)
            second.Clear();
        else
            second.Set(firstItem, firstAmount);
    }
}
