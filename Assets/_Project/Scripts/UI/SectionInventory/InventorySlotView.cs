using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Titanhold.UI.SectionInventory
{
    public sealed class InventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text amountText;
        [SerializeField] private TMP_Text fallbackNameText;
        [SerializeField] private LootItemTooltip tooltip;

        private RectTransform rectTransform;
        private global::ItemSlot currentSlot;
        private global::ItemDefinition currentDefinition;

        private void Awake()
        {
            rectTransform = transform as RectTransform;
            tooltip ??= FindAnyObjectByType<LootItemTooltip>(FindObjectsInactive.Include);
        }

        public void SetSlot(global::ItemSlot slot)
        {
            currentSlot = slot;

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
