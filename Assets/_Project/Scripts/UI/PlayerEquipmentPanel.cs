using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerEquipmentPanel : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private PlayerEquipment playerEquipment;
    [SerializeField] private PlayerEquipmentSlotView[] slotViews;
    [SerializeField] private PlayerItemInventoryEquipmentAdapter itemInventoryAdapter;
    [SerializeField] private Button closeButton;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (root == null)
            root = gameObject;

        playerEquipment ??= FindAnyObjectByType<PlayerEquipment>();
        itemInventoryAdapter ??= FindAnyObjectByType<PlayerItemInventoryEquipmentAdapter>();

        if (root != null)
            root.SetActive(false);

        IsOpen = false;
    }

    private void OnEnable()
    {
        if (playerEquipment != null)
            playerEquipment.OnEquipmentChanged += HandleEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        Refresh();
    }

    private void OnDisable()
    {
        if (playerEquipment != null)
            playerEquipment.OnEquipmentChanged -= HandleEquipmentChanged;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
    }

    public void Open()
    {
        if (root != null)
            root.SetActive(true);

        IsOpen = true;
        Refresh();
    }

    public void Close()
    {
        if (root != null)
            root.SetActive(false);

        IsOpen = false;
    }

    public void Refresh()
    {
        if (slotViews == null)
            return;

        foreach (PlayerEquipmentSlotView slotView in slotViews)
        {
            if (slotView == null)
                continue;

            ItemDefinition item = playerEquipment != null
                ? playerEquipment.GetEquipped(slotView.Slot)
                : null;

            slotView.Setup(item, itemInventoryAdapter);
        }
    }

    private void HandleEquipmentChanged(EquipmentSlotId slot, ItemDefinition item)
    {
        Refresh();
    }
}
