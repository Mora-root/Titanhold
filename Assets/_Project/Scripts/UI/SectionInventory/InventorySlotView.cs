using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventorySlotView : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler,
        IBeginDragHandler,
        IDragHandler,
        IEndDragHandler,
        IDropHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text fallbackNameText;
        [SerializeField] private LootItemTooltip tooltip;

        private RectTransform rectTransform;
        private global::ItemSlot currentSlot;
        private global::ItemDefinition currentDefinition;
        private global::ItemCategory currentCategory;
        private int currentSlotIndex = -1;

        public event Action<global::ItemCategory, int> RightClicked;
        public event Action<global::ItemCategory, int> DragStarted;
        public event Action<global::ItemCategory, int> Dropped;
        public event Action DragEnded;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            tooltip ??= FindAnyObjectByType<LootItemTooltip>(FindObjectsInactive.Include);
        }

        public void SetSlot(global::ItemSlot slot)
        {
            SetSlot(slot, default, -1);
        }

        public void SetSlot(global::ItemSlot slot, global::ItemCategory category, int slotIndex)
        {
            currentSlot = slot;
            currentCategory = category;
            currentSlotIndex = slotIndex;

            if (slot == null || slot.IsEmpty || slot.Stack == null || slot.Stack.Definition == null)
            {
                Clear();
                return;
            }

            global::ItemStack stack = slot.Stack;
            currentDefinition = stack.Definition;

            Sprite icon = currentDefinition.Icon;

            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }

            if (fallbackNameText != null)
                fallbackNameText.text = icon == null ? currentDefinition.ShortName : string.Empty;

            if (amountText != null)
                amountText.text = stack.Amount > 1 ? stack.Amount.ToString() : string.Empty;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (currentSlot == null || currentSlot.IsEmpty || currentDefinition == null)
                return;

            tooltip?.ShowLeftOf(currentDefinition, rectTransform);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            tooltip?.Hide();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            if (currentSlot == null || currentSlot.IsEmpty || currentSlotIndex < 0)
                return;

            RightClicked?.Invoke(currentCategory, currentSlotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (currentSlot == null || currentSlot.IsEmpty || currentSlotIndex < 0)
                return;

            tooltip?.Hide();
            DragStarted?.Invoke(currentCategory, currentSlotIndex);
        }

        public void OnDrag(PointerEventData eventData)
        {
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            DragEnded?.Invoke();
        }

        public void OnDrop(PointerEventData eventData)
        {
            if (currentSlotIndex < 0)
                return;

            Dropped?.Invoke(currentCategory, currentSlotIndex);
        }

        private void OnDisable()
        {
            tooltip?.Hide();
        }

        private void Clear()
        {
            currentDefinition = null;

            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (amountText != null)
                amountText.text = string.Empty;

            if (fallbackNameText != null)
                fallbackNameText.text = string.Empty;
        }
    }
}
