using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class ItemDragContext : MonoBehaviour
    {
        public bool HasSource => SourceRef.IsValid || SourceKind != ItemDragSourceKind.None;
        public bool IsDragging => HasSource;
        public global::ItemSlotRef SourceRef { get; private set; } = global::ItemSlotRef.None;
        public ItemDragSourceKind SourceKind { get; private set; } = ItemDragSourceKind.None;
        public global::ItemCategory SourceCategory { get; private set; }
        public int SourceIndex { get; private set; } = -1;
        public global::EquipmentSlotId SourceEquipmentSlotId { get; private set; }

        public void Begin(global::ItemSlotRef source)
        {
            SourceRef = source.IsValid ? source : global::ItemSlotRef.None;

            if (source.IsContainerSlot)
            {
                SourceKind = ItemDragSourceKind.ContainerSlot;
                SourceCategory = source.Category;
                SourceIndex = source.SlotIndex;
                SourceEquipmentSlotId = default;
                return;
            }

            if (source.IsEquipmentSlot)
            {
                SourceKind = ItemDragSourceKind.EquipmentSlot;
                SourceCategory = default;
                SourceIndex = -1;
                SourceEquipmentSlotId = source.EquipmentSlotId;
                return;
            }

            Clear();
        }

        public void BeginInventory(global::ItemCategory category, int index)
        {
            SourceRef = global::ItemSlotRef.None;
            SourceKind = ItemDragSourceKind.InventorySlot;
            SourceCategory = category;
            SourceIndex = index;
            SourceEquipmentSlotId = default;
        }

        public void BeginEquipment(global::EquipmentSlotId slotId)
        {
            SourceRef = global::ItemSlotRef.None;
            SourceKind = ItemDragSourceKind.EquipmentSlot;
            SourceCategory = default;
            SourceIndex = -1;
            SourceEquipmentSlotId = slotId;
        }

        public void Clear()
        {
            SourceRef = global::ItemSlotRef.None;
            SourceKind = ItemDragSourceKind.None;
            SourceCategory = default;
            SourceIndex = -1;
            SourceEquipmentSlotId = default;
        }
    }
}
