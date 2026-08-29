using System;
using TMPro;
using Titanhold.UI.Common;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventorySlotView : MonoBehaviour,
        IItemDragSourceView,
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
        [SerializeField] private ItemTooltipController tooltipController;

        private RectTransform rectTransform;
        private global::ItemSlot currentSlot;
        private global::ItemDefinition currentDefinition;
        private global::ItemCategory currentCategory;
        private int currentSlotIndex = -1;
        private bool dragHidden;
        private bool isPointerInside;

        public event Action<global::ItemCategory, int> RightClicked;
        public event Action<global::ItemCategory, int> DragStarted;
        public event Action<InventorySlotView, global::ItemCategory, int> DragStartedWithView;
        public event Action<global::ItemCategory, int> Dropped;
        public event Action DragEnded;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            ResolveTooltipController();
        }

        public void SetTooltipController(ItemTooltipController controller)
        {
            tooltipController = controller;
        }

        public void SetSlot(global::ItemSlot slot)
        {
            SetSlot(slot, default, -1);
        }

        public void SetSlot(global::ItemSlot slot, global::ItemCategory category, int slotIndex)
        {
            dragHidden = false;
            currentSlot = slot;
            currentCategory = category;
            currentSlotIndex = slotIndex;

            if (slot == null || slot.IsEmpty || slot.Stack == null || slot.Stack.Definition == null)
            {
                currentDefinition = null;
                RefreshDisplay();
                HideTooltip();
                return;
            }

            global::ItemStack stack = slot.Stack;
            currentDefinition = stack.Definition;

            RefreshDisplay();
            RefreshTooltipIfHovered();
        }

        public void SetDragHidden(bool hidden)
        {
            dragHidden = hidden;
            RefreshDisplay();

            if (hidden)
                HideTooltip();
            else
                RefreshTooltipIfHovered();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isPointerInside = true;
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isPointerInside = false;
            HideTooltip();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Right)
                return;

            if (currentSlot == null || currentSlot.IsEmpty || currentSlotIndex < 0)
                return;

            RightClicked?.Invoke(currentCategory, currentSlotIndex);
            SyncCurrentDefinitionFromSlot();
            RefreshDisplay();
            RefreshTooltipIfHovered();
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (currentSlot == null || currentSlot.IsEmpty || currentSlotIndex < 0)
                return;

            HideTooltip();
            DragStarted?.Invoke(currentCategory, currentSlotIndex);
            DragStartedWithView?.Invoke(this, currentCategory, currentSlotIndex);
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

            isPointerInside = true;
            Dropped?.Invoke(currentCategory, currentSlotIndex);
            SyncCurrentDefinitionFromSlot();
            RefreshDisplay();
            RefreshTooltipIfHovered();
        }

        private void OnDisable()
        {
            isPointerInside = false;
            HideTooltip();
        }

        private void Clear()
        {
            currentDefinition = null;
            dragHidden = false;
            RefreshDisplay();
            HideTooltip();
        }

        private void RefreshDisplay()
        {
            if (dragHidden || currentSlot == null || currentSlot.IsEmpty || currentSlot.Stack == null || currentDefinition == null)
            {
                HideItemVisuals();
                return;
            }

            global::ItemStack stack = currentSlot.Stack;
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

        private void HideItemVisuals()
        {
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

        private void RefreshTooltipIfHovered()
        {
            if (!isPointerInside && !IsPointerCurrentlyOverSlot())
                return;

            isPointerInside = true;
            ShowTooltip();
        }

        private void ShowTooltip()
        {
            SyncCurrentDefinitionFromSlot();

            if (dragHidden || currentSlot == null || currentSlot.IsEmpty || currentSlot.Stack == null || currentDefinition == null)
            {
                HideTooltip();
                return;
            }

            ItemTooltipData data = ItemTooltipBuilder.Build(currentSlot.Stack);
            if (data == null)
            {
                HideTooltip();
                return;
            }

            ResolveTooltipController();
            tooltipController?.Show(data, rectTransform);
        }

        private void HideTooltip()
        {
            tooltipController?.Hide();
        }

        private void ResolveTooltipController()
        {
            if (tooltipController != null)
                return;

            tooltipController = GetComponentInParent<ItemTooltipController>(true);
            if (tooltipController != null)
                return;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            if (canvas != null)
                tooltipController = canvas.GetComponentInChildren<ItemTooltipController>(true);
        }

        private bool IsPointerCurrentlyOverSlot()
        {
            if (rectTransform == null)
                return false;

            Canvas canvas = GetComponentInParent<Canvas>(true);
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? canvas.worldCamera
                : null;

            return RectTransformUtility.RectangleContainsScreenPoint(
                rectTransform,
                Input.mousePosition,
                eventCamera);
        }

        private void SyncCurrentDefinitionFromSlot()
        {
            if (currentSlot == null || currentSlot.IsEmpty || currentSlot.Stack == null || currentSlot.Stack.Definition == null)
            {
                currentDefinition = null;
                return;
            }

            currentDefinition = currentSlot.Stack.Definition;
        }
    }
}
