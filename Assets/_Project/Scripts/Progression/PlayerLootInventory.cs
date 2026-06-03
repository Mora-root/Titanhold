using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerLootInventory : MonoBehaviour
{
    public readonly struct LootInventorySlotView
    {
        public LootInventorySlotView(int index, LootItemDefinition item, int amount, bool isEmpty)
        {
            Index = index;
            Item = item;
            Amount = amount;
            IsEmpty = isEmpty;
        }

        public int Index { get; }
        public LootItemDefinition Item { get; }
        public int Amount { get; }
        public bool IsEmpty { get; }
    }

    public readonly struct LootItemStackView
    {
        public LootItemStackView(LootItemDefinition item, int amount)
        {
            Item = item;
            Amount = amount;
        }

        public LootItemDefinition Item { get; }
        public int Amount { get; }
    }

    [Serializable]
    private sealed class LootInventorySlot
    {
        [SerializeField] private LootItemDefinition item;
        [SerializeField] private int amount;

        public LootItemDefinition Item => item;
        public int Amount => amount;
        public bool IsEmpty => item == null || amount <= 0;

        public void Set(LootItemDefinition item, int amount)
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

    public void Add(LootItemDefinition item, int amount)
    {
        TryAdd(item, amount);
    }

    public bool TryAdd(LootItemDefinition item, int amount)
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

    public int GetAmount(LootItemDefinition item)
    {
        return TryGetAmount(item, out int amount) ? amount : 0;
    }

    public bool TryGetAmount(LootItemDefinition item, out int amount)
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

    private LootInventorySlot FindSlot(LootItemDefinition item)
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
}
