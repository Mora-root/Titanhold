using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerItemInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text fallbackNameText;
    [SerializeField] private EquipmentItemTooltip tooltip;

    private EquipmentItemDefinition currentItem;
    private int slotIndex = -1;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        tooltip ??= FindAnyObjectByType<EquipmentItemTooltip>(FindObjectsInactive.Include);
    }

    public void Setup(PlayerItemInventory.ItemInventorySlotView slot)
    {
        slotIndex = slot.Index;
        currentItem = slot.Item;

        if (slot.IsEmpty)
        {
            if (iconImage != null)
            {
                iconImage.sprite = null;
                iconImage.enabled = false;
            }

            if (fallbackNameText != null)
                fallbackNameText.text = "";

            tooltip?.Hide();
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
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentItem != null)
            tooltip?.Show(currentItem, eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide();
    }

    private void OnDisable()
    {
        tooltip?.Hide();
    }
}
