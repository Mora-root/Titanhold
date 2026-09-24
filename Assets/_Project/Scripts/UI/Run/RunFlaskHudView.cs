using System;
using System.Collections.Generic;
using Titanhold.Combat.Flasks;
using UnityEngine;

namespace Titanhold.UI.Run
{
    [DisallowMultipleComponent]
    public sealed class RunFlaskHudView : MonoBehaviour
    {
        [SerializeField] private RunFlaskSlotView[] slots =
            Array.Empty<RunFlaskSlotView>();

        private readonly List<RunFlaskSlotView> subscribedSlots = new();

        public event Action<int> SlotPressed;

        public int SlotCount => slots?.Length ?? 0;
        public IReadOnlyList<RunFlaskSlotView> Slots =>
            slots ?? Array.Empty<RunFlaskSlotView>();

#if UNITY_EDITOR
        public void ConfigureForEditor(RunFlaskSlotView[] configuredSlots)
        {
            slots = configuredSlots ?? Array.Empty<RunFlaskSlotView>();
            if (isActiveAndEnabled)
                RefreshSubscriptions();
        }
#endif

        private void OnEnable()
        {
            RefreshSubscriptions();
        }

        private void OnDisable()
        {
            ClearSubscriptions();
        }

        public bool TryRenderContent(
            int slotIndex,
            string flaskId,
            string displayName,
            Sprite icon,
            string keyLabel)
        {
            if (!TryGetSlot(slotIndex, out RunFlaskSlotView slot))
                return false;

            slot.RenderContent(
                slotIndex,
                flaskId,
                displayName,
                icon,
                keyLabel);
            return true;
        }

        public bool TryRenderCooldown(
            int slotIndex,
            FlaskCooldownSnapshot cooldown)
        {
            if (!TryGetSlot(slotIndex, out RunFlaskSlotView slot))
                return false;

            slot.RenderCooldown(cooldown);
            return true;
        }

        public bool TryRenderReady(int slotIndex)
        {
            if (!TryGetSlot(slotIndex, out RunFlaskSlotView slot))
                return false;

            slot.RenderReady();
            return true;
        }

        public void Clear()
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
                slots[i]?.RenderContent(i, string.Empty, string.Empty, null, string.Empty);
        }

        private bool TryGetSlot(
            int slotIndex,
            out RunFlaskSlotView slot)
        {
            slot = null;
            if (slots == null ||
                slotIndex < 0 ||
                slotIndex >= slots.Length)
            {
                return false;
            }

            slot = slots[slotIndex];
            return slot != null;
        }

        private void RefreshSubscriptions()
        {
            ClearSubscriptions();
            if (slots == null)
                return;

            for (int i = 0; i < slots.Length; i++)
            {
                RunFlaskSlotView slot = slots[i];
                if (slot == null || subscribedSlots.Contains(slot))
                    continue;

                slot.UseRequested += HandleSlotPressed;
                subscribedSlots.Add(slot);
            }
        }

        private void ClearSubscriptions()
        {
            for (int i = 0; i < subscribedSlots.Count; i++)
            {
                if (subscribedSlots[i] != null)
                    subscribedSlots[i].UseRequested -= HandleSlotPressed;
            }

            subscribedSlots.Clear();
        }

        private void HandleSlotPressed(int slotIndex)
        {
            SlotPressed?.Invoke(slotIndex);
        }
    }
}
