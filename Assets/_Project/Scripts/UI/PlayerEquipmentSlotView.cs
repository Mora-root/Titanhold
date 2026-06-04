using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class PlayerEquipmentSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private EquipmentSlot slot;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text fallbackNameText;
    [SerializeField] private GameObject emptyVisual;
    [SerializeField] private GameObject filledVisual;
    [SerializeField] private EquipmentItemTooltip tooltip;
    [SerializeField] private PlayerItemInventoryEquipmentAdapter itemInventoryAdapter;

    private RectTransform rectTransform;
    private EquipmentItemDefinition currentItem;

    public EquipmentSlot Slot => slot;

    private void Awake()
    {
        rectTransform = transform as RectTransform;
        tooltip ??= FindAnyObjectByType<EquipmentItemTooltip>(FindObjectsInactive.Include);
    }

    public void Setup(EquipmentItemDefinition item)
    {
        Setup(item, itemInventoryAdapter);
    }

    public void Setup(EquipmentItemDefinition item, PlayerItemInventoryEquipmentAdapter itemInventoryAdapter)
    {
        currentItem = item;

        if (itemInventoryAdapter != null)
            this.itemInventoryAdapter = itemInventoryAdapter;

        bool isEmpty = item == null;

        if (emptyVisual != null)
            emptyVisual.SetActive(isEmpty);

        if (filledVisual != null)
            filledVisual.SetActive(!isEmpty);

        if (isEmpty)
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

        Sprite icon = item.Icon;

        if (iconImage != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = icon != null;
        }

        if (fallbackNameText != null)
            fallbackNameText.text = icon == null ? item.ShortName : "";
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

        if (currentItem == null)
            return;

        itemInventoryAdapter?.TryUnequipToInventory(slot);
    }

    private void OnDisable()
    {
        tooltip?.Hide();
    }
}
