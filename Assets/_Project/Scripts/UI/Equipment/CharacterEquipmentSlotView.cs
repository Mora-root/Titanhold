using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentSlotView : MonoBehaviour,
        IPointerClickHandler,
        IDropHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler
    {
        [SerializeField] private global::EquipmentSlotId slotId;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;

        private global::ItemInstance currentItem;
        private bool dragHidden;

        public event Action<global::EquipmentSlotId> RightClicked;
        public event Action<global::EquipmentSlotId> Dropped;
        public event Action<global::EquipmentSlotId> DragStarted;
        public event Action<CharacterEquipmentSlotView, global::EquipmentSlotId> DragStartedWithView;
        public event Action DragEnded;

        public global::EquipmentSlotId SlotId => slotId;

        public void SetItem(global::ItemInstance item)
        {
            dragHidden = false;

            if (item == null || item.Definition == null)
            {
                Clear();
                return;
            }

            currentItem = item;
            RefreshDisplay();
        }

        public void SetDragHidden(bool hidden)
        {
            dragHidden = hidden;
            RefreshDisplay();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            if (currentItem == null || currentItem.Definition == null)
                return;

            RightClicked?.Invoke(slotId);
        }

        public void OnDrop(PointerEventData eventData)
        {
            Dropped?.Invoke(slotId);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (currentItem == null || currentItem.Definition == null)
                return;

            DragStarted?.Invoke(slotId);
            DragStartedWithView?.Invoke(this, slotId);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnded?.Invoke();
        }

        public void Clear()
        {
            currentItem = null;
            dragHidden = false;
            RefreshDisplay();
        }

        private void RefreshDisplay()
        {
            if (dragHidden || currentItem == null || currentItem.Definition == null)
            {
                HideItemVisuals();
                return;
            }

            global::ItemDefinition definition = currentItem.Definition;
            Sprite icon = definition.Icon;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (emptyState != null)
                emptyState.SetActive(false);

            if (filledState != null)
                filledState.SetActive(true);
        }

        private void HideItemVisuals()
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (nameText != null)
                nameText.text = string.Empty;

            if (emptyState != null)
                emptyState.SetActive(true);

            if (filledState != null)
                filledState.SetActive(false);
        }
    }
}
