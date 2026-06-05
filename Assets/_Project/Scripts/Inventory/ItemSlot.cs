using System;
using UnityEngine;

[Serializable]
public sealed class ItemSlot
{
    [SerializeField] private ItemStack stack;

    public ItemStack Stack => stack;
    public bool IsEmpty => stack == null;

    public void Set(ItemStack stack)
    {
        this.stack = stack ?? throw new ArgumentNullException(nameof(stack));
    }

    public ItemStack Take()
    {
        ItemStack taken = stack;
        stack = null;
        return taken;
    }

    public void Clear()
    {
        stack = null;
    }

    public bool CanStackWith(ItemStack incoming)
    {
        return !IsEmpty && stack.CanStackWith(incoming);
    }
}
