using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerLootInventory : MonoBehaviour
{
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
    private sealed class LootItemStack
    {
        [SerializeField] private LootItemDefinition item;
        [SerializeField] private int amount;

        public LootItemDefinition Item => item;
        public int Amount => amount;

        public LootItemStack(LootItemDefinition item, int amount)
        {
            this.item = item;
            this.amount = amount;
        }

        public void Add(int value)
        {
            amount += value;
        }
    }

    [SerializeField] private List<LootItemStack> stacks = new List<LootItemStack>();

    public event Action OnChanged;

    public void Add(LootItemDefinition item, int amount)
    {
        if (item == null)
            return;

        if (amount <= 0)
            return;

        LootItemStack stack = FindStack(item);

        if (stack != null)
        {
            stack.Add(amount);
        }
        else
        {
            stacks.Add(new LootItemStack(item, amount));
        }

        OnChanged?.Invoke();
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

        LootItemStack stack = FindStack(item);

        if (stack == null)
            return false;

        amount = stack.Amount;
        return true;
    }

    public int GetStacks(List<LootItemStackView> results)
    {
        if (results == null)
            return 0;

        results.Clear();

        foreach (LootItemStack stack in stacks)
        {
            if (stack == null || stack.Item == null || stack.Amount <= 0)
                continue;

            results.Add(new LootItemStackView(stack.Item, stack.Amount));
        }

        return results.Count;
    }

    private LootItemStack FindStack(LootItemDefinition item)
    {
        foreach (LootItemStack stack in stacks)
        {
            if (stack != null && stack.Item == item)
                return stack;
        }

        return null;
    }
}
