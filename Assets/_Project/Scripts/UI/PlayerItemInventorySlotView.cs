using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerItemInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text fallbackNameText;
    [SerializeField] private EquipmentItemTooltip tooltip;
    [SerializeField] private PlayerItemInventoryEquipmentAdapter equipAdapter;

    private RectTransform rectTransform;
    private EquipmentItemDefinition currentItem;
    private int slotIndex = -1;

    public int SlotIndex => slotIndex;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        tooltip ??= FindAnyObjectByType<EquipmentItemTooltip>(FindObjectsInactive.Include);
    }

    public void Setup(PlayerItemInventory.ItemInventorySlotView slot)
    {
        Setup(slot, equipAdapter);
    }

    public void Setup(PlayerItemInventory.ItemInventorySlotView slot, PlayerItemInventoryEquipmentAdapter equipAdapter)
    {
        slotIndex = slot.Index;
        currentItem = slot.Item;

        if (equipAdapter != null)
            this.equipAdapter = equipAdapter;

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
            tooltip?.ShowLeftOf(currentItem, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right)
            return;

        if (slotIndex < 0)
            return;

        equipAdapter?.TryEquipFromSlot(slotIndex);
    }

    private void OnDisable()
    {
        tooltip?.Hide();
    }
}
