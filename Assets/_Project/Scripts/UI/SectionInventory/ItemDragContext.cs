using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class ItemDragContext : MonoBehaviour
    {
        public bool HasSource => SourceKind != ItemDragSourceKind.None;
        public ItemDragSourceKind SourceKind { get; private set; } = ItemDragSourceKind.None;
        public global::ItemCategory SourceCategory { get; private set; }
        public int SourceIndex { get; private set; } = -1;
        public global::EquipmentSlotId SourceEquipmentSlotId { get; private set; }

        public void BeginInventory(global::ItemCategory category, int index)
        {
            SourceKind = ItemDragSourceKind.InventorySlot;
            SourceCategory = category;
            SourceIndex = index;
            SourceEquipmentSlotId = default;
        }

        public void BeginEquipment(global::EquipmentSlotId slotId)
        {
            SourceKind = ItemDragSourceKind.EquipmentSlot;
            SourceCategory = default;
            SourceIndex = -1;
            SourceEquipmentSlotId = slotId;
        }

        public void Clear()
        {
            SourceKind = ItemDragSourceKind.None;
            SourceCategory = default;
            SourceIndex = -1;
            SourceEquipmentSlotId = default;
        }
    }
}
