using System;
using UnityEngine;

[Serializable]
public sealed class ItemContainerSection
{
    [SerializeField] private ItemCategory category;
    [SerializeField] private ItemSlot[] slots;

    public ItemContainerSection(ItemCategory category, int capacity)
    {
        this.category = category;

        int safeCapacity = Math.Max(0, capacity);
        slots = new ItemSlot[safeCapacity];

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] = new ItemSlot();
        }
    }

    public ItemCategory Category => category;
    public ItemSlot[] Slots => slots;
    public int Capacity => slots != null ? slots.Length : 0;

    public bool CanAccept(ItemDefinition definition)
    {
        return definition != null && definition.Category == category;
    }

    public bool CanAccept(ItemStack stack)
    {
        return stack != null && CanAccept(stack.Definition);
    }

    public ItemSlot GetSlot(int index)
    {
        if (slots == null || index < 0 || index >= slots.Length)
            return null;

        slots[index] ??= new ItemSlot();
        return slots[index];
    }

    public int CountOccupiedSlots()
    {
        EnsureSlots();

        int count = 0;

        foreach (ItemSlot slot in slots)
        {
            if (slot != null && !slot.IsEmpty)
                count++;
        }

        return count;
    }

    public int CountFreeSlots()
    {
        EnsureSlots();

        int count = 0;

        foreach (ItemSlot slot in slots)
        {
            if (slot == null || slot.IsEmpty)
                count++;
        }

        return count;
    }

    public AddItemResult TryAdd(ItemDefinition definition, int amount)
    {
        if (definition == null)
            return new AddItemResult(0, Math.Max(0, amount));

        if (amount <= 0)
            return new AddItemResult(0, Math.Max(0, amount));

        if (!CanAccept(definition))
            return new AddItemResult(0, amount);

        return definition.MaxStack > 1
            ? AddStackable(definition, amount)
            : AddNonStackable(definition, amount);
    }

    public AddItemResult TryAddExistingStack(ItemStack stack)
    {
        if (stack == null || stack.Definition == null)
            return new AddItemResult(0, 0);

        if (stack.Amount <= 0)
            return new AddItemResult(0, 0);

        if (!CanAccept(stack.Definition))
            return new AddItemResult(0, stack.Amount);

        return stack.Definition.MaxStack > 1
            ? AddExistingStackable(stack)
            : AddExistingNonStackable(stack);
    }

    public bool Move(int fromIndex, int toIndex)
    {
        ItemSlot source = GetSlot(fromIndex);
        ItemSlot target = GetSlot(toIndex);

        if (source == null || target == null)
            return false;

        return MoveTo(source, target, this, this);
    }

    internal bool MoveTo(
        ItemSlot source,
        ItemSlot target,
        ItemContainerSection sourceSection,
        ItemContainerSection targetSection)
    {
        if (source == null || target == null || sourceSection == null || targetSection == null)
            return false;

        if (source.IsEmpty)
            return false;

        if (ReferenceEquals(source, target))
            return true;

        ItemStack sourceStack = source.Stack;

        if (!targetSection.CanAccept(sourceStack))
            return false;

        if (target.IsEmpty)
        {
            target.Set(source.Take());
            return true;
        }

        ItemStack targetStack = target.Stack;

        if (targetStack.CanStackWith(sourceStack))
        {
            int sourceAmountBefore = sourceStack.Amount;
            int remaining = targetStack.AddAmount(sourceStack.Amount);
            int movedAmount = sourceAmountBefore - remaining;

            sourceStack.RemoveAmount(movedAmount);

            if (sourceStack.Amount <= 0)
                source.Clear();

            return movedAmount > 0;
        }

        if (!sourceSection.CanAccept(targetStack))
            return false;

        ItemStack takenSource = source.Take();
        ItemStack takenTarget = target.Take();

        source.Set(takenTarget);
        target.Set(takenSource);
        return true;
    }

    private AddItemResult AddStackable(ItemDefinition definition, int amount)
    {
        EnsureSlots();

        int remaining = amount;
        int added = 0;

        foreach (ItemSlot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot == null || slot.IsEmpty)
                continue;

            ItemStack incoming = ItemStack.CreateStackable(definition, Math.Min(remaining, definition.MaxStack));

            if (!slot.CanStackWith(incoming))
                continue;

            int before = remaining;
            remaining = slot.Stack.AddAmount(remaining);
            added += before - remaining;
        }

        foreach (ItemSlot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot == null || !slot.IsEmpty)
                continue;

            int stackAmount = Math.Min(remaining, definition.MaxStack);
            slot.Set(ItemStack.CreateStackable(definition, stackAmount));
            remaining -= stackAmount;
            added += stackAmount;
        }

        return new AddItemResult(added, remaining);
    }

    private AddItemResult AddNonStackable(ItemDefinition definition, int amount)
    {
        EnsureSlots();

        int remaining = amount;
        int added = 0;

        foreach (ItemSlot slot in slots)
        {
            if (remaining <= 0)
                break;

            if (slot == null || !slot.IsEmpty)
                continue;

            ItemInstance instance = new ItemInstance(definition);
            slot.Set(ItemStack.CreateNonStackable(instance));
            remaining--;
            added++;
        }

        return new AddItemResult(added, remaining);
    }

    private AddItemResult AddExistingStackable(ItemStack stack)
    {
        if (stack.Instance != null)
            return new AddItemResult(0, stack.Amount);

        return AddStackable(stack.Definition, stack.Amount);
    }

    private AddItemResult AddExistingNonStackable(ItemStack stack)
    {
        if (stack.Amount != 1 || stack.Instance == null)
            return new AddItemResult(0, Math.Max(0, stack.Amount));

        EnsureSlots();

        foreach (ItemSlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty)
                continue;

            slot.Set(stack);
            return new AddItemResult(1, 0);
        }

        return new AddItemResult(0, 1);
    }

    private void EnsureSlots()
    {
        slots ??= Array.Empty<ItemSlot>();

        for (int i = 0; i < slots.Length; i++)
        {
            slots[i] ??= new ItemSlot();
        }
    }
}
