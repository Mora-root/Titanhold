using System;
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

        public event Action<global::ItemCategory, int> SlotRightClicked;

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
                    view.SetSlot(null, section.Category, -1);
                    continue;
                }

                global::ItemSlot slot = slots != null && i < slots.Length ? slots[i] : null;
                view.SetSlot(slot, section.Category, i);
            }
        }

        public void Clear()
        {
            foreach (InventorySlotView view in slotViews)
            {
                if (view == null)
                    continue;

                view.SetSlot(null, default, -1);
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
                view.RightClicked += HandleSlotRightClicked;
                slotViews.Add(view);
            }
        }

        private void HandleSlotRightClicked(global::ItemCategory category, int slotIndex)
        {
            SlotRightClicked?.Invoke(category, slotIndex);
        }
    }
}
