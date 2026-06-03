using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class LootInventorySlotView : MonoBehaviour
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text fallbackNameText;

    public void Setup(PlayerLootInventory.LootInventorySlotView slot)
    {
        if (slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (amountText != null)
                amountText.text = "";

            if (fallbackNameText != null)
                fallbackNameText.text = "";

            return;
        }

        Sprite icon = slot.Item != null ? slot.Item.Icon : null;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (fallbackNameText != null)
            fallbackNameText.text = icon == null && slot.Item != null ? slot.Item.ShortName : "";

        if (amountText != null)
            amountText.text = slot.Amount > 1 ? slot.Amount.ToString() : "";
    }
}
