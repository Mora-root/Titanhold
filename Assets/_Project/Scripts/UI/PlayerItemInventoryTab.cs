using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PlayerItemInventoryTab : MonoBehaviour
{
    [SerializeField] private PlayerItemInventory playerItemInventory;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private PlayerItemInventorySlotView slotPrefab;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<PlayerItemInventory.ItemInventorySlotView> cachedSlots = new();

    private void Awake()
    {
        playerItemInventory ??= FindAnyObjectByType<PlayerItemInventory>();
    }

    private void OnEnable()
    {
        if (playerItemInventory != null)
            playerItemInventory.OnChanged += HandleInventoryChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerItemInventory != null)
            playerItemInventory.OnChanged -= HandleInventoryChanged;
    }

    public void Refresh()
    {
        ClearRows();

        if (playerItemInventory == null || contentRoot == null || slotPrefab == null)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(true);

            return;
        }

        playerItemInventory.GetSlots(cachedSlots, includeEmpty: true);

        foreach (PlayerItemInventory.ItemInventorySlotView slot in cachedSlots)
        {
            PlayerItemInventorySlotView view = Instantiate(slotPrefab, contentRoot);
            view.Setup(slot);
        }

        if (emptyText != null)
            emptyText.gameObject.SetActive(cachedSlots.Count == 0);
    }

    private void ClearRows()
    {
        if (contentRoot == null)
            return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(contentRoot.GetChild(i).gameObject);
        }
    }

    private void HandleInventoryChanged()
    {
        Refresh();
    }
}
