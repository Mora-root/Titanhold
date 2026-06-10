public readonly struct ItemSlotRef
{
    private ItemSlotRef(
        ItemSlotRefKind kind,
        IItemContainerOwner containerOwner,
        ItemCategory category,
        int slotIndex,
        IEquipmentRuntimeOwner equipmentOwner,
        EquipmentSlotId equipmentSlotId)
    {
        Kind = kind;
        ContainerOwner = containerOwner;
        Category = category;
        SlotIndex = slotIndex;
        EquipmentOwner = equipmentOwner;
        EquipmentSlotId = equipmentSlotId;
    }

    public static ItemSlotRef None { get; } = new ItemSlotRef(
        ItemSlotRefKind.None,
        null,
        default,
        -1,
        null,
        default);

    public ItemSlotRefKind Kind { get; }
    public IItemContainerOwner ContainerOwner { get; }
    public ItemCategory Category { get; }
    public int SlotIndex { get; }
    public IEquipmentRuntimeOwner EquipmentOwner { get; }
    public EquipmentSlotId EquipmentSlotId { get; }

    public bool IsContainerSlot => Kind == ItemSlotRefKind.ContainerSlot &&
                                   ContainerOwner != null &&
                                   SlotIndex >= 0;

    public bool IsEquipmentSlot => Kind == ItemSlotRefKind.EquipmentSlot &&
                                   EquipmentOwner != null;

    public bool IsValid => IsContainerSlot || IsEquipmentSlot;

    public static ItemSlotRef ForContainer(IItemContainerOwner owner, ItemCategory category, int slotIndex)
    {
        return new ItemSlotRef(ItemSlotRefKind.ContainerSlot, owner, category, slotIndex, null, default);
    }

    public static ItemSlotRef ForEquipment(IEquipmentRuntimeOwner owner, EquipmentSlotId slotId)
    {
        return new ItemSlotRef(ItemSlotRefKind.EquipmentSlot, null, default, -1, owner, slotId);
    }
}
