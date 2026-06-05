using System;
using UnityEngine;

[Serializable]
public sealed class ItemStack
{
    [SerializeField] private ItemDefinition definition;
    [SerializeField] private int amount;
    [SerializeField] private ItemInstance instance;

    private ItemStack(ItemDefinition definition, int amount, ItemInstance instance)
    {
        this.definition = definition;
        this.amount = amount;
        this.instance = instance;
    }

    public ItemDefinition Definition => definition;
    public int Amount => amount;
    public ItemInstance Instance => instance;
    public bool IsFull => Definition != null && Amount >= Definition.MaxStack;
    public int FreeAmount => Definition != null ? Math.Max(0, Definition.MaxStack - Amount) : 0;

    public static ItemStack CreateStackable(ItemDefinition definition, int amount)
    {
        if (definition == null)
            throw new ArgumentNullException(nameof(definition));

        if (definition.MaxStack <= 1)
            throw new InvalidOperationException($"Item '{definition.Id}' is not stackable.");

        if (amount <= 0 || amount > definition.MaxStack)
            throw new ArgumentOutOfRangeException(nameof(amount), $"Amount must be between 1 and {definition.MaxStack}.");

        return new ItemStack(definition, amount, null);
    }

    public static ItemStack CreateNonStackable(ItemInstance instance)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (instance.Definition == null)
            throw new ArgumentException("ItemInstance must have a definition.", nameof(instance));

        if (instance.Definition.MaxStack > 1)
            throw new InvalidOperationException($"Item '{instance.Definition.Id}' is stackable and cannot be stored as a non-stackable stack.");

        return new ItemStack(instance.Definition, 1, instance);
    }

    public bool CanStackWith(ItemStack other)
    {
        if (other == null || IsFull)
            return false;

        if (Definition == null || other.Definition == null)
            return false;

        if (Instance != null || other.Instance != null)
            return false;

        if (Definition.MaxStack <= 1 || other.Definition.MaxStack <= 1)
            return false;

        if (ReferenceEquals(Definition, other.Definition))
            return true;

        return !string.IsNullOrWhiteSpace(Definition.Id) && Definition.Id == other.Definition.Id;
    }

    public int AddAmount(int amount)
    {
        if (Definition == null || Definition.MaxStack <= 1 || Instance != null)
            throw new InvalidOperationException("Cannot add amount to a non-stackable item.");

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        int added = Math.Min(amount, FreeAmount);
        this.amount += added;
        return amount - added;
    }

    public int RemoveAmount(int amount)
    {
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be positive.");

        int removed = Math.Min(amount, this.amount);
        this.amount -= removed;
        return removed;
    }
}
