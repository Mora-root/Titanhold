using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventoryDragContext : MonoBehaviour
    {
        public bool HasSource { get; private set; }
        public global::ItemCategory SourceCategory { get; private set; }
        public int SourceIndex { get; private set; } = -1;

        public void Begin(global::ItemCategory category, int index)
        {
            HasSource = true;
            SourceCategory = category;
            SourceIndex = index;
        }

        public void Clear()
        {
            HasSource = false;
            SourceCategory = default;
            SourceIndex = -1;
        }
    }
}
