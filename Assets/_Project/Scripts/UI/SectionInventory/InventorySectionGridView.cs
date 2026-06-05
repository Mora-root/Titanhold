using System.Collections.Generic;
using UnityEngine;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventorySectionGridView : MonoBehaviour
    {
        [SerializeField] private Transform contentRoot;
        [SerializeField] private InventorySlotView slotPrefab;

        private readonly List<InventorySlotView> slotViews = new();
        private bool loggedMissingSetup;

        public void ShowSection(global::ItemContainerSection section)
        {
            if (section == null)
            {
                Clear();
                return;
            }

            if (!CanRender())
            {
                Clear();
                return;
            }

            EnsureViewCount(section.Capacity);

            global::ItemSlot[] slots = section.Slots;

            for (int i = 0; i < slotViews.Count; i++)
            {
                InventorySlotView view = slotViews[i];
                bool shouldShow = i < section.Capacity;

                view.gameObject.SetActive(shouldShow);

                if (!shouldShow)
                {
                    view.SetSlot(null);
                    continue;
                }

                global::ItemSlot slot = slots != null && i < slots.Length ? slots[i] : null;
                view.SetSlot(slot);
            }
        }

        public void Clear()
        {
            foreach (InventorySlotView view in slotViews)
            {
                if (view == null)
                    continue;

                view.SetSlot(null);
                view.gameObject.SetActive(false);
            }
        }

        private bool CanRender()
        {
            if (contentRoot != null && slotPrefab != null)
                return true;

            if (!loggedMissingSetup)
            {
                Debug.LogWarning($"{nameof(InventorySectionGridView)} requires ContentRoot and SlotPrefab references.", this);
                loggedMissingSetup = true;
            }

            return false;
        }

        private void EnsureViewCount(int count)
        {
            while (slotViews.Count < count)
            {
                InventorySlotView view = Instantiate(slotPrefab, contentRoot);
                slotViews.Add(view);
            }
        }
    }
}
