using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.Equipment
{
    public sealed class CharacterEquipmentSlotView : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private global::EquipmentSlotId slotId;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject emptyState;
        [SerializeField] private GameObject filledState;

        private global::ItemInstance currentItem;

        public event Action<global::EquipmentSlotId> RightClicked;

        public global::EquipmentSlotId SlotId => slotId;

        public void SetItem(global::ItemInstance item)
        {
            if (item == null || item.Definition == null)
            {
                Clear();
                return;
            }

            currentItem = item;
            global::ItemDefinition definition = item.Definition;
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

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            if (currentItem == null || currentItem.Definition == null)
                return;

            RightClicked?.Invoke(slotId);
        }

        public void Clear()
        {
            currentItem = null;

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
