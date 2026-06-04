using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class LootInventorySlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text fallbackNameText;
    [SerializeField] private LootItemTooltip tooltip;
    [SerializeField] private PlayerLootInventoryDragController dragController;

    private RectTransform rectTransform;
    private int slotIndex = -1;
    private LootItemDefinition currentItem;
    private int currentAmount;
    private bool isEmpty = true;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        tooltip ??= FindAnyObjectByType<LootItemTooltip>(FindObjectsInactive.Include);
    }

    public void Setup(PlayerLootInventory.LootInventorySlotView slot)
    {
        Setup(slot, dragController);
    }

    public void Setup(PlayerLootInventory.LootInventorySlotView slot, PlayerLootInventoryDragController dragController)
    {
        if (dragController != null)
            this.dragController = dragController;

        slotIndex = slot.Index;
        currentItem = slot.Item;
        currentAmount = slot.Amount;
        isEmpty = slot.IsEmpty;

        if (slot.IsEmpty)
        {
            tooltip?.Hide();

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

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (isEmpty || currentItem == null)
            return;

        tooltip?.ShowLeftOf(currentItem, rectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tooltip?.Hide();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isEmpty || currentItem == null)
            return;

        tooltip?.Hide();
        dragController?.BeginDrag(slotIndex, currentItem, currentAmount, eventData.position);
    }

    public void OnDrag(PointerEventData eventData)
    {
        dragController?.Drag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragController?.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        dragController?.DropOn(slotIndex);
    }

    private void OnDisable()
    {
        tooltip?.Hide();
    }
}
