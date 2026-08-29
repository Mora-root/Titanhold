public readonly struct ItemSlotAddress
{
    public ItemSlotAddress(ItemContainer container, ItemCategory category, int slotIndex)
    {
        Container = container;
        Category = category;
        SlotIndex = slotIndex;
    }

    public ItemContainer Container { get; }
    public ItemCategory Category { get; }
    public int SlotIndex { get; }

    public bool IsValid
    {
        get
        {
            ItemContainerSection section = GetSection();
            return section != null &&
                   section.Slots != null &&
                   SlotIndex >= 0 &&
                   SlotIndex < section.Slots.Length &&
                   section.Slots[SlotIndex] != null;
        }
    }

    public ItemContainerSection GetSection()
    {
        return Container != null ? Container.GetSection(Category) : null;
    }

    public ItemSlot GetSlot()
    {
        ItemContainerSection section = GetSection();
        return section != null ? section.GetSlot(SlotIndex) : null;
    }
}
