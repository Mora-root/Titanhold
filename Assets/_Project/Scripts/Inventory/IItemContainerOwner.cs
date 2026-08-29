using System;

public interface IItemContainerOwner
{
    ItemContainer Container { get; }
    ItemContainerOwnerKind OwnerKind { get; }
    string OwnerId { get; }

    event Action Changed;
    event Action<ItemCategory> SectionChanged;

    ItemContainerSection GetSection(ItemCategory category);
    ItemSlot GetSlot(ItemCategory category, int index);
    int CountFreeSlots(ItemCategory category);
    int CountOccupiedSlots(ItemCategory category);
    void NotifyChanged(ItemCategory category);
    void NotifyTransferChanged(ItemCategory sourceCategory, ItemCategory targetCategory);
}
