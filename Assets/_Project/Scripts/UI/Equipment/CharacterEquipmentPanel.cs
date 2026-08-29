using System;
using Titanhold.UI.Common;
using UnityEngine;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentPanel : MonoBehaviour, IItemSlotEventSource
    {
        [SerializeField] private global::PlayerEquipmentRuntime equipmentRuntime;
        [SerializeField] private CharacterEquipmentSlotView[] slotViews;

        private global::CharacterEquipment subscribedEquipment;
        private bool loggedMissingRuntime;

        public event Action<global::EquipmentSlotId> SlotRightClicked;
        public event Action<global::EquipmentSlotId> SlotDropped;
        public event Action<global::EquipmentSlotId> SlotDragStarted;
        public event Action<CharacterEquipmentSlotView, global::EquipmentSlotId> SlotDragStartedWithView;
        public event Action SlotDragEnded;
        public event Action<global::ItemSlotRef> ItemSlotRightClicked;
        public event Action<IItemDragSourceView, global::ItemSlotRef, ItemDragVisualData> ItemSlotDragStarted;
        public event Action<global::ItemSlotRef> ItemSlotDropped;
        public event Action ItemSlotDragEnded;

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

        public void SetEquipmentRuntime(global::PlayerEquipmentRuntime runtime)
        {
            if (ReferenceEquals(equipmentRuntime, runtime))
            {
                RefreshAll();
                return;
            }

            if (isActiveAndEnabled)
                Unsubscribe();

            equipmentRuntime = runtime;
            loggedMissingRuntime = false;

            if (isActiveAndEnabled)
                Subscribe();

            RefreshAll();
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
                    slotView.DragStartedWithView += HandleSlotDragStartedWithView;
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
                    slotView.DragStartedWithView -= HandleSlotDragStartedWithView;
                    slotView.DragEnded -= HandleSlotDragEnded;
                }
            }
        }

        private void HandleSlotRightClicked(global::EquipmentSlotId slotId)
        {
            SlotRightClicked?.Invoke(slotId);

            global::ItemSlotRef slotRef = CreateSlotRef(slotId);
            if (slotRef.IsValid)
                ItemSlotRightClicked?.Invoke(slotRef);
        }

        private void HandleSlotDropped(global::EquipmentSlotId slotId)
        {
            SlotDropped?.Invoke(slotId);

            global::ItemSlotRef slotRef = CreateSlotRef(slotId);
            if (slotRef.IsValid)
                ItemSlotDropped?.Invoke(slotRef);
        }

        private void HandleSlotDragStarted(global::EquipmentSlotId slotId)
        {
            SlotDragStarted?.Invoke(slotId);
        }

        private void HandleSlotDragStartedWithView(CharacterEquipmentSlotView slotView, global::EquipmentSlotId slotId)
        {
            SlotDragStartedWithView?.Invoke(slotView, slotId);

            global::ItemSlotRef slotRef = CreateSlotRef(slotId);
            if (!slotRef.IsValid)
                return;

            global::ItemInstance item = GetEquipment()?.GetEquipped(slotId);
            global::ItemDefinition definition = item?.Definition;
            if (definition == null)
                return;

            ItemSlotDragStarted?.Invoke(
                slotView,
                slotRef,
                new ItemDragVisualData(definition.Icon, 1));
        }

        private void HandleSlotDragEnded()
        {
            SlotDragEnded?.Invoke();
            ItemSlotDragEnded?.Invoke();
        }

        private global::ItemSlotRef CreateSlotRef(global::EquipmentSlotId slotId)
        {
            return global::ItemSlotRef.ForEquipment(equipmentRuntime, slotId);
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
