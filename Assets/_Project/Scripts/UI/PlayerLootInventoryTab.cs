using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class PlayerLootInventoryTab : MonoBehaviour
{
    [SerializeField] private PlayerLootInventory playerLootInventory;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private LootInventorySlotView slotPrefab;
    [SerializeField] private PlayerLootInventoryDragController dragController;
    [SerializeField] private TMP_Text emptyText;

    private readonly List<PlayerLootInventory.LootInventorySlotView> cachedSlots = new();

    private void Awake()
    {
        playerLootInventory ??= FindAnyObjectByType<PlayerLootInventory>();
        dragController ??= FindAnyObjectByType<PlayerLootInventoryDragController>();
    }

    private void OnEnable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged += HandleInventoryChanged;

        Refresh();
    }

    private void OnDisable()
    {
        if (playerLootInventory != null)
            playerLootInventory.OnChanged -= HandleInventoryChanged;
    }

    public void Refresh()
    {
        ClearRows();

        if (playerLootInventory == null || contentRoot == null || slotPrefab == null)
        {
            if (emptyText != null)
                emptyText.gameObject.SetActive(true);

            return;
        }

        playerLootInventory.GetSlots(cachedSlots, includeEmpty: true);

        foreach (PlayerLootInventory.LootInventorySlotView slot in cachedSlots)
        {
            LootInventorySlotView view = Instantiate(slotPrefab, contentRoot);
            view.Setup(slot, dragController);
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
