using System;
using UnityEngine;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentPanel : MonoBehaviour
    {
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private CharacterEquipmentSlotView[] slotViews;

        private global::CharacterEquipment subscribedEquipment;
        private bool loggedMissingRuntime;

        public event Action<global::EquipmentSlotId> SlotRightClicked;
        public event Action<global::EquipmentSlotId> SlotDropped;
        public event Action<global::EquipmentSlotId> SlotDragStarted;
        public event Action SlotDragEnded;

        private void OnEnable()
        {
            SubscribeSlotViews();
            Subscribe();
            RefreshAll();
        }

        private void OnDisable()
        {
            UnsubscribeSlotViews();
            Unsubscribe();
        }

        public void RefreshAll()
        {
            global::CharacterEquipment equipment = GetEquipment();
            if (equipment == null)
            {
                ClearAll();
                LogMissingRuntime();
                return;
            }

            if (slotViews == null)
                return;

            foreach (CharacterEquipmentSlotView slotView in slotViews)
            {
                if (slotView == null)
                    continue;

                slotView.SetItem(equipment.GetEquipped(slotView.SlotId));
            }
        }

        public void RefreshSlot(global::EquipmentSlotId slotId)
        {
            global::CharacterEquipment equipment = GetEquipment();
            if (equipment == null)
            {
                ClearAll();
                LogMissingRuntime();
                return;
            }

            if (slotViews == null)
                return;

            foreach (CharacterEquipmentSlotView slotView in slotViews)
            {
                if (slotView == null || slotView.SlotId != slotId)
                    continue;

                slotView.SetItem(equipment.GetEquipped(slotId));
            }
        }

        private void Subscribe()
        {
            global::CharacterEquipment equipment = GetEquipment();
            if (equipment == null)
            {
                LogMissingRuntime();
                return;
            }

            if (ReferenceEquals(subscribedEquipment, equipment))
                return;

            Unsubscribe();
            subscribedEquipment = equipment;
            subscribedEquipment.SlotChanged += HandleSlotChanged;
        }

        private void Unsubscribe()
        {
            if (subscribedEquipment == null)
                return;

            subscribedEquipment.SlotChanged -= HandleSlotChanged;
            subscribedEquipment = null;
        }

        private void HandleSlotChanged(
            global::EquipmentSlotId slotId,
            global::ItemInstance oldItem,
            global::ItemInstance newItem)
        {
            RefreshSlot(slotId);
        }

        private void SubscribeSlotViews()
        {
            if (slotViews == null)
                return;

            foreach (CharacterEquipmentSlotView slotView in slotViews)
            {
                if (slotView != null)
                {
                    slotView.RightClicked += HandleSlotRightClicked;
                    slotView.Dropped += HandleSlotDropped;
                    slotView.DragStarted += HandleSlotDragStarted;
                    slotView.DragEnded += HandleSlotDragEnded;
                }
            }
        }

        private void UnsubscribeSlotViews()
        {
            if (slotViews == null)
                return;

            foreach (CharacterEquipmentSlotView slotView in slotViews)
            {
                if (slotView != null)
                {
                    slotView.RightClicked -= HandleSlotRightClicked;
                    slotView.Dropped -= HandleSlotDropped;
                    slotView.DragStarted -= HandleSlotDragStarted;
                    slotView.DragEnded -= HandleSlotDragEnded;
                }
            }
        }

        private void HandleSlotRightClicked(global::EquipmentSlotId slotId)
        {
            SlotRightClicked?.Invoke(slotId);
        }

        private void HandleSlotDropped(global::EquipmentSlotId slotId)
        {
            SlotDropped?.Invoke(slotId);
        }

        private void HandleSlotDragStarted(global::EquipmentSlotId slotId)
        {
            SlotDragStarted?.Invoke(slotId);
        }

        private void HandleSlotDragEnded()
        {
            SlotDragEnded?.Invoke();
        }

        private global::CharacterEquipment GetEquipment()
        {
            if (equipmentRuntime == null)
                return null;

            return equipmentRuntime.Equipment;
        }

        private void ClearAll()
        {
            if (slotViews == null)
                return;

            foreach (CharacterEquipmentSlotView slotView in slotViews)
            {
                if (slotView != null)
                    slotView.Clear();
            }
        }

        private void LogMissingRuntime()
        {
            if (loggedMissingRuntime)
                return;

            Debug.LogWarning($"{nameof(CharacterEquipmentPanel)} requires a PlayerEquipmentRuntime reference.", this);
            loggedMissingRuntime = true;
        }
    }
}
